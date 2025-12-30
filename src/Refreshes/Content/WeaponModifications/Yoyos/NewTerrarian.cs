using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Features.Models;
using Daybreak.Common.Mathematics;
using Daybreak.Common.Rendering;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Refreshes.Common.Particles;
using Refreshes.Common.Rendering;
using Refreshes.Content.Dusts;
using Refreshes.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Terraria.ModLoader.BackupIO;

namespace Refreshes.Content.Yoyos;

internal sealed class NewTerrarian : GlobalProjectile
{
    private sealed class TerrarianGasParticle : BaseParticle<TerrarianGasParticle>
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }

        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }

        public Vector2 Scale { get; set; }

        public int LifeTime { get; private set; }
        public int MaxLifeTime { get; set; }
        public float LifeTimeProgress => (float)LifeTime / MaxLifeTime;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            LifeTime = 0;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;

            Rotation += RotationSpeed;

            LifeTime++;
            if (LifeTime >= MaxLifeTime)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            const float start_size = 0.5f;
            const float final_size = 0f;
            const float start_alpha = 0f;
            const float final_alpha = 0f;

            var texture = Assets.Images.Particles.Circle.Asset.Value;
            var origin = texture.Size() / 2;

            var scaleMult = Bezier.Cubic(start_size, 0.5f, 0f, final_size, LifeTimeProgress);
            var alphaMult = Bezier.Cubic(start_alpha, 1f, 0.5f, final_alpha, LifeTimeProgress);

            spriteBatch.Draw(
                new DrawParameters(texture)
                {
                    Position = Position + settings.AnchorPosition,
                    Rotation = Angle.FromRadians(Rotation),
                    Color = Color.White * alphaMult,
                    Origin = origin,
                    Scale = Scale * scaleMult,
                }
            );
        }
    }

    private static RenderTargetLease? terrarian_gas_lease;
    private static RenderTargetLease? terrarian_gas_shader_lease;
    private static readonly ParticleRenderer terrarian_gas_particle_renderer = new();

    [ModSystemHooks.PostUpdateEverything]
    private static void UpdateParticles()
    {
        terrarian_gas_particle_renderer.Update();
    }

    public override void Load()
    {
        Main.RunOnMainThread(() =>
            {
                terrarian_gas_lease = ScreenspaceTargetPool.Shared.Rent(
                    Main.instance.GraphicsDevice,
                    (w, h) => (w / 2, h / 2)
                );
                terrarian_gas_shader_lease = ScreenspaceTargetPool.Shared.Rent(
                    Main.instance.GraphicsDevice,
                    (w, h) => (w / 2, h / 2)
                );
            }
        );

        On_Main.DrawProjectiles += DrawTerrarianGas;
    }

    public override void Unload()
    {
        Main.RunOnMainThread(() =>
        {
            terrarian_gas_lease?.Dispose();
            terrarian_gas_shader_lease?.Dispose();
        }
    );

        On_Main.DrawProjectiles -= DrawTerrarianGas;
    }

    private static void DrawTerrarianGas(On_Main.orig_DrawProjectiles orig, Main self)
    {
        if (terrarian_gas_lease == null || terrarian_gas_shader_lease == null)
        {
            orig(self);
            return;
        }

        Main.spriteBatch.Begin();
        Main.spriteBatch.End(out var ss);

        using (terrarian_gas_lease.Scope(clearColor: Color.Transparent))
        {
            var gasShader = Assets.Shaders.Weapons.TerrarianGas.Asset.Value;
            gasShader.Parameters["uTexture0"].SetValue(Assets.Images.Sample.PlantNoise.Asset.Value);
            gasShader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            gasShader.Parameters["uMultColor"].SetValue(new Vector4(0f, 0.2f, 0f, 1f));
            gasShader.Parameters["uAddColor"].SetValue(new Vector4(0f, 0f, 0.2f, 1f));

            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f) });
            terrarian_gas_particle_renderer.Settings.AnchorPosition = -Main.screenPosition;
            terrarian_gas_particle_renderer.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        using (terrarian_gas_shader_lease.Scope(clearColor: Color.Transparent))
        {
            var gasShader = Assets.Shaders.Weapons.TerrarianGas.Asset.Value;
            gasShader.Parameters["uTexture0"].SetValue(Assets.Images.Sample.LavaNoise_1.Asset.Value);
            gasShader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
            gasShader.Parameters["uMultColor"].SetValue(new Vector4(0f, 0.5f, 0f, 1f));
            gasShader.Parameters["uAddColor"].SetValue(new Vector4(0f, 0f, 0.3f, 0f));
            gasShader.Parameters["uColorAmount"].SetValue(4f);

            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, CustomEffect = gasShader });

            Main.spriteBatch.Draw(
                new DrawParameters(terrarian_gas_lease.Target)
                {
                    Position = Vector2.Zero,
                    Scale = Vector2.One,
                    Color = Color.White,
                }
            );

            Main.spriteBatch.End();
        }

        using (Main.spriteBatch.Scope())
        {
            var outlineShader = Assets.Shaders.Outline.Asset.Value;
            outlineShader.Parameters["uTexelSize"].SetValue(new Vector2(1f / terrarian_gas_shader_lease.Target.Width, 1f / terrarian_gas_shader_lease.Target.Height));
            outlineShader.Parameters["uOutlineColor"].SetValue(new Color(0.4f, 1f, 0.6f).ToVector4());

            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer, CustomEffect = outlineShader });

            Main.spriteBatch.Draw(
                new DrawParameters(terrarian_gas_shader_lease.Target)
                {
                    Position = Vector2.Zero,
                    Scale = Vector2.One * 2,
                    Color = Color.White,
                }
            );
        }

        orig(self);
    }

    private static readonly Color trail_color_start = new Color(0.4f, 1f, 0.6f) * 0.5f;
    private static readonly Color trail_color_end = new Color(0.4f, 0.6f, 1f) * 0.5f;

    private float TrailWidth(float p) => float.Lerp(6f, 12f, p);

    private const float trail_offset_amplitude = 10f;
    private const float trail_offset_phase = 1f;

    private const float trail_split_width_start = 0.65f;
    private const float trail_split_width_end = 0.8f;
    private const float trail_split_start = 50f;
    private const float trail_split_length = 100f;
    private const float trail_split_width_progress_start = 150f;
    private const float trail_split_width_progress_length = 200;

    private const float trail_fadeoff_length = 120f;

    private static readonly Color flare_color = new Color(0.4f, 1f, 0.6f);
    private static readonly Color flare_color_shine = new Color(1f, 1f, 1f, 0f);

    private const float flare_phase = 0.075f;

    [AllowNull]
    private Vector2[] previousPositions;
    [AllowNull]
    private Vector2[] previousDirections;
    [AllowNull]
    private float[] previousOffsetPhases;
    [AllowNull]
    private Vector2[] previousOffsetPositions;
    [AllowNull]
    private float[] previousRotations;
    private float trailCurrentPhase;
    private float totalDistance;

    private float flareCounter;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.Terrarian;
    }

    private void InitializeArrays(Projectile projectile)
    {
        if (previousPositions == null || previousDirections == null || previousOffsetPhases == null || previousOffsetPositions == null || previousRotations == null)
        {
            previousPositions = new Vector2[20];
            previousDirections = new Vector2[20];
            previousOffsetPhases = new float[20];
            previousOffsetPositions = new Vector2[20];
            previousRotations = new float[20];

            for (var i = 0; i < previousPositions.Length; i++)
            {
                previousPositions[i] = projectile.position + projectile.velocity;
                previousDirections[i] = projectile.velocity.SafeNormalize(Vector2.Zero);
                previousOffsetPhases[i] = 0;
                previousOffsetPositions[i] = previousPositions[i];
                previousRotations[i] = previousDirections[i].ToRotation();
            }
        }
    }

    public override void PostAI(Projectile projectile)
    {
        InitializeArrays(projectile);

        #region Trail position-related code
        for (var i = previousPositions.Length - 1; i > 0; i--)
        {
            previousPositions[i] = previousPositions[i - 1];
            previousDirections[i] = previousDirections[i - 1];
            previousOffsetPhases[i] = previousOffsetPhases[i - 1];
            previousRotations[i] = previousRotations[i - 1];
        }

        trailCurrentPhase += trail_offset_phase;
        previousPositions[0] = projectile.position + projectile.velocity;
        previousDirections[0] = projectile.velocity.SafeNormalize(Vector2.Zero);
        previousOffsetPhases[0] = trailCurrentPhase;
        previousRotations[0] = previousDirections[0].ToRotation();

        for (var i = 0; i < previousOffsetPositions.Length; i++)
        {
            var progress = (float)i / previousOffsetPositions.Length;
            var offset = previousDirections[i].RotatedBy(MathF.PI / 2) * MathF.Sin(previousOffsetPhases[i]) * trail_offset_amplitude * progress;
            previousOffsetPositions[i] = previousPositions[i] + offset;
        }

        totalDistance = 0;
        for (var i = 0; i < previousPositions.Length - 2; i++)
        {
            totalDistance += (previousPositions[i + 1] - previousPositions[i]).Length();
        }
        #endregion

        #region Dust spawning
        if (Main.rand.NextBool(3))
        {
            var dust = Dust.NewDustPerfect
            (
                Main.rand.NextFromList(previousPositions) + projectile.Size * 0.5f + Main.rand.NextVector2Circular(4f, 4f),
                ModContent.DustType<LightDotRGB>(),
                Main.rand.NextVector2Circular(4f, 4f),
                Scale: 1.5f,
                newColor: Color.Lerp(Color.White, Color.SpringGreen, Main.rand.NextFloat())
            );
            dust.fadeIn = Main.rand.NextFloat(0.1f, 0.3f);
            dust.noGravity = true;
        }
        if (Main.rand.NextBool(5))
        {
            var headDust = Dust.NewDustPerfect
            (
                projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                ModContent.DustType<LightDotRGB>(),
                projectile.velocity * 0.5f + Main.rand.NextVector2Circular(2f, 2f),
                Scale: 1f,
                newColor: Color.Lerp(Color.White, Color.SpringGreen, Main.rand.NextFloat())
            );
            headDust.fadeIn = Main.rand.NextFloat(0.05f, 0.15f);
            headDust.noGravity = true;
        }
        #endregion

        flareCounter += flare_phase;
        flareCounter %= 1f;

        for (int i = 0; i < 2; i++)
        {
            var particle = TerrarianGasParticle.Pool.RequestParticle();

            particle.Position = projectile.Center;
            particle.Velocity = -projectile.velocity * 0.1f + Main.rand.NextVector2Circular(1f, 1f);

            particle.Rotation = Main.rand.NextFloat(MathF.PI * 2f);
            particle.RotationSpeed = Main.rand.NextFloat(-0.05f, 0.05f);

            particle.Scale = new Vector2(Main.rand.NextFloat(2f, 3f));

            particle.MaxLifeTime = Main.rand.Next(5, 15);

            terrarian_gas_particle_renderer.Add(particle);
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        InitializeArrays(projectile);

        var yoyoTexture = TextureAssets.Projectile[ProjectileID.Terrarian].Value;

        var textureCenter = yoyoTexture.Size() * 0.5f;

        /*
        var trailTexture = TextureAssets.MagicPixel.Value;

        var trailShader = Assets.Shaders.SplittingTrail.CreateBasicTrailPass();
        trailShader.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        trailShader.Parameters.uImage0 = new HlslSampler { Texture = trailTexture, Sampler = SamplerState.PointClamp };
        trailShader.Parameters.uSplitProgressStart = trail_split_start / totalDistance;
        trailShader.Parameters.uSplitProgressEnd = (trail_split_start + trail_split_length) / totalDistance;
        trailShader.Parameters.uSplitWidthProgressStart = trail_split_width_progress_start / totalDistance;
        trailShader.Parameters.uSplitWidthProgressStart = (trail_split_width_progress_start + trail_split_width_progress_length) / totalDistance;
        trailShader.Parameters.uSplitWidthStart = trail_split_width_start;
        trailShader.Parameters.uSplitWidthEnd = trail_split_width_end;
        trailShader.Apply();

        var trailFadeoffLengthNormalized = (totalDistance - trail_fadeoff_length) / totalDistance;
        trailFadeoffLengthNormalized = MathF.Max(trailFadeoffLengthNormalized, 0f);

        Color StripColorFunction(float p)
        {
            var trailFadeoffProgress = Utils.GetLerpValue(trailFadeoffLengthNormalized, 1f, p, true);
            return Color.Lerp(Color.Lerp(trail_color_start, trail_color_end, trailFadeoffProgress), Color.Transparent, trailFadeoffProgress);
        }

        StripRenderer.DrawStripPadded(previousOffsetPositions, previousRotations, StripColorFunction, TrailWidth, projectile.Size * 0.5f - Main.screenPosition, false);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        */
        var spriteDirection = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        Main.spriteBatch.Draw(yoyoTexture, projectile.Center - Main.screenPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, textureCenter, projectile.scale, spriteDirection, 0f);

        var flareScaleProgress = Utils.PingPongFrom01To010(flareCounter);
        var flareScale = float.Lerp(0.5f, 1f, flareScaleProgress);
        Main.DrawPrettyStarSparkle(projectile.Opacity, SpriteEffects.None, projectile.Center - Main.screenPosition, flare_color_shine * 0.5f, flare_color * 0.5f, 1f, 0f, 1f, 2f, 2f, 0f, new Vector2(flareScale) * projectile.scale, Vector2.One * projectile.scale * 1.5f);
        var diagonalFlareScaleProgress = Utils.PingPongFrom01To010((flareCounter + 0.5f) % 1f);
        var diagonalFlareScale = float.Lerp(0.5f, 1f, diagonalFlareScaleProgress);
        Main.DrawPrettyStarSparkle(projectile.Opacity, SpriteEffects.None, projectile.Center - Main.screenPosition, flare_color_shine * 0.5f, flare_color * 0.5f, 1f, 0f, 1f, 2f, 2f, MathF.PI * 0.25f, new Vector2(diagonalFlareScale) * projectile.scale, Vector2.One * projectile.scale);

        return false;
    }
}

