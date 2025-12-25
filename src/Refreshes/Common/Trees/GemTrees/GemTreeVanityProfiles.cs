using System.Collections.Generic;
using ReLogic.Content;
using Terraria.ID;

namespace Refreshes.Common;

/// <summary>
///     Describes the assets used for a single gem tree.  This tree is derived
///     from the gem type as well as the biome type.
/// </summary>
/// <param name="Trunk">The unique tree trunk texture.</param>
/// <param name="TrunkGems">
///     The gem texture to pair with the tree trunk.
///     <br />
///     If <see langword="null" />, the default gem textures will be used.
/// </param>
/// <param name="Branches">The unique tree branches texture.</param>
/// <param name="BranchesGems">
///     The gem texture to pair with the tree branches.
///     <br />
///     If <see langword="null" />, the default gem textures will be used.
/// </param>
/// <param name="Tops">The unique tree top texture.</param>
/// <param name="TopsGems">
///     The gem texture to pair with the tree tops.
///     <br />
///     If <see langword="null" />, the default gem textures will be used.
/// </param>
public readonly record struct GemTreeVanityDescription(
    Asset<Texture2D> Trunk,
    Asset<Texture2D>? TrunkGems,
    Asset<Texture2D> Branches,
    Asset<Texture2D>? BranchesGems,
    Asset<Texture2D> Tops,
    Asset<Texture2D>? TopsGems
);

/// <summary>
///     Describes the assets used for a single gem tree sapling.  This tree is
///     derived from the gem type as well ass the biome type.
/// </summary>
/// <param name="Sapling">The unique tree sapling texture.</param>
/// <param name="SaplingGems">
///     The gem texture to pair with the tree sapling.
///     <br />
///     If <see langword="null" />, the default gem textures will be used.
/// </param>
public readonly record struct GemTreeVanitySaplingDescription(
    Asset<Texture2D> Sapling,
    Asset<Texture2D>? SaplingGems
);

/// <summary>
///     A gem-variant profile that describes how to render its biome variants.
/// </summary>
/// <param name="Purity">
///     The vanity description to use when there is no biome override (a.k.a.
///     the default; purity).
/// </param>
/// <param name="PuritySaplings">
///     The vanity description to use when there is no biome override (a.k.a.
///     the default; purity), for saplings.
/// </param>
/// <param name="Biomes">
///     The map of biome IDs -> gem tree descriptions for this gem variant.
/// </param>
/// <param name="SaplingBiomes">
///     The map of biome IDs -> gem sapling descriptions for this gem variant.
/// </param>
public readonly record struct GemTreeVanityProfile(
    GemTreeVanityDescription Purity,
    GemTreeVanitySaplingDescription PuritySaplings,
    Dictionary<int, GemTreeVanityDescription> Biomes,
    Dictionary<int, GemTreeVanitySaplingDescription> SaplingBiomes
)
{
    /// <summary>
    ///     Gets the most applicable description for this vanity profile,
    ///     returning the <see cref="Purity" /> description if no suitable
    ///     profile is found for the biome.
    /// </summary>
    public GemTreeVanityDescription GetDescription(int biomeId)
    {
        if (biomeId == BiomeConversionID.Purity || biomeId == -1)
        {
            return Purity;
        }

        return Biomes.GetValueOrDefault(biomeId, Purity);
    }

    /// <summary>
    ///     Gets the most applicable sapling description for this vanity
    ///     profile, returning the <see cref="PuritySaplings" /> description if
    ///     no suitable profile is found for the biome.
    /// </summary>
    public GemTreeVanitySaplingDescription GetSaplingDescription(int biomeId)
    {
        if (biomeId == BiomeConversionID.Purity || biomeId == -1)
        {
            return PuritySaplings;
        }

        return SaplingBiomes.GetValueOrDefault(biomeId, PuritySaplings);
    }
}

/// <summary>
///     Contains profile definitions for the default, vanilla gem trees.
/// </summary>
public static class GemTreeVanityProfiles
{
    /// <summary>
    ///     The Ruby gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Ruby { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Ruby_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Diamond gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Diamond { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Diamond_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Topaz gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Topaz { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Topaz_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Amethyst gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Amethyst { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amethyst_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Sapphire gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Sapphire { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Sapphire_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Emerald gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Emerald { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Emerald_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     The Amber gem tree profile.
    /// </summary>
    public static GemTreeVanityProfile Amber { get; } = new(
        Purity: new GemTreeVanityDescription(
            Trunk: Assets.Images.Content.GemTrees.PurityGemTree_Stone.Asset,
            TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber.Asset,
            Branches: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Branches.Asset,
            BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Branches.Asset,
            Tops: Assets.Images.Content.GemTrees.PurityGemTree_Stone_TreeTops.Asset,
            TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Tops.Asset
        ),
        PuritySaplings: new GemTreeVanitySaplingDescription(
            Sapling: Assets.Images.Content.GemTrees.PurityGemTree_Stone_Saplings.Asset,
            SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
        ),
        Biomes: new Dictionary<int, GemTreeVanityDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Trunk: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber.Asset,
                Branches: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Tops.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Trunk: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber.Asset,
                Branches: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Tops.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Trunk: Assets.Images.Content.GemTrees.HallowGemTree_Stone.Asset,
                TrunkGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber.Asset,
                Branches: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Branches.Asset,
                BranchesGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Branches.Asset,
                Tops: Assets.Images.Content.GemTrees.HallowGemTree_Stone_TreeTops.Asset,
                TopsGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Amber_Tops.Asset
            ),
        },
        SaplingBiomes: new Dictionary<int, GemTreeVanitySaplingDescription>
        {
            [BiomeConversionID.Corruption] = new(
                Sapling: Assets.Images.Content.GemTrees.CorruptionGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Crimson] = new(
                Sapling: Assets.Images.Content.GemTrees.CrimsonGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
            [BiomeConversionID.Hallow] = new(
                Sapling: Assets.Images.Content.GemTrees.HallowGemTree_Stone_Saplings.Asset,
                SaplingGems: Assets.Images.Content.GemTrees.Engraved.PurityGemTree_Saplings.Asset
            ),
        }
    );

    /// <summary>
    ///     Gets the vanilla gem tree profile corresponding to the tree
    ///     <paramref cref="tileId" />.
    /// </summary>
    /// <param name="tileId">The tree tile ID to get the profile for.</param>
    /// <returns>
    ///     The gem tree profile, assuming one exists for
    ///     <paramref name="tileId" />; otherwise, <see langword="null" />.
    /// </returns>
    public static GemTreeVanityProfile? GetProfile(int tileId)
    {
        return tileId switch
        {
            TileID.TreeRuby => Ruby,
            TileID.TreeDiamond => Diamond,
            TileID.TreeTopaz => Topaz,
            TileID.TreeAmethyst => Amethyst,
            TileID.TreeSapphire => Sapphire,
            TileID.TreeEmerald => Emerald,
            TileID.TreeAmber => Amber,
            _ => null,
        };
    }
}
