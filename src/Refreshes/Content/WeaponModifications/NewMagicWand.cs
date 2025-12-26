using System;
using Daybreak.Common.Rendering;
using Refreshes.Common.Particles;
using Refreshes.Common.Rendering;
using Refreshes.Core;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace Refreshes.Content;

public sealed class NewMagicWand : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.MagicMissile;
    }

    public override void AI(Projectile projectile)
    {
        base.AI(projectile);

        if (Main.rand.NextBool() && projectile.velocity.Length() > 2f)
        {
            var dust = Dust.NewDustPerfect(projectile.Center, DustID.MagicMirror, projectile.velocity * 0.2f + Main.rand.NextVector2Circular(2, 2), Alpha: 255, Scale: Main.rand.NextFloat(0.2f, 1.5f));
            dust.fadeIn = 0.75f;
            dust.noGravity = true;
        }

        if (Main.rand.NextBool(9))
        {
            var sparkle = ParticleOrchestrator._poolPrettySparkle.RequestParticle();
            sparkle.SetBasicInfo(TextureAssets.Extra[ExtrasID.SharpTears], null, projectile.velocity * 0.1f + Main.rand.NextVector2Circular(2, 2), projectile.Center);
            sparkle.ColorTint = Color.Lerp(Color.Cyan, Color.BlueViolet, Main.rand.NextFloat()) with { A = 0 } * 0.5f;
            sparkle.Scale = new Vector2(1f, 0.5f);
            sparkle.TimeToLive = Main.rand.Next(30, 50);

            ParticleEngine.PARTICLES.Add(sparkle);
        }

        // Specifically to disable the godawful Magic Missile casting sound that
        // plays 6 times a second!?
        projectile.soundDelay = 10;
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        Main.spriteBatch.End(out var ss);
        Main.spriteBatch.Begin(ss with { SortMode = SpriteSortMode.Immediate });

        // Head

        var texture = Assets.Images.Sample.Tweaks.MagicMissileStone.Asset.Value;
        var glowTexture = Assets.Images.Sample.GlowSmall.Asset.Value;
        var frame = texture.Frame(1, 2);
        var glowFrame = texture.Frame(1, 2);

        var veloScale = Utils.GetLerpValue(1f, 7f, projectile.velocity.Length(), clamped: true);
        var stoneRotation = projectile.rotation.AngleLerp(projectile.rotation + MathHelper.PiOver2, veloScale);

        Main.EntitySpriteDraw(glowTexture, projectile.Center - Main.screenPosition, glowTexture.Frame(), Color.BlueViolet with { A = 50 }, stoneRotation, glowTexture.Size() / 2, projectile.scale * 0.4f, 0);
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, Color.White with { A = 150 }, stoneRotation, frame.Size() / 2, projectile.scale, 0);
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, glowFrame, Color.Gray with { A = 0 }, stoneRotation, glowFrame.Size() / 2, projectile.scale, 0);

        // Trail

        var stripEffect = Assets.Shaders.Weapons.MagicMissileTrail.CreateMagicMissilePass();
        stripEffect.Parameters.uTime = Main.GlobalTimeWrappedHourly / 13 % 1f;
        stripEffect.Parameters.uWidth = 12f;
        stripEffect.Parameters.uGlowColor = new Vector4(0.7f, 0.8f, 1.2f, 0.2f);
        stripEffect.Parameters.uTexture0 = Assets.Images.Sample.FlameNoise.Asset.Value;
        stripEffect.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        stripEffect.Parameters.uImage0 = new HlslSampler
        {
            Texture = Assets.Images.Sample.Tweaks.FlamelashTrail.Asset.Value,
        };
        stripEffect.Apply();

        StripRenderer.DrawStripPadded(projectile.oldPos, projectile.oldRot, ColorFunction, WidthFunction, projectile.Size / 2 - Main.screenPosition);

        // Head Flare

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();

        var flare = TextureAssets.Extra[ExtrasID.SharpTears].Value;
        var flareScaleX = new Vector2(0.7f, 2f + MathF.Sin(Main.GlobalTimeWrappedHourly * 32) * 0.1f);
        Main.EntitySpriteDraw(flare, projectile.Center - Main.screenPosition, flare.Frame(), new Color(20, 50, 100, 0), MathHelper.PiOver2, flare.Size() / 2, projectile.scale * flareScaleX, 0);
        Main.EntitySpriteDraw(flare, projectile.Center - Main.screenPosition, flare.Frame(), new Color(150, 200, 255, 0), MathHelper.PiOver2, flare.Size() / 2, projectile.scale * flareScaleX * 0.5f, 0);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(ss);

        return false;

        static Color ColorFunction(float progress)
        {
            var earlyP = Math.Clamp(progress * 1.4f, 0, 1);

            return new Color((byte)(150 * earlyP) + 10, 160 - (byte)(50 * earlyP), 255, 80);
        }

        float WidthFunction(float progress)
        {
            var i = (int)Math.Floor(progress * ProjectileID.Sets.TrailCacheLength[projectile.type]);
            var width = Utils.GetLerpValue(0f, 30f, projectile.oldPos[i].Distance(projectile.position) + projectile.velocity.LengthSquared(), true);

            return 12f * (1f - progress * 0.5f) * width * Utils.GetLerpValue(-0.05f, 0.1f, progress, true);
        }
    }
}
