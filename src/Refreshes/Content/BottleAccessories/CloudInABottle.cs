using System.Reflection;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Features.Models;
using Daybreak.Common.Mathematics;
using Daybreak.Common.Rendering;
using Refreshes.Common.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using Terraria.ID;

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

    private static readonly ParticleRenderer particles = new();
    private static readonly ParticleRenderer large_particles = new();

    [ModSystemHooks.PostUpdateEverything]
    private static void UpdateParticles()
    {
        particles.Update();
        large_particles.Update();
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
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });

            Main.spriteBatch.Draw(
                new(cloudLease.Target)
                {
                    Position = Vector2.Zero,
                    Scale = new Vector2(1f / 0.5f),
                    Color = Color.White * 0.4f,
                }
            );


            Main.spriteBatch.End(out var ss2);

            Main.spriteBatch.Begin(ss2 with { SamplerState = SamplerState.PointClamp });

            large_particles.Settings.AnchorPosition = -Main.screenPosition;
            large_particles.Draw(Main.spriteBatch);
        }
    }

    private static void OnStarted_SpawnOurCloudParticles(CloudInABottleJump _, Player player, ref bool playSound)
    {
        playSound = false;

        SoundEngine.PlaySound(Assets.Sounds.Items.CloudJump.Asset with { PitchVariance = 0.3f, Pitch = 0f });

        var startDir = new Vector2(-player.velocity.X * 0.4f, 3f);
        var startAngle = startDir.ToRotation();

        const float start_speed = 3f;

        for (var i = 0; i < 3; i++)
        {
            var largeParticle = LargeCloudJumpParticle.Pool.RequestParticle();
            largeParticle.Position = player.Bottom;

            var spread = MathHelper.Lerp(-1.25f, 1.25f, i / 2f);

            largeParticle.Velocity = new Vector2(0, start_speed).RotatedBy(startAngle + spread - MathHelper.PiOver2);

            largeParticle.Rotation = Angle.FromRadians(largeParticle.Velocity.X * 0.15f);
            largeParticle.Scale = 1.3f;

            large_particles.Add(largeParticle);
        }

        for (int i = 0; i < 3; i++)
        {
            var p = CloudJumpParticle.Pool.RequestParticle();

            p.Position = player.Bottom;
            p.Velocity = Main.rand.NextVector2Circular(4f, 2f);
            p.Velocity.Y -= player.velocity.Y * 0.8f;
            p.Velocity.X -= player.velocity.X * 0.4f;

            p.Scale = Main.rand.NextFloat(2f, 2.5f);


            particles.Add(p);
        }

        for (var i = 0; i < 3; i++)
        {
            var dirVel = Main.rand.NextBool() ? -player.velocity : player.velocity;

            var particleVel = player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
            var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, new Color(84, 134, 237) * 0.5f, Color.White, 2, Main.rand.Next(24, 35));
            particle.LossPerFrame = 0.4f;
            particle.Swirly = true;
            particles.Add(particle);
        }

        for (var i = 0; i < 15; i++)
        {
            Dust.NewDust(player.Bottom, 1, 1, DustID.Cloud, -player.velocity.X * 0.4f, -player.velocity.Y * 0.2f);
        }

    }

    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ModPlayerHooks.CanShowExtraJumpVisuals.Original orig, ModPlayer self, ExtraJump jump)
    {
        if (jump != ExtraJump.CloudInABottle)
        {
            return orig(jump);
        }
        
        return false;
    }

    [PoolCapacity(300)]
    private sealed class CloudJumpParticle : BaseParticle<CloudJumpParticle>
    {
        private float alpha;
        public Vector2 Position;
        public float Scale;
        private float shrinkRate;
        public Vector2 Velocity;
        private bool flip;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            alpha = 1f;
            flip = Main.rand.NextBool(5);
            shrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;
            Velocity *= 0.94f;

            Scale *= shrinkRate;
            alpha = MathHelper.Clamp(Scale * 2f, 0f, 1f);

            if (alpha <= 0 || Scale <= 0.1f)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var tex = Assets.Images.Particles.Circle.Asset.Value;
            var origin = tex.Size() / 2;

            var lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            var color = lightColor * alpha;

            var effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(
                new DrawParameters(tex)
                {
                    Position = Position + settings.AnchorPosition,
                    Color = color,
                    Origin = origin,
                    Scale = new Vector2(Scale),
                    Effects = effects
                }
            );
        }
    }


    [PoolCapacity(300)]
    private sealed class LargeCloudJumpParticle : BaseParticle<LargeCloudJumpParticle>
    {
        public Vector2 Position;
        public float Scale;
        public Vector2 Velocity;
        public Angle Rotation;

        private int variation;

        private int lifeTime;
        private const int max_life = 28;
        private const int frame_count = 7;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            variation = Main.rand.Next(1, 3);
            Rotation = Angle.Zero;
            lifeTime = 0;
            Scale = 1f;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;
            Velocity *= 0.94f;

            if (++lifeTime >= max_life)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var tex = variation switch
            {
                1 => Assets.Images.Content.BottleAccessories.LargeCloudParticle_1.Asset.Value,
                2 => Assets.Images.Content.BottleAccessories.LargeCloudParticle_2.Asset.Value,
                _ => Assets.Images.Content.BottleAccessories.LargeCloudParticle_1.Asset.Value,
            };

            var progress = (float)lifeTime / max_life;
            var frameIndex = (int)(progress * frame_count);

            var frame = tex.Frame(1, frame_count, 0, frameIndex);

            var origin = frame.Size() / 2;

            var lightColor = Lighting.GetColor(Position.ToTileCoordinates());

            spriteBatch.Draw(
                new(tex)
                {
                    Position = Position + settings.AnchorPosition,
                    Source = frame,
                    Color = lightColor,
                    Origin = origin,
                    Scale = new Vector2(Scale),
                    Rotation = Rotation,
                }
            );
        }
    }
}