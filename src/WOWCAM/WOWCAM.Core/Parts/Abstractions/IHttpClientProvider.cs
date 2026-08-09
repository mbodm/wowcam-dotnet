namespace WOWCAM.Core.Parts.Abstractions;

internal interface IHttpClientProvider
{
    HttpClient GetHttpClient();
}
