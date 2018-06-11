using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;

namespace Monkland.Patches {
    [MonoModPatch("global::StorySession")]
    class patch_StorySession : StoryGameSession{
        [MonoModIgnore]
        public patch_StorySession(int saveStateNumber, RainWorldGame game) : base( saveStateNumber, game ) {
        }

        [MonoModIgnore]
        public extern void OriginalConstructor(ProcessManager manager);
        [MonoModConstructor, MonoModOriginalName( "OriginalConstructor" )]
        public void ctor_RainWorldGame(ProcessManager manager) {
            OriginalConstructor( manager );

            playerSessionRecords = new PlayerSessionRecord[100];
        }

    }
}