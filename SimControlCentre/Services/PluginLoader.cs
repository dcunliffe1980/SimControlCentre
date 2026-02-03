using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SimControlCentre.Contracts;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Service for dynamically loading plugins from DLL files
    /// </summary>
    public class PluginLoader
    {
        private readonly string _pluginsDirectory;
        private readonly IPluginContext _pluginContext;

        public PluginLoader(IPluginContext pluginContext)
        {
            _pluginContext = pluginContext;
            
            // Plugins folder should be relative to the application installation directory
            // This works for both:
            // - Development: <repo>\SimControlCentre\bin\Debug\net8.0-windows\Plugins
            // - Production: C:\Program Files\SimControlCentre\Plugins
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _pluginsDirectory = Path.Combine(appDirectory, "Plugins");
            
            // Ensure directory exists
            Directory.CreateDirectory(_pluginsDirectory);
            Logger.Info("Plugin Loader", $"Plugin directory: {_pluginsDirectory}");
        }

        /// <summary>
        /// Load all plugins from the plugins directory
        /// </summary>
        public List<IPlugin> LoadPlugins()
        {
            var plugins = new List<IPlugin>();

            try
            {
                // Get all DLL files in the plugins directory
                var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                Logger.Info("Plugin Loader", $"Found {dllFiles.Length} DLL file(s) in plugins directory");

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        Logger.Info("Plugin Loader", $"Loading: {Path.GetFileName(dllFile)}");
                        var loadedPlugins = LoadPluginFromFile(dllFile);
                        plugins.AddRange(loadedPlugins);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Plugin Loader", $"Failed to load {Path.GetFileName(dllFile)}", ex);
                    }
                }

                Logger.Info("Plugin Loader", $"Successfully loaded {plugins.Count} plugin(s)");
            }
            catch (Exception ex)
            {
                Logger.Error("Plugin Loader", "Error scanning plugins directory", ex);
            }

            return plugins;
        }

        /// <summary>
        /// Load plugins from a specific DLL file
        /// </summary>
        private List<IPlugin> LoadPluginFromFile(string dllPath)
        {
            var plugins = new List<IPlugin>();

            try
            {
                // Load the assembly
                var assembly = Assembly.LoadFrom(dllPath);
                Logger.Debug("Plugin Loader", $"Assembly loaded: {assembly.FullName}");

                // Find all types that implement IPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                Logger.Info("Plugin Loader", $"Found {pluginTypes.Count} plugin type(s) in {Path.GetFileName(dllPath)}");

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        // Try to create instance - some plugins may need special constructors
                        var plugin = TryCreatePluginInstance(pluginType);
                        
                        if (plugin != null)
                        {
                            // Initialize the plugin
                            plugin.Initialize(_pluginContext);
                            
                            plugins.Add(plugin);
                            Logger.Info("Plugin Loader", $"Loaded plugin: {plugin.DisplayName} v{plugin.Version} by {plugin.Author}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Plugin Loader", $"Failed to instantiate plugin type {pluginType.Name}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Plugin Loader", $"Failed to load assembly {Path.GetFileName(dllPath)}", ex);
                throw;
            }

            return plugins;
        }

        /// <summary>
        /// Try to create an instance of a plugin type
        /// Attempts different constructor patterns
        /// </summary>
        private IPlugin? TryCreatePluginInstance(Type pluginType)
        {
            // Try parameterless constructor first
            var parameterlessConstructor = pluginType.GetConstructor(Type.EmptyTypes);
            if (parameterlessConstructor != null)
            {
                Logger.Debug("Plugin Loader", $"Creating {pluginType.Name} with parameterless constructor");
                return (IPlugin)Activator.CreateInstance(pluginType)!;
            }

            // Try constructor with IPluginContext
            var contextConstructor = pluginType.GetConstructor(new[] { typeof(IPluginContext) });
            if (contextConstructor != null)
            {
                Logger.Debug("Plugin Loader", $"Creating {pluginType.Name} with IPluginContext constructor");
                return (IPlugin)Activator.CreateInstance(pluginType, _pluginContext)!;
            }

            // For plugins that need services, we'll need to handle dependency injection
            // For now, log and skip
            Logger.Warning("Plugin Loader", $"Plugin {pluginType.Name} has no compatible constructor. Skipping.");
            return null;
        }

        /// <summary>
        /// Get lighting plugins from loaded plugins
        /// </summary>
        public static List<ILightingPlugin> GetLightingPlugins(List<IPlugin> plugins)
        {
            return plugins.OfType<ILightingPlugin>().ToList();
        }

        /// <summary>
        /// Get device control plugins from loaded plugins
        /// </summary>
        public static List<IDeviceControlPlugin> GetDeviceControlPlugins(List<IPlugin> plugins)
        {
            Logger.Info("Plugin Loader", $"GetDeviceControlPlugins called with {plugins.Count} plugins");
            
            // Get the IDeviceControlPlugin type that the main app is using
            var mainAppType = typeof(IDeviceControlPlugin);
            Logger.Info("Plugin Loader", $"Main app IDeviceControlPlugin type:");
            Logger.Info("Plugin Loader", $"  Assembly: {mainAppType.Assembly.FullName}");
            Logger.Info("Plugin Loader", $"  Location: {mainAppType.Assembly.Location}");
            
            foreach (var plugin in plugins)
            {
                Logger.Info("Plugin Loader", $"Plugin '{plugin.PluginId}' type: {plugin.GetType().Name}");
                bool isDeviceControl = plugin is IDeviceControlPlugin;
                Logger.Info("Plugin Loader", $"  Is IDeviceControlPlugin: {isDeviceControl}");
                
                var interfaces = plugin.GetType().GetInterfaces();
                Logger.Info("Plugin Loader", $"  Implements {interfaces.Length} interfaces:");
                foreach (var iface in interfaces)
                {
                    Logger.Info("Plugin Loader", $"    - {iface.FullName}");
                    Logger.Info("Plugin Loader", $"      Assembly: {iface.Assembly.FullName}");
                    Logger.Info("Plugin Loader", $"      Location: {iface.Assembly.Location}");
                    
                    // Check if this interface name matches but is from different assembly
                    if (iface.Name == "IDeviceControlPlugin" && iface != mainAppType)
                    {
                        Logger.Warning("Plugin Loader", $"      ?? ASSEMBLY MISMATCH! Plugin interface != Main app interface");
                        Logger.Warning("Plugin Loader", $"         Plugin sees: {iface.Assembly.FullName}");
                        Logger.Warning("Plugin Loader", $"         Main app has: {mainAppType.Assembly.FullName}");
                    }
                }
            }
            
            var result = plugins.OfType<IDeviceControlPlugin>().ToList();
            Logger.Info("Plugin Loader", $"GetDeviceControlPlugins returning {result.Count} plugins");
            return result;
        }
    }
}
