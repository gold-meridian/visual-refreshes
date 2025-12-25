using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using MonoMod.RuntimeDetour;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;

namespace Refreshes.Content.BottleAccessories;

internal sealed class CloudInABottleModifications
{
    private delegate void orig_OnStarted(CloudInABottleJump self, Player player, ref bool playSound);

    private static Hook hook = null!;

    //cancels out dust trail
    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump)
    {
        return jump != ExtraJump.CloudInABottle;
    }

    //cancels out initial cloud burst, replaces with our own
    [OnLoad]
    private static void HookCloud()
    {
        hook = new Hook(
            typeof(CloudInABottleJump).GetMethod("OnStarted")!,
            (orig_OnStarted _, CloudInABottleJump _, Player player, ref bool playSound) =>
            {
                playSound = false;

                SoundEngine.PlaySound(Assets.Sounds.Items.CloudJump.Asset with { PitchVariance = 0.3f, Pitch = 0f });
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

                for (var i = 0; i < 3; i++)
                {
                    var p = DustFlameParticle.Pool.RequestParticle();

                    var particleVel = -player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
                    var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White, Color.White, 1, Main.rand.Next(24, 35));
                    particle.LossPerFrame = 0.8f;
                    particle.Swirly = true;
                    CloudParticleRenderer.Particles.Add(particle);
                }

                for (var i = 0; i < 3; i++)
                {
                    var p = DustFlameParticle.Pool.RequestParticle();

                    var particleVel = player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
                    var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White, Color.White, Main.rand.NextFloat(2.5f, 1.5f), Main.rand.Next(24, 35));
                    particle.LossPerFrame = 0.7f;
                    particle.Swirly = false;
                    CloudParticleRenderer.Particles.Add(particle);
                }

                for (var i = 0; i < 5; i++)
                {
                    //Dust.NewDust(player.Bottom, 1, 1, DustID.Cloud, -player.velocity.X * 0.4f, -player.velocity.Y * 0.2f);
                }
            }
        );
    }

    [OnUnload]
    private static void Unhook()
    {
        hook.Dispose();
    }
}

[PoolCapacity(300)]
public sealed class CloudJumpParticle : BaseParticle<CloudJumpParticle>
{
    public float Alpha;

    private float alphaDecay;
    public Vector2 Position;
    public float Rotation;
    public float RotationSpeed;
    public float Scale;
    private float shrinkRate;
    public Vector2 Velocity;

    public override void FetchFromPool()
    {
        base.FetchFromPool();
        Alpha = 1f;

        shrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
    }

    public override void Update(ref ParticleRendererSettings settings)
    {
        Position += Velocity;
        Velocity *= 0.94f;
        Rotation += RotationSpeed;

        Scale *= shrinkRate;
        Alpha = MathHelper.Clamp(Scale * 2f, 0f, 1f);

        if (Alpha <= 0 || Scale <= 0.1f)
        {
            ShouldBeRemovedFromRenderer = true;
        }
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
    {
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
    private void Init()
    {
        Main.RunOnMainThread(
            () =>
            {
                cloudLease = ScreenspaceTargetPool.Shared.Rent(
                    Main.instance.GraphicsDevice,
                    (w, h, _, _) => (w / 2, h / 2)
                );
            }
        );

        On_Main.DrawDust += DrawCloudParticles;
    }

    [OnUnload]
    private void Deinit()
    {
        Main.RunOnMainThread(
            () =>
            {
                cloudLease?.Dispose();
                cloudLease = null;
            }
        );
    }

    [ModSystemHooks.PostUpdateEverything]
    private void UpdateParticles()
    {
        if (!Main.dedServ)
        {
            Particles.Update();
        }
    }

    private void DrawCloudParticles(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        if (cloudLease is null)
        {
            orig(self);
            return;
        }

        var sb = Main.spriteBatch;

        sb.Begin();
        sb.End(out var ss);

        using (cloudLease.Scope(clearColor: Color.Transparent))
        {
            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f) });

            Particles.Settings.AnchorPosition = -Main.screenPosition;
            Particles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        using (sb.Scope())
        {
            var shader = Assets.Shaders.Outline.Asset.Value;

            shader.Parameters["uTexelSize"].SetValue(new Vector2(1f / cloudLease.Target.Width, 1f / cloudLease.Target.Height));
            shader.Parameters["uOutlineColor"].SetValue(new Color(163, 180, 191).ToVector4());

            sb.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });

            Main.spriteBatch.Draw(
                new DrawParameters(cloudLease.Target)
                {
                    Position = Vector2.Zero,
                    Scale = new Vector2(1f / 0.5f),
                    Color = Color.White * 0.8f,
                }
            );
        }
    }
}
