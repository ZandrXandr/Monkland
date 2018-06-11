using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using MonoMod;
using UnityEngine;

namespace Monkland.Patches {
    [MonoModPatch( "global::Menu.MenuDepthIllustration" )]
    class patch_MenuDepthIllustration: Menu.MenuDepthIllustration {
        [MonoModIgnore]
        public patch_MenuDepthIllustration(Menu.Menu menu, MenuObject owner, string folderName, string fileName, Vector2 pos, float depth, MenuShader shader) : base( menu, owner, folderName, fileName, pos, depth, shader ) {
        }

        public extern float orig_DepthAtPosition(Vector2 ps, bool devtool);
        public float DepthAtPosition(Vector2 ps, bool devtool) {
            try {
                return orig_DepthAtPosition(ps, devtool);
            } catch( System.Exception e ) {
                return 0;
            }
        }
    }
}
