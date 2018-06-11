using Monkland.Patches;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Monkland.UI;

namespace Monkland.SteamManagement {
    class NetworkRoomManager: NetworkManager {

        public int GAMEMANAGEMENT_CHANNEL = 1;

        public byte RoomHandler = 0;

        #region Message IDs
        public const byte RoomActivateRequestID = 0;
        public const byte RoomKillRequestID = 1;
        public const byte RoomActivateID = 2;
        public const byte RoomKillID = 3;

        #endregion

        #region Packet Handler

        public override void RegisterHandlers() {
            RoomHandler = MonklandSteamManager.instance.RegisterHandler( GAMEMANAGEMENT_CHANNEL, HandleRoomPackets );
        }

        public void HandleRoomPackets(BinaryReader br, CSteamID sentPlayer) {
            byte messageType = br.ReadByte();

            switch( messageType ) {
                case RoomActivateRequestID:
                RoomActivationMessage( br, sentPlayer, true );
                return;
                case RoomKillRequestID:
                RoomKillMessage( br, sentPlayer, true );
                return;
                case RoomActivateID:
                RoomActivationMessage( br, sentPlayer, false );
                return;
                case RoomKillID:
                RoomKillMessage( br, sentPlayer, false );
                return;
            }
        }

        #endregion

        #region Outgoing Messages

        public HashSet<int> requestedActivateRooms = new HashSet<int>();
        public HashSet<int> requestedKillRooms = new HashSet<int>();

        public HashSet<int> activatedRooms = new HashSet<int>();
        public HashSet<int> killedRooms = new HashSet<int>();

        public void SendRoomActivationRequest(AbstractRoom absRoom) {
            if( requestedActivateRooms.Contains( absRoom.index ) )
                return;
            MonklandSteamManager.DataPacket dataPacket = MonklandSteamManager.instance.GetNewPacket( GAMEMANAGEMENT_CHANNEL, RoomHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( dataPacket );

            //Writes the message type
            writer.Write( RoomActivateRequestID );

            //Send room ID that we want to activate
            writer.Write( absRoom.index );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, dataPacket );
            MonklandSteamManager.instance.SendPacket( dataPacket, (CSteamID)NetworkGameManager.managerID, EP2PSend.k_EP2PSendReliable );

            requestedActivateRooms.Add( absRoom.index );
            MonklandUI.AddMessage( "Requested realization of room " + absRoom.index );
            Debug.Log( "Requested realization of room " + absRoom.index );
        }
        public void SendRoomKillRequest(AbstractRoom absRoom) {
            if( requestedKillRooms.Contains( absRoom.index ) )
                return;
            MonklandSteamManager.DataPacket dataPacket = MonklandSteamManager.instance.GetNewPacket( GAMEMANAGEMENT_CHANNEL, RoomHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( dataPacket );

            //Writes the message type
            writer.Write( RoomKillRequestID );

            //Send room ID that we want to activate
            writer.Write( absRoom.index );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, dataPacket );
            MonklandSteamManager.instance.SendPacket( dataPacket, (CSteamID)NetworkGameManager.managerID, EP2PSend.k_EP2PSendReliable );

            requestedKillRooms.Add( absRoom.index );
            MonklandUI.AddMessage( "Requested realization of room " + absRoom.index );
            Debug.Log( "Requested killing of room " + absRoom.index );
        }

        public void SendRoomActivation(int roomID, ulong ownerID) {

            if( activatedRooms.Contains( roomID ) )
                return;

            if( killedRooms.Contains( roomID ) )
                killedRooms.Remove( roomID );

            MonklandSteamManager.DataPacket dataPacket = MonklandSteamManager.instance.GetNewPacket( GAMEMANAGEMENT_CHANNEL, RoomHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( dataPacket );

            //Writes the message type
            writer.Write( RoomActivateID );

            //Send room ID that we want to activate
            writer.Write( roomID );
            writer.Write( ownerID );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, dataPacket );
            MonklandSteamManager.instance.SendPacketToAll( dataPacket, false, EP2PSend.k_EP2PSendReliable );

            requestedActivateRooms.Add( roomID );
            MonklandUI.AddMessage( "Sending realization of room " + roomID );
            Debug.Log( "Sending realization of room " + roomID );
            activatedRooms.Add( roomID );
        }
        public void SendRoomKill(int roomID, ulong ownerID) {

            if( killedRooms.Contains( roomID ) )
                return;

            if( activatedRooms.Contains( roomID ) )
                activatedRooms.Remove( roomID );

            MonklandSteamManager.DataPacket dataPacket = MonklandSteamManager.instance.GetNewPacket( GAMEMANAGEMENT_CHANNEL, RoomHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( dataPacket );

            //Writes the message type
            writer.Write( RoomKillID );

            //Send room ID that we want to activate
            writer.Write( roomID );
            writer.Write( ownerID );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, dataPacket );
            MonklandSteamManager.instance.SendPacketToAll( dataPacket, false, EP2PSend.k_EP2PSendReliable );

            requestedActivateRooms.Add( roomID );
            MonklandUI.AddMessage( "Sending killing of room " + roomID );
            Debug.Log( "Sending killing of room " + roomID );
            killedRooms.Add(roomID);
        }

