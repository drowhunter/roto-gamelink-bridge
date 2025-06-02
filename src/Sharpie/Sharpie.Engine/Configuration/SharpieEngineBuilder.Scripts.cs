using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Sharpie.Engine.Configuration
{
    public partial class SharpieEngineBuilder
    {
        /// <summary>
        /// Adds a single script of the specified type to the engine.
        /// </summary>
        /// <typeparam name="TScript">The type of the script to add.</typeparam>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddScript(Type scriptType)
        {
            if (scriptType.IsNot<ISharpieScript>())
            {
                throw new ArgumentException($"Type {scriptType.Name} is not a valid script type.");
            }

            Services.AddSingleton(typeof(ISharpieScript), scriptType);

            return this;
        }
        
        /// <summary>
        /// Adds a single script of the specified type to the engine.
        /// </summary>
        /// <typeparam name="TScript">The type of the script to add.</typeparam>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddScript<TScript>() where TScript : class, ISharpieScript
        {
            return AddScript(typeof(TScript));
        }

        public SharpieEngineBuilder AddScripts()
        {
            return AddScripts(Assembly.GetEntryAssembly()!);
        }

        /// <summary>
        /// Adds all scripts from the specified assembly to the engine.
        /// </summary>
        /// <param name="assembly">The assembly to scan for script types.</param>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddScripts(Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => t.Is<ISharpieScript>() && !t.IsAbstract).ToList();

            foreach (var type in types)
            {
                AddScript(type);
            }

            return this;
        }

        /// <summary>
        /// Adds all scripts from the assembly of the specified type.
        /// </summary>
        /// <typeparam name="T">The type from which to get the assembly.</typeparam>
        /// <returns>The current instance of <see cref="SharpieEngineBuilder"/>.</returns>
        public SharpieEngineBuilder AddScriptsFrom<T>(T? obj = null) where T : class
        {
            var t = obj?.GetType() ?? typeof(T);
            return AddScripts(t.Assembly);
        }

        public SharpieEngineBuilder AddScripts(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory {path} does not exist.");
            }

            var files = Directory.GetFiles(path, "*.dll");

            foreach (var file in files)
            {
                var assembly = Assembly.LoadFrom(file);
                AddScripts(assembly);
            }

            return this;
        }
    }
}
