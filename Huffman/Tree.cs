using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huffman
{
    abstract class TreeNode : IComparable<TreeNode>, IComparable<Symbol>
    {
        public readonly int weight;
        public abstract List<Symbol> symbols { get; }

        public TreeNode(int weight)
        {
            this.weight = weight;
        }

        public int CompareTo(TreeNode other)
        {
            int weightCmp = weight.CompareTo(other.weight);
            if (weightCmp != 0)
                return weightCmp;

            int cmpLength = symbols.Count.CompareTo(other.symbols.Count);
            if (cmpLength != 0)
                return cmpLength;

            var symbolsZip = symbols.Zip(other.symbols, (n, w) => new { ThisSym = n, OtherSym = w });
            foreach (var nw in symbolsZip)
            {
                int cmp = nw.ThisSym.CompareTo(nw.OtherSym);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        public int CompareTo(Symbol other)
        {
            return weight.CompareTo(other.weight);
        }
    }

    class TreeNodeLeaf : TreeNode
    {
        public static char magic = 'L';
        public readonly Symbol symbol;
        public override List<Symbol> symbols => new List<Symbol>{ symbol };

        /// <summary>
        /// Creates bottom Node of tree
        /// </summary>
        /// <param name="symbol">Symbol describing bottom node</param>
        public TreeNodeLeaf(Symbol symbol) : base(symbol.weight)
        {
            this.symbol = symbol;
        }
    }

    class TreeNodeInternal: TreeNode
    {
        public TreeNode[] next;
        public static char magic = 'I';
        List<Symbol> _symbols;
        public override List<Symbol> symbols => _symbols;

        /// <summary>
        /// Creates node on top of 2 other nodes
        /// </summary>
        /// <param name="next0">Next node for bit 0</param>
        /// <param name="next1">Next node for bit 1</param>
        public TreeNodeInternal(TreeNode next0, TreeNode next1) : base(next0.weight + next1.weight)
        {
            _symbols = new List<Symbol>();
            symbols.AddRange(next0.symbols);
            symbols.AddRange(next1.symbols);

            next = new TreeNode[2];
            next[0] = next0;
            next[1] = next1;
        }

        public TreeNodeInternal() : base(0)
        {
            next = new TreeNode[2];
        }
    }

    // TODO: 2 symbols should be defined at start, otherwise not gonna work
    class Tree
    {
        public class DecodeState
        {
            BinaryWriter output;
            Tree tree;
            TreeNodeInternal node;
            public int writtenBytes;

            public DecodeState(BinaryWriter output, Tree tree)
            {
                this.output = output;
                this.tree = tree;

                writtenBytes = 0;
                node = tree.root;
            }

            public void ParseBit(bool bit)
            {
                TreeNode next = node.next[bit ? 1 : 0];
                
                // Continue parsing
                if (next is TreeNodeInternal inter)
                {
                    node = inter;
                }
                // Output byte from leaf, run away
                else if (next is TreeNodeLeaf leaf)
                {
                    leaf.symbol.Write(output);
                    node = tree.root;
                    writtenBytes++;
                }
            }
        }

        readonly TreeNodeInternal root;

        public Tree(SortedSet<Symbol> symbols)
        {
            // Initialize nodes list 
            SortedSet<TreeNode> nodes = new SortedSet<TreeNode>();
            foreach(Symbol symbol in symbols)
            {
                nodes.Add(new TreeNodeLeaf(symbol));
            }

            // Create tree from Nodes
            while (nodes.Count > 1)
            {
                // Merge 2 nodes together
                TreeNode node0 = nodes.Min();
                nodes.Remove(node0);
                TreeNode node1 = nodes.Min();
                nodes.Remove(node1);

                TreeNodeInternal mergedNode = new TreeNodeInternal(node0, node1);
                nodes.Add(mergedNode);
            }

            root = nodes.First() as TreeNodeInternal;
        }

        public Tree(Dictionary<BinaryCode, Symbol> codes)
        {
            root = new TreeNodeInternal();

            // Recreate Tree from provided BinaryCodes
            foreach (KeyValuePair<BinaryCode, Symbol> entry in codes)
            {
                BinaryCode code = entry.Key;
                Symbol sym = entry.Value;

                TreeNodeInternal node = root;
                TreeNodeInternal prev = null;

                // Traverse the tree and create necessary internal nodes on fly
                // FIXME: Dirty but idk how to fix
                int bitint = 0;
                foreach (bool bit in code)
                {
                    prev = node;

                    bitint = bit ? 1 : 0;
                    if (node.next[bitint] == null)
                        node.next[bitint] = new TreeNodeInternal();

                    if (node.next[bitint] is TreeNodeInternal inter)
                        node = inter;
                    else
                        throw new ArgumentException("Invalid Code!");
                }

                prev.next[bitint] = new TreeNodeLeaf(sym);
            }
        }

        public Dictionary<Symbol, BinaryCode> GenerateCodeTable()
        {
            return ParseNode(root);
        }

        Dictionary<Symbol, BinaryCode> ParseNode(TreeNode node)
        {
            Dictionary<Symbol, BinaryCode> dict = new Dictionary<Symbol, BinaryCode>();
            if (node is TreeNodeLeaf leaf)
            {
                dict.Add(leaf.symbol, new BinaryCode());
            }
            else if (node is TreeNodeInternal inter)
            {
                for (int bit = 0; bit <= 1; bit++)
                {
                    Dictionary<Symbol, BinaryCode> dict0 = ParseNode(inter.next[bit]);
                    foreach (KeyValuePair<Symbol, BinaryCode> entry0 in dict0)
                    {
                        Symbol sym = entry0.Key;
                        BinaryCode code = entry0.Value;
                        code.Insert(0, bit == 1);
                        dict.Add(sym, code);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Node type is not supported!");
            }


            return dict;
        }
    }
}
