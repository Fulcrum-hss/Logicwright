using System;
using System.Linq;
using Siemens.Engineering;

namespace Logicwright.TiaConnector
{
    internal sealed class TiaSessionOptions
    {
        public bool Attach { get; private set; }
        public bool Start { get; private set; }
        public bool WithUi { get; private set; } = true;
        public string OutputPath { get; private set; }

        public static TiaSessionOptions Parse(string[] args, bool requireOutput)
        {
            var options = new TiaSessionOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "--attach":
                        options.Attach = true;
                        break;
                    case "--start":
                        options.Start = true;
                        break;
                    case "--with-ui":
                        options.WithUi = true;
                        break;
                    case "--without-ui":
                        options.WithUi = false;
                        break;
                    case "--output":
                    case "-o":
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException(arg + " requires a file path.");
                        }
                        options.OutputPath = args[++i];
                        break;
                    default:
                        throw new ArgumentException("Unknown option: " + arg);
                }
            }

            if (options.Attach == options.Start)
            {
                throw new ArgumentException("Use exactly one of --attach or --start.");
            }

            if (requireOutput && string.IsNullOrWhiteSpace(options.OutputPath))
            {
                throw new ArgumentException("Missing required --output <file>.");
            }

            return options;
        }
    }

    internal static class TiaPortalSession
    {
        public static TiaPortal Open(TiaSessionOptions options)
        {
            return options.Attach ? AttachToRunningPortal() : StartPortal(options.WithUi);
        }

        private static TiaPortal AttachToRunningPortal()
        {
            var processes = TiaPortal.GetProcesses().ToList();
            Console.WriteLine("Running TIA Portal processes: " + processes.Count);

            if (processes.Count == 0)
            {
                throw new InvalidOperationException("No running TIA Portal process was found. Start TIA Portal first or use --start.");
            }

            var process = processes[0];
            Console.WriteLine("Attaching to TIA Portal process id: " + process.Id);
            return process.Attach();
        }

        private static TiaPortal StartPortal(bool withUi)
        {
            var mode = withUi ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface;
            Console.WriteLine("Starting TIA Portal through Openness, mode: " + mode);
            return new TiaPortal(mode);
        }
    }
}
