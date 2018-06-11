using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland;
using Monkland.SteamManagement;
using Monkland.UI;
using UnityEngine;

namespace Monkland.Patches {
    [MonoModPatch( "global::Weapon" )]
    class patch_Weapon: Weapon {

        [MonoModIgnore]
        public patch_Weapon(AbstractPhysicalObject abstractPhysicalObject, World world) : base( abstractPhysicalObject, world ) {}




    }
}
