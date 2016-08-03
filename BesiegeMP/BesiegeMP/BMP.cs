using System;
using System.Threading;
using System.Threading.Tasks;
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
            Task.Factory.StartNew(Settings.Load);
            myGO.AddComponent<GeneralGUI>();
        }

        public override void OnUnload()
        {
            Object.Destroy(myGO);
            Settings.Save();
        }

        public override string Name => "BesiegeMultiplayerMod";
        public override string DisplayName => "Besiege Mutliplayer Mod";
        public override string Author => "Mortimer";
        public override Version Version => new Version(0, 0, 1);
    }
}
