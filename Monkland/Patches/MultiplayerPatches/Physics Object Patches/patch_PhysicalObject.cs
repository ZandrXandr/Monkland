using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::PhysicalObject" )]
    class patch_PhysicalObject: PhysicalObject {

        public ulong ownerID;
        public ulong originalOwner;
        protected System.DateTime lastUpdateTime = System.DateTime.Now;
        public float updateSpeed = 1f / 20f;
        public bool shouldUpdate = true;

        private float g;

        [MonoModIgnore]
        public patch_PhysicalObject(AbstractPhysicalObject abstractPhysicalObject) : base( abstractPhysicalObject ) { }

        public extern void orig_Update(bool eu);
        public void Update(bool eu) {
            orig_Update( eu );
        }

        public virtual void NetworkUpdate() {
            if( lastUpdateTime == null )
                lastUpdateTime = System.DateTime.Now.Add( new TimeSpan( 0, 0, 0, 0, UnityEngine.Random.Range( 0, 500 ) ) );

            if( updateSpeed == 0 ) {
                updateSpeed = 1f / 20f;
            }

            if( ( System.DateTime.Now - lastUpdateTime ).TotalSeconds >= updateSpeed ) {
                lastUpdateTime = System.DateTime.Now;
                MonklandSteamManager.ObjectManager.SendObjectUpdate( this.abstractPhysicalObject );
            }
        }

        public override void Destroy() {
            base.Destroy();
            if( MonklandSteamManager.ObjectManager.OwnsObject( this ) )
                MonklandSteamManager.ObjectManager.SendObjectDestroy( this.abstractPhysicalObject );
        }

        public void PostUpdate() {

            if( bodyChunks != null ) {
                foreach( BodyChunk bc in bodyChunks ) {
                    if( bc == null )
                        continue;
                    patch_BodyChunk patchChunk = bc as patch_BodyChunk;
                    patchChunk.PostUpdate();
                }
            }

            /*if( graphicsModule != null && graphicsModule.bodyParts != null ) {

                foreach( BodyPart part in graphicsModule.bodyParts ) {
                    if( part == null )
                        continue;
                    if( part is GenericBodyPart ) {
                        patch_GenericBodyPart patchPart = part as patch_GenericBodyPart;
                        patchPart.PostUpdate();
                    }
                }

                //GraphicsModuleUpdated( true, true );
            }*/

        }

        public void SetGravity(float f) {
            g = f;
        }

        public void SetOwner(ulong ownerID) {
            this.ownerID = ownerID;
            ( abstractPhysicalObject as patch_AbstractPhysicalObject ).ownerID = ownerID;
        }

    }
}
