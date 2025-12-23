using Daybreak.Common.Features.Hooks;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Refreshes.Common.Rendering;
using Refreshes.Core;
using Terraria.GameContent;
using Terraria.ID;

namespace Refreshes.Content.Yoyos;

internal sealed class TerrarianModifications : GlobalProjectile
{
    private Vector2[] previousPositions = new Vector2[20];
    private float[] previousRotations = new float[20];

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.Terrarian;
    }

    public override void SetDefaults(Projectile entity)
    {
        for (int i = 0; i < previousPositions.Length; i++)
        {
            previousPositions[i] = entity.position;
            previousRotations[i] = entity.velocity.ToRotation();
        }
    }

    public override bool PreAI(Projectile projectile)
    {
        for (int i = previousPositions.Length - 1; i > 0; i--)
        {
            previousPositions[i] = previousPositions[i - 1];
            previousRotations[i] = previousRotations[i - 1];
        }
        previousPositions[0] = projectile.position;
        previousRotations[0] = projectile.velocity.ToRotation();

        return true;
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        var trailTexture = TextureAssets.MagicPixel.Value;

        var trailShader = Assets.Shaders.BasicTrail.CreateBasicTrailPass();
        trailShader.Parameters.uTransformMatrix = Main.GameViewMatrix.NormalizedTransformationmatrix;
        trailShader.Parameters.uImage0 = new HlslSampler { Texture = trailTexture, Sampler = SamplerState.PointClamp };
        trailShader.Apply();

        static Color StripColorFunction(float p) => Color.White;
        static float StripWidthFunction(float p) => 3f;

        PrimitiveRenderer.DrawStripPadded(previousPositions, previousRotations, StripColorFunction, StripWidthFunction, projectile.Size / 2 - Main.screenPosition);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();

        var yoyoTexture = TextureAssets.Projectile[ProjectileID.Terrarian].Value;

        Main.spriteBatch.Draw(yoyoTexture, projectile.position - Main.screenPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, yoyoTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

        return false;
    }
}
