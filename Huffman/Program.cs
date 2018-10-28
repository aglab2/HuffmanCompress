using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Huffman
{
    class Program
    {
        static UInt32 headerMagic = 0x46465548;
        static UInt32 codeMagic = 0x45444F43;
        static UInt32 dataMagic = 0x41544144;
        static char letterCodeMagic = 'C';

        static string compressArg = "compress";
        static string decompressArg = "decompress";

        /*
         * Header
         * uint32_t headerMagic = HUFF;
         * uint64_t fileSize;
         * uint32_t treeMagic = CODE;
         * uint16_t letterCount;
         * letter code
         * {
         *   uint8_t magic = C;
         *   Char Symbol
         *   {
         *     uint8_t magic = L;
         *     uint8_t letter;
         *   }
         *   Binary Code
         *   {
         *     uint8_t magic = B;
         *     uint8_t bitCount;
         *     binary code aligned on 8
         *   }
         * }
         * uint32_t dataMagic = DATA;
         * encoded data
         */
        static SortedSet<Symbol> generateSymbols(FileStream input)
        {
            int letter;
            int[] frequencies = new int[256];
            while ((letter = input.ReadByte()) != -1)
                frequencies[letter]++;

            SortedSet<Symbol> symbols = new SortedSet<Symbol>();
            for (int i = 0; i < 256; i++)
                if (frequencies[i] != 0)
                    symbols.Add(new CharSymbol(frequencies[i], (byte)i));

            input.Seek(0, SeekOrigin.Begin);
            return symbols;
        }

        static void encode(string inPath, string outPath)
        {
            FileStream input = new FileStream(inPath, FileMode.Open);
            FileStream outputFile = new FileStream(outPath, FileMode.OpenOrCreate);
            outputFile.SetLength(0);
            BitWriter output = new BitWriter(outputFile);

            // Prepare parse states
            SortedSet<Symbol> symbols = generateSymbols(input);
            Tree huffTree = new Tree(symbols);
            Dictionary<Symbol, BinaryCode> codes = huffTree.GenerateCodeTable();
            
            foreach (KeyValuePair<Symbol, BinaryCode> entry in codes)
            {
                Console.WriteLine(String.Format("{0} : {1}", entry.Key, entry.Value));
            }
            
            // Output header information
            output.Write(headerMagic);
            output.Write((UInt64)input.Length);
            output.Write(codeMagic);
            output.Write((UInt16)codes.Keys.Count);
            foreach (KeyValuePair<Symbol, BinaryCode> entry in codes)
            {
                Symbol sym = entry.Key;
                BinaryCode code = entry.Value;

                output.Write(letterCodeMagic);
                sym.Serialize(output);
                code.Serialize(output);
            }
            output.Write(dataMagic);

            // Parse the input file
            int letter;
            while ((letter = input.ReadByte()) != -1)
            {
                CharSymbol symbol = new CharSymbol(0, (byte) letter);
                BinaryCode code = codes[symbol];
                output.Write(code);
            }

            output.AlignAndWrite();
            output.Close();
            output.Dispose();
            input.Close();
            input.Dispose();
        }

        static void decode(string inPath, string outPath)
        {
            BitReader input = new BitReader(new FileStream(inPath, FileMode.Open));
            FileStream outputStream = new FileStream(outPath, FileMode.OpenOrCreate);
            outputStream.SetLength(0);
            BinaryWriter output = new BinaryWriter(outputStream);

            // Check if everything is fine
            UInt32 magic = input.ReadUInt32();
            if (magic != headerMagic)
                throw new ArgumentException("Bad Header");
            Int64 fileSize = input.ReadInt64();

            magic = input.ReadUInt32();
            if (magic != codeMagic)
                throw new ArgumentException("Bad Code Header");
            Int16 letterCount = input.ReadInt16();

            // Create Huffman tree from the input data: deserialize CODE section
            Dictionary<BinaryCode, Symbol> dict = new Dictionary<BinaryCode, Symbol>();
            for (int i = 0; i < letterCount; i++)
            {
                magic = input.ReadByte();
                if (magic != letterCodeMagic)
                    throw new ArgumentException("Bad Letter Code");

                CharSymbol sym = new CharSymbol(input);
                BinaryCode code = new BinaryCode(input);
                dict.Add(code, sym);
            }
            Tree huffTree = new Tree(dict);

            magic = input.ReadUInt32();
            if (magic != dataMagic)
                throw new ArgumentException("Bad Data Header");

            // Do it
            Tree.DecodeState state = new Tree.DecodeState(output, huffTree);
            while (state.writtenBytes < fileSize)
            {
                state.ParseBit(input.ReadBoolean());
            }


            input.Close();
            output.Close();
        }

        static void usage()
        {
            Console.WriteLine(String.Format("Usage: {0} {1}|{2} input output", System.AppDomain.CurrentDomain.FriendlyName, compressArg, decompressArg));
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
                usage();

            string inputFile = args[1];
            string outputFile = null;
            if (args.Length < 3)
            {
                if (args[0] == compressArg)
                    outputFile = inputFile + ".huff";
                else if (args[0] == decompressArg)
                    outputFile = inputFile + ".dec";
                else
                    usage();
            }
            else
            {
                outputFile = args[2];
            }


            if (args[0] == compressArg)
                encode(inputFile, outputFile);
            else if (args[0] == decompressArg)
                decode(inputFile, outputFile);
            else
                usage();
        }
    }
}
