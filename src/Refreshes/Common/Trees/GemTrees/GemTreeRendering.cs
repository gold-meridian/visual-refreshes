using System;
using Daybreak.Common.Features.Hooks;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace Refreshes.Common;

// TODO: Re-add paint support later?

/// <summary>
///     Contextualizes a gem tree rendering request.
/// </summary>
/// <param name="Renderer">
///     The <see cref="GemTreeRenderer"/> instance responsible for rendering the
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
        ///     The given <see cref="GemTreeRenderer"/> to use for a tile ID.
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

    private static void DrawTrees(
        On_TileDrawing.orig_DrawTrees orig,
        TileDrawing self
    )
    {
        var unscaledPosition = Main.Camera.UnscaledPosition;
        var zero = Vector2.Zero;
        var num = 0;
        var num2 = self._specialsCount[num];
        var num3 = 0.08f;
        var num4 = 0.06f;
        for (var i = 0; i < num2; i++)
        {
            var point = self._specialPositions[num][i];
            var x = point.X;
            var y = point.Y;
            var tile = Main.tile[x, y];
            if (tile == null || !tile.active())
            {
                continue;
            }

            var type = tile.type;
            var frameX = tile.frameX;
            var frameY = tile.frameY;
            var flag = tile.wall > 0;
            WorldGen.GetTreeFoliageDataMethod getTreeFoliageDataMethod = null;
            try
            {
                var flag2 = false;
                switch (type)
                {
                    case 5:
                        flag2 = true;
                        getTreeFoliageDataMethod = WorldGen.GetCommonTreeFoliageData;
                        break;

                    case 583:
                    case 584:
                    case 585:
                    case 586:
                    case 587:
                    case 588:
                    case 589:
                        flag2 = true;
                        getTreeFoliageDataMethod = WorldGen.GetGemTreeFoliageData;
                        break;

                    case 596:
                    case 616:
                        flag2 = true;
                        getTreeFoliageDataMethod = WorldGen.GetVanityTreeFoliageData;
                        break;

                    case 634:
                        flag2 = true;
                        getTreeFoliageDataMethod = WorldGen.GetAshTreeFoliageData;
                        break;
                }

                if (flag2 && frameY >= 198 && frameX >= 22)
                {
                    var treeFrame = WorldGen.GetTreeFrame(tile);
                    switch (frameX)
                    {
                        case 22:
                        {
                            var treeStyle3 = 0;
                            var topTextureFrameWidth3 = 80;
                            var topTextureFrameHeight3 = 80;
                            var num13 = 0;
                            var grassPosX = x + num13;
                            var floorY3 = y;
                            if (!getTreeFoliageDataMethod(x, y, num13, ref treeFrame, ref treeStyle3, out floorY3, out topTextureFrameWidth3, out topTextureFrameHeight3))
                            {
                                continue;
                            }

                            self.EmitTreeLeaves(x, y, grassPosX, floorY3);
                            if (treeStyle3 == 14)
                            {
                                var num14 = self._rand.Next(28, 42) * 0.005f;
                                num14 += (270 - Main.mouseTextColor) / 1000f;
                                if (tile.color() == 0)
                                {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num14 / 2f, 0.7f + num14);
                                }
                                else
                                {
                                    var color5 = WorldGen.paintColor(tile.color());
                                    var r3 = color5.R / 255f;
                                    var g3 = color5.G / 255f;
                                    var b3 = color5.B / 255f;
                                    Lighting.AddLight(x, y, r3, g3, b3);
                                }
                            }

                            var tileColor3 = tile.color();
                            var treeTopTexture = self.GetTreeTopTexture(treeStyle3, 0, tileColor3);
                            Vector2 vector = vector = new Vector2(x * 16 - (int)unscaledPosition.X + 8, y * 16 - (int)unscaledPosition.Y + 16) + zero;
                            var num15 = 0f;
                            if (!flag)
                            {
                                num15 = self.GetWindCycle(x, y, self._treeWindCounter);
                            }

                            vector.X += num15 * 2f;
                            vector.Y += Math.Abs(num15) * 2f;
                            var color6 = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                color6 = Color.White;
                            }
                            
                            if (type == 634)
                            {
                                var value3 = TextureAssets.GlowMask[316].Value;
                                var white3 = Color.White;
                                Main.spriteBatch.Draw(value3, vector, new Rectangle(treeFrame * (topTextureFrameWidth3 + 2), 0, topTextureFrameWidth3, topTextureFrameHeight3), white3, num15 * num3, new Vector2(topTextureFrameWidth3 / 2, topTextureFrameHeight3), 1f, SpriteEffects.None, 0f);
                            }

                            break;
                        }

                        case 44:
                        {
                            var treeStyle2 = 0;
                            var num9 = x;
                            var floorY2 = y;
                            var num10 = 1;
                            if (!getTreeFoliageDataMethod(x, y, num10, ref treeFrame, ref treeStyle2, out floorY2, out _, out _))
                            {
                                continue;
                            }

                            self.EmitTreeLeaves(x, y, num9 + num10, floorY2);
                            if (treeStyle2 == 14)
                            {
                                var num11 = self._rand.Next(28, 42) * 0.005f;
                                num11 += (270 - Main.mouseTextColor) / 1000f;
                                if (tile.color() == 0)
                                {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num11 / 2f, 0.7f + num11);
                                }
                                else
                                {
                                    var color3 = WorldGen.paintColor(tile.color());
                                    var r2 = color3.R / 255f;
                                    var g2 = color3.G / 255f;
                                    var b2 = color3.B / 255f;
                                    Lighting.AddLight(x, y, r2, g2, b2);
                                }
                            }

                            var tileColor2 = tile.color();
                            var treeBranchTexture2 = self.GetTreeBranchTexture(treeStyle2, 0, tileColor2);
                            var position2 = new Vector2(x * 16, y * 16) - unscaledPosition.Floor() + zero + new Vector2(16f, 12f);
                            var num12 = 0f;
                            if (!flag)
                            {
                                num12 = self.GetWindCycle(x, y, self._treeWindCounter);
                            }

                            if (num12 > 0f)
                            {
                                position2.X += num12;
                            }

                            position2.X += Math.Abs(num12) * 2f;
                            var color4 = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                color4 = Color.White;
                            }

                            var profile = GemTreeVanityProfiles.GetProfile(tile.TileType);
                            if (profile.HasValue && RenderCtx.HasValue)
                            {
                                treeBranchTexture2 = profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).Branches.Value;
                            }

                            Main.spriteBatch.Draw(treeBranchTexture2, position2, new Rectangle(0, treeFrame * 42, 40, 40), color4, num12 * num4, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);

                            if (profile.HasValue && RenderCtx.HasValue)
                            {
                                Main.spriteBatch.Draw((profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).BranchesGems ?? profile.Value.Purity.BranchesGems!).Value, position2, new Rectangle(0, treeFrame * 42, 40, 40), color4 * 2f, num12 * num4, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);
                            }

                            if (type == 634)
                            {
                                var value2 = TextureAssets.GlowMask[317].Value;
                                var white2 = Color.White;
                                Main.spriteBatch.Draw(value2, position2, new Rectangle(0, treeFrame * 42, 40, 40), white2, num12 * num4, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);
                            }

                            break;
                        }

                        case 66:
                        {
                            var treeStyle = 0;
                            var num5 = x;
                            var floorY = y;
                            var num6 = -1;
                            if (!getTreeFoliageDataMethod(x, y, num6, ref treeFrame, ref treeStyle, out floorY, out _, out _))
                            {
                                continue;
                            }

                            self.EmitTreeLeaves(x, y, num5 + num6, floorY);
                            if (treeStyle == 14)
                            {
                                var num7 = self._rand.Next(28, 42) * 0.005f;
                                num7 += (270 - Main.mouseTextColor) / 1000f;
                                if (tile.color() == 0)
                                {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num7 / 2f, 0.7f + num7);
                                }
                                else
                                {
                                    var color = WorldGen.paintColor(tile.color());
                                    var r = color.R / 255f;
                                    var g = color.G / 255f;
                                    var b = color.B / 255f;
                                    Lighting.AddLight(x, y, r, g, b);
                                }
                            }

                            var tileColor = tile.color();
                            var treeBranchTexture = self.GetTreeBranchTexture(treeStyle, 0, tileColor);
                            var position = new Vector2(x * 16, y * 16) - unscaledPosition.Floor() + zero + new Vector2(0f, 18f);
                            var num8 = 0f;
                            if (!flag)
                            {
                                num8 = self.GetWindCycle(x, y, self._treeWindCounter);
                            }

                            if (num8 < 0f)
                            {
                                position.X += num8;
                            }

                            position.X -= Math.Abs(num8) * 2f;
                            var color2 = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                color2 = Color.White;
                            }

                            var profile = GemTreeVanityProfiles.GetProfile(tile.TileType);
                            if (profile.HasValue && RenderCtx.HasValue)
                            {
                                treeBranchTexture = profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).Branches.Value;
                            }

                            Main.spriteBatch.Draw(treeBranchTexture, position, new Rectangle(42, treeFrame * 42, 40, 40), color2, num8 * num4, new Vector2(0f, 30f), 1f, SpriteEffects.None, 0f);

                            if (profile.HasValue && RenderCtx.HasValue)
                            {
                                Main.spriteBatch.Draw((profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).BranchesGems ?? profile.Value.Purity.BranchesGems!).Value, position, new Rectangle(42, treeFrame * 42, 40, 40), color2 * 2f, num8 * num4, new Vector2(0f, 30f), 1f, SpriteEffects.None, 0f);
                            }

                            if (type == 634)
                            {
                                var value = TextureAssets.GlowMask[317].Value;
                                var white = Color.White;
                                Main.spriteBatch.Draw(value, position, new Rectangle(42, treeFrame * 42, 40, 40), white, num8 * num4, new Vector2(0f, 30f), 1f, SpriteEffects.None, 0f);
                            }

                            break;
                        }
                    }
                }

                if (type == 323 && frameX >= 88 && frameX <= 132)
                {
                    var num16 = 0;
                    switch (frameX)
                    {
                        case 110:
                            num16 = 1;
                            break;

                        case 132:
                            num16 = 2;
                            break;
                    }

                    var treeTextureIndex = 15;
                    var num17 = 80;
                    var num18 = 80;
                    var num19 = 32;
                    var num20 = 0;
                    var palmTreeBiome = self.GetPalmTreeBiome(x, y);
                    var y2 = palmTreeBiome * 82;
                    if (palmTreeBiome >= 4 && palmTreeBiome <= 7)
                    {
                        treeTextureIndex = 21;
                        num17 = 114;
                        num18 = 98;
                        y2 = (palmTreeBiome - 4) * 98;
                        num19 = 48;
                        num20 = 2;
                    }

                    // Handle mod palms.
                    if (Math.Abs(palmTreeBiome) >= ModPalmTree.VanillaStyleCount)
                    {
                        y2 = 0;

                        // Oasis Tree
                        if (palmTreeBiome < 0)
                        {
                            num17 = 114;
                            num18 = 98;
                            num19 = 48;
                            num20 = 2;
                        }

                        treeTextureIndex = Math.Abs(palmTreeBiome) - ModPalmTree.VanillaStyleCount;
                        treeTextureIndex *= -2;

                        // Oasis tree
                        if (palmTreeBiome < 0)
                        {
                            treeTextureIndex -= 1;
                        }
                    }

                    int frameY2 = Main.tile[x, y].frameY;
                    var tileColor4 = tile.color();
                    var treeTopTexture2 = self.GetTreeTopTexture(treeTextureIndex, palmTreeBiome, tileColor4);
                    var position3 = new Vector2(x * 16 - (int)unscaledPosition.X - num19 + frameY2 + num17 / 2, y * 16 - (int)unscaledPosition.Y + 16 + num20) + zero;
                    var num21 = 0f;
                    if (!flag)
                    {
                        num21 = self.GetWindCycle(x, y, self._treeWindCounter);
                    }

                    position3.X += num21 * 2f;
                    position3.Y += Math.Abs(num21) * 2f;
                    var color7 = Lighting.GetColor(x, y);
                    if (tile.fullbrightBlock())
                    {
                        color7 = Color.White;
                    }

                    var profile = GemTreeVanityProfiles.GetProfile(tile.TileType);
                    if (profile.HasValue && RenderCtx.HasValue)
                    {
                        treeTopTexture2 = profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).Tops.Value;
                    }

                    Main.spriteBatch.Draw(treeTopTexture2, position3, new Rectangle(num16 * (num17 + 2), y2, num17, num18), color7, num21 * num3, new Vector2(num17 / 2, num18), 1f, SpriteEffects.None, 0f);

                    if (profile.HasValue && RenderCtx.HasValue)
                    {
                        Main.spriteBatch.Draw((profile.Value.GetDescription(RenderCtx.Value.CurrentBiome).TopsGems ?? profile.Value.Purity.TopsGems!).Value, position3, new Rectangle(num16 * (num17 + 2), y2, num17, num18), color7 * 2f, num21 * num3, new Vector2(num17 / 2, num18), 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                RenderCtx = null;
            }
        }
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
