using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CelesteLevelEditor
{
    class LevelEditor : Scene
    {
        public Session session;
        public MapData mapData;

        public Vector2 mousePosition;
        public Vector2 lastMouseScreenPosition;

        private static Camera Camera;
        private static AreaKey area;

        public BackgroundTiles bgTiles;
        public SolidTiles fgTiles;

        public VirtualMap<char> bgData;
        public VirtualMap<char> fgData;

        public bool fgDataUpdated;
        public bool bgDataUpdated;
        public int frameCount;

        private VirtualRenderTarget viewBuffer;

        public char tile;

        private MTexture button;
        private MTexture bg1;

        static LevelEditor()
        {
            LevelEditor.area = AreaKey.None;
        }

        public LevelEditor(Session session) : base()
        {

            button = GFX.Gui["editor/button"];
            bg1 = GFX.Gui["editor/bg-1"];

            AreaKey area = session.Area;
            area.ID = Calc.Clamp(area.ID, 0, AreaData.Areas.Count - 1);
            this.mapData = AreaData.Areas[area.ID].Mode[(int)area.Mode].MapData;
            this.mapData.Reload();
            this.session = session;

            tile = '1';

            if (area != LevelEditor.area)
            {
                LevelEditor.area = area;
                LevelEditor.Camera = new Camera();
                LevelEditor.Camera.Zoom = 3f;
                LevelEditor.Camera.CenterOrigin();
            }

            viewBuffer = VirtualContent.CreateRenderTarget("editorView", Math.Min(1920, Engine.ViewWidth), Math.Min(1080, Engine.ViewHeight), false, true, 0);

            LoadLevel();
        }

        private void LoadLevel()
        {
            MapData mapData = this.session.MapData;
            AreaData areaData = AreaData.Get(this.session);
            if (this.session.Area.ID == 0)
            {
                SaveData.Instance.Assists.DashMode = Assists.DashModes.Normal;
            }
            Rectangle tileBounds = mapData.TileBounds;
            GFX.FGAutotiler.LevelBounds.Clear();
            VirtualMap<char> virtualMap = new VirtualMap<char>(tileBounds.Width, tileBounds.Height, '0');
            VirtualMap<char> virtualMap2 = new VirtualMap<char>(tileBounds.Width, tileBounds.Height, '0');
            VirtualMap<bool> virtualMap3 = new VirtualMap<bool>(tileBounds.Width, tileBounds.Height, false);
            Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
            foreach (LevelData levelData in mapData.Levels)
            {
                int left = levelData.TileBounds.Left;
                int top = levelData.TileBounds.Top;
                string[] array = regex.Split(levelData.Bg);
                for (int i = top; i < top + array.Length; i++)
                {
                    for (int j = left; j < left + array[i - top].Length; j++)
                    {
                        virtualMap[j - tileBounds.X, i - tileBounds.Y] = array[i - top][j - left];
                    }
                }
                string[] array2 = regex.Split(levelData.Solids);
                for (int k = top; k < top + array2.Length; k++)
                {
                    for (int l = left; l < left + array2[k - top].Length; l++)
                    {
                        virtualMap2[l - tileBounds.X, k - tileBounds.Y] = array2[k - top][l - left];
                    }
                }
                for (int m = levelData.TileBounds.Left; m < levelData.TileBounds.Right; m++)
                {
                    for (int n = levelData.TileBounds.Top; n < levelData.TileBounds.Bottom; n++)
                    {
                        virtualMap3[m - tileBounds.Left, n - tileBounds.Top] = true;
                    }
                }
                GFX.FGAutotiler.LevelBounds.Add(new Rectangle(levelData.TileBounds.X - tileBounds.X, levelData.TileBounds.Y - tileBounds.Y, levelData.TileBounds.Width, levelData.TileBounds.Height));
            }
            foreach (Rectangle rectangle in mapData.Filler)
            {
                for (int num = rectangle.Left; num < rectangle.Right; num++)
                {
                    for (int num2 = rectangle.Top; num2 < rectangle.Bottom; num2++)
                    {
                        char c = '0';
                        if (rectangle.Top - tileBounds.Y > 0)
                        {
                            char c2 = virtualMap2[num - tileBounds.X, rectangle.Top - tileBounds.Y - 1];
                            if (c2 != '0')
                            {
                                c = c2;
                            }
                        }
                        if (c == '0' && rectangle.Left - tileBounds.X > 0)
                        {
                            char c3 = virtualMap2[rectangle.Left - tileBounds.X - 1, num2 - tileBounds.Y];
                            if (c3 != '0')
                            {
                                c = c3;
                            }
                        }
                        if (c == '0' && rectangle.Right - tileBounds.X < tileBounds.Width - 1)
                        {
                            char c4 = virtualMap2[rectangle.Right - tileBounds.X, num2 - tileBounds.Y];
                            if (c4 != '0')
                            {
                                c = c4;
                            }
                        }
                        if (c == '0' && rectangle.Bottom - tileBounds.Y < tileBounds.Height - 1)
                        {
                            char c5 = virtualMap2[num - tileBounds.X, rectangle.Bottom - tileBounds.Y];
                            if (c5 != '0')
                            {
                                c = c5;
                            }
                        }
                        if (c == '0')
                        {
                            c = '1';
                        }
                        virtualMap2[num - tileBounds.X, num2 - tileBounds.Y] = c;
                        virtualMap3[num - tileBounds.X, num2 - tileBounds.Y] = true;
                    }
                }
            }
            foreach (LevelData levelData2 in mapData.Levels)
            {
                for (int num3 = levelData2.TileBounds.Left; num3 < levelData2.TileBounds.Right; num3++)
                {
                    int num4 = levelData2.TileBounds.Top;
                    char value = virtualMap[num3 - tileBounds.X, num4 - tileBounds.Y];
                    int num5 = 1;
                    while (num5 < 4 && !virtualMap3[num3 - tileBounds.X, num4 - tileBounds.Y - num5])
                    {
                        virtualMap[num3 - tileBounds.X, num4 - tileBounds.Y - num5] = value;
                        num5++;
                    }
                    num4 = levelData2.TileBounds.Bottom - 1;
                    char value2 = virtualMap[num3 - tileBounds.X, num4 - tileBounds.Y];
                    int num6 = 1;
                    while (num6 < 4 && !virtualMap3[num3 - tileBounds.X, num4 - tileBounds.Y + num6])
                    {
                        virtualMap[num3 - tileBounds.X, num4 - tileBounds.Y + num6] = value2;
                        num6++;
                    }
                }
                for (int num7 = levelData2.TileBounds.Top - 4; num7 < levelData2.TileBounds.Bottom + 4; num7++)
                {
                    int num8 = levelData2.TileBounds.Left;
                    char value3 = virtualMap[num8 - tileBounds.X, num7 - tileBounds.Y];
                    int num9 = 1;
                    while (num9 < 4 && !virtualMap3[num8 - tileBounds.X - num9, num7 - tileBounds.Y])
                    {
                        virtualMap[num8 - tileBounds.X - num9, num7 - tileBounds.Y] = value3;
                        num9++;
                    }
                    num8 = levelData2.TileBounds.Right - 1;
                    char value4 = virtualMap[num8 - tileBounds.X, num7 - tileBounds.Y];
                    int num10 = 1;
                    while (num10 < 4 && !virtualMap3[num8 - tileBounds.X + num10, num7 - tileBounds.Y])
                    {
                        virtualMap[num8 - tileBounds.X + num10, num7 - tileBounds.Y] = value4;
                        num10++;
                    }
                }
            }
            foreach (LevelData levelData3 in mapData.Levels)
            {
                for (int num11 = levelData3.TileBounds.Left; num11 < levelData3.TileBounds.Right; num11++)
                {
                    int num12 = levelData3.TileBounds.Top;
                    if (virtualMap2[num11 - tileBounds.X, num12 - tileBounds.Y] == '0')
                    {
                        for (int num13 = 1; num13 < 8; num13++)
                        {
                            virtualMap3[num11 - tileBounds.X, num12 - tileBounds.Y - num13] = true;
                        }
                    }
                    num12 = levelData3.TileBounds.Bottom - 1;
                    if (virtualMap2[num11 - tileBounds.X, num12 - tileBounds.Y] == '0')
                    {
                        for (int num14 = 1; num14 < 8; num14++)
                        {
                            virtualMap3[num11 - tileBounds.X, num12 - tileBounds.Y + num14] = true;
                        }
                    }
                }
            }
            foreach (LevelData levelData4 in mapData.Levels)
            {
                for (int num15 = levelData4.TileBounds.Left; num15 < levelData4.TileBounds.Right; num15++)
                {
                    int num16 = levelData4.TileBounds.Top;
                    char value5 = virtualMap2[num15 - tileBounds.X, num16 - tileBounds.Y];
                    int num17 = 1;
                    while (num17 < 4 && !virtualMap3[num15 - tileBounds.X, num16 - tileBounds.Y - num17])
                    {
                        virtualMap2[num15 - tileBounds.X, num16 - tileBounds.Y - num17] = value5;
                        num17++;
                    }
                    num16 = levelData4.TileBounds.Bottom - 1;
                    char value6 = virtualMap2[num15 - tileBounds.X, num16 - tileBounds.Y];
                    int num18 = 1;
                    while (num18 < 4 && !virtualMap3[num15 - tileBounds.X, num16 - tileBounds.Y + num18])
                    {
                        virtualMap2[num15 - tileBounds.X, num16 - tileBounds.Y + num18] = value6;
                        num18++;
                    }
                }
                for (int num19 = levelData4.TileBounds.Top - 4; num19 < levelData4.TileBounds.Bottom + 4; num19++)
                {
                    int num20 = levelData4.TileBounds.Left;
                    char value7 = virtualMap2[num20 - tileBounds.X, num19 - tileBounds.Y];
                    int num21 = 1;
                    while (num21 < 4 && !virtualMap3[num20 - tileBounds.X - num21, num19 - tileBounds.Y])
                    {
                        virtualMap2[num20 - tileBounds.X - num21, num19 - tileBounds.Y] = value7;
                        num21++;
                    }
                    num20 = levelData4.TileBounds.Right - 1;
                    char value8 = virtualMap2[num20 - tileBounds.X, num19 - tileBounds.Y];
                    int num22 = 1;
                    while (num22 < 4 && !virtualMap3[num20 - tileBounds.X + num22, num19 - tileBounds.Y])
                    {
                        virtualMap2[num20 - tileBounds.X + num22, num19 - tileBounds.Y] = value8;
                        num22++;
                    }
                }
            }
            Vector2 position = new Vector2((float)tileBounds.X, (float)tileBounds.Y) * 8f;
            Calc.PushRandom(mapData.LoadSeed);
            this.bgTiles = new BackgroundTiles(position, virtualMap);
            this.fgTiles = new SolidTiles(position, virtualMap2);
            this.bgData = virtualMap;
            this.fgData = virtualMap2;

            fgTiles.Tiles.ClipCamera = LevelEditor.Camera;
            bgTiles.Tiles.ClipCamera = LevelEditor.Camera;

            Calc.PopRandom();
        }

        public override void Update()
        {
            Vector2 value;
            value.X = (this.lastMouseScreenPosition.X - MInput.Mouse.Position.X) / LevelEditor.Camera.Zoom;
            value.Y = (this.lastMouseScreenPosition.Y - MInput.Mouse.Position.Y) / LevelEditor.Camera.Zoom;
            LevelEditor.Camera.Position += new Vector2((float)Input.MoveX.Value, (float)Input.MoveY.Value) * 600f * Engine.DeltaTime;
            this.UpdateMouse();

            if (MInput.Keyboard.Pressed(Keys.Back))
                tile = '0';
            if (MInput.Keyboard.Pressed(Keys.D0))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? '1' : '3'; //DIRT | SNOW
            if (MInput.Keyboard.Pressed(Keys.D1))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? '5' : '6'; //TOWER | STONE
            if (MInput.Keyboard.Pressed(Keys.D2))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? '7' : '8'; //CEMENT | ROCK
            if (MInput.Keyboard.Pressed(Keys.D3))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? '9' : 'a'; //WOOD | WOODSTONE
            if (MInput.Keyboard.Pressed(Keys.D4))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'd' : 'e'; //TEMPLEA | TEMPLEB
            if (MInput.Keyboard.Pressed(Keys.D5))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'b' : 'f'; //CLIFFSIDE | CLIFFSIDE2
            if (MInput.Keyboard.Pressed(Keys.D6))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'g' : 'G'; //REFLECTION | REFLECTION2
            if (MInput.Keyboard.Pressed(Keys.D7))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'i' : 'j'; //SUMMIT | SUMMIT !SNOW
            if (MInput.Keyboard.Pressed(Keys.D8))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'c' : 'k'; //POOL | CORE
            if (MInput.Keyboard.Pressed(Keys.D9))
                tile = !MInput.Keyboard.Check(Keys.LeftShift) ? 'h' : 'l'; //GRASS | DEAD GRASS 

            if (MInput.Mouse.CheckLeftButton || MInput.Mouse.CheckRightButton)
            {
                int x = (int)(this.mousePosition / 8).X;
                int y = (int)(this.mousePosition / 8).Y;
                if (this.mousePosition.X < 0)
                    x -= 1;
                if (this.mousePosition.Y < 0)
                    y -= 1;
                int left = (int)fgTiles.Left / 8;
                int w = fgTiles.Tiles.TilesX;
                int top = (int)fgTiles.Top / 8;
                int h = fgTiles.Tiles.TilesY;
                int virtualX = x - left;
                int virtualY = y - top;
                if (virtualX < 0)
                    virtualX = 0;
                if (virtualX > w - 1)
                    virtualX = w - 1;
                if (virtualY < 0)
                    virtualY = 0;
                if (virtualY > h - 1)
                    virtualY = h - 1;

                if(MInput.Mouse.CheckLeftButton && fgData != null)
                {
                    if (fgData[virtualX, virtualY] != tile)
                    {
                        this.fgData[virtualX, virtualY] = tile;
                        fgDataUpdated = true;
                    }
                }
                if (MInput.Mouse.CheckRightButton && bgData != null)
                {
                    if (bgData[virtualX, virtualY] != tile && (tile <= 'd' || Char.IsDigit(tile)))
                    {
                        this.bgData[virtualX, virtualY] = tile;
                        bgDataUpdated = true;
                    }
                }
            }

            if( fgDataUpdated && frameCount == 0)
            {
                Rectangle tileBounds = this.session.MapData.TileBounds;
                Vector2 position = new Vector2(tileBounds.X, tileBounds.Y) * 8f;
                fgDataUpdated = false;
                Calc.PushRandom(mapData.LoadSeed);
                this.fgTiles = new SolidTiles(position, fgData);
                fgTiles.Tiles.ClipCamera = LevelEditor.Camera;
                Calc.PopRandom();
            }
            if (bgDataUpdated && frameCount == 4)
            {
                Rectangle tileBounds = this.session.MapData.TileBounds;
                Vector2 position = new Vector2(tileBounds.X, tileBounds.Y) * 8f;
                bgDataUpdated = false;
                Calc.PushRandom(mapData.LoadSeed);
                this.bgTiles = new BackgroundTiles(position, bgData);
                bgTiles.Tiles.ClipCamera = LevelEditor.Camera;
                Calc.PopRandom();
            }

            frameCount++;
            if (frameCount > 7)
                frameCount = 0;

            this.lastMouseScreenPosition = MInput.Mouse.Position;
            base.Update();
        }

        private void UpdateMouse()
        {
            this.mousePosition = Vector2.Transform(MInput.Mouse.Position, Matrix.Invert(LevelEditor.Camera.Matrix));
        }

        public override void BeforeRender()
        {
            base.BeforeRender();
        }

        public override void Render()
        {
            this.UpdateMouse();

            int x = (int)(this.mousePosition / 8).X;
            int y = (int)(this.mousePosition / 8).Y;
            if (this.mousePosition.X < 0)
                x -= 1;
            if (this.mousePosition.Y < 0)
                y -= 1;
            int left = (int)fgTiles.Left / 8;
            int w = fgTiles.Tiles.TilesX;
            int top = (int)fgTiles.Top / 8;
            int h = fgTiles.Tiles.TilesY;
            int virtualX = x - left;
            int virtualY = y - top;
            if (virtualX < 0)
                virtualX = 0;
            if (virtualX > w - 1)
                virtualX = w - 1;
            if (virtualY < 0)
                virtualY = 0;
            if (virtualY > h - 1)
                virtualY = h - 1;

            

            

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, LevelEditor.Camera.Matrix * Engine.ScreenMatrix);

            float width = 1920 / LevelEditor.Camera.Zoom;
            float height = 1080 / LevelEditor.Camera.Zoom;

            Draw.Line(0f, LevelEditor.Camera.Top, 0f, LevelEditor.Camera.Top + height, Color.DarkSlateBlue);
            Draw.Line(LevelEditor.Camera.Left, 0f, LevelEditor.Camera.Left + width, 0f, Color.DarkSlateBlue);

            bgTiles.Tiles.Render();
            fgTiles.Tiles.Render();

            foreach (LevelData ld in mapData.Levels)
            {
                Draw.HollowRect((float)ld.Position.X - 1, (float)ld.Position.Y - 1, (float)(ld.TileBounds.Width * 8) + 2, (float)(ld.TileBounds.Height * 8) + 2, Calc.HexToColor("ffffff"));
            }

            Draw.SpriteBatch.End();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);

            bg1.Draw(new Vector2(0, 0), new Vector2(0, 0), Color.White, new Vector2(16, Engine.ViewHeight / 4));
            bg1.Draw(new Vector2((Engine.Width-48), 0), new Vector2(0, 0), Color.White, new Vector2(6, Engine.ViewHeight / 4));

            button.Draw(new Vector2((Engine.Width - 40), 16), new Vector2(0, 0), Color.White, new Vector2(2, 2));
            button.Draw(new Vector2((Engine.Width - 40), 48), new Vector2(0, 0), Color.White, new Vector2(2, 2));
            button.Draw(new Vector2((Engine.Width - 40), 80), new Vector2(0, 0), Color.White, new Vector2(2, 2));
            button.Draw(new Vector2((Engine.Width - 40), 112), new Vector2(0, 0), Color.White, new Vector2(2, 2));


            PixelFontSize pixelFontSize = ActiveFont.Font.Get(16f);
            pixelFontSize.Size = 96f;

            ActiveFont.Draw("Tile: "+tile.ToString(), new Vector2(16, 32), Color.Red);

            //ActiveFont.Draw(x.ToString(), new Vector2(16, 32), Color.Red);
            //ActiveFont.Draw(y.ToString(), new Vector2(16, 64), Color.Red);

            //ActiveFont.Draw(virtualX.ToString(), new Vector2(16, 96), Color.Red);
            //ActiveFont.Draw(virtualY.ToString(), new Vector2(16, 128), Color.Red);

            //MTexture texture = fgTiles.Tiles.Tiles[virtualX, virtualY];
            //if (texture != null)
            //    texture.Draw(new Vector2(16, 16), Vector2.Zero, fgTiles.Tiles.Color * fgTiles.Tiles.Alpha, 2f);                

            //texturePreview.Draw(new Vector2(16, 24));

            Draw.SpriteBatch.End();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, LevelEditor.Camera.Matrix * Engine.ScreenMatrix);
            Draw.Line(this.mousePosition.X - 4f, this.mousePosition.Y, this.mousePosition.X + 3f, this.mousePosition.Y, Color.Yellow);
            Draw.Line(this.mousePosition.X, this.mousePosition.Y - 3f, this.mousePosition.X, this.mousePosition.Y + 4f, Color.Yellow);
            Draw.SpriteBatch.End();
        }

    }
}
