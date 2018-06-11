﻿using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkland;
using Monkland.Patches;
using Monkland.UI;

namespace Monkland.SteamManagement {
    class NetworkManager {

        public virtual void Reset() { }
        public virtual void Update() { }
        public virtual void RegisterHandlers() {

        }

    }
}