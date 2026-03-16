using Infrastructure.Concurrency;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

public class GameLockManagerTests
{
    private readonly GameLockManager _sut = new();

    [Fact]
    public async Task AcquireAsync_returns_non_null_IDisposable()
    {
        var gameId = Guid.NewGuid();

        using var handle = await _sut.AcquireAsync(gameId);

        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task Second_acquire_on_same_gameId_blocks_until_first_is_disposed()
    {
        var gameId = Guid.NewGuid();
        var handle1 = await _sut.AcquireAsync(gameId);

        // Second acquire should not complete within 100ms
        var acquireTask = _sut.AcquireAsync(gameId);
        var completed = await Task.WhenAny(acquireTask, Task.Delay(100));

        completed.ShouldNotBe(acquireTask, "Second acquire should block while first is held");

        // Release first handle — second should now complete
        handle1.Dispose();
        var handle2 = await Task.WhenAny(acquireTask, Task.Delay(1000));
        handle2.ShouldBe(acquireTask, "Second acquire should complete after first is disposed");

        (await acquireTask).Dispose();
    }

    [Fact]
    public async Task Different_gameIds_can_acquire_concurrently()
    {
        var gameId1 = Guid.NewGuid();
        var gameId2 = Guid.NewGuid();

        using var handle1 = await _sut.AcquireAsync(gameId1);

        // Different gameId should acquire immediately
        var acquireTask = _sut.AcquireAsync(gameId2);
        var completed = await Task.WhenAny(acquireTask, Task.Delay(100));

        completed.ShouldBe(acquireTask, "Different gameId should not block");

        (await acquireTask).Dispose();
    }

    [Fact]
    public async Task After_disposing_same_gameId_can_be_reacquired()
    {
        var gameId = Guid.NewGuid();

        var handle1 = await _sut.AcquireAsync(gameId);
        handle1.Dispose();

        // Should acquire without blocking
        var acquireTask = _sut.AcquireAsync(gameId);
        var completed = await Task.WhenAny(acquireTask, Task.Delay(100));

        completed.ShouldBe(acquireTask, "Should be able to re-acquire after dispose");

        (await acquireTask).Dispose();
    }
}
