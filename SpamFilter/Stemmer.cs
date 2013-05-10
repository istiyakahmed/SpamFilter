using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpamFilter
{
    class MorphemeNode
    {
        public HashSet<MorphemeNode> InLinks { get; private set; }
        public HashSet<MorphemeNode> OutLinks { get; private set; }
        public string Token { get; private set; }

        public MorphemeNode(string token)
        {
            Token = token;
            InLinks = new HashSet<MorphemeNode>();
            OutLinks = new HashSet<MorphemeNode>();
        }

        public MorphemeNode FindStem()
        {
            if (OutLinks.Count == 0)
                return this;

            List<Tuple<MorphemeNode, int>> supported = new List<Tuple<MorphemeNode, int>>();
            List<MorphemeNode> linked = new List<MorphemeNode>();

            foreach (MorphemeNode node in OutLinks)
            {
                //find how many in links also point to this
                int support = 0;
                foreach (MorphemeNode nodeIn in node.InLinks)
                {
                    if (InLinks.Contains(nodeIn))
                        support++;
                }
                if (support > 0)
                    supported.Add(new Tuple<MorphemeNode, int>(node, support));
                else
                    linked.Add(node);
            }

            if (supported.Count != 0)
            {
                //find the node most supported by its peers
                MorphemeNode best = null;
                double max = 0;
                foreach (Tuple<MorphemeNode, int> node in supported)
                {
                    double k = (double)node.Item2 / (double)node.Item1.InLinks.Count;
                    if (k > max)
                    {
                        max = k;
                        best = node.Item1;
                    }
                }

                return best.FindStem();
            }
            else
            {
                //find the node least likely to be a prefix or suffix
                MorphemeNode best = null;
                int min = 0;
                foreach (MorphemeNode node in linked)
                {
                    if (node.InLinks.Count < min || best == null)
                    {
                        best = node;
                        min = node.InLinks.Count;
                    }
                }
                if (min < 15)
                    return best.FindStem();
                else
                    return this;
            }
        }
    }

    class Stemmer
    {
        List<string> corpusWords;
        Queue<string> openSet;
        Dictionary<string, string> mappings;

        public Stemmer(IEnumerable<string> corpus)
        {
            mappings = new Dictionary<string, string>();
            openSet = new Queue<string>();
            corpusWords = new List<string>();
            corpusWords.AddRange(corpus);
            Dictionary<string, MorphemeNode> nodes = new Dictionary<string, MorphemeNode>();
            SplitPhase(nodes);

            foreach (string s in corpusWords)
                mappings.Add(s, FindStem(s, nodes));
        }

        private void SplitPhase(Dictionary<string, MorphemeNode> nodes)
        {
            //initialise the sets
            foreach (string word in corpusWords)
            {
                nodes.Add(word, new MorphemeNode(word));
                openSet.Enqueue(word);
            }

            while (openSet.Count != 0)
            {
                string tokenA = openSet.Dequeue();
                MorphemeNode nodeA = nodes[tokenA];

                foreach (string tokenB in corpusWords)
                {
                    if (tokenA == tokenB)
                        continue;

                    MorphemeNode nodeB = nodes[tokenB];

                    Tuple<string, string, string, string> morphemes = Split(tokenA, tokenB);
                    //connect to substrings
                    ConnectNodes(nodeA, GetNode(morphemes.Item1, nodes));
                    ConnectNodes(nodeA, GetNode(morphemes.Item2, nodes));
                    ConnectNodes(nodeB, GetNode(morphemes.Item3, nodes));
                    ConnectNodes(nodeB, GetNode(morphemes.Item4, nodes));
                    //add new morphemes to open set
                    if (!nodes.ContainsKey(morphemes.Item1) && morphemes.Item1 != "")
                        openSet.Enqueue(morphemes.Item1);
                    if (!nodes.ContainsKey(morphemes.Item2) && morphemes.Item2 != "")
                        openSet.Enqueue(morphemes.Item2);
                    if (!nodes.ContainsKey(morphemes.Item3) && morphemes.Item3 != "")
                        openSet.Enqueue(morphemes.Item3);
                    if (!nodes.ContainsKey(morphemes.Item4) && morphemes.Item4 != "")
                        openSet.Enqueue(morphemes.Item4);
                }
            }
        }

        private string FindStem(string word, Dictionary<string, MorphemeNode> nodes)
        {
            if (!nodes.ContainsKey(word))
                return word;

            return nodes[word].FindStem().Token;
        }

        public string Stem(string word)
        {
            if (mappings.ContainsKey(word))
                return mappings[word];
            else
                return word;
        }

        MorphemeNode GetNode(string key, Dictionary<string, MorphemeNode> nodes)
        {
            if (key != "")
            {
                MorphemeNode n;
                if (!nodes.TryGetValue(key, out n))
                {
                    n = new MorphemeNode(key);
                    nodes.Add(key, n);
                }

                return n;
            }

            return null;
        }

        void ConnectNodes(MorphemeNode a, MorphemeNode b)
        {
            if (a == null || b == null || a == b)
                return;

            a.OutLinks.Add(b);
            b.InLinks.Add(a);
        }


        private Tuple<string, string, string, string> Split(string a, string b)
        {

            List<Tuple<string, string, string, string, int>> candidates = new List<Tuple<string, string, string, string, int>>();
            candidates.Add(new Tuple<string, string, string, string, int>("", "", "", "", 0));
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    string a1 = a.Substring(0, i);
                    string a2 = a.Substring(i);
                    string b1 = b.Substring(0, j);
                    string b2 = b.Substring(j);

                    bool goodSplit = (a1 == b1 && a1 != "") ||
                        (a1 == b2 && a1 != "") ||
                        (a2 == b1 && a2 != "") ||
                        (a2 == b2 && a2 != "");
                    if (goodSplit)
                    {
                        if (a1 == a || a2 == a || b1 == a || b2 == a)
                            return new Tuple<string, string, string, string>(a1, a2, b1, b2);

                        int al = Math.Min(a1.Length, a2.Length);
                        int bl = Math.Min(b1.Length, b2.Length);
                        int smallest = Math.Max(al, bl);

                        candidates.Add(new Tuple<string, string, string, string, int>(a1, a2, b1, b2, smallest));
                    }
                }
            }

            //sort
            candidates.Sort(
                delegate(Tuple<string, string, string, string, int> first,
                Tuple<string, string, string, string, int> next)
                {
                    return next.Item5.CompareTo(first.Item5);
                }
            );

            Tuple<string, string, string, string, int> best = candidates.First();

            return new Tuple<string, string, string, string>(best.Item1, best.Item2, best.Item3, best.Item4);
        }

        public void Write(StreamWriter writer)
        {
            Dictionary<string, List<string>> groupings = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, string> stem in mappings)
            {
                if (!groupings.ContainsKey(stem.Value))
                    groupings.Add(stem.Value, new List<string>());

                groupings[stem.Value].Add(stem.Key);
            }

            foreach (KeyValuePair<string, List<string>> groups in groupings)
            {
                if (groups.Value.Count == 1)
                    continue;

                writer.WriteLine(groups.Key);

                foreach (string s in groups.Value)
                    writer.Write(s + " ");

                writer.WriteLine();
                writer.WriteLine();
            }
        }
    }
}
