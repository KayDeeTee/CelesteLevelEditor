using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;

namespace CelesteLevelEditor
{
    class TileButton : GUIElement
    {
        public int id;
        public MTexture selected;
        public MTexture unselected;

        public override bool OnClick(Vector2 vector)
        {
            bool blocked = false;
            if (vector.X > pos.X && vector.X < pos.X + size.X && vector.Y > pos.Y && vector.Y < pos.Y + size.Y)
            {
                foreach (GUIElement child in children)
                {
                    blocked = child.OnClick(vector);
                    if (blocked)
                        break;
                }
                if (!blocked)
                {
                    LevelEditor.tile = LevelEditor.fgtilebuttons[this.id];
                    LevelEditor.selected = this;
                }
            }
            return blocked;
        }

        public override void Render()
        {
            if( LevelEditor.selected == this )
                selected.Draw(pos, new Vector2(0, 0), Color.White, scale);
            else
                unselected.Draw(pos, new Vector2(0, 0), Color.White, scale);
            LevelEditor.fgtileicons[id].Draw(pos+new Vector2(8,8), new Vector2(0, 0), Color.White, scale);
            foreach (GUIElement child in children)
            {
                child.Render();
            }
        }

        public TileButton(int id, Vector2 offset, Vector2 size, Vector2 scale, MTexture sel, MTexture unsel, bool selectable, bool blockClickThrough) : base(offset, size, scale, unsel, selectable, blockClickThrough)
        {
            this.id = id;
            this.selected = sel;
            this.unselected = unsel;
        }
    }
}
