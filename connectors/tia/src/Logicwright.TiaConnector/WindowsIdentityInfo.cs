using System;
using System.Linq;
using System.Security.Principal;

namespace Logicwright.TiaConnector
{
    internal static class WindowsIdentityInfo
    {
        private const string OpennessGroupName = "Siemens TIA Openness";

        public static void Print()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            Console.WriteLine("Windows user: " + identity.Name);
            Console.WriteLine("Is administrator: " + principal.IsInRole(WindowsBuiltInRole.Administrator));
            Console.WriteLine("Is in Siemens TIA Openness group: " + IsInGroup(identity, OpennessGroupName));
        }

        private static bool IsInGroup(WindowsIdentity identity, string groupName)
        {
            return identity.Groups != null && identity.Groups
                .Select(group => Translate(group))
                .Any(name => name.IndexOf(groupName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string Translate(IdentityReference reference)
        {
            try
            {
                return reference.Translate(typeof(NTAccount)).Value;
            }
            catch
            {
                return reference.Value;
            }
        }
    }
}
