using System.Collections.Generic;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>Text as UTF-8 bytes and back, written out here so the bytes are the same on every machine.</summary>
    public static class Utf8
    {
        /// <summary>The UTF-8 bytes of <paramref name="text" />; a lone surrogate is written as U+FFFD.</summary>
        public static byte[] Bytes(string text)
        {
            var bytes = new List<byte>(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                int code = text[i];
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    code = char.ConvertToUtf32(text[i], text[++i]);
                else if (char.IsSurrogate(text[i]))
                    code = 0xFFFD;

                if (code < 0x80)
                {
                    bytes.Add((byte)code);
                }
                else if (code < 0x800)
                {
                    bytes.Add((byte)(0xC0 | (code >> 6)));
                    bytes.Add((byte)(0x80 | (code & 0x3F)));
                }
                else if (code < 0x10000)
                {
                    bytes.Add((byte)(0xE0 | (code >> 12)));
                    bytes.Add((byte)(0x80 | ((code >> 6) & 0x3F)));
                    bytes.Add((byte)(0x80 | (code & 0x3F)));
                }
                else
                {
                    bytes.Add((byte)(0xF0 | (code >> 18)));
                    bytes.Add((byte)(0x80 | ((code >> 12) & 0x3F)));
                    bytes.Add((byte)(0x80 | ((code >> 6) & 0x3F)));
                    bytes.Add((byte)(0x80 | (code & 0x3F)));
                }
            }

            return bytes.ToArray();
        }

        /// <summary>The text <paramref name="bytes" /> hold in UTF-8, or null where they are not UTF-8.</summary>
        public static string Text(byte[] bytes)
        {
            var text = new StringBuilder(bytes.Length);
            var i = 0;
            while (i < bytes.Length)
            {
                int first = bytes[i++];
                int more, code;
                if (first < 0x80)
                {
                    more = 0;
                    code = first;
                }
                else if ((first & 0xE0) == 0xC0)
                {
                    more = 1;
                    code = first & 0x1F;
                }
                else if ((first & 0xF0) == 0xE0)
                {
                    more = 2;
                    code = first & 0x0F;
                }
                else if ((first & 0xF8) == 0xF0)
                {
                    more = 3;
                    code = first & 0x07;
                }
                else
                {
                    return null;
                }

                for (; more > 0; more--)
                {
                    if (i >= bytes.Length || (bytes[i] & 0xC0) != 0x80)
                        return null;

                    code = (code << 6) | (bytes[i++] & 0x3F);
                }

                if (code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF))
                    return null;

                text.Append(char.ConvertFromUtf32(code));
            }

            return text.ToString();
        }
    }
}