        #endregion

        #region Incoming Messages

        public void RoomActivationMessage(BinaryReader reader, CSteamID sentPlayer, bool isRequest) {
            if( isRequest ) {
                if( NetworkGameManager.isManager ) {
                    int roomID = reader.ReadInt32();

                    if( !activationRequests.ContainsKey( roomID ) )
                        activationRequests[roomID] = new List<ulong>();

                    MonklandUI.AddMessage( "Recieved Request to activate room " + roomID );
                    activationRequests[roomID].Add( sentPlayer.m_SteamID );
                }
            } else {
                int roomId = reader.ReadInt32();
                ulong ownerID = reader.ReadUInt64();

                if( requestedActivateRooms.Contains( roomId ) )
                    requestedActivateRooms.Remove( roomId );

                patch_World world = patch_RainWorldGame.mainGame.world as patch_World;
                patch_AbstractRoom room = world.GetAbstractRoom( roomId ) as patch_AbstractRoom;

                room.ownerID = ownerID;

                world.RemoteActivate( room );

                MonklandUI.AddMessage("Realized room " + roomId);
            }
        }

        public void RoomKillMessage(BinaryReader reader, CSteamID sentPlayer, bool isRequest) {
            if( isRequest ) {
                if( NetworkGameManager.isManager ) {
                    int roomID = reader.ReadInt32();

                    if( !killRequests.ContainsKey( roomID ) )
                        killRequests[roomID] = new List<ulong>();

                    MonklandUI.AddMessage( "Recieved Request to kill room " + roomID );

                    killRequests[roomID].Add( sentPlayer.m_SteamID );
                }
            } else {
                int roomId = reader.ReadInt32();
                ulong ownerID = reader.ReadUInt64();

                if( requestedActivateRooms.Contains( roomId ) )
                    requestedActivateRooms.Remove( roomId );

                patch_World world = patch_RainWorldGame.mainGame.world as patch_World;
                patch_RoomRealizer realizer = patch_RainWorldGame.mainGame.roomRealizer as patch_RoomRealizer;
                patch_AbstractRoom room = world.GetAbstractRoom( roomId ) as patch_AbstractRoom;

                room.ownerID = ownerID;

                realizer.RemoteKill( room );
                MonklandUI.AddMessage( "Killed room " + roomId );
            }
        }

        #endregion

        #region Manager Logistics

        //All the rooms that are realized
        public Dictionary<int, patch_AbstractRoom> networkRealizedRooms = new Dictionary<int, patch_AbstractRoom>();
        //All the rooms that this player owns
        public Dictionary<int, patch_AbstractRoom> ownedRooms = new Dictionary<int, patch_AbstractRoom>();
        //Just a dictionary of all loaded rooms, and who owns them
        public Dictionary<int, ulong> roomOwners = new Dictionary<int, ulong>();

        //Requests from players to activate a room
        public Dictionary<int, List<ulong>> activationRequests = new Dictionary<int, List<ulong>>();
        //Requests from players to kill rooms
        public Dictionary<int, List<ulong>> killRequests = new Dictionary<int, List<ulong>>();

        public override void Update() {
            base.Update();
            ManageRoomRequests();
        }

        public void ManageRoomRequests() {

            //Handle requests to activate rooms
            foreach( KeyValuePair<int, List<ulong>> request in activationRequests ) {
                int roomID = request.Key;
                List<ulong> players = request.Value;

                //If the room we're checking for is already realized, skip
                if( networkRealizedRooms.ContainsKey( roomID ) )
                    continue;

                //If there are no players requesting this room, then just leave it.
                if( players.Count == 0 )
                    continue;

                //If the room hasn't been realized, realize it with the oldest player who requested it.
                ulong firstRequested = players[0];

                SendRoomActivation( roomID, firstRequested );
            }

            //Handle requests to kill rooms
            foreach( KeyValuePair<int, List<ulong>> request in killRequests ) {
                int roomID = request.Key;
                List<ulong> players = request.Value;

                //If the room has already been killed
                if( !networkRealizedRooms.ContainsKey( roomID ) )
                    continue;

                //If there's no players requesting to kill this room, skip it
                if( players.Count == 0 )
                    continue;

                //If there's a realization request for this room that has more than 0 players, someone still wants this room, so skip
                if( activationRequests.ContainsKey( roomID ) && activationRequests[roomID].Count > 0 )
                    continue;

                ulong firstKillRequester = players[0];

                SendRoomKill( roomID, firstKillRequester );
            }
        }

        #endregion

    }
}
