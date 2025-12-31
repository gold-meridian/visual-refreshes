using Daybreak.Common.Features.Hooks;
using System.Collections.Generic;
using System.Numerics;
using Terraria.GameContent;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Refreshes.Common.Decals;

[Autoload(Side = ModSide.Client)]
public static class DecalLoader
{
    public static IReadOnlyDictionary<ushort, DecalRenderer> DecalRenderersByType => _decalRenderers;

    private static readonly Dictionary<ushort, DecalRenderer> _decalRenderers = [];
    private const int MAX_DECALS = 8192;
    private static ulong[] _decalActivity = new ulong[MAX_DECALS/64];
    public static readonly DecalData[] Decals = new DecalData[MAX_DECALS];

    [OnLoad]
    private static void LoadDecalTypes()
    {
        foreach(var mod in ModLoader.Mods)
        {
            var renderers = mod.GetContent<DecalRenderer>();
            foreach (var renderer in renderers)
            {
                _decalRenderers[renderer.Type] = renderer;
            }
        }
        On_Main.DrawNPCs += On_Main_DrawNPCs_DrawDecals;
    }

    private static void On_Main_DrawNPCs_DrawDecals(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        
        for (int i = 0; i < _decalActivity.Length; i++) {
            var copy = _decalActivity[i];
            
            if (copy == 0)
                continue;
            
            var start = BitOperations.TrailingZeroCount(copy);
            var end = 64 - BitOperations.LeadingZeroCount(copy);
            
            for (int j = start; j < end; j++) {
                ref var decal = ref Decals[i * 64 + j];
                _decalRenderers[decal.Type].Draw(decal, Main.spriteBatch);
            }
        }
        orig(self, behindTiles);
    }
    
    //todo
    public static void AddDecal(DecalData data) {
        for (var i = 0; i < _decalActivity.Length; i++) {

            for (var j = 0; j < 64; j++) {
                var mask = 1UL << j;
                if ((_decalActivity[i] & mask) == 0) {
                    var index = i * 64 + j;
                    Decals[index] = data;
                    _decalActivity[i] |= mask;
                    return;
                }
            }
        }
    }
}

internal sealed class TestRenderer : DecalRenderer
{
    public override ushort Type => 0;
    public override void Draw(DecalData data, SpriteBatch spriteBatch)
    {
        Main.NewText(data.Position.ToWorldCoordinates());
        Main.NewText(Main.offScreenRange);
        if (TileDrawing.IsVisible(Main.tile[data.Position]))
        {
            Vector2 zero = new Vector2(Main.drawToScreen ? 0 : Main.offScreenRange);
            spriteBatch.Draw(TextureAssets.Item[50].Value, data.Position.ToWorldCoordinates() - Main.sceneTilePos + zero, null, Color.White, 0, Vector2.zeroVector, 5f, SpriteEffects.None, 0);
        }
    }
}