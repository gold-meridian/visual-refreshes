using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using MonoMod.RuntimeDetour;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;

namespace Refreshes.Content.BottleAccessories;

internal sealed class CloudInABottleModifications {
    //cancels out dust trail
    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump) => jump != ExtraJump.CloudInABottle;
    
    private static Hook hook = null!;
    private delegate void orig_OnStarted(CloudInABottleJump self, Player player, ref bool playSound);
    
    //cancels out initial cloud burst, replaces with our own
    [OnLoad]
    private static void HookCloud() {
        hook = new Hook(
            typeof(CloudInABottleJump).GetMethod("OnStarted")!, 
            (orig_OnStarted _, CloudInABottleJump _, Player player, ref bool playSound) => {
                playSound = false;

                SoundEngine.PlaySound(Assets.Sounds.Items.CloudJump.Asset with { PitchVariance = 0.3f, Pitch = 0f});
                for (int i = 0; i < 15; i++) {
                    var p = CloudJumpParticle.Pool.RequestParticle(); 
                    
                    p.Position = player.Bottom;
                    p.Velocity = Main.rand.NextVector2Circular(4f, 2f); 
                    p.Velocity.Y -= player.velocity.Y * 0.8f;
                    p.Velocity.X -= player.velocity.X * 0.4f;
                    
                    p.Scale = Main.rand.NextFloat(2f, 0.5f);
                    p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                    p.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);
                

                    CloudParticleRenderer.Particles.Add(p);
                }
            }
        );
    }
    
    [OnUnload]
    private static void Unhook() => hook.Dispose();
}


[PoolCapacity(300)]
public sealed class CloudJumpParticle : BaseParticle<CloudJumpParticle> {
    public Vector2 Position;
    public Vector2 Velocity;
    public float Scale;
    public float Rotation;
    public float Alpha;
    public float RotationSpeed;

    public override void FetchFromPool() {
        base.FetchFromPool();
        Alpha = 1f;
    }

    public override void Update(ref ParticleRendererSettings settings) {
        Position += Velocity;
        Velocity *= 0.94f;
        Rotation += RotationSpeed;
        Scale *= 0.97f;
        Alpha -= 0.03f;

        if (Alpha <= 0 || Scale <= 0.1f)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch) {
        var tex = Assets.Images.UI.AuthorTags.Tobias.Asset.Value; 
        var origin = tex.Size() / 2;
        var color = Color.White * Alpha;

        spriteBatch.Draw(tex, Position + settings.AnchorPosition, null, color, Rotation, origin, Scale, SpriteEffects.None, 0f);
    }
}

public sealed class CloudParticleRenderer : ModSystem
{
    private static RenderTargetLease? cloudLease;

    public static readonly ParticleRenderer Particles = new();

    public override void Load() {
        Main.RunOnMainThread(() => {
            cloudLease = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice);
        });

        On_Main.DrawDust += DrawCloudParticles;
    }

    public override void Unload() {
        cloudLease?.Dispose();
        cloudLease = null;
    }

    public override void PostUpdateEverything()
    {
        if (!Main.dedServ)
            Particles.Update();
    }

    private void DrawCloudParticles(On_Main.orig_DrawDust orig, Main self) {
        if (cloudLease is null) {
            orig(self);
            return;
        }

        var sb = Main.spriteBatch;
        
        sb.Begin();
        sb.End(out var ss);
        
        using (cloudLease.Scope(clearColor: Color.Transparent)) {
            Main.spriteBatch.Begin(ss with { RasterizerState = RasterizerState.CullNone });

            Particles.Settings.AnchorPosition = -Main.screenPosition;
            Particles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred, 
            BlendState.AlphaBlend, 
            Main.DefaultSamplerState, 
            DepthStencilState.None, 
            Main.Rasterizer, 
            null, 
            Main.Transform
        );

        Main.spriteBatch.Draw(cloudLease.Target, Vector2.Zero, Color.White);

        Main.spriteBatch.End();

        orig(self);
    }
}