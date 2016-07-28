using System;
using spaar.ModLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BesiegeMP
{
    public class BMP : Mod
    {
        private GameObject myGO;

        public override void OnLoad()
        {
            myGO = new GameObject("Multiplayer");
            Settings.Load();
            myGO.AddComponent<GeneralGUI>();
        }

        public override void OnUnload()
        {
            Settings.Save();
            Object.Destroy(myGO);
        }

        public override string Name => "BesiegeMultiplayerMod";
        public override string DisplayName => "Besiege Mutliplayer Mod";
        public override string Author => "Mortimer";
        public override Version Version => new Version(0, 0, 1);
    }
}
