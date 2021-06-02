using Parcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace InvIndexModuleSpace
{
    using log4net;

    public class InvIndexModule : MainModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InvIndexModule));

        private static CommandLineOptions options;

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            options = new CommandLineOptions();

            if (args != null)
            {
                if (!CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    throw new ArgumentException($@"Cannot parse the arguments. Possible usages:
{options.GetUsage()}");
                }
            }

            (new InvIndexModule()).RunModule(options);
        }

        public override void Run(ModuleInfo info, CancellationToken token = default(CancellationToken))
        {
            string Path = options.TextsPath;
            int PointsNum = options.PointsNum;

            List<string> Input;

            try
            {
                Input = InvertedIndex.LoadFromFile(Path);
            }


            catch (FileNotFoundException ex)
            {
                _log.Error("File with a given fileName not found, stopping the application...", ex);
                return;
            }

            _log.InfoFormat("Starting InvIndex Module on {0} points", PointsNum);

            var Points = new IPoint[PointsNum];
            var Channels = new IChannel[PointsNum];
            for (int i = 0; i < PointsNum; ++i)
            {
                Points[i] = info.CreatePoint();
                Channels[i] = Points[i].CreateChannel();
                Points[i].ExecuteClass("InvIndexModuleSpace.CalcInvIndex");
            }

            Dictionary<string, List<int>> ResInvIndex = new Dictionary<string, List<int>>();
            DateTime StartTime = DateTime.Now;
            _log.Info("Waiting for a result...");

            //Send to workers
            int Step = Input.Count/Channels.Length;
            
            for ( int ChannelIndex = 0, ChankBegin = 0; ChannelIndex < Channels.Length; ChannelIndex++)
            {
                Channels[ChannelIndex].WriteData(ChankBegin);
                int CnankEnd = (ChannelIndex >= Channels.Length - 1) ? Input.Count - 1 : ChankBegin + Step;
                if (InvertedIndex.bLoggingEnabled)
                {
                    Console.WriteLine("Worker {0} trying to send in range [{1}:{2}] total {3}", ChannelIndex, ChankBegin, CnankEnd, Input.Count);
                }
                string[] SubTexts = Input.GetRange(ChankBegin, CnankEnd - ChankBegin).ToArray();
                Channels[ChannelIndex].WriteObject(SubTexts);
                if (InvertedIndex.bLoggingEnabled)
                {
                    Console.WriteLine("Worker {0} sended range [{1}:{2}] of size {3} total {4}", ChannelIndex, ChankBegin, CnankEnd, SubTexts.Length, Input.Count);
                }
                ChankBegin = CnankEnd;
            }

            LogSendingTime(StartTime);

            if (InvertedIndex.bLoggingEnabled)
            {
                Console.WriteLine("Data was sanded to workers. Start reduce...");
            }

            //Reduce
            Dictionary<string, List<Tuple<int, int>>> InvIndex = new Dictionary<string, List<Tuple<int, int>>>();

            for (int i = PointsNum - 1; i >= 0; --i)
            {
                string ReceivedData =  Channels[i].ReadObject<string>();

                if (InvertedIndex.bLoggingEnabled)
                {
                    Console.WriteLine(String.Format("ReceivedData '{0}' from Point {1}", ReceivedData, i));
                }

                Dictionary<string, List<Tuple<int, int>>> CurrInvIndex = InvertedIndex.Deserialize(ReceivedData);

                //Merge lists
                foreach (KeyValuePair<string, List<Tuple<int, int>>> Pair in CurrInvIndex)
                {
                    if (InvIndex.ContainsKey(Pair.Key))
                    {
                        foreach (Tuple<int, int> ListItem in Pair.Value)
                        {
                            InvIndex[Pair.Key].Add(ListItem);
                        }
                    }
                    else
                    {
                        InvIndex[Pair.Key] = Pair.Value;
                    }
                }
            }

            LogResultFoundTime(StartTime);

            //Save result
           SaveInvertedIndex(InvIndex);
        }

        private static void LogResultFoundTime(DateTime time)
        {
            _log.InfoFormat(
                "Result found: time = {0}, saving the result to the file",
                Math.Round((DateTime.Now - time).TotalSeconds, 3));
        }

        private static void LogSendingTime(DateTime time)
        {
            _log.InfoFormat("Sending finished: time = {0}", Math.Round((DateTime.Now - time).TotalSeconds, 3));
        }

       private void SaveInvertedIndex(Dictionary<string, List<Tuple<int, int>>> InvIndex)
        {
            InvertedIndex.WriteToFile(Path.Combine("output", String.Format("output_{0:MM.dd.yy_H.mm.ss}.txt", DateTime.Now)), InvIndex);
        }
    }
}
