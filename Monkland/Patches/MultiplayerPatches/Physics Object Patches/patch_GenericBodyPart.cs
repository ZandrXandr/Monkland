using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland.SteamManagement;
using UnityEngine;
using Monkland.UI;

namespace Monkland.Patches {
    [MonoModPatch( "global::GenericBodyPart" )]
    class patch_GenericBodyPart: GenericBodyPart {

        public MonklandUI.QuickDisplayBox posBox;
        public MonklandUI.QuickDisplayBox targetBox;

        [MonoMod.MonoModIgnore]
        public patch_GenericBodyPart(GraphicsModule ow, float rd, float sfFric, float aFric, BodyChunk con) : base( ow, rd, sfFric, aFric, con ) { }

        public void PostUpdate() {
            return;

            if( posBox == null ) {
                posBox = new MonklandUI.QuickDisplayBox() {
                    area = new Rect( pos.x, pos.y, 10, 10 ),
                    color = new Color( 0, 0, 1, 0.2f ),
                    roomID = owner.owner.room.abstractRoom.index
                };
                MonklandUI.AddDisplayBox( posBox );
            }

            if( targetBox == null ) {
                targetBox = new MonklandUI.QuickDisplayBox() {
                    area = new Rect( pos.x, pos.y, 10, 10 ),
                    color = new Color( 1, 0, 0, 0.2f ),
                    roomID = owner.owner.room.abstractRoom.index
                };
                MonklandUI.AddDisplayBox( targetBox );
            }

            patch_BodyPart patchPart = this as BodyPart as patch_BodyPart;

            posBox.area.position = pos;
            targetBox.area.position = patchPart.targetPos;

            lastPos = pos;
            pos = patchPart.targetPos;

            patchPart.lastLerpPos = pos;
            pos = Vector2.Lerp( patchPart.lastLerpPos, patchPart.targetPos, 0.3f );
            patchPart.lastLerpPos = pos;

            posBox.area.position = pos;
            targetBox.area.position = patchPart.targetPos;
        }

    }
}
