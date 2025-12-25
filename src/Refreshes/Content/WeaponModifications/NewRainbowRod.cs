using System;
using Daybreak.Common.Rendering;
using Refreshes.Common.Particles;
using Refreshes.Common.Rendering;
using Refreshes.Core;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace Refreshes.Content;

public sealed class NewRainbowRod : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.RainbowRodBullet;
    }

    public override void AI(Projectile projectile)
    {
        base.AI(projectile);

        var glowColor = Main.hslToRgb(Math.Abs(1.5f - Main.GlobalTimeWrappedHourly) % 1f, 1f, 0.55f, 60);

        for (var i = 0; i < Main.rand.Next(3); i++)
        {
            var dustVel = projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f) + Main.rand.NextVector2Circular(3, 3);
            var light = Dust.NewDustPerfect(projectile.Center, DustID.RainbowMk2, dustVel, 0, glowColor, Main.rand.NextFloat(0.5f, 1f));
            light.noGravity = true;
        }

        if (Main.rand.NextBool())
        {
            var particleVel = projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f) + Main.rand.NextVector2Circular(2, 2);
            var particle = DustFlameParticle.RequestNew(projectile.Center, particleVel, glowColor, Color.White with { A = 0 }, Main.rand.NextFloat(1f, 1.75f), Main.rand.Next(15, 25));
            ParticleEngine.PARTICLES.Add(particle);
        }

        if (Main.rand.NextBool(5))
        {
            var sparkleColor = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.55f, 60) * 0.7f;

            var size = Main.rand.NextFloat(0.3f, 0.8f);
            var sparkle = ParticleOrchestrator._poolPrettySparkle.RequestParticle();
            sparkle.LocalPosition = projectile.Center;
            sparkle.Velocity = projectile.velocity * Main.rand.NextFloat(-0.2f, 0.3f) * size + Main.rand.NextVector2Circular(1, 1);
            sparkle.Rotation = MathHelper.PiOver2;
            sparkle.ColorTint = sparkleColor;
            sparkle.TimeToLive = Main.rand.Next(30, 60);
            sparkle.Scale = new Vector2(size);
            ParticleEngine.PARTICLES.Add(sparkle);
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        Main.spriteBatch.End(out var ss);
        Main.spriteBatch.Begin(ss with { SortMode = SpriteSortMode.Immediate });

        // Trail

        var stripEffect = Assets.Shaders.Weapons.RainbowRodTrail.CreateRainbowRodPass();
        stripEffect.Parameters.uTexture0 = Assets.Images.Sample.Worley.Asset.Value;
        stripEffect.Parameters.uWidth = 15f;
        stripEffect.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        stripEffect.Parameters.uImage0 = new HlslSampler
        {
            Texture = TextureAssets.Extra[ExtrasID.MagicMissileTrailShape].Value,
        };
        stripEffect.Apply();

        StripRenderer.DrawStripPadded(projectile.oldPos, projectile.oldRot, ColorFunction, WidthFunction, projectile.Size / 2 - Main.screenPosition);

        // Head
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();

        var texture = Assets.Images.Sample.Tweaks.RainbowRodFlame.Asset.Value;
        var animateFrame = (int)(Main.GlobalTimeWrappedHourly * 12 % 5);
        var frame = texture.Frame(2, 5, 0, animateFrame);
        var glowFrame = texture.Frame(2, 5, 1, animateFrame);
        var flameOrigin = frame.Size() * new Vector2(0.5f, 0.7f);

        var veloScale = Utils.GetLerpValue(2f, 7f, projectile.velocity.Length(), clamped: true);
        var flameRotation = projectile.rotation.AngleLerp(projectile.rotation - MathHelper.PiOver2, veloScale);

        var glowColor = Main.hslToRgb(Math.Abs(1.5f - Main.GlobalTimeWrappedHourly) % 1f, 0.9f, 0.6f, 100);

        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, glowColor, flameRotation, flameOrigin, projectile.scale * 1.1f, 0);
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, glowFrame, Color.White with { A = 0 }, flameRotation, flameOrigin, projectile.scale * 1.1f, 0);

        var sparkle = TextureAssets.Extra[ExtrasID.SharpTears].Value;
        var glowTexture = Assets.Images.Sample.GlowSmall.Asset.Value;

        glowColor.A = 0;
        Main.EntitySpriteDraw(glowTexture, projectile.Center - Main.screenPosition, glowTexture.Frame(), glowColor * 0.4f, flameRotation, glowTexture.Size() / 2, projectile.scale * 0.4f, 0);

        var sparkleScale = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.05f;
        Main.EntitySpriteDraw(sparkle, projectile.Center - Main.screenPosition, sparkle.Frame(), glowColor * 0.5f, MathHelper.PiOver2, sparkle.Size() / 2, projectile.scale * new Vector2(0.5f, 3f * sparkleScale), 0);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(ss);

        return false;

        static Color ColorFunction(float progress)
        {
            return Main.hslToRgb(Math.Abs(progress * 1.6f - Main.GlobalTimeWrappedHourly + 0.5f) % 1f, 1f, 0.6f, 150);
        }

        float WidthFunction(float progress)
        {
            var i = (int)Math.Clamp(Math.Floor(progress * ProjectileID.Sets.TrailCacheLength[projectile.type]), 0, ProjectileID.Sets.TrailCacheLength[projectile.type] - 1);

            var width = Utils.GetLerpValue(0f, 30f, projectile.oldPos[i].Distance(projectile.position) + projectile.velocity.LengthSquared(), true);
            return (15f + MathF.Sin(progress * 17f - Main.GlobalTimeWrappedHourly * 20f) * 3f) * Utils.GetLerpValue(-0.07f, 0.03f, progress, true) * (1f - MathF.Cbrt(progress) * 0.3f) * width;
        }
    }
}
