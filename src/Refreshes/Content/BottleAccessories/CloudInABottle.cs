using System.Reflection;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Features.Models;
using Daybreak.Common.Rendering;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;

namespace Refreshes.Content;

[Autoload(Side = ModSide.Client)]
internal static class CloudInABottle
{
    [Autoload(Side = ModSide.Client)]
    private sealed class Data : IStatic<Data>
    {
        public required RenderTargetLease CloudLease { get; init; }

        public static Data LoadData(Mod mod)
        {
            return Main.RunOnMainThread(
                () => new Data
                {
                    CloudLease = ScreenspaceTargetPool.Shared.Rent(
                        Main.instance.GraphicsDevice,
                        (w, h) => (w / 2, h / 2)
                    ),
                }
            ).GetAwaiter().GetResult();
        }

        public static void UnloadData(Data data)
        {
            Main.RunOnMainThread(
                () =>
                {
                    data.CloudLease.Dispose();
                }
            );
        }
    }

    [PoolCapacity(300)]
    private sealed class CloudJumpParticle : BaseParticle<CloudJumpParticle>
    {
        private float alpha;
        private Vector2 position;
        private float scale;
        private float shrinkRate;
        private Vector2 velocity;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            alpha = 1f;
            shrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            position += velocity;
            velocity *= 0.94f;

            scale *= shrinkRate;
            alpha = MathHelper.Clamp(scale * 2f, 0f, 1f);

            if (alpha <= 0 || scale <= 0.1f)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var tex = Assets.Images.Particles.Circle.Asset.Value;
            var origin = tex.Size() / 2;
            var color = new Color(213, 234, 231) * alpha;

            spriteBatch.Draw(
                new DrawParameters(tex)
                {
                    Position = position + settings.AnchorPosition,
                    Color = color,
                    Origin = origin,
                    Scale = new Vector2(scale),
                }
            );
        }
    }

    private static readonly ParticleRenderer particles = new();

    [ModSystemHooks.PostUpdateEverything]
    private static void UpdateParticles()
    {
        particles.Update();
    }

    [OnLoad]
    private static void ApplyHooks()
    {
        On_Main.DrawDust += DrawCloudParticles;

        MonoModHooks.Add(
            typeof(CloudInABottleJump).GetMethod("OnStarted", BindingFlags.Public | BindingFlags.Instance)!,
            OnStarted_SpawnOurCloudParticles
        );
    }

    private static void DrawCloudParticles(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);

        var cloudLease = IStatic<Data>.Instance.CloudLease;

        var sb = Main.spriteBatch;

        sb.Begin();
        sb.End(out var ss);

        using (cloudLease.Scope(clearColor: Color.Transparent))
        {
            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f) });

            particles.Settings.AnchorPosition = -Main.screenPosition;
            particles.Draw(Main.spriteBatch);

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

    private static void OnStarted_SpawnOurCloudParticles(CloudInABottleJump _, Player player, ref bool playSound)
    {
        playSound = false;

        SoundEngine.PlaySound(Assets.Sounds.Items.CloudJump.Asset with { PitchVariance = 0.3f, Pitch = 0f });

        /*
        for (int i = 0; i < 3; i++) {
            var p = CloudJumpParticle.Pool.RequestParticle();

            p.Position = player.Bottom;
            p.Velocity = Main.rand.NextVector2Circular(4f, 2f);
            p.Velocity.Y -= player.velocity.Y * 0.8f;
            p.Velocity.X -= player.velocity.X * 0.4f;

            p.Scale = Main.rand.NextFloat(2f, 2.5f);
            p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            p.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);


            CloudParticleRenderer.Particles.Add(p);
        }

        for (int i = 0; i < 5; i++) {
            var p = CloudJumpParticle.Pool.RequestParticle();

            p.Position = player.Bottom;
            p.Velocity = Main.rand.NextVector2Circular(4f, 2f);
            p.Velocity.Y += player.velocity.Y * 0.8f;
            p.Velocity.X += player.velocity.X * 0.4f;

            p.Scale = Main.rand.NextFloat(2f, 0.5f);
            p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            p.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);


            CloudParticleRenderer.Particles.Add(p);
        }
        */

        for (var i = 0; i < 3; i++)
        {
            // var p = DustFlameParticle.Pool.RequestParticle();

            var particleVel = -player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
            var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White, Color.White, 1, Main.rand.Next(24, 35));
            particle.LossPerFrame = 0.8f;
            particle.Swirly = true;
            particles.Add(particle);
        }

        for (var i = 0; i < 3; i++)
        {
            // var p = DustFlameParticle.Pool.RequestParticle();

            var particleVel = player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
            var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, Color.White, Color.White, Main.rand.NextFloat(2.5f, 1.5f), Main.rand.Next(24, 35));
            particle.LossPerFrame = 0.7f;
            particle.Swirly = false;
            particles.Add(particle);
        }

        /*
        for (var i = 0; i < 5; i++)
        {
            Dust.NewDust(player.Bottom, 1, 1, DustID.Cloud, -player.velocity.X * 0.4f, -player.velocity.Y * 0.2f);
        }
        */
    }

    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump)
    {
        return jump != ExtraJump.CloudInABottle;
    }
}
