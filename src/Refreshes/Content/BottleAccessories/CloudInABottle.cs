using System;
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
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });

            Main.spriteBatch.Draw(
                new (cloudLease.Target) {
                    Position = Vector2.Zero,
                    Scale = new Vector2(1f / 0.5f),
                    Color = Color.White * 0.6f,
                }
            );
        }
    }

    private static void OnStarted_SpawnOurCloudParticles(CloudInABottleJump _, Player player, ref bool playSound)
    {
        playSound = false;

        SoundEngine.PlaySound(Assets.Sounds.Items.CloudJump.Asset with { PitchVariance = 0.3f, Pitch = 0f });
        
        var largeParticle = LargeCloudJumpParticle.Pool.RequestParticle();

        largeParticle.Position = player.Bottom;
        largeParticle.Velocity = -player.velocity * 0.2f;
        largeParticle.Velocity.Y -= player.velocity.Y * 0.2f;
        largeParticle.Velocity.X -= player.velocity.X * 0.2f;

        largeParticle.Rotation = Angle.FromRadians(player.velocity.X * 0.05f);
        
        largeParticle.Scale = Main.rand.NextFloat(2f, 2.5f);


        ParticleEngine.PARTICLES.Add(largeParticle);
        
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

        for (int i = 0; i < 5; i++) 
        {
            var p = CloudJumpParticle.Pool.RequestParticle();

            p.Position = player.Bottom;
            p.Velocity = Main.rand.NextVector2Circular(4f, 2f);
            p.Velocity.Y += player.velocity.Y * 0.8f;
            p.Velocity.X += player.velocity.X * 0.4f;

            p.Scale = Main.rand.NextFloat(2f, 0.5f);


            particles.Add(p);
        }

        for (var i = 0; i < 3; i++)
        {
            var dirVel = Main.rand.NextBool() ? -player.velocity : player.velocity;
            
            var particleVel = -player.velocity * 0.7f + Main.rand.NextVector2Circular(5, 5);
            var particle = DustFlameParticle.RequestNew(player.Bottom, particleVel, new Color(84, 134, 237) * 0.5f, Color.White, 2, Main.rand.Next(24, 35));
            particle.LossPerFrame = 0.4f;
            particle.Swirly = true;
            particles.Add(particle);
        }

        
        for (var i = 0; i < 5; i++)
        {
            Dust.NewDust(player.Bottom, 1, 1, DustID.Cloud, -player.velocity.X * 0.4f, -player.velocity.Y * 0.2f);
        }
        
    }

    [ModPlayerHooks.CanShowExtraJumpVisuals]
    public static bool CancelVanillaVisuals(ExtraJump jump)
    {
        return jump != ExtraJump.CloudInABottle;
    }
    
    [PoolCapacity(300)]
    private sealed class CloudJumpParticle : BaseParticle<CloudJumpParticle>
    {
        public float Alpha;
        public Vector2 Position;
        public float Scale;
        public float ShrinkRate;
        public Vector2 Velocity;
        private bool flip;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            Alpha = 1f;
            ShrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
            flip = Main.rand.NextBool(5);
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;
            Velocity *= 0.94f;

            Scale *= ShrinkRate;
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
            
            var lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            var color = lightColor * Alpha;
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
        public float ShrinkRate;
        public Vector2 Velocity;
        public Angle Rotation;
        public float RotationSpeed;

        public override void FetchFromPool()
        {
            base.FetchFromPool();

            Rotation = Angle.Zero;
            RotationSpeed = 0;
            ShrinkRate = Main.rand.NextFloat(0.91f, 0.95f);
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;
            Velocity *= 0.94f;

            Scale *= ShrinkRate;

            if (Scale <= 0.1f)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var tex = Assets.Images.Content.BottleAccessories.LargeCloudParticle_1.Asset.Value;
            var origin = tex.Size() / 2;
            
            var lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            var color = lightColor;

            spriteBatch.Draw(
                new DrawParameters(tex)
                {
                    Position = Position + settings.AnchorPosition,
                    Color = color,
                    Origin = origin,
                    Scale = new Vector2(Scale),
                    Rotation = Rotation,
                }
            );
        }
    }
}
