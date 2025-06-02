using Sharpie.Engine.Contracts.Configuration;
using Sharpie.Engine.Contracts.Plugins;

namespace Sharpie.Engine
{
    public class SharpieEngine(
        Warehouse warehouse,
        SharpieEngineSettings configuration,
        IEnumerable<ISharpieScript> scripts,
        ILogger<SharpieEngine> logger
        ) : ISharpieEngine
    {
        private bool _isRunning;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler OnUpdate;
        public event Action<ISharpiePlugin, PluginStateChangedEventArgs>? OnPluginStateChanged;

        public bool IsRunning => _isRunning;

        public void Start(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                logger.LogWarning("Engine is already running. Cannot start again.");
                return;
            }

            logger.LogInformation("Starting Sharpie Engine");

            ResetPlugins();

            _isRunning = true;
            var thread = new Thread(async () =>
            {
                

                await StartScripts();

                OnStarted?.Invoke(this, EventArgs.Empty);
                logger.LogInformation("Sharpie Engine started successfully");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Warehouse._lock.Wait(cancellationToken);
                    try
                    {
                        logger.LogDebug("Active Plugins: {Count}", warehouse.ActivePlugins.Count);
                        foreach (var plugin in warehouse.ActivePlugins)
                        {
                            switch(plugin.State)
                            {
                                case PluginState.NotStarted:
                            
                                    await StartPlugin(plugin);
                                    break;
                                case PluginState.Started:
                                    ExecutePlugin(plugin);
                                    break;
                                case PluginState.Stopped:                                
                                case PluginState.Error:
                                    logger.LogError($"plugin {plugin.GetType().Name} is in error state. Attempting to restart...");
                                    break;
                            }                        
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _isRunning = false;
                        // Handle cancellation
                        break;
                    }
                    catch (Exception ex)
                    {
                        _isRunning = false;
                        // Handle other exceptions
                        logger.LogError($"Error: {ex.Message}");
                        break;
                    }
                    finally
                    {
                        OnUpdate?.Invoke(this, EventArgs.Empty);
                        Warehouse._lock.Release();                    
                        Thread.Sleep(configuration.PollRate);
                    }
                    UpdateScripts();
                }


                await StopScripts();
                StopPlugins();
                OnStopped?.Invoke(this, EventArgs.Empty);

            })
            {
                Name = "SharpieEngine",
                IsBackground = true
            };

            thread.Start();
        }

    

        private async Task<bool> StartPlugin(ISharpiePlugin plugin)
        {
            if (plugin.State == PluginState.Started)
                return true;
        
            try
            {
                logger.LogInformation($"Starting plugin {plugin.GetType().Name}");
                await plugin.Start();
                ChangePluginState(plugin, PluginState.Started);
            

            }
            catch (Exception ex)
            {
                ChangePluginState(plugin, PluginState.Error);
            
                var msg = $"Error starting plugin {plugin.GetType().Name}: {ex.Message}";
            
                logger.LogError(msg);
                throw new Exception(msg, ex);

            }
            return plugin.State == PluginState.Started;
        }

        private void StopPlugins()
        {
            logger.LogInformation("Stopping Sharpie Engine");
            _isRunning = false;
            foreach (var plugin in warehouse.ActivePlugins.Where(p => new PluginState[] { PluginState.Started, PluginState.Error }.Contains(p.State) ))
            {
            
                try
                {
                    logger.LogInformation($"Stopping plugin {plugin.GetType().Name}");
                    plugin.Stop();                
                    ChangePluginState(plugin, PluginState.Stopped);
                
                }
                catch (Exception ex)
                {
                    ChangePluginState(plugin, PluginState.Error);                
                    logger.LogError($"Error stopping plugin {plugin.GetType().Name}: {ex.Message}");
                }
            
            }
       
        }

        private int ResetPlugins()
        {
            int i = 0;
            foreach (var plugin in warehouse.ActivePlugins.Where(p => p.State != PluginState.NotStarted))
            {
                logger.LogDebug($"Resetting plugin {plugin.GetType().Name} to NotStarted state.");
            
                ChangePluginState(plugin, PluginState.NotStarted);
                i++;
            }

            return i;
        }

        private void ExecutePlugin(ISharpiePlugin plugin)
        {
            if(plugin.State != PluginState.Started)
            {
                logger.LogWarning($"plugin {plugin.GetType().Name} is not started. Skipping execution.");
                return;
            }

            try
            {
                plugin.Execute();
            }
            catch (Exception ex)
            {
                ChangePluginState(plugin, PluginState.Error);
                // Handle plugin execution exceptions
                throw new Exception($"Error executing plugin {plugin.GetType().Name}", ex);
            }
        
        }

        private void ChangePluginState(ISharpiePlugin plugin, PluginState newState)
        {
            if (plugin.State == newState)
                return;
            plugin.State = newState;
            OnPluginStateChanged?.Invoke(plugin, new PluginStateChangedEventArgs { State = newState });
        }
    
    
        private async Task StartScripts()
        {
            foreach (var script in scripts)
            {
                try
                {
                    await script.Start();
                    logger.LogInformation($"Script {script.GetType().Name} started successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error starting script {script.GetType().Name}: {ex.Message}");
                }
            }
        }

        private async Task StopScripts()
        {
            foreach (var script in scripts)
            {
                try
                {
                    await script.Stop();
                    logger.LogInformation($"Script {script.GetType().Name} stopped successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error stopping script {script.GetType().Name}: {ex.Message}");
                }
            }
        }

        private void UpdateScripts()
        {
            foreach (var script in scripts)
            {
                try
                {
                    script.Update();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error updating script {script.GetType().Name}: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            
        }
    }
}
