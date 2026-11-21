using System.Numerics;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class TileMovementTest : MovementTest
{
    protected override bool AddWalls => false;

    [Test]
    public async Task MovementStopsAtTileCenters()
    {
        await MoveAndSettle(DirectionFlag.East, 0.4f);

        var position = Transform.GetWorldPosition(ToServer(Player));
        Assert.That(position.X % 1f, Is.EqualTo(0.5f).Within(0.02f));
        Assert.That(position.Y % 1f, Is.EqualTo(0.5f).Within(0.02f));
        Assert.That(position.X, Is.GreaterThan(0.5f));
    }

    [Test]
    public async Task BlockedDiagonalFallsBackToValidCardinalStep()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);
        await SetTile(Plating, SEntMan.GetNetCoordinates(coords.Offset(new Vector2(1, 1))), MapData.Grid);
        var wall = await Spawn("WallSolid", SEntMan.GetNetCoordinates(coords.Offset(new Vector2(1, 1))));
        await Server.WaitPost(() =>
        {
            var wallUid = ToServer(wall);
            var xform = SEntMan.GetComponent<TransformComponent>(wallUid);
            var meta = SEntMan.GetComponent<MetaDataComponent>(wallUid);
            Server.System<SharedTransformSystem>().AnchorEntity((wallUid, xform, meta), MapData.Grid);
        });

        var start = Transform.GetWorldPosition(ToServer(Player));
        await MoveAndSettle(DirectionFlag.NorthEast, 0.2f);

        var position = Transform.GetWorldPosition(ToServer(Player));
        Assert.That(position.X, Is.GreaterThan(start.X));
        Assert.That(position.Y, Is.EqualTo(start.Y).Within(0.02f));
    }

    [Test]
    public async Task ValidDiagonalMovesOneTileOnBothAxes()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);
        foreach (var offset in new[] { new Vector2(0, 1), new Vector2(1, 1) })
        {
            await SetTile(Plating, SEntMan.GetNetCoordinates(coords.Offset(offset)), MapData.Grid);
        }

        var start = Transform.GetWorldPosition(ToServer(Player));
        await MoveAndSettle(DirectionFlag.NorthEast, 0.2f);

        var position = Transform.GetWorldPosition(ToServer(Player));
        Assert.That(position.X, Is.GreaterThan(start.X));
        Assert.That(position.Y, Is.GreaterThan(start.Y));
        Assert.That(position.X - start.X, Is.EqualTo(position.Y - start.Y).Within(0.02f));
        Assert.That(position.X % 1f, Is.EqualTo(0.5f).Within(0.02f));
        Assert.That(position.Y % 1f, Is.EqualTo(0.5f).Within(0.02f));
    }

    [Test]
    public async Task WallMountedMachineDoesNotBlockMovement()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);
        var machine = await Spawn("APCBasic", SEntMan.GetNetCoordinates(coords.Offset(new Vector2(1, 0))));
        await Server.WaitPost(() =>
        {
            var uid = ToServer(machine);
            var xform = SEntMan.GetComponent<TransformComponent>(uid);
            var meta = SEntMan.GetComponent<MetaDataComponent>(uid);
            Server.System<SharedTransformSystem>().AnchorEntity((uid, xform, meta), MapData.Grid);
        });

        await MoveAndSettle(DirectionFlag.East, 0.4f);

        var position = Transform.GetWorldPosition(ToServer(Player));
        Assert.That(position.X, Is.GreaterThan(1.49f));
    }

    [Test]
    public async Task AirlockOpensForTileMovement()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);
        await SetTile(Plating, SEntMan.GetNetCoordinates(coords.Offset(new Vector2(1, 0))), MapData.Grid);
        var airlock = await Spawn("WoodDoor", SEntMan.GetNetCoordinates(coords.Offset(new Vector2(1, 0))));
        await Server.WaitPost(() =>
        {
            var uid = ToServer(airlock);
            var xform = SEntMan.GetComponent<TransformComponent>(uid);
            var meta = SEntMan.GetComponent<MetaDataComponent>(uid);
            Server.System<SharedTransformSystem>().AnchorEntity((uid, xform, meta), MapData.Grid);
        });

        await MoveAndSettle(DirectionFlag.East, 0.4f);

        var door = Comp<DoorComponent>(airlock);
        Assert.That(door.State, Is.Not.EqualTo(DoorState.Closed));
        var position = Transform.GetWorldPosition(ToServer(Player));
        Assert.That(position.X, Is.GreaterThan(1.49f));
    }

    private async Task MoveAndSettle(DirectionFlag direction, float seconds)
    {
        await SetMovementKey(direction, BoundKeyState.Down);
        await RunSeconds(seconds);
        await SetMovementKey(direction, BoundKeyState.Up);
        await RunSeconds(1);
    }
}
