using System;
using System.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>Primitive values and vectors written to and read from byte arrays, little-endian.</summary>
    /// <remarks>Safe to call from any thread.</remarks>
    public static class ByteCodec
    {
        static readonly double[] DoubleBuffer = new double[1];
        static readonly float[] FloatBuffer = new float[1];
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, false);
        static readonly char[] CharBuffer = new char[1024];

        /// <summary>Writes a 32-bit unsigned integer, little-endian.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 4.</param>
        public static void Write(uint value, byte[] data, ref int pos)
        {
            // Byte by byte rather than through BitConverter, so the order is little-endian on any machine.
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>Reads a 32-bit unsigned integer, little-endian.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 4 on success.</param>
        /// <param name="value">The value, or 0 on failure.</param>
        /// <returns>Whether 4 bytes were there.</returns>
        public static bool TryReadUInt32(byte[] data, ref int pos, out uint value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0;
                return false;
            }

            value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return true;
        }

        /// <summary>Writes a 32-bit signed integer, little-endian.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 4.</param>
        public static void Write(int value, byte[] data, ref int pos)
        {
            // Byte by byte rather than through BitConverter, so the order is little-endian on any machine.
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>Writes a 64-bit signed integer, little-endian.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 8.</param>
        public static void Write(long value, byte[] data, ref int pos)
        {
            // Byte by byte rather than through BitConverter, so the order is little-endian on any machine.
            data[pos++] = (byte)(value & 0xFF);
            data[pos++] = (byte)((value >> 8) & 0xFF);
            data[pos++] = (byte)((value >> 16) & 0xFF);
            data[pos++] = (byte)((value >> 24) & 0xFF);
            data[pos++] = (byte)((value >> 32) & 0xFF);
            data[pos++] = (byte)((value >> 40) & 0xFF);
            data[pos++] = (byte)((value >> 48) & 0xFF);
            data[pos++] = (byte)((value >> 56) & 0xFF);
        }

        /// <summary>Reads a 32-bit signed integer, little-endian.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 4 on success.</param>
        /// <param name="value">The value, or 0 on failure.</param>
        /// <returns>Whether 4 bytes were there.</returns>
        public static bool TryReadInt32(byte[] data, ref int pos, out int value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0;
                return false;
            }

            value = data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24);
            pos += 4;
            return true;
        }

        /// <summary>Reads a 64-bit signed integer, little-endian.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 8 on success.</param>
        /// <param name="value">The value, or 0 on failure.</param>
        /// <returns>Whether 8 bytes were there.</returns>
        public static bool TryReadInt64(byte[] data, ref int pos, out long value)
        {
            if (pos + 8 > data.Length)
            {
                value = 0;
                return false;
            }

            value = data[pos]
                    | ((long)data[pos + 1] << 8)
                    | ((long)data[pos + 2] << 16)
                    | ((long)data[pos + 3] << 24)
                    | ((long)data[pos + 4] << 32)
                    | ((long)data[pos + 5] << 40)
                    | ((long)data[pos + 6] << 48)
                    | ((long)data[pos + 7] << 56);
            pos += 8;
            return true;
        }

        /// <summary>Writes a 32-bit float.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 4.</param>
        public static void Write(float value, byte[] data, ref int pos)
        {
            lock (FloatBuffer)
            {
                FloatBuffer[0] = value;
                Buffer.BlockCopy(FloatBuffer, 0, data, pos, 4);
            }

            pos += 4;
        }

        /// <summary>Reads a 32-bit float.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 4 on success.</param>
        /// <param name="value">The value, or 0 on failure.</param>
        /// <returns>Whether 4 bytes were there.</returns>
        public static bool TryReadFloat(byte[] data, ref int pos, out float value)
        {
            if (pos + 4 > data.Length)
            {
                value = 0f;
                return false;
            }

            lock (FloatBuffer)
            {
                Buffer.BlockCopy(data, pos, FloatBuffer, 0, 4);
                value = FloatBuffer[0];
            }

            pos += 4;
            return true;
        }

        /// <summary>Writes a 64-bit float.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 8.</param>
        public static void Write(double value, byte[] data, ref int pos)
        {
            lock (DoubleBuffer)
            {
                DoubleBuffer[0] = value;
                Buffer.BlockCopy(DoubleBuffer, 0, data, pos, 8);
            }

            pos += 8;
        }

        /// <summary>Reads a 64-bit float.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 8 on success.</param>
        /// <param name="value">The value, or 0 on failure.</param>
        /// <returns>Whether 8 bytes were there.</returns>
        public static bool TryReadDouble(byte[] data, ref int pos, out double value)
        {
            if (pos + 8 > data.Length)
            {
                value = 0.0;
                return false;
            }

            lock (DoubleBuffer)
            {
                Buffer.BlockCopy(data, pos, DoubleBuffer, 0, 8);
                value = DoubleBuffer[0];
            }

            pos += 8;
            return true;
        }

        /// <summary>Writes a vector of 32-bit floats, X, Y then Z.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 12.</param>
        public static void Write(Vector3 value, byte[] data, ref int pos)
        {
            Write(value.X, data, ref pos);
            Write(value.Y, data, ref pos);
            Write(value.Z, data, ref pos);
        }

        /// <summary>Reads a vector of 32-bit floats, X, Y then Z.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 12 on success.</param>
        /// <param name="value">The vector, or <see cref="Vector3.Zero" /> on failure.</param>
        /// <returns>Whether 12 bytes were there.</returns>
        public static bool TryReadVector3(byte[] data, ref int pos, out Vector3 value)
        {
            value = Vector3.Zero;
            float x, y, z;
            if (!TryReadFloat(data, ref pos, out x))
                return false;
            if (!TryReadFloat(data, ref pos, out y))
                return false;
            if (!TryReadFloat(data, ref pos, out z))
                return false;
            value = new Vector3(x, y, z);
            return true;
        }

        /// <summary>Writes a vector of 64-bit floats, X, Y then Z.</summary>
        /// <param name="value">The value.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 24.</param>
        public static void Write(Vector3D value, byte[] data, ref int pos)
        {
            Write(value.X, data, ref pos);
            Write(value.Y, data, ref pos);
            Write(value.Z, data, ref pos);
        }

        /// <summary>Reads a vector of 64-bit floats, X, Y then Z.</summary>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 24 on success.</param>
        /// <param name="value">The vector, or <see cref="Vector3D.Zero" /> on failure.</param>
        /// <returns>Whether 24 bytes were there.</returns>
        public static bool TryReadVector3D(byte[] data, ref int pos, out Vector3D value)
        {
            value = Vector3D.Zero;
            double x, y, z;
            if (!TryReadDouble(data, ref pos, out x))
                return false;
            if (!TryReadDouble(data, ref pos, out y))
                return false;
            if (!TryReadDouble(data, ref pos, out z))
                return false;
            value = new Vector3D(x, y, z);
            return true;
        }

        /// <summary>Writes the text of a <see cref="StringBuilder" /> as UTF-8 after its length in bytes.</summary>
        /// <remarks>Allocates nothing.</remarks>
        /// <param name="value">The text.</param>
        /// <param name="data">The destination.</param>
        /// <param name="pos">Where to write; moved on by 4 and the byte length.</param>
        public static void WriteString(StringBuilder value, byte[] data, ref int pos)
        {
            if (value == null || value.Length == 0)
            {
                Write(0, data, ref pos);
                return;
            }

            var charCount = value.Length;

            var totalByteCount = 0;
            lock (CharBuffer)
            {
                var offset = 0;
                while (offset < charCount)
                {
                    var chunkSize = Math.Min(CharBuffer.Length, charCount - offset);
                    value.CopyTo(offset, CharBuffer, 0, chunkSize);
                    totalByteCount += Utf8.GetByteCount(CharBuffer, 0, chunkSize);
                    offset += chunkSize;
                }

                Write(totalByteCount, data, ref pos);

                offset = 0;
                while (offset < charCount)
                {
                    var chunkSize = Math.Min(CharBuffer.Length, charCount - offset);
                    value.CopyTo(offset, CharBuffer, 0, chunkSize);
                    var bytesWritten = Utf8.GetBytes(CharBuffer, 0, chunkSize, data, pos);
                    pos += bytesWritten;
                    offset += chunkSize;
                }
            }
        }

        /// <summary>Reads UTF-8 text after its length in bytes, appending it to a <see cref="StringBuilder" />.</summary>
        /// <remarks>Allocates no string.</remarks>
        /// <param name="data">The source.</param>
        /// <param name="pos">Where to read; moved on by 4 and the byte length on success.</param>
        /// <param name="value">Appended to.</param>
        /// <returns>Whether the text was read.</returns>
        public static bool TryReadString(byte[] data, ref int pos, StringBuilder value)
        {
            if (value == null)
                return false;

            int byteCount;
            if (!TryReadInt32(data, ref pos, out byteCount))
                return false;

            if (byteCount < 0)
                return false;

            if (byteCount == 0)
                return true;

            if (pos + byteCount > data.Length)
                return false;

            lock (CharBuffer)
            {
                var bytesProcessed = 0;
                while (bytesProcessed < byteCount)
                {
                    var bytesRemaining = byteCount - bytesProcessed;
                    var bytesToProcess = Math.Min(bytesRemaining, CharBuffer.Length * 3);
                    var charsDecoded = Utf8.GetChars(data, pos + bytesProcessed, bytesToProcess, CharBuffer, 0);
                    if (value.Capacity < value.Length + charsDecoded)
                        value.Capacity = value.Length + charsDecoded;
                    value.Append(CharBuffer, 0, charsDecoded);
                    var bytesConsumed = Utf8.GetByteCount(CharBuffer, 0, charsDecoded);
                    bytesProcessed += bytesConsumed;
                }
            }

            pos += byteCount;
            return true;
        }

        /// <summary>Writes a string as UTF-8 after its length in bytes, or -1 for null.</summary>
        public static void WriteString(string value, byte[] data, ref int pos)
        {
            if (value == null)
            {
                Write(-1, data, ref pos);
                return;
            }

            if (value.Length == 0)
            {
                Write(0, data, ref pos);
                return;
            }

            var bytes = Utf8.GetBytes(value);
            Write(bytes.Length, data, ref pos);
            Array.Copy(bytes, 0, data, pos, bytes.Length);
            pos += bytes.Length;
        }

        /// <summary>Reads a UTF-8 string after its length in bytes, null where the length is -1.</summary>
        public static bool TryReadString(byte[] data, ref int pos, out string value)
        {
            value = null;
            int byteCount;
            if (!TryReadInt32(data, ref pos, out byteCount))
                return false;

            if (byteCount < 0)
                return true;

            if (byteCount == 0)
            {
                value = string.Empty;
                return true;
            }

            if (pos + byteCount > data.Length)
                return false;

            value = Utf8.GetString(data, pos, byteCount);
            pos += byteCount;
            return true;
        }

        /// <summary>How many bytes <paramref name="value" /> takes written: its 4-byte length and its UTF-8 bytes.</summary>
        public static int SizeOfString(string value)
        {
            if (value == null || value.Length == 0)
                return 4;
            return 4 + Utf8.GetByteCount(value);
        }
    }
}