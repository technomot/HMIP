using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Archiver
{
    public class CodecResult
    {
        public long InputSize { get; set; }
        public long OutputSize { get; set; }
        public double Entropy { get; set; }
        public bool IntegrityOk { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public static class ArithmeticCodec
    {
        private static readonly byte[] Signature = { 0x41, 0x52, 0x43, 0x31 };
        private const ulong Top = 0xFFFFFFFF;
        private const ulong FirstQtr = 0x40000000;
        private const ulong Half = 0x80000000;
        private const ulong ThirdQtr = 0xC0000000;
        private const int MaxTotal = 65535;
        private const int BufferSize = 1 << 16;

        public static CodecResult Compress(string source, string target, IProgress<int> progress = null)
        {
            var timer = Stopwatch.StartNew();
            long length = new FileInfo(source).Length;
            long[] counts = new long[256];
            byte[] hash;

            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                byte[] buffer = new byte[BufferSize];
                long done = 0;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++) counts[buffer[i]]++;
                    sha.AppendData(buffer, 0, read);
                    done += read;
                    progress?.Report((int)(done * 10 / length));
                }
                hash = sha.GetHashAndReset();
            }

            ushort[] freq = ScaleCounts(counts, length);
            uint[] cum = new uint[257];
            for (int i = 0; i < 256; i++) cum[i + 1] = cum[i] + freq[i];
            uint total = cum[256];

            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
            using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
            {
                var writer = new BinaryWriter(output);
                writer.Write(Signature);
                writer.Write(length);
                writer.Write(hash);
                writer.Write((ushort)freq.Count(f => f > 0));
                for (int i = 0; i < 256; i++)
                {
                    if (freq[i] == 0) continue;
                    writer.Write((byte)i);
                    writer.Write(freq[i]);
                }
                writer.Flush();

                if (length > 0)
                {
                    var encoder = new Encoder(new BitWriter(output));
                    byte[] buffer = new byte[BufferSize];
                    long done = 0;
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            int s = buffer[i];
                            encoder.Encode(cum[s], cum[s + 1], total);
                        }
                        done += read;
                        progress?.Report(10 + (int)(done * 90 / length));
                    }
                    encoder.Finish();
                }
            }

            progress?.Report(100);
            timer.Stop();

            return new CodecResult
            {
                InputSize = length,
                OutputSize = new FileInfo(target).Length,
                Entropy = Entropy(counts, length),
                IntegrityOk = true,
                Elapsed = timer.Elapsed
            };
        }

        public static CodecResult Decompress(string source, string target, IProgress<int> progress = null)
        {
            var timer = Stopwatch.StartNew();
            long length;
            bool ok;

            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
            {
                var reader = new BinaryReader(input);
                if (!reader.ReadBytes(4).SequenceEqual(Signature))
                    throw new InvalidDataException("The file is not an archive created by this program.");

                length = reader.ReadInt64();
                byte[] expected = reader.ReadBytes(32);
                int distinct = reader.ReadUInt16();

                ushort[] freq = new ushort[256];
                for (int i = 0; i < distinct; i++)
                {
                    int symbol = reader.ReadByte();
                    freq[symbol] = reader.ReadUInt16();
                }

                uint[] cum = new uint[257];
                for (int i = 0; i < 256; i++) cum[i + 1] = cum[i] + freq[i];
                uint total = cum[256];

                if (length < 0 || (length > 0 && total == 0))
                    throw new InvalidDataException("The archive header is damaged.");

                byte[] lookup = new byte[Math.Max(total, 1)];
                for (int s = 0; s < 256; s++)
                    for (uint c = cum[s]; c < cum[s + 1]; c++)
                        lookup[c] = (byte)s;

                using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
                using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    byte[] buffer = new byte[BufferSize];
                    int pos = 0;

                    if (length > 0)
                    {
                        var decoder = new Decoder(new BitReader(input));
                        for (long n = 0; n < length; n++)
                        {
                            uint c = decoder.Target(total);
                            if (c >= total) throw new InvalidDataException("The archive data is damaged.");
                            int s = lookup[c];
                            decoder.Consume(cum[s], cum[s + 1], total);

                            buffer[pos++] = (byte)s;
                            if (pos == buffer.Length)
                            {
                                output.Write(buffer, 0, pos);
                                sha.AppendData(buffer, 0, pos);
                                pos = 0;
                                progress?.Report((int)(n * 100 / length));
                            }
                        }
                    }

                    if (pos > 0)
                    {
                        output.Write(buffer, 0, pos);
                        sha.AppendData(buffer, 0, pos);
                    }

                    ok = sha.GetHashAndReset().SequenceEqual(expected);
                }
            }

            progress?.Report(100);
            timer.Stop();

            return new CodecResult
            {
                InputSize = new FileInfo(source).Length,
                OutputSize = length,
                IntegrityOk = ok,
                Elapsed = timer.Elapsed
            };
        }

        private static ushort[] ScaleCounts(long[] counts, long total)
        {
            var result = new ushort[256];
            if (total <= MaxTotal)
            {
                for (int i = 0; i < 256; i++) result[i] = (ushort)counts[i];
                return result;
            }

            long budget = MaxTotal - 256;
            for (int i = 0; i < 256; i++)
            {
                if (counts[i] == 0) continue;
                result[i] = (ushort)Math.Max(1, counts[i] * budget / total);
            }
            return result;
        }

        private static double Entropy(long[] counts, long total)
        {
            if (total == 0) return 0;
            double h = 0;
            foreach (long c in counts)
            {
                if (c == 0) continue;
                double p = (double)c / total;
                h -= p * Math.Log2(p);
            }
            return h;
        }

        private sealed class Encoder
        {
            private readonly BitWriter bits;
            private ulong low = 0;
            private ulong high = Top;
            private int pending = 0;

            public Encoder(BitWriter bits)
            {
                this.bits = bits;
            }

            public void Encode(uint cumLow, uint cumHigh, uint total)
            {
                ulong range = high - low + 1;
                high = low + range * cumHigh / total - 1;
                low = low + range * cumLow / total;

                while (true)
                {
                    if (high < Half)
                    {
                        Emit(0);
                    }
                    else if (low >= Half)
                    {
                        Emit(1);
                        low -= Half;
                        high -= Half;
                    }
                    else if (low >= FirstQtr && high < ThirdQtr)
                    {
                        pending++;
                        low -= FirstQtr;
                        high -= FirstQtr;
                    }
                    else break;

                    low <<= 1;
                    high = (high << 1) | 1;
                }
            }

            public void Finish()
            {
                pending++;
                Emit(low < FirstQtr ? 0 : 1);
                bits.Finish();
            }

            private void Emit(int bit)
            {
                bits.Write(bit);
                for (; pending > 0; pending--) bits.Write(1 - bit);
            }
        }

        private sealed class Decoder
        {
            private readonly BitReader bits;
            private ulong low = 0;
            private ulong high = Top;
            private ulong value = 0;

            public Decoder(BitReader bits)
            {
                this.bits = bits;
                for (int i = 0; i < 32; i++) value = (value << 1) | (uint)bits.Read();
            }

            public uint Target(uint total)
            {
                ulong range = high - low + 1;
                return (uint)(((value - low + 1) * total - 1) / range);
            }

            public void Consume(uint cumLow, uint cumHigh, uint total)
            {
                ulong range = high - low + 1;
                high = low + range * cumHigh / total - 1;
                low = low + range * cumLow / total;

                while (true)
                {
                    ulong shift;
                    if (high < Half) shift = 0;
                    else if (low >= Half) shift = Half;
                    else if (low >= FirstQtr && high < ThirdQtr) shift = FirstQtr;
                    else break;

                    value -= shift;
                    low -= shift;
                    high -= shift;

                    low <<= 1;
                    high = (high << 1) | 1;
                    value = (value << 1) | (uint)bits.Read();
                }
            }
        }

        private sealed class BitWriter
        {
            private readonly Stream stream;
            private readonly byte[] buffer = new byte[BufferSize];
            private int pos = 0;
            private int current = 0;
            private int filled = 0;

            public BitWriter(Stream stream)
            {
                this.stream = stream;
            }

            public void Write(int bit)
            {
                current = (current << 1) | bit;
                if (++filled < 8) return;

                buffer[pos++] = (byte)current;
                current = 0;
                filled = 0;

                if (pos == buffer.Length)
                {
                    stream.Write(buffer, 0, pos);
                    pos = 0;
                }
            }

            public void Finish()
            {
                if (filled > 0)
                {
                    buffer[pos++] = (byte)(current << (8 - filled));
                    current = 0;
                    filled = 0;
                }
                stream.Write(buffer, 0, pos);
                pos = 0;
            }
        }

        private sealed class BitReader
        {
            private readonly Stream stream;
            private readonly byte[] buffer = new byte[BufferSize];
            private int size = 0;
            private int pos = 0;
            private int current = 0;
            private int left = 0;

            public BitReader(Stream stream)
            {
                this.stream = stream;
            }

            public int Read()
            {
                if (left == 0)
                {
                    if (pos == size)
                    {
                        size = stream.Read(buffer, 0, buffer.Length);
                        pos = 0;
                        if (size == 0) return 0;
                    }
                    current = buffer[pos++];
                    left = 8;
                }
                left--;
                return (current >> left) & 1;
            }
        }
    }
}