using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLMeshFrameworkHelpers
{
    public class RegistryHelpers
    {
        public static string GetMachineDetails()
        {
            string returnValue = "Unknown";
            try
            {
                using (RegistryKey MachineKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var versionKey = MachineKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (versionKey != null)
                        {
                            returnValue = (string)versionKey.GetValue("ProductName", "Unknown");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get Machine Details: " + ex.ToString());
            }

            return returnValue;
        }

        public static string GetMachineID()
        {
            string returnValue = "Unknown";
            try
            {
                using (RegistryKey MachineKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var versionKey = MachineKey.OpenSubKey(@"SOFTWARE\WOLMeshAgent",true))
                    {
                        if (versionKey != null)
                        {
                            string machineName = (string)versionKey.GetValue("MachineName", "");
                            if(machineName == Environment.MachineName)
                            {
                                returnValue = (string)versionKey.GetValue("MachineID", "");
                                if (string.IsNullOrEmpty(returnValue))
                                {
                                    string machineID = Guid.NewGuid().ToString();
                                    versionKey.SetValue("MachineID", machineID);
                                    return machineID;
                                }
                                else
                                {
                                    return returnValue;
                                }
                            }
                            else
                            {
                                string machineID = Guid.NewGuid().ToString();
                                versionKey.SetValue("MachineName", Environment.MachineName);
                                versionKey.SetValue("MachineID", machineID);
                                return machineID;
                            }
                        }
                        else
                        {
                            var key =  MachineKey.CreateSubKey(@"SOFTWARE\WOLMeshAgent",true);
                            key.SetValue("MachineName", Environment.MachineName);
                            string machineID = Guid.NewGuid().ToString();
                            key.SetValue("MachineID", machineID);
                            return machineID;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get Machine Details: " + ex.ToString());
            }

            return returnValue;
        }
    }
}
