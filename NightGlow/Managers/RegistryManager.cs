﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;

namespace NightGlow.Managers;

internal class RegistryManager
{

    public static object? GetValueOrDefault(string path, string entryName)
    {
        var (hiveName, relativePath) = DeconstructPath(path);
        using var registryKey = GetRegistryKeyFromHiveName(hiveName).OpenSubKey(relativePath, false);
        return registryKey?.GetValue(entryName);
    }

    public static void SetValue(string path, string entryName, object entryValue)
    {
        try
        {
            var (hiveName, relativePath) = DeconstructPath(path);
            using var registryKey = GetRegistryKeyFromHiveName(hiveName).CreateSubKey(relativePath, true);
            registryKey.SetValue(entryName, entryValue);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            RunRegistryCli(new[]
            {
                "add", path,
                "/v", entryName,
                "/d", entryValue.ToString()!,
                "/t", GetRegistryValueType(entryValue.GetType()),
                "/f"
            }, true);
        }
    }

    public static void DeleteValue(string path, string entryName)
    {
        try
        {
            var (hiveName, relativePath) = DeconstructPath(path);
            using var registryKey = GetRegistryKeyFromHiveName(hiveName).OpenSubKey(relativePath, true);
            registryKey?.DeleteValue(entryName, false);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
        {
            RunRegistryCli(new[]
            {
                "delete", path,
                "/v", entryName,
                "/f"
            }, true);
        }
    }

    private static (string hiveName, string relativePath) DeconstructPath(string entryName)
    {
        var separatorIndex = entryName.IndexOf('\\');
        return (entryName[..separatorIndex], entryName[(separatorIndex + 1)..]);
    }

    private static RegistryKey GetRegistryKeyFromHiveName(string hiveName)
    {
        if (string.Equals(hiveName, "HKLM", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(hiveName, "HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
        {
            return Registry.LocalMachine;
        }
        if (string.Equals(hiveName, "HKCU", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(hiveName, "HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
        {
            return Registry.CurrentUser;
        }
        throw new NotSupportedException($"Unsupported or invalid hive '{hiveName}'.");
    }

    private static string GetRegistryValueType(Type type)
    {
        if (type == typeof(int)) return "REG_DWORD";
        if (type == typeof(string)) return "REG_SZ";
        throw new NotSupportedException($"Unsupported registry value type '{type}'.");
    }

    private static void RunRegistryCli(IReadOnlyList<string> arguments, bool asAdmin = false)
    {
        var processInfo = new ProcessStartInfo("reg");

        foreach (var arg in arguments)
            processInfo.ArgumentList.Add(arg);

        if (asAdmin)
        {
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
        }

        using var process = Process.Start(processInfo);
        process?.WaitForExit(3000);
    }

}
