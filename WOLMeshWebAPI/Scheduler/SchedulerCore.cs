using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using WOLMeshWebAPI.Hubs;

namespace WOLMeshWebAPI.Scheduler
{
    public class SchedulerCore
    {
        public class MyRegistry : Registry
        {

           private IServiceScopeFactory _ServiceScopeFactory;
            private IHubContext<Hubs.WOLMeshHub> _hubContext;
            //public MyRegistry(IServiceProvider ServiceScopeFactory)
            //{
            //    this._ServiceScopeFactory = (IServiceScopeFactory)ServiceScopeFactory;
            //    RegisterJobs();
            //}

            public MyRegistry(IServiceScopeFactory serviceScopeFactory, IHubContext<Hubs.WOLMeshHub> hubContext)
            {

                this._ServiceScopeFactory = serviceScopeFactory;
                this._hubContext = hubContext;
                RegisterJobs();
            }

            private void RegisterJobs()
            {
                Schedule(new DBMaintenanceJob(_ServiceScopeFactory)).ToRunNow();
                Schedule(new DBMaintenanceJob(_ServiceScopeFactory)).ToRunOnceAt(23, 55).AndEvery(1).Days();
                Schedule(new OnlineDevicesJob(_ServiceScopeFactory, _hubContext)).ToRunNow();
                Schedule(new OnlineDevicesJob(_ServiceScopeFactory, _hubContext)).ToRunEvery(1).Minutes();
            }
            public static void registerHorizonJob()
            {

            }
        }

        public class OnlineDevicesJob : IJob
        {
            private IServiceScopeFactory serviceScopeFactory;
            private IHubContext<Hubs.WOLMeshHub> _hubContext;


            public OnlineDevicesJob(IServiceScopeFactory serviceScopeFactory, IHubContext<Hubs.WOLMeshHub> hubContext)
            {
                this.serviceScopeFactory = serviceScopeFactory;
                this._hubContext = hubContext;
            }

            public void Execute()
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Online Devices Job Running");
                using (var serviceScope = serviceScopeFactory.CreateScope())
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Evaluating manual machines for online status");

                    DB.AppDBContext _context = serviceScope.ServiceProvider.GetService<DB.AppDBContext>();
                    _ = Parallel.ForEach(_context.ManualMachines, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (currentMachine) =>
                    {
                        System.Net.IPAddress[] addresses = null;
                        try
                        {
                            addresses = System.Net.Dns.GetHostAddresses(currentMachine.MachineName);
                        }
                        catch (Exception ex)
                        {
                            addresses = null;
                            NLog.LogManager.GetCurrentClassLogger().Debug("Failed to lookup device {0}", currentMachine);
                        }

                        if (addresses != null)
                        {
                            if (addresses.Length == 1)
                            {
                                var thisMachineIPString = addresses[0].ToString();
                                if (currentMachine.lastKnownIP != thisMachineIPString)
                                {
                                    currentMachine.lastKnownIP = thisMachineIPString;
                                }
                                PingOptions po = new PingOptions
                                {
                                    Ttl = 2 * 1000, // two seconds ttl
                                };

                                Ping _ping = new Ping();
                                PingReply result = _ping.Send(System.Net.IPAddress.Parse(thisMachineIPString), 1 * 1000);
                                NLog.LogManager.GetCurrentClassLogger().Debug("Pinged Machine: {0} - on IP: {1} - Response: {2} - RTT: {3}", currentMachine.MachineName, thisMachineIPString, result.Status.ToString(), result.RoundtripTime);
                                if (result.Status == IPStatus.Success)
                                {
                                    
                                    if (!currentMachine.isOnline)
                                    {
                                        ViewModels.RecentActivity ra = new ViewModels.RecentActivity
                                        {
                                            device = currentMachine.MachineName,
                                            type = ViewModels.RecentActivity.activityType.DeviceOnline,
                                            result = true
                                        };
                                        ra.GetActivityDescriptionByType();
                                        Runtime.SharedObjects.AddActivity(ra);
                                    }
                                    currentMachine.isOnline = true;
                                    currentMachine.LastHeardFrom = DateTime.Now;

                                }
                                else
                                {
                                    if (currentMachine.isOnline)
                                    {
                                        ViewModels.RecentActivity ra = new ViewModels.RecentActivity
                                        {
                                            device = currentMachine.MachineName,
                                            type = ViewModels.RecentActivity.activityType.DeviceOffline,
                                            result = true
                                        };
                                        ra.GetActivityDescriptionByType();
                                        Runtime.SharedObjects.AddActivity(ra);
                                    }
                                    currentMachine.isOnline = false;
                                }
                            }
                            else
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Could not ping {0} as zero or more than 1 ip address was resolved. {1}", currentMachine.MachineName, addresses.Length);
                            }
                        }

                    });

