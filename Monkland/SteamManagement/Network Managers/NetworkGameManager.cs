using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Monkland.Patches;
using UnityEngine;

namespace Monkland.SteamManagement {
    class NetworkGameManager: NetworkManager {

        public static ulong playerID;
        public static int PlayerIndex;
        public static ulong managerID;

        public static bool isManager { get { return playerID == managerID; } }

        public int DEFAULT_CHANNEL = 0;

        public byte UtilityHandler = 0;

        public HashSet<ulong> readiedPlayers = new HashSet<ulong>();
        private bool isReady = false;

        //All the rooms that are loaded, and which players want those rooms loaded
        public Dictionary<int, List<ulong>> realizedRoomPlayers = new Dictionary<int, List<ulong>>();
        //All the abstract rooms, and which players want them abstract
        public Dictionary<int, List<ulong>> killRoomPlayers = new Dictionary<int, List<ulong>>();

        //Each realized room has a host
        public Dictionary<int, ulong> realizedRoomHosts = new Dictionary<int, ulong>();
        //Each abstract room has a host
        public Dictionary<int, ulong> abstractRoomHosts = new Dictionary<int, ulong>();

        //This just stores which room are and aren't realized.
        public Dictionary<int, bool> isRealized = new Dictionary<int, bool>();

        public HashSet<int> activeRealizationRequests = new HashSet<int>();
        public HashSet<int> activeKillRequests = new HashSet<int>();

        public override void Reset() {
            playerID = SteamUser.GetSteamID().m_SteamID;
            managerID = 0;


            //Readied players
            readiedPlayers.Clear();
            isReady = false;

            //Room management
            realizedRoomPlayers.Clear();
            killRoomPlayers.Clear();
            realizedRoomHosts.Clear();
            abstractRoomHosts.Clear();
            isRealized.Clear();
            activeKillRequests.Clear();
            activeRealizationRequests.Clear();
        }

        #region Packet Handler

        public override void RegisterHandlers() {
            UtilityHandler = MonklandSteamManager.instance.RegisterHandler( DEFAULT_CHANNEL, HandleUtilPackets );
        }

        public void HandleUtilPackets(BinaryReader br, CSteamID sentPlayer) {
            byte messageType = br.ReadByte();

            switch( messageType ) {
                case 0:
                ReadReadyUpPacket( br, sentPlayer );
                return;
                case 1:
                patch_Rainworld.mainRW.processManager.RequestMainProcessSwitch( ProcessManager.ProcessID.Game );
                return;
            }
        }

        #endregion

        #region Outgoing Packets

        public void ToggleReady() {
            isReady = !isReady;

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( DEFAULT_CHANNEL, UtilityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            //Write message type
            writer.Write( (byte)0 );

            //Write if the player is ready
            writer.Write( isReady );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacketToAll( packet, false, EP2PSend.k_EP2PSendReliable );
        }

        #endregion

        #region Incoming Packets

        public void ReadReadyUpPacket(BinaryReader reader, CSteamID sent) {
            bool isReady = reader.ReadBoolean();
            if( isReady ) {
                if( !readiedPlayers.Contains( sent.m_SteamID ) )
                    readiedPlayers.Add( sent.m_SteamID );
            } else {
                if( readiedPlayers.Contains( sent.m_SteamID ) )
                    readiedPlayers.Remove( sent.m_SteamID );
            }
        }

        #endregion

        public void SendPlayersToGame() {
            if( readiedPlayers.Count != MonklandSteamManager.connectedPlayers.Count )
                return;

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket( DEFAULT_CHANNEL, UtilityHandler );
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket( packet );

            //Write message type
            writer.Write( (byte)1 );

            MonklandSteamManager.instance.FinalizeWriterToPacket( writer, packet );
            MonklandSteamManager.instance.SendPacketToAll( packet, false, EP2PSend.k_EP2PSendReliable );
        }

    }
}