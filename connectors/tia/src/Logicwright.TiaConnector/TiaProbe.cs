using System;
using System.Linq;
using Siemens.Engineering;

namespace Logicwright.TiaConnector
{
    internal sealed class ProbeOptions
    {
        public bool Attach { get; private set; }
        public bool Start { get; private set; }
        public bool WithUi { get; private set; } = true;

        public static ProbeOptions Parse(string[] args)
        {
            var options = new ProbeOptions();

            foreach (var arg in args)
            {
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
                    default:
                        throw new ArgumentException("Unknown probe option: " + arg);
                }
            }

            if (options.Attach == options.Start)
            {
                throw new ArgumentException("Use exactly one of --attach or --start.");
            }

            return options;
        }
    }

    internal sealed class TiaProbe
    {
        public int Run(ProbeOptions options)
        {
            WindowsIdentityInfo.Print();
            TiaRuntime.Print();

            using (var portal = options.Attach ? AttachToRunningPortal() : StartPortal(options.WithUi))
            {
                PrintPortalInfo(portal);
            }

            return 0;
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

        private static void PrintPortalInfo(TiaPortal portal)
        {
            Console.WriteLine("Connected to TIA Portal.");

            var project = portal.Projects.FirstOrDefault();
            if (project == null)
            {
                Console.WriteLine("No project is currently open.");
                return;
            }

            Console.WriteLine("Open project: " + project.Name);
            Console.WriteLine("Project path: " + project.Path);
            Console.WriteLine("Devices:");

            foreach (var device in project.Devices)
            {
                Console.WriteLine("  - " + device.Name);
            }
        }
    }
}
