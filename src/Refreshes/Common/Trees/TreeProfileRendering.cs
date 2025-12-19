using System;
using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace Refreshes.Common;

[UsedImplicitly]
internal sealed class TreeProfileRendering
{
    [OnLoad]
    [UsedImplicitly]
    private static void InjectHooks()
    {
        On_TileDrawing.DrawTrees += DrawTrees_RewriteTreeRenderingForProfiles;
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

                            var windIntensity = hasWall ? 0f : self.GetWindCycle(x, y, self._treeWindCounter);
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

                            //hardcoded big tree style, todo: reimplement using profiles when ready
                            /*if (treeStyle3 == 0 && seededRandom.NextFloat() < customRectChance) {
                                rect = new Rectangle(0, 0, 216, 190);
                                vector.X -= 2;
                                vector.Y += 4;

                                origin = new Vector2(216 / 2, 190);

                                num15 = self.GetWindCycle(x, y, self._treeWindCounter) * 0.5f;

                                //treeTopTexture = Assets.Images.Content.Trees.Tree_Tops_0.Asset.Value;
                                treeTopTexture = profile.GetTop(tile.color());
                            } else {
                                rect = new Rectangle(treeFrame * (topTextureFrameWidth3 + 2), 0, topTextureFrameWidth3, topTextureFrameHeight3);

                                origin = new Vector2(topTextureFrameWidth3 / 2, topTextureFrameHeight3);
                            }*/

                            /*
                            var gemProfile = GemTreeVanityProfiles.GetProfile(tile.TileType);
                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                topTexture = gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).Tops.Value;
                            }
                            */

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

                            var num9 = x;

                            self.EmitTreeLeaves(x, y, num9 + x_offset, floorY);
                            if (treeStyle == 14)
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
                            var treeBranchTexture2 = self.GetTreeBranchTexture(treeStyle, 0, tileColor2);
                            var position2 = new Vector2(x * 16, y * 16) - screenPosition.Floor() + zero +
                                            new Vector2(16f, 12f);
                            var num12 = 0f;
                            if (!hasWall)
                            {
                                num12 = self.GetWindCycle(x, y, self._treeWindCounter);
                            }

                            //tree branch pos sway (remove later)
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

                            //left branch
                            Main.spriteBatch.Draw(treeBranchTexture2, position2, new Rectangle(0, treeFrame * 42, 40, 40), color4, num12 * branch_sway_factor, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);

                            //ashtree left branch
                            if (type == 634)
                            {
                                var value2 = TextureAssets.GlowMask[317].Value;
                                var white2 = Color.White;
                                Main.spriteBatch.Draw(
                                    value2,
                                    position2,
                                    new Rectangle(0, treeFrame * 42, 40, 40),
                                    white2,
                                    num12 * branch_sway_factor,
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

                            var num5 = x;

                            self.EmitTreeLeaves(x, y, num5 + x_offset, floorY);
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
                            var position = new Vector2(x * 16, y * 16) - screenPosition.Floor() + zero +
                                           new Vector2(0f, 18f);
                            var num8 = 0f;
                            if (!hasWall)
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

                            //right branch
                            Main.spriteBatch.Draw(
                                treeBranchTexture,
                                position,
                                new Rectangle(42, treeFrame * 42, 40, 40),
                                color2,
                                num8 * branch_sway_factor,
                                new Vector2(0f, 30f),
                                1f,
                                SpriteEffects.None,
                                0f
                            );

                            //ashtree right branch
                            if (type == 634)
                            {
                                var value = TextureAssets.GlowMask[317].Value;
                                var white = Color.White;
                                Main.spriteBatch.Draw(
                                    value,
                                    position,
                                    new Rectangle(42, treeFrame * 42, 40, 40),
                                    white,
                                    num8 * branch_sway_factor,
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
