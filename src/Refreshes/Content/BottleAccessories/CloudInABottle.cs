using System;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using MonoMod.RuntimeDetour;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using Terraria.ID;

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
                // for (int i = 0; i < 3; i++) {
                //     var p = CloudJumpParticle.Pool.RequestParticle(); 
                //     
                //     p.Position = player.Bottom;
                //     p.Velocity = Main.rand.NextVector2Circular(4f, 2f); 
                //     p.Velocity.Y -= player.velocity.Y * 0.8f;
                //     p.Velocity.X -= player.velocity.X * 0.4f;
                //     
                //     p.Scale = Main.rand.NextFloat(2f, 2.5f);
                //     p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                //     p.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);
                //
                //
                //     CloudParticleRenderer.Particles.Add(p);
                // }
                //
                // for (int i = 0; i < 5; i++) {
                //     var p = CloudJumpParticle.Pool.RequestParticle(); 
                //     
                //     p.Position = player.Bottom;
                //     p.Velocity = Main.rand.NextVector2Circular(4f, 2f); 
                //     p.Velocity.Y += player.velocity.Y * 0.8f;
                //     p.Velocity.X += player.velocity.X * 0.4f;
                //     
                //     p.Scale = Main.rand.NextFloat(2f, 0.5f);
                //     p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                //     p.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);
                //
                //
                //     CloudParticleRenderer.Particles.Add(p);
                // }
                
                for (int i = 0; i < 3; i++) {
                    var p = DustFlameParticle.Pool.RequestParticle(); 
                    
                    var particleVel = -player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
                    var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White,  Color.White, 1, Main.rand.Next(24, 35));
                    particle.LossPerFrame = 0.8f;
                    particle.Swirly = true;
                    CloudParticleRenderer.Particles.Add(particle);
                }
                
                for (int i = 0; i < 3; i++) {
                    var p = DustFlameParticle.Pool.RequestParticle(); 
                    
                    var particleVel = player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
                    var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White,  Color.White, Main.rand.NextFloat(2.5f, 1.5f), Main.rand.Next(24, 35));
                    particle.LossPerFrame = 0.7f;
                    particle.Swirly = false;
                    CloudParticleRenderer.Particles.Add(particle);
                }
                
                for (int i = 0; i < 5; i++)
                {
                    //Dust.NewDust(player.Bottom, 1, 1, DustID.Cloud, -player.velocity.X * 0.4f, -player.velocity.Y * 0.2f);
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
    private float shrinkRate;

    public override void FetchFromPool() {
        base.FetchFromPool();
        Alpha = 1f;
        
        shrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
    }

    public override void Update(ref ParticleRendererSettings settings) {
        Position += Velocity;
        Velocity *= 0.94f;
        Rotation += RotationSpeed;
        
        Scale *= shrinkRate;
        Alpha = MathHelper.Clamp(Scale * 2f, 0f, 1f);

        if (Alpha <= 0 || Scale <= 0.1f)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch) {
        var tex = Assets.Images.Particles.Circle.Asset.Value; 
        var origin = tex.Size() / 2;
        var color = new Color(213, 234, 231) * Alpha;

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
        Main.RunOnMainThread(() => {
            cloudLease?.Dispose();
            cloudLease = null;
        });
    }

    [ModSystemHooks.PostUpdateEverything]
    private void UpdateParticles()
    {
        if (!Main.dedServ)
            Particles.Update();
    }

    private void DrawCloudParticles(On_Main.orig_DrawDust orig, Main self) {
        
        orig(self);
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
            var shader = Assets.Shaders.Outline.Asset.Value;
            
            shader.Parameters["uTexelSize"].SetValue(new Vector2(1f / cloudLease.Target.Width, 1f / cloudLease.Target.Height));
            shader.Parameters["uOutlineColor"].SetValue(new Color(163, 180, 191).ToVector4());
            
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp,  TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });
            
            Main.spriteBatch.Draw(new DrawParameters(cloudLease.Target) {
                Position = Vector2.Zero,
                Scale = new(1f / 0.5f),
                Color = Color.White * 0.8f,
            });
        }
    }
}

public class DustFlameParticle : BaseParticle<DustFlameParticle>
{
    public bool ApplyLighting;
    public Color ColorInsideTint;
    public Color ColorTint;

    public int LifeTime;
    public float LossPerFrame;
    private int MaxLifeTime;

    public Vector2 Position;
    public float Rotation;
    public float Scale;

    public bool Swirly;
    private int Variant;
    public Vector2 Velocity;

    public static DustFlameParticle RequestNew(Vector2 position, Vector2 velocity, Color color, Color insideColor, float scale, int lifeTime = 20)
    {
        var particle = Pool.RequestParticle();
		particle.Position = position;
        particle.Velocity = velocity;
        particle.ColorTint = color;
        particle.ColorInsideTint = insideColor;
        particle.Scale = scale;
        particle.MaxLifeTime = lifeTime;
        particle.Variant = Main.rand.Next(6);
        particle.Rotation = (int)Math.Round(velocity.ToRotation() / MathHelper.PiOver4) * MathHelper.PiOver4 + MathHelper.PiOver2;
        particle.LossPerFrame = 0.15f;
        return particle;
    }

    public override void FetchFromPool()
    {
        base.FetchFromPool();
        Swirly = false;
        ApplyLighting = false;
        LifeTime = 0;
    }

    public override void Update(ref ParticleRendererSettings settings)
    {
        Position += Velocity;
        Velocity *= 1f - LossPerFrame;
        Velocity.Y -= 0.03f;
        Scale *= 0.99f;

        if (Collision.SolidTiles(Position - new Vector2(6), 12, 12))
        {
            LifeTime++;
            Velocity *= 0.8f;
        }

        if (++LifeTime >= MaxLifeTime)
        {
            ShouldBeRemovedFromRenderer = true;
        }
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        var progress = (float)LifeTime / MaxLifeTime;

        var frameCount = Swirly ? 8 : 10;
        var texture = Swirly ? Assets.Images.Particles.SwirlyFlameParticle.Asset.Value : Assets.Images.Particles.DustFlameParticle.Asset.Value;
        var frame = texture.Frame(frameCount, 6, (int)MathF.Floor(progress * frameCount), Variant % 3);
        var glowFrame = texture.Frame(frameCount, 6, (int)MathF.Floor(progress * frameCount), Variant % 3 + 3);

        SpriteEffects spriteEffects = 0;
        if (Variant > 3)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }

        var drawScale = Scale * MathF.Cbrt(progress);

        var lightColor = Color.White;

        if (ApplyLighting)
        {
            lightColor = Lighting.GetColor(Position.ToTileCoordinates());
        }

        spritebatch.Draw(texture, Position + settings.AnchorPosition, frame, ColorTint.MultiplyRGBA(lightColor), Rotation, frame.Size() / 2, drawScale, spriteEffects, 0);
        spritebatch.Draw(texture, Position + settings.AnchorPosition, glowFrame, ColorInsideTint.MultiplyRGBA(lightColor), Rotation, frame.Size() / 2, drawScale, spriteEffects, 0);
    }
}