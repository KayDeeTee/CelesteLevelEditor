using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CelesteLevelEditor
{
    public class MapElement
    {
        public string Name;
        public Dictionary<string, object> Attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public List<MapElement> Children = new List<MapElement>();
        public MapElement Parent;

        public List<MapElement> Select(params string[] paths)
        {
            if (paths == null || paths.Length == 0) { return Children; }

            List<MapElement> elements = new List<MapElement>();
            SelectChildren(elements, paths[0]);

            int count = paths.Length;
            for (int i = 1; i < count; i++)
            {
                string path = paths[i];
                int length = elements.Count;
                for (int j = 0; j < length; j++)
                {
                    MapElement element = elements[0];
                    elements.RemoveAt(0);
                    element.SelectChildren(elements, path);
                }
            }
            return elements;
        }
        public List<MapElement> SelectChildren(string path)
        {
            List<MapElement> elements = new List<MapElement>();
            SelectChildren(elements, path);
            return elements;
        }
        private void SelectChildren(List<MapElement> elements, string path)
        {
            int count = Children.Count;
            for (int i = 0; i < count; i++)
            {
                MapElement child = Children[i];
                if (child.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    elements.Add(child);
                }
            }
        }
        public MapElement SelectFirst(params string[] paths)
        {
            if (paths == null || paths.Length == 0) { return Children.Count == 0 ? null : Children[0]; }

            MapElement element = null;
            int count = paths.Length;
            for (int i = 0; i < count; i++)
            {
                string path = paths[i];

                SelectFirst(path, i == 0 ? this : element, out element);
                if (element == null) { return null; }
            }
            return element;
        }
        private void SelectFirst(string path, MapElement elementOn, out MapElement element)
        {
            int count = elementOn.Children.Count;
            for (int i = 0; i < count; i++)
            {
                MapElement child = elementOn.Children[i];
                if (child.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    element = child;
                    return;
                }
            }
            element = null;
        }
        public MapElement SelectParent(string path)
        {
            if (string.IsNullOrEmpty(path)) { return Parent; }

            MapElement element = Parent;
            while (element != null)
            {
                if (element.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }

                element = element.Parent;
            }
            return element;
        }
        public bool HasAttr(string name)
        {
            return Attributes.ContainsKey(name);
        }
        public string Attr(string name, string defaultValue = "")
        {
            object obj;
            if (!Attributes.TryGetValue(name, out obj))
            {
                return defaultValue;
            }
            return obj.ToString();
        }
        public bool AttrBool(string name, bool defaultValue = false)
        {
            object obj;
            if (!Attributes.TryGetValue(name, out obj))
            {
                obj = defaultValue;
            }
            if (obj is bool)
            {
                return (bool)obj;
            }
            return bool.Parse(obj.ToString());
        }
        public float AttrFloat(string name, float defaultValue = 0f)
        {
            object obj;
            if (!Attributes.TryGetValue(name, out obj))
            {
                obj = defaultValue;
            }
            if (obj is float)
            {
                return (float)obj;
            }
            return float.Parse(obj.ToString(), CultureInfo.InvariantCulture);
        }
        public int AttrInt(string name, int defaultValue = 0)
        {
            object obj;
            if (!Attributes.TryGetValue(name, out obj))
            {
                obj = defaultValue;
            }
            if (obj is int)
            {
                return (int)obj;
            }
            return int.Parse(obj.ToString(), CultureInfo.InvariantCulture);
        }
        public char AttrChar(string name, char defaultValue = '\0')
        {
            object obj;
            if (!Attributes.TryGetValue(name, out obj))
            {
                obj = defaultValue;
            }
            if (obj is char)
            {
                return (char)obj;
            }
            string val = obj.ToString();
            return val.Length == 0 ? defaultValue : val[0];
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> pair in Attributes)
            {
                sb.Append(pair.Key).Append(' ').Append(pair.Value).Append(' ');
            }
            return $"{Name} {sb.ToString()}";
        }
    }
}