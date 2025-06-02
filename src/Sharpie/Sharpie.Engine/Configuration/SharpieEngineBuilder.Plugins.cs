using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Sharpie.Engine.Contracts.Plugins;

using System.Reflection;

namespace Sharpie.Engine.Configuration
{

    public partial class SharpieEngineBuilder : ISharpieEngineBuilder
    {
        public SharpieEngineBuilder AddPlugin<TPlugin>()//IPluginSettings settings = null)
            where TPlugin : class, ISharpiePlugin
        {
            return AddPluginInternal<TPlugin>(null);
        }

        public SharpieEngineBuilder AddPlugin<TPlugin, TSettings>(Action<TSettings> setup)
            where TPlugin : class, ISharpiePlugin
            where TSettings : class, IPluginSettings, new()
        {
            if (setup != null)
                return AddPluginInternal<TPlugin>(settings => setup((TSettings)settings));

            return AddPluginInternal<TPlugin>();

        }

        public SharpieEngineBuilder AddPlugins(params IPluginSettings[] settings)
        {
            return AddPluginsFromAssembly(Assembly.GetEntryAssembly(), settings);
        }

        /// <summary>
        /// Adds all plugins from the specified assembly to the engine.
        /// </summary>
        /// <param name="assembly">The assembly to scan for plugin types.</param>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddPluginsFromAssembly(Assembly assembly, params IPluginSettings[] settings)
        {
            if (assembly != null)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Is<ISharpiePlugin>() && !t.IsAbstract);

