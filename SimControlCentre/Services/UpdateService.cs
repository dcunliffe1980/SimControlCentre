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
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // 5 second timeout
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SimControlCentre");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        /// <summary>
        /// Get the current application version
        /// </summary>
        public string GetCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    // Return Major.Minor.Build format (ignore Revision)
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch (Exception ex)
            {
                UpdateDiagnostics.Log($"Error getting version: {ex.Message}");
            }
            
            // Fallback to hardcoded version
            return "1.1.2";
        }

        /// <summary>
        /// Check if an update is available
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                UpdateDiagnostics.Log($"Checking for updates at: {GitHubApiUrl}");
                UpdateDiagnostics.Log($"Current version: {GetCurrentVersion()}");
                
                // Test basic internet connectivity first
                try
                {
                    using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var testResponse = await testClient.GetAsync("https://www.google.com").ConfigureAwait(false);
                    UpdateDiagnostics.Log($"Internet connectivity test: {testResponse.StatusCode}");
                }
                catch (Exception ex)
                {
                    UpdateDiagnostics.Log($"Internet connectivity test failed: {ex.Message}");
                    return new UpdateInfo
                    {
                        IsAvailable = false,
                        CurrentVersion = GetCurrentVersion(),
                        Error = "No internet connection detected. Please check your network settings."
                    };
                }
                
                var response = await _httpClient.GetAsync(GitHubApiUrl).ConfigureAwait(false);
                
                UpdateDiagnostics.Log($"Response status: {response.StatusCode}");
                UpdateDiagnostics.Log($"Response headers: {response.Headers}");
                
                // Handle 404 - no releases exist yet
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    UpdateDiagnostics.Log("No releases found (404)");
                    UpdateDiagnostics.Log("This could mean:");
                    UpdateDiagnostics.Log("  1. No releases published yet");
                    UpdateDiagnostics.Log("  2. All releases are drafts or pre-releases");
                    UpdateDiagnostics.Log("  3. Repository is private and needs authentication");
                    
                    // Try to get the error message from GitHub
                    try
                    {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        UpdateDiagnostics.Log($"GitHub error: {errorBody}");
                    }
                    catch { }
                    
                    return new UpdateInfo
                    {
                        IsAvailable = false,
                        CurrentVersion = GetCurrentVersion(),
                        Error = "No releases available yet. This is the first version!"
                    };
                }
                
                // Ensure successful response
                response.EnsureSuccessStatusCode();
                
                // Read and parse JSON
                var releaseData = await response.Content.ReadFromJsonAsync<GitHubRelease>().ConfigureAwait(false);
                
                UpdateDiagnostics.Log("Successfully parsed release data");
                
                if (releaseData == null || string.IsNullOrWhiteSpace(releaseData.tag_name))
                {
                    UpdateDiagnostics.Log("Release data is null or empty");
                    return new UpdateInfo 
                    { 
                        IsAvailable = false, 
                        CurrentVersion = GetCurrentVersion(),
                        Error = "Could not parse release data from GitHub"
                    };
                }

                UpdateDiagnostics.Log($"Latest release tag: {releaseData.tag_name}");
                
                // Remove 'v' prefix from tag name if present
                var latestVersion = releaseData.tag_name.TrimStart('v');
                var currentVersion = GetCurrentVersion();

                UpdateDiagnostics.Log($"Comparing: Current={currentVersion}, Latest={latestVersion}");
                
                // Compare versions
                var isNewer = IsVersionNewer(latestVersion, currentVersion);

                UpdateDiagnostics.Log($"Is newer: {isNewer}");

                // Convert GitHub assets to our model
                var assets = releaseData.assets?.Select(a => new ReleaseAsset
                {
                    Name = a.name,
                    BrowserDownloadUrl = a.browser_download_url
                }).ToList();

                UpdateDiagnostics.Log($"Found {assets?.Count ?? 0} assets");

                return new UpdateInfo
                {
                    IsAvailable = isNewer,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = releaseData.html_url,
                    ReleaseNotes = releaseData.body,
                    Assets = assets
                };
            }
            catch (HttpRequestException ex)
            {
                UpdateDiagnostics.Log($"HTTP error: {ex.Message}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = "Unable to check for updates. Please check your internet connection."
                };
            }
            catch (TaskCanceledException ex)
            {
                UpdateDiagnostics.Log($"Timeout: {ex.Message}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = "Update check timed out. Please try again."
                };
            }
            catch (Exception ex)
            {
                UpdateDiagnostics.Log($"Error: {ex.Message}");
                UpdateDiagnostics.Log($"Stack: {ex.StackTrace}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = $"Error: {ex.Message}"
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
        public List<ReleaseAsset>? Assets { get; set; }
    }

    public class ReleaseAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
    }

    // GitHub API response model
    internal class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string html_url { get; set; } = "";
        public string body { get; set; } = "";
        public List<GitHubAsset>? assets { get; set; }
    }

    internal class GitHubAsset
    {
        public string name { get; set; } = "";
        public string browser_download_url { get; set; } = "";
    }
}
