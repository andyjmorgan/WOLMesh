using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Models
{
    public class ServiceSettings
    {
        public int httpsPort { get; set; }
        public int relayCount { get; set; }
        public string SSLCertificatePFXFilePath { get; set; }
        public string SSLCertificatePFXPassword { get; set; }

        public ServiceSettings()
        {
            httpsPort = 7443;
            relayCount = 3;
        }


    }
}
