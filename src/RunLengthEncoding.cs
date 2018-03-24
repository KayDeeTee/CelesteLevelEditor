using System.Collections.Generic;
using System.Text;

namespace CelesteLevelEditor
{
    public static class RunLengthEncoding
    {
        public static byte[] Encode(string str)
        {
            List<byte> list = new List<byte>();
            for (int i = 0; i < str.Length; i++)
            {
                byte count = 1;
                char value = str[i];
                while (i + 1 < str.Length && str[i + 1] == value && count < 255)
                {
                    count++;
                    i++;
                }
                list.Add(count);
                list.Add((byte)value);
            }
            return list.ToArray();
        }
        public static string Decode(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
            {
                byte count = bytes[i];
                char value = (char)bytes[i + 1];
                stringBuilder.Append(value, count);
            }
            return stringBuilder.ToString();
        }
    }
}