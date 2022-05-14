using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AIStringExtractor
{
    public class TextEntry
    {
        public int Index { get; set; }
        public string ID { get; set; }
        public string Text { get; set; }
    }
    public class LUAFile
    {
        public class TableEntry
        {
            public byte type;
            public object data;
        }

        public class StringConst
        {
            public int index;
            public string data;
        }
        



        public byte[] Header;

        public string Source;
        public Encoding Encoding;
        public byte[] FuncHeader;
        public List<uint> ByteCode = new List<uint>();
        public List<TableEntry> ConstantTable = new List<TableEntry>();
        public Dictionary<string, TextEntry> StringTable;
        public byte[] Tail;
        public LUAFile()
        {
            StringTable = new Dictionary<string, TextEntry>();
            Encoding = Encoding.UTF8;
        }
        public void LoadStream(Stream s)
        {
            using BinaryReader br = new BinaryReader(s);
            Header = br.ReadBytes(0xc);
            Source = ReadString(br);
            FuncHeader = br.ReadBytes(0xc);
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                ByteCode.Add(br.ReadUInt32());
            count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                TableEntry t = new TableEntry();
                t.type = br.ReadByte();
                string str;
                switch (t.type)
                {
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        t.data = br.ReadDouble();
                        break;
                    case 4:
                        
                        t.data = ReadString(br);
                        break;

                }
                ConstantTable.Add(t);
            }
            this.Tail = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            br.Close();
            int[] reg = new int[256];
            foreach (uint bc in ByteCode)
            {
                uint opcode = bc & 0x3f;
                int b = (int)((bc & 0x7F800000) >> 23);
                int c = (int)((bc & 0x3FC000) >> 14);
                switch (opcode)
                {
                    case 1: //LOADK
                        int bx = (int)((bc & 0xFFFFC000) >> 14);
                        int a = (int)((bc & 0x3FC0) >> 6);
                        reg[a] = bx;
                        continue;
                    case 9: //SETTABLE
                        if (((bc & 0x80000000) != 0))
                            b = (int)((bc & 0x7F800000) >> 23);
                        else
                            b = reg[(int)((bc & 0x7F800000) >> 23)];
                        if (((bc & 0x400000) != 0))
                            c = (int)((bc & 0x3FC000) >> 14);
                        else
                            c = reg[(int)((bc & 0x3FC000) >> 14)];
                        break;
                    default:
                        continue;
                }
                if (ConstantTable[c].type == 4)
                {
                    TextEntry sc = new TextEntry();
                    sc.Index = c;
                    sc.ID = (string)ConstantTable[b].data;
                    sc.Text = (string)ConstantTable[c].data;
                    if (!StringTable.ContainsKey(sc.ID))
                        StringTable.Add(sc.ID, sc);
                    else
                    {
                        string tmp = (string)ConstantTable[b].data;
                        int i = 1;
                        string tmp1 = tmp + ":" + i;
                        while (StringTable.ContainsKey(tmp1))
                        {
                            i++;
                            tmp1 = tmp + ":" + i;
                        }
                        sc.ID = tmp;
                        StringTable.Add(sc.ID, sc);
                    }
                }
            }

        }
        public void SaveFile(string file)
        {
            using BinaryWriter bw = new BinaryWriter(new FileStream(file, FileMode.Create));
            bw.Write(Header);
            WriteString(bw, Source);
            bw.Write(FuncHeader);
            bw.Write(ByteCode.Count);
            foreach (uint bc in ByteCode)
                bw.Write(bc);
            bw.Write(ConstantTable.Count);
            foreach (var value in StringTable.Values)
                if (value.Text != (string)ConstantTable[value.Index].data)
                    ConstantTable[value.Index].data = value.Text;
            string str;
            foreach (var t in ConstantTable)
            {
                bw.Write(t.type);
                switch (t.type)
                {
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        str = (string)t.data;
                        WriteString(bw,str);
                        break;

                }
            }
            bw.Write(Tail);
        }


        string ReadString(BinaryReader br)
        {
            int size;
            if (Header[8] == 8)
                size = (int)br.ReadInt64();
            else
                    size = br.ReadInt32();
            string str = Encoding.GetString(br.ReadBytes(size - 1));
            br.ReadByte();
            return str;
        }

        void WriteString(BinaryWriter bw, string str)
        {
            int size = Encoding.GetByteCount(str) + 1;

            bw.Write(size);
            bw.Write(Encoding.GetBytes(str));
            bw.Write((byte)0);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            string input;
            if (args.Length >= 1)
                input = args[0];
            else
            {
                Console.WriteLine("Please specify input file.");
                return;
            }
            try
            {
                using FileStream fs = File.OpenRead(input);
                using PositionXorStream pxs = new PositionXorStream(fs, 4);
                byte[] data = new byte[pxs.Length];
                pxs.Read(data, 0, (int)pxs.Length);
                File.WriteAllBytes(Path.ChangeExtension(input, ".decrypted.lua"),  data);
                LUAFile lua = new LUAFile();
                pxs.Position = 0;
                lua.LoadStream(pxs);
                var options = new JsonSerializerOptions();
                options.WriteIndented = true;
                options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                string jsonString = JsonSerializer.Serialize(lua.StringTable.Values, options);
                File.WriteAllText(Path.ChangeExtension(input, ".json"), jsonString);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }

        public class PositionXorStream : Stream
        {
            public PositionXorStream(FileStream fs, int pos)
            {
                this.BaseStream = fs;
                this.StartPosition = pos;
            }

            public long StartPosition { get; set; }
            public Stream BaseStream { get; }

            public override bool CanRead => BaseStream.CanRead;
            public override bool CanSeek => BaseStream.CanSeek;
            public override bool CanWrite => BaseStream.CanWrite;
            public override long Length => BaseStream.Length;

            public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

            public override void Flush()
            {
                BaseStream.Flush();
            }

            public void Decrypt(byte[] buffer, int offset, int count, long pos)
            {
                for (int i = offset;i < count;i++,pos++)
                {
                    if (pos >= StartPosition)
                        buffer[i] ^= (byte)(pos & 0xff);
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long pos = Position;
                int read = BaseStream.Read(buffer, offset, count);
                Decrypt(buffer, offset, count, pos);
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                BaseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Decrypt(buffer, offset, count, Position);
                BaseStream.Write(buffer, offset, count);
            }
        }
    }
}
