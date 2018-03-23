using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace CelesteLevelEditor
{
    class LevelEditor : Scene
    {
        public List<Rectangle> LevelBounds;

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

        private VirtualRenderTarget viewBuffer;

        public char tile;

        private MTexture button;
        private MTexture bg1;

        private Dictionary<string, LevelData> hovered;

        public Dictionary<char, TerrainType> lookup;

        public FieldInfo fiTexture;

        public byte[] adjacent;

        public class TerrainType
        {
            public TerrainType(char id)
            {
                this.Ignores = new HashSet<char>();
                this.Masked = new List<Masked>();
                this.Center = new Tiles();
                this.Padded = new Tiles();
                this.ID = id;
            }
            public bool Ignore(char c)
            {
                return this.ID != c && (this.Ignores.Contains(c) || this.Ignores.Contains('*'));
            }
            public char ID;
            public HashSet<char> Ignores;
            public List<Masked> Masked;
            public Tiles Center;
            public Tiles Padded;
        }

        public class Masked
        {
            // Token: 0x06002176 RID: 8566 RVA: 0x000AC7C1 File Offset: 0x000AA9C1
            public Masked()
            {
                this.Mask = new byte[9];
                this.Tiles = new Tiles();
            }

            // Token: 0x04001B8B RID: 7051
            public byte[] Mask;

            // Token: 0x04001B8C RID: 7052
            public Tiles Tiles;
        }

        public class Tiles
        {
            // Token: 0x06002177 RID: 8567 RVA: 0x000AC7E1 File Offset: 0x000AA9E1
            public Tiles()
            {
                this.Textures = new List<MTexture>();
                this.OverlapSprites = new List<string>();
            }

            // Token: 0x04001B8D RID: 7053
            public List<MTexture> Textures;

            // Token: 0x04001B8E RID: 7054
            public List<string> OverlapSprites;

            // Token: 0x04001B8F RID: 7055
            public bool HasOverlays;
        }

        public void ReadInto(TerrainType data, Tileset tileset, XmlElement xml)
        {
            foreach (object obj in xml)
            {
                if (!(obj is XmlComment))
                {
                    XmlElement xml2 = obj as XmlElement;
                    string text = xml2.Attr("mask");
                    Tiles tiles;
                    if (text == "center")
                    {
                        tiles = data.Center;
                    }
                    else if (text == "padding")
                    {
                        tiles = data.Padded;
                    }
                    else
                    {
                        Masked masked = new Masked();
                        tiles = masked.Tiles;
                        int i = 0;
                        int num = 0;
                        while (i < text.Length)
                        {
                            if (text[i] == '0')
                            {
                                masked.Mask[num++] = 0;
                            }
                            else if (text[i] == '1')
                            {
                                masked.Mask[num++] = 1;
                            }
                            else if (text[i] == 'x' || text[i] == 'X')
                            {
                                masked.Mask[num++] = 2;
                            }
                            i++;
                        }
                        data.Masked.Add(masked);
                    }
                    string[] array = xml2.Attr("tiles").Split(new char[]
                    {
                        ';'
                    });
                    for (int j = 0; j < array.Length; j++)
                    {
                        string[] array2 = array[j].Split(new char[]
                        {
                            ','
                        });
                        int x = int.Parse(array2[0]);
                        int y = int.Parse(array2[1]);
                        MTexture item = tileset[x, y];
                        tiles.Textures.Add(item);
                    }
                    if (xml2.HasAttr("sprites"))
                    {
                        foreach (string item2 in xml2.Attr("sprites").Split(new char[]
                        {
                            ','
                        }))
                        {
                            tiles.OverlapSprites.Add(item2);
                        }
                        tiles.HasOverlays = true;
                    }
                }
            }
            data.Masked.Sort(delegate (Masked a, Masked b)
            {
                int num2 = 0;
                int num3 = 0;
                for (int k = 0; k < 9; k++)
                {
                    if (a.Mask[k] == 2)
                    {
                        num2++;
                    }
                    if (b.Mask[k] == 2)
                    {
                        num3++;
                    }
                }
                return num2 - num3;
            });
        }

        public void LoadTileset(string filename)
        {
            Dictionary<char, XmlElement> dictionary = new Dictionary<char, XmlElement>();
            this.lookup = new Dictionary<char, TerrainType>();
            foreach (object obj in Calc.LoadContentXML(filename).GetElementsByTagName("Tileset"))
            {
                XmlElement xmlElement = (XmlElement)obj;
                char c = xmlElement.AttrChar("id");
                Tileset tileset = new Tileset(GFX.Game["tilesets/" + xmlElement.Attr("path")], 8, 8);
                TerrainType terrainType = new TerrainType(c);
                this.ReadInto(terrainType, tileset, xmlElement);
                if (xmlElement.HasAttr("copy"))
                {
                    char key = xmlElement.AttrChar("copy");
                    if (!dictionary.ContainsKey(key))
                    {
                        throw new Exception("Copied tilesets must be defined before the tilesets that copy them!");
                    }
                    this.ReadInto(terrainType, tileset, dictionary[key]);
                }
                if (xmlElement.HasAttr("ignores"))
                {
                    foreach (string text in xmlElement.Attr("ignores").Split(new char[]
                    {
                        ','
                    }))
                    {
                        if (text.Length > 0)
                        {
                            terrainType.Ignores.Add(text[0]);
                        }
                    }
                }
                dictionary.Add(c, xmlElement);
                this.lookup.Add(c, terrainType);
            }
        }

        private bool IsEmpty(char id)
        {
            return id == '0' || id == '\0';
        }

        private char GetTile(VirtualMap<char> mapData, int x, int y, Rectangle forceFill, char forceID, Autotiler.Behaviour behaviour)
        {
            if (forceFill.Contains(x, y))
            {
                return forceID;
            }
            if (mapData == null)
            {
                if (!behaviour.EdgesExtend)
                {
                    return '0';
                }
                return forceID;
            }
            else
            {
                if (x >= 0 && y >= 0 && x < mapData.Columns && y < mapData.Rows)
                {
                    return mapData[x, y];
                }
                if (!behaviour.EdgesExtend)
                {
                    return '0';
                }
                int x2 = Calc.Clamp(x, 0, mapData.Columns - 1);
                int y2 = Calc.Clamp(y, 0, mapData.Rows - 1);
                return mapData[x2, y2];
            }
        }

        private bool CheckTile(TerrainType set, VirtualMap<char> mapData, int x, int y, Rectangle forceFill, Autotiler.Behaviour behaviour)
        {
            if (forceFill.Contains(x, y))
            {
                return true;
            }
            if (mapData == null)
            {
                return behaviour.EdgesExtend;
            }
            if (x >= 0 && y >= 0 && x < mapData.Columns && y < mapData.Rows)
            {
                char c = mapData[x, y];
                return !this.IsEmpty(c) && !set.Ignore(c);
            }
            if (!behaviour.EdgesExtend)
            {
                return false;
            }
            char c2 = mapData[Calc.Clamp(x, 0, mapData.Columns - 1), Calc.Clamp(y, 0, mapData.Rows - 1)];
            return !this.IsEmpty(c2) && !set.Ignore(c2);
        }

        private bool CheckForSameLevel(int x1, int y1, int x2, int y2)
        {
            foreach (Rectangle rectangle in this.LevelBounds)
            {
                if (rectangle.Contains(x1, y1) && rectangle.Contains(x2, y2))
                {
                    return true;
                }
            }
            return false;
        }

        static LevelEditor()
        {
            LevelEditor.area = AreaKey.None;
        }

        private Tiles TileHandler(VirtualMap<char> mapData, int x, int y, Rectangle forceFill, char forceID, Autotiler.Behaviour behaviour)
        {
            char tile = this.GetTile(mapData, x, y, forceFill, forceID, behaviour);
            if (this.IsEmpty(tile))
            {
                return null;
            }
            TerrainType terrainType = this.lookup[tile];
            bool flag = true;
            int num = 0;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    bool flag2 = this.CheckTile(terrainType, mapData, x + j, y + i, forceFill, behaviour);
                    if (!flag2 && behaviour.EdgesIgnoreOutOfLevel && !this.CheckForSameLevel(x, y, x + j, y + i))
                    {
                        flag2 = true;
                    }
                    this.adjacent[num++] = (byte)(flag2 ? 1 : 0);
                    if (!flag2)
                    {
                        flag = false;
                    }
                }
            }
            if (!flag)
            {
                foreach (Masked masked in terrainType.Masked)
                {
                    bool flag3 = true;
                    int num2 = 0;
                    while (num2 < 9 && flag3)
                    {
                        if (masked.Mask[num2] != 2 && masked.Mask[num2] != this.adjacent[num2])
                        {
                            flag3 = false;
                        }
                        num2++;
                    }
                    if (flag3)
                    {
                        return masked.Tiles;
                    }
                }
                return null;
            }
            bool flag4;
            if (!behaviour.PaddingIgnoreOutOfLevel)
            {
                flag4 = (!this.CheckTile(terrainType, mapData, x - 2, y, forceFill, behaviour) || !this.CheckTile(terrainType, mapData, x + 2, y, forceFill, behaviour) || !this.CheckTile(terrainType, mapData, x, y - 2, forceFill, behaviour) || !this.CheckTile(terrainType, mapData, x, y + 2, forceFill, behaviour));
            }
            else
            {
                flag4 = ((!this.CheckTile(terrainType, mapData, x - 2, y, forceFill, behaviour) && this.CheckForSameLevel(x, y, x - 2, y)) || (!this.CheckTile(terrainType, mapData, x + 2, y, forceFill, behaviour) && this.CheckForSameLevel(x, y, x + 2, y)) || (!this.CheckTile(terrainType, mapData, x, y - 2, forceFill, behaviour) && this.CheckForSameLevel(x, y, x, y - 2)) || (!this.CheckTile(terrainType, mapData, x, y + 2, forceFill, behaviour) && this.CheckForSameLevel(x, y, x, y + 2)));
            }
            if (flag4)
            {
                return this.lookup[tile].Padded;
            }
            return this.lookup[tile].Center;
        }

        public LevelEditor(Session session) : base()
        {
            fiTexture = typeof(VirtualTexture).GetField("Texture", BindingFlags.NonPublic | BindingFlags.Instance);
            LoadTileset(Path.Combine("Graphics", "ForegroundTiles.xml"));

            Logger.Log("CLE", lookup['1'].Center.Textures.Count.ToString());
            Logger.Log("CLE", lookup['1'].Padded.Textures.Count.ToString());

            this.hovered = new Dictionary<string, LevelData>();

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

            adjacent = new byte[9];

            LevelBounds = new List<Rectangle>();

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
                this.LevelBounds.Add(new Rectangle(levelData.TileBounds.X - tileBounds.X, levelData.TileBounds.Y - tileBounds.Y, levelData.TileBounds.Width, levelData.TileBounds.Height));
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

        public bool Check(LevelData ld, Vector2 point)
        {
            int room_x = ld.Bounds.X;
            int room_y = ld.Bounds.Y;
            int room_w = ld.Bounds.Width;
            int room_h = (int)(Math.Ceiling(ld.Bounds.Height/8f)*8);
            //int room_w = (int)Math.Ceiling(ld.Bounds.Width / 8f);
            //int room_h = (int)Math.Ceiling(ld.Bounds.Height / 8f);
            return point.X >= (float)room_x && point.Y >= (float)room_y && point.X < (float)(room_x+room_w) && point.Y < (float)room_y+room_h;
        }

        public override void Update()
        {
            Vector2 value;
            value.X = (this.lastMouseScreenPosition.X - MInput.Mouse.Position.X) / LevelEditor.Camera.Zoom;
            value.Y = (this.lastMouseScreenPosition.Y - MInput.Mouse.Position.Y) / LevelEditor.Camera.Zoom;
            LevelEditor.Camera.Position += new Vector2((float)Input.MoveX.Value, (float)Input.MoveY.Value) * 600f * Engine.DeltaTime;
            this.UpdateMouse();

            hovered.Clear();

            foreach( LevelData levelData in mapData.Levels)
            {
                if (Check(levelData, mousePosition))
                    this.hovered.Add(levelData.Name, levelData);
            }

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

            if (  (MInput.Mouse.CheckLeftButton || MInput.Mouse.CheckRightButton))
            {
                int room_x = -1;
                int room_y = -1;
                if (hovered.Count == 1)
                {
                    foreach (LevelData levelData in hovered.Values)
                    {
                        room_x = (int)((this.mousePosition.X - levelData.Bounds.X) / 8f);
                        room_y = (int)((this.mousePosition.Y - levelData.Bounds.Y) / 8f);

                        if( MInput.Mouse.CheckLeftButton)
                        {                          
                            char[] charArray = levelData.Solids.ToCharArray();
                            int pos = (room_y * (int)(levelData.Bounds.Width/8)) + room_x;
                            charArray[pos] = tile;
                            levelData.Solids = new string(charArray);
                        }
                        if (MInput.Mouse.CheckRightButton)
                        {
                            char[] charArray = levelData.Bg.ToCharArray();
                            int pos = (room_y * (int)(levelData.Bounds.Width / 8)) + room_x;
                            charArray[pos] = tile;
                            levelData.Bg = new string(charArray);
                        }
                    }
                }


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

                        Autotiler.Behaviour behaviour = new Autotiler.Behaviour
                        {
                            EdgesExtend = true,
                            EdgesIgnoreOutOfLevel = false,
                            PaddingIgnoreOutOfLevel = true
                        };

                        for ( int i = -1; i <= 1; i++)
                        {
                            for( int j = -1; j <= 1; j++)
                            {
                                int _x = virtualX + i;
                                int _y = virtualY + j;

                                if (_x < 0) _x = 0;
                                if (_x > w) _x = w;
                                if (_y < 0) _y = 0;
                                if (_y > h) _y = h;

                                Tiles tiles = TileHandler(fgData, _x, _y, Rectangle.Empty, '0', behaviour);
                                if (tiles != null)
                                    fgTiles.Tiles.Tiles[_x, _y] = tiles.Textures[0];
                                //else
                                    //fgTiles.Tiles.Tiles[_x, _y] = null;
                            }
                        }

                        //TerrainType terrainType = lookup[tile];
                        //this.fgTiles.Tiles.Tiles[virtualX, virtualY] = Calc.Random.Choose(terrainType.Masked[1].Tiles.Textures);
                        //fiTexture.SetValue(this.fgTiles.Tiles.Tiles[virtualX, virtualY], lookup[tile].Padded.Textures[0]);
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

            if( fgDataUpdated && MInput.Mouse.ReleasedLeftButton)
            {
                Rectangle tileBounds = this.session.MapData.TileBounds;
                Vector2 position = new Vector2(tileBounds.X, tileBounds.Y) * 8f;
                fgDataUpdated = false;
                Calc.PushRandom(mapData.LoadSeed);
                this.fgTiles = new SolidTiles(position, fgData);
                fgTiles.Tiles.ClipCamera = LevelEditor.Camera;
                Calc.PopRandom();
            }
            if (bgDataUpdated && MInput.Mouse.ReleasedRightButton)
            {
                Rectangle tileBounds = this.session.MapData.TileBounds;
                Vector2 position = new Vector2(tileBounds.X, tileBounds.Y) * 8f;
                bgDataUpdated = false;
                Calc.PushRandom(mapData.LoadSeed);
                this.bgTiles = new BackgroundTiles(position, bgData);
                bgTiles.Tiles.ClipCamera = LevelEditor.Camera;
                Calc.PopRandom();
            }

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

            int room_x = -1;
            int room_y = -1;
            if (hovered.Count == 1)
            {
                foreach(LevelData levelData in hovered.Values)
                {
                    room_x = (int)((this.mousePosition.X - levelData.Bounds.X)/8f);
                    room_y = (int)((this.mousePosition.Y - levelData.Bounds.Y)/8f);
                }
            }

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, LevelEditor.Camera.Matrix * Engine.ScreenMatrix);

            float width = 1920 / LevelEditor.Camera.Zoom;
            float height = 1080 / LevelEditor.Camera.Zoom;

            Draw.Line(0f, LevelEditor.Camera.Top, 0f, LevelEditor.Camera.Top + height, Color.DarkSlateBlue);
            Draw.Line(LevelEditor.Camera.Left, 0f, LevelEditor.Camera.Left + width, 0f, Color.DarkSlateBlue);

            bgTiles.Tiles.Render();
            fgTiles.Tiles.Render();

            foreach (LevelData ld in mapData.Levels)
            {
                Draw.HollowRect((float)ld.Position.X, (float)ld.Position.Y, (float)(ld.TileBounds.Width * 8), (float)(ld.TileBounds.Height * 8), Calc.HexToColor("ffffff"));
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

            ActiveFont.Draw("RX: " + room_x.ToString(), new Vector2(16, 64), Color.Red);
            ActiveFont.Draw("RY: " + room_y.ToString(), new Vector2(16, 96), Color.Red);


            int roomListOffset = 0;
            foreach( string roomName in hovered.Keys)
            {

                ActiveFont.Draw("R:" + roomName, new Vector2(16, 128+(roomListOffset*32)), Color.Red);
                roomListOffset++;
            }

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
