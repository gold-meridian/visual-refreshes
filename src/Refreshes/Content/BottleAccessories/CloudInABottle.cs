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
    
    private float alphaDecay;

    public override void FetchFromPool() {
        base.FetchFromPool();
        Alpha = 1f;
        
        alphaDecay = Main.rand.NextFloat(0.02f, 0.05f);
    }

    public override void Update(ref ParticleRendererSettings settings) {
        Position += Velocity;
        Velocity *= 0.94f;
        Rotation += RotationSpeed;
        Scale *= .97f;
        Alpha -= alphaDecay;

        if (Alpha <= 0 || Scale <= 0.1f)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch) {
        var tex = Assets.Images.Particles.Circle.Asset.Value; 
        var origin = tex.Size() / 2;
        var color = Color.White * Alpha;

        spriteBatch.Draw(tex, Position + settings.AnchorPosition, null, color, Rotation, origin, Scale, SpriteEffects.None, 0f);
    }
}

public sealed class CloudParticleRenderer : ModSystem
{
    private static RenderTargetLease? cloudLease;

    public static ParticleRenderer Particles = new();

    [OnLoad]
    private void Init() {
        Main.RunOnMainThread(() => {
            cloudLease = ScreenspaceTargetPool.Shared.Rent(
                Main.instance.GraphicsDevice, 
                (w, h, _, _) => (w / 2, h / 2) 
            );
        });

        On_Main.DrawDust += DrawCloudParticles;
    }

    [OnUnload]
    private void Deinit() {
        cloudLease?.Dispose();
        cloudLease = null;
    }

    [ModSystemHooks.PostUpdateEverything]
    private void UpdateParticles()
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
        
        using (cloudLease.Scope(clearColor: Color.Transparent)) 
        {
            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f)});

            Particles.Settings.AnchorPosition = -Main.screenPosition;
            Particles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        using (sb.Scope())
        {
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp,  TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer});
            
            Main.spriteBatch.Draw(new DrawParameters(cloudLease.Target)
            {
                Position = Vector2.Zero,
                Scale = new(1f / 0.5f),
                Color = Color.White
            });
        }

        orig(self);
    }
}