internal sealed class NewTerrarianBeam : GlobalProjectile
{
    private static readonly Color trail_color_start = new Color(1f, 1f, 1f) * 0.5f;
    private Color TrailColorEnd() => MainColor() * 0.5f;

    private Color MainColor() => Main.hslToRgb(0.3f + Hue, 1f, 0.5f);

    private float TrailWidth(float p) => float.Lerp(8f, 6f, p);

    public float Hue { get; private set; }

    [AllowNull]
    private Vector2[] previousPositions;
    [AllowNull]
    private float[] previousRotations;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.TerrarianBeam;
    }

    public override void SetDefaults(Projectile entity)
    {
        Hue = Main.rand.NextFloat(0f, 0.2f);
    }

    private void InitializeArrays(Projectile projectile)
    {
        if (previousPositions == null || previousRotations == null)
        {
            previousPositions = new Vector2[20];
            previousRotations = new float[20];

            for (var i = 0; i < previousPositions.Length; i++)
            {
                previousPositions[i] = projectile.position + projectile.velocity;
                previousRotations[i] = projectile.velocity.ToRotation();
            }
        }
    }

    public override void PostAI(Projectile projectile)
    {
        InitializeArrays(projectile);

        for (var i = previousPositions.Length - 1; i > 0; i--)
        {
            previousPositions[i] = previousPositions[i - 1];
            previousRotations[i] = previousRotations[i - 1];
        }
        previousPositions[0] = projectile.position + projectile.velocity;
        previousRotations[0] = projectile.velocity.ToRotation();

        if (Main.rand.NextBool(5))
        {
            var headDust = Dust.NewDustPerfect
            (
                projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                ModContent.DustType<LightDotRGB>(),
                projectile.velocity * 0.5f + Main.rand.NextVector2Circular(2f, 2f),
                Scale: 1f * projectile.Opacity,
                Alpha: projectile.alpha,
                newColor: Color.Lerp(Color.White, MainColor(), Main.rand.NextFloat())
            );
            headDust.fadeIn = Main.rand.NextFloat(0.05f, 0.15f);
            headDust.noGravity = true;
        }
    }

    public override Color? GetAlpha(Projectile projectile, Color lightColor)
    {
        return Color.White * projectile.Opacity;
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        InitializeArrays(projectile);

        var yoyoTexture = Assets.Images.Extras.TerrarianBeam.Asset.Value;

        var textureCenter = yoyoTexture.Size() * 0.5f;
        var positionOffset = textureCenter + new Vector2(0, projectile.gfxOffY);

        var trailTexture = TextureAssets.MagicPixel.Value;

        var trailShader = Assets.Shaders.SplittingTrail.CreateBasicTrailPass();
        trailShader.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        trailShader.Parameters.uImage0 = new HlslSampler { Texture = trailTexture, Sampler = SamplerState.PointClamp };
        trailShader.Parameters.uSplitProgressStart = 0f;
        trailShader.Parameters.uSplitProgressEnd = 0.5f;
        trailShader.Parameters.uSplitWidthProgressStart = 0.5f;
        trailShader.Parameters.uSplitWidthProgressEnd = 1f;
        trailShader.Parameters.uSplitWidthStart = 0.5f;
        trailShader.Parameters.uSplitWidthEnd = 1f;
        trailShader.Apply();

        Color StripColorFunction(float p)
        {
            return Color.Lerp(Color.Lerp(trail_color_start, TrailColorEnd(), p), Color.Transparent, p) * projectile.Opacity;
        }

        StripRenderer.DrawStripPadded(previousPositions, previousRotations, StripColorFunction, TrailWidth, positionOffset - Main.screenPosition, false);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();

        var spriteDirection = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        var shader = Assets.Shaders.HueShift.Asset.Value;
        shader.Parameters["uHueDifference"].SetValue(Hue);

        Main.spriteBatch.End(out var spriteBatchSnapshot);
        Main.spriteBatch.Begin(spriteBatchSnapshot with
        {
            SortMode = SpriteSortMode.Immediate,
            CustomEffect = shader
        });

        var fadeOutColor = Color.Lerp(Color.White, MainColor(), 1f - projectile.Opacity);
        var finalColor = new Color(projectile.GetAlpha(lightColor).ToVector4() * fadeOutColor.ToVector4());

        Main.spriteBatch.Draw(yoyoTexture, projectile.position - Main.screenPosition + positionOffset, null, finalColor, projectile.rotation, textureCenter, projectile.scale, spriteDirection, 0f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(spriteBatchSnapshot);

        return false;
    }
}