using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace PhalanxChronicle.Battle
{
    public sealed class AppUpdateCheckResult
    {
        public bool HasUpdate { get; set; }

        public string CurrentVersion { get; set; } = string.Empty;

        public string LatestTag { get; set; } = string.Empty;

        public string ReleasePageUrl { get; set; } = string.Empty;
    }

    public sealed class AppUpdateService
    {
        private const string DismissedReleaseTagKey = "app_update.dismissed_release_tag";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/anomo06822/phalanx-chronicle/releases/latest";
        private const string LatestReleaseFallbackUrl = "https://github.com/anomo06822/phalanx-chronicle/releases/latest";
        private const int RequestTimeoutSeconds = 5;
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public IEnumerator CheckForAvailableUpdate(Action<AppUpdateCheckResult> onCompleted)
        {
            AppUpdateCheckResult result = new AppUpdateCheckResult
            {
                CurrentVersion = Application.version,
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
            request.Headers.Add("Accept", "application/vnd.github+json");
            request.Headers.Add("User-Agent", "PhalanxChronicle-UpdateCheck");

            Task<HttpResponseMessage> responseTask = HttpClient.SendAsync(request);
            while (!responseTask.IsCompleted)
            {
                yield return null;
            }

            if (responseTask.IsFaulted || responseTask.IsCanceled)
            {
                onCompleted?.Invoke(result);
                yield break;
            }

            using HttpResponseMessage response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                onCompleted?.Invoke(result);
                yield break;
            }

            Task<string> contentTask = response.Content.ReadAsStringAsync();
            while (!contentTask.IsCompleted)
            {
                yield return null;
            }

            if (contentTask.IsFaulted ||
                contentTask.IsCanceled ||
                string.IsNullOrWhiteSpace(contentTask.Result))
            {
                onCompleted?.Invoke(result);
                yield break;
            }

            try
            {
                GitHubLatestReleaseDto dto = JsonUtility.FromJson<GitHubLatestReleaseDto>(contentTask.Result);
                if (dto == null ||
                    dto.draft ||
                    dto.prerelease ||
                    string.IsNullOrWhiteSpace(dto.tag_name))
                {
                    onCompleted?.Invoke(result);
                    yield break;
                }

                result.LatestTag = dto.tag_name;
                result.ReleasePageUrl = string.IsNullOrWhiteSpace(dto.html_url)
                    ? LatestReleaseFallbackUrl
                    : dto.html_url;
                result.HasUpdate = AppVersionComparer.ShouldPrompt(
                    result.CurrentVersion,
                    dto.tag_name,
                    GetDismissedReleaseTag());
            }
            catch (Exception)
            {
                result.HasUpdate = false;
            }

            onCompleted?.Invoke(result);
        }

        public bool HasDismissed(string releaseTag)
        {
            string normalizedReleaseTag = AppVersionComparer.Normalize(releaseTag);
            return !string.IsNullOrWhiteSpace(normalizedReleaseTag) &&
                   string.Equals(normalizedReleaseTag, GetDismissedReleaseTag(), StringComparison.Ordinal);
        }

        public void MarkDismissed(string releaseTag)
        {
            string normalizedReleaseTag = AppVersionComparer.Normalize(releaseTag);
            if (string.IsNullOrWhiteSpace(normalizedReleaseTag))
            {
                return;
            }

            PlayerPrefs.SetString(DismissedReleaseTagKey, normalizedReleaseTag);
            PlayerPrefs.Save();
        }

        private static string GetDismissedReleaseTag()
        {
            return AppVersionComparer.Normalize(PlayerPrefs.GetString(DismissedReleaseTagKey, string.Empty));
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds),
            };
        }

        [Serializable]
        private sealed class GitHubLatestReleaseDto
        {
            public string tag_name;
            public string html_url;
            public bool draft;
            public bool prerelease;
        }
    }
}
