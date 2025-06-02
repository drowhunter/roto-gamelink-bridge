using Microsoft.Extensions.DependencyInjection;

using Sharpie.Engine.Contracts.Plugins;

using System.Reflection;

namespace Sharpie.Engine.Configuration
{
    public interface ISharpieEngineBuilder
    {
        IServiceCollection Services { get; }

        SharpieEngineBuilder AddPlugin<TPlugin>() where TPlugin : class, ISharpiePlugin;

        SharpieEngineBuilder AddPlugin<TPlugin, TSettings>(Action<TSettings> setup) 
            where TPlugin : class, ISharpiePlugin
            where TSettings : class, IPluginSettings, new();
        SharpieEngineBuilder AddPlugins(params IPluginSettings[] settings);
        SharpieEngineBuilder AddPluginsFromAssembly(Assembly assembly, params IPluginSettings[] settings);
        SharpieEngineBuilder AddPlugins(string path, params IPluginSettings[] settings);
        SharpieEngineBuilder AddPluginsFromAssemblyOf<T>(T obj = null, params IPluginSettings[] settings) where T : class;


        SharpieEngineBuilder AddScript(Type scriptType);
        SharpieEngineBuilder AddScript<TScript>() where TScript : class, ISharpieScript;
        SharpieEngineBuilder AddScripts();
        SharpieEngineBuilder AddScripts(Assembly assembly);
        SharpieEngineBuilder AddScripts(string path);
        SharpieEngineBuilder AddScriptsFrom<T>(T? obj = null) where T : class;
    }


}
