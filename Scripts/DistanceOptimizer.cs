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
    public class DistanceOptimizer : UdonSharpBehaviour
    {
        public Controller GameController;
        public AudioController AudioController;
        public Player[] Players;
        public DealerUIController DealerUI;
        [Space]
        public GameObject[] TurnOff;


        public void Start()
        {
            SendCustomEventDelayedFrames(nameof(DelayedStart), 20);
        }

        public void DelayedStart() // Has delayed start to loosen up start load time a lil bit and make sure stuff is initialized
        {
            if (GetComponent<Collider>().ClosestPoint(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) == Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position)
            {
                foreach (var item in TurnOff)
                {
                    item.SetActive(true);
                }

                AudioController.isActive = true;
                
                foreach (var player in Players)
                {
                    player.isRunningProperty = true;
                }

                DealerUI.Running = true;
            }
            else
            {
                foreach (var item in TurnOff)
                {
                    item.SetActive(false);
                }

                AudioController.isActive = false;

                foreach (var player in Players)
                {
                    player.isRunningProperty = false;
                }
                
                DealerUI.Running = false;
            }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;

            foreach (var item in TurnOff)
            {
                item.SetActive(true);
            }

            AudioController.isActive = true;
            
            foreach (var playerScript in Players)
            {
                playerScript.isRunningProperty = true;
            }
            
            DealerUI.Running = true;
        }
        
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;

            foreach (var item in TurnOff)
            {
                item.SetActive(false);
            }

            foreach (var playerScript in GameController.Players)
            {
                if (playerScript.isOwner)
                {
                    playerScript.ReleaseControl();
                }
            }

            AudioController.isActive = false;
            
            foreach (var playerScript in Players)
            {
                playerScript.isRunningProperty = false;
            }
            
            DealerUI.Running = false;
        }
    }
}
