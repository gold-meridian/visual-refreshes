using System;
using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.Utilities;

namespace Refreshes.Common;

// TODO: Re-add paint support for gem trees.

[UsedImplicitly]
internal sealed class TreeProfileRendering
{
    [OnLoad]
    [UsedImplicitly]
    private static void InjectHooks()
    {
        On_TileDrawing.DrawTrees += DrawTrees_RewriteTreeRenderingForProfiles;
        On_TileDrawing.EmitTreeLeaves += EmitTreeLeaves_Rewrite;
    }

    private static void EmitTreeLeaves_Rewrite(On_TileDrawing.orig_EmitTreeLeaves orig, TileDrawing self, int tilePosX, int tilePosY, int grassPosX, int grassPosY)
    {
        if (!self._isActiveAndNotPaused)
        {
            return;
        }

        var topTile = Main.tile[tilePosX, tilePosY];
        if (topTile.LiquidAmount > 0)
        {
            return;
        }

        var isBranch = tilePosX != grassPosX;
        var treeFrame = 0;
        var treeStyle = 0;

        WorldGen.GetCommonTreeFoliageData(tilePosX, tilePosY, isBranch ? (tilePosX - grassPosX) : 0, ref treeFrame, ref treeStyle, out var floorY, out _, out _);

        var treeProfile = TreeProfiles.GetTreeProfile(treeStyle);

        var seededRandom = new UnifiedRandom(tilePosX * 1000 + tilePosY);
        if (TreeProfiles.TryGetAlternative(treeStyle, out var altProfile) && seededRandom.NextFloat() < 0.2f)
        {
            treeProfile = altProfile;
        }

        var treeHeight = grassPosY - tilePosY;
        WorldGen.GetTreeLeaf(tilePosX, topTile, Main.tile[grassPosX, grassPosY], ref treeHeight, out _, out var passStyle);

        if (IsLeafStyleIgnored(passStyle))
        {
            return;
        }

        var isSpecialTree = passStyle is >= 917 and <= 925 or >= 1113 and <= 1121;
        float spawnChance = self._leafFrequency;

        if (isSpecialTree)
        {
            spawnChance /= 2;
        }

        if (!WorldGen.DoesWindBlowAtThisHeight(tilePosY))
        {
            spawnChance = 10000;
        }

        if (isBranch)
        {
            spawnChance *= 3;
        }

        if (self._rand.Next((int)Math.Max(1, spawnChance)) != 0)
        {
            return;
        }

        var spawnPos = new Vector2(tilePosX * 16 + 8, tilePosY * 16 + 8);

        if (isBranch)
        {
            spawnPos.X += (tilePosX - grassPosX) * 12;
            var branchSegment = topTile.frameY switch
            {
                220 => 1,
                242 => 2,
                _ => 0,
            };

            if (topTile.frameX == 66)
            {
                spawnPos += branchSegment switch
                {
                    0 or 1 => new Vector2(0, -6f),
                    2 => new Vector2(0, 8f),
                    var i => Vector2.Zero,
                };
            }
            else
            {
                spawnPos += branchSegment switch
                {
                    0 => new Vector2(0, 4f),
                    1 => new Vector2(2f, -6f),
                    2 => new Vector2(6f, -6f),
                    _ => Vector2.Zero,
                };
            }
        }
        else
        {
            spawnPos += new Vector2(-16f, -16f);
            if (isSpecialTree)
            {
                spawnPos.Y -= Main.rand.Next(0, 28) * 4;
            }
        }

        spawnPos += treeProfile.LeafOffset;

        if (WorldGen.SolidTile(spawnPos.ToTileCoordinates()))
        {
            return;
        }

        var leaf = Gore.NewGoreDirect(spawnPos, Utils.RandomVector2(Main.rand, -2f, 2f), passStyle, Main.rand.NextFloat(0.7f, 1.3f));
        leaf.Frame.CurrentColumn = topTile.color();

        return;

        static bool IsLeafStyleIgnored(int style)
        {
            return style switch
            {
                -1 or 912 or 913 or 1278 => true,
                _ => false,
            };
        }
    }

