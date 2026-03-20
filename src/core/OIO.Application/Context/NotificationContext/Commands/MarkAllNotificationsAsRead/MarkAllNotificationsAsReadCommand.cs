using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.NotificationContext.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand() : ICommand<MarkAllNotificationsAsReadResponse>;

public sealed record MarkAllNotificationsAsReadResponse(int UpdatedCount);
