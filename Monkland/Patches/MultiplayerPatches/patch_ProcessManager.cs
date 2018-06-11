using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod;

namespace Monkland.Patches {
    [MonoModPatch( "global::ProcessManager" )]
    class patch_ProcessManager: ProcessManager {
        [MonoModIgnore]
        public patch_ProcessManager(RainWorld rainWorld) : base( rainWorld ) {
        }

        [MonoModIgnore]
        private FLabel loadingLabel;
        [MonoModIgnore]
        private float blackDelay;
        [MonoModIgnore]
        private float blackFadeTime = 0.45f;
        [MonoModIgnore]
        private bool pauseFadeUpdate;

        public void ImmediateSwitchCustom(MainLoopProcess newProcess) {
            MainLoopProcess mainLoopProcess = this.currentMainLoop;
            if( this.currentMainLoop != null ) {
                this.currentMainLoop.ShutDownProcess();
                this.currentMainLoop.processActive = false;
                this.currentMainLoop = null;
                this.soundLoader.ReleaseAllUnityAudio();
                HeavyTexturesCache.ClearRegisteredFutileAtlases();
                GC.Collect();
                Resources.UnloadUnusedAssets();
            }
                this.rainWorld.progression.Revert();
            this.currentMainLoop = newProcess;
            if( mainLoopProcess != null ) {
                mainLoopProcess.CommunicateWithUpcomingProcess( this.currentMainLoop );
            }
            this.blackFadeTime = this.currentMainLoop.FadeInTime;
            this.blackDelay = this.currentMainLoop.InitialBlackSeconds;
            if( this.fadeSprite != null ) {
                this.fadeSprite.RemoveFromContainer();
                Futile.stage.AddChild( this.fadeSprite );
            }
            if( this.loadingLabel != null ) {
                this.loadingLabel.RemoveFromContainer();
                Futile.stage.AddChild( this.loadingLabel );
            }
            if( this.musicPlayer != null ) {
                this.musicPlayer.UpdateMusicContext( this.currentMainLoop );
            }
            this.pauseFadeUpdate = true;
        }

    }
}
