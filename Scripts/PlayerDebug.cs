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
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerDebug : UdonSharpBehaviour
    {
        public Player PlayerController;
        [Space] 
        public TextMeshProUGUI PlayerName;
        public TextMeshProUGUI IOwnThis;
        public TextMeshProUGUI ThisIsMine;
        public TextMeshProUGUI HasStood;
        [Space]
        public TextMeshProUGUI Split;
        public TextMeshProUGUI Stood;
        public TextMeshProUGUI NormalBust;
        public TextMeshProUGUI SplitBust;
        public TextMeshProUGUI Active;

        public void Update()
        {
            PlayerName.text = Networking.GetOwner(PlayerController.gameObject).displayName;
            IOwnThis.text = Networking.IsOwner(PlayerController.gameObject).ToString();
            ThisIsMine.text = PlayerController.ImInThis.ToString();
            HasStood.text = PlayerController.HasFinishedTurn.ToString();

            Split.text = PlayerController.PlayerUI.DealerUIAnimator.GetBool("Split").ToString();
            Stood.text = PlayerController.PlayerUI.DealerUIAnimator.GetBool("Stood").ToString();
            NormalBust.text = PlayerController.PlayerUI.DealerUIAnimator.GetBool("NormalBust").ToString();
            SplitBust.text = PlayerController.PlayerUI.DealerUIAnimator.GetBool("SplitBust").ToString();
            Active.text = PlayerController.PlayerUI.DealerUIAnimator.GetBool("Active").ToString();
        }
    }
}
