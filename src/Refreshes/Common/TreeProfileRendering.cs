using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using System;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Utilities;

namespace Refreshes.Common;

[UsedImplicitly]
internal sealed class TreeProfileRendering {
    [OnLoad, UsedImplicitly]
    static void InjectHooks() {
        On_TileDrawing.DrawTrees += On_TileDrawingOnDrawTrees;
    }

    private static void On_TileDrawingOnDrawTrees(On_TileDrawing.orig_DrawTrees orig, TileDrawing self) {
        Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
		Vector2 zero = Vector2.Zero;
		int num = 0;
		int num2 = self._specialsCount[num];
		float num3 = 0.08f;
		float num4 = 0.06f;
        
        const float customRectChance = 0.2f;
        
		for (int i = 0; i < num2; i++) {
			Point point = self._specialPositions[num][i];
			int x = point.X;
			int y = point.Y;
			Tile tile = Main.tile[x, y];
			if (tile == null || !tile.active())
				continue;
            
            UnifiedRandom seededRandom = new UnifiedRandom(x * 1000 + y);

			ushort type = tile.type;
			short frameX = tile.frameX;
			short frameY = tile.frameY;
			bool flag = tile.wall > 0;
			WorldGen.GetTreeFoliageDataMethod getTreeFoliageDataMethod = null;
            try {
                bool flag2 = false;
                switch(type) {
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

                if(flag2 && frameY >= 198 && frameX >= 22) {
                    int treeFrame = WorldGen.GetTreeFrame(tile);
                    switch(frameX) {
                        // generic trees??
                        case 22: {
                            int treeStyle3 = 0;
                            int topTextureFrameWidth3 = 80;
                            int topTextureFrameHeight3 = 80;
                            int num13 = 0;
                            int grassPosX = x + num13;
                            int floorY3 = y;
                            if(!getTreeFoliageDataMethod(x, y, num13, ref treeFrame, ref treeStyle3, out floorY3,out topTextureFrameWidth3, out topTextureFrameHeight3))
                                continue;

                            //self.EmitTreeLeaves(x, y, grassPosX, floorY3);
                            
                            //treestyle == 14 = giant mushroom tree
                            if(treeStyle3 == 14) {
                                float num14 = self._rand.Next(28, 42) * 0.005f;
                                num14 += (270 - Main.mouseTextColor) / 1000f;
                                if(tile.color() == 0) {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num14 / 2f, 0.7f + num14);
                                }
                                else {
                                    Color color5 = WorldGen.paintColor(tile.color());
                                    float r3 = color5.R / 255f;
                                    float g3 = color5.G / 255f;
                                    float b3 = color5.B / 255f;
                                    Lighting.AddLight(x, y, r3, g3, b3);
                                }
                            }

                            byte tileColor3 = tile.color();
                            Texture2D treeTopTexture = self.GetTreeTopTexture(treeStyle3, 0, tileColor3);
                            Vector2 vector = (vector = new Vector2(x * 16 - (int)unscaledPosition.X + 8, y * 16 - (int)unscaledPosition.Y + 16) + zero);
                            float num15 = 0f;
                            if(!flag)
                                num15 = self.GetWindCycle(x, y, self._treeWindCounter);

                            vector.X += num15 * 2f;
                            vector.Y += Math.Abs(num15) * 2f;
                            
                            Color color6 = Lighting.GetColor(x, y);
                            if(tile.fullbrightBlock())
                                color6 = Color.White;

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
                            
                            // draw treetop
                            Main.spriteBatch.Draw(
                                profile.GetTop(tileColor3), 
                                vector,
                                rect, 
                                color6, 
                                num15 * num3,
                                origin + variation.OriginOffset, 
                                1f,
                                SpriteEffects.None,
                                0f);
                            
                            //ashtree, tbd
                            if(type == 634) {
                                Texture2D value3 = TextureAssets.GlowMask[316].Value;
                                Color white3 = Color.White;
                                Main.spriteBatch.Draw(value3, vector,
                                    new Rectangle(treeFrame * (topTextureFrameWidth3 + 2), 0, topTextureFrameWidth3,
                                        topTextureFrameHeight3), white3, num15 * num3,
                                    new Vector2(topTextureFrameWidth3 / 2, topTextureFrameHeight3), 1f,
                                    SpriteEffects.None, 0f);
                            }

                            break;
                        }
                        case 44: {
                            int treeStyle2 = 0;
                            int num9 = x;
                            int floorY2 = y;
                            int num10 = 1;
                            if(!getTreeFoliageDataMethod(x, y, num10, ref treeFrame, ref treeStyle2, out floorY2,
                                   out var _, out var _))
                                continue;

                            self.EmitTreeLeaves(x, y, num9 + num10, floorY2);
                            if(treeStyle2 == 14) {
                                float num11 = (float)self._rand.Next(28, 42) * 0.005f;
                                num11 += (float)(270 - Main.mouseTextColor) / 1000f;
                                if(tile.color() == 0) {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num11 / 2f, 0.7f + num11);
                                }
                                else {
                                    Color color3 = WorldGen.paintColor(tile.color());
                                    float r2 = (float)(int)color3.R / 255f;
                                    float g2 = (float)(int)color3.G / 255f;
                                    float b2 = (float)(int)color3.B / 255f;
                                    Lighting.AddLight(x, y, r2, g2, b2);
                                }
                            }

                            byte tileColor2 = tile.color();
                            Texture2D treeBranchTexture2 = self.GetTreeBranchTexture(treeStyle2, 0, tileColor2);
                            Vector2 position2 = new Vector2(x * 16, y * 16) - unscaledPosition.Floor() + zero +
                                                new Vector2(16f, 12f);
                            float num12 = 0f;
                            if(!flag)
                                num12 = self.GetWindCycle(x, y, self._treeWindCounter);

                            //tree branch pos sway (remove later)
                            if(num12 > 0f)
                                position2.X += num12;

                            position2.X += Math.Abs(num12) * 2f;
                            Color color4 = Lighting.GetColor(x, y);
                            if(tile.fullbrightBlock())
                                color4 = Color.White;

                            //left branch
                            Main.spriteBatch.Draw(treeBranchTexture2, position2, new Rectangle(0, treeFrame * 42, 40, 40), color4, num12 * num4, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);
                            
                            //ashtree left branch
                            if(type == 634) {
                                Texture2D value2 = TextureAssets.GlowMask[317].Value;
                                Color white2 = Color.White;
                                Main.spriteBatch.Draw(value2, position2, new Rectangle(0, treeFrame * 42, 40, 40),
                                    white2, num12 * num4, new Vector2(40f, 24f), 1f, SpriteEffects.None, 0f);
                            }

                            break;
                        }
                        case 66: {
                            int treeStyle = 0;
                            int num5 = x;
                            int floorY = y;
                            int num6 = -1;
                            if(!getTreeFoliageDataMethod(x, y, num6, ref treeFrame, ref treeStyle, out floorY,
                                   out var _, out var _))
                                continue;

                            self.EmitTreeLeaves(x, y, num5 + num6, floorY);
                            if(treeStyle == 14) {
                                float num7 = (float)self._rand.Next(28, 42) * 0.005f;
                                num7 += (float)(270 - Main.mouseTextColor) / 1000f;
                                if(tile.color() == 0) {
                                    Lighting.AddLight(x, y, 0.1f, 0.2f + num7 / 2f, 0.7f + num7);
                                }
                                else {
                                    Color color = WorldGen.paintColor(tile.color());
                                    float r = (float)(int)color.R / 255f;
                                    float g = (float)(int)color.G / 255f;
                                    float b = (float)(int)color.B / 255f;
                                    Lighting.AddLight(x, y, r, g, b);
                                }
                            }

                            byte tileColor = tile.color();
                            Texture2D treeBranchTexture = self.GetTreeBranchTexture(treeStyle, 0, tileColor);
                            Vector2 position = new Vector2(x * 16, y * 16) - unscaledPosition.Floor() + zero +
                                               new Vector2(0f, 18f);
                            float num8 = 0f;
                            if(!flag)
                                num8 = self.GetWindCycle(x, y, self._treeWindCounter);

                            if(num8 < 0f)
                                position.X += num8;

                            position.X -= Math.Abs(num8) * 2f;
                            Color color2 = Lighting.GetColor(x, y);
                            if(tile.fullbrightBlock())
                                color2 = Color.White;

                            //right branch
                            Main.spriteBatch.Draw(treeBranchTexture, position,
                                new Rectangle(42, treeFrame * 42, 40, 40), color2, num8 * num4, new Vector2(0f, 30f),
                                1f, SpriteEffects.None, 0f);
                            
                            //ashtree right branch
                            if(type == 634) {
                                Texture2D value = TextureAssets.GlowMask[317].Value;
                                Color white = Color.White;
                                Main.spriteBatch.Draw(value, position, new Rectangle(42, treeFrame * 42, 40, 40), white,
                                    num8 * num4, new Vector2(0f, 30f), 1f, SpriteEffects.None, 0f);
                            }

                            break;
                        }
                    }
                }

                if(type == 323 && frameX >= 88 && frameX <= 132) {
                    int num16 = 0;
                    switch(frameX) {
                        case 110:
                            num16 = 1;
                            break;
                        case 132:
                            num16 = 2;
                            break;
                    }

                    int treeTextureIndex = 15;
                    int num17 = 80;
                    int num18 = 80;
                    int num19 = 32;
                    int num20 = 0;
                    int palmTreeBiome = self.GetPalmTreeBiome(x, y);
                    int y2 = palmTreeBiome * 82;
                    if(palmTreeBiome >= 4 && palmTreeBiome <= 7) {
                        treeTextureIndex = 21;
                        num17 = 114;
                        num18 = 98;
                        y2 = (palmTreeBiome - 4) * 98;
                        num19 = 48;
                        num20 = 2;
                    }

                    // Handle mod palms.
                    if(Math.Abs(palmTreeBiome) >= ModPalmTree.VanillaStyleCount) {
                        y2 = 0;

                        // Oasis Tree
                        if(palmTreeBiome < 0) {
                            num17 = 114;
                            num18 = 98;
                            num19 = 48;
                            num20 = 2;
                        }

                        treeTextureIndex = (Math.Abs(palmTreeBiome) - ModPalmTree.VanillaStyleCount);
                        treeTextureIndex *= -2;

                        // Oasis tree
                        if(palmTreeBiome < 0)
                            treeTextureIndex -= 1;
                    }

                    int frameY2 = Main.tile[x, y].frameY;
                    byte tileColor4 = tile.color();
                    Texture2D treeTopTexture2 = self.GetTreeTopTexture(treeTextureIndex, palmTreeBiome, tileColor4);
                    Vector2 position3 = new Vector2(x * 16 - (int)unscaledPosition.X - num19 + frameY2 + num17 / 2,
                        y * 16 - (int)unscaledPosition.Y + 16 + num20) + zero;
                    float num21 = 0f;
                    if(!flag)
                        num21 = self.GetWindCycle(x, y, self._treeWindCounter);

                    position3.X += num21 * 2f;
                    position3.Y += Math.Abs(num21) * 2f;
                    Color color7 = Lighting.GetColor(x, y);
                    if(tile.fullbrightBlock())
                        color7 = Color.White;

                    //palms
                    Main.spriteBatch.Draw(treeTopTexture2, position3,
                        new Rectangle(num16 * (num17 + 2), y2, num17, num18), color7, num21 * num3,
                        new Vector2(num17 / 2, num18), 1f, SpriteEffects.None, 0f);
                }
            }
            catch {
                
            }
		}
    }
}