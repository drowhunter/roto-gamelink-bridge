﻿
namespace com.rotovr.sdk;
#if NO_UNITY
public static class Debug
{
    public static void Log(string message)
    {
        System.Console.WriteLine(message);
    }
    public static void LogError(string message)
    {
        System.Console.WriteLine("Error: " + message);
    }

    internal static void LogWarning(string message)
    {
        System.Console.WriteLine("Warning: " + message);
    }
}

#endif