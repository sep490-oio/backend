using System.Collections.Concurrent;
using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OIO.Infrastructure.Outbox;

internal interface IOutboxMessageResolver
{
    internal Result<object, Error> DeserializeEvent(string type, string content);
}


internal sealed class OutboxMessageResolver : IOutboxMessageResolver
{
   
    
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

    public Result<object, Error> DeserializeEvent(string type, string content)
    {
        try
        {
            var messageType = GetOrAddMessageType(type);
            var deserialized = JsonSerializer.Deserialize(content, messageType);

            if (deserialized is null)
            {
                throw new InvalidOperationException(
                    $"Deserialized outbox message content is null for type '{type}'.");
            }
            
            return deserialized;
        }
        catch (Exception e)
        {
            return Error.Unexpected("OutboxMessage.Deserialize.failed.", e.Message);
        }
    }

    private static Type GetOrAddMessageType(string typename)
    {
        return TypeCache.GetOrAdd(typename, t =>
            Domain.AssemblyReference.Assembly.GetType(t)
            ?? throw new InvalidOperationException($"Failed to get outbox message type '{t}' from assembly.")
        );
    }
        
}