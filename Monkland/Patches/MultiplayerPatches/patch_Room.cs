using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkland.SteamManagement;
using UnityEngine;
using MonoMod;

namespace Monkland.Patches {
    [MonoModPatch( "global::Room" )]
    class patch_Room: Room {

        public static World mainWorld;
        public ulong ownerID;

        [MonoMod.MonoModIgnore]
        public patch_Room(RainWorldGame game, World world, AbstractRoom abstractRoom) : base( game, world, abstractRoom ) { }

        [MonoModIgnore]
        public extern void OriginalConstructor(RainWorldGame game, World world, AbstractRoom abstractRoom);
        [MonoModConstructor, MonoModOriginalName( "OriginalConstructor" )]
        public void ctor_Room(RainWorldGame game, World world, AbstractRoom abstractRoom) {
            OriginalConstructor( game, world, abstractRoom );
            this.physicalObjects = new List<PhysicalObject>[5];
            for( int i = 0; i < this.physicalObjects.Length; i++ ) {
                this.physicalObjects[i] = new List<PhysicalObject>();
            }
        }


        public extern void orig_Update();
        public void Update() {
            if( ownerID == 0 ) {
                ownerID = NetworkGameManager.playerID;
            }

            if( mainWorld == null )
                mainWorld = this.world;

            if( NetworkGameManager.playerID != ownerID ) {
                List<UpdatableAndDeletable> deletes = new List<UpdatableAndDeletable>();

                foreach( UpdatableAndDeletable uad in updateList ) {
                    if( uad is Creature && ( (Creature)uad ).Template.TopAncestor().type == CreatureTemplate.Type.Slugcat ) {

                    } else if( uad is PhysicalObject ) {
                        if( !MonklandSteamManager.ObjectManager.IsNetObject( uad as PhysicalObject ) )
                            deletes.Add( uad );
                    }
                }

                foreach( UpdatableAndDeletable uad in deletes )
                    RemoveObject( uad );
            }

            orig_Update();

            HashSet<string> updatedThisFrame = new HashSet<string>();

            foreach( UpdatableAndDeletable uad in updateList ) {
                if( uad is PhysicalObject ) {
                    //If the UAD is a physics object, but not a creature
                    patch_PhysicalObject physicalObject = uad as patch_PhysicalObject;
                    if( uad.slatedForDeletetion ) {
                        continue;
                    }

                    if( MonklandSteamManager.ObjectManager.OwnsObject( physicalObject ) ) {
                        physicalObject.NetworkUpdate();
                        continue;
                    }

                    if( MonklandSteamManager.ObjectManager.IsNetObject( physicalObject ) ) {
                        physicalObject.PostUpdate();
                        continue;
                    }

                }
            }


        }

    }
}