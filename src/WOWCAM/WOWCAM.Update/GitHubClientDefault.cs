using System.Net.Http.Headers;
using System.Text.Json;

namespace WOWCAM.Update;

internal sealed class GitHubClientDefault(HttpClient httpClient) : IGitHubClient
{
    private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<GitHubReleaseData> GetLatestReleaseDataAsync(string user, string repo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var json = await FetchLatestReleaseJsonAsync(user, repo, httpClient, cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        var tagName = doc.RootElement.GetProperty("tag_name").GetString() ??
            throw new InvalidOperationException("Could not found 'tag_name' in GitHub's JSON response.");

        var firstAsset = doc.RootElement.GetProperty("assets").EnumerateArray().First();
        var downloadUrl = firstAsset.GetProperty("browser_download_url").GetString() ??
            throw new InvalidOperationException("Could not found 'browser_download_url' in GitHub's JSON response.");

        if (!downloadUrl.EndsWith(".zip") || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri) || uri == null)
            throw new InvalidOperationException("Download url in GitHub's JSON response was not a valid zip download URL.");

        return new GitHubReleaseData(new Version(tagName), downloadUrl, uri.Segments.Last());
    }

    private static async Task<string> FetchLatestReleaseJsonAsync(string user, string repo, HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{user}/{repo}/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd(user);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var prettyStatusCode = $"HTTP {(int)response.StatusCode} ({response.StatusCode})";
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub response was {prettyStatusCode}.");

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
            throw new InvalidOperationException($"GitHub response was ${prettyStatusCode}, but JSON content was an empty string.");

        return json;
    }
}
