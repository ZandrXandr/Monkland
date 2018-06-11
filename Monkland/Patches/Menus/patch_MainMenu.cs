using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;

namespace Monkland.Patches {
    [MonoMod.MonoModPatch( "global::Menu.MainMenu" )]
    class patch_MainMenu: MainMenu {
        [MonoMod.MonoModConstructor]
        public patch_MainMenu(ProcessManager manager, bool showRegionSpecificBkg) : base( manager, showRegionSpecificBkg ) {
            float num3 = ( base.CurrLang != InGameTranslator.LanguageID.Italian ) ? 110f : 150f;
            this.pages[0].subObjects.Add( new SimpleButton( this, this.pages[0], "Co-op", "COOP", new Vector2( 683f - num3 / 2f, 170f ), new Vector2( num3, 30f ) ) );
        }

        public extern void orig_Singal(MenuObject sender, string message);
        public void Singal(MenuObject sender, string message) {

            if( message == "COOP" )
                ( (patch_ProcessManager)this.manager ).ImmediateSwitchCustom( new SteamMultiplayerMenu( this.manager , true) );
            else
                orig_Singal( sender, message );
        }

    }
}
