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
using VRC.Udon.Common.Interfaces;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Player : UdonSharpBehaviour
    {
        [FieldChangeCallback(nameof(isRunningProperty))] public bool Running;
        public Controller BlackjackController;
        
        [Header("Player Controller")] 
        [Space(20)]
        public int PlayerNum = 0;
        [UdonSynced] public bool IsActive = false;
        [UdonSynced] public bool HasFinishedTurn;
        [UdonSynced] public int currentOwnerID;
        [Space] 
        public bool CanSplit;
        public bool HasSplit;
        public PlayerDeck NormalDeck;
        public PlayerDeck SplitDeck;
        public GameObject SecondCard;
        public int SecondCardValue;
        [Space]
        public DeckManager Deck;
        [Space] 
        public bool isOwner;
        [HideInInspector] public bool ImInThis;
        public PlayerUIController PlayerUI;
        [Space] 
        public TextMeshProUGUI PlayerName;
        [Space(20)] [Header("Chip Stuff")] 
        public GameObject BetPanel;
        public Animator ChipAnimator;
        public TextMeshProUGUI CurrentBet;
        public int TotalBet;
        public TextMeshProUGUI TotalChips;
        public TextMeshProUGUI Timer;
        private float TimeLeft = 30f;

        private VRCPlayerApi _localPlayer;
        private bool JustHitNormal = false;
        private bool JustHitSplit = false;
        private bool isSplitting = false;
        private bool justReleasedControl;
        private ChipController ChipController;
        private bool CanBet = true;
        [HideInInspector] public bool myTurn = false;


        public bool isRunningProperty
        {
            get => Running;

            set
            {
                Running = value;

                PlayerUI.Running = value;
                
            }
        }
        
        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            SplitDeck.isSplitDeck = true;
            NormalDeck.isSplitDeck = false;
            HasFinishedTurn = true;
            ChipController = BlackjackController.ChipController;
        }

        private void Update()
        {
            isOwner = Networking.IsOwner(gameObject) && ImInThis;

            if (Running)
            {
                PlayerName.text = Networking.GetOwner(gameObject).displayName;
            
                CurrentBet.text = TotalBet.ToString();
                TotalChips.text = ChipController.CurrentChipCount.ToString();
                BetPanel.SetActive(isOwner && BlackjackController.UsingChips);
                ChipAnimator.SetBool("Active", !BlackjackController.localGameActive);

                if (isOwner)
                {
                    TotalBet = ChipController.CurrentBet[BlackjackController.TableIndex];

                    if (BlackjackController.localGameActive && !HasFinishedTurn && myTurn)
                    {
                        TimeLeft -= Time.deltaTime;

                        Timer.text = Mathf.RoundToInt(TimeLeft).ToString();

                        if (TimeLeft < 0f)
                        {
                            TimeLeft = 30f;
                            HasFinishedTurn = true;
                            EndTurn();
                        }
                    }
                    else
                    {
                        TimeLeft = 30;
                    }
                }
            }
        }

        public void LockPlayer()
        {
            if (ImInThis && !Networking.IsOwner(gameObject))
            {
                ImInThis = false;
            }
        }

        public void GlobalStartFirstCard()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartMatch));
        }
        
        public void GlobalStartSecondCard()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartMatchSecondCard));
        }

        public void StartMatch()
        {
            if (Networking.IsOwner(gameObject))
            {
                NormalDeck.SummonCard();
                CanBet = false;
            }
        }
        
        public void StartMatchSecondCard()
        {
            Debug.Log("Starting match with player " + PlayerNum);

            if (Networking.IsOwner(gameObject))
            {
                NormalDeck.SummonCard();
                
                PlayerUI.StartGame();
            }
        }

        public void _TakeControl()
        {
            if (BlackjackController.localGameActive || justReleasedControl) return;
            
            Networking.SetOwner(_localPlayer, gameObject);
            Networking.SetOwner(_localPlayer, NormalDeck.gameObject);
            Networking.SetOwner(_localPlayer, SplitDeck.gameObject);
            Networking.SetOwner(_localPlayer, PlayerUI.gameObject);

            ImInThis = true;
            IsActive = true;
            currentOwnerID = _localPlayer.playerId;
            RequestSerialization();
            
            BlackjackController.AudioController.PlayTakeControl();
            
            Debug.Log($"Taking control of {gameObject.name}");
        }

        public void ReleaseControl()
        {
            if (justReleasedControl) return;
            justReleasedControl = true;
            
            ImInThis = false;
            
            BlackjackController.AudioController.PlayReleaseControl();
            
            Debug.Log($"Releasing control of {gameObject.name}");
            
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GlobalReleaseControl));
            SendCustomEventDelayedSeconds(nameof(ResetJustReleasedControl), 2f);
        }

        public void ResetJustReleasedControl()
        {
            justReleasedControl = false;
        }

        public void GlobalReleaseControl()
        {
            if (IsActive && !HasFinishedTurn)
            {
                Stand();
            }

            //IsActive = false;
            ImInThis = false;
            isOwner = false;
            myTurn = false;

            if (Networking.IsOwner(BlackjackController.gameObject))
            {
                Networking.SetOwner(_localPlayer, gameObject);
                Networking.SetOwner(_localPlayer, NormalDeck.gameObject);
                Networking.SetOwner(_localPlayer, SplitDeck.gameObject);
                Networking.SetOwner(_localPlayer, PlayerUI.gameObject);

                IsActive = false;
                HasFinishedTurn = true;
            }
            
            RequestSerialization();
        }

        public void StartTurn()
        {
            if (isOwner)
            {
                PlayerUI.PlayerUIAnimator.ResetTrigger("Reset");
                PlayerUI.Active(true);
                PlayerUI.MyTurn(true);
                myTurn = true;
            }

            if (Networking.IsOwner(BlackjackController.gameObject))
            {
                PlayerUI.ActivateDealerUI();
            }

            BlackjackController.AudioController.PlayStartActivePlay();
        }

        public void EndTurn()
        {
            if (isOwner)
            {
                HasFinishedTurn = true;
                RequestSerialization();
            }

            PlayerUI.Active(false);
            PlayerUI.MyTurn(false);
            PlayerUI.Stand();
            myTurn = false;
        }

        public void _ManualNormalHitMe()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(HitMeNormal));
            PlayerUI.SetWantToHitNormal(false);
        }

        public void _ManualSplitHitMe()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(HitMeSplit));
            PlayerUI.SetWantToHitSplit(false);
        }
        
        public void _AutoNormalHitMe()
        {
            if (BlackjackController.ManualPlay)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(MarkNormalHit));
            }
            else
            {
                HitMeNormal();
            }
        }

        public void _AutoSplitHitMe()
        {
            if (BlackjackController.ManualPlay)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(MarkSplitHit));
            }
            else
            {
                HitMeSplit();
            }
        }

        public void MarkNormalHit()
        {
            PlayerUI.SetWantToHitNormal(true);
            BlackjackController.AudioController.PlayDealerWantToHit();
        }

        public void MarkSplitHit()
        {
            PlayerUI.SetWantToHitSplit(true);
            BlackjackController.AudioController.PlayDealerWantToHit();
        }

        public void HitMeNormal()
        {
            PlayerUI.SetWantToHitNormal(false);
            
            if (JustHitNormal || isSplitting || !isOwner) return;
            JustHitNormal = true;
            NormalDeck.SummonCard();
            
            BlackjackController.AudioController.PlayDrawCard();

            SendCustomEventDelayedSeconds(nameof(ResetJustHitNormal), 1f);
        }
        
        public void HitMeSplit()
        {
            PlayerUI.SetWantToHitSplit(false);
            
            if (JustHitSplit || !HasSplit || isSplitting || !isOwner) return;
            JustHitSplit = true;
            SplitDeck.SummonCard();
            
            BlackjackController.AudioController.PlayDrawCard();
            
            SendCustomEventDelayedSeconds(nameof(ResetJustHitSplit), 1f);
        }

        public void ResetJustHitNormal()
        {
            JustHitNormal = false;
        }

        public void ResetJustHitSplit()
        {
            JustHitSplit = false;
        }

        public void Split()
        {
            if (!CanSplit || HasSplit || HasFinishedTurn) return;
            CanSplit = false;
            HasSplit = true;

            if (BlackjackController.UsingChips)
            {
                ChipController.AddToBet(BlackjackController.TableIndex, TotalBet);;
            }
            
            BlackjackController.AudioController.PlaySplitDeck();

            NormalDeck.TotalDeckValue -= SecondCardValue;
            NormalDeck.localDeckValue -= SecondCardValue;
            SplitDeck.TotalDeckValue += SecondCardValue;
            SplitDeck.localDeckValue += SecondCardValue;

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SyncedSplit));
            
            SplitDeck.SendCustomEventDelayedSeconds("SummonCard", 1f);
            NormalDeck.SendCustomEventDelayedSeconds("SummonCard", 1f);

            isSplitting = true;
            SendCustomEventDelayedSeconds(nameof(ResetIsSplitting), 1f);
        }

        public void SyncedSplit()
        {
            NormalDeck.NextDeckPosition--;
            SplitDeck.NextDeckPosition++;
            SecondCard.transform.SetParent(SplitDeck.DeckPositions[0]);
            SecondCard.transform.SetPositionAndRotation(SplitDeck.DeckPositions[0].position, SplitDeck.DeckPositions[0].rotation);
            PlayerUI.SplitDeck();
        }

        public void ResetIsSplitting()
        {
            isSplitting = false;
        }

        public void Stand()
        {
            Debug.Log("Player " + PlayerNum + " has stood!");
            
            BlackjackController.AudioController.PlayStand();
            
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(EndTurn));
        }

        public void Bust()
        {
            Debug.Log("Player " + PlayerNum + " has bust!");
            
            BlackjackController.AudioController.PlayBust();
            
            EndTurn();
        }

        public void CheckBust()
        {
            if (HasSplit)
            {
                if (NormalDeck.IsBust && SplitDeck.IsBust)
                {
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Bust));
                }
            }
            else
            {
                if (NormalDeck.IsBust)
                {
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Bust));
                }
            }
        }

        public void ResetPlayer()
        {
            Debug.Log($"Resetting Player {PlayerNum}");
            HasSplit = false;
            CanSplit = false;

            PlayerUI.ResetAnimator();
            
            Deck.InitializeDeck();
            
            NormalDeck.Reset();
            SplitDeck.Reset();

            if (isOwner)
            {
                HasFinishedTurn = false;
                RequestSerialization();
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (IsActive && BlackjackController.localGameActive && !HasFinishedTurn)
            {
                GlobalReleaseControl();
            }
            
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player.playerId == currentOwnerID)
            {
                GlobalReleaseControl();
            }
        }


        public void SetWinState()
        {
            CanBet = true;

            var dealerValue = BlackjackController.Dealer.GetDeckValue();
            var normalValue = NormalDeck.localDeckValue;

            if (!IsActive) return;

            if (normalValue > 21)
            {
                PlayerUI.SetLose();

                if (isOwner)
                {
                    BlackjackController.AudioController.PlayLoseMatch();

                    if (BlackjackController.UsingChips)
                    {
                        ChipController.RemoveChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                    }
                }
            }
            else if (normalValue > dealerValue || dealerValue > 21)
            {
                PlayerUI.SetWin();
                
                if (isOwner)
                {
                    BlackjackController.AudioController.PlayWinMatch();

                    if (BlackjackController.UsingChips)
                    {
                        ChipController.AddChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                    }
                }
            }
            else if (normalValue == dealerValue)
            {
                PlayerUI.SetDraw();
                
                if (isOwner)
                {
                    BlackjackController.AudioController.PlayDrawMatch();
                }
            }
            else
            {
                PlayerUI.SetLose();
                
                if (isOwner)
                {
                    BlackjackController.AudioController.PlayLoseMatch();

                    if (BlackjackController.UsingChips)
                    {
                        ChipController.RemoveChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                    }
                }
            }
            
            if (HasSplit)
            {
                var splitValue = SplitDeck.localDeckValue;
                
                if (splitValue > 21)
                {
                    PlayerUI.SplitLose();
                    
                    if (isOwner)
                    {
                        if (BlackjackController.UsingChips)
                        {
                            ChipController.RemoveChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                        }
                    }
                }
                else if (splitValue > dealerValue || dealerValue > 21)
                {
                    PlayerUI.SplitWin();
                    
                    if (isOwner)
                    {
                        if (BlackjackController.UsingChips)
                        {
                            ChipController.AddChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                        }
                    }
                }
                else if (splitValue == dealerValue)
                {
                    PlayerUI.SplitDraw();
                }
                else
                {
                    PlayerUI.SplitLose();
                    
                    if (isOwner)
                    {
                        if (BlackjackController.UsingChips)
                        {
                            ChipController.RemoveChips(ChipController.CurrentBet[BlackjackController.TableIndex]);
                        }
                    }
                }
            }
            
            SendCustomEventDelayedFrames(nameof(DelayedResetBet), 5);
        }

        public void ForceResetPlayer()
        {
            Debug.Log($"Resetting Player {PlayerNum}");
            HasSplit = false;
            CanSplit = false;

            PlayerUI.ResetAnimator();
            
            Deck.InitializeDeck();
            
            NormalDeck.Reset();
            SplitDeck.Reset();

            if (Networking.IsOwner(gameObject))
            {
                GlobalReleaseControl();
            }
            
            ChipController.ResetBet(BlackjackController.TableIndex);
        }

        public void DelayedResetBet()
        {
            ChipController.ResetBet(BlackjackController.TableIndex);
        }

        #region Chip Stuff

        public void _Add5ToBet()
        {
            if (!CanBet || BlackjackController.localGameActive) return;
            
            ChipController.AddToBet(BlackjackController.TableIndex, 5);

            TotalBet = ChipController.CurrentBet[BlackjackController.TableIndex];
            RequestSerialization();
        }

        public void _Add10ToBet()
        {
            if (!CanBet || BlackjackController.localGameActive) return;
            
            ChipController.AddToBet(BlackjackController.TableIndex, 10);
            
            TotalBet = ChipController.CurrentBet[BlackjackController.TableIndex];
            RequestSerialization();
        }

        public void _Remove5FromBet()
        {
            if (!CanBet || ChipController.CurrentBet[BlackjackController.TableIndex] - 5 < 0 || BlackjackController.localGameActive) return;
            
            ChipController.TakeFromBet(BlackjackController.TableIndex, 5);
            
            TotalBet = ChipController.CurrentBet[BlackjackController.TableIndex];
            RequestSerialization();
        }

        public void _Remove10FromBet()
        {
            if (!CanBet || ChipController.CurrentBet[BlackjackController.TableIndex] - 10 < 0 || BlackjackController.localGameActive) return;
            
            ChipController.TakeFromBet(BlackjackController.TableIndex, 10);
            
            TotalBet = ChipController.CurrentBet[BlackjackController.TableIndex];
            RequestSerialization();
        }

        #endregion
    }
}
