using System.Reflection;
using Daybreak.Common.Features.Hooks;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.Graphics.Shaders;

namespace Refreshes.Content.RunningEffects;

internal sealed class RunDustSwap
{
    private static ILHook _spawnFastRunParticlesHook;

    [OnLoad]
    private static void ApplyEdits()
    {
        var m_spawnFastRunParticles = typeof(Player).GetMethod("SpawnFastRunParticles", BindingFlags.Instance | BindingFlags.NonPublic);
        _spawnFastRunParticlesHook = new ILHook(m_spawnFastRunParticles, ReplaceRunningDusts);
    }

    [OnUnload]
    private static void Unhook()
    {
        _spawnFastRunParticlesHook.Dispose();
    }

    private static void ReplaceRunningDusts(ILContext ctx)
    {
        var c = new ILCursor(ctx);
        while (c.TryGotoNext(MoveType.Before, i => i.MatchCall("Terraria.Dust", "NewDust")))
        {
            continue;
        }

        // remove original NewDust call due to a different approach
        c.Remove();
        // push player onto stack
        c.EmitLdarg0();
        // capture original call parameters to not mess with compat
        c.EmitDelegate(
            (Vector2 position, int width, int height, int type, float speedX, float speedY, int alpha, Color color, float scale, Player player) =>
            {
                if (Main.rand.NextBool(3))
                {
                    var tilePos = position.ToTileCoordinates();
                    var dustWhoAmI = WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, Main.tile[tilePos]);
                    Main.dust[dustWhoAmI].position.X = player.Center.X;
                    // move up so the dust isnt at the center of the tile below the player
                    Main.dust[dustWhoAmI].position.Y -= 8;
                    Main.dust[dustWhoAmI].velocity.Y = -2f;
                    Main.dust[dustWhoAmI].velocity.X = player.direction * 2;
                    Main.dust[dustWhoAmI].scale *= 0.8f;
                    Main.dust[dustWhoAmI].shader = GameShaders.Armor.GetSecondaryShader(player.cShoe, player);
                }
            }
        );
        c.EmitRet();
    }
}
