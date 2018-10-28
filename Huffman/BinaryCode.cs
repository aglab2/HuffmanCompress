using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huffman
{
    class BinaryCode : List<bool>, IComparable<BinaryCode>, IEquatable<BinaryCode>
    {
        public static char magic = 'B';

        public BinaryCode() : base() { }

        public int CompareTo(BinaryCode other)
        {
            int cmpLength = Count.CompareTo(other.Count);
            if (cmpLength != 0)
                return cmpLength;

            var symbolsZip = this.Zip(other, (n, w) => new { ThisSym = n, OtherSym = w });
            foreach (var nw in symbolsZip)
            {
                int cmp = nw.ThisSym.CompareTo(nw.OtherSym);
                if (cmp != 0)
                    return cmp;
            }
            return 0;
        }

        public bool Equals(BinaryCode other)
        {
            bool cmpLength = Count.Equals(other.Count);
            if (!cmpLength)
                return cmpLength;

            var symbolsZip = this.Zip(other, (n, w) => new { ThisSym = n, OtherSym = w });
            foreach (var nw in symbolsZip)
            {
                bool cmp = nw.ThisSym.Equals(nw.OtherSym);
                if (cmp)
                    return cmp;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (bool b in this)
            {
                hash <<= 1;
                hash |= b.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            string ret = "";
            foreach(bool bit in this)
            {
                ret += bit ? "1" : "0";
            }
            return ret;
        }

        public BinaryCode(BitReader stream) : base()
        {
            char magic = stream.ReadChar();
            if (magic != BinaryCode.magic)
                throw new ArgumentException("Bad BinaryCode magic!");
            byte bitCount = stream.ReadByte();
            for (int i = 0; i < bitCount; i++)
            {
                Add(stream.ReadBoolean());
            }
            stream.Align();
        }

        public void Serialize(BitWriter output)
        {
            output.Write(BinaryCode.magic);
            output.Write((byte)Count);
            foreach (bool bit in this)
            {
                output.Write(bit);
            }
            output.AlignAndWrite();
        }
    }

    class BitWriter : System.IO.BinaryWriter
    {
        private bool[] curByte = new bool[8];
        private byte curBitIndx = 0;

        public BitWriter(Stream s) : base(s) { }

        public override void Flush()
        {
            base.Write(ConvertToByte(curByte));
            base.Flush();
        }

        public void AlignAndWrite()
        {
            if (curBitIndx != 0)
            {
                base.Write(ConvertToByte(curByte));
                curBitIndx = 0;
                curByte = new bool[8];
            }
        }

        public override void Write(bool value)
        {
            curByte[curBitIndx] = value;
            curBitIndx++;

            if (curBitIndx == 8)
            {
                AlignAndWrite();
            }
        }

        public void Write(BinaryCode code)
        {
            foreach (bool b in code)
                Write(b);
        }
        
        private static byte ConvertToByte(bool[] bools)
        {
            byte b = 0;

            byte bitIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (bools[i])
                {
                    b |= (byte)(((byte)1) << bitIndex);
                }
                bitIndex++;
            }

            return b;
        }
    }

    class BitReader : System.IO.BinaryReader
    {
        private bool[] curByte = new bool[8];
        private byte curBitIndx = 0;
        private BitArray ba;

        public BitReader(Stream s) : base(s)
        {
            Align();
        }

        public void Align()
        {
            this.curBitIndx = 8;
        }

        public override bool ReadBoolean()
        {
            if (curBitIndx == 8)
            {
                ba = new BitArray(new byte[] { base.ReadByte() });
                ba.CopyTo(curByte, 0);
                ba = null;
                this.curBitIndx = 0;
            }

            bool b = curByte[curBitIndx];
            curBitIndx++;
            return b;
        }
    }
}
