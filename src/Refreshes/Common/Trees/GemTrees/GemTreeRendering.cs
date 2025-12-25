using Daybreak.Common.Features.Hooks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace Refreshes.Common;

/// <summary>
///     Contextualizes a gem tree rendering request.
/// </summary>
/// <param name="Renderer">
///     The <see cref="GemTreeRenderer" /> instance responsible for rendering the
///     tile.
/// </param>
/// <param name="CurrentBiome">The ID of the current biome.</param>
public readonly record struct RendererContext(
    GemTreeRenderer Renderer,
    int CurrentBiome
);

/// <summary>
///     Handles reworked gem tree rendering.  Features include gem glowmasks
///     which get disproportionately affected by light as well as unique stone
///     trunk textures for non-purity stone.
/// </summary>
public static class GemTreeRendering
{
    /// <summary>
    ///     ID sets pertaining to the rendering of gem trees.
    /// </summary>
    public static class Sets
    {
        private static readonly object[] default_renderers =
        [
            TileID.TreeTopaz, new TreeRenderer(),
            TileID.TreeAmethyst, new TreeRenderer(),
            TileID.TreeSapphire, new TreeRenderer(),
            TileID.TreeEmerald, new TreeRenderer(),
            TileID.TreeRuby, new TreeRenderer(),
            TileID.TreeDiamond, new TreeRenderer(),
            TileID.TreeAmber, new TreeRenderer(),
            TileID.GemSaplings, new SaplingRenderer(),
        ];

        /// <summary>
        ///     The given <see cref="GemTreeRenderer" /> to use for a tile ID.
        /// </summary>
        public static GemTreeRenderer?[] GemTreeRenderers { get; private set; } = [];

        [ModSystemHooks.ResizeArrays]
        private static void ResizeArrays()
        {
            GemTreeRenderers = TileID.Sets.Factory.CreateNamedSet(ModContent.GetInstance<ModImpl>(), nameof(GemTreeRenderers))
                                     .Description("Renderers for gem trees")
                                     .RegisterCustomSet(default(GemTreeRenderer), default_renderers);
        }
    }

    // TODO: Make real configs for these?

    /// <summary>
    ///     Whether the broad implementation is enabled, allowing for glowing
    ///     gems and per-biome textures.
    /// </summary>
    public static bool Enabled => true /* someCondition */;

    /// <summary>
    ///     Whether gems should be rendered as glowmasks with disproportionate
    ///     lighting.
    /// </summary>
    public static bool GlowingGemsEnabled => Enabled /* && someCondition */;

    /// <summary>
    ///     Whether gem trunks should be rendered with unique textures depending
    ///     on the stone they grow on.
    /// </summary>
    public static bool BiomeVariantsEnabled => Enabled /* && someCondition */;

    /// <summary>
    ///     Whether biome variant-gem trees should drop stone variants
    ///     respective to their biome.
    /// </summary>
    public static bool BiomeVariantDropsEnabled => BiomeVariantsEnabled /* && synced && someCondition */;

    internal static RendererContext? RenderCtx { get; set; }

    [OnLoad]
    private static void ApplyHooks()
    {
        On_TileDrawing.DrawSingleTile += DrawSingleTile_DrawGemProfileVariants;
        On_TileDrawing.GetTileDrawData += GetTileDrawData_OverrideWithGemProfileVariants;
        On_TileDrawing.GetTileDrawTexture_Tile_int_int += GetTileDrawTexture_Tile_int_int_OverrideWithGemProfileVariants;
        On_TileDrawing.GetTileDrawTexture_Tile_int_int_int += GetTileDrawTexture_Tile_int_int_int_OverrideWithGemProfileVariants;
        On_TileDrawing.DrawAnimatedTile_AdjustForVisionChangers += DrawAnimatedTile_AdjustForVisionChangers_ClearRenderContext;
        On_WorldGen.KillTile_GetItemDrops += KillTile_GetItemDrops_ChangeStoneTypeForGemTrees;
    }

    private static void DrawSingleTile_DrawGemProfileVariants(
        On_TileDrawing.orig_DrawSingleTile orig,
        TileDrawing self,
        TileDrawInfo drawData,
        bool solidLayer,
        int waterStyleOverride,
        Vector2 screenPosition,
        Vector2 screenOffset,
        int tileX,
        int tileY
    )
    {
        if (!BiomeVariantsEnabled || Sets.GemTreeRenderers[Main.tile[tileX, tileY].TileType] is not { } renderer)
        {
            orig(self, drawData, solidLayer, waterStyleOverride, screenPosition, screenOffset, tileX, tileY);
            return;
        }

        try
        {
            RenderCtx = renderer.GetContext(tileX, tileY);
            orig(self, drawData, solidLayer, waterStyleOverride, screenPosition, screenOffset, tileX, tileY);
        }
        finally
        {
            RenderCtx = null;
        }
    }

    private static Texture2D GetTileDrawTexture_Tile_int_int_OverrideWithGemProfileVariants(
        On_TileDrawing.orig_GetTileDrawTexture_Tile_int_int orig,
        TileDrawing self,
        Tile tile,
        int tileX,
        int tileY
    )
    {
        if (!BiomeVariantsEnabled)
        {
            return orig(self, tile, tileX, tileY);
        }

        // Special case for tiles drawn in DrawGrass.
        if (!RenderCtx.HasValue)
        {
            if (Sets.GemTreeRenderers[tile.TileType] is { } renderer)
            {
                RenderCtx = renderer.GetContext(tileX, tileY);
            }
            else
            {
                return orig(self, tile, tileX, tileY);
            }
        }

        var profile = GemTreeVanityProfiles.GetProfile(RenderCtx.Value.Renderer.GetTreeTileType(tile));
        if (!profile.HasValue)
        {
            return orig(self, tile, tileX, tileY);
        }

        return RenderCtx.Value.Renderer.GetTileDrawTexture(RenderCtx.Value, profile.Value).Value;
    }

