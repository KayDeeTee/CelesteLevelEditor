using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteLevelEditor
{
    class CustomKeyboardKey : VirtualButton.KeyboardKey
    {
        public CustomKeyboardKey(Keys key) : base(key) { }

        public override void Update()
        {
            base.Update();
            if (this.Key == Keys.F10 && this.Pressed)
            {
                Engine.Scene = new LevelEditor(CelesteLevelEditor.session);
            }
        }
    }
}