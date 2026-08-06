namespace WOWCAM.Parts.Abstractions;

internal interface IHttpClientProvider
{
    HttpClient GetHttpClient();
}
