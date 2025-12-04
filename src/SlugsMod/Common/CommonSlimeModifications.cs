using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using Terraria.GameContent;
using Terraria.ID;

namespace SlugsMod.Common;

[UsedImplicitly]
internal sealed class CommonSlimeModifications : GlobalNPC {
    private bool _wasAirborneLastFrame;
    
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type 
        is NPCID.BlueSlime 
        or NPCID.IceSlime 
        or NPCID.UmbrellaSlime;

    public override bool InstancePerEntity => true;

    [OnLoad, UsedImplicitly]
    static void ReplaceTextures() {
        TextureAssets.Npc[NPCID.BlueSlime] = Assets.Images.NPCs.CommonSlime.Asset;
        TextureAssets.Npc[NPCID.IceSlime] = Assets.Images.NPCs.IceSlime.Asset;
        TextureAssets.Npc[NPCID.UmbrellaSlime] = Assets.Images.NPCs.UmbrellaSlime.Asset;
    }
    
    [GlobalNPCHooks.PostAI, UsedImplicitly]
    void SlimeEffects(NPC npc) {
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
                var dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.t_Slime, 0, 0, 0, npc.color);
            }
        }
    }
}