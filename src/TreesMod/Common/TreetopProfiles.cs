using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.GameContent;

namespace TreesMod.Common;

public record struct TreetopVariation(int Width, int Height, Vector2 OriginOffset = default);

public readonly record struct TreetopStyleProfile(int PaintIdx, Asset<Texture2D> TopTexture, Asset<Texture2D> BranchTexture, TreetopVariation[] Variations = null!) {
    public TreetopVariation GetVariation(int frame) => Variations[frame % Variations.Length];
    
    public Texture2D GetTop(int paintColor) {
        if (paintColor == 0) return TopTexture.Value;
        return Main.instance.TilePaintSystem.TryGetTreeTopAndRequestIfNotReady(PaintIdx, 0, paintColor) ?? TopTexture.Value;
    }
}

public static class TreetopProfiles {
    private const int treetop_resize_buffer = 128;
    
    public static readonly Dictionary<int, TreetopStyleProfile> Profiles = new();

    public static int VanillaTreetopCount;
    
    [OnLoad, UsedImplicitly]
    static void ResizeTextureArrays() {
        if (VanillaTreetopCount == 0) {
            VanillaTreetopCount = TextureAssets.TreeTop.Length;
        }
        
        if (TextureAssets.TreeTop.Length < treetop_resize_buffer) {
            Array.Resize(ref TextureAssets.TreeTop, treetop_resize_buffer);
        }
    }
    
    
    [OnUnload, UsedImplicitly]
    static void RevertTextureArrays() {
        if (VanillaTreetopCount > 0) {
            for (int i = VanillaTreetopCount; i < TextureAssets.TreeTop.Length; i++) {
                TextureAssets.TreeTop[i] = null;
            }

            Array.Resize(ref TextureAssets.TreeTop, VanillaTreetopCount);
        }

        Profiles.Clear();
    }
    
    [OnLoad, UsedImplicitly]
    static void Load() {
        //TextureAssets.TreeTop[60] = Assets.Images.Content.Trees.Tree_Tops_0.Asset;

        #region Forest Trees

        // for some reason, default forest treetop is idx 0, and the rest are 6 through 10 :\ annoying, but workable atleast
        
        Profiles[0] = new (
            PaintIdx: 0,
            TopTexture: TextureAssets.TreeTop[0],
            BranchTexture: TextureAssets.TreeBranch[0],
            Variations: new TreetopVariation[] {
                new(82, 82, Vector2.Zero),
                new(82, 82, Vector2.Zero),
                new(82, 82, Vector2.Zero)
            }
        );
        
        for(int i = 6; i <= 10; i++) {
            Profiles[i] = new (
                PaintIdx: i,
                TopTexture: TextureAssets.TreeTop[i],
                BranchTexture: TextureAssets.TreeBranch[i],
                Variations: new TreetopVariation[] {
                    new(82, 82, Vector2.Zero),
                    new(82, 82, Vector2.Zero),
                    new(82, 82, Vector2.Zero)
                }
            );
        }

        #endregion
    }

    public static bool TryGetProfile(int style, out TreetopStyleProfile profile) 
        => Profiles.TryGetValue(style, out profile);
}