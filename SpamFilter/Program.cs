using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpamFilter
{
    class Program
    {
        static EmailData data;

        static void Main(string[] args)
        {
            data = new EmailData();
            data.ReadData(Directory.GetCurrentDirectory() + "\\lingspam-mini600");
            data.ReadStopWords(Directory.GetCurrentDirectory() + "\\words.txt");
            data.SelectFeatures(200);
            data.ConstructDataSets();
            using (StreamWriter writer = new StreamWriter("body.csv"))
            {
                data.Body.WriteCSV(writer);
                writer.Flush();
            }
            using (StreamWriter writer = new StreamWriter("subject.csv"))
            {
                data.Subject.WriteCSV(writer);
                writer.Flush();
            }
            RunNaiveBayesCSV();
            //RunNaiveBayesStemmed();
            //RunNaiveBayesMapped();
        }

        static void RunCrossValidation(IEnumerable<FeatureVector> datasetS, IEnumerable<FeatureVector> datasetB)
        {
            //create stratified data
            List<FeatureVector>[] strataS = new List<FeatureVector>[10];
            List<FeatureVector>[] trainingSetsS = new List<FeatureVector>[10];
            List<FeatureVector>[] strataB = new List<FeatureVector>[10];
            List<FeatureVector>[] trainingSetsB = new List<FeatureVector>[10];
            IClassifier[] classifiersS = new IClassifier[10];
            IClassifier[] classifiersB = new IClassifier[10];

            for (int i = 0; i < 10; i++)
            {
                strataS[i] = new List<FeatureVector>();
                trainingSetsS[i] = new List<FeatureVector>();
                strataB[i] = new List<FeatureVector>();
                trainingSetsB[i] = new List<FeatureVector>();
                classifiersS[i] = new NaiveBayes();
                classifiersB[i] = new NaiveBayes();
            }

            //put equal amounts of samples in each strata
            int st = 0;
            foreach (FeatureVector v in datasetS)
            {
                strataS[st].Add(v);
                st++;
                st = st % 10;
            }
            st = 0;
            foreach (FeatureVector v in datasetB)
            {
                strataB[st].Add(v);
                st++;
                st = st % 10;
            }

            //make test folds
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i == j)
                        continue;

                    trainingSetsS[i].AddRange(strataS[j]);
                    trainingSetsB[i].AddRange(strataB[j]);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                classifiersS[i].Build(trainingSetsS[i]);
                classifiersB[i].Build(trainingSetsB[i]);
            }

            //perform tests
            int[,] bodyM = new int[2, 2];
            int[,] subjM = new int[2, 2];
            double avgB = 0;
            double avgS = 0;
            for (int i = 0; i < 10; i++)
            {
                int correct = 0;
                foreach (FeatureVector v in strataS[i])
                {
                    if (classifiersS[i].Classify(v) == v.IsSpamClass)
                    {
                        correct++;
                        if (v.IsSpamClass.Value)
                            subjM[0, 0]++;
                        else
                            subjM[1, 1]++;
                    }
                    else
                    {
                        if (v.IsSpamClass.Value)
                            subjM[0, 1]++;
                        else
                            subjM[1, 0]++;
                    }
                }
                avgS += correct / (double)strataS[i].Count();

                correct = 0;
                foreach (FeatureVector v in strataB[i])
                {
                    if (classifiersB[i].Classify(v) == v.IsSpamClass)
                    {
                        correct++;
                        if (v.IsSpamClass.Value)
                            bodyM[0, 0]++;
                        else
                            bodyM[1, 1]++;
                    }
                    else
                    {
                        if (v.IsSpamClass.Value)
                            bodyM[0, 1]++;
                        else
                            bodyM[1, 0]++;
                    }
                }
                avgB += correct / (double)strataS[i].Count();
            }
            Console.WriteLine("Subject Confusion Matrix");
            Console.Write(subjM[0, 0]); Console.Write(" "); Console.WriteLine(subjM[1, 0]);
            Console.Write(subjM[0, 1]); Console.Write(" "); Console.WriteLine(subjM[1, 1]);
            Console.WriteLine("Body Confusion Matrix");
            Console.Write(bodyM[0, 0]); Console.Write(" "); Console.WriteLine(bodyM[1, 0]);
            Console.Write(bodyM[0, 1]); Console.Write(" "); Console.WriteLine(bodyM[1, 1]);

            Console.WriteLine("Subject avg");
            Console.WriteLine(avgS / 10.0);
            Console.WriteLine("Body avg");
            Console.WriteLine(avgB / 10.0);
        }

        static void RunNaiveBayesCSV()
        {
            List<FeatureVector> datasetS = new List<FeatureVector>();
            List<FeatureVector> datasetB = new List<FeatureVector>();

            //read csv
            StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "\\subject.csv");
            string feat = reader.ReadLine();
            char[] delim = {','};
            string[] header = feat.Split(delim);
            string[] features = new string[header.Length - 1];
            for (int i = 0; i < features.Length; i++)
                features[i] = header[i];
            while (!reader.EndOfStream)
            {
                FeatureVector f = new FeatureVector(features);
                string d = reader.ReadLine();
                if (d != "")
                    f.FromCSV(d);
                datasetS.Add(f);
            }
            reader.Close();
            reader = new StreamReader(Directory.GetCurrentDirectory() + "\\body.csv");
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                FeatureVector f = new FeatureVector(features);
                string d = reader.ReadLine();
                if (d != "")
                    f.FromCSV(d);
                datasetB.Add(f);
            }
            reader.Close();

            //perform validation
            RunCrossValidation(datasetS, datasetB);
        }

        static void RunNaiveBayesStemmed()
        {
            Corpus subject = new StemmedCorpus(data.Subject, 400);
            Corpus body = new StemmedCorpus(data.Body, 1000);
            subject.SelectFeatures(data.StopWords, 200);
            body.SelectFeatures(data.StopWords, 200);
            subject.ConstructDataSet();
            body.ConstructDataSet();

            //run validation
            RunCrossValidation(subject.DataSet, body.DataSet);
        }

        static void RunNaiveBayesMapped()
        {
            Corpus subject = new MappedCorpus(data.Subject);
            Corpus body = new MappedCorpus(data.Body);
            subject.SelectFeatures(data.StopWords, 200);
            body.SelectFeatures(data.StopWords, 200);
            subject.ConstructDataSet();
            body.ConstructDataSet();

            //run validation
            RunCrossValidation(subject.DataSet, body.DataSet);
        }
    }
}
