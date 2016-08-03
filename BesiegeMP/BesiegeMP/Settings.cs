using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using BesiegeMP.CrapForWeb;
using UnityEngine;

namespace BesiegeMP
{
    public static class Settings
    {
        private static readonly string SettingsPath = Application.dataPath + "/Mods/settings.bmp.xml";
        public static String Name = "";
        public static String ServerName = "";
        public static int Port = 8888;
        public static String Location = "Tannu Tuva";
        public static bool getLocation = true;
        public static String ServerPassword = "";
        public static int maxPlayers = 2;
        public static String adress = "";
        public static int ticks = 32;
        public static bool serverDistributesEverything = false;
        private static Network.Network network;
        private static Timer timer;
        private static int oldTicks = ticks;


        internal static void Save()
        {
            SaveSettings ss = new SaveSettings(Name, ServerName, Port, Location, getLocation, ServerPassword, maxPlayers, ticks, serverDistributesEverything);
            new XmlSerializer(typeof(SaveSettings)).Serialize(new StreamWriter(SettingsPath, false), ss);
        }

        internal static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                SaveSettings ss = (SaveSettings)new XmlSerializer(typeof(SaveSettings)).Deserialize(new StreamReader(SettingsPath));
                if (getLocation)
                {
                    Util.LocalIPAddress();
                }
                else
                {
                    lock (Location)
                    {
                        Location = ss.Location;
                    }
                }
                lock (Name)
                {
                    Name = ss.Name;
                }
                lock (ServerName)
                {
                    ServerName = ss.ServerName;
                }
                    Port = ss.ServerPort;
                getLocation = ss.getLoc;
                lock (ServerPassword)
                {
                    ServerPassword = ss.ServerPass;
                }
                maxPlayers = ss.maxPlayers;
                ticks = ss.ticks;
                serverDistributesEverything = ss.sde;
            }
            else
            {
                Util.LocalIPAddress();
                SetStuff(Location);
            }
        }

        private static void SetStuff(String location)
        {
            try
            {
                Name name = Web.GetAsyncJSON<Name>("http://uinames.com/api/?region=" + Location);
                lock (Name)
                {
                    Name = name.name;
                }
                lock (ServerName)
                {
                    ServerName = name.surname;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Name name = Web.GetAsyncJSON<Name>("http://uinames.com/api/");
                lock (Name)
                {
                    Name = name.name;
                }
                lock (ServerName)
                {
                    ServerName = name.surname;
                }
            }
        }

        private static void CheckTicks(object state)
        {
            if (oldTicks != ticks)
            {
                network.networkThread.ChangeTicks(ticks);
                oldTicks = ticks;
            }
        }

        internal static void SetNetworkAndStartCheckTicks(object net)
        {
            network = (Network.Network)net;
            timer = new Timer(CheckTicks, null, 0, 10000);
        }
    }

    internal class SaveSettings
    {
        public string Name;
        public String ServerName;
        public int ServerPort;
        public String Location;
        public bool getLoc;
        public string ServerPass;
        public int maxPlayers;
        public int ticks;
        public bool sde;

        public SaveSettings(string Name, string serverName, int serverPort, String location, bool getloc, String serverPass, int max, int ticks, bool SdE)
        {
            this.Name = Name;
            ServerName = serverName;
            ServerPort = serverPort;
            Location = location;
            getLoc = getloc;
            ServerPass = serverPass;
            maxPlayers = max;
            this.ticks = ticks;
            sde = SdE;
        }

        public SaveSettings()
        {
        }
    }
}
