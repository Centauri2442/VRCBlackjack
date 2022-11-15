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
    public class PlayerUIController : UdonSharpBehaviour
    {
        public bool Running;
        
        public Player PlayerController;
        [Space]
        public GameObject OwnerUI;
        public GameObject DealerUI;
        public GameObject DealerUICanvas;
        public GameObject NotOwnerUI;
        public GameObject PlayerName;
        [Space]
        public Animator PlayerUIAnimator;
        public Animator DealerUIAnimator;
        [Space] 
        public TextMeshProUGUI PlayerOwner;
        public TextMeshProUGUI NormalHelper;
        public TextMeshProUGUI SplitHelper;

        private bool isOwner;
        private bool isDealer;
        private Controller BlackjackController;
        private bool isStood;


        private void Start()
        {
            BlackjackController = PlayerController.BlackjackController;
        }

        public void Update()
        {
            isOwner = PlayerController.isOwner;

            if (Running)
            {
                OwnerUI.SetActive(isOwner);
                NotOwnerUI.SetActive((!isOwner && !PlayerController.IsActive) && !BlackjackController.localGameActive);
                DealerUI.SetActive(Networking.IsOwner(BlackjackController.gameObject) && BlackjackController.ManualPlay);
                PlayerName.SetActive(!isOwner && PlayerController.IsActive);
                PlayerOwner.text = Networking.GetOwner(gameObject).displayName;

                if (DealerUI.activeSelf)
                {
                    DealerUICanvas.layer = 0;
                }
                else
                {
                    DealerUICanvas.layer = 18;
                }

                var helpersEnabled = BlackjackController.HelpersEnabled;
            
                NormalHelper.gameObject.SetActive(helpersEnabled);
                SplitHelper.gameObject.SetActive(helpersEnabled);
            
                if (helpersEnabled)
                {
                    var normalDeckVal = PlayerController.NormalDeck.localDeckValue;
                    var splitDeckVal = PlayerController.SplitDeck.localDeckValue;

                    if (normalDeckVal > 0)
                    {
                        NormalHelper.text = normalDeckVal.ToString();
                    }
                    else
                    {
                        NormalHelper.text = "";
                    }
                
                    if (splitDeckVal > 0)
                    {
                        SplitHelper.text = splitDeckVal.ToString();
                    }
                    else
                    {
                        SplitHelper.text = "";
                    }
                }
                else
                {
                    NormalHelper.text = "";
                    SplitHelper.text = "";
                }
            }
            else
            {
                OwnerUI.SetActive(false);
                NotOwnerUI.SetActive(false);
                DealerUI.SetActive(false);
                PlayerName.SetActive(false);
            }
        }

        public void CanSplit(bool value)
        {
            PlayerUIAnimator.SetBool("CanSplit", value);
        }
        
        public void HasSplit(bool value)
        {
            PlayerUIAnimator.SetBool("HasSplit", value);
            DealerUIAnimator.SetBool("Split", true);
        }

        public void MyTurn(bool value)
        {
            PlayerUIAnimator.SetBool("MyTurn", value);
        }
        
        public void Active(bool value)
        {
            PlayerUIAnimator.SetBool("Active", value);
        }

        public void ActivateDealerUI()
        {
            DealerUIAnimator.SetTrigger("Activate");
        }

        public void ResetAnimator()
        {
            PlayerUIAnimator.ResetTrigger("Win");
            PlayerUIAnimator.ResetTrigger("Draw");
            PlayerUIAnimator.ResetTrigger("Lose");
            PlayerUIAnimator.SetTrigger("Reset");
            PlayerUIAnimator.SetBool("CanSplit", false);
            PlayerUIAnimator.SetBool("HasSplit", false);
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.ResetTrigger("ResetBust");
            
            DealerUIAnimator.SetBool("Stood", false);
            DealerUIAnimator.SetBool("Split", false);
            DealerUIAnimator.SetBool("NormalBust", false);
            DealerUIAnimator.SetBool("SplitBust", false);
            DealerUIAnimator.CrossFadeInFixedTime("Active.PlayerDealerIdleOff", 0f);
            isStood = false;
            SetWantToHitNormal(false);
            SetWantToHitSplit(false);
        }

        public void SetWin()
        {
            Debug.Log("Win!");
            PlayerUIAnimator.SetTrigger("ResetBust");
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("Win");
        }
        
        public void SetDraw()
        {
            Debug.Log("Draw!");
            PlayerUIAnimator.SetTrigger("ResetBust");
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("Draw");
        }

        public void SetLose()
        {
            Debug.Log("Lose!");
            PlayerUIAnimator.SetTrigger("ResetBust");
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("Lose");
        }

        public void SplitDeck()
        {
            PlayerUIAnimator.SetTrigger("Split");
            HasSplit(true);
        }

        public void NormalBust()
        {
            PlayerUIAnimator.SetTrigger("NormalBust");
            DealerUIAnimator.SetBool("NormalBust", true);
        }
        
        public void SplitBust()
        {
            PlayerUIAnimator.SetTrigger("SplitBust");
            DealerUIAnimator.SetBool("SplitBust", true);
        }

        public void SplitWin()
        {
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("SplitWin");
        }

        public void SplitDraw()
        {
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("SplitDraw");
        }

        public void SplitLose()
        {
            PlayerUIAnimator.ResetTrigger("StartGame");
            PlayerUIAnimator.SetTrigger("SplitLose");
        }

        public void StartGame()
        {
            PlayerUIAnimator.SetTrigger("StartGame");
        }

        public void Stand()
        {
            DealerUIAnimator.SetBool("Stood", true);
            DealerUIAnimator.SetBool("Split", false);
            SetWantToHitNormal(false);
            SetWantToHitSplit(false);

            if (!isStood && BlackjackController.localGameActive)
            {
                SendCustomEventDelayedSeconds(nameof(StandDelayed), 0.75f);
                isStood = true;
            }
        }

        public void StandDelayed()
        {
            SetWantToHitNormal(false);
            SetWantToHitSplit(false);
            DealerUIAnimator.SetTrigger("Deactivate");
        }

        public void SetWantToHitNormal(bool value)
        {
            if (isStood && value)
            {
                value = false;
            }
            
            DealerUIAnimator.SetBool("WantToHitNormal", value);
        }
        
        public void SetWantToHitSplit(bool value)
        {
            if (isStood && value)
            {
                value = false;
            }
            
            DealerUIAnimator.SetBool("WantToHitSplit", value);
        }
    }
}
