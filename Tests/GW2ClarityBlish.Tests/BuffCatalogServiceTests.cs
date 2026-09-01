using System.Net;
using Xunit;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(_responder(request));
}

public class BuffCatalogServiceTests
{
    [Fact]
    public async Task GetIconUrlAsync_ReturnsUrlFromApi_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"id":100,"icon":"https://render.guildwars2.com/icon.png"}]""")
        });
        var service = new BuffCatalogService(new HttpClient(handler));

        var url = await service.GetIconUrlAsync(100);

        Assert.Equal("https://render.guildwars2.com/icon.png", url);
    }

    [Fact]
    public async Task GetIconUrlAsync_ReturnsFallback_OnHttpError()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = new BuffCatalogService(new HttpClient(handler));

        var url = await service.GetIconUrlAsync(100);

        Assert.Equal(BuffCatalogService.UnknownIconUrl, url);
    }

    [Fact]
    public async Task GetIconUrlAsync_ReturnsFallback_OnNetworkFailure()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("connexion refusee"));
        var service = new BuffCatalogService(new HttpClient(handler));

        var url = await service.GetIconUrlAsync(100);

        Assert.Equal(BuffCatalogService.UnknownIconUrl, url);
    }

    [Fact]
    public async Task GetIconUrlAsync_ReturnsFallback_OnTimeout()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new TaskCanceledException("timeout"));
        var service = new BuffCatalogService(new HttpClient(handler));

        var url = await service.GetIconUrlAsync(100);

        Assert.Equal(BuffCatalogService.UnknownIconUrl, url);
    }

    [Fact]
    public async Task GetIconUrlAsync_UsesCache_OnSecondCall_NoSecondHttpCall()
    {
        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"id":100,"icon":"https://render.guildwars2.com/icon.png"}]""")
            };
        });
        var service = new BuffCatalogService(new HttpClient(handler));

        await service.GetIconUrlAsync(100);
        await service.GetIconUrlAsync(100);

        Assert.Equal(1, callCount);
    }
}
