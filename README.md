# Wake On Lan Mesh Helper
<p align="center">
  <img src="https://github.com/andyjmorgan/WOLMesh/blob/master/Icons/icon128.png?raw=true"/>
</p>

The Wake On Lan Mesh helper (WOLMesh from here on in) is designed to help with managing the power of Remote Computers via Wake on Lan. With all the mayhem of COVID-19, many administrators find themselves supporting far more users remotely than ever before. Some have elected to allow their users to logon via Citrix CVAD / VMware Horizon / Microsoft RDP technologies that allow remote pc access.

This use case really falls down when the user accidentally or intentionally shutdown their PC. Or perhaps the PC is powered down due to power policies. Wake on Lan can help here if you happen to be on the same subnet, are able to broadcast and know the mac address of the PC.

WOLMesh aims to help people who have limitations around Wake on Lan today by providing a Web Portal and REST API to allow administrators to view power status of remote machines and power them on via other machines on the same subnet should they need to.

<p align="center">
  <img src="https://github.com/andyjmorgan/WOLMesh/raw/master/WOL%20Mesh%20Preview.png" width=900 height=408>
</p>

## How it works:

Wake on Lan Mesh Helper has three components:

1. A web service / server.
2. An agent to be installed on PC's.
3. (Optional) relays running linux / macos that will relay broadcasts to remote networks.


### WOLMesh Server:

The WOLMesh Server is an ASP.Net Core Web API written in .Net Core 3.1. This service provides the following services:

1. Web API
2. Web Portal (Angular)
3. SignalR Realtime Communication.

The WOLMesh service needs to be run on a central server, or machine that will not be powered down and the agents must be pointed at it. Once the process is started, it accepts connections via SignalR as devices register themselves with the server. Devices are stored in the local.db (sqlite) database.

From the web portal, you can view the registered devices and their power state. You can also power devices on from the console.

From the REST API, you can list and power manage devices in bulk or in an automated fashion.

### WOLMesh Agent:

Requires .Net Framework 4.7.2

The WOLMesh Agent needs to be installed on all devices you wish to manage with WOLMesh. Once installed, you must configure the agent to communicate with the server via a SignalR websocket. Once connected, the agent monitors it's network connection and state and sends it to the server.

Once connected, the device can be leveraged to wake up other devices on the same subnet.

#### How machines are woken:

<p align="center">
  <img src="https://github.com/andyjmorgan/WOLMesh/blob/master/Icons/Peer.png?raw=true">
</p>

When a wake request is sent for a known device, from the Web App or REST API, WOLMesh check's its active connections, if devices are found on the same subnet, subnet local broadcasts are sent from up to 3 devices (configurable) to the mac address of the machine requested.

<p align="center">
  <img src="https://github.com/andyjmorgan/WOLMesh/blob/master/Icons/direct.png?raw=true">
</p>

If the server is on the same subnet as the target device, the server will attempt to also send a wake up.

<p align="center">
  <img src="https://github.com/andyjmorgan/WOLMesh/blob/master/Icons/SubnetDirected.png?raw=true">
</p>

If no devices are online in the target subnet, and the server is not on the same subnet. It will attempt to perform a directed broadcast to the subnet. This will most likely be blocked by most networks, but it never hurts to try ;)

# How to install:

In the publish folder above, you'll find both the agent and server zip file:

## Web Server:

https://github.com/andyjmorgan/WOLMesh/raw/master/Publish

Download the above web service zip file and extract it on a machine (tested on windows 10 / Server 2016). Once extracted, run the following executable with administrator priveledges to start the web service: 

WOLMeshWebAPI.exe

Once started, the webservice will begin listening on the default port of 7443. A self signed certificate will be created on demand. you can then browse to https://server:7443 to begin using the webapp.

**Note:** Please ensure to run the process as administrator to ensure it can bind to the local ip address, for remote access.

### Run as service:

If you would prefer to run this as a service: here's an example command:

```json
New-Service -Name "WOLMeshWebAPI" -DisplayName "Wake on Lan Mesh Web API" -BinaryPathName C:\wolmesh\WOLMeshWebAPI.exe -Description "This Web Application will wake Remote PC's leveraging Wake On Lan" -StartupType Automatic
```

## Agent:

https://github.com/andyjmorgan/WOLMesh/tree/master/Publish

1. On a target machine, install the agent msi file. 
2. Download the nodeconfig.json file and modify the server address

```json
"serveraddress": "https://recording.lab.local:7443",
```

3. Drop the nodeconfig.json file into the installation folder
4. Start the Wake On Lan Mesh Agent service.
5. Your device should now appear in the console.

## Linux / Raspberry Pi:

https://github.com/andyjmorgan/WOLMesh/raw/master/Publish

1. Download the correct version for your build (linux x64, ARM for raspberry pi) above.
2. Extract the contents to /usr/bin/wolmeshclient or another directory in the linux file system.
3. Download the daemon-nodeconfig.json file, modify the serveradddress field and store it in the same directory as the installation.
4. Make the file executable:

```bash
sudo chmod +x /usr/bin/wolmeshclient/WOLMeshClientDaemon
```

5. once it's executable, run:

```bash
sudo /usr/bin/wolmeshclient/./WOLMeshClientDaemon
```

6. The Device should now appear in the console.

### Running as a Daemon:

1. Run the following command to create the service reference:

```bash
sudo nano /etc/systemd/system/WOLMeshDaemon.service
```

2. Add the following text to the config file:

```bash
[Unit]
Description=WOL Mesh Daemon Service
After=network.target
StartLimitIntervalSec=0

[Service]
Type=simple
Restart=always
RestartSec=1
ExecStart=/usr/bin/wolmeshclient/WOLMeshClientDaemon

[Install]
WantedBy=multi-user.target
```

3: reload the daemon, enable the service and start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable WOLMeshDaemon
sudo systemctl start WOLMeshDaemon
```


### Troubleshooting:

Check /var/log/WOLMeshCoreClientDaemon.log

# Known issues:

1. Machines need to be configured to enable Wake on Lan (bios and driver settings). This guide covers it well: https://www.partitionwizard.com/partitionmanager/enable-wake-on-lan.html


# Change Log:

# 1.1 release:

1. Added support for Linux (x64)
2. Added support for Mac OS
3. Added support for Arm (raspberry pi)
4. Added the ability to run web service as service.
5. Added the ability to run the agent / client on linux / arm as a Daemon.
6. Changed the core logic to allow the server to send the Wake On Lan message if on the same subnet.
7. Changed the logic to try a directed broadcast if all else fails.
8. Added Swagger and Swagger UI. (/swagger/index.html)
9. Added the ability to change the default ssl certificate for the web service.

# To Do:

This is my "I gotta do these things next" list. I welcome all suggestions.

1. Implement Authentication.
2.  ~~Allow server to use another ssl cert.~~
3.  ~~Allow wake up of unknown devices.~~
4. ~~Networks view.~~
5. Online devices view.
6. Search.
7.  ~~Run as Service for web service. Stupid .Net Core 3.0 changed everything.~~
8.  ~~Test agent for Linux / Raspberry Pi.~~
9. ~~Add Swagger and test REST API.~~
10. Make the agent a little easier to deploy.
