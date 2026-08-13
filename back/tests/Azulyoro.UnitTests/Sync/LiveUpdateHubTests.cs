using Azulyoro.Infrastructure.Sync;

namespace Azulyoro.UnitTests.Sync;

public class LiveUpdateHubTests
{
    [Fact]
    public async Task Broadcasts_one_provider_update_to_each_subscriber()
    {
        var hub = new LiveUpdateHub();
        var fixtureId = Guid.CreateVersion7();
        await using var first = hub.Subscribe(fixtureId);
        await using var second = hub.Subscribe(fixtureId);

        var update = new LiveFixtureUpdate(
            fixtureId,
            "SecondHalf",
            67,
            2,
            1,
            [new LiveEventUpdate(67, null, "Goal", "Normal Goal", "Boca Juniors", "Jugador", null)]);

        hub.Publish(update);

        var firstReader = first.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        var secondReader = second.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        try
        {
            Assert.True(await firstReader.MoveNextAsync());
            Assert.True(await secondReader.MoveNextAsync());
            Assert.Equal(update, firstReader.Current);
            Assert.Equal(update, secondReader.Current);
        }
        finally
        {
            await firstReader.DisposeAsync();
            await secondReader.DisposeAsync();
        }
    }
}
