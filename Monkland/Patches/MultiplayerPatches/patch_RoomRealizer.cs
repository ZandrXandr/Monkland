using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland.SteamManagement;

namespace Monkland.Patches {

    [MonoModPatch( "global::RoomRealizer" )]
    class patch_RoomRealizer: RoomRealizer {

        public bool isManagerRequest = false;

        [MonoModIgnore]
        public World world;

        [MonoModIgnore]
        public patch_RoomRealizer(AbstractCreature followCreature, World world) : base( followCreature, world ) {
        }


        public extern void orig_KillRoom(AbstractRoom room);
        public void KillRoom(AbstractRoom room) {
            if( isManagerRequest )
                orig_KillRoom( room );
            else
                MonklandSteamManager.RoomManager.SendRoomKillRequest(room);
        }

        public void RemoteKill(AbstractRoom room) {
            UnityEngine.Debug.Log("Remote Kill");
            isManagerRequest = true;
            KillRoom( room );
            isManagerRequest = false;
        }

    }
}