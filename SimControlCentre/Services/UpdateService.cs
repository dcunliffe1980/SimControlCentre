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
                Timeout = TimeSpan.FromSeconds(10) // Increase timeout
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
                Console.WriteLine($"[UpdateService] Error getting version: {ex.Message}");
            }
            
            // Fallback to hardcoded version
            return "1.1.1";
        }

        /// <summary>
        /// Check if an update is available
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                Console.WriteLine($"[UpdateService] Checking for updates at: {GitHubApiUrl}");
                Console.WriteLine($"[UpdateService] Current version: {GetCurrentVersion()}");
                
                // Test basic internet connectivity first
                try
                {
                    using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var testResponse = await testClient.GetAsync("https://www.google.com");
                    Console.WriteLine($"[UpdateService] Internet connectivity test: {testResponse.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UpdateService] Internet connectivity test failed: {ex.Message}");
                    return new UpdateInfo
                    {
                        IsAvailable = false,
                        CurrentVersion = GetCurrentVersion(),
                        Error = "No internet connection detected. Please check your network settings."
                    };
                }
                
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                
                Console.WriteLine($"[UpdateService] Response status: {response.StatusCode}");
                
                // Handle 404 - no releases exist yet
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("[UpdateService] No releases found (404)");
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
                var releaseData = await response.Content.ReadFromJsonAsync<GitHubRelease>();
                
                Console.WriteLine($"[UpdateService] Successfully parsed release data");
                
                if (releaseData == null || string.IsNullOrWhiteSpace(releaseData.tag_name))
                {
                    Console.WriteLine("[UpdateService] Release data is null or empty");
                    return new UpdateInfo 
                    { 
                        IsAvailable = false, 
                        CurrentVersion = GetCurrentVersion(),
                        Error = "Could not parse release data from GitHub"
                    };
                }

                Console.WriteLine($"[UpdateService] Latest release tag: {releaseData.tag_name}");
                
                // Remove 'v' prefix from tag name if present
                var latestVersion = releaseData.tag_name.TrimStart('v');
                var currentVersion = GetCurrentVersion();

                Console.WriteLine($"[UpdateService] Comparing: Current={currentVersion}, Latest={latestVersion}");
                
                // Compare versions
                var isNewer = IsVersionNewer(latestVersion, currentVersion);

                Console.WriteLine($"[UpdateService] Is newer: {isNewer}");

                return new UpdateInfo
                {
                    IsAvailable = isNewer,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = releaseData.html_url,
                    ReleaseNotes = releaseData.body
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[UpdateService] HTTP error: {ex.Message}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = "Unable to check for updates. Please check your internet connection."
                };
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[UpdateService] Timeout: {ex.Message}");
                return new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = GetCurrentVersion(),
                    Error = "Update check timed out. Please try again."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateService] Error: {ex.Message}");
                Console.WriteLine($"[UpdateService] Stack: {ex.StackTrace}");
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
    }

    // GitHub API response model
    internal class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string html_url { get; set; } = "";
        public string body { get; set; } = "";
    }
}
