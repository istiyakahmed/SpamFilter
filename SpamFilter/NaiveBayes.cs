using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpamFilter
{
    class NaiveBayes : IClassifier
    {
        double pYes, pNo;
        double[] yesMean, noMean, yesSD, noSD;

        public void Build(IEnumerable<FeatureVector> dataSet)
        {
            int noFeatures = dataSet.First().Count;
            yesMean = new double[noFeatures];
            noMean = new double[noFeatures];
            yesSD = new double[noFeatures];
            noSD = new double[noFeatures];

            //get mean
            int yesCount = 0;
            int noCount = 0;
            foreach (FeatureVector v in dataSet)
            {
                if (v.IsSpamClass.Value)
                    yesCount++;
                else
                    noCount++;
            }

            for (int i = 0; i < noFeatures; i++)
            {           
                foreach (FeatureVector v in dataSet)
                {
                    if (v.IsSpamClass.Value)
                    {
                        yesMean[i] += v[i];
                    }
                    else
                    {
                        noMean[i] += v[i];
                    }
                }
                if (yesCount > 0)
                    yesMean[i] = yesMean[i] / yesCount;
                if (noCount > 0)
                    noMean[i] = noMean[i] / noCount;
            }

            //get sd
            for (int i = 0; i < noFeatures; i++)
            {
                foreach (FeatureVector v in dataSet)
                {
                    if (v.IsSpamClass.Value)
                    {
                        yesSD[i] += Math.Pow(yesMean[i] - v[i], 2);
                    }
                    else
                    {
                        noSD[i] += Math.Pow(noMean[i] - v[i], 2);
                    }
                }
                if (yesCount > 0)
                {
                    yesSD[i] = yesSD[i] / yesCount;
                    if (yesSD[i] > 0)
                        yesSD[i] = Math.Sqrt(yesSD[i]);
                }
                if (noCount > 0)
                {
                    noSD[i] = noSD[i] / noCount;
                    if (noSD[i] > 0)
                        noSD[i] = Math.Sqrt(noSD[i]);
                }
            }

            //get probability
            pYes = yesCount / (double)dataSet.Count();
            pNo = noCount / (double)dataSet.Count();
        }

        public bool Classify(FeatureVector inputVector)
        {
            double fYesProduct = 1;
            double fNoProduct = 1;
            for (int i = 0; i < inputVector.Count; i++)
            {
                double yesP = CalculateGaussian(inputVector[i], yesMean[i], yesSD[i]);
                double noP = CalculateGaussian(inputVector[i], noMean[i], noSD[i]);
                fYesProduct *= yesP;
                fNoProduct *= noP;
            }

            double yes = fYesProduct * pYes;
            double no = fNoProduct * pNo;

            return yes > no;
        }

        private double CalculateGaussian(double x, double mean, double sd)
        {
            //exceptional case
            if (sd == 0)
            {
                if (x == mean)
                    return 1;
                else
                    return double.Epsilon;
            }

            return Math.Exp((-Math.Pow((x - mean), 2)) / (2 * Math.Pow(sd, 2))) / (Math.Sqrt(2 * Math.PI * Math.Pow(sd, 2)));
        }
    }
}
