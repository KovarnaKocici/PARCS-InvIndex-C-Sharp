using Parcs.Module.CommandLine;

namespace InvIndexModuleSpace
{
    using CommandLine;

    public class CommandLineOptions : BaseModuleOptions
    {
        [Option("texts", Required = true, HelpText = "File path to texts.")]
        public string TextsPath { get; set; }
        [Option("p", Required = true, HelpText = "Number of points.")]
        public int PointsNum { get; set; }        
    }
}
