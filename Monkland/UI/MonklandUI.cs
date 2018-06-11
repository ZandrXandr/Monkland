using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Monkland.Patches;
using Monkland.SteamManagement;

namespace Monkland.UI {
    class MonklandUI {

        public static FStage worldStage;
        public static FContainer uiContainer;

        public static bool shouldDisplayOwners = false;
        public static bool shouldDisplayBoxes = false;
        public static bool shouldDisplayMessages = false;

        private static FLabel statusLabel;

        private static List<QuickDisplayMessage> displayMessages = new List<QuickDisplayMessage>();
        private static List<QuickDisplayBox> displayBoxes = new List<QuickDisplayBox>();
        private static List<FLabel> uiLabels = new List<FLabel>();

        public static Room currentRoom;
        public static AbstractCreature trackedPlayer;

        public MonklandUI(FStage stage) {
            worldStage = stage;

            uiContainer = new FContainer();

            statusLabel = new FLabel( "font", "STATUSLABEL" );
            statusLabel.alignment = FLabelAlignment.Left;
            statusLabel.SetPosition( 50, Futile.screen.height - 50 );
            uiContainer.AddChild( statusLabel );

            for( int i = 0; i < 200; i++ ) {
                FLabel displayLabel = new FLabel( "font", string.Empty );
                uiContainer.AddChild( displayLabel );
                uiLabels.Add( displayLabel );
            }

            stage.AddChild( uiContainer );
        }

        public void Update(RainWorldGame game) {

            if( Input.GetKeyDown( KeyCode.Alpha1 ) )
                shouldDisplayOwners = !shouldDisplayOwners;
            if( Input.GetKeyDown( KeyCode.Alpha2 ) )
                shouldDisplayBoxes = !shouldDisplayBoxes;
            if( Input.GetKeyDown( KeyCode.Alpha3 ) )
                shouldDisplayMessages = !shouldDisplayMessages;

            FindPlayer( game );

            DisplayQuickMessages();
            DisplayObjectOwners();
            DisplayQuickBoxes();

        }

        private void FindPlayer(RainWorldGame game) {
            if( game.Players.Count > 0 ) {
                trackedPlayer = game.Players[0];
                if( trackedPlayer != null ) {
                    currentRoom = trackedPlayer.Room.realizedRoom;
                }
            }
        }

        private void DisplayQuickMessages() {

            if( !shouldDisplayMessages ) {



                return;
            }

            //List of messages to remove this frame
            List<QuickDisplayMessage> toRemove = new List<QuickDisplayMessage>();

            //Loop through all quick display messages
            foreach( QuickDisplayMessage msg in displayMessages ) {
                msg.life -= Time.deltaTime;


            }

        }
        private void DisplayObjectOwners() {

            if( !shouldDisplayMessages ) {

            }

        }
        private void DisplayQuickBoxes() {

            if( !shouldDisplayBoxes ) {
                foreach( QuickDisplayBox box in displayBoxes ) {
                    box.displaySprite.SetPosition( -Vector2.one * 9999 );
                }
                return;
            }

            if( trackedPlayer == null || currentRoom == null )
                return;
            statusLabel.text = string.Empty;

            foreach( QuickDisplayBox box in displayBoxes ) {
                if( box.roomID != currentRoom.abstractRoom.index ) {
                    box.displaySprite.SetPosition( -Vector2.one * 9999 );
                    continue;
                }

                box.displaySprite.SetPosition( box.area.position - currentRoom.game.cameras[0].pos );
                box.displaySprite.scaleX = box.area.width;
                box.displaySprite.scaleY = box.area.height;
                box.displaySprite.color = box.color;
            }

        }

        public static void AddMessage(string messsage, float time = 3) {
            AddMessage( messsage, time, false, Vector2.zero, Color.white );
        }
        public static void AddMessage(string messsage, float time, bool isWorld, Vector2 worldPos) {
            AddMessage( messsage, time, isWorld, worldPos, Color.white );
        }
        public static void AddMessage(string messsage, float time, bool isWorld, Vector2 worldPos, Color color) {
            QuickDisplayMessage msg = new QuickDisplayMessage() {
                text = messsage,
                life = time,
                isWorld = isWorld,
                worldPos = worldPos,
                color = color,
                roomID = ( trackedPlayer == null ? 0 : trackedPlayer.Room.index )
            };

            displayMessages.Add( msg );
        }

        public static void AddDisplayBox(QuickDisplayBox box) {
            displayBoxes.Add( box );
            uiContainer.AddChild( box.displaySprite );
        }
        public static void RemoveDisplayBox(QuickDisplayBox box) {
            displayBoxes.Remove( box );
            uiContainer.RemoveChild( box.displaySprite );
        }

        public void ClearSprites() {
            displayBoxes.Clear();
            displayMessages.Clear();
            uiContainer.RemoveAllChildren();
            uiContainer.RemoveFromContainer();
            uiContainer = null;
        }

        public class QuickDisplayMessage {
            public string text;
            public float life;
            public Color color;
            public bool isWorld = false;
            public Vector2 worldPos;
            public int roomID;
        }
        public class QuickDisplayBox {
            public Rect area = new Rect( 0, 0, 30, 30 );
            public bool screenSpace = false;
            public Color color = new Color( 1, 1, 1, 0.2f );
            public int roomID;
            public FSprite displaySprite = new FSprite( "pixel", true );
        }
    }
}