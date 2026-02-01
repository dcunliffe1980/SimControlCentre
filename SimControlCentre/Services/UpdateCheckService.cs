using System;
using System.Threading.Tasks;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Service that manages update checking state and notifies listeners
    /// </summary>
    public class UpdateCheckService
    {
        private readonly UpdateService _updateService;
        private UpdateCheckStatus _status = UpdateCheckStatus.NotStarted;
        private UpdateInfo? _latestUpdateInfo;

        public UpdateCheckService()
        {
            _updateService = new UpdateService();
        }

        /// <summary>
        /// Event fired when the update check status changes
        /// </summary>
        public event EventHandler<UpdateCheckStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// Current status of the update check
        /// </summary>
        public UpdateCheckStatus Status => _status;

        /// <summary>
        /// Latest update info from the last check
        /// </summary>
        public UpdateInfo? LatestUpdateInfo => _latestUpdateInfo;

        /// <summary>
        /// Start checking for updates in the background
        /// </summary>
        public void StartCheckInBackground()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait a bit for app to fully start
                    await Task.Delay(5000).ConfigureAwait(false);

                    UpdateStatus(UpdateCheckStatus.Checking, null);

                    UpdateDiagnostics.Log("[UpdateCheckService] Starting update check...");

                    // Create timeout
                    var updateTask = _updateService.CheckForUpdateAsync();
                    var timeoutTask = Task.Delay(15000);

                    var completedTask = await Task.WhenAny(updateTask, timeoutTask).ConfigureAwait(false);

                    if (completedTask == timeoutTask)
                    {
                        UpdateDiagnostics.Log("[UpdateCheckService] Update check timed out");
                        var errorInfo = new UpdateInfo
                        {
                            IsAvailable = false,
                            CurrentVersion = _updateService.GetCurrentVersion(),
                            Error = "Update check timed out"
                        };
                        UpdateStatus(UpdateCheckStatus.Error, errorInfo);
                        return;
                    }

                    var updateInfo = await updateTask.ConfigureAwait(false);
                    UpdateDiagnostics.Log($"[UpdateCheckService] Check complete. Available: {updateInfo.IsAvailable}, Error: {updateInfo.Error}");

                    if (!string.IsNullOrEmpty(updateInfo.Error))
                    {
                        UpdateStatus(UpdateCheckStatus.Error, updateInfo);
                    }
                    else if (updateInfo.IsAvailable)
                    {
                        UpdateStatus(UpdateCheckStatus.UpdateAvailable, updateInfo);
                    }
                    else
                    {
                        UpdateStatus(UpdateCheckStatus.UpToDate, updateInfo);
                    }
                }
                catch (Exception ex)
                {
                    UpdateDiagnostics.Log($"[UpdateCheckService] Exception: {ex.Message}");
                    var errorInfo = new UpdateInfo
                    {
                        IsAvailable = false,
                        CurrentVersion = _updateService.GetCurrentVersion(),
                        Error = ex.Message
                    };
                    UpdateStatus(UpdateCheckStatus.Error, errorInfo);
                }
            });
        }

        /// <summary>
        /// Manually check for updates now
        /// </summary>
        public async Task<UpdateInfo> CheckNowAsync()
        {
            try
            {
                UpdateStatus(UpdateCheckStatus.Checking, null);
                UpdateDiagnostics.Log("[UpdateCheckService] Manual check started");

                var updateInfo = await _updateService.CheckForUpdateAsync().ConfigureAwait(false);
                UpdateDiagnostics.Log($"[UpdateCheckService] Manual check complete. Available: {updateInfo.IsAvailable}");

                if (!string.IsNullOrEmpty(updateInfo.Error))
                {
                    UpdateStatus(UpdateCheckStatus.Error, updateInfo);
                }
                else if (updateInfo.IsAvailable)
                {
                    UpdateStatus(UpdateCheckStatus.UpdateAvailable, updateInfo);
                }
                else
                {
                    UpdateStatus(UpdateCheckStatus.UpToDate, updateInfo);
                }

                return updateInfo;
            }
            catch (Exception ex)
            {
                UpdateDiagnostics.Log($"[UpdateCheckService] Manual check exception: {ex.Message}");
                var errorInfo = new UpdateInfo
                {
                    IsAvailable = false,
                    CurrentVersion = _updateService.GetCurrentVersion(),
                    Error = ex.Message
                };
                UpdateStatus(UpdateCheckStatus.Error, errorInfo);
                return errorInfo;
            }
        }

        private void UpdateStatus(UpdateCheckStatus newStatus, UpdateInfo? updateInfo)
        {
            _status = newStatus;
            _latestUpdateInfo = updateInfo;

            StatusChanged?.Invoke(this, new UpdateCheckStatusChangedEventArgs(newStatus, updateInfo));
        }
    }

    /// <summary>
    /// Status of the update check
    /// </summary>
    public enum UpdateCheckStatus
    {
        NotStarted,
        Checking,
        UpToDate,
        UpdateAvailable,
        Error
    }

    /// <summary>
    /// Event args for status change events
    /// </summary>
    public class UpdateCheckStatusChangedEventArgs : EventArgs
    {
        public UpdateCheckStatus Status { get; }
        public UpdateInfo? UpdateInfo { get; }

        public UpdateCheckStatusChangedEventArgs(UpdateCheckStatus status, UpdateInfo? updateInfo)
        {
            Status = status;
            UpdateInfo = updateInfo;
        }
    }
}
