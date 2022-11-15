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
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(Collider))]
    public class Interactable : UdonSharpBehaviour
    {
        public bool Active;
        public string Event;
        public UdonSharpBehaviour TargetBehaviour;
        [Space] 
        public string PressName;
        public Animator PressViz;

        public Transform Fingie;
        public Transform ClosestPoint;

        private VRCPlayerApi _localPlayer;
        private Collider col;
        private bool isPressingLeft;
        private bool isPressingRight;
        private bool hasPressViz;


        public void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            col = GetComponent<Collider>();
            hasPressViz = PressViz != null;
            Active = false;
        }
        
        public override void PostLateUpdate()
        {
            if (!gameObject.activeSelf || !gameObject.activeInHierarchy) return; // *SCREAMING IN PAIN CAUSE OH GOD OH FUCK WHAT IS THIS SHIT FFS WHY DO I HAVE TO DO THIS PLEASE SAVE ME WHYYYYYY*
            
            CheckButton();
        }

        public void CheckButton()
        {
            if (!Active || !_localPlayer.IsUserInVR()) return;

            var leftHandPos = Vector3.zero;
            var rightHandPos = Vector3.zero;
            
            if (_localPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal) != Vector3.zero && _localPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal) != Vector3.zero) // First checks if player has fingies before grabbing hand
            {
                leftHandPos = _localPlayer.GetBonePosition(HumanBodyBones.LeftIndexDistal);
                rightHandPos = _localPlayer.GetBonePosition(HumanBodyBones.RightIndexDistal);
            }
            else
            {
                leftHandPos = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                rightHandPos = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            }

            if (Fingie != null && ClosestPoint != null)
            {
                Fingie.position = leftHandPos;
                ClosestPoint.position = col.ClosestPoint(leftHandPos);
            }
            
            if (col.ClosestPoint(leftHandPos) == leftHandPos && !isPressingLeft && leftHandPos != Vector3.zero)
            {
                TargetBehaviour.SendCustomEvent(Event);
                isPressingLeft = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.2f, 0.5f, 0.1f);

                if (hasPressViz)
                {
                    PressViz.SetTrigger(PressName);
                }
                
                Debug.Log($"Pressed button for {transform.parent.name} | LEFT HAND");
            }
            else if (col.ClosestPoint(leftHandPos) != leftHandPos)
            {
                isPressingLeft = false;
            }
            
            
            if (col.ClosestPoint(rightHandPos) == rightHandPos && !isPressingRight&& rightHandPos != Vector3.zero)
            {
                TargetBehaviour.SendCustomEvent(Event);
                isPressingRight = true;
                _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.5f, 0.1f);
                
                if (hasPressViz)
                {
                    PressViz.SetTrigger(PressName);
                }
                
                Debug.Log($"Pressed button for {transform.parent.name} | RIGHT HAND");
            }
            else if (col.ClosestPoint(rightHandPos) != rightHandPos)
            {
                isPressingRight = false;
            }
        }
    }
}
