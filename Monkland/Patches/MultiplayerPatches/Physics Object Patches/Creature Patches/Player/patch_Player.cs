using Monkland.SteamManagement;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkland.UI;

namespace Monkland.Patches {
    [MonoModPatch( "global::Player" )]
    class patch_Player: patch_Creature {
        [MonoModIgnore]
        public patch_Player(AbstractCreature abstractCreature, World world) : base( abstractCreature, world ) { }

        [MonoModIgnore]
        public int animationFrame;

        public extern void orig_Update(bool eu);
        public void Update(bool eu) {
            try {
                orig_Update( eu );
            } catch( System.Exception e ) {
                UnityEngine.Debug.Log( e );
            }
        }

        public extern void orig_checkInput();
        public void checkInput() {
            if( MonklandSteamManager.ObjectManager.OwnsObject( this ) ) {
                orig_checkInput();
            } else {
                return;
            }
        }

        public override void NetworkUpdate() {
            updateSpeed = 1 / 40f;
            base.NetworkUpdate();
        }

        public void SetAnimFrame(int set) {
            animationFrame = set;
        }

    }
}
