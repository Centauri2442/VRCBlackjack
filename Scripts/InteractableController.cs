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
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(Collider))]
    public class InteractableController : UdonSharpBehaviour
    {
        public Controller Controller;
        public Player Player;
        public Interactable JoinGame;
        public Interactable Stand;
        public Interactable Split;
        public Interactable HitSplit;
        public Interactable HitNormal;
        public Interactable LeaveGame;
        public Interactable Add10;
        public Interactable Add5;
        public Interactable Remove10;
        public Interactable Remove5;
        private bool hasInitialized = false;
        private bool isActive = false;
        

        public void Start()
        {
            SendCustomEventDelayedFrames(nameof(DelayedStart), 20);
        }

        public void DelayedStart()
        {
            Controller = Player.BlackjackController;
            hasInitialized = true;
        }

        public void Update()
        {
            if (!hasInitialized) return;

            if (!isActive)
            {
                JoinGame.Active = false;
                Stand.Active = false;
                Split.Active = false;
                HitSplit.Active = false;
                HitNormal.Active = false;
                HitSplit.Active = false;
                LeaveGame.Active = false;
                Add10.Active = false;
                Add5.Active = false;
                Remove10.Active = false;
                Remove5.Active = false;
                
                SetActiveState(false);
            }
            else
            {
                SetActiveState(true);
            }
            
            var isOwner = Player.isOwner;
            var gameActive = Controller.localGameActive;
            var usingChips = Controller.UsingChips;

            JoinGame.Active = !isOwner && !gameActive;
            Stand.Active = isOwner && gameActive && Player.myTurn;
            Split.Active = isOwner && gameActive && Player.myTurn && Player.CanSplit;
            HitSplit.Active = isOwner && gameActive && Player.myTurn && Player.HasSplit;
            HitNormal.Active = isOwner && gameActive && Player.myTurn;
            LeaveGame.Active = isOwner;
            Add10.Active = isOwner && !gameActive && usingChips;
            Add5.Active = isOwner && !gameActive && usingChips;
            Remove10.Active = isOwner && !gameActive && usingChips;
            Remove5.Active = isOwner && !gameActive && usingChips;

        }

        public void SetActiveState(bool state)
        {
            JoinGame.gameObject.SetActive(state);
            Stand.gameObject.SetActive(state);
            Split.gameObject.SetActive(state);
            HitSplit.gameObject.SetActive(state);
            HitNormal.gameObject.SetActive(state);
            LeaveGame.gameObject.SetActive(state);
            JoinGame.gameObject.SetActive(state);
            Add10.gameObject.SetActive(state);
            Add5.gameObject.SetActive(state);
            Remove10.gameObject.SetActive(state);
            Remove5.gameObject.SetActive(state);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer != player) return;

            isActive = true;
        }
        
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer != player) return;

            isActive = false;
        }
    }
}
