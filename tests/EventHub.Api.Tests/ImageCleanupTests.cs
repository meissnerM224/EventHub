using System.Net;
using System.Net.Http.Json;
using Amazon.S3;
using EventHub.Api.Models;
using EventHub.Domain.Storage;
using EventHub.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public sealed class ImageCleanupTests(EventHubWebApplicationFactory factory)
{
    [Fact]
    public async Task Cleanup_DeletesOldOrphans_AndKeepsReferencedImages()
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var orphan = await UploadAsync(organizer);
        var used = await UploadAsync(organizer);
        await CreateEventWithImageAsync(organizer, used);

        // Past the 24 h grace period.
        using (factory.Clock.Advance(TimeSpan.FromHours(25)))
        {
            await RunCleanerAsync();
        }

        Assert.False(await ExistsAsync(orphan));
        Assert.True(await ExistsAsync(used));
    }

    [Fact]
    public async Task Cleanup_KeepsRecentOrphans_TheFormMayStillBeOpen()
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var fresh = await UploadAsync(organizer);

        await RunCleanerAsync();

        Assert.True(await ExistsAsync(fresh));
    }

    private async Task RunCleanerAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrphanImageCleaner>().RunAsync(CancellationToken.None);
    }

    private static async Task<string> UploadAsync(HttpClient organizer)
    {
        var response = await organizer.PostAsync("/api/uploads/images", EndpointTests.ImageContent());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadResult>())!.Url;
    }

    private static async Task CreateEventWithImageAsync(HttpClient organizer, string image)
    {
        var response = await organizer.PostAsJsonAsync("/api/events", new
        {
            title = $"Cleanup {Guid.NewGuid():N}",
            description = "Test description",
            image,
            location = "Theater Kassel",
            startAt = DateTimeOffset.UtcNow.AddDays(3),
            doorsOpenAt = DateTimeOffset.UtcNow.AddDays(3).AddHours(-1),
            maxParticipants = 10,
            categoryId = EventHubWebApplicationFactory.FirstCategoryId,
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<bool> ExistsAsync(string url)
    {
        try
        {
            await factory.S3.GetObjectMetadataAsync(ImagePath.Bucket, url[$"/{ImagePath.Bucket}/".Length..]);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
