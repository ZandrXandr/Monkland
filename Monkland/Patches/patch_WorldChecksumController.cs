using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkland.Patches {
    [MonoMod.MonoModPatch( "global::WorldChecksumController" )]
    class patch_WorldChecksumController{
     
        public bool ControlCheckSum(RainWorld.BuildType type) {
            return true;
        }

    }
}