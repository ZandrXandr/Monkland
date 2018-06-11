using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod;
using Monkland.SteamManagement;
using Monkland.UI;

namespace Monkland.Patches {
    [MonoModPatch( "global::BodyChunk" )]
    class patch_BodyChunk: BodyChunk {

        public Vector2 lastLerpPos;
        public Vector2 targetPos;

        public MonklandUI.QuickDisplayBox posBox;
        public MonklandUI.QuickDisplayBox targetBox;

        [MonoModIgnore]
        public patch_BodyChunk(PhysicalObject owner, int index, Vector2 pos, float rad, float mass) : base( owner, index, pos, rad, mass ) { }

        public void PostUpdate() {
            if( targetPos == Vector2.zero ) {
                return;
            }

            if( posBox == null ) {
                posBox = new MonklandUI.QuickDisplayBox() {
                    area = new Rect( pos.x, pos.y, 10, 10 ),
                    color = new Color( 0, 0, 1, 0.2f ),
                    roomID = owner.room.abstractRoom.index
                };
                MonklandUI.AddDisplayBox( posBox );
            }

            if( targetBox == null ) {
                targetBox = new MonklandUI.QuickDisplayBox() {
                    area = new Rect( pos.x, pos.y, 10, 10 ),
                    color = new Color( 1, 0, 0, 0.2f ),
                    roomID = owner.room.abstractRoom.index
                };
                MonklandUI.AddDisplayBox( targetBox );
            }

            Vector2 newPos = Vector2.Lerp( lastLerpPos, targetPos, 0.3f );
            lastLastPos = lastPos;
            lastPos = lastLerpPos;
            pos = newPos;
            lastLerpPos = newPos;

            posBox.area.position = newPos;
            targetBox.area.position = targetPos;
        }

    }
}
