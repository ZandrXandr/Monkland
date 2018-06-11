using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Monkland.SteamManagement {
    class SyncTree<T> {

        public Type defaultType;

        public Dictionary<string, byte> handlerIDs = new Dictionary<string, byte>();
        public Dictionary<string, Type> handlerShortcuts = new Dictionary<string, Type>();
        public List<ObjectHandler<T>> handlers = new List<ObjectHandler<T>>();

        public SyncTree() {
            defaultType = typeof( T );
        }

        public byte RegisterObjectHandler(ObjectHandler<T> toRegister, Type typeToHandle) {
            if( handlers.Count == 256 ) {
                throw new Exception( "Trying to register more than 256 object handlers. This means we need to increase things to a ushort instead of a byte" );
            }
            byte NextID = (byte)handlers.Count;

            toRegister.objectTypeHandlerID = NextID;
            Debug.Log( string.Format( "Registering type {0} with an ID of {1}", typeToHandle.Name, NextID ) );
            handlerIDs.Add( typeToHandle.Name, NextID );
            handlers.Add( toRegister );

            return NextID;
        }
        public ObjectHandler<T> GetObjectHandler(Type t) {
            //If there's no handler for this class, use the lowest subclass that has a handler, from shortcuts.
            if( !handlerIDs.ContainsKey( t.Name ) )
                return handlers[GetHandlerFromShortcut( t )];
            return handlers[handlerIDs[t.Name]];
        }
        public ObjectHandler<T> GetObjectHandler(byte id) {
            return handlers[id];
        }

        private byte GetHandlerFromShortcut(Type t) {
            //If there's no shortcut, generate one
            if( !handlerShortcuts.ContainsKey( t.Name ) )
                GenerateShortcut( t );
            try {
                //Return the handler ID of the shortcut's result
                return handlerIDs[handlerShortcuts[t.Name].Name];
            } catch( System.Exception e ) {
                Debug.LogError( e );
                Debug.Log( string.Format( "Tried to get shortcut for object {0}, the generated shortcut is {1}", t.Name, handlerShortcuts[t.Name].Name ) );
                return 0;
            }
        }
        private void GenerateShortcut(Type t) {

            //Get the type this class.
            Type baseType = t.BaseType;

            //As long as this class has a base type
            while( baseType.Name != defaultType.Name ) {
                //Check if this base type has a handler
                if( handlerIDs.ContainsKey( baseType.Name ) )
                    //If this type has a handler, break out of the loop
                    break;
                //If the base type has a shortcut, then we should just use that shortcut.
                if( handlerShortcuts.ContainsKey( baseType.Name ) ) {
                    baseType = handlerShortcuts[baseType.Name];
                    break;
                }
                baseType = baseType.BaseType;
            }

            Debug.Log( string.Format( "Generating shortcut for type {0}, to type {1}", t.Name, baseType.Name ) );
            handlerShortcuts[t.Name] = baseType;

        }

    }

    public class ObjectHandler<T> {
        public byte objectTypeHandlerID;

        public virtual void WriteObjectCreation(T absObject, BinaryWriter writer) {

        }
        public virtual T ReadObjectCreation(object id, BinaryReader reader) {
            return default( T );
        }

        public virtual void RealizeCreatedObject(T absPhys) {

        }

        public virtual void WriteObjectUpdate(T obj, BinaryWriter writer) {

        }
        public virtual void ReadObjectUpdate(T obj, BinaryReader reader) {

        }

        public virtual void WriteObjectDestruction(T obj, BinaryWriter writer) {

        }
        public virtual void ReadObjectDestruction(T obj, BinaryReader reader) {

        }

        public virtual void WriteObjectEvent(T obj, BinaryWriter writer, byte eventID, object[] extraData) {
            writeEvents[eventID]( obj, writer, extraData );
        }
        public virtual void ReadObjectEvent(T obj, BinaryReader reader, byte eventID) {
            readEvents[eventID]( obj, reader );
        }

        #region Event Handlers

        public delegate void EventWriteFunction(T obj, BinaryWriter writer, object[] extraData);
        public delegate void EventReadFunction(T obj, BinaryReader reader);

        public List<EventWriteFunction> writeEvents = new List<EventWriteFunction>();
        public List<EventReadFunction> readEvents = new List<EventReadFunction>();

        public byte RegisterEvent(EventWriteFunction writeFunc, EventReadFunction readFunc) {
            writeEvents.Add( writeFunc );
            readEvents.Add( readFunc );
            return (byte)( writeEvents.Count - 1 );
        }

        public virtual void RegisterEvents() {
        }
        #endregion
    }
}
