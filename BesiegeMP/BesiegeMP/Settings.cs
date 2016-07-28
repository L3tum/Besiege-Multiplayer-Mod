using System;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using BesiegeMP.CrapForWeb;
using UnityEngine;

namespace BesiegeMP
{
    public static class Settings
    {
        private static readonly string SettingsPath = Application.dataPath + "/Mods/settings.bmp.xml";
        public static String Name { get; set; }
        public static String ServerName { get; set; }
        public static int ServerPort { get; set; }
        public static String Location { get; set; }
        public static bool getLocation { get; set; }
        public static String ServerPassword { get; set; }
        public static int maxPlayers { get; set; }
        public static String adress { get; private set; }


        public static void Save()
        {
            SaveSettings ss = new SaveSettings(Name, ServerName, ServerPort, Location, getLocation, ServerPassword, maxPlayers);
            new XmlSerializer(typeof(SaveSettings)).Serialize(new StreamWriter(SettingsPath, false), ss);
        }

        public static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                SaveSettings ss = (SaveSettings)new XmlSerializer(typeof(SaveSettings)).Deserialize(new StreamReader(SettingsPath));
                Name = ss.Name;
                ServerName = ss.ServerName;
                ServerPort = ss.ServerPort;
                getLocation = ss.getLoc;
                ServerPassword = ss.ServerPass;
                maxPlayers = ss.maxPlayers;
                if (getLocation)
                {
                    adress = Util.LocalIPAddress();
                    Region region = Web.GetAsyncJSON<Region>("http://ip-api.com/json/" + adress);
                    Location = region.country;
                }
                else
                {
                    Location = ss.Location;
                }
            }
            else
            {
                getLocation = true;
                adress = Util.LocalIPAddress();
                Region region = Web.GetAsyncJSON<Region>("http://ip-api.com/json/" + adress);
                Location = region.country;
                try
                {
                    Debug.Log(13);
                    Name name = Web.GetAsyncJSON<Name>("http://uinames.com/api/?region=" + Location);
                    Name = name.name;
                    ServerName = name.surname;
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.Log(14);
                    Name name = Web.GetAsyncJSON<Name>("http://uinames.com/api/");
                    Name = name.name;
                    ServerName = name.surname;
                }
                ServerPassword = "";
                ServerPort = 8888;
                maxPlayers = 2;
            }
            Debug.Log(Name);
            Debug.Log(ServerName);
            Debug.Log(ServerPort);
            Debug.Log(getLocation);
            Debug.Log(ServerPassword);
            Debug.Log(maxPlayers);
            Debug.Log(Location);
        }
    }

    public class SaveSettings
    {
        public string Name;
        public String ServerName;
        public int ServerPort;
        public String Location;
        public bool getLoc;
        public string ServerPass;
        public int maxPlayers;

        public SaveSettings(string Name, string serverName, int serverPort, String location, bool getloc, String serverPass, int max)
        {
            this.Name = Name;
            ServerName = serverName;
            ServerPort = serverPort;
            Location = location;
            getLoc = getloc;
            ServerPass = serverPass;
            maxPlayers = max;
        }

        public SaveSettings()
        {
        }
    }
}
