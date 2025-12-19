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
                    var treeFrame = WorldGen.GetTreeFrame(tile);
                    switch (frameX)
                    {
                        // left branch
                        case 22:
                        {
                            var treeStyle3 = 0;
                            var topTextureFrameWidth3 = 80;
                            var topTextureFrameHeight3 = 80;
                            var num13 = 0;
                            var grassPosX = x + num13;
                            var floorY3 = y;
                            if (!foliageDataProvider(x, y, num13, ref treeFrame, ref treeStyle3, out floorY3, out topTextureFrameWidth3, out topTextureFrameHeight3))
                            {
                                continue;
                            }

                            //self.EmitTreeLeaves(x, y, grassPosX, floorY3);

                            //treestyle == 14 = giant mushroom tree
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
                            Vector2 vector = vector = new Vector2(x * 16 - (int)screenPosition.X + 8, y * 16 - (int)screenPosition.Y + 16) + zero;
                            var num15 = 0f;
                            if (!hasWall)
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

                            TreeProfiles.TryGetProfile(treeStyle3, out var profile);
                            var variation = profile.GetVariation(treeFrame);

                            Rectangle rect = new(treeFrame * (variation.Width + 2), 0, variation.Width, variation.Height);
                            Vector2 origin = new(variation.Width / 2, variation.Height);

#region reimpl
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
#endregion

                            treeTopTexture = profile.GetTop(tileColor3);

                            var gemProfile = GemTreeVanityProfiles.GetProfile(tile.TileType);
                            if (gemProfile.HasValue && GemTreeRendering.RenderCtx.HasValue)
                            {
                                treeTopTexture = gemProfile.Value.GetDescription(GemTreeRendering.RenderCtx.Value.CurrentBiome).Tops.Value;
                            }

                            // draw treetop
                            Main.spriteBatch.Draw(
                                treeTopTexture,
                                vector,
                                rect,
                                color6,
                                num15 * treetop_sway_factor,
                                origin + variation.OriginOffset,
                                1f,
                                SpriteEffects.None,
                                0f
                            );

                            //ashtree, tbd
                            if (type == 634)
                            {
                                var value3 = TextureAssets.GlowMask[316].Value;
                                var white3 = Color.White;
                                Main.spriteBatch.Draw(
                                    value3,
                                    vector,
                                    new Rectangle(
                                        treeFrame * (topTextureFrameWidth3 + 2),
                                        0,
                                        topTextureFrameWidth3,
                                        topTextureFrameHeight3
                                    ),
                                    white3,
                                    num15 * treetop_sway_factor,
                                    new Vector2(topTextureFrameWidth3 / 2, topTextureFrameHeight3),
                                    1f,
                                    SpriteEffects.None,
                                    0f
                                );
                            }

                            break;
                        }

                        // right branch
                        case 44:
                        {
                            var treeStyle2 = 0;
                            var num9 = x;
                            var floorY2 = y;
                            var num10 = 1;
                            if (!foliageDataProvider(
                                    x,
                                    y,
                                    num10,
                                    ref treeFrame,
                                    ref treeStyle2,
                                    out floorY2,
                                    out _,
                                    out _
                                ))
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

                        // tree top
                        case 66:
                        {
                            var treeStyle = 0;
                            var num5 = x;
                            var floorY = y;
                            var num6 = -1;
                            if (!foliageDataProvider(
                                    x,
                                    y,
                                    num6,
                                    ref treeFrame,
                                    ref treeStyle,
                                    out floorY,
                                    out _,
                                    out _
                                ))
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

                    var windIntensity = hasWall
                        ? 0f
                        : self.GetWindCycle(x, y, self._treeWindCounter);

                    palmTopPos.X += windIntensity * 2f;
                    palmTopPos.Y += Math.Abs(windIntensity) * 2f;

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
