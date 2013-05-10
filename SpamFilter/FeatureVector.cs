using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpamFilter
{
    class FeatureVector
    {
        private Dictionary<String, double> Features { get; set; }

        public bool? IsSpamClass { get; set; }

        public FeatureVector(IEnumerable<string> features, bool isSpam)
        {
            IsSpamClass = isSpam;
            Features = new Dictionary<string, double>();
            foreach (string s in features)
                Features.Add(s, 0);
        }

        public FeatureVector(IEnumerable<string> features)
        {
            IsSpamClass = null;
            Features = new Dictionary<string, double>();
            foreach (string s in features)
                Features.Add(s, 0);
        }

        public int Count
        {
            get { return Features.Count; }
        }

        public double this[string feature]
        {
            get
            {
                return Features[feature];
            }
            set
            {
                Features[feature] = value;
            }
        }

        public double this[int index]
        {
            get
            {
                return Features.ElementAt(index).Value;
            }
            set
            {
                string s = Features.ElementAt(index).Key;
                Features[s] = value;
            }
        }

        public string ToCSV()
        {
            string csv = "";

            for (int i = 0; i < Count; i++)
                csv += this[i] + ",";

            if (IsSpamClass == null)
                csv += "unknown";
            else if (IsSpamClass == true)
                csv += "spam";
            else
                csv += "legit";

            return csv;
        }

        public void FromCSV(string csv)
        {
            char[] delim = { ',' };
            string[] fields = csv.Split(delim);

            for (int i = 0; i < fields.Length - 1; i++)
                this[i] = double.Parse(fields[i]);

            if (fields[fields.Length - 1] == "spam")
                IsSpamClass = true;
            else if (fields[fields.Length - 1] == "legit")
                IsSpamClass = false;
            else
                IsSpamClass = null;
        }
    }
}