                foreach (var type in types)
                {
                    var settingType = GetPluginSettingsType(type);
                    IPluginSettings setting = null;
                    if (settingType != null)
                        setting = settings?.FirstOrDefault(s => s.GetType() == settingType);

                    AddPluginByGeneric(type, setting);
                }
            }
            return this;
        }


        /// <summary>
        /// Adds all plugins from the assembly of the specified type.
        /// </summary>
        /// <typeparam name="T">The type from which to get the assembly.</typeparam>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddPluginsFromAssemblyOf<T>(T obj = null, params IPluginSettings[] settings) where T : class
        {
            var t = obj?.GetType() ?? typeof(T);
            return AddPluginsFromAssembly(t.Assembly, settings);
        }

        public SharpieEngineBuilder AddPlugins(string path, params IPluginSettings[] settings)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory {path} does not exist.");
            }

            var files = Directory.GetFiles(path, "*.dll");

            foreach (var file in files)
            {
                var assembly = Assembly.LoadFrom(file);
                AddPluginsFromAssembly(assembly);
            }

            return this;
        }


        /// <summary>
        /// Adds a single plugin of the specified type to the engine.
        /// </summary>
        /// <typeparam name="TPlugin">The type of the plugin to add.</typeparam>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        private SharpieEngineBuilder AddPluginInternal<TPlugin>(Action<object> setup = null)
            where TPlugin : class, ISharpiePlugin
        {
            if (IsPluginConfigurable<TPlugin>(out var settings))
            {
                setup?.Invoke(settings);

                Services.AddSingleton(settings);
            }

            Services.AddSingleton<TPlugin>();

            var pluginType = typeof(TPlugin);

            // Register the Globals
            var globalAttr = pluginType.GetCustomAttribute<GlobalTypeAttribute>();

            if (globalAttr?.Type == null || globalAttr.Type.IsNot<ISharpieGlobal<TPlugin>>())
                throw new InvalidOperationException($"plugin {pluginType.Name} must have a GlobalTypeAttribute defined that implements ISharpieGlobal.");


            bool a = pluginType.Is<IUpdateablePlugin>();
            bool b = globalAttr.Type.Is<IUpdateableGlobal<TPlugin>>();

            if (a != b)
                throw new Exception($"plugin {pluginType.Name} and its global type {globalAttr.Type.Name} must both implement IUpdateablePlugin/IUpdateableSharpieGlobal interface.");



            Services.AddSingleton(globalAttr.Type, sp => GlobalResolver<TPlugin>(sp, globalAttr));

            return this;
        }



        Type GetPluginSettingsType(Type pluginType)
        {
            var configPluginInterface = pluginType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConfigurablePlugin<>));
            if (configPluginInterface != null)
            {
                return configPluginInterface.GetGenericArguments()[0];
            }
            return null;
        }


        private bool IsPluginConfigurable<TPlugin>(out object settings)
            where TPlugin : class, ISharpiePlugin
        {
            Type settingType = GetPluginSettingsType(typeof(TPlugin));
            settings = null;

            if (settingType != null)
            {
                settings = Activator.CreateInstance(settingType);

                if (Configuration != null)
                {
                    var section = Configuration.GetSection($"Sharpie.Engine:Plugins:{typeof(TPlugin).Name}");
                    if (section.Exists())
                        section.Bind(settings);

                }
            }



            return settings != null;
        }




        private static ISharpieGlobal<TPlugin> GlobalResolver<TPlugin>(IServiceProvider sp, GlobalTypeAttribute globalAttr)
            where TPlugin : class, ISharpiePlugin
        {
            var pluginType = typeof(TPlugin);

            var plugin = (ISharpiePlugin)sp.GetRequiredService(pluginType);



            var glob = (ISharpieGlobal<TPlugin>)Activator.CreateInstance(globalAttr.Type);

            glob.plugin = (TPlugin)plugin;

            if (plugin is IUpdateablePlugin p && glob is IUpdateableGlobal<TPlugin> g)
            {
                p.OnUpdated += () => g.OnUpdate?.Invoke();
            }
            sp.GetRequiredService<Warehouse>().AddPlugin(plugin);

            return glob;
        }

        private SharpieEngineBuilder AddPluginByGeneric(Type pluginType, IPluginSettings setting = null)
        {
            if (pluginType == null) throw new ArgumentNullException(nameof(pluginType));
            if (!pluginType.IsClass || pluginType.IsAbstract) throw new ArgumentException("Type must be a non-abstract class.", nameof(pluginType));
            if (!typeof(ISharpiePlugin).IsAssignableFrom(pluginType)) throw new ArgumentException("Type must implement ISharpiePlugin.", nameof(pluginType));

            if (setting == null)
            {
                var method = typeof(SharpieEngineBuilder)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == nameof(AddPlugin) && m.IsGenericMethod && m.GetGenericArguments().Length == 1);

                if (method == null)
                    throw new InvalidOperationException("Could not find generic AddPlugin<TPlugin> method.");

                var genericMethod = method.MakeGenericMethod(pluginType);
                genericMethod.Invoke(this, null);
            }
            else
            {
                var method = typeof(SharpieEngineBuilder)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == nameof(AddPlugin) && m.IsGenericMethod && m.GetGenericArguments().Length == 2);
                if (method == null)
                    throw new InvalidOperationException("Could not find generic AddPlugin<TPlugin,TSetting> method.");

                var genericMethod = method.MakeGenericMethod(pluginType, setting.GetType());

                // invoke AddPlugin<TPlugin, TSettings>(Action<TSettings> setup)
                // with a lambda that sets the setting instance using the settings parameter
                var actionType = typeof(Action<>).MakeGenericType(setting.GetType());

                var copyFrom = GetType().GetMethod(nameof(CopyFrom)).MakeGenericMethod(setting.GetType());

                var setupDelegate = Delegate.CreateDelegate(
                    actionType,
                    this,
                    copyFrom
                );
                //var setupDelegate = Delegate.CreateDelegate(
                //    actionType,
                //    setting,
                //    setting.GetType().GetMethod("CopyFrom") ?? throw new InvalidOperationException($"Type {setting.GetType().Name} must have a CopyFrom method.")
                //);

                // If no CopyFrom method, fallback to property copy
                // But for now, assume CopyFrom exists as a convention

                genericMethod.Invoke(this, [setupDelegate]);

            }


            return this;
        }

        private void CopyFrom<T>(T source, T dest) where T : class, IPluginSettings
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (dest == null) throw new ArgumentNullException(nameof(dest));

            // Assuming T has properties that can be copied
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite && prop.CanRead)
                {
                    var value = prop.GetValue(source);
                    prop.SetValue(dest, value);
                }
            }



        }

    }
}
