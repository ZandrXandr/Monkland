using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;

namespace Monkland.Patches {
    class patch_GameSession: GameSession {

        [MonoModIgnore]
        public patch_GameSession(RainWorldGame game) : base( game ) {
        }

        public extern void orig_AddPlayer(AbstractCreature player);
        public void AddPlayer(AbstractCreature player) {
            orig_AddPlayer( player );
            player.ID = new EntityID( player.ID.spawner, Players.Count );
        }
    }
}
