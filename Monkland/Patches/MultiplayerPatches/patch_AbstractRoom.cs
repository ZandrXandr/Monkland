using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::AbstractRoom" )]
    class patch_AbstractRoom: AbstractRoom {

        public ulong ownerID;

        [MonoModIgnore]
        public patch_AbstractRoom(string name, int[] connections, int index, int swarmRoomIndex, int shelterIndex, int gateIndex) : base( name, connections, index, swarmRoomIndex, shelterIndex, gateIndex ) { }

        public extern void orig_RealizeRoom(World world, RainWorldGame game);
        public void RealizeRoom(World world, RainWorldGame game) {
            orig_RealizeRoom( world, game );
            ( this.realizedRoom as patch_Room ).ownerID = ownerID;
        }

        public extern void orig_AddEntity(AbstractWorldEntity ent);
        public void AddEntity(AbstractWorldEntity ent) {
            orig_AddEntity( ent );
            if( ownerID == NetworkGameManager.playerID ) {
                if( ent is AbstractPhysicalObject ) {
                    patch_AbstractPhysicalObject patchObject = ent as patch_AbstractPhysicalObject;

                    if( patchObject.ownerID == 0 ) {
                        patchObject.ownerID = ownerID;
                        if( patchObject.realizedObject != null )
                            ( patchObject.realizedObject as patch_PhysicalObject ).ownerID = ownerID;

                        MonklandSteamManager.ObjectManager.ownedObjects[patchObject.ID.ToString()] = patchObject;
                    }
                }
            }
        }

        public void MoveEntityToDen(AbstractWorldEntity ent) {
            ent.IsEnteringDen( ent.pos );
            this.entities.Remove( ent );
            if( ent is AbstractCreature ) {
                this.creatures.Remove( (AbstractCreature)ent );
            }
            if( this.entitiesInDens.IndexOf( ent ) == -1 ) {
                this.entitiesInDens.Add( ent );
            }

            if( ent is AbstractPhysicalObject && ( ent as AbstractPhysicalObject ).realizedObject != null )
                MonklandSteamManager.ObjectManager.SendObjectEvent( ent as AbstractPhysicalObject, NetworkObjectManager.PhysicsObjectHandler.EnterDenID, true );
        }

        public void MoveEntityOutOfDen(AbstractWorldEntity ent) {
            ent.IsExitingDen();
            this.entitiesInDens.Remove( ent );
            this.AddEntity( ent );

            if( ent is AbstractPhysicalObject && ( ent as AbstractPhysicalObject ).realizedObject != null )
                MonklandSteamManager.ObjectManager.SendObjectEvent( ent as AbstractPhysicalObject, NetworkObjectManager.PhysicsObjectHandler.EnterDenID, false );
        }

        public extern void orig_Update(int timePassed);
        public void Update(int timePassed) {
            orig_Update( timePassed );

            List<AbstractPhysicalObject> remove = new List<AbstractPhysicalObject>();

            foreach( AbstractWorldEntity we in entities ) {
                if( we is AbstractPhysicalObject ) {
                    patch_AbstractPhysicalObject patchPhys = we as patch_AbstractPhysicalObject;

                    if( !MonklandSteamManager.ObjectManager.IsNetObject( patchPhys ) && !MonklandSteamManager.ObjectManager.OwnsObject( patchPhys ) ) {
                        if( patchPhys.realizedObject != null )
                            patchPhys.realizedObject.Destroy();
                        patchPhys.realizedObject = null;
                        patchPhys.Destroy();
                        remove.Add( patchPhys );
                    }
                }
            }

            foreach( AbstractPhysicalObject phys in remove ) {
                RemoveEntity( phys );
            }

        }

    }
}
