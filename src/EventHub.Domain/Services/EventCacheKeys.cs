using EventHub.Domain.Models;

namespace EventHub.Domain.Services;

internal static class EventCacheKeys
{
    public const string ListPrefix = "events:list:";

    public static string List(EventFilter f) =>
        $"{ListPrefix}c={f.CategoryId}:l={f.Location?.ToLowerInvariant()}:f={f.From:o}:t={f.To:o}";

    public static string Detail(Guid id) => $"events:details:{id}";
    
    public const  string Categories = "categories:list";
    
}
