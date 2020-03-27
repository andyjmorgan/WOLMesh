using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WOLMeshWebAPI.ViewModels;
using static WOLMeshTypes.Models;

namespace WOLMeshWebAPI.Runtime
{
    public class SharedObjects
    {


        public static void LoadConfig()
        {

            string SettingsFilePath = Directory.GetCurrentDirectory() + @"\ServiceSettings.JSON";
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var configString = System.IO.File.ReadAllText(SettingsFilePath);
                    Runtime.SharedObjects.ServiceConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ServiceSettings>(configString);
                    if(ServiceConfiguration.DBVersion == 0)
                    {
                        string dbPath = Directory.GetCurrentDirectory() + @"\local.db";
                        if (System.IO.File.Exists(dbPath))
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info("Deleting old Database");
                            System.IO.File.Delete(dbPath);
                        }
                        ServiceConfiguration.DBVersion = 1;
                        SaveConfig();
                    }
                }
                catch (Exception ex)
                {
                    Runtime.SharedObjects.ServiceConfiguration = new Models.ServiceSettings();
                }
            }
        }

        public static List<ViewModels.RecentActivity> RecentActivity = new List<ViewModels.RecentActivity>();
        public static object RecentActivityLockObject = new object();

        public static void SaveConfig()
        {
            
            string SettingsFilePath = Directory.GetCurrentDirectory() + @"\ServiceSettings.JSON";
            System.IO.File.WriteAllText(SettingsFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(ServiceConfiguration, Newtonsoft.Json.Formatting.Indented));
            var activity = new ViewModels.RecentActivity
            {
                device = "WebService",
                result = true,
                type = ViewModels.RecentActivity.activityType.ServiceSettingsUpdate
            };
            activity.GetActivityDescriptionByType();
            AddActivity(activity);
        }

        public static Models.ServiceSettings ServiceConfiguration = new Models.ServiceSettings();
        public static object connectionsLockObject = new object();
        public static Hubs.ConnectionList connections = new Hubs.ConnectionList();
        public static List<NetworkDetails> localNetworks = new List<NetworkDetails>();

        public static void AddActivity(ViewModels.RecentActivity ra)
        {
            lock (RecentActivityLockObject)
            {
                RecentActivity.Add(ra);
                RecentActivity = RecentActivity.Take(ServiceConfiguration.MaxStoredActivities).ToList();
            }
        }

        public static List<ViewModels.RecentActivity> GetActivity()
        {
            lock (RecentActivityLockObject)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ViewModels.RecentActivity>>(Newtonsoft.Json.JsonConvert.SerializeObject(RecentActivity.OrderByDescending(x=> x.time).ToList(), Newtonsoft.Json.Formatting.Indented));
            }
        }

        public static void ClearActivity()
        {
            lock (RecentActivityLockObject)
            {
                RecentActivity = new List<RecentActivity>();
            }
        }

        public static void AddHubConnection(Hubs.ConnectionList.Connection connection)
        {
            lock (connectionsLockObject)
            {
                var currentConnection = connections.Connections.Where(x => x.ID == connection.ID).FirstOrDefault();
                if (currentConnection == null)
                {
                    connections.Connections.Add(connection);
                    var activity = new RecentActivity
                    {
                        type = ViewModels.RecentActivity.activityType.DeviceOnline,
                        device = connection.name,
                        result = true,
                    };
                    activity.GetActivityDescriptionByType();
                    AddActivity(activity);
                }
                else
                {
                    //update object
                    currentConnection.ID = connection.ID;
                    currentConnection.ConnectionID = connection.ConnectionID;
                    currentConnection.type = connection.type;
                    var activity = new RecentActivity
                    {
                        type = ViewModels.RecentActivity.activityType.DeviceUpdate,
                        device = connection.name,
                        result = true,
                    };
                    activity.GetActivityDescriptionByType();
                    AddActivity(activity);
                }

                

            }
        }
        public static void RemoveHubConnection(string connectionid, string error = "")
        {
            lock (connectionsLockObject)
            {
                List<Hubs.ConnectionList.Connection> toBeRemoved = connections.Connections.Where(x => x.ConnectionID == connectionid).ToList();
                if (toBeRemoved.Count > 0)
                {
                    foreach (var i in toBeRemoved)
                    {
                        var disconnectActivity = new RecentActivity
                        {
                            type = ViewModels.RecentActivity.activityType.DeviceOffline,
                            device = i.name,
                            result = error.Length == 0,
                            errorReason = error,


                        };
                        disconnectActivity.GetActivityDescriptionByType();
                        AddActivity(disconnectActivity);
                    }
                    

                    connections.Connections = connections.Connections.Except(toBeRemoved).ToList();
                }
            }
        }
        public static string GetMachineIDFromSessionID(string sessionID)
        {
            lock (connectionsLockObject)
            {
                var currentConnection = connections.Connections.Where(x => x.ConnectionID == sessionID).FirstOrDefault();
                if (currentConnection != null)
                {
                    return currentConnection.ID;
                }
                return "";
            }
        }

        public static List<Hubs.ConnectionList.Connection> GetOnlineSessions()
        {
            lock (connectionsLockObject)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Hubs.ConnectionList.Connection>>(Newtonsoft.Json.JsonConvert.SerializeObject(connections.Connections));
            }
        }
        public static int GetOnlineSessionsByNetwork(int id)
        {
            lock (connectionsLockObject)
            {
                return connections.Connections.Where(x => x.AccessibleNetworks.Contains(id)).Count();
                //return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Hubs.ConnectionList.Connection>>(Newtonsoft.Json.JsonConvert.SerializeObject();
            }
        }
        public static bool isMachineOnline(string machineID)
        {
            lock (connectionsLockObject)
            {
                return connections.Connections.Where(x => x.ID == machineID).Count() > 0;
            }
        }
    }
}
