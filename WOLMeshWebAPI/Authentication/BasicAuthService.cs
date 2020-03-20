using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Authentication
{
    public class MachineUser
    {
        public string name;
        public string id;

    }
    public interface IAuthenticationService
    {
        MachineUser Login(string username, string password);
    }
    public class BasicAuthService : IAuthenticationService
    {
        public MachineUser Login(string name, string id)
        {
            return new MachineUser
            {
                id = "",
                name = ""
            };
        }
    }
}
