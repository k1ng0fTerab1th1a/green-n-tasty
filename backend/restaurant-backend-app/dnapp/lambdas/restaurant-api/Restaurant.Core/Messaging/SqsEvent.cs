using System.Text.Json;

namespace Restaurant.Core.Messaging;

public sealed record SqsEvent(
    string EventType,
    JsonElement Payload
);