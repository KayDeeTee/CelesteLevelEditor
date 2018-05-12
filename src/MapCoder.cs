using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace CelesteLevelEditor
{
    public static class MapCoder
    {
        private static readonly HashSet<string> IgnoreAttributes = new HashSet<string> { "_eid", "_package" };
        private static string InnerTextAttributeName = "innerText";
        private static string OutputFileExtension = ".bin";

        public static void ToBinary(string filename, string outdir = null)
        {
            XmlDocument mapData = new XmlDocument();
            mapData.Load(filename);

            int children = mapData.ChildNodes.Count;
            for (int i = 0; i < children; i++)
            {
                XmlElement root = mapData.ChildNodes[i] as XmlElement;
                if (root != null)
                {
                    ToBinary(root, string.IsNullOrEmpty(outdir) ? Path.ChangeExtension(filename, OutputFileExtension) : outdir);
                    return;
                }
            }
        }
        public static void ToBinary(XmlElement rootElement, string outfilename)
        {
            Dictionary<string, short> ids = new Dictionary<string, short>();
            CreateLookupTable(ids, rootElement);
            AddLookupValue(ids, InnerTextAttributeName);

            using (FileStream fileStream = new FileStream(outfilename, FileMode.Create))
            {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                binaryWriter.Write("CELESTE MAP");
                binaryWriter.Write(Path.GetFileNameWithoutExtension(outfilename));
                binaryWriter.Write((short)ids.Count);

                foreach (KeyValuePair<string, short> keyValuePair in ids)
                {
                    binaryWriter.Write(keyValuePair.Key);
                }

                WriteElement(ids, binaryWriter, rootElement);
                binaryWriter.Flush();
            }
        }
        private static void CreateLookupTable(Dictionary<string, short> ids, XmlElement element)
        {
            AddLookupValue(ids, element.Name);

            int count = element.Attributes.Count;
            for (int i = 0; i < count; i++)
            {
                XmlAttribute attribute = element.Attributes[i];
                if (IgnoreAttributes.Contains(attribute.Name)) { continue; }

                AddLookupValue(ids, attribute.Name);

                ParseValue(attribute.Value, out byte type, out object value);
                if (type == 5)
                {
                    AddLookupValue(ids, attribute.Value);
                }
            }

            int children = element.ChildNodes.Count;
            for (int i = 0; i < children; i++)
            {
                XmlElement node = element.ChildNodes[i] as XmlElement;
                if (node != null)
                {
                    CreateLookupTable(ids, node);
                }
            }
        }
        private static void AddLookupValue(Dictionary<string, short> ids, string name)
        {
            if (!ids.ContainsKey(name))
            {
                ids.Add(name, (short)ids.Count);
            }
        }
        private static void WriteElement(Dictionary<string, short> ids, BinaryWriter writer, XmlElement element)
        {
            int children = 0;
            for (int i = element.ChildNodes.Count - 1; i >= 0; i--)
            {
                if (element.ChildNodes[i] is XmlElement)
                {
                    children++;
                }
            }

            int attributes = 0;
            for (int i = element.Attributes.Count - 1; i >= 0; i--)
            {
                if (!IgnoreAttributes.Contains(element.Attributes[i].Name))
                {
                    attributes++;
                }
            }

            if (element.InnerText.Length > 0 && children == 0)
            {
                attributes++;
            }

            writer.Write(ids[element.Name]);
            writer.Write((byte)attributes);

            int count = element.Attributes.Count;
            for (int i = 0; i < count; i++)
            {
                XmlAttribute attribute = element.Attributes[i];

                if (IgnoreAttributes.Contains(attribute.Name)) { continue; }

                ParseValue(attribute.Value, out byte type, out object result);
                writer.Write(ids[attribute.Name]);
                writer.Write(type);

                switch (type)
                {
                    case 0: writer.Write((bool)result); break;
                    case 1: writer.Write((byte)result); break;
                    case 2: writer.Write((short)result); break;
                    case 3: writer.Write((int)result); break;
                    case 4: writer.Write((float)result); break;
                    case 5: writer.Write(ids[(string)result]); break;
                }
            }

            if (element.InnerText.Length > 0 && children == 0)
            {
                writer.Write(ids[InnerTextAttributeName]);
                if (element.Name == "solids" || element.Name == "bg")
                {
                    byte[] encoded = RunLengthEncoding.Encode(element.InnerText);
                    writer.Write((byte)7);
                    writer.Write((short)encoded.Length);
                    writer.Write(encoded);
                }
                else {
                    writer.Write((byte)6);
                    writer.Write(element.InnerText);
                }
            }

            writer.Write((short)children);
            children = element.ChildNodes.Count;
            for (int i = 0; i < children; i++)
            {
                XmlElement node = element.ChildNodes[i] as XmlElement;
                if (node != null)
                {
                    WriteElement(ids, writer, node);
                }
            }
        }
        private static void ParseValue(string value, out byte type, out object result)
        {
            if (bool.TryParse(value, out bool boolVal))
            {
                type = 0;
                result = boolVal;
            }
            else if (byte.TryParse(value, out byte byteVal))
            {
                type = 1;
                result = byteVal;
            }
            else if (short.TryParse(value, out short shortVal))
            {
                type = 2;
                result = shortVal;
            }
            else if (int.TryParse(value, out int intVal))
            {
                type = 3;
                result = intVal;
            }
            else if (float.TryParse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float floatVal))
            {
                type = 4;
                result = floatVal;
            }
            else {
                type = 5;
                result = value;
            }
        }
        public static void ToXML(string filename, string outfilename)
        {
            MapElement element = FromBinary(filename);
            XmlDocument doc = new XmlDocument();
            WriteXML(element, doc, doc);
            doc.Save(outfilename);
        }
        public static XmlDocument ToXML(string filename)
        {
            MapElement element = FromBinary(filename);
            XmlDocument doc = new XmlDocument();
            WriteXML(element, doc, doc);
            return doc;
        }
        public static void WriteXML(MapElement element, XmlDocument doc, XmlNode node)
        {
            XmlElement xml = doc.CreateElement(element.Name);
            foreach (KeyValuePair<string, object> pair in element.Attributes)
            {
                if (pair.Key == InnerTextAttributeName)
                {
                    xml.InnerText = pair.Value.ToString();
                }
                else {
                    XmlAttribute attr = doc.CreateAttribute(pair.Key);
                    attr.Value = pair.Value.ToString();
                    xml.Attributes.Append(attr);
                }
            }

            int count = element.Children.Count;
            for (int i = 0; i < count; i++)
            {
                MapElement child = element.Children[i];
                WriteXML(child, doc, xml);
            }

            node.AppendChild(xml);
        }
        public static MapElement FromBinary(string filename)
        {
            MapElement element;
            using (FileStream fileStream = File.OpenRead(filename))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                binaryReader.ReadString();

                string package = binaryReader.ReadString();
                int strings = binaryReader.ReadInt16();

                string[] lookupTable = new string[strings];
                for (int i = 0; i < strings; i++)
                {
                    lookupTable[i] = binaryReader.ReadString();
                }

                element = ReadElement(binaryReader, null, lookupTable);
                element.Attributes.Add("_package", package);
            }
            return element;
        }
        private static MapElement ReadElement(BinaryReader reader, MapElement parent, string[] lookupTable)
        {
            MapElement element = new MapElement();
            element.Name = lookupTable[reader.ReadInt16()];
            element.Parent = parent;
            int attributes = reader.ReadByte();
            for (int i = 0; i < attributes; i++)
            {
                string key = lookupTable[reader.ReadInt16()];
                byte type = reader.ReadByte();
                object value = null;
                if (type == 0)
                {
                    value = reader.ReadBoolean();
                }
                else if (type == 1)
                {
                    value = (int)reader.ReadByte();
                }
                else if (type == 2)
                {
                    value = (int)reader.ReadInt16();
                }
                else if (type == 3)
                {
                    value = reader.ReadInt32();
                }
                else if (type == 4)
                {
                    value = reader.ReadSingle();
                }
                else if (type == 5)
                {
                    value = lookupTable[reader.ReadInt16()];
                }
                else if (type == 6)
                {
                    value = reader.ReadString();
                }
                else if (type == 7)
                {
                    int count = reader.ReadInt16();
                    value = RunLengthEncoding.Decode(reader.ReadBytes(count));
                }
                element.Attributes.Add(key, value);
            }

            int elements = reader.ReadInt16();
            for (int j = 0; j < elements; j++)
            {
                element.Children.Add(ReadElement(reader, element, lookupTable));
            }
            return element;
        }
    }
}