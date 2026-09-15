using EventHub.Domain.Interfaces;

namespace EventHub.Infrastructure.Persistence;

public class EfTransactionRunner(EventHubDbContext context) : ITransactionRunner
{
    public async Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await work();
        await transaction.CommitAsync();
        return result;
    }
}