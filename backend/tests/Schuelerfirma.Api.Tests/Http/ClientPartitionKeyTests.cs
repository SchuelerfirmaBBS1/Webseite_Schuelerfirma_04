using System.Net;
using Schuelerfirma.Api.Infrastructure.Http;

namespace Schuelerfirma.Api.Tests.Http;

public class ClientPartitionKeyTests
{
    private static string Key(string address) => RateLimitingExtensions.ClientPartitionKey(IPAddress.Parse(address));

    [Fact]
    public void IPv4Addresses_CountIndividually()
    {
        Assert.Equal("203.0.113.7", Key("203.0.113.7"));
        Assert.NotEqual(Key("203.0.113.7"), Key("203.0.113.8"));
    }

    [Fact]
    public void IPv4MappedAddresses_CountAsTheIPv4Address()
    {
        // Kestrel reports IPv4 clients like this on dual-stack sockets.
        Assert.Equal(Key("203.0.113.7"), Key("::ffff:203.0.113.7"));
    }

    [Fact]
    public void IPv6Addresses_CountPer64Network()
    {
        Assert.Equal("2001:db8:1:2::/64", Key("2001:db8:1:2:aaaa:bbbb:cccc:dddd"));
        Assert.Equal(Key("2001:db8:1:2::1"), Key("2001:db8:1:2:ffff:ffff:ffff:ffff"));
        Assert.NotEqual(Key("2001:db8:1:2::1"), Key("2001:db8:1:3::1"));
    }

    [Fact]
    public void AMissingAddress_HasItsOwnKey()
    {
        Assert.Equal("unknown", RateLimitingExtensions.ClientPartitionKey(null));
    }
}
