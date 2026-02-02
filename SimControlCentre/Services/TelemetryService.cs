using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Manages telemetry providers and provides unified access to telemetry data
    /// </summary>
    public class TelemetryService : IDisposable
    {
        private readonly List<ITelemetryProvider> _providers = new();
        private ITelemetryProvider? _activeProvider;
        private FlagStatus _lastFlagStatus = FlagStatus.None;
        private bool _isDisposed;
        private readonly TelemetryRecorder _recorder = new();

        /// <summary>
        /// Telemetry recorder for debugging
        /// </summary>
        public TelemetryRecorder Recorder => _recorder;

        /// <summary>
        /// Currently active telemetry provider
        /// </summary>
        public ITelemetryProvider? ActiveProvider => _activeProvider;

        /// <summary>
        /// Is any provider currently connected
        /// </summary>
        public bool IsConnected => _activeProvider?.IsConnected ?? false;

        /// <summary>
        /// Latest telemetry data from active provider
        /// </summary>
        public TelemetryData? LatestData => _activeProvider?.GetLatestData();

        /// <summary>
        /// Event fired when telemetry data is updated
        /// </summary>
        public event EventHandler<TelemetryUpdatedEventArgs>? TelemetryUpdated;

        /// <summary>
        /// Event fired when flag status changes
        /// </summary>
        public event EventHandler<FlagChangedEventArgs>? FlagChanged;

        /// <summary>
        /// Event fired when connection state changes
        /// </summary>
        public event EventHandler<bool>? ConnectionChanged;

        public TelemetryService()
        {
            Logger.Info("Telemetry", "TelemetryService initialized");
        }

        /// <summary>
        /// Register a telemetry provider
        /// </summary>
        public void RegisterProvider(ITelemetryProvider provider)
        {
            if (_providers.Contains(provider))
            {
                Logger.Warning("Telemetry", $"Provider {provider.ProviderName} already registered");
                return;
            }

            Logger.Info("Telemetry", $"Registering provider: {provider.ProviderName}");
            _providers.Add(provider);

            // Subscribe to provider events
            provider.TelemetryUpdated += OnProviderTelemetryUpdated;
            provider.ConnectionChanged += OnProviderConnectionChanged;
        }

        /// <summary>
        /// Start all registered providers
        /// </summary>
        public void StartAll()
        {
            Logger.Info("Telemetry", $"Starting {_providers.Count} provider(s)");
            
            foreach (var provider in _providers)
            {
                try
                {
                    Logger.Info("Telemetry", $"Starting provider: {provider.ProviderName}");
                    provider.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("Telemetry", $"Failed to start provider {provider.ProviderName}", ex);
                }
            }
        }

        /// <summary>
        /// Stop all providers
        /// </summary>
        public void StopAll()
        {
            Logger.Info("Telemetry", "Stopping all providers");
            
            foreach (var provider in _providers)
            {
                try
                {
                    provider.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error("Telemetry", $"Error stopping provider {provider.ProviderName}", ex);
                }
            }

            _activeProvider = null;
        }

        private void OnProviderTelemetryUpdated(object? sender, TelemetryUpdatedEventArgs e)
        {
            if (sender is not ITelemetryProvider provider)
                return;

            // If this provider is now connected, make it the active provider
            if (provider.IsConnected && _activeProvider != provider)
            {
                Logger.Info("Telemetry", $"Switching active provider to: {provider.ProviderName}");
                _activeProvider = provider;
            }

            // Record telemetry if recording is active
            if (_recorder.IsRecording)
            {
                _recorder.RecordSnapshot(e.Data);
            }

            // Check for flag changes
            if (e.Data.CurrentFlag != _lastFlagStatus)
            {
                Logger.Info("Telemetry", $"Flag changed: {_lastFlagStatus} ? {e.Data.CurrentFlag}");
                FlagChanged?.Invoke(this, new FlagChangedEventArgs(_lastFlagStatus, e.Data.CurrentFlag));
                _lastFlagStatus = e.Data.CurrentFlag;
            }

            // Forward telemetry update event
            TelemetryUpdated?.Invoke(this, e);
        }

        private void OnProviderConnectionChanged(object? sender, bool isConnected)
        {
            if (sender is not ITelemetryProvider provider)
                return;

            Logger.Info("Telemetry", $"Provider {provider.ProviderName} connection changed: {isConnected}");

            if (!isConnected && _activeProvider == provider)
            {
                Logger.Info("Telemetry", $"Active provider {provider.ProviderName} disconnected");
                _activeProvider = null;
                _lastFlagStatus = FlagStatus.None;
                
                // Try to find another connected provider
                _activeProvider = _providers.FirstOrDefault(p => p.IsConnected);
                if (_activeProvider != null)
                {
                    Logger.Info("Telemetry", $"Switched to provider: {_activeProvider.ProviderName}");
                }
            }

            ConnectionChanged?.Invoke(this, isConnected);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Logger.Info("Telemetry", "Disposing TelemetryService");
            StopAll();
            _recorder.Dispose();

            foreach (var provider in _providers)
            {
                provider.TelemetryUpdated -= OnProviderTelemetryUpdated;
                provider.ConnectionChanged -= OnProviderConnectionChanged;
            }

            _providers.Clear();
            _isDisposed = true;
        }
    }
}
