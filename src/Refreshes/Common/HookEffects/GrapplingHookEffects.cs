using System.Collections.Generic;
using Daybreak.Common.Features.Hooks;
using MonoMod.Cil;
using Terraria.Audio;

namespace Refreshes.Common;

/// <summary>
///     Provides custom behavior for our grappling hook changes.
/// </summary>
public interface IGrapplingHookEffects
{
    /// <summary>
    ///     Called when hitting a tile to play a sound.
    /// </summary>
    /// <returns>
    ///     Whether the vanilla hit sound should play.
    /// </returns>
    bool PlayHitSound(
        Projectile projectile,
        Tile tile,
        int tileX,
        int tileY
    );

    /// <summary>
    ///     Called when the grappling hook hits a tile.  Can be used for tile
    ///     on-hit effects, such as particles.
    /// </summary>
    void HitTile(
        Projectile projectile,
        Tile tile,
        int tileX,
        int tileY
    );
}

public static class GrapplingHookEffects
{
    /// <summary>
    ///     Allows you to map a raw implementation of
    ///     <see cref="IGrapplingHookEffects"/> to an existing hook projectile
    ///     ID (either from vanilla, another mod, or your own if you're
    ///     weak-referencing this mod).
    /// </summary>
    public static Dictionary<int, IGrapplingHookEffects> CustomEffects { get; } = [];

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        CustomEffects.Clear();
    }

    public static IGrapplingHookEffects? GetEffects(int projId)
    {
        if (CustomEffects.TryGetValue(projId, out var effects))
        {
            return effects;
        }

        return ProjectileLoader.GetProjectile(projId) as IGrapplingHookEffects;
    }

    [OnLoad]
    private static void ApplyHooks()
    {
        IL_Projectile.AI_007_GrapplingHooks += AI_007_GrapplingHooks_ApplySoundAndParticleEffects;
    }

    private static void AI_007_GrapplingHooks_ApplySoundAndParticleEffects(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.Before, i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));

        c.EmitLdarg0();
        c.EmitDelegate(
            (int type, int x, int y, int style, float volumeScale, float pitchOffset, Projectile self) =>
            {
                var tileX = x / 16;
                var tileY = y / 16;
                var tile = Framing.GetTileSafely(tileX, tileY);

                var effects = GetEffects(self.type);
                if (effects?.PlayHitSound(self, tile, tileX, tileY) ?? true)
                {
                    SoundEngine.PlaySound(type, x, y, style, volumeScale, pitchOffset);
                }

                effects?.HitTile(self, tile, tileX, tileY);
            }
        );

        // Immediately gets popped since PlaySound normally returns a value.
        c.Remove();
        c.EmitLdnull();
    }
}
