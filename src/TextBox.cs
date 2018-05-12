using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteLevelEditor
{
    class TextBox : GUIElement
    {
        public string currentText;
        KeyboardState currentState;
        KeyboardState oldState;
        float barCounter;
        bool bar;
        Keys? repeatKey;
        float repeatCounter;

        public PixelFont pixelFont;

        public TextBox(Vector2 offset, Vector2 size, Vector2 scale, MTexture unsel, bool selectable, bool blockClickThrough) : base(offset, size, scale, unsel, selectable, blockClickThrough)
        {
            Logger.Log("CLE", Dialog.Language.FontFace);
            pixelFont = Fonts.Faces[Dialog.Language.FontFace];
            currentText = "N/A";
        }

        public override void Update()
        {
            base.Update();
            if (LevelEditor.selected == this)
            {
                this.oldState = this.currentState;
                this.currentState = Keyboard.GetState();
                this.barCounter += Engine.DeltaTime;
                while (this.barCounter >= 0.5f)
                {
                    this.barCounter -= 0.5f;
                    this.bar = !this.bar;
                }
                if (this.repeatKey != null)
                {
                    if (this.currentState[this.repeatKey.Value] == KeyState.Down)
                    {
                        this.repeatCounter += Engine.DeltaTime;
                        while (this.repeatCounter >= 0.5f)
                        {
                            this.HandleKey(this.repeatKey.Value);
                            this.repeatCounter -= 0.0333333351f;
                        }
                    }
                    else
                    {
                        this.repeatKey = null;
                    }
                }
                foreach (Keys key in this.currentState.GetPressedKeys())
                {
                    if (this.oldState[key] == KeyState.Up)
                    {
                        this.HandleKey(key);
                        return;
                    }
                }
            }
        }

        public override void Render()
        {
            //Vector2 vector4 = vector3 + new Vector2(x, (float)(-32 + (this.Exists ? 0 : 64)));
            
            //float n = Math.Min(1f, 440f / ActiveFont.Measure(this.currentText).X);
            float n = Math.Min(1f, 440f / pixelFont.Get(48f).Measure(this.currentText).X);
            Draw.Rect(pos, size.X, size.Y, Color.White);
            if (this.bar && LevelEditor.selected == this)
            {
                //Draw.SpriteBatch.DrawString(Draw.DefaultFont, this.currentText + "|", pos, Color.Black * 0.8f);
                pixelFont.Draw(48f, currentText + "|", pos, new Vector2(0f, 0f), Vector2.One * n, Color.Black * 0.8f, 0f, Color.Transparent, 0f, Color.Transparent);
                //ActiveFont.Draw(this.currentText + "|", pos, new Vector2(0f, 0f), Vector2.One * n, Color.Black * 0.8f);
            }
            else
            {
                //Draw.SpriteBatch.DrawString(Draw.DefaultFont, this.currentText + "|", pos, Color.Black * 0.8f);
                pixelFont.Draw(48f, currentText, pos, new Vector2(0f, 0f), Vector2.One * n, Color.Black * 0.8f, 0f, Color.Transparent, 0f, Color.Transparent);
                //ActiveFont.Draw(this.currentText, pos, new Vector2(0f, 0f), Vector2.One * n, Color.Black * 0.8f);
            }

            foreach (GUIElement child in children)
            {
                child.Render();
            }
        }

        private void HandleKey(Keys key)
        {
            if (key != Keys.OemTilde && key != Keys.Oem8 && key != Keys.Enter && this.repeatKey != key)
            {
                this.repeatKey = new Keys?(key);
                this.repeatCounter = 0f;
            }
            if (key <= Keys.Enter)
            {
                if (key != Keys.Back)
                {
                    if (key != Keys.Tab)
                    {
                        if (key == Keys.Enter)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (this.currentText.Length > 0)
                    {
                        this.currentText = this.currentText.Substring(0, this.currentText.Length - 1);
                        return;
                    }
                    return;
                }
            }
            else
            {
                if (key > Keys.F12)
                {
                    switch (key)
                    {
                        case Keys.OemSemicolon:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += ":";
                                return;
                            }
                            this.currentText += ";";
                            return;
                        case Keys.OemPlus:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "+";
                                return;
                            }
                            this.currentText += "=";
                            return;
                        case Keys.OemComma:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "<";
                                return;
                            }
                            this.currentText += ",";
                            return;
                        case Keys.OemMinus:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "_";
                                return;
                            }
                            this.currentText += "-";
                            return;
                        case Keys.OemPeriod:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += ">";
                                return;
                            }
                            this.currentText += ".";
                            return;
                        case Keys.OemQuestion:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "?";
                                return;
                            }
                            this.currentText += "/";
                            return;
                        case Keys.OemTilde:
                            break;
                        case Keys.OemOpenBrackets:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "{";
                                return;
                            }
                            this.currentText += "[";
                            return;
                        case Keys.OemCloseBrackets:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "}";
                                return;
                            }
                            this.currentText += "]";
                            return;
                        case Keys.OemQuotes:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "\"";
                                return;
                            }
                            this.currentText += "'";
                            return;
                        case Keys.Oem8:
                            break;
                        case Keys.OemBackslash:
                            if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                            {
                                this.currentText += "|";
                                return;
                            }
                            this.currentText += "\\";
                            return;
                        default:
                            break;
                    }
                    return;
                }
                switch (key)
                {
                    case Keys.Space:
                        this.currentText += "";
                        return;
                    case Keys.PageUp:
                    case Keys.PageDown:
                    case Keys.End:
                    case Keys.Home:
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Select:
                    case Keys.Print:
                    case Keys.Execute:
                    case Keys.PrintScreen:
                    case Keys.Insert:
                    case Keys.Help:
                        break;
                    case Keys.Up:
                        return;
                    case Keys.Down:
                        return;
                    case Keys.Delete:
                        this.currentText = "";
                        return;
                    case Keys.D0:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += ")";
                            return;
                        }
                        this.currentText += "0";
                        return;
                    case Keys.D1:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "!";
                            return;
                        }
                        this.currentText += "1";
                        return;
                    case Keys.D2:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "@";
                            return;
                        }
                        this.currentText += "2";
                        return;
                    case Keys.D3:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "#";
                            return;
                        }
                        this.currentText += "3";
                        return;
                    case Keys.D4:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "$";
                            return;
                        }
                        this.currentText += "4";
                        return;
                    case Keys.D5:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "%";
                            return;
                        }
                        this.currentText += "5";
                        return;
                    case Keys.D6:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "^";
                            return;
                        }
                        this.currentText += "6";
                        return;
                    case Keys.D7:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "&";
                            return;
                        }
                        this.currentText += "7";
                        return;
                    case Keys.D8:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "*";
                            return;
                        }
                        this.currentText += "8";
                        return;
                    case Keys.D9:
                        if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                        {
                            this.currentText += "(";
                            return;
                        }
                        this.currentText += "9";
                        return;
                    default:
                        break;
                }
            }
            if (key.ToString().Length == 1)
            {
                if (this.currentState[Keys.LeftShift] == KeyState.Down || this.currentState[Keys.RightShift] == KeyState.Down)
                {
                    this.currentText += key.ToString();
                    return;
                }
                this.currentText += key.ToString().ToLower();
                return;
            }
        }
    }
}
