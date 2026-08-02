namespace WOWCAM.Parts.Core;

public sealed class HttpClientProviderDefault(HttpClient httpClient) : IHttpClientProvider
{
    private readonly HttpClient httpClient = httpClient;

    public HttpClient GetHttpClient()
    {
        return httpClient;
    }
}
