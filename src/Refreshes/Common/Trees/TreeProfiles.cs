using System;
using System.Collections.Generic;
using System.Linq;
using Daybreak.Common.Features.Hooks;
using ReLogic.Content;
using Terraria.GameContent;

namespace Refreshes.Common;

/// <summary>
///     A framing variation for tree top rendering.
/// </summary>
/// <param name="Width">The width of this frame.</param>
/// <param name="Height">The height of this frame.</param>
/// <param name="OriginOffset">
///     The origin offset to apply to the position.
/// </param>
public record struct TreetopVariation(
    int Width,
    int Height,
    Vector2 OriginOffset = default
);

/// <summary>
///     A basic profile describing how to render a tree.
/// </summary>
/// <param name="TreeTopIdx">
///     The style index, as used in
///     <see cref="TilePaintSystemV2.TryGetTreeTopAndRequestIfNotReady" />.
/// </param>
/// <param name="TopTexture">The texture to use for the tree top.</param>
/// <param name="BranchTexture">
///     The texture to use for the tree branches.
/// </param>
/// <param name="Variations">Any framing variations.</param>
public readonly record struct TreeStyleProfile(
    int TreeTopIdx,
    Asset<Texture2D> TopTexture,
    Asset<Texture2D> BranchTexture,
    TreetopVariation[] Variations,
    float WindScale = 1f,
    Vector2 LeafOffset = default
)
{
    /// <summary>
    ///     Whether any variations are explicitly stated.
    ///     <br />
    ///     This will be <see langword="false" /> if <see cref="Variations" /> has
    ///     no items, which is the default case for an empty profile.
    /// </summary>
    public bool HasVariations => Variations.Length > 0;

    /// <summary>
    ///     Gets the variation for the frame.
    /// </summary>
    public TreetopVariation GetVariation(int frame)
    {
        return Variations[frame % Variations.Length];
    }

    /// <summary>
    ///     Gets the tree top texture, requesting the painted variant if
    ///     specified.
    /// </summary>
    public Texture2D GetTop(int paintColor)
    {
        if (paintColor == 0)
        {
            return TopTexture.Value;
        }

        return Main.instance.TilePaintSystem.TryGetTreeTopAndRequestIfNotReady(TreeTopIdx, 0, paintColor) ?? TopTexture.Value;
    }

    /// <summary>
    ///     Gets the tree branch texture, requesting the painted variant if
    ///     specified.
    /// </summary>
    public Texture2D GetBranch(int paintColor)
    {
        if (paintColor == 0)
        {
            return BranchTexture.Value;
        }

        return Main.instance.TilePaintSystem.TryGetTreeBranchAndRequestIfNotReady(TreeTopIdx, 0, paintColor) ?? BranchTexture.Value;
    }
}

public static class TreeProfiles
{
    /// <summary> to ensure theres enough room for vanilla + our variants while hopefully being safe for modtree </summary>
    private const int big_style_offset = 40;

    private static readonly List<TreeStyleProfile> profiles = Enumerable.Repeat(default(TreeStyleProfile), (int)VanillaTreeStyle.Count).ToList();
    private static readonly Dictionary<int, TreeStyleProfile> alt_profiles = new();

    private static Asset<Texture2D>[]? oldTreeTops;

    public static TreeStyleProfile GetTreeProfile(int style)
    {
        return profiles[style];
    }

    public static bool TryGetAlternative(int style, out TreeStyleProfile altProfile)
    {
        return alt_profiles.TryGetValue(style, out altProfile);
    }

    [ModSystemHooks.ResizeArrays]
    private static void ResizeTreeTops()
    {
        oldTreeTops ??= TextureAssets.TreeTop;
        Array.Resize(ref TextureAssets.TreeTop, big_style_offset + (int)VanillaTreeStyle.Count);
    }

    [OnUnload]
    private static void RestoreVanillaTreeTops()
    {
        profiles.Clear();
        alt_profiles.Clear();

        if (oldTreeTops is not null)
        {
            TextureAssets.TreeTop = oldTreeTops;
        }
    }

