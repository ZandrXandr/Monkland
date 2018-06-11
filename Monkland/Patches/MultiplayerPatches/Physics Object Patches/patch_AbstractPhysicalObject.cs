using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using UnityEngine;
using Monkland.SteamManagement;

namespace Monkland.Patches {
    [MonoModPatch( "global::AbstractPhysicalObject" )]
    class patch_AbstractPhysicalObject: AbstractPhysicalObject {

        public ulong ownerID;

        [MonoModIgnore]
        public patch_AbstractPhysicalObject(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base( world, type, realizedObject, pos, ID ) { }

        public void OriginalRealize() {
            try {
                if( this.realizedObject != null ) {
                    return;
                }
            } catch( Exception e ) {
                Debug.LogError( "Failed On Realize Object Check" );
            }


            try {
                switch( this.type ) {
                    case AbstractPhysicalObject.AbstractObjectType.Rock:
                    this.realizedObject = new Rock( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.Spear:
                    if( ( this as AbstractPhysicalObject as AbstractSpear ).explosive ) {
                        this.realizedObject = new ExplosiveSpear( this, this.world );
                    } else {
                        this.realizedObject = new Spear( this, this.world );
                    }
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.FlareBomb:
                    this.realizedObject = new FlareBomb( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.VultureMask:
                    this.realizedObject = new VultureMask( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.PuffBall:
                    this.realizedObject = new PuffBall( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.DangleFruit:
                    this.realizedObject = new DangleFruit( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.PebblesPearl:
                    this.realizedObject = new PebblesPearl( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer:
                    this.realizedObject = new SLOracleSwarmer( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer:
                    this.realizedObject = new SSOracleSwarmer( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.DataPearl:
                    this.realizedObject = new DataPearl( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.SeedCob:
                    this.realizedObject = new SeedCob( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.WaterNut:
                    if( ( this as AbstractPhysicalObject as WaterNut.AbstractWaterNut ).swollen ) {
                        this.realizedObject = new SwollenWaterNut( this );
                    } else {
                        this.realizedObject = new WaterNut( this );
                    }
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.JellyFish:
                    this.realizedObject = new JellyFish( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.Lantern:
                    this.realizedObject = new Lantern( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.KarmaFlower:
                    this.realizedObject = new KarmaFlower( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.Mushroom:
                    this.realizedObject = new Mushroom( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.VoidSpawn:
                    this.realizedObject = new VoidSpawn( this, ( base.Room.realizedRoom == null ) ? 0f : base.Room.realizedRoom.roomSettings.GetEffectAmount( RoomSettings.RoomEffect.Type.VoidMelt ), base.Room.realizedRoom != null && VoidSpawnKeeper.DayLightMode( base.Room.realizedRoom ) );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant:
                    this.realizedObject = new FirecrackerPlant( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.SlimeMold:
                    this.realizedObject = new SlimeMold( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.FlyLure:
                    this.realizedObject = new FlyLure( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.ScavengerBomb:
                    this.realizedObject = new ScavengerBomb( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.SporePlant:
                    this.realizedObject = new SporePlant( this, this.world );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.EggBugEgg:
                    this.realizedObject = new EggBugEgg( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.NeedleEgg:
                    this.realizedObject = new NeedleEgg( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.DartMaggot:
                    this.realizedObject = new DartMaggot( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.BubbleGrass:
                    this.realizedObject = new BubbleGrass( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.NSHSwarmer:
                    this.realizedObject = new NSHSwarmer( this );
                    break;
                    case AbstractPhysicalObject.AbstractObjectType.OverseerCarcass:
                    this.realizedObject = new OverseerCarcass( this, this.world );
                    break;
                }
            } catch( Exception e ) {
                Debug.LogError( string.Format( "Failed on realized object creation, world is {0}, type is {1}, and this is {2}", world, type, this ) );
            }

            try {
                for( int i = 0; i < this.stuckObjects.Count; i++ ) {
                    if( this.stuckObjects[i].A.realizedObject == null && this.stuckObjects[i].A != this ) {
                        this.stuckObjects[i].A.Realize();
                    }
                    if( this.stuckObjects[i].B.realizedObject == null && this.stuckObjects[i].B != this ) {
                        this.stuckObjects[i].B.Realize();
                    }
                }
            } catch( Exception e ) {
                Debug.LogError( "Failed on Stuck Realization" );
            }
        }
        public void Realize() {
            try {
                OriginalRealize();
            } catch( Exception e ) {
                Debug.LogError( e );
                Debug.LogError( this.type + "|" + this.world + "|" + this.stuckObjects );
            }
            try {
                patch_PhysicalObject phys = ( this.realizedObject as patch_PhysicalObject );
                phys.ownerID = ownerID;
            } catch( Exception e ) {
                Debug.LogError( e );
            }
        }

        public extern void orig_Move(WorldCoordinate coord);
        public void Move(WorldCoordinate coord) {
            orig_Move( coord );

            if( ownerID == NetworkGameManager.playerID )
                MonklandSteamManager.ObjectManager.SendObjectEvent( this, NetworkObjectManager.PhysicsObjectHandler.MoveEventID );
        }

    }
}