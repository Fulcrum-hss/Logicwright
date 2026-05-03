using System;
using System.Linq;

namespace Logicwright.TiaConnector
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                TiaAssemblyResolver.Register();

                if (args.Length == 0 || IsHelp(args[0]))
                {
                    PrintUsage();
                    return 0;
                }

                var command = args[0].ToLowerInvariant();
                switch (command)
                {
                    case "env":
                        WindowsIdentityInfo.Print();
                        TiaRuntime.Print();
                        return 0;

                    case "probe":
                        var options = ProbeOptions.Parse(args.Skip(1).ToArray());
                        return new TiaProbe().Run(options);

                    default:
                        Console.Error.WriteLine("Unknown command: " + args[0]);
                        PrintUsage();
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.GetType().FullName);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static bool IsHelp(string value)
        {
            return value == "-h" || value == "--help" || value == "help";
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Logicwright TIA Connector");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Logicwright.TiaConnector.exe env");
            Console.WriteLine("  Logicwright.TiaConnector.exe probe --attach");
            Console.WriteLine("  Logicwright.TiaConnector.exe probe --start");
            Console.WriteLine("  Logicwright.TiaConnector.exe probe --start --without-ui");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine("  --attach connects to the first running TIA Portal process.");
            Console.WriteLine("  --start starts a new TIA Portal process through Openness.");
        }
    }
}
