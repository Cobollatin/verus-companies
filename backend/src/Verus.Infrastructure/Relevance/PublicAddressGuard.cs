using System.Net;
using System.Net.Sockets;

namespace Verus.Infrastructure.Relevance;

/// <summary>
/// SSRF protection: outbound fetches may only reach public internet addresses. Applied at connect time,
/// after DNS resolution, so redirects and DNS rebinding cannot reach internal hosts.
/// </summary>
internal static class PublicAddressGuard
{
    public static bool IsPublic(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address),
            _ => false,
        };
    }

    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken
    )
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var target =
            addresses.FirstOrDefault(IsPublic)
            ?? throw new UnsafeAddressException(
                $"Refusing to connect to '{host}': it does not resolve to a public address."
            );

        if (Array.Exists(addresses, a => !IsPublic(a)))
        {
            throw new UnsafeAddressException($"Refusing to connect to '{host}': it resolves to a non-public address.");
        }

        var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublicIPv4(byte[] b) =>
        b[0] switch
        {
            0 or 10 or 127 => false, // "this" network, private, loopback
            100 when b[1] is >= 64 and <= 127 => false, // carrier-grade NAT
            169 when b[1] == 254 => false, // link-local (incl. cloud metadata 169.254.169.254)
            172 when b[1] is >= 16 and <= 31 => false, // private
            192 when b[1] == 168 => false, // private
            192 when b[1] == 0 && b[2] is 0 or 2 => false, // IETF protocol assignments, TEST-NET-1
            198 when b[1] is 18 or 19 => false, // benchmarking
            198 when b[1] == 51 && b[2] == 100 => false, // TEST-NET-2
            203 when b[1] == 0 && b[2] == 113 => false, // TEST-NET-3
            >= 224 => false, // multicast, reserved, broadcast
            _ => true,
        };

    private static bool IsPublicIPv6(IPAddress address)
    {
        if (
            address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6Multicast
            || address.IsIPv6UniqueLocal
            || address.Equals(IPAddress.IPv6None)
        )
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8); // 2001:db8::/32 docs
    }
}

/// <summary>Raised when a host resolves to a non-public address. Never retried.</summary>
internal sealed class UnsafeAddressException(string message) : IOException(message);
