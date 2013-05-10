using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpamFilter
{
    class MappedCorpus : Corpus
    {
        private LexicalMapper Mapper { get; set; }
        public MappedCorpus(Corpus baseData)
            : base(baseData)
        {
        }

        public override void SelectFeatures(IEnumerable<string> stoplist, int maxFeatures)
        {
            //make the lexical mapper out of words found in the samples
            base.ExtractTokens(stoplist);
            List<IEnumerable<string>> dFeatures = new List<IEnumerable<string>>();
            foreach (Dictionary<string, int> d in SampleFrequencies)
                dFeatures.Add(d.Keys);
            Mapper = new LexicalMapper(dFeatures);

            //remake the feature set
            SampleFrequencies = new List<Dictionary<string, int>>();
            base.SelectFeatures(stoplist, maxFeatures);
        }

        protected override string FilterWord(string token, IEnumerable<string> stopWords)
        {
            string word = base.FilterWord(token, stopWords);

            if (Mapper == null)
                return word;

            return Mapper.Map(word);
        }
    }
}
