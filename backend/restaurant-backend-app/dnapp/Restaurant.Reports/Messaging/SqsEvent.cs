using System.Text.Json;

namespace Restaurant.Reports.Messaging;

public sealed record SqsEvent(
    string EventType,
    JsonElement Payload
);