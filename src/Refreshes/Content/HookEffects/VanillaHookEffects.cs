using System;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Features.Models;
using Daybreak.Common.Rendering;
using Refreshes.Common;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using Terraria.ID;

namespace Refreshes.Content.HookEffects;

[Autoload(Side = ModSide.Client)]
internal static class VanillaHookEffects
{
    [Autoload(Side = ModSide.Client)]
    private sealed class Data : IStatic<Data>
    {
        public required RenderTargetLease SparkLease { get; init; }

        public static Data LoadData(Mod mod)
        {
            return Main.RunOnMainThread(
                () => new Data
                {
                    SparkLease = ScreenspaceTargetPool.Shared.Rent(
                        Main.instance.GraphicsDevice,
                        (w, h) => (w / 2, h / 2)
                    ),
                }
            ).GetAwaiter().GetResult();
        }

        public static void UnloadData(Data data)
        {
            Main.RunOnMainThread(
                () =>
                {
                    data.SparkLease.Dispose();
                }
            );
        }
    }

    public class PixelSparkParticle : BaseParticle<PixelSparkParticle>
    {
        public float Scale { get; set; }

        private bool collide;
        private Color colorGlow;
        private Color colorTint;
        private Vector2 endPosition;
        private Vector2 gravity;
        private float mass;
        private Vector2 position;
        private Vector2 velocity;

        public static PixelSparkParticle RequestNew(
            Vector2 position,
            Vector2 velocity,
            Vector2 gravity,
            Color color,
            Color glowColor,
            float mass,
            bool collide = true
        )
        {
            var spark = Pool.RequestParticle();
            spark.position = position;
            spark.endPosition = position;
            spark.velocity = velocity;
            spark.gravity = gravity;
            spark.colorTint = color;
            spark.colorGlow = glowColor;
            spark.mass = mass;
            spark.collide = collide;
            spark.Scale = Main.rand.NextFloat(0.9f, 1.1f);
            return spark;
        }

