using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Monkland.SteamManagement;
using Monkland;
using UnityEngine;
using RWCustom;

namespace Monkland.Patches {
    [MonoModPatch( "global::Creature" )]
    class patch_Creature: patch_PhysicalObject {
        [MonoModIgnore]
        public patch_Creature(AbstractCreature abstractCreature, World world) : base( abstractCreature ) {
        }

        [MonoModIgnore]
        public CreatureState state;
        [MonoModIgnore]
        public CreatureTemplate Template;
        [MonoModIgnore]
        public IntVector2? enteringShortCut;

        public IntVector2 suckedIntoPosition;
        public bool isBeingCarriedByOtherCreature;
        public bool spitOutAllSticksAtShortcut;
        public Room spitIntoRoom;

        [MonoModIgnore]
        public extern void OriginalConstructor(AbstractCreature abstractCreature, World world);
        [MonoModConstructor, MonoModOriginalName( "OriginalConstructor" )]
        public void ctor_Creature(AbstractCreature abstractCreature, World world) {
            OriginalConstructor( abstractCreature, world );
        }

        public extern void orig_Update(bool eu);
        public void Update(bool eu) {
            orig_Update( eu );
        }

        public extern void orig_SuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther);
        private void SuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther) {
            orig_SuckedIntoShortCut( entrancePos, carriedByOther );

            isBeingCarriedByOtherCreature = carriedByOther;
            suckedIntoPosition = entrancePos;

            Debug.Log( "Sucked Into Shortcut" );

            if( MonklandSteamManager.ObjectManager.OwnsObject( this ) )
                MonklandSteamManager.ObjectManager.SendObjectEvent( abstractPhysicalObject, NetworkObjectManager.PhysicsObjectHandler.EnterShortcutID);
        }

        public void SuckedIntoShortcutPublic(IntVector2 entrancePos, bool carriedByOther) {
            this.SuckedIntoShortCut( entrancePos, carriedByOther );
        }

        public extern void orig_SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks);
        public void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks) {
            orig_SpitOutOfShortCut( pos, newRoom, spitOutAllSticks );

            suckedIntoPosition = pos;
            spitOutAllSticksAtShortcut = spitOutAllSticks;
            spitIntoRoom = newRoom;

            if( MonklandSteamManager.ObjectManager.OwnsObject( this ) )
                MonklandSteamManager.ObjectManager.SendObjectEvent( abstractPhysicalObject, NetworkObjectManager.PhysicsObjectHandler.ExitShortcutID);
        }

        public extern void orig_Grab(PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying);
        public void Grab(PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying) {
            orig_Grab( obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying );
        }
    }
}
