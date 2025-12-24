using Daybreak.Common.Features.Hooks;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Refreshes.Common.Rendering;
using Refreshes.Core;
using System;
using System.Diagnostics;
using Terraria.GameContent;
using Terraria.ID;

namespace Refreshes.Content.Yoyos;

internal sealed class TerrarianModifications : GlobalProjectile
{
    private const float trail_fadeoff_length = 120f;

    private Vector2[]? previousPositions;
    private float[]? previousRotations;
    private float totalDistance;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.Terrarian;
    }

    public override void PostAI(Projectile projectile)
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

        for (var i = previousPositions.Length - 1; i > 0; i--)
        {
            previousPositions[i] = previousPositions[i - 1];
            previousRotations[i] = previousRotations[i - 1];
        }
        previousPositions[0] = projectile.position + projectile.velocity;
        previousRotations[0] = projectile.velocity.ToRotation();

        totalDistance = 0;
        for (var i = 0; i < previousPositions.Length - 2; i++)
        {
            totalDistance += (previousPositions[i + 1] - previousPositions[i]).Length();
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        Debug.Assert(previousPositions != null);
        Debug.Assert(previousRotations != null);

        var yoyoTexture = TextureAssets.Projectile[ProjectileID.Terrarian].Value;

        var textureCenter = yoyoTexture.Size() * 0.5f;
        var positionOffset = textureCenter + new Vector2(0, projectile.gfxOffY);

        var trailTexture = TextureAssets.MagicPixel.Value;

        var trailShader = Assets.Shaders.BasicTrail.CreateBasicTrailPass();
        trailShader.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        trailShader.Parameters.uImage0 = new HlslSampler { Texture = trailTexture, Sampler = SamplerState.PointClamp };
        trailShader.Apply();

        var trailFadeoffLengthNormalized = (totalDistance - trail_fadeoff_length) / totalDistance;
        trailFadeoffLengthNormalized = MathF.Max(trailFadeoffLengthNormalized, 0f);

        Color StripColorFunction(float p)
        {
            var trailFadeoffProgress = Utils.GetLerpValue(trailFadeoffLengthNormalized, 1f, p, true);
            return Color.Lerp(Color.Chartreuse * 0.5f, Color.Transparent, trailFadeoffProgress);
        }
        static float StripWidthFunction(float p) => 8f;

        PrimitiveRenderer.DrawStripPadded(previousPositions, previousRotations, StripColorFunction, StripWidthFunction, positionOffset - Main.screenPosition);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();

        var spriteDirection = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        Main.spriteBatch.Draw(yoyoTexture, projectile.position - Main.screenPosition + positionOffset, null, projectile.GetAlpha(lightColor), projectile.rotation, textureCenter, projectile.scale, spriteDirection, 0f);

        return false;
    }
}
