using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;

namespace SlugsMod.Common;

/// <summary>
///     Changes appearance of common slimes like green slimes, blue slimes, etc.
/// </summary>
[UsedImplicitly]
internal sealed class CommonSlimeModifications : GlobalNPC {
    private bool _wasAirborneLastFrame;
    private float _frameCounter;
    private int _frame;
    
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type 
        is NPCID.BlueSlime;

    public override bool InstancePerEntity => true;

    [OnLoad, UsedImplicitly]
    static void ReplaceTextures() {
        TextureAssets.Npc[NPCID.BlueSlime] = Assets.Images.NPCs.CommonSlime.Asset;
    }

    public override void SetStaticDefaults() {
        Main.npcFrameCount[NPCID.BlueSlime] = 6;
    }

    public override void PostAI(NPC npc) {
        if (npc.velocity.X != 0) {
            npc.spriteDirection = (npc.velocity.X > 0 ? 1 : -1);
        }
        
        npc.rotation = npc.velocity.X * 0.05f * npc.velocity.Y * -0.5f;
        npc.rotation = MathHelper
            .Clamp(npc.rotation, -0.25f, 0.52f);
        
        bool currentlyAirborne = (npc.velocity.Y != 0f && npc.collideY);
        _wasAirborneLastFrame = currentlyAirborne;

        if(_wasAirborneLastFrame) {
            for(int i = 0; i < Main.rand.Next(5, 10); i++) {
                var dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.t_Slime, 0, 0, 100, npc.color);
            }
        }
    }

    //tbd?
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        if (!npc.IsABestiaryIconDummy)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Main.Transform
            );
        }

        return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }
    
    public override void FindFrame(NPC npc, int frameHeight) {
        const int idleFrame1 = 0;
        const int idleFrame2 = 1;
        const int aboutToJumpFrame = 3;
        const int jumpUpFrame = 5;
        const int jumpDownFrame = 4;
        const int animationSpeed = 8;

        if (npc.velocity.Y < 0f) {
            _frame = jumpUpFrame;
        }
        else if (npc.velocity.Y > 0f) {
            _frame = jumpDownFrame;
        }
        else {
            if (npc.ai[0] >= -30f && npc.ai[0] < 0f) {
                _frame = aboutToJumpFrame;
            }
            else {
                _frameCounter += 1.0f;
                if (_frameCounter > animationSpeed) {
                    _frameCounter = 0;
                    _frame++;
                }

                if (_frame > idleFrame2) {
                    _frame = idleFrame1;
                }
            }
        }
        
        npc.frame.Y = _frame * frameHeight;
    }
}