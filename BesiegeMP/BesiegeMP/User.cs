using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BesiegeMP
{
    [Serializable]
    class User
    {
        [NonSerialized]
        public Dictionary<String, Component> gameObjects = new Dictionary<string, Component>();

        public int connectionID;
        public String name;
        public String adress;
        public int port;
        public int userSocketID;
        public int ID;

        private static int userIDs = -2;

        public static int GetNextID()
        {
            userIDs++;
            return userIDs;
        }
    }
}
