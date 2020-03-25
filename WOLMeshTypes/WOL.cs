using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static WOLMeshTypes.Models;

namespace WOLMeshTypes
{

    public class WOL
    {
        //https://stackoverflow.com/questions/861873/wake-on-lan-using-c-sharp

        public static async Task WakeOnLan(WakeUpCall wakeup, List<NetworkDetails> NetworkList)
        {
            try
            {
                byte[] magicPacket = BuildMagicPacket(wakeup.MacAddress);
                foreach (var network in NetworkList)
                {
                    if (network.BroadcastAddress == wakeup.BroadcastAddress && network.SubnetMask == wakeup.SubnetMask)
                    {
                        try
                        {
                            await SendWakeOnLan(network.IPAddress, network.BroadcastAddress, magicPacket);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to send wake on lan to network: " + network.BroadcastAddress + " with Exception: " + ex.ToString());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to send wake on lan to network with Exception: " + ex.ToString());
            }
       }
        public static async Task SUbnetDirectedWakeOnLan(string mac, NetworkDetails network)
        {
            try
            {
                byte[] magicPacket = BuildMagicPacket(mac);
                
                                  
                        try
                        {
                            await SendWakeOnLan(network.IPAddress, network.BroadcastAddress, magicPacket);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to send subnet directed wake on lan to network: " + network.BroadcastAddress + " with Exception: " + ex.ToString());
                        }                 
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send wake on lan to network with Exception: " + ex.ToString());
            }
        }

        static byte[] BuildMagicPacket(string macAddress) // MacAddress in any standard HEX format
        {
            macAddress = Regex.Replace(macAddress, "[: -]", "");
            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    for (int i = 0; i < 6; i++)  //First 6 times 0xff
                    {
                        bw.Write((byte)0xff);
                    }
                    for (int i = 0; i < 16; i++) // then 16 times MacAddress
                    {
                        bw.Write(macBytes);
                    }
                }
                return ms.ToArray(); // 102 bytes magic packet
            }
        }

        static async Task SendWakeOnLan(string localIpAddress, string broadcastAddress, byte[] magicPacket)
        {

            IPAddress localip = null;

            if (string.IsNullOrEmpty(localIpAddress))
            {
                localip = IPAddress.Any;
            }
            else
            {
               localip = IPAddress.Parse(localIpAddress);
            }
            using (UdpClient client = new UdpClient(new IPEndPoint(localip, 0)))
            {
                client.EnableBroadcast = true;
                await client.SendAsync(magicPacket, magicPacket.Length, broadcastAddress, 9);
                await client.SendAsync(magicPacket, magicPacket.Length, broadcastAddress, 7);
                //await client.SendAsync(magicPacket, magicPacket.Length, "255.255.255.255", 7);
                //await client.SendAsync(magicPacket, magicPacket.Length, "255.255.255.255", 9);



            }
        }

        //static async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
        //{
        //    using (UdpClient client = new UdpClient(new IPEndPoint(localIpAddress, 0)))
        //    {
        //        await client.SendAsync(magicPacket, magicPacket.Length, multicastIpAddress.ToString(), 9);
        //        await client.SendAsync(magicPacket, magicPacket.Length, multicastIpAddress.ToString(), 7);
        //        await client.SendAsync(magicPacket, magicPacket.Length, "255.255.255.255", 7);
        //        await client.SendAsync(magicPacket, magicPacket.Length, "255.255.255.255", 9);



        //    }
        //}

    }
}
