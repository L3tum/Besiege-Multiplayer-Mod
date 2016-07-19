using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using spaar.ModLoader;

namespace BesiegeMP
{
    public class BMP : Mod
    {
        public override void OnLoad()
        {
            throw new NotImplementedException();
        }

        public override void OnUnload()
        {
            throw new NotImplementedException();
        }

        public override string Name => "BesiegeMultiplayerMod";
        public override string DisplayName => "Besiege Mutliplayer Mod";
        public override string Author => "Mortimer";
        public override Version Version => new Version(0, 0, 1);
    }
}
