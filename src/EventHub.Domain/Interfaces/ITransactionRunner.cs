namespace EventHub.Domain.Interfaces;

public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> work);
}