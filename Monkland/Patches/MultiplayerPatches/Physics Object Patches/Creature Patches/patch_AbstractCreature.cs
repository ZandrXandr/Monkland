using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod;
using Monkland.SteamManagement;
using Steamworks;

namespace Monkland.Patches {
    [MonoModPatch( "global::AbstractCreature" )]
    class patch_AbstractCreature: AbstractCreature {

        public ulong ownerIDNew {
            get {
                return ( this as AbstractCreature as AbstractPhysicalObject as patch_AbstractPhysicalObject ).ownerID;
            }
            set {
                ( this as AbstractCreature as AbstractPhysicalObject as patch_AbstractPhysicalObject ).ownerID = value;
            }
        }

        [MonoModIgnore]
        public patch_AbstractCreature(World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID) : base( world, creatureTemplate, realizedCreature, pos, ID ) { }

        public void OriginalRealize() {
            try {

                if( this.realizedCreature != null ) {
                    return;
                }

            } catch( Exception e ) {
                Debug.LogError( "Failed On Realize Object Check" );
            }

            try {
                switch( this.creatureTemplate.TopAncestor().type ) {
                    case CreatureTemplate.Type.Slugcat:
                    this.realizedCreature = new Player( this, this.world );
                    break;
                    case CreatureTemplate.Type.LizardTemplate:
                    this.realizedCreature = new Lizard( this, this.world );
                    break;
                    case CreatureTemplate.Type.Fly:
                    this.realizedCreature = new Fly( this, this.world );
                    break;
                    case CreatureTemplate.Type.Leech:
                    this.realizedCreature = new Leech( this, this.world );
                    break;
                    case CreatureTemplate.Type.Snail:
                    this.realizedCreature = new Snail( this, this.world );
                    break;
                    case CreatureTemplate.Type.Vulture:
                    this.realizedCreature = new Vulture( this, this.world );
                    break;
                    case CreatureTemplate.Type.GarbageWorm:
                    GarbageWormAI.MoveAbstractCreatureToGarbage( this, base.Room );
                    this.realizedCreature = new GarbageWorm( this, this.world );
                    break;
                    case CreatureTemplate.Type.LanternMouse:
                    this.realizedCreature = new LanternMouse( this, this.world );
                    break;
                    case CreatureTemplate.Type.CicadaA:
                    this.realizedCreature = new Cicada( this, this.world, this.creatureTemplate.type == CreatureTemplate.Type.CicadaA );
                    break;
                    case CreatureTemplate.Type.Spider:
                    this.realizedCreature = new Spider( this, this.world );
                    break;
                    case CreatureTemplate.Type.JetFish:
                    this.realizedCreature = new JetFish( this, this.world );
                    break;
                    case CreatureTemplate.Type.BigEel:
                    this.realizedCreature = new BigEel( this, this.world );
                    break;
                    case CreatureTemplate.Type.Deer:
                    this.realizedCreature = new Deer( this, this.world );
                    break;
                    case CreatureTemplate.Type.TubeWorm:
                    this.realizedCreature = new TubeWorm( this, this.world );
                    break;
                    case CreatureTemplate.Type.DaddyLongLegs:
                    this.realizedCreature = new DaddyLongLegs( this, this.world );
                    break;
                    case CreatureTemplate.Type.TentaclePlant:
                    if( this.creatureTemplate.type == CreatureTemplate.Type.TentaclePlant ) {
                        this.realizedCreature = new TentaclePlant( this, this.world );
                    } else {
                        this.realizedCreature = new PoleMimic( this, this.world );
                    }
                    break;
                    case CreatureTemplate.Type.MirosBird:
                    this.realizedCreature = new MirosBird( this, this.world );
                    break;
                    case CreatureTemplate.Type.TempleGuard:
                    this.realizedCreature = new TempleGuard( this, this.world );
                    break;
                    case CreatureTemplate.Type.Centipede:
                    case CreatureTemplate.Type.RedCentipede:
                    case CreatureTemplate.Type.Centiwing:
                    case CreatureTemplate.Type.SmallCentipede:
                    this.realizedCreature = new Centipede( this, this.world );
                    break;
                    case CreatureTemplate.Type.Scavenger:
                    this.realizedCreature = new Scavenger( this, this.world );
                    break;
                    case CreatureTemplate.Type.Overseer:
                    this.realizedCreature = new Overseer( this, this.world );
                    break;
                    case CreatureTemplate.Type.VultureGrub:
                    if( this.creatureTemplate.type == CreatureTemplate.Type.VultureGrub ) {
                        this.realizedCreature = new VultureGrub( this, this.world );
                    } else if( this.creatureTemplate.type == CreatureTemplate.Type.Hazer ) {
                        this.realizedCreature = new Hazer( this, this.world );
                    }
                    break;
                    case CreatureTemplate.Type.EggBug:
                    this.realizedCreature = new EggBug( this, this.world );
                    break;
                    case CreatureTemplate.Type.BigSpider:
                    case CreatureTemplate.Type.SpitterSpider:
                    this.realizedCreature = new BigSpider( this, this.world );
                    break;
                    case CreatureTemplate.Type.BigNeedleWorm:
                    if( this.creatureTemplate.type == CreatureTemplate.Type.SmallNeedleWorm ) {
                        this.realizedCreature = new SmallNeedleWorm( this, this.world );
                    } else {
                        this.realizedCreature = new BigNeedleWorm( this, this.world );
                    }
                    break;
                    case CreatureTemplate.Type.DropBug:
                    this.realizedCreature = new DropBug( this, this.world );
                    break;
                }
            } catch( Exception e ) {
                Debug.LogError( e );
                Debug.LogError( string.Format( "Failed on realized object creation, world is {0}, type is {1}, and this is {2}", world, type, this ) );
            }

            try {
                this.InitiateAI();
                for( int i = 0; i < this.stuckObjects.Count; i++ ) {
                    if( this.stuckObjects[i].A.realizedObject == null ) {
                        this.stuckObjects[i].A.Realize();
                    }
                    if( this.stuckObjects[i].B.realizedObject == null ) {
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

                phys.ownerID = ( this as AbstractCreature as AbstractPhysicalObject as patch_AbstractPhysicalObject ).ownerID;
            } catch( Exception e ) {
                Debug.LogError( e );
            }
        }

    }
}
