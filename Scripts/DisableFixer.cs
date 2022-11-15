/*
Copyright 2022 CentauriCore

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DisableFixer : UdonSharpBehaviour
    {
        public Player[] Players;
        public Controller Controller;
        

        public void OnDisable()
        {
            foreach (var player in Players)
            {
                if (player.isOwner)
                {
                    if (player.BlackjackController.localGameActive)
                    {
                        player.EndTurn();
                    }
                    
                    player.ReleaseControl();
                }
            }

            if (Networking.IsOwner(Controller.gameObject))
            {
                Controller.ForceGlobalResetNoTimer();
            }
        }
    }
}
