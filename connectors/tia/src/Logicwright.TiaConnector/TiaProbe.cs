using System;
using System.Linq;
using Siemens.Engineering;

namespace Logicwright.TiaConnector
{
    internal sealed class TiaProbe
    {
        public int Run(TiaSessionOptions options)
        {
            WindowsIdentityInfo.Print();
            TiaRuntime.Print();

            using (var portal = TiaPortalSession.Open(options))
            {
                PrintPortalInfo(portal);
            }

            return 0;
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
