namespace WOWCAM.Update;

internal interface IGitHubClient
{
    Task<GitHubReleaseData> GetLatestReleaseDataAsync(string user, string repo, CancellationToken cancellationToken = default);
}
