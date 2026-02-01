using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Service to check for application updates from GitHub
    /// </summary>
    public class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/dcunliffe1980/SimControlCentre/releases/latest";
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SimControlCentre");
        }

        /// <summary>
        /// Get the current application version
        /// </summary>
        public string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.1.0";
        }

        /// <summary>
        /// Check if an update is available
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GitHubRelease>(GitHubApiUrl);
                
                if (response == null || string.IsNullOrWhiteSpace(response.tag_name))
                {
                    return new UpdateInfo { IsAvailable = false, CurrentVersion = GetCurrentVersion() };
                }

                // Remove 'v' prefix from tag name if present
                var latestVersion = response.tag_name.TrimStart('v');
                var currentVersion = GetCurrentVersion();

                // Compare versions
                var isNewer = IsVersionNewer(latestVersion, currentVersion);

                return new UpdateInfo
                {
                    IsAvailable = isNewer,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = response.html_url,
                    ReleaseNotes = response.body
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateService] Error checking for updates: {ex.Message}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = ex.Message
                };
            }
        }

        private bool IsVersionNewer(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = Version.Parse(latestVersion);
                var current = Version.Parse(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UpdateInfo
    {
        public bool IsAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string? LatestVersion { get; set; }
        public string? ReleaseUrl { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? Error { get; set; }
    }

    // GitHub API response model
    internal class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string html_url { get; set; } = "";
        public string body { get; set; } = "";
    }
}