    private static void DrawTrees_RewriteTreeRenderingForProfiles(On_TileDrawing.orig_DrawTrees orig, TileDrawing self)
    {
        // The rotation values are multiplied by these for cushioning.
        const float treetop_sway_factor = 0.08f;
        const float branch_sway_factor = 0.06f;

        const int tile_counter_type = (int)TileDrawing.TileCounterType.Tree;
        var treeCount = self._specialsCount[tile_counter_type];

        var screenPosition = Main.Camera.UnscaledPosition;

        for (var i = 0; i < treeCount; i++)
        {
            var (x, y) = self._specialPositions[tile_counter_type][i];

            //todo (?)
            var seededRandom = new UnifiedRandom(x * 1000 + y);
            const float big_tree_chance = 0.2f;

            var tile = Main.tile[x, y];
            if (!tile.HasTile)
            {
                continue;
            }

            var type = tile.type;
            var frameX = tile.frameX;
            var frameY = tile.frameY;
            var hasWall = tile.wall > 0;

            GemTreeRendering.RenderCtx = GemTreeRendering.Sets.GemTreeRenderers[type] is { } renderer
                ? renderer.GetContext(x, y)
                : null;

            try
            {
                // Foliage data providers are provided for all common trees, but
                // not palm trees.
                var foliageDataProvider = type switch
                {
                    TileID.Trees => WorldGen.GetCommonTreeFoliageData,
                    TileID.TreeTopaz or TileID.TreeAmethyst or TileID.TreeSapphire or TileID.TreeEmerald or TileID.TreeRuby or TileID.TreeDiamond or TileID.TreeAmber => WorldGen.GetGemTreeFoliageData,
                    TileID.VanityTreeSakura or TileID.VanityTreeYellowWillow => WorldGen.GetVanityTreeFoliageData,
                    TileID.TreeAsh => WorldGen.GetAshTreeFoliageData,
                    _ => default(WorldGen.GetTreeFoliageDataMethod),
                };

                // Regular cases are of course handled here.  This handles
                // branch and top rendering.
                if (foliageDataProvider is not null && frameY >= 198 && frameX >= 22)
                {
                    var xOffset = frameX switch
                    {
                        22 => 0,
                        44 => 1,
                        66 => -1,
                        _ => default(int?),
                    };

                    if (!xOffset.HasValue)
                    {
                        continue;
                    }

                    var treeFrame = WorldGen.GetTreeFrame(tile);
                    var treeStyle = 0;

                    if (
                        !foliageDataProvider(
                            x,
                            y,
                            xOffset.Value,
                            ref treeFrame,
                            ref treeStyle,
                            out var floorY,
                            out var topTextureFrameWidth,
                            out var topTextureFrameHeight
                        )
                    )
                    {
                        continue;
                    }

                    var treeProfile = TreeProfiles.GetTreeProfile(treeStyle);
                    var gemProfile = GemTreeVanityProfiles.GetProfile(tile.TileType);

                    var isBig = false;

                    if (TreeProfiles.TryGetAlternative(treeStyle, out var altProfile) && seededRandom.NextFloat() < big_tree_chance)
                    {
                        treeProfile = altProfile;
                        isBig = true;
                    }

                    switch (frameX)
                    {
                        // tree top
                        case 22:
                        {
                            var grassPosX = x + xOffset.Value;

                            // TODO: Sydney should just make this configurable?
                            self.EmitTreeLeaves(x, y, grassPosX, floorY);

                            // Emit light from glowing mushroom tree tops.
                            if (treeStyle == (int)VanillaTreeStyle.GlowingMushroom1)
                            {
                                if (tile.color() == 0)
                                {
                                    var colorIntensity = self._rand.Next(28, 42) * 0.005f;
                                    {
                                        colorIntensity += (270 - Main.mouseTextColor) / 1000f;
                                    }

                                    Lighting.AddLight(x, y, 0.1f, 0.2f + colorIntensity / 2f, 0.7f + colorIntensity);
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

                            var tileColor = tile.color();
                            var topTexture = treeProfile.GetTop(tileColor);
                            var topPos = new Vector2(x * 16 - (int)screenPosition.X + 8, y * 16 - (int)screenPosition.Y + 16);

                            var windIntensity = hasWall ? 0f : self.GetWindCycle(x, y, self._treeWindCounter) * treeProfile.WindScale;
                            {
                                topPos.X += windIntensity * 2f;
                                topPos.Y += Math.Abs(windIntensity) * 2f;
                            }

                            var tileLight = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                tileLight = Color.White;
                            }

                            var variation = treeProfile.HasVariations
                                ? treeProfile.GetVariation(treeFrame)
                                : new TreetopVariation(
                                    Width: topTextureFrameWidth,
                                    Height: topTextureFrameHeight
                                );

                            var sourceRect = new Rectangle(
                                treeFrame * (variation.Width + 2),
                                0,
                                variation.Width,
                                variation.Height
                            );

                            var origin = new Vector2(
                                variation.Width / 2f,
                                variation.Height
                            );

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                topTexture = gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).Tops.Value;
                            }

                            // draw treetop
                            Main.spriteBatch.Draw(
                                topTexture,
                                topPos,
                                sourceRect,
                                tileLight,
                                windIntensity * treetop_sway_factor,
                                origin + variation.OriginOffset,
                                1f,
                                SpriteEffects.None,
                                0f
                            );

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                Main.spriteBatch.Draw(
                                    (gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).TopsGems ?? gemProfile.Value.Purity.TopsGems!).Value,
                                    topPos,
                                    sourceRect,
                                    tileLight * (GemTreeRendering.GlowingGemsEnabled ? 2f : 1f),
                                    windIntensity * treetop_sway_factor,
                                    origin + variation.OriginOffset,
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            if (type == TileID.TreeAsh)
                            {
                                var ashTopGlow = TextureAssets.GlowMask[GlowMaskID.TreeAshTop].Value;

                                Main.spriteBatch.Draw(
                                    ashTopGlow,
                                    topPos,
                                    new Rectangle(
                                        treeFrame * (topTextureFrameWidth + 2),
                                        0,
                                        topTextureFrameWidth,
                                        topTextureFrameHeight
                                    ),
                                    Color.White,
                                    windIntensity * treetop_sway_factor,
                                    new Vector2(topTextureFrameWidth / 2f, topTextureFrameHeight),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            break;
                        }

                        // left branch
                        case 44:
                        {
                            const int x_offset = 1;

                            // TODO: Sydney should just make this configurable?
                            self.EmitTreeLeaves(x, y, x + x_offset, floorY);

                            // Emit light from glowing mushroom tree tops.
                            if (treeStyle == (int)VanillaTreeStyle.GlowingMushroom1)
                            {
                                if (tile.color() == 0)
                                {
                                    var colorIntensity = self._rand.Next(28, 42) * 0.005f;
                                    {
                                        colorIntensity += (270 - Main.mouseTextColor) / 1000f;
                                    }

                                    Lighting.AddLight(x, y, 0.1f, 0.2f + colorIntensity / 2f, 0.7f + colorIntensity);
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

                            var tileColor = tile.color();
                            var branchTexture = treeProfile.GetBranch(tileColor);
                            var branchPos = new Vector2(x * 16, y * 16) - screenPosition.Floor() + new Vector2(16f, 12f);

                            var windIntensity = hasWall ? 0f : self.GetWindCycle(x, y, self._treeWindCounter);
                            {
                                if (windIntensity > 0f)
                                {
                                    branchPos.X += windIntensity;
                                }

                                branchPos.X += Math.Abs(windIntensity) * 2f;
                            }

                            var tileLight = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                tileLight = Color.White;
                            }

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                branchTexture = gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).Branches.Value;
                            }

                            Main.spriteBatch.Draw(
                                branchTexture,
                                branchPos,
                                new Rectangle(0, treeFrame * 42, 40, 40),
                                tileLight,
                                windIntensity * branch_sway_factor,
                                new Vector2(40f, 24f),
                                1f,
                                SpriteEffects.None,
                                0f
                            );

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                Main.spriteBatch.Draw(
                                    (gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).BranchesGems ?? gemProfile.Value.Purity.BranchesGems!).Value,
                                    branchPos,
                                    new Rectangle(0, treeFrame * 42, 40, 40),
                                    tileLight * (GemTreeRendering.GlowingGemsEnabled ? 2f : 1f),
                                    windIntensity * branch_sway_factor,
                                    new Vector2(40f, 24f),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            if (type == TileID.TreeAsh)
                            {
                                var ashBranchGlow = TextureAssets.GlowMask[GlowMaskID.TreeAshBranches].Value;

                                Main.spriteBatch.Draw(
                                    ashBranchGlow,
                                    branchPos,
                                    new Rectangle(0, treeFrame * 42, 40, 40),
                                    Color.White,
                                    windIntensity * branch_sway_factor,
                                    new Vector2(40f, 24f),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            break;
                        }

                        // right branch
                        case 66:
                        {
                            const int x_offset = -1;

                            // TODO: Sydney should just make this configurable?
                            self.EmitTreeLeaves(x, y, x + x_offset, floorY);

                            // Emit light from glowing mushroom tree tops.
                            if (treeStyle == (int)VanillaTreeStyle.GlowingMushroom1)
                            {
                                if (tile.color() == 0)
                                {
                                    var colorIntensity = self._rand.Next(28, 42) * 0.005f;
                                    {
                                        colorIntensity += (270 - Main.mouseTextColor) / 1000f;
                                    }

                                    Lighting.AddLight(x, y, 0.1f, 0.2f + colorIntensity / 2f, 0.7f + colorIntensity);
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
                            var branchTexture = treeProfile.GetBranch(tileColor);
                            var branchPos = new Vector2(x * 16, y * 16) - screenPosition.Floor() + new Vector2(0f, 18f);

                            var windIntensity = hasWall ? 0f : self.GetWindCycle(x, y, self._treeWindCounter);
                            {
                                if (windIntensity < 0f)
                                {
                                    branchPos.X += windIntensity;
                                }

                                branchPos.X -= Math.Abs(windIntensity) * 2f;
                            }

                            var tileLight = Lighting.GetColor(x, y);
                            if (tile.fullbrightBlock())
                            {
                                tileLight = Color.White;
                            }

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                branchTexture = gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).Branches.Value;
                            }

                            Main.spriteBatch.Draw(
                                branchTexture,
                                branchPos,
                                new Rectangle(42, treeFrame * 42, 40, 40),
                                tileLight,
                                windIntensity * branch_sway_factor,
                                new Vector2(0f, 30f),
                                1f,
                                SpriteEffects.None,
                                0f
                            );

                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                Main.spriteBatch.Draw(
                                    (gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).BranchesGems ?? gemProfile.Value.Purity.BranchesGems!).Value,
                                    branchPos,
                                    new Rectangle(42, treeFrame * 42, 40, 40),
                                    tileLight * (GemTreeRendering.GlowingGemsEnabled ? 2f : 1f),
                                    windIntensity * branch_sway_factor,
                                    new Vector2(0f, 30f),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            if (type == TileID.TreeAsh)
                            {
                                var ashBranchGlow = TextureAssets.GlowMask[GlowMaskID.TreeAshBranches].Value;

                                Main.spriteBatch.Draw(
                                    ashBranchGlow,
                                    branchPos,
                                    new Rectangle(42, treeFrame * 42, 40, 40),
                                    Color.White,
                                    windIntensity * branch_sway_factor,
                                    new Vector2(0f, 30f),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            break;
                        }
                    }
                }

                // Then palm trees are handled here.  Because they have no
                // branches, only the tops are rendered here.
                if (type == TileID.PalmTree && frameX is >= 88 and <= 132)
                {
                    var palmTopIdx = 15;
                    var palmTopWidth = 80;
                    var palmTopHeight = 80;
                    var palmTopHorizontalOffset = 32;
                    var palmTopVerticalOffset = 0;
                    var palmBiome = self.GetPalmTreeBiome(x, y);
                    var palmTopYFrame = palmBiome * 82;
                    var palmTopXFrame = frameX switch
                    {
                        110 => 1,
                        132 => 2,
                        _ => 0,
                    };

                    // Oasis tree
                    if (palmBiome is >= 4 and <= 7)
                    {
                        palmTopIdx = 21;
                        palmTopWidth = 114;
                        palmTopHeight = 98;
                        palmTopYFrame = (palmBiome - 4) * 98;
                        palmTopHorizontalOffset = 48;
                        palmTopVerticalOffset = 2;
                    }

                    // Handle modded palm trees.
                    if (Math.Abs(palmBiome) >= ModPalmTree.VanillaStyleCount)
                    {
                        palmTopYFrame = 0;

                        // Oasis tree
                        if (palmBiome < 0)
                        {
                            palmTopWidth = 114;
                            palmTopHeight = 98;
                            palmTopHorizontalOffset = 48;
                            palmTopVerticalOffset = 2;
                        }

                        palmTopIdx = Math.Abs(palmBiome) - ModPalmTree.VanillaStyleCount;
                        palmTopIdx *= -2;

                        // Oasis tree
                        if (palmBiome < 0)
                        {
                            palmTopIdx -= 1;
                        }
                    }

                    var palmTopColor = tile.color();
                    var palmTopTexture = self.GetTreeTopTexture(palmTopIdx, palmBiome, palmTopColor);
                    var palmTopPos = new Vector2(
                        x * 16 - (int)screenPosition.X - palmTopHorizontalOffset + frameY + palmTopWidth / 2f,
                        y * 16 - (int)screenPosition.Y + 16 + palmTopVerticalOffset
                    );

                    var windIntensity = hasWall ? 0f : self.GetWindCycle(x, y, self._treeWindCounter);
                    {
                        palmTopPos.X += windIntensity * 2f;
                        palmTopPos.Y += Math.Abs(windIntensity) * 2f;
                    }

                    var palmTopLight = Lighting.GetColor(x, y);
                    if (tile.fullbrightBlock())
                    {
                        palmTopLight = Color.White;
                    }

                    Main.spriteBatch.Draw(
                        palmTopTexture,
                        palmTopPos,
                        new Rectangle(palmTopXFrame * (palmTopWidth + 2), palmTopYFrame, palmTopWidth, palmTopHeight),
                        palmTopLight,
                        windIntensity * treetop_sway_factor,
                        new Vector2(palmTopWidth / 2f, palmTopHeight),
                        1f,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
