using System.Net;

namespace Stipps.CloudflareIpUpdater.Models;

public record IpValues(IPAddress? V4, IPAddress? V6)
{
    public IEnumerable<IPAddress> AsEnumerable()
    {
        var addresses = new List<IPAddress>();
        if (V4 is not null)
        {
            addresses.Add(V4);
        }

        if (V6 is not null)
        {
            addresses.Add(V6);
        }

        return addresses;
    }
}