using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpamFilter
{
    class LexicalMapper
    {
        private Dictionary<string, string> mappings;
        const double probThreshold = 0.7;
        public LexicalMapper(IEnumerable<IEnumerable<string>> sampleTokens)
        {
            mappings = new Dictionary<string,string>();

            Dictionary<string, int> df = new Dictionary<string, int>();
            Dictionary<Tuple<string, string>, int> pairings = new Dictionary<Tuple<string, string>, int>();
            double omega = 0;
            //see how often each word appears in each document and how often theyre paired with other words
            foreach (IEnumerable<string> document in sampleTokens)
            {
                for (int i = 0; i < document.Count(); i++)
                {
                    string a = document.ElementAt(i);
                    if (!df.ContainsKey(a))
                        df.Add(a, 0);
                    df[a]++;
                    omega++;

                    for (int j = i+1; j < document.Count(); j++)
                    {
                        string b = document.ElementAt(j);

                        //sort the pair to make sure its entered the same each time
                        Tuple<string, string> pair;
                        if (string.Compare(a, b, true) < 0)
                            pair = new Tuple<string, string>(a, b);
                        else
                            pair = new Tuple<string, string>(b, a);

                        if (!pairings.ContainsKey(pair))
                            pairings.Add(pair, 0);
                        pairings[pair]++;
                    }
                }
            }

            //find pairings that are statistically significant
            Dictionary<string, Tuple<string, double>> significance = new Dictionary<string,Tuple<string,double>>();
            foreach (KeyValuePair<Tuple<string, string>, int> pair in pairings)
            {
                //P(a^b)
                double aib = pair.Value / omega;
                //P(a)
                double pa = df[pair.Key.Item1] / omega;
                //P(b)
                double pb = df[pair.Key.Item2] / omega;
                //P(a|b)
                double agb = aib / pb;
                //P(b|a)
                double bga = aib / pa;

                //only include if its seen in enough samples
                int minFreq = Math.Min(df[pair.Key.Item1], df[pair.Key.Item2]);
                if (minFreq < sampleTokens.Count() / 80)
                    continue;

                double maxProb = Math.Max(agb, bga);
                double minProb = Math.Min(agb, bga);
                if (maxProb >= probThreshold && maxProb - minProb < 0.8)
                {
                    string a, b;
                    if (agb > bga)
                    {
                        a = pair.Key.Item1;
                        b = pair.Key.Item2;
                    }
                    else
                    {
                        a = pair.Key.Item2;
                        b = pair.Key.Item1;
                    }

                    if (!significance.ContainsKey(a))
                        significance.Add(a, new Tuple<string,double>(b, maxProb));
                    else if (significance[a].Item2 < maxProb)
                        significance[a] = new Tuple<string,double>(b, maxProb);
                }
            }

            //finialise mappings
            foreach (KeyValuePair<string, Tuple<string, double>> v in significance)
                mappings.Add(v.Key, v.Value.Item1);
        }

        public string Map(string word)
        {
            string prev = word;
            string next = "";
            while (prev != next)
            {
                
                if (mappings.ContainsKey(prev))
                    prev = mappings[prev];
                else
                    next = prev;
            }

            return next;
        }

        public void Write(StreamWriter writer)
        {
            Dictionary<string, List<string>> groupings = new Dictionary<string, List<string>>();

            foreach (string word in mappings.Keys)
            {
                string mapped = Map(word);

                if (!groupings.ContainsKey(mapped))
                    groupings.Add(mapped, new List<string>());

                groupings[mapped].Add(word);
            }

            foreach (KeyValuePair<string, List<string>> group in groupings)
            {
                writer.WriteLine(group.Key);

                foreach (string s in group.Value)
                    writer.Write(s + " ");

                writer.WriteLine();
                writer.WriteLine();
            }
        }
    }
}
