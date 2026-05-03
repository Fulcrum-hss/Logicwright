using System;
using System.IO;
using System.Reflection;

namespace Logicwright.TiaConnector
{
    internal static class TiaRuntime
    {
        public static void Print()
        {
            Console.WriteLine("TIA PublicAPI path: " + TiaAssemblyResolver.PublicApiPath);
            Console.WriteLine("TIA PublicAPI exists: " + Directory.Exists(TiaAssemblyResolver.PublicApiPath));
            Console.WriteLine("TIA assembly search paths:");
            foreach (var directory in TiaAssemblyResolver.Directories)
            {
                Console.WriteLine("  - " + directory + " (exists: " + Directory.Exists(directory) + ")");
            }

            PrintAssembly("Siemens.Engineering.Base");
            PrintAssembly("Siemens.Engineering.Step7");
            PrintAssembly("Siemens.Engineering.WinCC");
            PrintAssembly("Siemens.Engineering.WinCCUnified");
        }

        private static void PrintAssembly(string assemblyName)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
                Console.WriteLine(assemblyName + ": " + assembly.FullName);
                Console.WriteLine("  Location: " + assembly.Location);
            }
            catch (Exception ex)
            {
                Console.WriteLine(assemblyName + ": not loaded (" + ex.Message + ")");
            }
        }
    }
}
