using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>Packs a script's <see cref="TokenCode" /> bytes into as few bytes as it can, and unpacks them again.</summary>
    /// <remarks>
    /// The packed bytes are the count of bytes, seven bits a byte, then the bytes coded by a <see cref="ContextModel" /> that
    /// starts knowing nothing.
    /// </remarks>
    public static class BytePacker
    {
        // A blueprint is a few kilobytes; a count past this is a damaged text, refused before any work is done.
        const int Longest = 1 << 20;

        /// <summary>The packed form of <paramref name="bytes" />.</summary>
        public static byte[] Pack(byte[] bytes)
        {
            var packed = new List<byte>();
            var length = bytes.Length;
            do
            {
                var low = length & 0x7F;
                length >>= 7;
                packed.Add((byte)(length != 0 ? low | 0x80 : low));
            } while (length != 0);

            var model = new ContextModel();
            var coder = new RangeEncoder();
            foreach (var symbol in bytes)
                model.Encode(coder, symbol);

            packed.AddRange(coder.Finish());
            return packed.ToArray();
        }

        /// <summary>The bytes <paramref name="packed" /> holds, or null where it is damaged.</summary>
        public static byte[] Unpack(byte[] packed)
        {
            var length = 0;
            var at = 0;
            for (var shift = 0;; shift += 7)
            {
                if (at >= packed.Length || shift > 21)
                    return null;

                var next = packed[at++];
                length |= (next & 0x7F) << shift;
                if ((next & 0x80) == 0)
                    break;
            }

            if (length > Longest)
                return null;

            var model = new ContextModel();
            var coder = new RangeDecoder(packed, at);
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                bytes[i] = model.Decode(coder);
                if (coder.IsOverrun)
                    return null;
            }

            return bytes;
        }
    }
}
