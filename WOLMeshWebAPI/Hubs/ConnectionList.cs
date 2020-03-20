using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Hubs
{
    public class ConnectionList
    {
        public class Connection
        {
            public string ConnectionID { get; set; }
            public string ID { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public List<int> AccessibleNetworks { get; set; }
        }
        public List<Connection> Connections { get; set; }

        public ConnectionList()
        {
            Connections = new List<Connection>();
        }
    }
}
