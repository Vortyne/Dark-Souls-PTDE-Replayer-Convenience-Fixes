// Minimal readers/writers for Dark Souls PTDE GameParam files.
//
// Supports exactly what DS1 PTDE uses:
//   * BND3 archives (uncompressed, IDs + names, as in GameParam.parambnd / paramdef.paramdefbnd)
//   * PARAMDEF version 0x68 (field layouts)
//   * PARAM files with 32-bit row offsets and Shift-JIS row names
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParamPatcher
{
    static class Bin
    {
        public static readonly Encoding ShiftJis = Encoding.GetEncoding(932);

        public static int I32(byte[] d, int o) => BitConverter.ToInt32(d, o);
        public static uint U32(byte[] d, int o) => BitConverter.ToUInt32(d, o);
        public static ushort U16(byte[] d, int o) => BitConverter.ToUInt16(d, o);

        public static void Put(byte[] d, int o, int v) => BitConverter.GetBytes(v).CopyTo(d, o);
        public static void Put(byte[] d, int o, uint v) => BitConverter.GetBytes(v).CopyTo(d, o);
        public static void Put(byte[] d, int o, ushort v) => BitConverter.GetBytes(v).CopyTo(d, o);

        public static byte[] CStr(byte[] d, int o)
        {
            int end = Array.IndexOf(d, (byte)0, o);
            return Slice(d, o, end - o);
        }

        public static string FixStr(byte[] d, int o, int len)
        {
            int end = o;
            while (end < o + len && d[end] != 0) end++;
            return ShiftJis.GetString(d, o, end - o);
        }

        public static byte[] Slice(byte[] d, int o, int len)
        {
            var r = new byte[len];
            Buffer.BlockCopy(d, o, r, 0, len);
            return r;
        }
    }

    // ------------------------------------------------------------------- DCX

    /// <summary>DCX "DFLT" compression (zlib), as used by Dark Souls Remastered.</summary>
    static class Dcx
    {
        const int HeaderSize = 0x4C; // up to and including "DCA\0" + its size; zlib data follows

        public static bool IsDcx(byte[] d) => d.Length >= 4 && d[0] == 'D' && d[1] == 'C' && d[2] == 'X' && d[3] == 0;

        static int BE(byte[] d, int o) => (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];
        static void PutBE(byte[] d, int o, uint v) { d[o] = (byte)(v >> 24); d[o + 1] = (byte)(v >> 16); d[o + 2] = (byte)(v >> 8); d[o + 3] = (byte)v; }

        /// <summary>Returns the decompressed payload and the original header (reused when compressing).</summary>
        public static (byte[] data, byte[] header) Decompress(byte[] d)
        {
            if (Encoding.ASCII.GetString(d, 0x28, 4) != "DFLT")
                throw new InvalidDataException("Unsupported DCX compression (expected DFLT)");
            int size = BE(d, 0x1C), compressed = BE(d, 0x20);
            // Skip the 2-byte zlib header; DeflateStream reads the raw deflate data.
            using (var input = new MemoryStream(d, HeaderSize + 2, compressed - 2))
            using (var inflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
            {
                var output = new byte[size];
                int read = 0;
                while (read < size)
                {
                    int n = inflate.Read(output, read, size - read);
                    if (n == 0) throw new InvalidDataException("DCX data ended early");
                    read += n;
                }
                return (output, Bin.Slice(d, 0, HeaderSize));
            }
        }

        public static byte[] Compress(byte[] data, byte[] header)
        {
            var z = new MemoryStream();
            z.WriteByte(0x78); z.WriteByte(0xDA); // zlib header, as in the game's files
            using (var deflate = new System.IO.Compression.DeflateStream(z, System.IO.Compression.CompressionLevel.Optimal, true))
                deflate.Write(data, 0, data.Length);
            uint a = 1, b = 0; // Adler-32 trailer
            foreach (byte x in data) { a = (a + x) % 65521; b = (b + a) % 65521; }
            var adler = new byte[4];
            PutBE(adler, 0, (b << 16) | a);
            z.Write(adler, 0, 4);
            var output = new byte[HeaderSize + z.Length];
            header.CopyTo(output, 0);
            PutBE(output, 0x1C, (uint)data.Length);
            PutBE(output, 0x20, (uint)z.Length);
            z.ToArray().CopyTo(output, HeaderSize);
            return output;
        }
    }

    // ------------------------------------------------------------------- BND3

    class BndFile
    {
        public uint Flags;
        public int Id;
        public byte[] Name;
        public byte[] Data;
    }

    class Bnd3
    {
        const int EntrySize = 24; // flags, size, offset, id, nameOffset, uncompressedSize
        readonly byte[] header;
        public readonly List<BndFile> Files = new List<BndFile>();

        public Bnd3(byte[] data)
        {
            if (Dcx.IsDcx(data)) data = Dcx.Decompress(data).data;
            if (data.Length < 0x20 || Encoding.ASCII.GetString(data, 0, 4) != "BND3")
                throw new InvalidDataException("Not a BND3 archive");
            if (data[0xC] != 0x74 || data[0xD] != 0)
                throw new InvalidDataException("Unsupported BND3 format (expected DS1 PTDE, little endian)");
            int count = Bin.I32(data, 0x10);
            header = Bin.Slice(data, 0, 0x20);
            for (int i = 0; i < count; i++)
            {
                int o = 0x20 + i * EntrySize;
                int size = Bin.I32(data, o + 4), offset = Bin.I32(data, o + 8);
                if (size != Bin.I32(data, o + 20))
                    throw new InvalidDataException("Compressed BND3 entries are not supported");
                Files.Add(new BndFile
                {
                    Flags = Bin.U32(data, o),
                    Id = Bin.I32(data, o + 12),
                    Name = Bin.CStr(data, Bin.I32(data, o + 16)),
                    Data = Bin.Slice(data, offset, size),
                });
            }
        }

        public static Bnd3 Load(string path) => new Bnd3(File.ReadAllBytes(path));

        public string NameOf(int index) => Bin.ShiftJis.GetString(Files[index].Name);

        public byte[] ToBytes()
        {
            int count = Files.Count;
            int namesStart = 0x20 + count * EntrySize;
            var names = new MemoryStream();
            var nameOffsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                nameOffsets[i] = namesStart + (int)names.Length;
                names.Write(Files[i].Name, 0, Files[i].Name.Length);
                names.WriteByte(0);
            }
            int headersEnd = namesStart + (int)names.Length;
            int total = headersEnd;
            var offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                total += (0x10 - total % 0x10) % 0x10;
                offsets[i] = total;
                total += Files[i].Data.Length;
            }
            var output = new byte[total];
            header.CopyTo(output, 0);
            Bin.Put(output, 0x10, count);
            Bin.Put(output, 0x14, headersEnd);
            names.ToArray().CopyTo(output, namesStart);
            for (int i = 0; i < count; i++)
            {
                int o = 0x20 + i * EntrySize;
                var f = Files[i];
                Bin.Put(output, o, f.Flags);
                Bin.Put(output, o + 4, f.Data.Length);
                Bin.Put(output, o + 8, offsets[i]);
                Bin.Put(output, o + 12, f.Id);
                Bin.Put(output, o + 16, nameOffsets[i]);
                Bin.Put(output, o + 20, f.Data.Length);
                f.Data.CopyTo(output, offsets[i]);
            }
            return output;
        }
    }

    // ------------------------------------------------------------------- PARAMDEF

    /// <summary>A field value: a number (ints and f32 are both exact as double) or a dummy8 byte array.</summary>
    struct Value : IEquatable<Value>
    {
        public readonly double Num;
        public readonly byte[] Bytes;

        public Value(double num) { Num = num; Bytes = null; }
        public Value(byte[] bytes) { Num = 0; Bytes = bytes; }

        public bool Equals(Value o) =>
            Bytes == null ? o.Bytes == null && Num.Equals(o.Num)
                          : o.Bytes != null && Bytes.SequenceEqual(o.Bytes);
        public override bool Equals(object obj) => obj is Value v && Equals(v);
        public override int GetHashCode() => Bytes == null ? Num.GetHashCode() : Bytes.Length;
    }

    class Field
    {
        public string Name;      // internal name without :bits / [count]
        public string Type;      // s8 u8 s16 u16 s32 u32 f32 dummy8
        public int Offset;
        public int Bits;         // 0 = not a bitfield
        public int BitOffset;
        public int Count = 1;    // >1 only for dummy8 arrays

        public static int SizeOf(string type)
        {
            switch (type)
            {
                case "s8": case "u8": case "dummy8": return 1;
                case "s16": case "u16": return 2;
                case "s32": case "u32": case "f32": return 4;
                default: return -1;
            }
        }

        bool IsArray => Type == "dummy8" && Count > 1;
        public bool IsPadding => Type == "dummy8";

        long ReadUnit(byte[] row)
        {
            switch (Type)
            {
                case "s8": return (sbyte)row[Offset];
                case "u8": case "dummy8": return row[Offset];
                case "s16": return BitConverter.ToInt16(row, Offset);
                case "u16": return BitConverter.ToUInt16(row, Offset);
                case "s32": return BitConverter.ToInt32(row, Offset);
                case "u32": return BitConverter.ToUInt32(row, Offset);
                default: throw new InvalidOperationException(Type);
            }
        }

        void WriteUnit(byte[] row, long v)
        {
            switch (Type)
            {
                case "s8": case "u8": case "dummy8": row[Offset] = (byte)v; break;
                case "s16": case "u16": BitConverter.GetBytes((ushort)v).CopyTo(row, Offset); break;
                case "s32": case "u32": BitConverter.GetBytes((uint)v).CopyTo(row, Offset); break;
                default: throw new InvalidOperationException(Type);
            }
        }

        public Value Read(byte[] row)
        {
            if (IsArray) return new Value(Bin.Slice(row, Offset, Count));
            if (Type == "f32") return new Value(BitConverter.ToSingle(row, Offset));
            long v = ReadUnit(row);
            if (Bits > 0) v = (v >> BitOffset) & ((1L << Bits) - 1);
            return new Value(v);
        }

        public void Write(byte[] row, Value value)
        {
            if (IsArray) { value.Bytes.CopyTo(row, Offset); return; }
            if (Type == "f32") { BitConverter.GetBytes((float)value.Num).CopyTo(row, Offset); return; }
            long v = (long)value.Num;
            if (Bits > 0)
            {
                long mask = ((1L << Bits) - 1) << BitOffset;
                v = (ReadUnit(row) & ~mask) | ((v << BitOffset) & mask);
            }
            WriteUnit(row, v);
        }

        /// <summary>Parse a CSV cell (DSMapStudio style) into this field's value.</summary>
        public Value Parse(string text)
        {
            text = text.Trim();
            var inv = CultureInfo.InvariantCulture;
            if (IsArray)
            {
                var parts = text.Trim('[', ']').Split('|').Where(p => p != "").ToArray();
                if (parts.Length != Count)
                    throw new FormatException($"expected {Count} values for {Name}, got '{text}'");
                return new Value(parts.Select(p => byte.Parse(p, inv)).ToArray());
            }
            if (Type == "dummy8") text = text.Trim('[', ']');
            if (Type == "f32")
                return new Value(float.Parse(text, NumberStyles.Float, inv)); // rounded through f32
            if (!long.TryParse(text, NumberStyles.Integer, inv, out long n))
                throw new FormatException($"'{text}' is not a valid number for {Name}");
            return new Value(n);
        }

        public string Format(Value value)
        {
            var inv = CultureInfo.InvariantCulture;
            if (value.Bytes != null) return "[" + string.Join("|", value.Bytes) + "]";
            if (Type != "f32") return ((long)value.Num).ToString(inv);
            float f = (float)value.Num;
            for (int digits = 1; digits <= 9; digits++)
            {
                string s = f.ToString("G" + digits, inv);
                if (float.Parse(s, inv) != f) continue;
                // "G" switches to exponent form when there are few digits (70 -> "7E+01"); write it out.
                if (s.IndexOf('E') >= 0 && Math.Abs(f) < 1e15f && Math.Abs(f) > 1e-15f)
                    s = decimal.Parse(s, NumberStyles.Float, inv).ToString(inv);
                return s;
            }
            return f.ToString("R", inv);
        }
    }

    class ParamDef
    {
        static readonly Regex NameRe = new Regex(@"^\s*(\w+?)\s*(?::\s*(\d+))?\s*(?:\[\s*(\d+)\s*\])?\s*$");

        public readonly string ParamType;
        public readonly List<Field> Fields = new List<Field>();
        public readonly int RowSize;

        public ParamDef(byte[] data)
        {
            int headerSize = Bin.U16(data, 4), fieldCount = Bin.U16(data, 8), fieldSize = Bin.U16(data, 10);
            ParamType = Bin.FixStr(data, 0xC, 0x20);
            if (Bin.U16(data, 0x2E) != 0x68) throw new InvalidDataException("Unsupported PARAMDEF version");
            int offset = 0;
            Field bitUnit = null; // first field of the bitfield unit being filled
            int bitsUsed = 0;
            for (int i = 0; i < fieldCount; i++)
            {
                int b = headerSize + i * fieldSize;
                string type = Bin.FixStr(data, b + 0x40, 8); // 0x6C holds the enum/display type
                string internalName = Bin.FixStr(data, b + 0x8C, 0x20);
                int byteCount = Bin.I32(data, b + 0x64);
                var m = NameRe.Match(internalName);
                int size = Field.SizeOf(type);
                if (!m.Success || size < 0)
                    throw new InvalidDataException($"Unsupported field '{type} {internalName}'");
                string name = m.Groups[1].Value;
                if (m.Groups[2].Success)
                {
                    int bits = int.Parse(m.Groups[2].Value);
                    if (bitUnit == null || Field.SizeOf(bitUnit.Type) != size || bitsUsed + bits > size * 8)
                    {
                        bitUnit = new Field { Type = type, Offset = offset };
                        bitsUsed = 0;
                        offset += size;
                    }
                    Fields.Add(new Field { Name = name, Type = type, Offset = bitUnit.Offset, Bits = bits, BitOffset = bitsUsed });
                    bitsUsed += bits;
                    continue;
                }
                bitUnit = null;
                int count = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 1;
                Fields.Add(new Field { Name = name, Type = type, Offset = offset, Count = type == "dummy8" ? count : 1 });
                offset += byteCount;
            }
            RowSize = offset;
        }

        /// <summary>Returns {param type: ParamDef} from a paramdef.paramdefbnd.</summary>
        public static Dictionary<string, ParamDef> LoadAll(byte[] bnd)
        {
            var defs = new Dictionary<string, ParamDef>();
            foreach (var f in new Bnd3(bnd).Files)
            {
                var d = new ParamDef(f.Data);
                defs[d.ParamType] = d;
            }
            return defs;
        }
    }

    // ------------------------------------------------------------------- PARAM

    class Row
    {
        public int Id;
        public byte[] Name;  // raw Shift-JIS, or null
        public byte[] Data;
    }

    class Param
    {
        const int HeaderSize = 0x30;
        readonly byte[] header;
        readonly int tableGap; // some banks (format 1) leave a gap between the row table and the data
        Dictionary<int, Row> index;

        public readonly string ParamType;
        public readonly List<Row> Rows = new List<Row>();
        public ParamDef Def { get; private set; }
        public int RowSize { get; private set; }

        public Param(byte[] data)
        {
            if (data[0x2C] != 0 || (data[0x2D] != 1 && data[0x2D] != 2))
                throw new InvalidDataException("Unsupported PARAM format (expected DS1 PTDE)");
            int dataStart = Bin.U16(data, 4), rowCount = Bin.U16(data, 10);
            header = Bin.Slice(data, 0, HeaderSize);
            ParamType = Bin.FixStr(data, 0xC, 0x20);
            tableGap = dataStart - (HeaderSize + 12 * rowCount);
            var entries = Enumerable.Range(0, rowCount)
                .Select(i => HeaderSize + 12 * i)
                .Select(o => (id: Bin.I32(data, o), dataOff: Bin.I32(data, o + 4), nameOff: Bin.I32(data, o + 8)))
                .ToList();
            var offsets = entries.Select(e => e.dataOff).Distinct().OrderBy(o => o).ToList();
            RowSize = offsets.Count > 1 ? offsets[1] - offsets[0] : -1;
            if (RowSize < 0) { pendingData = data; pendingOffsets = entries.Select(e => e.dataOff).ToList(); }
            foreach (var e in entries)
                Rows.Add(new Row
                {
                    Id = e.id,
                    Name = e.nameOff != 0 ? Bin.CStr(data, e.nameOff) : null,
                    Data = Bin.Slice(data, e.dataOff, Math.Max(RowSize, 0)),
                });
        }

        // Single-row params (several in Remastered) can't tell their row size until the paramdef is known.
        byte[] pendingData;
        List<int> pendingOffsets;

        /// <summary>Attach the layout; reads the row data of single-row params now that the size is known.</summary>
        public void Bind(ParamDef def)
        {
            Def = def;
            if (RowSize < 0)
            {
                RowSize = def.RowSize;
                for (int i = 0; i < Rows.Count; i++)
                    Rows[i].Data = Bin.Slice(pendingData, pendingOffsets[i], RowSize);
                pendingData = null;
                pendingOffsets = null;
            }
            else if (RowSize != def.RowSize)
                throw new InvalidDataException($"{ParamType}: row size {RowSize} != paramdef {def.RowSize}");
        }

        public Row Find(int id)
        {
            if (index == null)
            {
                index = new Dictionary<int, Row>();
                foreach (var r in Rows)
                    if (!index.ContainsKey(r.Id)) index[r.Id] = r;
            }
            return index.TryGetValue(id, out var row) ? row : null;
        }

        public Row AddRow(int id, byte[] name)
        {
            var row = new Row { Id = id, Name = name, Data = new byte[RowSize] };
            int pos = Rows.Count;
            while (pos > 0 && Rows[pos - 1].Id > id) pos--;
            Rows.Insert(pos, row);
            index = null;
            return row;
        }

        public byte[] ToBytes()
        {
            int count = Rows.Count;
            int dataStart = HeaderSize + 12 * count + tableGap;
            int namesStart = dataStart + count * RowSize;
            var names = new MemoryStream();
            var nameOffsets = new Dictionary<string, int>(); // identical names are stored once
            var table = new byte[12 * count];
            for (int i = 0; i < count; i++)
            {
                var row = Rows[i];
                int nameOff = 0;
                if (row.Name != null)
                {
                    string key = Convert.ToBase64String(row.Name);
                    if (!nameOffsets.TryGetValue(key, out nameOff))
                    {
                        nameOff = nameOffsets[key] = namesStart + (int)names.Length;
                        names.Write(row.Name, 0, row.Name.Length);
                        names.WriteByte(0);
                    }
                }
                Bin.Put(table, 12 * i, row.Id);
                Bin.Put(table, 12 * i + 4, dataStart + i * RowSize);
                Bin.Put(table, 12 * i + 8, nameOff);
            }
            var output = new byte[namesStart + names.Length + 2]; // trailing pad, as SoulsFormats writes
            header.CopyTo(output, 0);
            Bin.Put(output, 0, namesStart);
            Bin.Put(output, 4, (ushort)dataStart);
            Bin.Put(output, 10, (ushort)count);
            table.CopyTo(output, HeaderSize);
            for (int i = 0; i < count; i++)
                Rows[i].Data.CopyTo(output, dataStart + i * RowSize);
            names.ToArray().CopyTo(output, namesStart);
            return output;
        }
    }

    /// <summary>A loaded GameParam.parambnd (PTDE) or GameParam.parambnd.dcx (Remastered),
    /// with every PARAM parsed and bound to its paramdef.</summary>
    class GameParam
    {
        readonly Bnd3 bnd;
        readonly byte[] dcxHeader; // set when the file was DCX-compressed; saved the same way
        readonly Dictionary<string, int> fileIndex = new Dictionary<string, int>();
        public readonly Dictionary<string, Param> Params = new Dictionary<string, Param>();

        public bool IsDcx => dcxHeader != null;

        public GameParam(byte[] data, Dictionary<string, ParamDef> defs)
        {
            if (Dcx.IsDcx(data)) (data, dcxHeader) = Dcx.Decompress(data);
            bnd = new Bnd3(data);
            for (int i = 0; i < bnd.Files.Count; i++)
            {
                string shortName = Path.GetFileNameWithoutExtension(bnd.NameOf(i).Replace('\\', '/').Split('/').Last());
                var param = new Param(bnd.Files[i].Data);
                if (!defs.TryGetValue(param.ParamType, out var def))
                    throw new InvalidDataException($"No paramdef for {shortName} ({param.ParamType})");
                param.Bind(def);
                Params[shortName] = param;
                fileIndex[shortName] = i;
            }
        }

        /// <summary>Case-insensitive lookup by short param name, e.g. "AtkParam_PC".</summary>
        public (string shortName, Param param) Get(string name)
        {
            foreach (var kv in Params)
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return (kv.Key, kv.Value);
            return (null, null);
        }

        public byte[] ToBytes()
        {
            foreach (var kv in Params)
                bnd.Files[fileIndex[kv.Key]].Data = kv.Value.ToBytes();
            var bytes = bnd.ToBytes();
            return IsDcx ? Dcx.Compress(bytes, dcxHeader) : bytes;
        }
    }
}
