using Daybreak.Common.Features.Hooks;
using MonoMod.Cil;
using Terraria.Graphics.Shaders;

namespace Refreshes.Content;

internal static class RunDustSwap
{
    [OnLoad]
    private static void ApplyHooks()
    {
        IL_Player.SpawnFastRunParticles += SpawnFastRunParticles_ReplaceRunningDusts;
    }

    private static void SpawnFastRunParticles_ReplaceRunningDusts(ILContext ctx)
    {
        var c = new ILCursor(ctx)
        {
            Next = null,
        };

        c.GotoPrev(MoveType.Before, x => x.MatchCall<Dust>(nameof(Dust.NewDust)));

        c.Remove();
        c.EmitLdarg0(); // this (Player)
        c.EmitDelegate(
            (Vector2 position, int _, int _, int _, float _, float _, int _, Color _, float _, Player player) =>
            {
                if (!Main.rand.NextBool(3))
                {
                    return;
                }

                var tilePos = position.ToTileCoordinates();
                var dust = Main.dust[WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, Main.tile[tilePos])];
                {
                    // Move up so the dust isn't at the center of the tile below the
                    // player.
                    dust.position.X = player.Center.X;
                    dust.position.Y -= 8;

                    dust.velocity.Y = -2f;
                    dust.velocity.X = player.direction * 2;

                    dust.scale *= 0.8f;

                    dust.shader = GameShaders.Armor.GetSecondaryShader(player.cShoe, player);
                }
            }
        );
        c.EmitRet();
    }
}
