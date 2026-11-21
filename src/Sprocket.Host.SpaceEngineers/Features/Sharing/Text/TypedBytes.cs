using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>
    /// Bytes as characters that paste anywhere as they are, nearly 8 bits to a character: the 255 Braille patterns from U+2801,
    /// which spell nothing in any language, which no Unicode normalization changes, and which a chat counts as one character
    /// each. The blank pattern, U+2800, is left out, as some places treat it as a space.
    /// </summary>
    /// <remarks>
    /// Part of the shared text's format. The bytes, after a leading 1 so none is lost, are one number written in base 255,
    /// most significant digit first. The game keeps characters its font cannot draw: Custom Data shows them as boxes.
    /// </remarks>
    public static class TypedBytes
    {
        const int First = 0x2801;
        const int Base = 255;

        /// <summary>The characters <paramref name="bytes" /> are written as.</summary>
        public static string Write(byte[] bytes)
        {
            var number = new List<byte>(bytes.Length + 1) { 1 };
            number.AddRange(bytes);
            var digits = new List<char>();
            while (number.Count > 0)
            {
                var remainder = 0;
                var quotient = new List<byte>(number.Count);
                foreach (var b in number)
                {
                    var value = (remainder << 8) | b;
                    var digit = value / Base;
                    remainder = value % Base;
                    if (quotient.Count > 0 || digit > 0)
                        quotient.Add((byte)digit);
                }

                digits.Add((char)(First + remainder));
                number = quotient;
            }

            digits.Reverse();
            return new string(digits.ToArray());
        }

        /// <summary>The bytes <paramref name="text" /> holds, or null where a character is not one of the 255.</summary>
        public static byte[] Read(string text)
        {
            var number = new List<byte>();
            foreach (var c in text)
            {
                var digit = c - First;
                if (digit < 0 || digit >= Base)
                    return null;

                var carry = digit;
                for (var i = number.Count - 1; i >= 0; i--)
                {
                    var value = number[i] * Base + carry;
                    number[i] = (byte)value;
                    carry = value >> 8;
                }

                while (carry > 0)
                {
                    number.Insert(0, (byte)carry);
                    carry >>= 8;
                }
            }

            if (number.Count == 0 || number[0] != 1)
                return null;

            number.RemoveAt(0);
            return number.ToArray();
        }
    }
}
