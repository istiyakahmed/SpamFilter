using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpamFilter
{
    interface IClassifier
    {
        void Build(IEnumerable<FeatureVector> dataSet);

        bool Classify(FeatureVector inputVector);
    }
}
