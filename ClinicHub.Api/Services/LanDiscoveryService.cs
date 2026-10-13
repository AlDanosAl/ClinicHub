using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClinicHub.Api.Services;

/// <summary>
/// Позволяет телефону найти сервер в локальной сети без ввода адреса. Телефон рассылает по Wi-Fi
/// короткое сообщение CLINICHUB_DISCOVER на UDP-порт 45454; сервер отвечает «CLINICHUB|порт».
/// IP-адрес сервера телефон берёт из адреса отправителя ответа, поэтому сменившийся IP компьютера
/// (DHCP) не проблема — телефон просто находит его заново.
/// </summary>
public sealed class LanDiscoveryService : BackgroundService
{
    public const int UdpPort = 45454;
    public const string Probe = "CLINICHUB_DISCOVER";

    private readonly IServer _server;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _config;
    private readonly ILogger<LanDiscoveryService> _log;

    public LanDiscoveryService(
        IServer server,
        IHostApplicationLifetime lifetime,
        IConfiguration config,
        ILogger<LanDiscoveryService> log)
    {
        _server = server;
        _lifetime = lifetime;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Адреса сервера известны только после запуска Kestrel.
        var started = new TaskCompletionSource();
        using (_lifetime.ApplicationStarted.Register(() => started.TrySetResult()))
        using (stoppingToken.Register(() => started.TrySetCanceled()))
        {
            try { await started.Task; }
            catch (OperationCanceledException) { return; }
        }

        var port = ResolveHttpPort();

        UdpClient udp;
        try
        {
            udp = new UdpClient(AddressFamily.InterNetwork);
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Автопоиск телефонов недоступен: не удалось занять UDP-порт {Port}. " +
                                "Подключать телефон придётся вводя адрес вручную.", UdpPort);
            return;
        }

        _log.LogInformation("Автопоиск телефонов включён: UDP {UdpPort}, отвечаем портом API {Port}.", UdpPort, port);
        try
        {
            foreach (var ip in LanAddresses.GetIPv4Candidates())
                _log.LogInformation("  адрес этого компьютера для ручного ввода в приложении: http://{Ip}:{Port}", ip, port);
        }
        catch
        {
            // Только подсказка в консоли — на работу не влияет.
        }

        using (udp)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var received = await udp.ReceiveAsync(stoppingToken);
                    var text = Encoding.UTF8.GetString(received.Buffer).Trim();
                    if (text != Probe)
                        continue;

                    _log.LogInformation("Телефон {Ip} искал сервер — отвечаю: порт {Port}.", received.RemoteEndPoint.Address, port);
                    var reply = Encoding.UTF8.GetBytes($"CLINICHUB|{port}");
                    await udp.SendAsync(reply, reply.Length, received.RemoteEndPoint);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException)
                {
                    // Например, «порт недоступен» от ушедшего клиента — слушаем дальше.
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Ошибка автопоиска телефонов");
                }
            }
        }
    }

    /// <summary>Порт, по которому API слушает обычный http (телефон ходит по http).</summary>
    private int ResolveHttpPort()
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? Array.Empty<string>();
        foreach (var address in addresses.OrderBy(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? 0 : 1))
        {
            var m = Regex.Match(address, @":(\d+)(?:/.*)?$");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var p))
                return p;
        }

        return _config.GetValue("Discovery:HttpPort", 5112);
    }
}
