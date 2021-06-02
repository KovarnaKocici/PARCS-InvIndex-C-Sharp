using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;

namespace InvIndexModuleSpace
{
    [Serializable]
    public class InvertedIndex
    {
        public static  bool bLoggingEnabled = false;
        public static List<string> LoadFromFile(string Path)
        {
            List<string> Res = new List<string>();

            using (var FileStream = File.OpenRead(Path))
            {
                using (var StreamReader = new StreamReader(FileStream))
                {
                    string Line;
                    while ((Line = StreamReader.ReadLine()) != null)
                    {
                        if (!String.IsNullOrWhiteSpace(Line))
                        {
                            // Use regular expressions to replace characters
                            // that are not letters or numbers with spaces.
                            Regex reg_exp = new Regex("[^a-zA-Z0-9]");
                            Line = reg_exp.Replace(Line, " ");

                            Res.Add(Line);

                            //Console.WriteLine("LoadFromFile Line: '{0}'", Line);
                        }
                    }
                }
            }

            return Res;
        }

        public static Dictionary<string, Tuple<int, int>> ParseText(int File, string Text)
        {
            Dictionary<string,Tuple<int, int>> Res = new Dictionary<string, Tuple<int, int>>();

            string[] AllWords = SplitText(Text);

            String AllWordsStr = "";
            foreach (String Word in AllWords)
            {
                AllWordsStr +=String.Format("{0}, ", Word);
            }

            if (bLoggingEnabled)
            {
                Console.WriteLine("ParseText:: Words are: {0}", AllWordsStr);
            }

            string[] UniqueWords = (from string Word in AllWords
                               orderby Word
                              select Word).Distinct().ToArray();

            String UniqueWordsStr = "";
            foreach (String Word in UniqueWords)
            {
                UniqueWordsStr += String.Format("{0}, ", Word);
            }

            if (bLoggingEnabled)
            {
                Console.WriteLine("ParseText:: Unique words are :{0}", UniqueWordsStr);
            }

            foreach (var Word in UniqueWords)
            {
                Res.Add(Word, new Tuple<int, int>(File, CountWords(AllWords, Word)));
            }

            return Res;
        }

        private static string[] SplitText(string Query)
        {
            string[] Keywords = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return Keywords;
        }

        private static int CountWords(string[] SplittedText, string Word)
        {
            int NumOccur = 0;

            foreach (string WordInText in SplittedText)
            {
                if (WordInText == Word)
                {
                    NumOccur += 1;
                }

                if (bLoggingEnabled)
                {
                    Console.WriteLine("CountWords:: {0}=={1}?={2} NumOccur: {3}", WordInText, Word, WordInText == Word, NumOccur);
                }
            }
            return NumOccur;
        }

        public static string Serialize(Dictionary<string, Tuple<int, int>> InvIndex)
        {
            //Convert to format [word, file, num_occur;]
            var Res = string.Join("; ", InvIndex.Select(
                  p => string.Format(
                   "{0}, ({1}, {2})"
                  , p.Key
                  , p.Value.Item1.ToString()
                  , p.Value.Item2.ToString()
                  )
              ));

            return Res;
        }

        public static Dictionary<string, List<Tuple<int, int>>> Deserialize(string Data)
        {
            //Remove ( )
            String FormatedData = Data.Replace("(", String.Empty);
            FormatedData = FormatedData.Replace(")", String.Empty);

            if (bLoggingEnabled)
            {
                Console.WriteLine("Deserialize:: Data: {0}", FormatedData);
            }

            List<Tuple<string, Tuple<int, int>>> WordFileOccurList = FormatedData.Split(';')
                   .Select(s => s.Split(','))
                   .Select(p => new Tuple<string, Tuple<int, int>>(p[0].Trim(), new Tuple<int, int>(int.Parse(p[1].Trim()), int.Parse(p[2].Trim()))))
                   .ToList();

            //Convert to dictionary now as there may be duplicates
            Dictionary<string, List<Tuple<int, int>>> Res = new Dictionary<string, List<Tuple<int, int>>>();

            foreach (Tuple<string, Tuple<int, int>> KeyValuePair in WordFileOccurList)
            {
                if (bLoggingEnabled)
                {
                    Console.WriteLine("Deserialize:: KeyValuePair ({0}, {1}) from WordFileOccurList processing", KeyValuePair.Item1, KeyValuePair.Item2);
                }

                if (Res.ContainsKey(KeyValuePair.Item1))
                {
                    List<Tuple<int, int>> TempList = Res[KeyValuePair.Item1];
                    TempList.Add(KeyValuePair.Item2); ;
                    Res[KeyValuePair.Item1] = TempList;
                }
                else 
                {
                    Res.Add(KeyValuePair.Item1, new List<Tuple<int, int>>(){ KeyValuePair.Item2} );
                }
            }

            if (bLoggingEnabled)
            {
                Console.WriteLine("Deserialize:: Result dictionary contains {0}", Res.Count);
            }

            return Res;
        }

        public static void WriteToFile(string Path, Dictionary<string, List<Tuple<int, int>>> InvIndex)
        {

            //Print result
            string Text = String.Format("Result found: Entries in Dictionary {0}", InvIndex.Count);
            Text += Environment.NewLine;

            foreach (KeyValuePair<string, List<Tuple<int, int>>> Pair in InvIndex)
            {
                string OccurStr = "";
                int KeyIndex = 0;

                for(int PairIndex = 0; PairIndex< Pair.Value.Count; PairIndex++)
                {
                    Tuple<int, int> Occur = Pair.Value[PairIndex];
                    OccurStr += String.Format("({0}, {1})", Occur.Item1, Occur.Item2); // (file, num occur)
                    if (PairIndex != Pair.Value.Count - 1)
                    {
                        OccurStr += ", ";
                    }
                }

                Text += String.Format("{0}: [{1}]", Pair.Key, OccurStr); // key: [(file, num occur), ...]
                Text += Environment.NewLine;
                if (KeyIndex != InvIndex.Count - 1)
                {
                    OccurStr += "; ";
                }

                KeyIndex++;
            }

            File.WriteAllText(Path, Text);
        }

    }

}
