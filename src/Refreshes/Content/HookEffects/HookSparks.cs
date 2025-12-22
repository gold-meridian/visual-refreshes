using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using MonoMod.Cil;
using Refreshes.Common.Particles;
using System;
using System.Reflection;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using Terraria.ID;

namespace Refreshes.Content.HookEffects;

internal sealed class HookEffects {
    [OnLoad]
    static void ApplyHookEffects() {
        MethodInfo method = typeof(Projectile).GetMethod("AI_007_GrapplingHooks", BindingFlags.Instance | BindingFlags.NonPublic);
        MonoModHooks.Modify(method, SpawnSparksOnHit);
    }
    private static void SpawnSparksOnHit(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        c.TryGotoNext(MoveType.Before,i => i.MatchCall("Terraria.Audio.SoundEngine", "PlaySound"));
        // remove original PlaySound call, due to it using a different overload
        c.Remove();
        // capture all the parameters from the stack, but don't use them
        c.EmitDelegate((int type, int x, int y, int style, float volumeScale, float pitchOffset) =>
        {
            return SoundEngine.PlaySound(SoundID.Item52 with { Pitch = 0.5f }, new Vector2(x, y));
        });
        c.EmitLdarg0();
        c.EmitDelegate((Projectile self) =>
        {
            for (int i = 0; i < 2; i++) {
                Vector2 velocity = self.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3, 7f) * -1f;
                var particle = PixelSparkParticle.RequestNew(self.Center, velocity, Vector2.UnitY * 1, Color.Yellow, Color.Red, 0.01f);
                particle.Scale *= 0.05f;
                PixelSparkRenderer.Particles.Add(particle);
            }
        });
    }
}
public sealed class PixelSparkRenderer : ModSystem {
    private static RenderTargetLease? sparkLease;

    public static ParticleRenderer Particles = new();
    [OnLoad]
    private void Init()
    {
        Main.RunOnMainThread(() => {
            sparkLease = ScreenspaceTargetPool.Shared.Rent(
                Main.instance.GraphicsDevice,
                (w, h, _, _) => (w/2, h/2)
            );
        });

        On_Main.DrawDust += DrawSparkParticles;
    }
    [OnUnload]
    private void Deinit()
    {
        sparkLease?.Dispose();
        sparkLease = null;
    }

    [ModSystemHooks.PostUpdateEverything]
    private void UpdateParticles()
    {
        if (!Main.dedServ)
            Particles.Update();
    }
    private static void DrawSparkParticles(On_Main.orig_DrawDust orig, Main self) {
        var sb = Main.spriteBatch;

        sb.Begin();
        sb.End(out var ss);

        using (sparkLease.Scope(clearColor: Color.Transparent))
        {
            Main.spriteBatch.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Matrix.CreateScale(0.5f) });
            Particles.Settings.AnchorPosition = -Main.screenPosition;
            Particles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }
        using (sb.Scope())
        {
            sb.Begin(ss with { SamplerState = SamplerState.PointClamp, TransformMatrix = Main.Transform, RasterizerState = Main.Rasterizer });

            Main.spriteBatch.Draw(new DrawParameters(sparkLease.Target)
            {
                Position = Vector2.Zero,
                Scale = new(2f),
                Color = Color.White,
            });
        }
        orig(self);
    }
}

public class PixelSparkParticle : BaseParticle<PixelSparkParticle>
{
    public bool Collide;
    public Color ColorGlow;
    public Color ColorTint;
    private Vector2 EndPosition;
    public Vector2 Gravity;
    public float Mass;

    public Vector2 Position;
    public float Scale;
    public Vector2 Velocity;

    public static PixelSparkParticle RequestNew(Vector2 position, Vector2 velocity, Vector2 gravity, Color color, Color glowColor, float mass, bool collide = true)
    {
        var spark = Pool.RequestParticle();
        spark.Position = position;
        spark.EndPosition = position;
        spark.Velocity = velocity;
        spark.Gravity = gravity;
        spark.ColorTint = color;
        spark.ColorGlow = glowColor;
        spark.Mass = mass;
        spark.Collide = collide;
        spark.Scale = Main.rand.NextFloat(0.9f, 1.1f);
        return spark;
    }

    public override void FetchFromPool()
    {
        base.FetchFromPool();
        Velocity = Vector2.Zero;
    }

    public override void Update(ref ParticleRendererSettings settings)
    {
        Position += Velocity;
        Velocity += Gravity * (0.5f - Scale * 0.5f);
        if (Velocity.Length() > 24f)
        {
            Velocity = Vector2.Lerp(Velocity, Velocity.SafeNormalize(Vector2.Zero) * 24f, 0.4f);
        }

        var lerpFactor = 0.2f;
        if (Collide)
        {
            var bounce = Collision.TileCollision(Position - new Vector2(2, 3), Velocity, 4, 2);
            if (Math.Abs(bounce.X - Velocity.X) > 0)
            {
                Velocity.X *= -0.3f;
                lerpFactor = 0.5f;
            }

            if (Math.Abs(bounce.Y - Velocity.Y) > 0)
            {
                Scale *= 0.95f;
                Velocity.Y *= -Main.rand.NextFloat(0.6f, 0.9f);
                Velocity.X *= 0.6f;
                lerpFactor = 0.5f;
            }

            if (Collision.SolidCollision(Position - Vector2.One, 2, 2))
            {
                Scale *= 0.8f;
            }
        }

        if (Scale < 0.1f)
        {
            Scale *= 0.9f + Math.Clamp(Mass, 0f, 1f) * 0.01f;
            lerpFactor = 0.5f;

            if (Scale < 0.01f)
            {
                ShouldBeRemovedFromRenderer = true;
            }
        }
        else
        {
            Scale *= 0.4f + Math.Clamp(Mass * 0.5f, 0f, 0.5f);
        }

        EndPosition = Vector2.Lerp(EndPosition, Position - Velocity, lerpFactor);
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        var texture = Assets.Images.Particles.PixelSparkParticle.Asset.Value;
        var glow = Assets.Images.Particles.GlowSmall.Asset.Value;

        var rotation = MathF.Round(Position.AngleFrom(EndPosition) / MathHelper.TwoPi * 8f) / 8f * MathHelper.TwoPi;
        var fadeOut = Utils.GetLerpValue(0f, 0.05f, Scale, true);

        var particlePos = new Vector2((int)Math.Round(Position.X / 2) * 2, (int)Math.Round(Position.Y / 2) * 2) + settings.AnchorPosition;
        var unRotatedVel = Velocity.RotatedBy(-rotation);
        var stretch = new Vector2(1.1f + Position.Distance(EndPosition) * 0.15f, 1f) * (1f + Mass * 0.01f);

        spritebatch.Draw(texture, particlePos, texture.Frame(), ColorTint * fadeOut, rotation, texture.Size() * 0.5f, stretch, 0, 0);
        spritebatch.Draw(glow, particlePos, glow.Frame(), ColorGlow with { A = 0 } * fadeOut, rotation, glow.Size() * 0.5f, new Vector2(0.06f, 0.12f) * stretch, 0, 0);
    }
}