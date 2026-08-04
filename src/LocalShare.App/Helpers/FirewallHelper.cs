using System.Diagnostics;
using System.IO;

namespace LocalShare.App.Helpers;

public static class FirewallHelper
{
    public static void RegisterFirewallRules()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return;

            var ruleName = "360 LocalShare P2P LAN Network";

            // Add Inbound TCP Allow Rule via Netsh
            var tcpArgs = $"advfirewall firewall add rule name=\"{ruleName} (TCP)\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any protocol=TCP";
            RunNetshCommand(tcpArgs);

            // Add Inbound UDP Allow Rule via Netsh
            var udpArgs = $"advfirewall firewall add rule name=\"{ruleName} (UDP)\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any protocol=UDP";
            RunNetshCommand(udpArgs);
        }
        catch
        {
            // Ignore non-admin or silent firewall registration errors
        }
    }

    private static void RunNetshCommand(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var proc = Process.Start(startInfo);
            proc?.WaitForExit(1000);
        }
        catch { }
    }
}
