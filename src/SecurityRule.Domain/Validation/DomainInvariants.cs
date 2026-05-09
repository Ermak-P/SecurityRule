using System.Net;
using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Validation;

public static class DomainInvariants
{
    private static readonly HashSet<string> AllowedProtocols =
    [
        "TCP",
        "UDP",
        "ICMP",
        "ANY"
    ];

    public static void ValidateServer(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);

        if (string.IsNullOrWhiteSpace(server.Name))
            throw new DomainValidationException("Название сервера обязательно.");

        if (string.IsNullOrWhiteSpace(server.OperatingSystem))
            throw new DomainValidationException("Операционная система обязательна.");

        if (string.IsNullOrWhiteSpace(server.IpAddress))
            throw new DomainValidationException("IP адрес сервера обязателен.");

        if (!IPAddress.TryParse(server.IpAddress, out _))
            throw new DomainValidationException("IP адрес сервера имеет неверный формат.");
    }

    public static void ValidateServiceConnection(ServiceConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.DestinationServiceId <= 0)
            throw new DomainValidationException("Сервис назначения обязателен.");

        if (string.IsNullOrWhiteSpace(connection.Protocol))
            return;

        if (!AllowedProtocols.Contains(connection.Protocol.Trim().ToUpperInvariant()))
            throw new DomainValidationException("Недопустимое значение протокола.");
    }
}
