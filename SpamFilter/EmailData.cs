using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpamFilter
{
    class EmailData
    {
        public Corpus Subject { get; private set; }
        public Corpus Body { get; private set; }

        public IEnumerable<String> StopWords { get; set; }

        public EmailData()
        {
            Subject = new Corpus();
            Body = new Corpus();
            StopWords = new HashSet<string>();
        }

        public void ReadData(String directory)
        {
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(directory));

            foreach (String s in dirs)
            {
                List<string> files = new List<string>(Directory.EnumerateFiles(s));

                bool isSpam = files[0].Contains("spmsg");

                StreamReader file = new StreamReader(files[0]);
                string subject = file.ReadLine();
                subject = subject.Substring(8);//cut off the subject: header
                Subject.AddSample(subject, isSpam);
                file.ReadLine(); //skip a line
                Body.AddSample(file.ReadLine(), isSpam);

                file.Close();
            }
        }

        public void ReadStopWords(String directory)
        {
            StreamReader file = new StreamReader(directory);

            HashSet<String> words = new HashSet<string>();

            while (!file.EndOfStream)
                words.Add(file.ReadLine());

            StopWords = words;

            file.Close();
        }

        public void SelectFeatures(int numFeatures)
        {
            Subject.SelectFeatures(StopWords, numFeatures);
            Body.SelectFeatures(StopWords, numFeatures);
        }

        public void ConstructDataSets()
        {
            Subject.ConstructDataSet();
            Body.ConstructDataSet();
        }
    }
}
