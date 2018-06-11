using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;
using RWCustom;
using System.IO;
using Monkland.SteamManagement;

namespace Monkland {
    class SteamMultiplayerMenu: Menu.Menu {

        public MultiplayerChat gameChat;
        public MultiplayerPlayerList playerList;

        public SimpleButton backButton;
        public SimpleButton startGameButton;
        public SimpleButton readyUpButton;

        private FSprite darkSprite;
        private FSprite blackFadeSprite;
        private float blackFade;
        private float lastBlackFade;

        public SteamMultiplayerMenu(ProcessManager manager, bool shouldCreateLobby = false) : base( manager, ProcessManager.ProcessID.MainMenu ) {

            if( shouldCreateLobby ) {
                MonklandSteamManager.instance.CreateLobby();
            }

            this.blackFade = 1f;
            this.lastBlackFade = 1f;
            this.pages.Add( new Page( this, null, "main", 0 ) );
            this.scene = new InteractiveMenuScene( this, this.pages[0], MenuScene.SceneID.Landscape_SU );
            this.pages[0].subObjects.Add( this.scene );
            this.darkSprite = new FSprite( "pixel", true );
            this.darkSprite.color = new Color( 0f, 0f, 0f );
            this.darkSprite.anchorX = 0f;
            this.darkSprite.anchorY = 0f;
            this.darkSprite.scaleX = 1368f;
            this.darkSprite.scaleY = 770f;
            this.darkSprite.x = -1f;
            this.darkSprite.y = -1f;
            this.darkSprite.alpha = 0.85f;
            this.pages[0].Container.AddChild( this.darkSprite );
            this.blackFadeSprite = new FSprite( "Futile_White", true );
            this.blackFadeSprite.scaleX = 87.5f;
            this.blackFadeSprite.scaleY = 50f;
            this.blackFadeSprite.x = manager.rainWorld.screenSize.x / 2f;
            this.blackFadeSprite.y = manager.rainWorld.screenSize.y / 2f;
            this.blackFadeSprite.color = new Color( 0f, 0f, 0f );
            Futile.stage.AddChild( this.blackFadeSprite );

            //Back button
            this.backButton = new SimpleButton( this, this.pages[0], base.Translate( "BACK" ), "EXIT", new Vector2( 200f, 50f ), new Vector2( 110f, 30f ) );
            this.pages[0].subObjects.Add( this.backButton );

            //Ready Up button
            this.startGameButton = new SimpleButton( this, this.pages[0], "Start Game", "STARTGAME", new Vector2( -100000, 50f ), new Vector2( 110f, 30f ) );
            this.pages[0].subObjects.Add( this.startGameButton );

            //Ready Up button
            this.readyUpButton = new SimpleButton( this, this.pages[0], "Ready UP", "READYUP", new Vector2( 1000, 50f ), new Vector2( 110f, 30f ) );
            this.pages[0].subObjects.Add( this.readyUpButton );

            //Multiplayer Chat
            this.gameChat = new MultiplayerChat( this, this.pages[0], new Vector2( 420, 125 ), new Vector2( 800, 600 ) );
            this.pages[0].subObjects.Add( this.gameChat );

            //Invite menu
            playerList = new MultiplayerPlayerList( this, this.pages[0], new Vector2( 200, 125 ), new Vector2( 200, 600 ), new Vector2( 180, 180 ) );
            this.pages[0].subObjects.Add( this.playerList );

        }

        protected override void Init() {
            base.Init();
        }

        public override void Update() {
            this.lastBlackFade = this.blackFade;
            float num = 0f;
            if( this.blackFade < num ) {
                this.blackFade = Custom.LerpAndTick( this.blackFade, num, 0.05f, 0.06666667f );
            } else {
                this.blackFade = Custom.LerpAndTick( this.blackFade, num, 0.05f, 0.125f );
            }

            if( NetworkGameManager.managerID == NetworkGameManager.playerID ) {
                startGameButton.pos = new Vector2( 700, 50f );
            }

            base.Update();
        }

        public override void GrafUpdate(float timeStacker) {
            base.GrafUpdate( timeStacker );
            this.blackFadeSprite.alpha = Mathf.Lerp( this.lastBlackFade, this.blackFade, timeStacker );
        }

        public override void ShutDownProcess() {
            base.ShutDownProcess();
            gameChat.ClearMessages();
            playerList.ClearList();
            this.darkSprite.RemoveFromContainer();
        }

        public override void Singal(MenuObject sender, string message) {
            if( message == "EXIT" ) {
                manager.RequestMainProcessSwitch( ProcessManager.ProcessID.MainMenu );
                MonklandSteamManager.instance.OnGameExit();
                Steamworks.SteamMatchmaking.LeaveLobby( SteamManagement.MonklandSteamManager.lobbyID );
            } else if( message == "READYUP" ) {
                MonklandSteamManager.GameManager.ToggleReady();
            } else if( message == "STARTGAME" ) {
                if( NetworkGameManager.isManager) {
                    MonklandSteamManager.GameManager.SendPlayersToGame();
                }
            }
        }
    }
}