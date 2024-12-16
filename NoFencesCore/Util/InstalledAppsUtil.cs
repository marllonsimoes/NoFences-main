using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoFencesCore
{
    internal class InstalledAppsUtil
    {
        List<string> GetAllInstalledInRegistry()
        {

            List<string> installed = new List<string>();

            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    Debug.WriteLine("============================================");
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        Debug.WriteLine($"{subkey.GetValue("DisplayName")}");
                    }
                }
            }

            Debug.WriteLine("============================================ x64");

            string registry_key64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key64))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    Debug.WriteLine("============================================");
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        foreach (string valueKey in subkey.GetValueNames())
                        {
                            Debug.WriteLine($"{valueKey} - {subkey.GetValue(valueKey)}");
                        }
                    }
                }
            }

            return installed;
        }
    }
}
