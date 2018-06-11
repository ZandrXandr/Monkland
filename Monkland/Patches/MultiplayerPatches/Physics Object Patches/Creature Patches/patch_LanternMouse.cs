using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::LanternMouse" )]
    class patch_LanternMouse: patch_Creature {

        [MonoModIgnore]
        public patch_LanternMouse(AbstractCreature abstractCreature, World world) : base( abstractCreature, world ) {
        }

        public extern void orig_Act();
        public void Act() {
            if( ownerID == NetworkGameManager.playerID )
                orig_Act();
        }
    }
}
