using System.Text.Json.Serialization;

namespace OIO.Application.Context.PaymentContext.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethodStatusFilter { Active, Disabled, All }