    /// <summary>
    ///     Registers an alternate variant of an existing treetop style.
    /// </summary>
    /// <param name="style"></param>
    /// <param name="tex"></param>
    /// <param name="branches"></param>
    /// <param name="variations"></param>
    private static void RegisterAltProfile(int style, Asset<Texture2D> tex, Asset<Texture2D> branches, TreetopVariation[] variations, Vector2 leafOffset = default)
    {
        var paintIdx = big_style_offset + style;

        if (paintIdx >= TextureAssets.TreeTop.Length)
        {
            ResizeTreeTops();
        }

        TextureAssets.TreeTop[paintIdx] = tex;

        alt_profiles[style] = new TreeStyleProfile(
            TreeTopIdx: paintIdx,
            TopTexture: tex,
            BranchTexture: branches,
            Variations: variations,
            LeafOffset: leafOffset
        );
    }

    [OnLoad]
    private static void LoadDefaultProfiles()
    {
        // Load in default data for vanilla tree styles so we always have every
        // vanilla profile filled in, even if we haven't set any variations for
        // them yet.
        for (var i = 0; i < (int)VanillaTreeStyle.Count; i++)
        {
            profiles[i] = new TreeStyleProfile(
                TreeTopIdx: i,
                TopTexture: TextureAssets.TreeTop[i],
                BranchTexture: TextureAssets.TreeBranch[i],
                Variations: []
            );
        }

        foreach (var forest in VanillaTreeStyles.Forest)
        {
            var style = (int)forest;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                ]
            );
        }

        foreach (var vanity in VanillaTreeStyles.Vanity)
        {
            var style = (int)vanity;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(118, 96, Vector2.Zero),
                    new TreetopVariation(118, 96, Vector2.Zero),
                    new TreetopVariation(118, 96, Vector2.Zero),
                ]
            );
        }

        foreach (var boreal in new[] { VanillaTreeStyle.Boreal3, VanillaTreeStyle.Boreal4, VanillaTreeStyle.Boreal5 })
        {
            var style = (int)boreal;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                ]
            );
        }

        foreach (var jungle in new[] { VanillaTreeStyle.Jungle1 })
        {
            var style = (int)jungle;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(114, 96, Vector2.Zero),
                    new TreetopVariation(114, 96, Vector2.Zero),
                    new TreetopVariation(114, 96, Vector2.Zero),
                ]
            );
        }

        foreach (var jungle in new[] { VanillaTreeStyle.Jungle2, VanillaTreeStyle.Jungle3 })
        {
            var style = (int)jungle;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(116, 96, Vector2.Zero),
                    new TreetopVariation(116, 96, Vector2.Zero),
                    new TreetopVariation(116, 96, Vector2.Zero),
                ]
            );
        }

        foreach (var corruption in VanillaTreeStyles.Corruption)
        {
            var style = (int)corruption;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                ]
            );
        }

        foreach (var crimson in VanillaTreeStyles.Crimson)
        {
            var style = (int)crimson;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                ]
            );
        }

        foreach (var glowingMushroom in VanillaTreeStyles.GlowingMushroom)
        {
            var style = (int)glowingMushroom;

            profiles[style] = new TreeStyleProfile(
                TreeTopIdx: style,
                TopTexture: TextureAssets.TreeTop[style],
                BranchTexture: TextureAssets.TreeBranch[style],
                Variations:
                [
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                    new TreetopVariation(80, 80, Vector2.Zero),
                ]
            );
        }

        RegisterAltProfile(
            style: (int)VanillaTreeStyle.Forest1,
            tex: Assets.Images.Content.Trees.Tree_Tops_0.Asset,
            branches: TextureAssets.TreeBranch[0],
            variations:
            [
                new TreetopVariation(216, 190, new Vector2(2, -4)),
            ],
            leafOffset: new Vector2(40, -70)
        );
    }
}
