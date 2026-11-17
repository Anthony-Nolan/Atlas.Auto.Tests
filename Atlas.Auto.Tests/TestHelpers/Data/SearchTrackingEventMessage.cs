using Azure.Messaging.ServiceBus;

namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// Custom properties of one search-tracking-events message, as Atlas sets them (SearchTrackingServiceBusClient).
/// </summary>
internal record SearchTrackingEventMessage(
    string? EventType,
    string? SearchIdentifier,
    bool HasOriginalSearchIdentifier,
    string? OriginalSearchIdentifier,
    string? AttemptNumber)
{
    public const string EventTypeProperty = "EventType";
    public const string SearchIdentifierProperty = "SearchIdentifier";
    public const string OriginalSearchIdentifierProperty = "OriginalSearchIdentifier";
    public const string AttemptNumberProperty = "AttemptNumber";

    public static SearchTrackingEventMessage From(ServiceBusReceivedMessage message)
    {
        var properties = message.ApplicationProperties;
        return new SearchTrackingEventMessage(
            Read(properties, EventTypeProperty),
            Read(properties, SearchIdentifierProperty)?.ToLowerInvariant(),
            properties.ContainsKey(OriginalSearchIdentifierProperty),
            Read(properties, OriginalSearchIdentifierProperty)?.ToLowerInvariant(),
            Read(properties, AttemptNumberProperty));
    }

    /// <summary>
    /// GUIDs come back as <see cref="Guid"/>, attempt numbers as numbers: compare them as text.
    /// </summary>
    private static string? Read(IReadOnlyDictionary<string, object> properties, string name) =>
        properties.TryGetValue(name, out var value) ? value?.ToString() : null;
}
