using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpamFilter
{
    class StemmedCorpus : Corpus
    {
        protected Stemmer Stemmer { get; private set; }
        private int stemSize;

        public StemmedCorpus(Corpus baseData, int stemListSize)
            : base(baseData)
        {
            stemSize = stemListSize;
        }

        public override void SelectFeatures(IEnumerable<string> stoplist, int maxFeatures)
        {
            //make the stemmer
            base.SelectFeatures(stoplist, stemSize);
            Stemmer = new Stemmer(base.Features);

            //reselect features
            SampleFrequencies = new List<Dictionary<string, int>>();
            base.SelectFeatures(stoplist, maxFeatures);
        }

        protected override string FilterWord(string token, IEnumerable<string> stopWords)
        {
            string word = base.FilterWord(token, stopWords);

            if (Stemmer == null)
                return word;

            return Stemmer.Stem(word);
        }
    }
}
