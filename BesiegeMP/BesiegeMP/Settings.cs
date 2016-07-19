using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace BesiegeMP
{
    public static class Settings
    {
        private static readonly string SettingsPath = Application.dataPath + "/Mods/settings.bmp.xml";
        public static String Name { get; protected set; }
        public static int Port { get; protected set; }
        public static String ServerName { get; protected set; }
        public static int ServerPort { get; protected set; }
        public static String Location { get; protected set; }
        public static bool getLocation { get; protected set; }


        public static void Save()
        {
            SaveSettings ss = new SaveSettings(Name, Port, ServerName, ServerPort, Location, getLocation);
            new XmlSerializer(typeof(SaveSettings)).Serialize(new StreamWriter(SettingsPath, false), ss);
        }

        public static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                SaveSettings ss = (SaveSettings)new XmlSerializer(typeof(SaveSettings)).Deserialize(new StreamReader(SettingsPath));
                Name = ss.Name;
                Port = ss.Port;
                ServerName = ss.ServerName;
                ServerPort = ss.ServerPort;
                Location = ss.Location;
                getLocation = ss.getLoc;
            }
            else
            {
                Name =
            }
        }
    }

    public class SaveSettings
    {
        public string Name;
        public int Port;
        public String ServerName;
        public int ServerPort;
        public String Location;
        public bool getLoc;

        public SaveSettings(string Name, int Port, string serverName, int serverPort, String location, bool getloc)
        {
            this.Name = Name;
            this.Port = Port;
            ServerName = serverName;
            ServerPort = serverPort;
            Location = location;
            getLoc = getloc;
        }

        public SaveSettings()
        {
        }
    }
}
