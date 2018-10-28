using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huffman
{
    abstract class Symbol : IComparable<Symbol>, IEquatable<Symbol>
    {
        public readonly int weight;

        protected Symbol(int weight)
        {
            this.weight = weight;
        }

        public abstract void Serialize(BinaryWriter stream);
        public abstract void Write(BinaryWriter stream);
        public abstract int CompareTo(Symbol other);
        public abstract bool Equals(Symbol other);
    }

    class CharSymbol : Symbol
    {
        public static char magic = 'L';
        public readonly byte letter;

        public CharSymbol(int weight, byte letter) : base(weight)
        {
            this.letter = letter;
        }

        public CharSymbol(BinaryReader stream) : base(0)
        {
            char magic = stream.ReadChar();
            if (magic != CharSymbol.magic)
                throw new ArgumentException("Bad CharSymbol magic!");
            
            letter = stream.ReadByte();
        }

        public override void Serialize(BinaryWriter stream)
        {
            stream.Write(CharSymbol.magic);
            stream.Write(letter);
        }

        public override string ToString()
        {
            return letter.ToString();
        }

        public override int CompareTo(Symbol other)
        {
            if (other is CharSymbol symbol)
            {
                return letter.CompareTo(symbol.letter);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override bool Equals(Symbol other)
        {
            if (other is CharSymbol symbol)
            {
                return letter.Equals(symbol.letter);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override int GetHashCode()
        {
            return letter.GetHashCode();
        }

        public override void Write(BinaryWriter stream)
        {
            stream.Write(letter);
        }
    }
}
