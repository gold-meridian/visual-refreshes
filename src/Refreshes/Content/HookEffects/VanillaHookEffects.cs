using Daybreak.Common.Features.Hooks;
using Refreshes.Common;
using Terraria.Audio;
using Terraria.ID;

namespace Refreshes.Content.HookEffects;

internal static class VanillaHookEffects
{
    private abstract class HookUnimplemented : IGrapplingHookEffects
    {
        bool IGrapplingHookEffects.PlayHitSound(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            return true;
        }

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY) { }
    }

    private sealed class GrapplingHook : IGrapplingHookEffects
    {
        bool IGrapplingHookEffects.PlayHitSound(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            SoundEngine.PlaySound(SoundID.Item52 with { Pitch = 0.5f }, new Vector2(tileX * 16, tileY * 16));
            return false;
        }

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY) { }
    }

    private sealed class SquirrelHook : HookUnimplemented;

    private abstract class GemHook : IGrapplingHookEffects
    {
        bool IGrapplingHookEffects.PlayHitSound(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            SoundEngine.PlaySound(SoundID.Item52 with { Pitch = 0.5f }, new Vector2(tileX * 16, tileY * 16));
            return false;
        }

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY) { }
    }

    private sealed class AmethystHook : GemHook;

    private sealed class TopazHook : GemHook;

    private sealed class SapphireHook : GemHook;

    private sealed class EmeraldHook : GemHook;

    private sealed class RubyHook : GemHook;

    private sealed class AmberHook : GemHook;

    private sealed class DiamondHook : GemHook;

    private sealed class WebSlinger : HookUnimplemented;

    private sealed class SkeletronHand : HookUnimplemented;

    private sealed class SlimeHook : HookUnimplemented;

    private sealed class FishHook : HookUnimplemented;

    private sealed class IvyWhip : HookUnimplemented;

    private sealed class BatHook : HookUnimplemented;

    private sealed class CandyCaneHook : HookUnimplemented;

    private abstract class DualHook : IGrapplingHookEffects
    {
        bool IGrapplingHookEffects.PlayHitSound(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            SoundEngine.PlaySound(SoundID.Item52 with { Pitch = 0.5f }, new Vector2(tileX * 16, tileY * 16));
            return false;
        }

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY) { }
    }

    private sealed class DualHookBlue : DualHook;

    private sealed class DualHookRed : DualHook;

    private sealed class HookOfDissonance : HookUnimplemented;

    private sealed class ThornHook : HookUnimplemented;

    private sealed class IlluminantHook : HookUnimplemented;

    private sealed class WormHook : HookUnimplemented;

    private sealed class TendonHook : HookUnimplemented;

    private sealed class AntiGravityHook : HookUnimplemented;

    private sealed class SpookyHook : HookUnimplemented;

    private sealed class ChristmasHook : HookUnimplemented;

    private abstract class LunarHook : HookUnimplemented;

    private sealed class LunarHookSolar : LunarHook;

    private sealed class LunarHookVortex : LunarHook;

    private sealed class LunarHookNebula : LunarHook;

    private sealed class LunarHookStardust : LunarHook;

    [ModSystemHooks.PostSetupContent]
    private static void LoadVanillaHookEffects()
    {
        GrapplingHookEffects.CustomEffects[ProjectileID.Hook] = new GrapplingHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.SquirrelHook] = new SquirrelHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookAmethyst] = new AmethystHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookTopaz] = new TopazHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookSapphire] = new SapphireHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookEmerald] = new EmeraldHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookRuby] = new RubyHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.AmberHook] = new AmberHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.GemHookDiamond] = new DiamondHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.Web] = new WebSlinger();
        GrapplingHookEffects.CustomEffects[ProjectileID.SkeletronHand] = new SkeletronHand();
        GrapplingHookEffects.CustomEffects[ProjectileID.SlimeHook] = new SlimeHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.FishHook] = new FishHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.IvyWhip] = new IvyWhip();
        GrapplingHookEffects.CustomEffects[ProjectileID.BatHook] = new BatHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.CandyCaneHook] = new CandyCaneHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.DualHookBlue] = new DualHookBlue();
        GrapplingHookEffects.CustomEffects[ProjectileID.DualHookRed] = new DualHookRed();
        GrapplingHookEffects.CustomEffects[ProjectileID.QueenSlimeHook] = new HookOfDissonance();
        GrapplingHookEffects.CustomEffects[ProjectileID.ThornHook] = new ThornHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.IlluminantHook] = new IlluminantHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.WormHook] = new WormHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.TendonHook] = new TendonHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.AntiGravityHook] = new AntiGravityHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.WoodHook] = new SpookyHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.ChristmasHook] = new ChristmasHook();
        GrapplingHookEffects.CustomEffects[ProjectileID.LunarHookSolar] = new LunarHookSolar();
        GrapplingHookEffects.CustomEffects[ProjectileID.LunarHookVortex] = new LunarHookVortex();
        GrapplingHookEffects.CustomEffects[ProjectileID.LunarHookNebula] = new LunarHookNebula();
        GrapplingHookEffects.CustomEffects[ProjectileID.LunarHookStardust] = new LunarHookStardust();
    }
}
