using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::World" )]
    class patch_World: World {

        public bool managerRequested = false;
        public static bool firstRoom;

        [MonoModIgnore]
        public patch_World(RainWorldGame game, Region region, string name, bool singleRoomWorld) : base( game, region, name, singleRoomWorld ) {
        }

        public extern void orig_ActivateRoom(AbstractRoom room);
        public void ActivateRoom(AbstractRoom room) {
            if( firstRoom ) {
                orig_ActivateRoom( room );
                firstRoom = false;
                return;
            }

            if( managerRequested ) {
                orig_ActivateRoom( room );
            } else {
                MonklandSteamManager.RoomManager.SendRoomActivationRequest( room );
            }

        }

        public void RemoteActivate(patch_AbstractRoom absRoom) {
            managerRequested = true;
            this.ActivateRoom( absRoom );
            managerRequested = false;
        }

    }
}
