using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteLevelEditor
{
    class GUIElement
    {
        protected MTexture texture;
        protected Vector2 pos;
        protected Vector2 offset;
        protected Vector2 size;
        protected Vector2 scale;
        protected List<GUIElement> children;
        protected bool blocks;
        protected bool selectable;

        public void Add(GUIElement elem)
        {
            children.Add(elem);
            elem.Move(this);
        }

        public void Remove(GUIElement elem)
        {
            children.Remove(elem);
        }

        public void Move(GUIElement parent)
        {
            pos.X = parent.pos.X + this.offset.X;
            pos.Y = parent.pos.Y + this.offset.Y;
            foreach (GUIElement child in children)
            {
                Move(this);
            }
        }

        public void Move(Vector2 offset)
        {
            this.pos = this.pos - this.offset + offset;
            this.offset = offset;
            foreach (GUIElement child in children)
            {
                child.Move(this);
            }
        }

        public virtual bool OnClick(Vector2 vector)
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
                if (!blocked && selectable)
                    LevelEditor.selected = this;
            }
            return blocked;
        }

        public GUIElement(Vector2 offset, Vector2 size, Vector2 scale, MTexture tex, bool selectable, bool blockClickThrough)
        {
            children = new List<GUIElement>();
            this.offset = offset;
            this.pos = new Vector2(offset.X, offset.Y);
            this.size = size;
            this.scale = scale;
            this.texture = tex;
            //this.OnClick = OnClick;
            this.selectable = selectable;
            this.blocks = blockClickThrough;
        }

        public virtual void Update()
        {
            foreach(GUIElement child in children)
            {
                child.Update();
            }
        }

        public virtual void Render()
        {
            texture.Draw(pos, new Vector2(0, 0), Color.White, scale);

            foreach(GUIElement child in children)
            {
                child.Render();
            }
            //if(t == TYPE.TileButton)
                //LevelEditor.fgtileicons[id].Draw(new Vector2(x+8, y+8), new Vector2(0, 0), Color.White, 2f, 0f, SpriteEffects.None);
        }

    }
}
