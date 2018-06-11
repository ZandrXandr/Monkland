using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using UnityEngine;

namespace Monkland.Patches {
    [MonoModPatch("global::BodyPart")]
    class patch_BodyPart: BodyPart {

        public Vector2 lastLerpPos;
        public Vector2 targetPos;

        [MonoModIgnore]
        public patch_BodyPart(GraphicsModule ow) : base( ow ) {}
    }
}
