using System;
using Daybreak.Common.Rendering;
using Refreshes.Common.Particles;
using Refreshes.Common.Rendering;
using Refreshes.Core;
using Terraria.GameContent;
using Terraria.ID;

namespace Refreshes.Content;

public sealed class NewFlamelash : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.Flamelash;
    }

    public override void AI(Projectile projectile)
    {
        base.AI(projectile);

        if (projectile.velocity.Length() > 2f)
        {
            var particleVel = -projectile.velocity * 0.2f + Main.rand.NextVector2Circular(5, 5);
            var lightColor = Color.Lerp(Color.Gold, Color.PaleGoldenrod, Main.rand.NextBool(4).ToInt()) with { A = 50 };
            var particle = DustFlameParticle.RequestNew(projectile.Center + projectile.velocity * 2f, particleVel, new Color(255, 15, 35, 80), lightColor, Main.rand.NextFloat(0.5f, 1.5f), Main.rand.Next(24, 35));
            particle.LossPerFrame = 0.05f;
            ParticleEngine.PARTICLES.Add(particle);
        }

        if (Main.rand.NextBool(6) && (projectile.velocity.Length() < 2f || Main.rand.NextBool()))
        {
            var particleVel = projectile.velocity * 0.5f - Vector2.UnitY * Main.rand.NextFloat();
            var lightColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat()) with { A = 50 };
            var particle = DustFlameParticle.RequestNew(projectile.Center, particleVel, new Color(255, 15, 35, 40), lightColor, Main.rand.NextFloat(1f, 1.5f), Main.rand.Next(20, 25));
            particle.LossPerFrame = 0.3f;
            particle.Swirly = true;
            ParticleEngine.PARTICLES.Add(particle);
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        Main.spriteBatch.End(out var ss);
        Main.spriteBatch.Begin(ss with { SortMode = SpriteSortMode.Immediate });

        // Head Glow

        var texture = TextureAssets.Projectile[projectile.type].Value;
        var frame = texture.Frame(1, 6, 0, projectile.frame);
        var origin = frame.Size() * new Vector2(0.5f, 0.6f);
        var veloScale = Utils.GetLerpValue(1f, 7f, projectile.velocity.Length(), clamped: true);
        var flameRotation = projectile.rotation.AngleLerp(projectile.rotation - MathHelper.PiOver2, veloScale);

        var glowTexture = Assets.Images.Sample.GlowSmall.Asset.Value;
        var glowPower = projectile.penetrate < 2 ? 0.5f : 0.35f;
        Main.EntitySpriteDraw(glowTexture, projectile.Center - Main.screenPosition, glowTexture.Frame(), Color.Red with { A = 50 }, flameRotation, glowTexture.Size() / 2, projectile.scale * glowPower, 0);

        // Trail

        var stripEffect = Assets.Shaders.Weapons.FlamelashTrail.CreateFlamelashPass();
        stripEffect.Parameters.uTime = Main.GlobalTimeWrappedHourly / 13 % 1f;
        stripEffect.Parameters.uGlowColor = new Vector4(0.8f, 0.5f, 0.1f, 0.3f);
        stripEffect.Parameters.uTexture0 = Assets.Images.Sample.FlameNoise.Asset.Value;
        stripEffect.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        stripEffect.Parameters.uWidth = projectile.penetrate < 2 ? 18f : 14f;
        stripEffect.Parameters.uImage0 = new HlslSampler
        {
            Texture = Assets.Images.Sample.Tweaks.FlamelashTrail.Asset.Value,
        };
        stripEffect.Apply();

        StripRenderer.DrawStripPadded(projectile.oldPos, projectile.oldRot, ColorFunction, WidthFunction, projectile.Size / 2 - Main.screenPosition);

        // Head

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, Color.White with { A = 100 }, flameRotation, origin, projectile.scale, 0);

        if (projectile.penetrate < 2)
        {
            for (var i = 0; i < 4; i++)
            {
                var outerFrame = texture.Frame(1, 6, 0, (projectile.frame + i) % 6);

                var offset = new Vector2(4f, 0).RotatedBy(i / 4f * MathHelper.TwoPi);
                Main.EntitySpriteDraw(texture, projectile.Center + offset - Main.screenPosition, outerFrame, Color.LightGoldenrodYellow with { A = 0 } * 0.5f, flameRotation, origin, projectile.scale, 0);
            }
        }

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(ss);

        return false;

        static Color ColorFunction(float progress)
        {
            return new Color(150, 2, 10, (byte)(80 - 70 * progress));
        }

        float WidthFunction(float progress)
        {
            var i = (int)Math.Floor(progress * ProjectileID.Sets.TrailCacheLength[projectile.type]);
            var width = Utils.GetLerpValue(0f, 30f, projectile.oldPos[i].Distance(projectile.position) + projectile.velocity.LengthSquared(), true);
            var normalWidth = projectile.penetrate < 2 ? 25f : 20f;
            return (normalWidth + MathF.Sin(progress * 17f - Main.GlobalTimeWrappedHourly * 20f) * 5f) * (1f - progress * 0.75f) * width * Utils.GetLerpValue(-0.05f, 0.05f, progress, true);
        }
    }
}
