using System;
using BesiegeMP.CrapForWeb;
using BesiegeMP.Network;

namespace BesiegeMP
{
    static class Util
    {
        public static Network.Network network;
        public static void LocalIPAddress()
        {
            string url = "http://icanhazip.com/";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            Settings.adress = sr.ReadToEnd().Trim();
            if (Settings.getLocation)
            {
                Settings.Location = Web.GetAsyncJSON<Region>("http://ip-api.com/json/" + Settings.adress).country;
            }
        }

        public static void SendGenericMessageAndFigureOutIfSDE(NetworkData networkData)
        {
            if (network.NetworkThread.ServerDistributesEverything && !network.NetworkThread.IsServer)
            {
                lock (network.NetworkThread.MessagesToSendOnce) lock(network.NetworkThread.Server)
                {
                    network.NetworkThread.MessagesToSendOnce.Add(new NetworkData(network.NetworkThread.Server.connectionID, networkData.channelId, networkData.message));
                }
            }
            else if ((network.NetworkThread.ServerDistributesEverything && network.NetworkThread.IsServer) || !network.NetworkThread.ServerDistributesEverything)
            {
                lock (network.NetworkThread.MessagesToSendToEveryone)
                {
                    network.NetworkThread.MessagesToSendToEveryone.Add(networkData);
                }
            }
        }
    }
}
