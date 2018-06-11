using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Monkland.Patches {
    [MonoModPatch( "global::PlayerGraphics" )]
    class patch_PlayerGraphics: PlayerGraphics {
        [MonoModIgnore]
        public patch_PlayerGraphics(PhysicalObject ow) : base( ow ) {}

        [MonoModIgnore]
        private PlayerObjectLooker objectLooker;

        [MonoModIgnore]
        private Vector2 legDirection;

        [MonoModIgnore]
        private class PlayerObjectLooker {
            [MonoModIgnore]
            public Vector2 mostInterestingLookPoint;
        }

        public Vector2 legDirectionGet {
            get {
                return legDirection;
            }
            set {
                legDirection = value;
            }
        }

        public Vector2 lookPoint {
            get {
                return objectLooker.mostInterestingLookPoint;
            }
        }

    }
}
