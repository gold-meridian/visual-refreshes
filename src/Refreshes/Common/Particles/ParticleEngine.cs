using Daybreak.Common.Features.Hooks;
using Terraria.Graphics.Renderers;

namespace Refreshes.Common.Particles;

/// <summary>
///     Contains particle systems.
/// </summary>
public static class ParticleEngine
{
    /// <summary>
    ///     Renders behind dust.
    /// </summary>
    public static readonly ParticleRenderer Particles = new();

    /// <summary>
    ///     Renders behind front gore.
    /// </summary>
    public static readonly ParticleRenderer GoreLayer = new();

    [OnLoad]
    public static void Load()
    {
        On_Main.DrawDust += DrawParticles;
        On_Main.DrawGore += DrawGoreParticles;
        On_Main.UpdateParticleSystems += UpdateParticles;
    }

    private static void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
    {
        orig(self);

        Particles.Update();
        GoreLayer.Update();
    }

    private static void DrawParticles(On_Main.orig_DrawDust orig, Main self)
    {
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        Particles.Settings.AnchorPosition = -Main.screenPosition;
        Particles.Draw(Main.spriteBatch);
        Main.spriteBatch.End();

        orig(self);
    }

    private static void DrawGoreParticles(On_Main.orig_DrawGore orig, Main self)
    {
        //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        GoreLayer.Settings.AnchorPosition = -Main.screenPosition;
        GoreLayer.Draw(Main.spriteBatch);
        //Main.spriteBatch.End();

        orig(self);
    }
}
