namespace WOWCAM.Parts.Abstractions;

internal sealed class HttpClientProviderDefault : IHttpClientProvider
{
    // This is an abstraction for DI (since Microsoft offers no IHttpClient) and we also configure the HttpClient here

    private readonly HttpClient httpClient;

    public HttpClientProviderDefault(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;

        // The HttpClient (also used for the Curse downloads) should look a bit more like a Chrome browser

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br, zstd");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"150\", \"Not:A-Brand\";v=\"99\", \"Google Chrome\";v=\"150\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
    }

    public HttpClient GetHttpClient()
    {
        return httpClient;
    }
}
