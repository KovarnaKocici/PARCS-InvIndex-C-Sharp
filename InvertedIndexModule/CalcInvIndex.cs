using InvIndexModuleSpace;
using Parcs;
using System;
using System.Collections.Generic;
using System.Threading;

namespace InvIndexModuleSpace
{
    public class CalcInvIndex : IModule
    {
        public void Run(ModuleInfo info, CancellationToken token = default(CancellationToken))
        {
            int File = info.Parent.ReadInt();
            string[] Texts = (string[])info.Parent.ReadObject(typeof(string[]));

            if (InvertedIndex.bLoggingEnabled)
            {
                Console.WriteLine("CalcInvIndex RUN on StartIndex: {0}, NumTexts: {1}", File, Texts.Length);
            }

            string Res = "";
            for(int i = 0; i< Texts.Length; i++)
            {
                if (InvertedIndex.bLoggingEnabled)
                {
                    Console.WriteLine("Work file {0} on line '{1}'", File, Texts[i].Length == 0 ? "0" : Texts[i]);
                }

                Dictionary<string, Tuple<int, int>> InvIndex = InvertedIndex.ParseText(File, Texts[i]);
                Res += InvertedIndex.Serialize(InvIndex);
                Res += (i == Texts.Length - 1) ? "" : "; ";
                File++;
            }

            if (InvertedIndex.bLoggingEnabled)
            {
                Console.WriteLine("Work COMPLETED: {0}", Res);
            }

            info.Parent.WriteObject(Res);
        }
    }
}
