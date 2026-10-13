using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ClinicHub.Api.Services;

/// <summary>Находит адреса этого компьютера в локальной сети — их телефон вписывает в QR-код как адрес сервера.</summary>
public static class LanAddresses
{
    private static readonly string[] VirtualMarkers =
        { "vethernet", "virtual", "vmware", "hyper-v", "vpn", "wsl", "docker", "tap-", "tailscale", "zerotier", "loopback" };

    /// <summary>IPv4-адреса: сначала 192.168.x.x, потом 10.x.x.x, потом 172.16–31.x.x; адреса виртуальных адаптеров в конце.</summary>
    public static List<string> GetIPv4Candidates()
    {
        var found = new List<(int Rank, string Ip)>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            var name = (ni.Name + " " + ni.Description).ToLowerInvariant();
            var isVirtual = VirtualMarkers.Any(name.Contains);

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var b = ua.Address.GetAddressBytes();
                if (b[0] == 127 || (b[0] == 169 && b[1] == 254))
                    continue;

                var rank = b[0] == 192 && b[1] == 168 ? 0
                    : b[0] == 10 ? 1
                    : b[0] == 172 && b[1] >= 16 && b[1] <= 31 ? 2
                    : 3;
                if (isVirtual)
                    rank += 10;

                found.Add((rank, ua.Address.ToString()));
            }
        }

        return found.OrderBy(x => x.Rank).Select(x => x.Ip).Distinct().ToList();
    }
}
