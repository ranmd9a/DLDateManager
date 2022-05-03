using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DLDateManager.Utilities
{
    internal static class RegistoryUtil
    {
        public static string? GetInstallDir()
        {
            string steamKeyName = $@"{Registry.LocalMachine}\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980";

            string? value = (string?) Registry.GetValue(steamKeyName, "InstallLocation", null);
            if (value != null)
            {
                return value;
            }

            string oculusKeyName = $@"{Registry.LocalMachine}\Software\WOW6432Node\Oculus VR, LLC\Oculus\Config";
            value = (string?) Registry.GetValue(oculusKeyName, "InitialAppLibrary", null);
            if (value != null)
            {
                return Path.Combine(value, "Software", "hyperbolic-magnetism-beat-saber");
            }
            return null;
        }
    }
}
