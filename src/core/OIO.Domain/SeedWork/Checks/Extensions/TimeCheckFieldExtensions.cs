using System;
using System.Globalization;
using FluentCheck.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class TimeCheckFieldExtensions
{
    // ---------- helpers ----------
    private static string F(DateTime dt)
        => dt.ToString("O", CultureInfo.InvariantCulture);

    private static string F(DateTimeOffset dto)
        => dto.ToString("O", CultureInfo.InvariantCulture);
    
    private static string F(DateOnly d)
        => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string F(TimeSpan ts)
        => ts.ToString("c", CultureInfo.InvariantCulture); // [-]d.hh:mm:ss.fffffff
    
    private static readonly TimeSpan Day = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaxTimeOfDay = Day - TimeSpan.FromTicks(1);
    private static void GuardTimeOfDay(TimeSpan t, string paramName)
    {
        if (t < TimeSpan.Zero || t >= Day)
            throw new ArgumentOutOfRangeException(paramName, t, "Time-of-day must be in [00:00, 24:00).");
    }

    private static bool IsValidTimeOfDay(TimeSpan t) => t >= TimeSpan.Zero && t < Day;
    
    /// <summary>
    /// Expand circular interval into 1 or 2 linear intervals in [0, 24h).
    /// Semantics: half-open [start, end) by default. If start == end and treatEqualAsFullDay = true => full day [0, 24h).
    /// </summary>
    private static int Expand(
        TimeSpan start,
        TimeSpan end,
        (TimeSpan a, TimeSpan b)[] buffer,
        bool treatEqualAsFullDay)
    {
        if (start == end && treatEqualAsFullDay)
        {
            buffer[0] = (TimeSpan.Zero, Day);
            return 1;
        }

        if (start < end)
        {
            buffer[0] = (start, end);
            return 1;
        }

        // wrap
        buffer[0] = (start, Day);
        buffer[1] = (TimeSpan.Zero, end);
        return 2;
    }

    private static bool ContainsCircular(
        TimeSpan value,
        TimeSpan start,
        TimeSpan end,
        bool inclusiveEnd,
        bool treatEqualAsFullDay)
    {
        if (start == end && treatEqualAsFullDay)
            return true;

        if (start < end)
            return inclusiveEnd ? (value >= start && value <= end) : (value >= start && value < end);

        // wrap
        return inclusiveEnd ? (value >= start || value <= end) : (value >= start || value < end);
    }

    private static bool OverlapsHalfOpen((TimeSpan a, TimeSpan b) x, (TimeSpan a, TimeSpan b) y)
        => x.a < y.b && y.a < x.b;

    // ============================================================
    // DateTime
    // ============================================================

    public static CheckField<TOwner, DateTime> After<TOwner>(
        this CheckField<TOwner, DateTime> check,
        DateTime time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property > time) 
            return check;

        var err = error ?? check.Property.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> AfterIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        DateTime time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) return check;

        var v = check.Property.Value;
        
        if (v > time) 
            return check;

        var err = error ?? v.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime> Before<TOwner>(
        this CheckField<TOwner, DateTime> check,
        DateTime time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property < time)
            return check;

        var err = error ?? check.Property.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> BeforeIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        DateTime time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) return check;

        var v = check.Property.Value;
        
        if (v < time) 
            return check;

        var err = error ?? v.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime> TimeRange<TOwner>(
        this CheckField<TOwner, DateTime> check,
        DateTime start,
        DateTime end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var v = check.Property;
        
        if (v >= start && v <= end) 
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> TimeRangeIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        DateTime start,
        DateTime end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) return check;

        var v = check.Property.Value;
        
        if (v >= start && v <= end)
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime> NotInPast<TOwner>(
        this CheckField<TOwner, DateTime> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        var now = (nowProvider ?? (() => DateTime.UtcNow))();
        
        if (check.Property >= now)
            return check;

        var err = error ?? check.Property.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> NotInPastIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) return check;

        var now = (nowProvider ?? (() => DateTime.UtcNow))();
        if (check.Property.Value >= now) 
            return check;

        var err = error ?? check.Property.Value.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime> NotInFuture<TOwner>(
        this CheckField<TOwner, DateTime> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) 
            return check;

        var now = (nowProvider ?? (() => DateTime.UtcNow))();
        
        if (check.Property <= now) 
            return check;

        var err = error ?? check.Property.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> NotInFutureIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var now = (nowProvider ?? (() => DateTime.UtcNow))();
        
        if (check.Property.Value <= now)
            return check;

        var err = error ?? check.Property.Value.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime> Utc<TOwner>(
        this CheckField<TOwner, DateTime> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property.Kind == DateTimeKind.Utc) 
            return check;

        var err = error ?? check.Property.UtcError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTime?> UtcIfHasValue<TOwner>(
        this CheckField<TOwner, DateTime?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var v = check.Property.Value;
        
        if (v.Kind == DateTimeKind.Utc) 
            return check;

        var err = error ?? v.UtcError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    /// <summary>
    /// Check [start, end) not overlapping with [otherStart, otherEnd)
    /// - This field: start
    /// - Parameter: end
    /// </summary>
    public static CheckField<TOwner, DateTime> NotOverlapping<TOwner>(
        this CheckField<TOwner, DateTime> start,
        DateTime end,
        DateTime otherStart,
        DateTime otherEnd,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue) return start;

        // overlap iff start < otherEnd && otherStart < end
        var overlap = start.Property < otherEnd && otherStart < end;
        if (!overlap) return start;
        
        message ??= $"{start.FieldName} must not overlap with [{otherStart:O}, {otherEnd:O}). " +
                    $"Current: [{start.Property:O}, {end:O}).";


        var err = error ?? start.Property.NotOverlappingError(
            isInvariant: start.IsInvariant,
            codePrefix: start.OwnerName,
            propertyName: start.PropertyName,
            field: start.FieldName,
            message: message);

        return start.Fail(err);
    }

    // ============================================================
    // DateTimeOffset
    // ============================================================

    public static CheckField<TOwner, DateTimeOffset> After<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        DateTimeOffset time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property > time)
            return check;

        var err = error ?? check.Property.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> AfterIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        DateTimeOffset time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var v = check.Property.Value;
        
        if (v > time) return check;

        var err = error ?? v.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> Before<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        DateTimeOffset time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property < time)
            return check;

        var err = error ?? check.Property.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> BeforeIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        DateTimeOffset time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var v = check.Property.Value;
        
        if (v < time) return check;

        var err = error ?? v.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> TimeRange<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        DateTimeOffset start,
        DateTimeOffset end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        var v = check.Property;
        
        if (v >= start && v <= end)
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> TimeRangeIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        DateTimeOffset start,
        DateTimeOffset end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var v = check.Property.Value;
        
        if (v >= start && v <= end)
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> NotInPast<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        Func<DateTimeOffset>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) 
            return check;

        var now = (nowProvider ?? (() => DateTimeOffset.UtcNow))();
        
        if (check.Property >= now) 
            return check;

        var err = error ?? check.Property.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> NotInPastIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        Func<DateTimeOffset>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var now = (nowProvider ?? (() => DateTimeOffset.UtcNow))();
        
        if (check.Property.Value >= now)
            return check;

        var err = error ?? check.Property.Value.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> NotInFuture<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        Func<DateTimeOffset>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        var now = (nowProvider ?? (() => DateTimeOffset.UtcNow))();
        
        if (check.Property <= now)
            return check;

        var err = error ?? check.Property.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> NotInFutureIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        Func<DateTimeOffset>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var now = (nowProvider ?? (() => DateTimeOffset.UtcNow))();
        
        if (check.Property.Value <= now) 
            return check;

        var err = error ?? check.Property.Value.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> Utc<TOwner>(
        this CheckField<TOwner, DateTimeOffset> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property.Offset == TimeSpan.Zero) 
            return check;

        var err = error ?? check.Property.UtcError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset?> UtcIfHasValue<TOwner>(
        this CheckField<TOwner, DateTimeOffset?> check,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var v = check.Property.Value;
        
        if (v.Offset == TimeSpan.Zero)
            return check;

        var err = error ?? v.UtcError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateTimeOffset> NotOverlapping<TOwner>(
        this CheckField<TOwner, DateTimeOffset> start,
        DateTimeOffset end,
        DateTimeOffset otherStart,
        DateTimeOffset otherEnd,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue)
            return start;

        var overlap = start.Property < otherEnd && otherStart < end;
        
        if (!overlap)
            return start;

        var err = error ?? start.Property.NotOverlappingError(
            isInvariant: start.IsInvariant,
            codePrefix: start.OwnerName,
            propertyName: start.PropertyName,
            field: start.FieldName,
            message: message);

        return start.Fail(err);
    }
    
     // ============================================================
    // DateOnly
    // ============================================================

    public static CheckField<TOwner, DateOnly> After<TOwner>(
        this CheckField<TOwner, DateOnly> check,
        DateOnly time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property > time) 
            return check;

        var err = error ?? check.Property.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly?> AfterIfHasValue<TOwner>(
        this CheckField<TOwner, DateOnly?> check,
        DateOnly time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var v = check.Property.Value;
        
        if (v > time)
            return check;

        var err = error ?? v.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly> Before<TOwner>(
        this CheckField<TOwner, DateOnly> check,
        DateOnly time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property < time) 
            return check;

        var err = error ?? check.Property.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly?> BeforeIfHasValue<TOwner>(
        this CheckField<TOwner, DateOnly?> check,
        DateOnly time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var v = check.Property.Value;
        
        if (v < time) 
            return check;

        var err = error ?? v.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly> TimeRange<TOwner>(
        this CheckField<TOwner, DateOnly> check,
        DateOnly start,
        DateOnly end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) 
            return check;

        var v = check.Property;
        
        if (v >= start && v <= end) 
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly?> TimeRangeIfHasValue<TOwner>(
        this CheckField<TOwner, DateOnly?> check,
        DateOnly start,
        DateOnly end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var v = check.Property.Value;
        
        if (v >= start && v <= end) 
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }
    
    public static CheckField<TOwner, DateOnly> NotInPast<TOwner>(
        this CheckField<TOwner, DateOnly> check,
        Func<DateOnly>? todayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var today = (todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow)))();
        
        if (check.Property >= today)
            return check;

        var err = error ?? check.Property.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly?> NotInPastIfHasValue<TOwner>(
        this CheckField<TOwner, DateOnly?> check,
        Func<DateOnly>? todayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var today = (todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow)))();
        
        if (check.Property.Value >= today)
            return check;

        var err = error ?? check.Property.Value.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly> NotInFuture<TOwner>(
        this CheckField<TOwner, DateOnly> check,
        Func<DateOnly>? todayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        var today = (todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow)))();
        
        if (check.Property <= today) 
            return check;

        var err = error ?? check.Property.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly?> NotInFutureIfHasValue<TOwner>(
        this CheckField<TOwner, DateOnly?> check,
        Func<DateOnly>? todayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        var today = (todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow)))();
        
        if (check.Property.Value <= today)
            return check;

        var err = error ?? check.Property.Value.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, DateOnly> NotOverlapping<TOwner>(
        this CheckField<TOwner, DateOnly> start,
        DateOnly end,
        DateOnly otherStart,
        DateOnly otherEnd,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue)
            return start;

        var overlap = start.Property < otherEnd && otherStart < end;
        
        if (!overlap) 
            return start;

        message ??= $"{start.FieldName} must not overlap with [{F(otherStart)}, {F(otherEnd)}).";

        var err = error ?? start.Property.NotOverlappingError(
            isInvariant: start.IsInvariant,
            codePrefix: start.OwnerName,
            propertyName: start.PropertyName,
            field: start.FieldName,
            message: message);

        return start.Fail(err);
    }

    // ============================================================
    // TimeSpan
    // ============================================================

    public static CheckField<TOwner, TimeSpan> After<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        TimeSpan time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property > time) 
            return check;

        var err = error ?? check.Property.AfterError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan> Before<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        TimeSpan time,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || check.Property < time) return check;

        var err = error ?? check.Property.BeforeError(
            time: F(time),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan> TimeRange<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        TimeSpan start,
        TimeSpan end,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var v = check.Property;
        
        if (v >= start && v <= end) 
            return check;

        var err = error ?? v.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }
    
    public static CheckField<TOwner, TimeSpan> NotInPast<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        Func<TimeSpan>? nowTimeOfDayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue)
            return check;

        var now = (nowTimeOfDayProvider ?? (() => DateTime.UtcNow.TimeOfDay))();
        
        if (check.Property >= now)
            return check;

        var err = error ?? check.Property.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan?> NotInPastIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> check,
        Func<TimeSpan>? nowTimeOfDayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var now = (nowTimeOfDayProvider ?? (() => DateTime.UtcNow.TimeOfDay))();
        
        if (check.Property.Value >= now)
            return check;

        var err = error ?? check.Property.Value.NotInPastError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan> NotInFuture<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        Func<TimeSpan>? nowTimeOfDayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) 
            return check;

        var now = (nowTimeOfDayProvider ?? (() => DateTime.UtcNow.TimeOfDay))();
        
        if (check.Property <= now) 
            return check;

        var err = error ?? check.Property.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan?> NotInFutureIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> check,
        Func<TimeSpan>? nowTimeOfDayProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) 
            return check;

        var now = (nowTimeOfDayProvider ?? (() => DateTime.UtcNow.TimeOfDay))();
        
        if (check.Property.Value <= now)
            return check;

        var err = error ?? check.Property.Value.NotInFutureError(
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }
    
    public static CheckField<TOwner, TimeSpan> NotOverlapping<TOwner>(
        this CheckField<TOwner, TimeSpan> start,
        TimeSpan end,
        TimeSpan otherStart,
        TimeSpan otherEnd,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue) return start;

        var overlap = start.Property < otherEnd && otherStart < end;
        
        if (!overlap)
            return start;

        var err = error ?? start.Property.NotOverlappingError(
            isInvariant: start.IsInvariant,
            codePrefix: start.OwnerName,
            propertyName: start.PropertyName,
            field: start.FieldName,
            message: message);

        return start.Fail(err);
    }
    
    extension<TOwner>(CheckField<TOwner, TimeSpan?> check)
    {
        public CheckField<TOwner, TimeSpan?> AfterIfHasValue(TimeSpan time,
            string? message = null, Error? error = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue) 
                return check;
        
            return check.Property.Value
                .Narrowed(check)
                .After(time, message, error)
                .Widen();
        }

        public CheckField<TOwner, TimeSpan?> BeforeIfHasValue(TimeSpan time,
            string? message = null, Error? error = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue)
                return check;
        
            return check.Property.Value
                .Narrowed(check)
                .Before(time, message, error)
                .Widen();
        }

        public CheckField<TOwner, TimeSpan?> TimeRangeIfHasValue(TimeSpan start, TimeSpan end,
            string? message = null, Error? error = null)
        {
            if (!check.ShouldContinue || !check.Property.HasValue)
                return check;
        
            return check.Property.Value
                .Narrowed(check)
                .TimeRange(start, end, message, error)
                .Widen();
        }
    }

    // --- tiny helpers to reuse non-null impl without duplicating logic
    private static CheckField<TOwner, TimeSpan> Narrowed<TOwner>(
        this TimeSpan value, CheckField<TOwner, TimeSpan?> origin)
        => origin.Narrow(value);

    private static CheckField<TOwner, TimeSpan?> Widen<TOwner>(
        this CheckField<TOwner, TimeSpan> narrowed)
        => narrowed.Narrow<TimeSpan?>(narrowed.Property);

    #region TimeSpan Time Of Day Checks

    extension<TOwner>(CheckField<TOwner, TimeSpan> check)
    {
        public CheckField<TOwner, TimeSpan> NotInPastTimeOfDay(
            Func<DateTime>? nowProvider = null,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue) 
                return check;

            var now = (nowProvider ?? (() => DateTime.UtcNow))();
            
            if (check.Property >= now.TimeOfDay)
                return check;

            var err = error ?? check.Property.NotInPastError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, TimeSpan> NotInFutureTimeOfDay(
            Func<DateTime>? nowProvider = null,
            string? message = null,
            Error? error = null)
        {
            if (!check.ShouldContinue)
                return check;

            var now = (nowProvider ?? (() => DateTime.UtcNow))();
            
            if (check.Property <= now.TimeOfDay)
                return check;

            var err = error ?? check.Property.NotInFutureError(
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(err);
        }
    }

    // nullable (skip if null)
    public static CheckField<TOwner, TimeSpan?> NotInPastTimeOfDayIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;
        
        return check
            .Narrow(check.Property.Value)
            .NotInPastTimeOfDay(nowProvider, message, error)
            .Narrow<TimeSpan?>(check.Property);
    }

    public static CheckField<TOwner, TimeSpan?> NotInFutureTimeOfDayIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> check,
        Func<DateTime>? nowProvider = null,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;
        return check
            .Narrow(check.Property.Value)
            .NotInFutureTimeOfDay(nowProvider, message, error)
            .Narrow<TimeSpan?>(check.Property);
    }
    
    // ---------------------------------------------------------------------
    // TimeOfDayRange (wrap midnight)
    // ---------------------------------------------------------------------

    public static CheckField<TOwner, TimeSpan> TimeOfDayRange<TOwner>(
        this CheckField<TOwner, TimeSpan> check,
        TimeSpan start,
        TimeSpan end,
        bool inclusiveEnd = true,
        bool treatEqualAsFullDay = true,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        GuardTimeOfDay(start, nameof(start));
        GuardTimeOfDay(end, nameof(end));

        if (!IsValidTimeOfDay(check.Property))
        {
            // Property invalid as time-of-day
            message ??= $"{check.FieldName} must be a time-of-day in [00:00, 24:00).";
            var errInvalid = error ?? check.Property.TimeRangeError(
                timeStart: F(TimeSpan.Zero),
                timeEnd: F(MaxTimeOfDay),
                isInvariant: check.IsInvariant,
                codePrefix: check.OwnerName,
                propertyName: check.PropertyName,
                field: check.FieldName,
                message: message);

            return check.Fail(errInvalid);
        }

        if (ContainsCircular(check.Property, start, end, inclusiveEnd, treatEqualAsFullDay))
            return check;

        var err = error ?? check.Property.TimeRangeError(
            timeStart: F(start),
            timeEnd: F(end),
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, TimeSpan?> TimeOfDayRangeIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> check,
        TimeSpan start,
        TimeSpan end,
        bool inclusiveEnd = true,
        bool treatEqualAsFullDay = true,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue || !check.Property.HasValue) return check;

        // reuse non-null impl (carry fieldFailed via Narrow)
        var narrowed = check
            .Narrow(check.Property.Value)
            .TimeOfDayRange(start, end, inclusiveEnd, treatEqualAsFullDay, message, error);

        return check; // state đã được fail vào State; nullable check giữ nguyên type
    }

    // ---------------------------------------------------------------------
    // NotOverlappingTimeOfDay (circular overlap)
    //   This field is "start", parameter "end" is end of current range
    //   Overlap check uses half-open semantics [start, end)
    //   Touching at boundary is allowed: end == otherStart => NOT overlap
    // ---------------------------------------------------------------------

    public static CheckField<TOwner, TimeSpan> NotOverlappingTimeOfDay<TOwner>(
        this CheckField<TOwner, TimeSpan> start,
        TimeSpan end,
        TimeSpan otherStart,
        TimeSpan otherEnd,
        bool treatEqualAsFullDay = true,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue) return start;

        GuardTimeOfDay(start.Property, "start.Property");
        GuardTimeOfDay(end, nameof(end));
        GuardTimeOfDay(otherStart, nameof(otherStart));
        GuardTimeOfDay(otherEnd, nameof(otherEnd));

        // Expand both circular ranges to linear segments
        var aBuf = new (TimeSpan a, TimeSpan b)[2];
        var bBuf = new (TimeSpan a, TimeSpan b)[2];

        var aCount = Expand(start.Property, end, aBuf, treatEqualAsFullDay);
        var bCount = Expand(otherStart, otherEnd, bBuf, treatEqualAsFullDay);

        for (int i = 0; i < aCount; i++)
        for (int j = 0; j < bCount; j++)
        {
            if (OverlapsHalfOpen(aBuf[i], bBuf[j]))
            {
                // Bạn có thể tự override message cho rõ overlap với khoảng nào
                message ??= $"{start.FieldName} must not overlap with [{F(otherStart)}, {F(otherEnd)}) (time-of-day).";

                var err = error ?? start.Property.NotOverlappingError(
                    isInvariant: start.IsInvariant,
                    codePrefix: start.OwnerName,
                    propertyName: start.PropertyName,
                    field: start.FieldName,
                    message: message);

                return start.Fail(err);
            }
        }

        return start;
    }

    public static CheckField<TOwner, TimeSpan?> NotOverlappingTimeOfDayIfHasValue<TOwner>(
        this CheckField<TOwner, TimeSpan?> start,
        TimeSpan end,
        TimeSpan otherStart,
        TimeSpan otherEnd,
        bool treatEqualAsFullDay = true,
        string? message = null,
        Error? error = null)
    {
        if (!start.ShouldContinue || !start.Property.HasValue) return start;

        // narrow then validate overlap; state fail sẽ được ghi vào State
        start.Narrow(start.Property.Value)
            .NotOverlappingTimeOfDay(end, otherStart, otherEnd, treatEqualAsFullDay, message, error);

        return start;
    }
    #endregion
}