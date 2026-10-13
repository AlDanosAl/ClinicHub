using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ClinicHub.SmsAgent.Services;

/// <summary>
/// Поиск сервера ClinicHub в локальной сети. Рассылаем по Wi-Fi сообщение CLINICHUB_DISCOVER на
/// UDP-порт 45454; сервер отвечает «CLINICHUB|порт». IP берём из адреса отправителя ответа.
/// </summary>
public static class ServerDiscovery
{
    public const int Port = 45454;
    private const string Probe = "CLINICHUB_DISCOVER";

    /// <summary>Возвращает адрес вида http://192.168.0.5:5112 или null, если сервер не ответил.</summary>
    public static async Task<string?> FindAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)) { EnableBroadcast = true };
            var probe = Encoding.UTF8.GetBytes(Probe);
            var targets = GetBroadcastTargets();

            // Шлём несколько раз: первый пакет по Wi-Fi может потеряться.
            var sender = Task.Run(async () =>
            {
                for (var i = 0; i < 6 && !cts.IsCancellationRequested; i++)
                {
                    foreach (var target in targets)
                    {
                        try { await udp.SendAsync(probe, probe.Length, new IPEndPoint(target, Port)); }
                        catch { /* этот адрес недоступен — пробуем остальные */ }
                    }

                    try { await Task.Delay(900, cts.Token); }
                    catch (OperationCanceledException) { break; }
                }
            });

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var received = await udp.ReceiveAsync(cts.Token);
                    var parts = Encoding.UTF8.GetString(received.Buffer).Trim().Split('|');
                    if (parts.Length == 2 && parts[0] == "CLINICHUB" && int.TryParse(parts[1], out var port))
                        return $"http://{received.RemoteEndPoint.Address}:{port}";
                }
            }
            finally
            {
                cts.Cancel();
                try { await sender; } catch { }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException)
        {
        }

        return null;
    }

    private static List<IPAddress> GetBroadcastTargets()
    {
        var result = new List<IPAddress> { IPAddress.Broadcast };
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask is null)
                        continue;

                    var ip = ua.Address.GetAddressBytes();
                    var mask = ua.IPv4Mask.GetAddressBytes();
                    var broadcast = new byte[4];
                    for (var i = 0; i < 4; i++)
                        broadcast[i] = (byte)(ip[i] | ~mask[i]);

                    var address = new IPAddress(broadcast);
                    if (!result.Contains(address))
                        result.Add(address);
                }
            }
        }
        catch
        {
            // На части версий Android список интерфейсов недоступен — хватит общей рассылки.
        }

        return result;
    }
}