    private static Texture2D GetTileDrawTexture_Tile_int_int_int_OverrideWithGemProfileVariants(
        On_TileDrawing.orig_GetTileDrawTexture_Tile_int_int_int orig,
        TileDrawing self,
        Tile tile,
        int tileX,
        int tileY,
        int paintOverride
    )
    {
        if (!BiomeVariantsEnabled || !RenderCtx.HasValue)
        {
            return orig(self, tile, tileX, tileY, paintOverride);
        }

        var profile = GemTreeVanityProfiles.GetProfile(RenderCtx.Value.Renderer.GetTreeTileType(tile));
        if (!profile.HasValue)
        {
            return orig(self, tile, tileX, tileY, paintOverride);
        }

        return RenderCtx.Value.Renderer.GetTileDrawTexture(RenderCtx.Value, profile.Value).Value;
    }

    private static void DrawAnimatedTile_AdjustForVisionChangers_ClearRenderContext(
        On_TileDrawing.orig_DrawAnimatedTile_AdjustForVisionChangers orig,
        TileDrawing self,
        int i,
        int j,
        Tile tileCache,
        ushort typeCache,
        short tileFrameX,
        short tileFrameY,
        ref Color tileLight,
        bool canDoDust
    )
    {
        orig(self, i, j, tileCache, typeCache, tileFrameX, tileFrameY, ref tileLight, canDoDust);

        // Clear the context always to support saplings.
        RenderCtx = null;
    }

    private static void GetTileDrawData_OverrideWithGemProfileVariants(
        On_TileDrawing.orig_GetTileDrawData orig,
        TileDrawing self,
        int x,
        int y,
        Tile tileCache,
        ushort typeCache,
        ref short tileFrameX,
        ref short tileFrameY,
        out int tileWidth,
        out int tileHeight,
        out int tileTop,
        out int halfBrickHeight,
        out int addFrX,
        out int addFrY,
        out SpriteEffects tileSpriteEffect,
        out Texture2D glowTexture,
        out Rectangle glowSourceRect,
        out Color glowColor
    )
    {
        orig(
            self,
            x,
            y,
            tileCache,
            typeCache,
            ref tileFrameX,
            ref tileFrameY,
            out tileWidth,
            out tileHeight,
            out tileTop,
            out halfBrickHeight,
            out addFrX,
            out addFrY,
            out tileSpriteEffect,
            out glowTexture,
            out glowSourceRect,
            out glowColor
        );

        if (!BiomeVariantsEnabled || !RenderCtx.HasValue)
        {
            return;
        }

        var profile = GemTreeVanityProfiles.GetProfile(RenderCtx.Value.Renderer.GetTreeTileType(tileCache));
        if (!profile.HasValue)
        {
            return;
        }

        if (GlowingGemsEnabled)
        {
            glowColor = Lighting.GetColor(x, y) * 2f;
        }

        RenderCtx.Value.Renderer.GetGlowData(RenderCtx.Value, profile.Value, tileCache, tileWidth, tileHeight, out glowTexture, out glowSourceRect);
    }

    public static int GetBiomeFromTile(Tile tile)
    {
        return tile.TileType switch
        {
            TileID.Ebonstone => BiomeConversionID.Corruption,
            TileID.Crimstone => BiomeConversionID.Crimson,
            TileID.Pearlstone => BiomeConversionID.Hallow,
            _ => BiomeConversionID.Purity,
        };
    }

    private static void KillTile_GetItemDrops_ChangeStoneTypeForGemTrees(
        On_WorldGen.orig_KillTile_GetItemDrops orig,
        int x,
        int y,
        Tile tileCache,
        out int dropItem,
        out int dropItemStack,
        out int secondaryItem,
        out int secondaryItemStack,
        bool includeLargeObjectDrops
    )
    {
        orig(x, y, tileCache, out dropItem, out dropItemStack, out secondaryItem, out secondaryItemStack, includeLargeObjectDrops);

        if (!BiomeVariantDropsEnabled || Sets.GemTreeRenderers[tileCache.TileType] is not { } renderer)
        {
            return;
        }

        var ctx = renderer.GetContext(x, y);
        if (ctx.CurrentBiome == -1 || ctx.CurrentBiome == BiomeConversionID.Purity)
        {
            return;
        }

        if (dropItem == ItemID.StoneBlock)
        {
            dropItem = ctx.CurrentBiome switch
            {
                BiomeConversionID.Corruption => ItemID.EbonstoneBlock,
                BiomeConversionID.Crimson => ItemID.CrimstoneBlock,
                BiomeConversionID.Hallow => ItemID.PearlstoneBlock,
                _ => dropItem,
            };
        }
    }

    [GlobalTileHooks.CreateDust]
    private static void CreateDust_ChangeDustTypeForGemTrees(int i, int j, int type, ref int dustType)
    {
        if (!BiomeVariantsEnabled || Sets.GemTreeRenderers[Main.tile[i, j].TileType] is not { } renderer)
        {
            return;
        }

        var ctx = renderer.GetContext(i, j);
        if (ctx.CurrentBiome == -1 || ctx.CurrentBiome == BiomeConversionID.Purity)
        {
            return;
        }

        if (dustType == DustID.Stone)
        {
            dustType = ctx.CurrentBiome switch
            {
                BiomeConversionID.Corruption => Main.rand.NextBool() ? DustID.Stone : DustID.Corruption,
                BiomeConversionID.Crimson => DustID.Crimstone,
                BiomeConversionID.Hallow => DustID.Stone,
                _ => dustType,
            };
        }
    }
}
