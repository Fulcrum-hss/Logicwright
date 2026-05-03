using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logicwright.TiaConnector
{
    internal static class TiaAssemblyResolver
    {
        private static readonly string PortalRoot = @"C:\Program Files\Siemens\Automation\Portal V21";

        private static readonly string[] SearchDirectories =
        {
            Path.Combine(PortalRoot, @"PublicAPI\V21\net48"),
            Path.Combine(PortalRoot, @"Bin\PublicAPI"),
            Path.Combine(PortalRoot, @"Bin\PublicAPI\V21\net48"),
            Path.Combine(PortalRoot, "Bin")
        };

        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }

            registered = true;
            AppendToPath(Path.Combine(PortalRoot, "Bin"));
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        public static string PublicApiPath
        {
            get { return SearchDirectories[0]; }
        }

        public static string[] Directories
        {
            get { return SearchDirectories.ToArray(); }
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            foreach (var directory in SearchDirectories)
            {
                var candidate = Path.Combine(directory, assemblyName);
                if (File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }

            return null;
        }

        private static void AppendToPath(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var paths = current.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (paths.Any(path => string.Equals(path, directory, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Environment.SetEnvironmentVariable("PATH", current + Path.PathSeparator + directory);
        }
    }
}