                    _context.SaveChanges();

                }

                Models.ServiceSettings sConfig = Runtime.SharedObjects.ServiceConfiguration;
                if (sConfig.KeepDevicesOnline || sConfig.KeepManualDevicesOnline)
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Online Devices Job Is configured to turn machines on");

                    DateTime CurrentTime = DateTime.Now;
                    DayOfWeek CurrentDay = CurrentTime.DayOfWeek;

                    switch (CurrentDay)
                    {
                        case DayOfWeek.Monday:
                        case DayOfWeek.Tuesday:
                        case DayOfWeek.Wednesday:
                        case DayOfWeek.Thursday:
                        case DayOfWeek.Friday:
                            NLog.LogManager.GetCurrentClassLogger().Debug("Today is a weekday, looking for devices to turn on");

                            DoWake(CurrentTime, sConfig);
                            break;

                        case DayOfWeek.Saturday:
                        case DayOfWeek.Sunday:
                            NLog.LogManager.GetCurrentClassLogger().Debug("Today is a Weekend");

                            if (sConfig.KeepDevicesOnlineAtWeekend)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Today is a Weekend, wakeup is configured for weekends, looking for devices to turn on");
                                DoWake(CurrentTime, sConfig);
                            }
                            break;
                    }
                }

               

            }
                
            private void DoWake(DateTime currentDate, Models.ServiceSettings sConfig)
            {
                int HourOfDay = currentDate.Hour;
                if(HourOfDay >= sConfig.KeepDevicesOnlineStartHour && HourOfDay < sConfig.KeepDevicesOnlineEndHour)
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Curent time is in scope of auto turn on settings.");

                    using (var serviceScope = serviceScopeFactory.CreateScope())
                    {

                        var activeDevices = Runtime.SharedObjects.GetOnlineSessions();
                        DB.AppDBContext _context = serviceScope.ServiceProvider.GetService<DB.AppDBContext>();
                    NLog.LogManager.GetCurrentClassLogger().Debug("Pulling machine detail views to evaluate.");

                        var manualMachines = _context.ManualMachines;

                        foreach(var machine in manualMachines.Where(x=> !x.isOnline).ToList())
                        {
                            if(machine.LastWakeCount < sConfig.MaxWakeRetries)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Current device meets the criteria for power on: {0}", machine.MachineName);
                                var wakeups = Controllers.Helpers.WakeManualMachine(machine, _context, this._hubContext, activeDevices);

                                machine.LastWakeCount += 1;

                                var results = wakeups.Where(x => x.Sent).ToList();
                                if (results.Count > 0)
                                {
                                    var activity = new ViewModels.RecentActivity
                                    {
                                        device = machine.MachineName,
                                        result = true,
                                        type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                    };
                                    activity.GetActivityDescriptionByType();
                                    Runtime.SharedObjects.AddActivity(activity);
                                }
                                else
                                {
                                    var activity = new ViewModels.RecentActivity
                                    {
                                        device = machine.MachineName,
                                        result = false,
                                        type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                        errorReason = "All Attempts made to wake device failed."
                                    };
                                    activity.GetActivityDescriptionByType();
                                    Runtime.SharedObjects.AddActivity(activity);
                                }
                            }
                            else if (machine.LastWakeCount == sConfig.MaxWakeRetries)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Current device has exhausted all retries: {0}", machine.MachineName);

                                var activity = new ViewModels.RecentActivity
                                {
                                    device = machine.MachineName,
                                    result = false,
                                    type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                    errorReason = "All Attempts made to wake device failed."
                                };
                                activity.GetActivityDescriptionByType();
                                Runtime.SharedObjects.AddActivity(activity);
                                machine.LastWakeCount += 1;
                            }
                        }                     
                        
                        var machines = _context.Machines;

                        foreach(DB.MachineItems machine in machines)
                        {
                            if(machine.LastWakeCount < sConfig.MaxWakeRetries)
                            {
                                if (!((machine.DeviceType == WOLMeshTypes.Models.DeviceType.Relay)))
                                {
                                    if (!Runtime.SharedObjects.isMachineOnline(machine.ID))
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Debug("Current device meets the criteria for power on: {0}", machine.HostName);

                                        var wakeups = Controllers.Helpers.WakeMachine(machine, _context, this._hubContext, activeDevices);
                                        

                                        machine.LastWakeCount += 1;

                                        var results = wakeups.Where(x => x.Sent).ToList();
                                        if (results.Count > 0)
                                        {
                                            var activity = new ViewModels.RecentActivity
                                            {
                                                device = machine.HostName,
                                                result = true,
                                                type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                            };
                                            activity.GetActivityDescriptionByType();
                                            Runtime.SharedObjects.AddActivity(activity);
                                        }
                                        else
                                        {
                                            var activity = new ViewModels.RecentActivity
                                            {
                                                device = machine.HostName,
                                                result = false,
                                                type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                                errorReason = "All Attempts made to wake device failed."
                                            };
                                            activity.GetActivityDescriptionByType();
                                            Runtime.SharedObjects.AddActivity(activity);
                                        }

                                    }
                                }
                            }
                            else if(machine.LastWakeCount == sConfig.MaxWakeRetries)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Current device has exhausted all retries: {0}", machine.HostName);

                                var activity = new ViewModels.RecentActivity
                                {
                                    device = machine.HostName,
                                    result = false,
                                    type = ViewModels.RecentActivity.activityType.PolicyWakeUpCall,
                                    errorReason = "All Attempts made to wake device failed."
                                };
                                activity.GetActivityDescriptionByType();
                                Runtime.SharedObjects.AddActivity(activity);
                                machine.LastWakeCount += 1;
                            }
                            
                        }
                        _context.SaveChangesAsync();
                    }
                }
                

            }

        }

       
        public class DBMaintenanceJob : IJob
        {

            private IServiceScopeFactory serviceScopeFactory;

            public DBMaintenanceJob(IServiceScopeFactory serviceScopeFactory)
            {
                this.serviceScopeFactory = serviceScopeFactory;
            }


            public void Execute()
            {
                NLog.LogManager.GetCurrentClassLogger().Info("DB maintenance running");
                try
                {
                    using (var serviceScope = serviceScopeFactory.CreateScope())
                    {

                        var _context = serviceScope.ServiceProvider.GetService<DB.AppDBContext>();
                        var machines = _context.Machines.Select(x => x.ID).ToList();
                        var networks = _context.Networks.Select(x => x.NetworkID).ToList();

                        NLog.LogManager.GetCurrentClassLogger().Info("Machines: {0} - Networks: {1}", machines, networks);
                        var lostConnections = _context.MachineNetworkDetails.Where(x => !machines.Contains(x.DeviceID) || !networks.Contains(x.NetworkID)).ToList();
                        NLog.LogManager.GetCurrentClassLogger().Info("Lost Connections: {0}", lostConnections);
                        if (lostConnections.Count > 0)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Warn("Removing {0} lost connections.", lostConnections.Count());
                            _context.MachineNetworkDetails.RemoveRange(lostConnections);
                            NLog.LogManager.GetCurrentClassLogger().Warn("Removed {0} lost connections, saving...", lostConnections.Count());

                            _context.SaveChanges();
                            NLog.LogManager.GetCurrentClassLogger().Warn("Saved!", lostConnections.Count());

                        }
                    }
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("An exception was caught while trying to clean up the database: {0}", ex.ToString());
                }
            }
        }
    }
}
