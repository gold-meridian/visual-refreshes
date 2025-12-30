using Daybreak.Common.Features.Hooks;
using MonoMod.Cil;
using Terraria.Graphics.Shaders;
using Terraria.ID;

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
                var tile = Main.tile[tilePos];

                if (tile.IsTileInvisible) return;

                var dust = Main.dust[WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, tile)];
                {
                    // Move up so the dust isn't at the center of the tile below the
                    // player.
                    dust.position.X = player.Center.X;
                    dust.position.Y -= 8;

                    dust.velocity.Y = -2f;
                    dust.velocity.X = player.direction * 2;

                    dust.scale *= 0.8f;

                    if (tile.TileColor > PaintID.None)
                        dust.shader = GameShaders.Armor.GetSecondaryShader(tile.TileColor, player);
                }
            }
        );
        c.EmitRet();
    }
}