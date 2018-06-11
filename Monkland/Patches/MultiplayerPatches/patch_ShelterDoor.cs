using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::ShelterDoor" )]
    class patch_ShelterDoor: ShelterDoor {
        [MonoModIgnore]
        public patch_ShelterDoor(Room room) : base( room ) {
        }

        [MonoModIgnore]
        private float openUpTicks = 350f;

        [MonoModIgnore]
        private float initialWait = 80f;

        public extern void orig_Update(bool eu);
        public override void Update(bool eu) {
            initialWait = 25;
            openUpTicks = 60;
            orig_Update( eu );
        }

    }
}
