using System;
using System.Collections.Generic;
using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using ReLogic.Content;
using Terraria.GameContent;

namespace Refreshes.Common;

public record struct TreetopVariation(int Width, int Height, Vector2 OriginOffset = default);

public readonly record struct TreeStyleProfile(int PaintIdx, Asset<Texture2D> TopTexture, Asset<Texture2D> BranchTexture, TreetopVariation[] Variations = null!)
{
    public TreetopVariation GetVariation(int frame)
    {
        return Variations[frame % Variations.Length];
    }

    public Texture2D GetTop(int paintColor)
    {
        if (paintColor == 0)
        {
            return TopTexture.Value;
        }

        return Main.instance.TilePaintSystem.TryGetTreeTopAndRequestIfNotReady(PaintIdx, 0, paintColor) ?? TopTexture.Value;
    }
}

public static class TreeProfiles
{
    private const int treetop_resize_buffer = 128;

    public static readonly Dictionary<int, TreeStyleProfile> Profiles = new();

    public static int VanillaTreetopCount;

    [OnLoad]
    [UsedImplicitly]
    private static void ResizeTextureArrays()
    {
        if (VanillaTreetopCount == 0)
        {
            VanillaTreetopCount = TextureAssets.TreeTop.Length;
        }

        if (TextureAssets.TreeTop.Length < treetop_resize_buffer)
        {
            Array.Resize(ref TextureAssets.TreeTop, treetop_resize_buffer);
        }
    }

    [OnUnload]
    [UsedImplicitly]
    private static void RevertTextureArrays()
    {
        if (VanillaTreetopCount > 0)
        {
            for (var i = VanillaTreetopCount; i < TextureAssets.TreeTop.Length; i++)
            {
                TextureAssets.TreeTop[i] = null;
            }

            Array.Resize(ref TextureAssets.TreeTop, VanillaTreetopCount);
        }

        Profiles.Clear();
    }

    [OnLoad]
    [UsedImplicitly]
    private static void Load()
    {
#region Forest Trees
        // for some reason, default forest treetop is idx 0, and the rest are 6 through 10 :\ annoying, but workable atleast

        Profiles[0] = new TreeStyleProfile(
            PaintIdx: 0,
            TopTexture: TextureAssets.TreeTop[0],
            BranchTexture: TextureAssets.TreeBranch[0],
            Variations: new TreetopVariation[]
            {
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
            }
        );

        for (var i = 6; i <= 10; i++)
        {
            Profiles[i] = new TreeStyleProfile(
                PaintIdx: i,
                TopTexture: TextureAssets.TreeTop[i],
                BranchTexture: TextureAssets.TreeBranch[i],
                Variations: new TreetopVariation[]
                {
                    new(80, 80, Vector2.Zero),
                    new(80, 80, Vector2.Zero),
                    new(80, 80, Vector2.Zero),
                }
            );
        }
#endregion

#region Vanity Trees
        // sakura
        Profiles[29] = new TreeStyleProfile(
            PaintIdx: 29,
            TopTexture: TextureAssets.TreeTop[29],
            BranchTexture: TextureAssets.TreeBranch[29],
            Variations: new TreetopVariation[]
            {
                new(118, 96, Vector2.Zero),
                new(118, 96, Vector2.Zero),
                new(118, 96, Vector2.Zero),
            }
        );

        // willow
        Profiles[30] = new TreeStyleProfile(
            PaintIdx: 30,
            TopTexture: TextureAssets.TreeTop[30],
            BranchTexture: TextureAssets.TreeBranch[30],
            Variations: new TreetopVariation[]
            {
                new(118, 96, Vector2.Zero),
                new(118, 96, Vector2.Zero),
                new(118, 96, Vector2.Zero),
            }
        );
#endregion

#region Tundra Trees
        for (var i = 16; i <= 18; i++)
        {
            Profiles[i] = new TreeStyleProfile(
                PaintIdx: i,
                TopTexture: TextureAssets.TreeTop[i],
                BranchTexture: TextureAssets.TreeBranch[i],
                Variations: new TreetopVariation[]
                {
                    new(80, 80, Vector2.Zero),
                    new(80, 80, Vector2.Zero),
                    new(80, 80, Vector2.Zero),
                }
            );
        }
#endregion

#region Jungle Trees
        Profiles[2] = new TreeStyleProfile(
            PaintIdx: 2,
            TopTexture: TextureAssets.TreeTop[2],
            BranchTexture: TextureAssets.TreeBranch[2],
            Variations: new TreetopVariation[]
            {
                new(114, 96, Vector2.Zero),
                new(114, 96, Vector2.Zero),
                new(114, 96, Vector2.Zero),
            }
        );

        Profiles[11] = new TreeStyleProfile(
            PaintIdx: 11,
            TopTexture: TextureAssets.TreeTop[11],
            BranchTexture: TextureAssets.TreeBranch[11],
            Variations: new TreetopVariation[]
            {
                new(116, 96, Vector2.Zero),
                new(116, 96, Vector2.Zero),
                new(116, 96, Vector2.Zero),
            }
        );

        Profiles[13] = new TreeStyleProfile(
            PaintIdx: 13,
            TopTexture: TextureAssets.TreeTop[13],
            BranchTexture: TextureAssets.TreeBranch[13],
            Variations: new TreetopVariation[]
            {
                new(116, 96, Vector2.Zero),
                new(116, 96, Vector2.Zero),
                new(116, 96, Vector2.Zero),
            }
        );
#endregion

#region Misc
        // corrupt tree
        Profiles[1] = new TreeStyleProfile(
            PaintIdx: 1,
            TopTexture: TextureAssets.TreeTop[1],
            BranchTexture: TextureAssets.TreeBranch[1],
            Variations: new TreetopVariation[]
            {
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
            }
        );

        // crimson tree
        Profiles[5] = new TreeStyleProfile(
            PaintIdx: 5,
            TopTexture: TextureAssets.TreeTop[5],
            BranchTexture: TextureAssets.TreeBranch[5],
            Variations: new TreetopVariation[]
            {
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
            }
        );

        // giant mushroom
        Profiles[14] = new TreeStyleProfile(
            PaintIdx: 14,
            TopTexture: TextureAssets.TreeTop[14],
            BranchTexture: TextureAssets.TreeBranch[14],
            Variations: new TreetopVariation[]
            {
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
                new(80, 80, Vector2.Zero),
            }
        );
#endregion
    }

    public static bool TryGetProfile(int style, out TreeStyleProfile profile)
    {
        return Profiles.TryGetValue(style, out profile);
    }
}
