using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpamFilter
{
    class Corpus
    {
        public List<Tuple<String, bool>> Samples { get; private set; }

        private Dictionary<String, int> featureDF;
        public Dictionary<String, int> FeatureDF
        {
            get { return featureDF; }
            set
            {
                featureDF = value;

                List<String> features = new List<string>();
                foreach (KeyValuePair<String, int> f in featureDF)
                    features.Add(f.Key);

                Features = features.AsReadOnly();
            }
        }

        public List<Dictionary<string, int>> SampleFrequencies { get; set; }

        public IReadOnlyList<String> Features { get; private set; }

        public IEnumerable<FeatureVector> DataSet { get; private set; }

        public Tuple<String, bool> this[int index]
        {
            get
            {
                return Samples[index];
            }
        }

        public Corpus()
        {
            Samples = new List<Tuple<string,bool>>();
            SampleFrequencies = new List<Dictionary<string, int>>();
        }

        public Corpus(Corpus data)
            : this()
        {
            Samples = data.Samples;
        }

        public void AddSample(String sample, bool isSpam)
        {
            Samples.Add(new Tuple<String, bool>(sample, isSpam));
        }

        /// <summary>
        /// Finds all valid tokens in the sample
        /// </summary>
        /// <param name="sample">Sample to be processed</param>
        /// <param name="stopWords">Words to be omitted from result</param>
        /// <returns>Dictionary containing all found tokens and their frequency in the sample</returns>
        private void ExtractTokensFromSample(string sample, IEnumerable<string> stopWords)
        {
            Dictionary<String, int> foundTokens = new Dictionary<string, int>();

            List<String> usedWords = new List<string>();

                char[] delimiter = {' '};
                
                String[] tokens = sample.Split(delimiter);

                foreach (String token in tokens)
                {
                    //filter out invalid tokens and words
                    String word = FilterWord(token, stopWords);
                    if (word == "")
                        continue;

                    //Only count the word once per document
                    bool used = false;
                    foreach (String s in usedWords)
                        if (s == word)
                        {
                            used = true;
                            break;
                        }

                    //add to set
                    if (!used)
                    {
                        if (foundTokens.ContainsKey(word))
                            foundTokens[word]++;
                        else
                            foundTokens[word] = 1;
                    }
                }

                SampleFrequencies.Add(foundTokens);
        }

        /// <summary>
        /// Extracts the valid tokens used in the entire sample set
        /// </summary>
        /// <param name="stopWords">Words to be omitted in the result</param>
        /// <returns>Returns a dictionary of words and their document frequency</returns>
        protected Dictionary<String, int> ExtractTokens(IEnumerable<String> stopWords)
        {
            Dictionary<String, int> foundTokens = new Dictionary<string, int>();

            foreach (Tuple<String, bool> sample in Samples)
            {
                String text = sample.Item1;

                ExtractTokensFromSample(text, stopWords);
            }

            foreach (Dictionary<string, int> d in SampleFrequencies)
            {
                foreach (string word in d.Keys)
                {
                    if (foundTokens.ContainsKey(word))
                        foundTokens[word]++;
                    else
                        foundTokens[word] = 1;
                }              
            }

            return foundTokens;
        }

        public virtual void SelectFeatures(IEnumerable<string> stoplist, int maxFeatures)
        {
            Dictionary<String, int> featureRange = ExtractTokens(stoplist);

            List<KeyValuePair<String, int>> range = featureRange.ToList();
            //sort keys by value
            range.Sort(
                delegate(KeyValuePair<string, int> firstPair,
                KeyValuePair<string, int> nextPair)
                {
                    return nextPair.Value.CompareTo(firstPair.Value);
                }
            );
            //take only the best
            if (maxFeatures < range.Count)
                range.RemoveRange(maxFeatures, range.Count() - maxFeatures > 0 ? range.Count() - maxFeatures : 0);

            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> pair in range)
                d.Add(pair.Key, pair.Value);

            FeatureDF = d;
        }

        protected virtual String FilterWord(string token, IEnumerable<String> stopWords)
        {
            char[] punctuation = { '.', '?', '!', ',', ':', '$', '%', '(', ')', '/', '\\', '\'', '\"' };
            //punctuation wont be included
            String word = token.Trim(punctuation);
            if (word == "")
                return "";

            //numbers wont be counted
            decimal d;
            if (decimal.TryParse(word, out d))
                return "";

            //make word lower case
            word = word.ToLower();

            //stop words not included
            if (stopWords.Contains(word))
                return "";

            //single character words not included
            if (word.Length == 1)
                return "";

            //return the filtered word
            return word;
        }

        public void ConstructDataSet()
        {
            List<FeatureVector> dataSet = new List<FeatureVector>();

            //calculate the raw weights for each sample
            for (int i = 0; i < Samples.Count; i++)
            {
                Dictionary<string, int> sample = SampleFrequencies[i];
                FeatureVector vector = new FeatureVector(Features, Samples[i].Item2);

                foreach (KeyValuePair<string, int> word in sample)
                {
                    if (!Features.Contains(word.Key))
                        continue;

                    //tfidf formula               
                    double weight = word.Value * Math.Log(Samples.Count / (double)FeatureDF[word.Key]);

                    vector[word.Key] = weight;
                }

                dataSet.Add(vector);
            }

            //normalise using cosine normalisation
            foreach (FeatureVector vector in dataSet)
            {
                //sum the squared tfidf scores
                double total = 0;
                for (int i = 0; i < vector.Count; i++)
                    total += Math.Pow(vector[i], 2);

                if (total == 0)
                    continue;

                //normalise each feature
                for (int i = 0; i < vector.Count; i++)
                {
                    double norm = vector[i] / Math.Sqrt(total);
                    vector[i] = norm;
                }
                    
            }

            DataSet = dataSet;
        }

        public void WriteCSV(StreamWriter stream)
        {
            foreach (string s in Features)
                stream.Write(s + ",");
            stream.WriteLine("class");
            foreach (FeatureVector v in DataSet)
                stream.WriteLine(v.ToCSV());
        }
    }
}