        public override void FetchFromPool()
        {
            base.FetchFromPool();
            velocity = Vector2.Zero;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            position += velocity;
            velocity += gravity * (0.5f - Scale * 0.5f);
            if (velocity.Length() > 24f)
            {
                velocity = Vector2.Lerp(velocity, velocity.SafeNormalize(Vector2.Zero) * 24f, 0.4f);
            }

            var lerpFactor = 0.2f;
            if (collide)
            {
                var bounce = Collision.TileCollision(position - new Vector2(2, 3), velocity, 4, 2);
                if (Math.Abs(bounce.X - velocity.X) > 0)
                {
                    velocity.X *= -0.3f;
                    lerpFactor = 0.5f;
                }

                if (Math.Abs(bounce.Y - velocity.Y) > 0)
                {
                    Scale *= 0.95f;
                    velocity.Y *= -Main.rand.NextFloat(0.6f, 0.9f);
                    velocity.X *= 0.6f;
                    lerpFactor = 0.5f;
                }

                if (Collision.SolidCollision(position - Vector2.One, 2, 2))
                {
                    Scale *= 0.8f;
                }
            }

            if (Scale < 0.1f)
            {
                Scale *= 0.9f + Math.Clamp(mass, 0f, 1f) * 0.01f;
                lerpFactor = 0.5f;

                if (Scale < 0.01f)
                {
                    ShouldBeRemovedFromRenderer = true;
                }
            }
            else
            {
                Scale *= 0.4f + Math.Clamp(mass * 0.5f, 0f, 0.5f);
            }

            endPosition = Vector2.Lerp(endPosition, position - velocity, lerpFactor);
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            var texture = Assets.Images.Particles.PixelSparkParticle.Asset.Value;
            var glow = Assets.Images.Particles.GlowSmall.Asset.Value;

            var rotation = MathF.Round(position.AngleFrom(endPosition) / MathHelper.TwoPi * 8f) / 8f * MathHelper.TwoPi;
            var fadeOut = Utils.GetLerpValue(0f, 0.05f, Scale, true);

            var particlePos = new Vector2((int)Math.Round(position.X / 2) * 2, (int)Math.Round(position.Y / 2) * 2) + settings.AnchorPosition;
            // var unRotatedVel = velocity.RotatedBy(-rotation);
            var stretch = new Vector2(1.1f + position.Distance(endPosition) * 0.15f, 1f) * (1f + mass * 0.01f);

            spritebatch.Draw(texture, particlePos, texture.Frame(), colorTint * fadeOut, rotation, texture.Size() * 0.5f, stretch, 0, 0);
            spritebatch.Draw(glow, particlePos, glow.Frame(), colorGlow with { A = 0 } * fadeOut, rotation, glow.Size() * 0.5f, new Vector2(0.06f, 0.12f) * stretch, 0, 0);
        }
    }

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

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            for (var i = 0; i < 2; i++)
            {
                var velocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3, 7f) * -1f;
                var particle = PixelSparkParticle.RequestNew(projectile.Center, velocity, Vector2.UnitY * 1, Color.Yellow, Color.Red * 0.33f, 0.01f);
                particle.Scale *= 0.033f;
                particles.Add(particle);
            }
        }
    }

    private sealed class SquirrelHook : HookUnimplemented;

    private abstract class GemHook : IGrapplingHookEffects
    {
        bool IGrapplingHookEffects.PlayHitSound(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            SoundEngine.PlaySound(SoundID.Item52 with { Pitch = 0.5f }, new Vector2(tileX * 16, tileY * 16));
            return false;
        }

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            for (var i = 0; i < 2; i++)
            {
                var (color, glowColor) = GetSparkParticleColor();
                var velocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3, 7f) * -1f;
                var particle = PixelSparkParticle.RequestNew(projectile.Center, velocity, Vector2.UnitY * 1, color, glowColor * 0.4f, 0.01f);
                particle.Scale *= 0.033f;
                particles.Add(particle);
            }
        }

        protected abstract (Color Color, Color GlowColor) GetSparkParticleColor();
    }

    private sealed class AmethystHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(219, 97, 255), new Color(165, 0, 236));
        }
    }

    private sealed class TopazHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(255, 221, 62), new Color(255, 198, 0));
        }
    }

    private sealed class SapphireHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(13, 107, 216), new Color(25, 33, 191));
        }
    }

    private sealed class EmeraldHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(81, 207, 160), new Color(33, 184, 115));
        }
    }

    private sealed class RubyHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(195, 41, 44), new Color(155, 21, 18));
        }
    }

    private sealed class AmberHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(252, 193, 45), new Color(191, 82, 0));
        }
    }

    private sealed class DiamondHook : GemHook
    {
        protected override (Color Color, Color GlowColor) GetSparkParticleColor()
        {
            return (new Color(155, 200, 202), new Color(218, 185, 210));
        }
    }

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

        void IGrapplingHookEffects.HitTile(Projectile projectile, Tile tile, int tileX, int tileY)
        {
            for (var i = 0; i < 2; i++)
            {
                var velocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3, 7f) * -1f;
                var particle = PixelSparkParticle.RequestNew(projectile.Center, velocity, Vector2.UnitY * 1, Color.Yellow, Color.Red * 0.33f, 0.01f);
                particle.Scale *= 0.033f;
                particles.Add(particle);
            }

            if (Main.rand.NextBool())
            {
                for (var i = 0; i < Main.rand.Next(1) + 1; i++)
                {
                    var (color, glowColor) = GetSpecialColor();
                    var velocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3, 7f) * -1f;
                    var particle = PixelSparkParticle.RequestNew(projectile.Center, velocity, Vector2.UnitY * 1, color, glowColor * 0.6f, 0.01f);
                    particle.Scale *= 0.05f;
                    particles.Add(particle);
                }
            }
        }

        protected abstract (Color Color, Color GlowColor) GetSpecialColor();
    }

    private sealed class DualHookBlue : DualHook
    {
        protected override (Color Color, Color GlowColor) GetSpecialColor()
        {
            return (new Color(13, 107, 216), new Color(25, 33, 191));
        }
    }

    private sealed class DualHookRed : DualHook
    {
        protected override (Color Color, Color GlowColor) GetSpecialColor()
        {
            return (new Color(195, 41, 44), new Color(155, 21, 18));
        }
    }

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

    private static readonly ParticleRenderer particles = new();

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

    [ModSystemHooks.PostUpdateEverything]
    private static void UpdateParticles()
    {
        particles.Update();
    }

    [OnLoad]
    private static void ApplyHooks()
    {
        On_Main.DrawDust += DrawSparkParticles;
    }

    private static void DrawSparkParticles(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);

        var sparkLease = IStatic<Data>.Instance.SparkLease;

        var sb = Main.spriteBatch;

        sb.Begin();
        sb.End(out var ss);

        using (sparkLease.Scope(clearColor: Color.Transparent))
        {
            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f) });
            particles.Settings.AnchorPosition = -Main.screenPosition;
            particles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        using (sb.Scope())
        {
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });

            Main.spriteBatch.Draw(
                new DrawParameters(sparkLease.Target)
                {
                    Position = Vector2.Zero,
                    Scale = new Vector2(2f),
                    Color = Color.White,
                }
            );
        }
    }
}
