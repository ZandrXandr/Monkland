using Monkland.Patches;
using Monkland.UI;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Monkland.SteamManagement {
    class NetworkObjectManager: NetworkManager {

        public const int ENTITY_CHANNEL = 2;
        public byte EntityHandler = 0;

        public const byte CreateEventID = 0;
        public const byte UpdateEventID = 1;
        public const byte DestroyEventID = 2;
        public const byte EventEventID = 3;
        public const byte InfoEventID = 4;

        private int lastPlayerID = 0;

        public Dictionary<string, patch_AbstractPhysicalObject> ownedObjects = new Dictionary<string, patch_AbstractPhysicalObject>();
        public Dictionary<string, patch_AbstractPhysicalObject> networkObjects = new Dictionary<string, patch_AbstractPhysicalObject>();
        public Dictionary<string, ulong> objectOwners = new Dictionary<string, ulong>();

        public HashSet<AbstractPhysicalObject> sendWhenRealizedQueue = new HashSet<AbstractPhysicalObject>();

        private bool sendObjectToOtherOnly = true;

        public override void Reset() {

            //Entities
            ownedObjects.Clear();
            networkObjects.Clear();
            requestedObjects.Clear();
        }

        public override void Update() {

            HashSet<AbstractPhysicalObject> remove = new HashSet<AbstractPhysicalObject>();

            foreach( AbstractPhysicalObject abs in sendWhenRealizedQueue ) {
                if( abs.realizedObject != null ) {
                    SendObjectCreation( abs );
                    remove.Add( abs );
                }
            }

            foreach( AbstractPhysicalObject abs in remove ) {
                sendWhenRealizedQueue.Remove( abs );
            }
        }

        #region Network Data Management

        public override void RegisterHandlers() {
            EntityHandler = MonklandSteamManager.instance.RegisterHandler( ENTITY_CHANNEL, HandlePacketEntities );

            RegisterSyncTrees();
        }

        #region Incoming Packet Handlers
        //Reads packet type and sends the code off to wherever it needs to be
        public void HandlePacketEntities(BinaryReader br, CSteamID sentPlayer) {
            byte messageType = br.ReadByte();

            //Gets the type of handler to use for this object
            byte handlerID = br.ReadByte();

            //Reads the ID of this entity from the packet
            EntityID id = ReadIDFromStream( br );

            switch( messageType ) {

                //Create
                case CreateEventID:
                HandleCreatePacketRead( handlerID, id, br, sentPlayer );
                return;
                //Update
                case UpdateEventID:
                HandleUpdatePacketRead( handlerID, id, br, sentPlayer );
                return;
                //Destroy
                case DestroyEventID:
                HandleDestroyPacketRead( handlerID, id, br, sentPlayer );
                return;
                //Event
                case EventEventID:
                HandleEventPacketRead( handlerID, id, br, sentPlayer );
                return;

                case InfoEventID:
                OnObjectInfoRequested( id, br, sentPlayer );
                return;
            }
        }

        //Handles object creation
        public void HandleCreatePacketRead(byte handlerID, EntityID objectID, BinaryReader br, CSteamID sentPlayer) {
            if( requestedObjects.Contains( objectID + "|" + sentPlayer ) )
                requestedObjects.Remove( objectID + "|" + sentPlayer );

            if( networkObjects.ContainsKey( objectID.ToString() ) )
                return;

            patch_AbstractPhysicalObject obj = HandleObjectCreationRead( handlerID, objectID, br ) as patch_AbstractPhysicalObject;

            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.handlers[handlerID];

            objectOwners.Add( obj.ID.ToString(), sentPlayer.m_SteamID );
            networkObjects.Add( objectID.ToString(), obj );
            obj.ownerID = sentPlayer.m_SteamID;

            handler.RealizeCreatedObject( obj );

            obj.ownerID = sentPlayer.m_SteamID;
            ( obj.realizedObject as patch_PhysicalObject ).ownerID = sentPlayer.m_SteamID;

            if( obj.realizedObject != null ) {
                ( obj.realizedObject as patch_PhysicalObject ).ownerID = sentPlayer.m_SteamID;
            }

            string logString = string.Format(
                "Created {0} from player {1} in room {2}",
                obj.ID,
                SteamFriends.GetFriendPersonaName( (CSteamID)obj.ownerID ),
                obj.Room.index
            );

            Debug.Log( logString );

            MonklandUI.AddMessage( logString );
        }

        //Handles object updates
        public void HandleUpdatePacketRead(byte handlerID, EntityID objectID, BinaryReader br, CSteamID sentPlayer) {

            if( !networkObjects.ContainsKey( objectID.ToString() ) ) {
                RequestUpdatedObjectInfo( handlerID, objectID, sentPlayer );
            } else {
                patch_AbstractPhysicalObject obj = networkObjects[objectID.ToString()];

                if( sentPlayer.m_SteamID != obj.ownerID ) {
                    Debug.LogError( string.Format( "Mismatched owner ID for object {0} , ids are {1} and {2}", objectID, sentPlayer.m_SteamID, obj.ownerID ) );
                    return;
                }

                ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( obj.realizedObject.GetType() );

                handler.ReadObjectUpdate( obj, br );
            }
        }

        //Handles object destruction
        public void HandleDestroyPacketRead(byte handlerID, EntityID objectID, BinaryReader br, CSteamID sentPlayer) {
            if( !networkObjects.ContainsKey( objectID.ToString() ) )
                return;
            patch_AbstractPhysicalObject obj = networkObjects[objectID.ToString()];

            if( sentPlayer.m_SteamID != obj.ownerID )
                return;

            HandleObjectDestroyRead( handlerID, obj, br );

            obj.Destroy();
            networkObjects.Remove( objectID.ToString() );

            MonklandUI.AddMessage( string.Format(
                "Destroyed object {0} in room {1}",
                obj.type,
                obj.Room.index
            ) );
        }

        //Handles object events
        public void HandleEventPacketRead(byte handlerID, EntityID objectID, BinaryReader br, CSteamID sentPlayer) {

            if( !networkObjects.ContainsKey( objectID.ToString() ) ) {
                RequestUpdatedObjectInfo( handlerID, objectID, sentPlayer );
                return;
            }

            byte eventID = br.ReadByte();
            patch_AbstractPhysicalObject obj = networkObjects[objectID.ToString()];

            if( sentPlayer.m_SteamID != obj.ownerID )
                return;

            HandleObjectEventRead( handlerID, obj, br, eventID );
        }

        #endregion

        #region Outgoing Packet Handlers

        //Handles object creation
        public void SendObjectCreation(AbstractPhysicalObject absObject) {

            if( absObject.realizedObject == null ) {
                sendWhenRealizedQueue.Add( absObject );
                return;
            }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            PhysicalObject physObject = absObject.realizedObject;

            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( physObject.GetType() );
            patch_AbstractPhysicalObject patched = absObject as patch_AbstractPhysicalObject;

            //Write message type
            writer.Write( CreateEventID );

            //Write the handler ID for this physics object
            writer.Write( handler.objectTypeHandlerID );

            //Write the Physics Object ID
            WriteIDToStream( patched.ID, writer );

            handler.WriteObjectCreation( patched, writer );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacketToAll( packet, sendObjectToOtherOnly, EP2PSend.k_EP2PSendReliable );

            ownedObjects.Add( patched.ID.ToString(), patched );
            Debug.Log( "Object " + patched.ID.ToString() + " added to owned objects" );
        }

        //Handles object updates
        public void SendObjectUpdate(AbstractPhysicalObject absObject) {

            if( absObject.realizedObject == null ) {
                if( !sendWhenRealizedQueue.Contains( absObject ) )
                    sendWhenRealizedQueue.Add( absObject );
                return;
            }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            packet.priority = 1;
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            PhysicalObject physObject = absObject.realizedObject;

            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( physObject.GetType() );

            //Write message type
            writer.Write( UpdateEventID );

            //Write the handler ID for this physics object
            writer.Write( handler.objectTypeHandlerID );

            //Write the Physics Object ID
            WriteIDToStream( absObject.ID, writer );

            try {
                handler.WriteObjectUpdate( absObject, writer );
                MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
                MonklandSteamManager.instance.SendPacketToAll( packet, sendObjectToOtherOnly, EP2PSend.k_EP2PSendUnreliableNoDelay );
            } catch( System.Exception e ) {
                Debug.LogError( e );
            }
        }

        //Handles object destruction
        public void SendObjectDestroy(AbstractPhysicalObject absObject) {

            if( !OwnsObject( absObject as patch_AbstractPhysicalObject ) )
                return;

            if( absObject.realizedObject == null ) {
                sendWhenRealizedQueue.Add( absObject );
                return;
            }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            PhysicalObject physObject = absObject.realizedObject;

            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( physObject.GetType() );

            //Write message type
            writer.Write( DestroyEventID );

            //Write the handler ID for this physics object
            writer.Write( handler.objectTypeHandlerID );

            //Write the Physics Object ID
            WriteIDToStream( absObject.ID, writer );

            //Write all the other object shit
            handler.WriteObjectDestruction( absObject, writer );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacketToAll( packet, sendObjectToOtherOnly, EP2PSend.k_EP2PSendReliable );
        }

        //Handles object events
        public void SendObjectEvent(AbstractPhysicalObject absObject, byte eventID, params object[] extraData) {

            if( !OwnsObject( absObject as patch_AbstractPhysicalObject ) )
                return;

            if( absObject.realizedObject == null ) {
                sendWhenRealizedQueue.Add( absObject );
                return;
            }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            PhysicalObject physObject = absObject.realizedObject;

            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( physObject.GetType() );

            //Write message type
            writer.Write( EventEventID );

            //Write the handler ID for this physics object
            writer.Write( handler.objectTypeHandlerID );

            //Write the Physics Object ID
            WriteIDToStream( absObject.ID, writer );

            writer.Write( eventID );
            handler.WriteObjectEvent( absObject, writer, eventID, extraData );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacketToAll( packet, sendObjectToOtherOnly, EP2PSend.k_EP2PSendReliable );
        }

        #endregion

        #endregion

        #region Re-Request Object

        private HashSet<string> requestedObjects = new HashSet<string>();

        public void RequestUpdatedObjectInfo(byte handlerId, EntityID id, CSteamID owner) {
            if( requestedObjects.Contains( id + "|" + owner ) )
                return;
            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            //Write message type
            writer.Write( InfoEventID );

            //Write the handler ID for this physics object
            writer.Write( handlerId );

            //Write the Physics Object ID
            WriteIDToStream( id, writer );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacket( packet, owner, EP2PSend.k_EP2PSendReliable );

            Debug.Log( "Requesting object indo for object ID " + id.ToString() );
            MonklandUI.AddMessage( "Requested info for object " + id.ToString() );
            requestedObjects.Add( id + "|" + owner );
        }

        public void OnObjectInfoRequested(EntityID objectID, BinaryReader br, CSteamID sentPlayer) {

            if( !ownedObjects.ContainsKey( objectID.ToString() ) )
                return;

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( ENTITY_CHANNEL, EntityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            AbstractPhysicalObject physObject = ownedObjects[objectID.ToString()];
            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.GetObjectHandler( physObject.realizedObject.GetType() );

            //Write message type
            writer.Write( CreateEventID );

            //Write the handler ID for this physics object
            writer.Write( handler.objectTypeHandlerID );

            //Write the Physics Object ID
            WriteIDToStream( physObject.ID, writer );

            handler.WriteObjectCreation( physObject, writer );

            MonklandUI.AddMessage( "Sending info for object " + objectID.ToString() );
            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacket( packet, sentPlayer, EP2PSend.k_EP2PSendReliable );
        }

        #endregion

        #region Packet Reading and Writing 

        #region IDs for object handlers
        public byte BaseObjectHandler = 0;
        public byte CarryableHandler = 0;
        public byte WeaponHandler = 0;
        public byte SpearHandler = 0;

        public byte CreatureHandler = 0;
        public byte PlayerHandler = 0;

        public byte WaterNutHandler = 0;

        public byte GraphicsModuleObjectHandler = 0;
        public byte PlayerModuleObjectHandler = 0;

        public byte BodyPartObjectHandler = 0;
        public byte LimbObjectHandler = 0;
        #endregion

        public SyncTree<AbstractPhysicalObject> physObjectSyncTree;
        public SyncTree<GraphicsModule> graphModuleSyncTree;
        public SyncTree<BodyPart> bodyPartSyncTree;

        public void RegisterSyncTrees() {

            physObjectSyncTree = new SyncTree<AbstractPhysicalObject>();
            graphModuleSyncTree = new SyncTree<GraphicsModule>();
            bodyPartSyncTree = new SyncTree<BodyPart>();

            physObjectSyncTree.defaultType = typeof( PhysicalObject );

            //Register Phys Object handlers
            {
                BaseObjectHandler = physObjectSyncTree.RegisterObjectHandler( new PhysicsObjectHandler(), typeof( PhysicalObject ) );
                CarryableHandler = physObjectSyncTree.RegisterObjectHandler( new CarryableItemHandler(), typeof( PlayerCarryableItem ) );
                WeaponHandler = physObjectSyncTree.RegisterObjectHandler( new WeaponObjectHandler(), typeof( Weapon ) );
                SpearHandler = physObjectSyncTree.RegisterObjectHandler( new SpearObjectHandler(), typeof( Spear ) );

                CreatureHandler = physObjectSyncTree.RegisterObjectHandler( new CreatureObjectHandler(), typeof( Creature ) );
                PlayerHandler = physObjectSyncTree.RegisterObjectHandler( new PlayerObjectHandler(), typeof( Player ) );

                WaterNutHandler = physObjectSyncTree.RegisterObjectHandler( new WaterNutObjectHandler(), typeof( WaterNut ) );
            }


            //Register Graphics Module sync trees
            {
                GraphicsModuleObjectHandler = graphModuleSyncTree.RegisterObjectHandler( new GraphicsModuleHandler(), typeof( GraphicsModule ) );
                PlayerModuleObjectHandler = graphModuleSyncTree.RegisterObjectHandler( new PlayerGraphicsHandler(), typeof( PlayerGraphics ) );
            }

            //Register Body Part sync trees
            {
                BodyPartObjectHandler = bodyPartSyncTree.RegisterObjectHandler( new BodyPartHandler(), typeof( BodyPart ) );
                LimbObjectHandler = bodyPartSyncTree.RegisterObjectHandler( new LimbHandler(), typeof( Limb ) );
            }

            foreach( ObjectHandler<AbstractPhysicalObject> handler in physObjectSyncTree.handlers )
                handler.RegisterEvents();
            foreach( ObjectHandler<GraphicsModule> handler in graphModuleSyncTree.handlers )
                handler.RegisterEvents();
            foreach( ObjectHandler<BodyPart> handler in bodyPartSyncTree.handlers )
                handler.RegisterEvents();
        }

        #region Delegate functions
        private void HandleObjectCreationWrite(int handlerID, AbstractPhysicalObject obj, BinaryWriter writer) {
            physObjectSyncTree.handlers[handlerID].WriteObjectCreation( obj, writer );
        }
        private AbstractPhysicalObject HandleObjectCreationRead(int handlerID, EntityID id, BinaryReader reader) {
            ObjectHandler<AbstractPhysicalObject> handler = physObjectSyncTree.handlers[handlerID];

            AbstractPhysicalObject absObject = handler.ReadObjectCreation( id, reader );

            return absObject;
        }

        private void HandleObjectUpdateWrite(int handlerID, AbstractPhysicalObject obj, BinaryWriter writer) {
            physObjectSyncTree.handlers[handlerID].WriteObjectUpdate( obj, writer );
        }
        private void HandleObjectUpdateRead(int handlerID, AbstractPhysicalObject obj, BinaryReader reader) {
            physObjectSyncTree.handlers[handlerID].ReadObjectUpdate( obj, reader );
        }

        private void HandleObjectDestroyWrite(int handlerID, AbstractPhysicalObject obj, BinaryWriter writer) {
            physObjectSyncTree.handlers[handlerID].WriteObjectDestruction( obj, writer );
        }
        private void HandleObjectDestroyRead(int handlerID, AbstractPhysicalObject obj, BinaryReader reader) {
            physObjectSyncTree.handlers[handlerID].ReadObjectDestruction( obj, reader );
        }

        private void HandleObjectEventWrite(int handlerID, AbstractPhysicalObject obj, BinaryWriter writer, byte eventID, object[] extraData) {
            physObjectSyncTree.handlers[handlerID].WriteObjectEvent( obj, writer, eventID, extraData );
        }
        private void HandleObjectEventRead(int handlerID, AbstractPhysicalObject obj, BinaryReader reader, byte eventID) {
            physObjectSyncTree.handlers[handlerID].ReadObjectEvent( obj, reader, eventID );
        }
        #endregion


        #region Physics Object Sync Tree
        //Base physics object handler
        public class PhysicsObjectHandler: ObjectHandler<AbstractPhysicalObject> {

            public static byte MoveEventID = 0;
            public static byte EnterDenID = 0;
            public static byte EnterShortcutID = 0;
            public static byte ExitShortcutID = 0;

            public override void WriteObjectCreation(AbstractPhysicalObject absObject, BinaryWriter writer) {
                //Write object type
                writer.Write( (byte)absObject.type );

                if( absObject is AbstractConsumable ) {
                    AbstractConsumable consumable = absObject as AbstractConsumable;

                    writer.Write( consumable.originRoom );
                    writer.Write( consumable.placedObjectIndex );

                    writer.Write( consumable.minCycles );
                    writer.Write( consumable.maxCycles );

                    writer.Write( consumable.isConsumed );
                }

                //Write object position
                WriteWorldCoordToStream( absObject.pos, writer );
            }
            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {

                EntityID realID = (EntityID)id;

                //Read object type
                AbstractPhysicalObject.AbstractObjectType objectType = (AbstractPhysicalObject.AbstractObjectType)reader.ReadByte();
                //Read object position
                WorldCoordinate objectCoords = ReadWorldCoordFromStream( reader );

                //Create object
                AbstractPhysicalObject abstractObject;

                if( AbstractConsumable.IsTypeConsumable( objectType ) ) {
                    AbstractConsumable absConsumable = new AbstractConsumable( patch_RainWorldGame.mainGame.world, objectType, null, objectCoords, realID, 0, 0, null );

                    absConsumable.originRoom = reader.ReadInt32();
                    absConsumable.placedObjectIndex = reader.ReadInt32();

                    absConsumable.minCycles = reader.ReadInt32();
                    absConsumable.maxCycles = reader.ReadInt32();

                    absConsumable.isConsumed = reader.ReadBoolean();
                    abstractObject = absConsumable;
                } else {
                    abstractObject = new AbstractPhysicalObject( patch_RainWorldGame.mainGame.world, objectType, null, objectCoords, realID );
                }

                return abstractObject;
            }

            public override void RealizeCreatedObject(AbstractPhysicalObject absPhys) {
                try {
                    absPhys.RealizeInRoom();

                    patch_PhysicalObject physObject = absPhys.realizedObject as patch_PhysicalObject;
                    physObject.SetGravity( 0 );
                    if( physObject is patch_Creature )
                        physObject.ChangeCollisionLayer( 4 );
                    else
                        physObject.ChangeCollisionLayer( 3 );
                } catch( System.Exception e ) {
                    Debug.LogError( e );
                    Debug.LogError( "Tried to realize object " + absPhys.type );
                }
            }

            public override void WriteObjectUpdate(AbstractPhysicalObject obj, BinaryWriter writer) {
                if( obj.realizedObject == null )
                    return;

                if( obj.realizedObject.bodyChunks != null ) {

                    BodyChunk[] chunks = obj.realizedObject.bodyChunks;
                    writer.Write( chunks.Length );
                    for( int i = 0; i < chunks.Length; i++ ) {
                        BodyChunk bc = chunks[i];
                        writer.Write( bc.pos.x );
                        writer.Write( bc.pos.y );

                        if( bc.rotationChunk != null ) {
                            writer.Write( bc.rotationChunk.pos.x );
                            writer.Write( bc.rotationChunk.pos.y );
                        }
                    }
                }

                GraphicsModule gm = obj.realizedObject.graphicsModule;

                if( gm != null ) {
                    SyncTree<GraphicsModule> moduleTree = MonklandSteamManager.ObjectManager.graphModuleSyncTree;

                    ObjectHandler<GraphicsModule> handler = moduleTree.GetObjectHandler( gm.GetType() );

                    handler.WriteObjectUpdate( gm, writer );
                }

            }
            public override void ReadObjectUpdate(AbstractPhysicalObject obj, BinaryReader reader) {
                if( obj.realizedObject == null )
                    return;

                if( obj.realizedObject.bodyChunks != null ) {

                    BodyChunk[] chunks = obj.realizedObject.bodyChunks;
                    int chunkCount = reader.ReadInt32();

                    for( int i = 0; i < chunkCount; i++ ) {
                        patch_BodyChunk bc = chunks[i] as patch_BodyChunk;
                        bc.targetPos = new Vector2( reader.ReadSingle(), reader.ReadSingle() );

                        if( obj.ID.spawner >= 0 )
                            Debug.Log( "Setting target of body chunk to " + bc.targetPos );

                        if( bc.rotationChunk != null ) {
                            ( bc.rotationChunk as patch_BodyChunk ).targetPos = new Vector2( reader.ReadSingle(), reader.ReadSingle() );
                        }
                    }
                }

                GraphicsModule gm = obj.realizedObject.graphicsModule;

                if( gm != null ) {
                    SyncTree<GraphicsModule> moduleTree = MonklandSteamManager.ObjectManager.graphModuleSyncTree;

                    ObjectHandler<GraphicsModule> handler = moduleTree.GetObjectHandler( gm.GetType() );

                    handler.ReadObjectUpdate( gm, reader );
                }
            }

            public override void WriteObjectDestruction(AbstractPhysicalObject obj, BinaryWriter writer) {

            }
            public override void ReadObjectDestruction(AbstractPhysicalObject obj, BinaryReader reader) {

            }

            public override void WriteObjectEvent(AbstractPhysicalObject obj, BinaryWriter writer, byte eventID, object[] extraData) {
                writeEvents[eventID]( obj, writer, extraData );
            }
            public override void ReadObjectEvent(AbstractPhysicalObject obj, BinaryReader reader, byte eventID) {
                readEvents[eventID]( obj, reader );
            }

            #region Event Handlers

            public override void RegisterEvents() {
                MoveEventID = RegisterEvent( MoveEventWrite, MoveEventRead );
                EnterDenID = RegisterEvent( EnterDenEventWrite, EnterDenEventRead );
                EnterShortcutID = RegisterEvent( EnterShortcutWrite, EnterShortcutRead );
                ExitShortcutID = RegisterEvent( ExitShortcutWrite, ExitShortcutRead );
            }

            #region Default Events
            public void MoveEventWrite(AbstractPhysicalObject obj, BinaryWriter writer, object[] extraData) {
                WriteWorldCoordToStream( obj.pos, writer );
            }
            public void MoveEventRead(AbstractPhysicalObject obj, BinaryReader reader) {
                obj.Move( ReadWorldCoordFromStream( reader ) );
            }

            public void EnterDenEventWrite(AbstractPhysicalObject obj, BinaryWriter writer, object[] extraData) {
                bool into = (bool)extraData[0];
                writer.Write( into );
                Debug.Log( string.Format( "Sending creature {0} {1} den", obj.ID.ToString(), into ? "entering" : "exiting" ) );
            }
            public void EnterDenEventRead(AbstractPhysicalObject obj, BinaryReader reader) {
                bool into = reader.ReadBoolean();
                Debug.Log( string.Format( "Creature {0} is {1} den", obj.ID.ToString(), into ? "entering" : "exiting" ) );
                if( into )
                    obj.Room.MoveEntityToDen( obj );
                else
                    obj.Room.MoveEntityOutOfDen( obj );
            }

            public void EnterShortcutWrite(AbstractPhysicalObject obj, BinaryWriter writer, object[] extraData) {
                if( obj is AbstractCreature ) {
                    Debug.Log( "Writing Creature Enter Shortcut" );

                    patch_Creature creature = obj.realizedObject as patch_Creature;
                    writer.Write( creature.isBeingCarriedByOtherCreature );

                    IntVector2 shortcutPos = creature.suckedIntoPosition;
                    writer.Write( shortcutPos.x );
                    writer.Write( shortcutPos.y );
                }
            }
            public void EnterShortcutRead(AbstractPhysicalObject obj, BinaryReader reader) {
                if( obj is AbstractCreature ) {
                    Debug.Log( "Reading Creature Enter Shortcut" );

                    patch_Creature creature = obj.realizedObject as patch_Creature;
                    bool carried = reader.ReadBoolean();
                    IntVector2 shortcutPos = new IntVector2( reader.ReadInt32(), reader.ReadInt32() );

                    Vector2 vector = creature.room.MiddleOfTile( shortcutPos ) + Custom.IntVector2ToVector2( creature.room.ShorcutEntranceHoleDirection( shortcutPos ) ) * -5f;
                    creature.SuckedIntoShortcutPublic( shortcutPos, carried );
                    if( creature.graphicsModule != null )
                        creature.graphicsModule.SuckedIntoShortCut( vector );
                }
            }

            public void ExitShortcutWrite(AbstractPhysicalObject obj, BinaryWriter writer, object[] extraData) {
                if( obj is AbstractCreature ) {
                    patch_Creature creature = obj.realizedObject as patch_Creature;

                    Debug.Log( "Writing Creature Exit Shortcut " + creature.spitIntoRoom.abstractRoom );

                    writer.Write( creature.spitOutAllSticksAtShortcut );

                    IntVector2 shortcutPos = creature.suckedIntoPosition;
                    writer.Write( shortcutPos.x );
                    writer.Write( shortcutPos.y );

                    writer.Write( creature.spitIntoRoom.abstractRoom.index );
                }
            }
            public void ExitShortcutRead(AbstractPhysicalObject obj, BinaryReader reader) {
                if( obj is AbstractCreature ) {
                    Debug.Log( "Reading Creature Exit Shortcut" );

                    patch_Creature creature = obj.realizedObject as patch_Creature;

                    bool spitSticks = reader.ReadBoolean();
                    IntVector2 shortcutPos = new IntVector2( reader.ReadInt32(), reader.ReadInt32() );
                    int roomIndex = reader.ReadInt32();

                    creature.SpitOutOfShortCut( shortcutPos, patch_RainWorldGame.mainGame.world.GetAbstractRoom( roomIndex ).realizedRoom, spitSticks );
                }
            }
            #endregion

            #endregion

        }

        //Grabbable object handler TODO Add stuff to this
        public class CarryableItemHandler: PhysicsObjectHandler {

        }
        //Weapon object handler
        public class WeaponObjectHandler: CarryableItemHandler {
            public override void WriteObjectCreation(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectCreation( obj, writer );
            }
            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {
                return base.ReadObjectCreation( id, reader );
            }

            public override void WriteObjectUpdate(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                if( obj.realizedObject != null ) {

                    Weapon weapon = obj.realizedObject as Weapon;

                    writer.Write( weapon.rotationSpeed );
                    writer.Write( weapon.rotation.x );
                    writer.Write( weapon.rotation.y );
                    writer.Write( (byte)weapon.mode );
                }
            }
            public override void ReadObjectUpdate(AbstractPhysicalObject obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );


                if( obj.realizedObject != null ) {
                    Weapon weapon = obj.realizedObject as Weapon;

                    weapon.rotationSpeed = reader.ReadSingle();
                    weapon.rotation = new Vector2( reader.ReadSingle(), reader.ReadSingle() );
                    Weapon.Mode weaponMode = (Weapon.Mode)reader.ReadByte();

                    if( weapon.mode != weaponMode )
                        weapon.ChangeMode( weapon.mode );
                }
            }


        }
        //Spear object handler
        public class SpearObjectHandler: WeaponObjectHandler {

            public override void WriteObjectCreation(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectCreation( obj, writer );
                AbstractSpear spear = obj as AbstractSpear;

                writer.Write( spear.explosive );
                writer.Write( spear.stuckInWallCycles );
                writer.Write( spear.stuckVertically );
            }
            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {
                EntityID realID = (EntityID)id;
                //Read object type
                AbstractPhysicalObject.AbstractObjectType objectType = (AbstractPhysicalObject.AbstractObjectType)reader.ReadByte();
                //Read object position
                WorldCoordinate objectCoords = ReadWorldCoordFromStream( reader );

                bool isExplosive = reader.ReadBoolean();
                AbstractSpear spear = new AbstractSpear( patch_RainWorldGame.mainGame.world, null, objectCoords, realID, isExplosive );

                spear.explosive = isExplosive;
                spear.stuckInWallCycles = reader.ReadInt32();
                spear.stuckVertically = reader.ReadBoolean();

                return spear;
            }

        }

        public class WaterNutObjectHandler: WeaponObjectHandler {

            public override void WriteObjectCreation(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectCreation( obj, writer );

                WaterNut.AbstractWaterNut awn = obj as WaterNut.AbstractWaterNut;

                writer.Write( awn.swollen );

                writer.Write( awn.originRoom );
                writer.Write( awn.placedObjectIndex );

                writer.Write( awn.minCycles );
                writer.Write( awn.maxCycles );

                writer.Write( awn.isConsumed );
            }
            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {
                EntityID realID = (EntityID)id;
                //Read object type
                AbstractPhysicalObject.AbstractObjectType objectType = (AbstractPhysicalObject.AbstractObjectType)reader.ReadByte();
                //Read object position
                WorldCoordinate objectCoords = ReadWorldCoordFromStream( reader );

                WaterNut.AbstractWaterNut absWaterNut = new WaterNut.AbstractWaterNut( patch_RainWorldGame.mainGame.world, null, objectCoords, realID, 0, 0, null, false );

                absWaterNut.swollen = reader.ReadBoolean();

                absWaterNut.originRoom = reader.ReadInt32();
                absWaterNut.placedObjectIndex = reader.ReadInt32();

                absWaterNut.minCycles = reader.ReadInt32();
                absWaterNut.maxCycles = reader.ReadInt32();

                absWaterNut.isConsumed = reader.ReadBoolean();

                return absWaterNut;
            }


        }

        public class CreatureObjectHandler: PhysicsObjectHandler {

            public static byte ViolenceEventID = 0;

            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {
                EntityID realID = (EntityID)id;
                //Read object type
                AbstractPhysicalObject.AbstractObjectType objectType = (AbstractPhysicalObject.AbstractObjectType)reader.ReadByte();
                //Read object position
                WorldCoordinate objectCoords = ReadWorldCoordFromStream( reader );

                CreatureTemplate.Type creatureType = (CreatureTemplate.Type)reader.ReadByte();

                AbstractCreature absCreature = new AbstractCreature( patch_RainWorldGame.mainGame.world, StaticWorld.GetCreatureTemplate( creatureType ), null, objectCoords, realID );

                if( reader.ReadBoolean() )
                    absCreature.spawnData = reader.ReadString();

                return absCreature;
            }
            public override void WriteObjectCreation(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectCreation( obj, writer );

                AbstractCreature creature = obj as AbstractCreature;

                writer.Write( (byte)creature.creatureTemplate.type );
                writer.Write( creature.spawnData != null );
                if( creature.spawnData != null )
                    writer.Write( creature.spawnData );
            }

            public override void WriteObjectUpdate(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                if( obj.realizedObject != null ) {
                    Creature creature = obj.realizedObject as Creature;
                    writer.Write( (ushort)creature.stun );
                    writer.Write( (ushort)creature.blind );
                }
            }
            public override void ReadObjectUpdate(AbstractPhysicalObject obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                if( obj.realizedObject != null ) {
                    Creature creature = obj.realizedObject as Creature;
                    creature.stun = reader.ReadUInt16();
                    creature.blind = reader.ReadUInt16();
                }

            }

            public override void RealizeCreatedObject(AbstractPhysicalObject absPhys) {
                base.RealizeCreatedObject( absPhys );
            }

            public override void RegisterEvents() {
                base.RegisterEvents();

                ViolenceEventID = RegisterEvent( ViolenceEventWrite, ViolenceEventRead );
            }

            public void ViolenceEventWrite(AbstractPhysicalObject obj, BinaryWriter writer, object[] extraData) {

                #region Extra Data
                int index = 0;
                BodyChunk source = extraData[index++] as BodyChunk;
                Vector2 direction = (Vector2)extraData[index++];
                BodyChunk hitChunk = extraData[index++] as BodyChunk;
                PhysicalObject.Appendage.Pos hitAppendage = extraData[index++] as PhysicalObject.Appendage.Pos;
                Creature.DamageType damageType = (Creature.DamageType)extraData[index++];
                float damage = (float)extraData[index++];
                float stunBonus = (float)extraData[index++];
                #endregion



            }
            public void ViolenceEventRead(AbstractPhysicalObject obj, BinaryReader reader) {

            }

        }

        public class PlayerObjectHandler: CreatureObjectHandler {
            public override AbstractPhysicalObject ReadObjectCreation(object id, BinaryReader reader) {
                EntityID realID = (EntityID)id;
                Debug.Log( string.Format( "Created player {0}", realID.ToString() ) );
                AbstractCreature originalCreature = base.ReadObjectCreation( realID, reader ) as AbstractCreature;

                originalCreature.state = new PlayerState( originalCreature, 1, reader.ReadByte(), false );
                patch_RainWorldGame.mainGame.session.AddPlayer( originalCreature );
                return originalCreature;
            }
            public override void WriteObjectCreation(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectCreation( obj, writer );

                AbstractCreature creature = obj as AbstractCreature;

                writer.Write( ( creature.state as PlayerState ).slugcatCharacter );
            }

            public override void WriteObjectUpdate(AbstractPhysicalObject obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                if( obj.realizedObject != null ) {
                    Player realizedPlayer = obj.realizedObject as Player;

                    writer.Write( (byte)realizedPlayer.bodyMode );
                    writer.Write( (byte)realizedPlayer.animation );
                    writer.Write( realizedPlayer.input[0].x );
                    writer.Write( realizedPlayer.input[0].y );
                    writer.Write( realizedPlayer.aerobicLevel );
                    writer.Write( realizedPlayer.animationFrame );
                }
            }
            public override void ReadObjectUpdate(AbstractPhysicalObject obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                if( obj.realizedObject != null ) {
                    Player realizedPlayer = obj.realizedObject as Player;

                    realizedPlayer.bodyMode = (Player.BodyModeIndex)reader.ReadByte();
                    realizedPlayer.animation = (Player.AnimationIndex)reader.ReadByte();
                    for( int i = 0; i < realizedPlayer.input.Length; i++ ) {
                        realizedPlayer.input[i].x = reader.ReadInt32();
                        realizedPlayer.input[i].y = reader.ReadInt32();
                    }
                    realizedPlayer.aerobicLevel = reader.ReadSingle();
                    ( realizedPlayer as Creature as PhysicalObject as patch_PhysicalObject as patch_Creature as patch_Player ).SetAnimFrame( reader.ReadInt32() );
                }
            }

            public override void RealizeCreatedObject(AbstractPhysicalObject absPhys) {
                patch_RainWorldGame.mainGame.world.GetAbstractRoom( absPhys.pos.room ).AddEntity( absPhys );
                base.RealizeCreatedObject( absPhys );
            }
        }

        #endregion

        #region Graphics Module Sync Tree

        public class GraphicsModuleHandler: ObjectHandler<GraphicsModule> {

            public override void WriteObjectUpdate(GraphicsModule obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                if( obj.bodyParts != null ) {

                    writer.Write( (ushort)obj.bodyParts.Length );

                    for( int i = 0; i < obj.bodyParts.Length; i++ ) {
                        BodyPart bp = obj.bodyParts[i];
                        ObjectHandler<BodyPart> handler = MonklandSteamManager.ObjectManager.bodyPartSyncTree.GetObjectHandler( bp.GetType() );

                        handler.WriteObjectUpdate( bp, writer );
                    }
                }

                writer.Write( obj.owner.room.game.evenUpdate );
            }
            public override void ReadObjectUpdate(GraphicsModule obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                if( obj.bodyParts != null ) {
                    int bpCount = reader.ReadUInt16();
                    for( int i = 0; i < bpCount && i < obj.bodyParts.Length; i++ ) {
                        BodyPart bp = obj.bodyParts[i];
                        ObjectHandler<BodyPart> handler = MonklandSteamManager.ObjectManager.bodyPartSyncTree.GetObjectHandler( bp.GetType() );

                        handler.ReadObjectUpdate( bp, reader );
                    }
                }

                reader.ReadBoolean();
            }

        }

        public class PlayerGraphicsHandler: GraphicsModuleHandler {
            public override void WriteObjectUpdate(GraphicsModule obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                patch_PlayerGraphics pg = obj as patch_PlayerGraphics;

                writer.Write( pg.legDirectionGet.x );
                writer.Write( pg.legDirectionGet.y );
            }
            public override void ReadObjectUpdate(GraphicsModule obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                patch_PlayerGraphics pg = obj as patch_PlayerGraphics;

                pg.legDirectionGet = new Vector2( reader.ReadSingle(), reader.ReadSingle() );
            }
        }

        #endregion

        #region Body Part Sync tree

        public class BodyPartHandler: ObjectHandler<BodyPart> {

            public override void WriteObjectUpdate(BodyPart obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                writer.Write( obj.pos.x );
                writer.Write( obj.pos.y );
            }
            public override void ReadObjectUpdate(BodyPart obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                patch_BodyPart patch = obj as patch_BodyPart;

                patch.targetPos = new Vector2( reader.ReadSingle(), reader.ReadSingle() );
            }


        }

        public class LimbHandler: BodyPartHandler {

            public override void WriteObjectUpdate(BodyPart obj, BinaryWriter writer) {
                base.WriteObjectUpdate( obj, writer );

                Limb objectLimb = obj as Limb;
                writer.Write( (byte)objectLimb.mode );
                writer.Write( objectLimb.reachedSnapPosition );
                writer.Write( objectLimb.absoluteHuntPos.x );
                writer.Write( objectLimb.absoluteHuntPos.y );
            }
            public override void ReadObjectUpdate(BodyPart obj, BinaryReader reader) {
                base.ReadObjectUpdate( obj, reader );

                Limb objectLimb = obj as Limb;
                objectLimb.mode = (Limb.Mode)reader.ReadByte();
                objectLimb.reachedSnapPosition = reader.ReadBoolean();

                objectLimb.absoluteHuntPos = new Vector2( reader.ReadSingle(), reader.ReadSingle() );
            }

        }

        #endregion

        #endregion

        #region Object Info

        public ulong GetOwnerID(AbstractPhysicalObject obj) {
            if( objectOwners.ContainsKey( obj.ID.ToString() ) )
                return objectOwners[obj.ID.ToString()];
            else
                return NetworkGameManager.playerID;
        }

        public bool IsNetObject(PhysicalObject obj) {
            return IsNetObject( obj.abstractPhysicalObject as patch_AbstractPhysicalObject );
        }
        public bool IsNetObject(patch_AbstractPhysicalObject obj) {
            if( MonklandSteamManager.isInGame == false )
                return false;
            return networkObjects.ContainsKey( GetObjectNetID( obj ) );
        }

        public bool OwnsObject(patch_PhysicalObject obj) {
            return OwnsObject( obj.abstractPhysicalObject as patch_AbstractPhysicalObject );
        }
        public bool OwnsObject(patch_AbstractPhysicalObject obj) {
            if( MonklandSteamManager.isInGame == false )
                return false;
            return obj.ownerID == NetworkGameManager.playerID;
        }

        public string GetObjectNetID(patch_AbstractPhysicalObject obj) {
            return obj.ID.ToString();
        }

        #endregion

        #region ReadWrite Utils

        private static EntityID ReadIDFromStream(BinaryReader reader) {
            return new EntityID( reader.ReadInt32(), reader.ReadInt32() );
        }
        private static void WriteIDToStream(PhysicalObject obj, BinaryWriter writer) {
            WriteIDToStream( obj.abstractPhysicalObject.ID, writer );
        }
        private static void WriteIDToStream(EntityID ID, BinaryWriter writer) {
            writer.Write( ID.spawner );
            writer.Write( ID.number );
        }

        private static WorldCoordinate ReadWorldCoordFromStream(BinaryReader reader) {
            WorldCoordinate newCoord = new WorldCoordinate();
            newCoord.room = reader.ReadInt32();
            newCoord.x = reader.ReadInt32();
            newCoord.y = reader.ReadInt32();
            newCoord.abstractNode = reader.ReadInt32();
            return newCoord;
        }
        private static void WriteWorldCoordToStream(WorldCoordinate coord, BinaryWriter writer) {
            writer.Write( coord.room );
            writer.Write( coord.x );
            writer.Write( coord.y );
            writer.Write( coord.abstractNode );
        }

        #endregion

    }
}