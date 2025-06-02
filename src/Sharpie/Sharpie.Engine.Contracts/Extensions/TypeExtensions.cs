using Sharpie.Engine.Contracts;

namespace Sharpie.Engine
{
    internal static class TypeExtensions
    {
        public static bool IsNot<T>(this Type type)
            where T : class
            => ! type.Is<T>();

        public static bool Is<T>(this Type type) 
            where T : class         
        {
            return typeof(T).IsAssignableFrom(type);
        }
    }
}