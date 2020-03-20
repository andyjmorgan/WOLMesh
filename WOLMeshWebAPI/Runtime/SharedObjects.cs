using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Runtime
{
    public class SharedObjects
    {

        public static Models.ServiceSettings ServiceConfiguration = new Models.ServiceSettings();
        public static object connectionsLockObject = new object();
        public static Hubs.ConnectionList connections = new Hubs.ConnectionList();

        public static void AddHubConnection(Hubs.ConnectionList.Connection connection)
        {
            lock (connectionsLockObject)
            {
                var currentConnection = connections.Connections.Where(x => x.ID == connection.ID).FirstOrDefault();
                if(currentConnection == null)
                {
                    connections.Connections.Add(connection);
                }
                else
                {
                    //update object
                    currentConnection.ID = connection.ID;
                    currentConnection.ConnectionID = connection.ConnectionID;
                    currentConnection.type = connection.type;
                }

            }
        }
        public static void RemoveHubConnection(string connectionid)
        {
            lock (connectionsLockObject)
            {
                List<Hubs.ConnectionList.Connection> toBeRemoved = connections.Connections.Where(x => x.ConnectionID == connectionid).ToList();
                if(toBeRemoved.Count > 0)
                {
                    connections.Connections = connections.Connections.Except(toBeRemoved).ToList();
                }
            }
        }
        public static string GetMachineIDFromSessionID(string sessionID)
        {
            lock (connectionsLockObject)
            {
                var currentConnection = connections.Connections.Where(x => x.ConnectionID == sessionID).FirstOrDefault();
                if(currentConnection != null)
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
        public static bool isMachineOnline(string machineID)
        {
            lock (connectionsLockObject)
            {
                return connections.Connections.Where(x => x.ID == machineID).Count() > 0;
            }
        }
    }
}
