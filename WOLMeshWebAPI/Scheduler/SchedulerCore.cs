using System;
using System.Collections.Generic;
using System.Linq;
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

                Models.ServiceSettings sConfig = Runtime.SharedObjects.ServiceConfiguration;
                if (sConfig.KeepDevicesOnline)
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
