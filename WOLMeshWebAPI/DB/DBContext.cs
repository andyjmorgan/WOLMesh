using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.DB
{

    public class MachineItems
    {
        [Key]
        public string ID { get; set; }
        public string HostName { get; set; }
        public string CurrentUser { get; set; }
        public string DomainName { get; set; }
        public string WindowsVersion { get; set; }
        public DateTime LastHeardFrom { get; set; }
        public string MacAddress { get; set; }
        public string BroadcastAddress { get; set; }
        public string IPAddress { get; set; }
    }
    public class Networks
    {
        [Key]
        public int NetworkID { get; set; }
        public String SubnetMask { get; set; }
        public string BroadcastAddress { get; set; }
    }

    public class DeviceNetworkDetails
    {
        [Key]
        public long internalkey { get; set; }
        public string DeviceID { get; set; }
        public int NetworkID { get; set; }
        public string MacAddress { get; set; }
        public string IPAddress { get; set; }
    }
    public class AppDBContext : DbContext
    {
        public DbSet<MachineItems> Machines { get; set; }
        public DbSet<Networks> Networks { get; set; }
        public DbSet<DeviceNetworkDetails> MachineNetworkDetails { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
            var migrations = this.Database.GetPendingMigrations();
            if (migrations.Count() > 0)
            {
                this.Database.Migrate();
            }

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Local.db");
        }
    }



}
