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
    public class PlayerDeck : UdonSharpBehaviour
    {
        public Player PlayerController;
        [Space] 
        public bool IsBust;
        [Space]
        [UdonSynced] public int TotalDeckValue;
        [HideInInspector] public int localDeckValue;
        public int[] DeckValue = new int[11];
        public Animator[] DeckAnimators = new Animator[11];
        [Space] 
        public int NextDeckPosition = 0;
        public Transform[] DeckPositions;

        private DeckManager Deck;
        private VRCPlayerApi _localPlayer;
        private int NumOfAces = 0;
        [HideInInspector] public bool isSplitDeck;
        public TextMeshProUGUI DealerValueDisplay;


        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            Deck = PlayerController.Deck;
        }

        public void SummonCard()
        {
            if (PlayerController.HasFinishedTurn) return;
            
            var cardIndex = GetUnusedCardIndex();

            if (cardIndex < 0 || NextDeckPosition > 10)
            {
                Debug.Log("No free cards left!");
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkSpawnCard" + cardIndex + "DeckPos" + NextDeckPosition);
            NextDeckPosition++;
        }

        public void SpawnCard(int cardIndex, int deckPos)
        {
            var card = Instantiate(Deck.CardReferences[cardIndex]).transform;
            
            PlayerController.BlackjackController.AudioController.PlayDrawCard();
            
            card.SetParent(DeckPositions[deckPos]);
            card.SetPositionAndRotation(DeckPositions[deckPos].position, DeckPositions[deckPos].rotation);

            var cardAnimator = card.GetComponent<Animator>();
            DeckAnimators[deckPos] = cardAnimator;
            cardAnimator.SetTrigger("NotHidden");

            string value = "0";
            var valueStartIndex = card.name.IndexOf("[")+1;
            var valueEndIndex = card.name.IndexOf("]");
            value = card.name.Substring(valueStartIndex, (valueEndIndex - valueStartIndex));
            DeckValue[deckPos] = Convert.ToInt32(value);

            if (Networking.IsOwner(gameObject))
            {
                AddCardValue(DeckValue[deckPos]);
            }
            
            if (deckPos == 1)
            {
                PlayerController.SecondCard = card.gameObject;
                PlayerController.SecondCardValue = DeckValue[deckPos];
                
                CheckIfCanSplit();
            }
            
            SetAnimatorArray();

            Debug.Log("Spawning card: " + card.name + " | " + "Deck Position: " + deckPos);
            
            if (VRCPlayerApi.GetPlayerCount() < 2)
            {
                DealerValueDisplay.text = localDeckValue.ToString();
            }
            else
            {
                DealerValueDisplay.text = TotalDeckValue.ToString();
            }
        }

        public void SetAnimatorArray()
        {
            for (int i = 0; i < DeckAnimators.Length; i++)
            {
                if (DeckPositions[i].childCount > 0)
                {
                    DeckAnimators[i] = DeckPositions[i].GetChild(0).GetComponent<Animator>();
                }
            }
        }

        private void CheckIfCanSplit()
        {
            if (DeckValue[0] == DeckValue[1])
            {
                if (!PlayerController.BlackjackController.UsingChips)
                {
                    PlayerController.CanSplit = true;
                    PlayerController.PlayerUI.CanSplit(true);
                }
                else if (PlayerController.TotalBet * 2 <= PlayerController.BlackjackController.ChipController.CurrentChipCount)
                {
                    PlayerController.CanSplit = true;
                    PlayerController.PlayerUI.CanSplit(true);
                }
                else
                {
                    PlayerController.CanSplit = false;
                    PlayerController.PlayerUI.CanSplit(false);
                }
            }
        }

        private void AddCardValue(int value)
        {
            if (value != 11)
            {
                localDeckValue += value;
            }
            else
            {
                if (localDeckValue >= 11)
                {
                    localDeckValue += 1;
                }
                else
                {
                    localDeckValue += 11;
                    NumOfAces++;
                }
            }

            if (localDeckValue > 21 && NumOfAces > 0)
            {
                localDeckValue -= 10;
                NumOfAces--;
            }
            else if (localDeckValue > 21)
            {
                IsBust = true;

                if (isSplitDeck)
                {
                    PlayerController.PlayerUI.SendCustomNetworkEvent(NetworkEventTarget.All, "SplitBust");
                }
                else
                {
                    PlayerController.PlayerUI.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalBust");
                }
                
                PlayerController.CheckBust();
            }
            
            RequestSerialization();
            
            #if UNITY_EDITOR
            
            OnPreSerialization();
            
            #endif
        }

        private int GetUnusedCardIndex()
        {
            int unusedCard = -1;

            var startIndex = UnityEngine.Random.Range(0, Deck.CardReferences.Length);
            var index = startIndex;

            if (Deck.NumOfEachCardLeft[index] > 0)
            {
                unusedCard = index;
            }
            else
            {
                while (index < Deck.CardReferences.Length - 1)
                {
                    if (Deck.NumOfEachCardLeft[index] < 1)
                    {
                        index++;
                    }
                    else
                    {
                        unusedCard = index;
                        
                        return unusedCard;
                    }
                }

                if (index >= Deck.CardReferences.Length-1)
                {
                    if (Deck.NumOfEachCardLeft[index] < 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        unusedCard = index;
                        
                        return unusedCard;
                    }
                }
                
                while (index < startIndex)
                {
                    if (Deck.NumOfEachCardLeft[index] < 1)
                    {
                        index++;
                    }
                    else
                    {
                        unusedCard = index;
                        
                        return unusedCard;
                    }
                }

                if (index == startIndex)
                {
                    return -1;
                }
            }

            return unusedCard;
        }

        #region Spawn Card Network Events
        
        public void NetworkSpawnCard0DeckPos0() { SpawnCard(0, 0); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos1() { SpawnCard(0, 1); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos2() { SpawnCard(0, 2); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos3() { SpawnCard(0, 3); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos4() { SpawnCard(0, 4); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos5() { SpawnCard(0, 5); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos6() { SpawnCard(0, 6); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos7() { SpawnCard(0, 7); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos8() { SpawnCard(0, 8); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos9() { SpawnCard(0, 9); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard0DeckPos10() { SpawnCard(0, 10); Deck.NumOfEachCardLeft[0]--; }
        public void NetworkSpawnCard1DeckPos0() { SpawnCard(1, 0); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos1() { SpawnCard(1, 1); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos2() { SpawnCard(1, 2); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos3() { SpawnCard(1, 3); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos4() { SpawnCard(1, 4); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos5() { SpawnCard(1, 5); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos6() { SpawnCard(1, 6); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos7() { SpawnCard(1, 7); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos8() { SpawnCard(1, 8); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos9() { SpawnCard(1, 9); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard1DeckPos10() { SpawnCard(1, 10); Deck.NumOfEachCardLeft[1]--; }
        public void NetworkSpawnCard2DeckPos0() { SpawnCard(2, 0); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos1() { SpawnCard(2, 1); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos2() { SpawnCard(2, 2); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos3() { SpawnCard(2, 3); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos4() { SpawnCard(2, 4); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos5() { SpawnCard(2, 5); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos6() { SpawnCard(2, 6); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos7() { SpawnCard(2, 7); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos8() { SpawnCard(2, 8); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos9() { SpawnCard(2, 9); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard2DeckPos10() { SpawnCard(2, 10); Deck.NumOfEachCardLeft[2]--; }
        public void NetworkSpawnCard3DeckPos0() { SpawnCard(3, 0); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos1() { SpawnCard(3, 1); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos2() { SpawnCard(3, 2); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos3() { SpawnCard(3, 3); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos4() { SpawnCard(3, 4); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos5() { SpawnCard(3, 5); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos6() { SpawnCard(3, 6); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos7() { SpawnCard(3, 7); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos8() { SpawnCard(3, 8); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos9() { SpawnCard(3, 9); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard3DeckPos10() { SpawnCard(3, 10); Deck.NumOfEachCardLeft[3]--; }
        public void NetworkSpawnCard4DeckPos0() { SpawnCard(4, 0); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos1() { SpawnCard(4, 1); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos2() { SpawnCard(4, 2); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos3() { SpawnCard(4, 3); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos4() { SpawnCard(4, 4); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos5() { SpawnCard(4, 5); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos6() { SpawnCard(4, 6); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos7() { SpawnCard(4, 7); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos8() { SpawnCard(4, 8); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos9() { SpawnCard(4, 9); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard4DeckPos10() { SpawnCard(4, 10); Deck.NumOfEachCardLeft[4]--; }
        public void NetworkSpawnCard5DeckPos0() { SpawnCard(5, 0); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos1() { SpawnCard(5, 1); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos2() { SpawnCard(5, 2); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos3() { SpawnCard(5, 3); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos4() { SpawnCard(5, 4); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos5() { SpawnCard(5, 5); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos6() { SpawnCard(5, 6); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos7() { SpawnCard(5, 7); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos8() { SpawnCard(5, 8); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos9() { SpawnCard(5, 9); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard5DeckPos10() { SpawnCard(5, 10); Deck.NumOfEachCardLeft[5]--; }
        public void NetworkSpawnCard6DeckPos0() { SpawnCard(6, 0); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos1() { SpawnCard(6, 1); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos2() { SpawnCard(6, 2); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos3() { SpawnCard(6, 3); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos4() { SpawnCard(6, 4); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos5() { SpawnCard(6, 5); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos6() { SpawnCard(6, 6); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos7() { SpawnCard(6, 7); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos8() { SpawnCard(6, 8); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos9() { SpawnCard(6, 9); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard6DeckPos10() { SpawnCard(6, 10); Deck.NumOfEachCardLeft[6]--; }
        public void NetworkSpawnCard7DeckPos0() { SpawnCard(7, 0); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos1() { SpawnCard(7, 1); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos2() { SpawnCard(7, 2); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos3() { SpawnCard(7, 3); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos4() { SpawnCard(7, 4); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos5() { SpawnCard(7, 5); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos6() { SpawnCard(7, 6); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos7() { SpawnCard(7, 7); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos8() { SpawnCard(7, 8); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos9() { SpawnCard(7, 9); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard7DeckPos10() { SpawnCard(7, 10); Deck.NumOfEachCardLeft[7]--; }
        public void NetworkSpawnCard8DeckPos0() { SpawnCard(8, 0); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos1() { SpawnCard(8, 1); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos2() { SpawnCard(8, 2); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos3() { SpawnCard(8, 3); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos4() { SpawnCard(8, 4); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos5() { SpawnCard(8, 5); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos6() { SpawnCard(8, 6); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos7() { SpawnCard(8, 7); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos8() { SpawnCard(8, 8); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos9() { SpawnCard(8, 9); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard8DeckPos10() { SpawnCard(8, 10); Deck.NumOfEachCardLeft[8]--; }
        public void NetworkSpawnCard9DeckPos0() { SpawnCard(9, 0); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos1() { SpawnCard(9, 1); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos2() { SpawnCard(9, 2); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos3() { SpawnCard(9, 3); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos4() { SpawnCard(9, 4); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos5() { SpawnCard(9, 5); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos6() { SpawnCard(9, 6); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos7() { SpawnCard(9, 7); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos8() { SpawnCard(9, 8); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos9() { SpawnCard(9, 9); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard9DeckPos10() { SpawnCard(9, 10); Deck.NumOfEachCardLeft[9]--; }
        public void NetworkSpawnCard10DeckPos0() { SpawnCard(10, 0); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos1() { SpawnCard(10, 1); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos2() { SpawnCard(10, 2); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos3() { SpawnCard(10, 3); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos4() { SpawnCard(10, 4); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos5() { SpawnCard(10, 5); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos6() { SpawnCard(10, 6); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos7() { SpawnCard(10, 7); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos8() { SpawnCard(10, 8); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos9() { SpawnCard(10, 9); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard10DeckPos10() { SpawnCard(10, 10); Deck.NumOfEachCardLeft[10]--; }
        public void NetworkSpawnCard11DeckPos0() { SpawnCard(11, 0); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos1() { SpawnCard(11, 1); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos2() { SpawnCard(11, 2); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos3() { SpawnCard(11, 3); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos4() { SpawnCard(11, 4); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos5() { SpawnCard(11, 5); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos6() { SpawnCard(11, 6); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos7() { SpawnCard(11, 7); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos8() { SpawnCard(11, 8); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos9() { SpawnCard(11, 9); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard11DeckPos10() { SpawnCard(11, 10); Deck.NumOfEachCardLeft[11]--; }
        public void NetworkSpawnCard12DeckPos0() { SpawnCard(12, 0); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos1() { SpawnCard(12, 1); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos2() { SpawnCard(12, 2); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos3() { SpawnCard(12, 3); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos4() { SpawnCard(12, 4); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos5() { SpawnCard(12, 5); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos6() { SpawnCard(12, 6); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos7() { SpawnCard(12, 7); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos8() { SpawnCard(12, 8); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos9() { SpawnCard(12, 9); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard12DeckPos10() { SpawnCard(12, 10); Deck.NumOfEachCardLeft[12]--; }
        public void NetworkSpawnCard13DeckPos0() { SpawnCard(13, 0); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos1() { SpawnCard(13, 1); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos2() { SpawnCard(13, 2); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos3() { SpawnCard(13, 3); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos4() { SpawnCard(13, 4); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos5() { SpawnCard(13, 5); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos6() { SpawnCard(13, 6); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos7() { SpawnCard(13, 7); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos8() { SpawnCard(13, 8); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos9() { SpawnCard(13, 9); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard13DeckPos10() { SpawnCard(13, 10); Deck.NumOfEachCardLeft[13]--; }
        public void NetworkSpawnCard14DeckPos0() { SpawnCard(14, 0); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos1() { SpawnCard(14, 1); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos2() { SpawnCard(14, 2); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos3() { SpawnCard(14, 3); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos4() { SpawnCard(14, 4); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos5() { SpawnCard(14, 5); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos6() { SpawnCard(14, 6); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos7() { SpawnCard(14, 7); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos8() { SpawnCard(14, 8); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos9() { SpawnCard(14, 9); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard14DeckPos10() { SpawnCard(14, 10); Deck.NumOfEachCardLeft[14]--; }
        public void NetworkSpawnCard15DeckPos0() { SpawnCard(15, 0); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos1() { SpawnCard(15, 1); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos2() { SpawnCard(15, 2); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos3() { SpawnCard(15, 3); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos4() { SpawnCard(15, 4); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos5() { SpawnCard(15, 5); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos6() { SpawnCard(15, 6); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos7() { SpawnCard(15, 7); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos8() { SpawnCard(15, 8); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos9() { SpawnCard(15, 9); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard15DeckPos10() { SpawnCard(15, 10); Deck.NumOfEachCardLeft[15]--; }
        public void NetworkSpawnCard16DeckPos0() { SpawnCard(16, 0); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos1() { SpawnCard(16, 1); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos2() { SpawnCard(16, 2); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos3() { SpawnCard(16, 3); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos4() { SpawnCard(16, 4); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos5() { SpawnCard(16, 5); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos6() { SpawnCard(16, 6); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos7() { SpawnCard(16, 7); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos8() { SpawnCard(16, 8); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos9() { SpawnCard(16, 9); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard16DeckPos10() { SpawnCard(16, 10); Deck.NumOfEachCardLeft[16]--; }
        public void NetworkSpawnCard17DeckPos0() { SpawnCard(17, 0); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos1() { SpawnCard(17, 1); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos2() { SpawnCard(17, 2); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos3() { SpawnCard(17, 3); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos4() { SpawnCard(17, 4); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos5() { SpawnCard(17, 5); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos6() { SpawnCard(17, 6); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos7() { SpawnCard(17, 7); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos8() { SpawnCard(17, 8); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos9() { SpawnCard(17, 9); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard17DeckPos10() { SpawnCard(17, 10); Deck.NumOfEachCardLeft[17]--; }
        public void NetworkSpawnCard18DeckPos0() { SpawnCard(18, 0); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos1() { SpawnCard(18, 1); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos2() { SpawnCard(18, 2); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos3() { SpawnCard(18, 3); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos4() { SpawnCard(18, 4); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos5() { SpawnCard(18, 5); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos6() { SpawnCard(18, 6); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos7() { SpawnCard(18, 7); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos8() { SpawnCard(18, 8); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos9() { SpawnCard(18, 9); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard18DeckPos10() { SpawnCard(18, 10); Deck.NumOfEachCardLeft[18]--; }
        public void NetworkSpawnCard19DeckPos0() { SpawnCard(19, 0); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos1() { SpawnCard(19, 1); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos2() { SpawnCard(19, 2); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos3() { SpawnCard(19, 3); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos4() { SpawnCard(19, 4); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos5() { SpawnCard(19, 5); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos6() { SpawnCard(19, 6); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos7() { SpawnCard(19, 7); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos8() { SpawnCard(19, 8); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos9() { SpawnCard(19, 9); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard19DeckPos10() { SpawnCard(19, 10); Deck.NumOfEachCardLeft[19]--; }
        public void NetworkSpawnCard20DeckPos0() { SpawnCard(20, 0); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos1() { SpawnCard(20, 1); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos2() { SpawnCard(20, 2); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos3() { SpawnCard(20, 3); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos4() { SpawnCard(20, 4); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos5() { SpawnCard(20, 5); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos6() { SpawnCard(20, 6); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos7() { SpawnCard(20, 7); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos8() { SpawnCard(20, 8); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos9() { SpawnCard(20, 9); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard20DeckPos10() { SpawnCard(20, 10); Deck.NumOfEachCardLeft[20]--; }
        public void NetworkSpawnCard21DeckPos0() { SpawnCard(21, 0); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos1() { SpawnCard(21, 1); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos2() { SpawnCard(21, 2); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos3() { SpawnCard(21, 3); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos4() { SpawnCard(21, 4); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos5() { SpawnCard(21, 5); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos6() { SpawnCard(21, 6); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos7() { SpawnCard(21, 7); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos8() { SpawnCard(21, 8); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos9() { SpawnCard(21, 9); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard21DeckPos10() { SpawnCard(21, 10); Deck.NumOfEachCardLeft[21]--; }
        public void NetworkSpawnCard22DeckPos0() { SpawnCard(22, 0); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos1() { SpawnCard(22, 1); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos2() { SpawnCard(22, 2); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos3() { SpawnCard(22, 3); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos4() { SpawnCard(22, 4); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos5() { SpawnCard(22, 5); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos6() { SpawnCard(22, 6); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos7() { SpawnCard(22, 7); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos8() { SpawnCard(22, 8); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos9() { SpawnCard(22, 9); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard22DeckPos10() { SpawnCard(22, 10); Deck.NumOfEachCardLeft[22]--; }
        public void NetworkSpawnCard23DeckPos0() { SpawnCard(23, 0); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos1() { SpawnCard(23, 1); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos2() { SpawnCard(23, 2); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos3() { SpawnCard(23, 3); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos4() { SpawnCard(23, 4); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos5() { SpawnCard(23, 5); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos6() { SpawnCard(23, 6); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos7() { SpawnCard(23, 7); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos8() { SpawnCard(23, 8); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos9() { SpawnCard(23, 9); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard23DeckPos10() { SpawnCard(23, 10); Deck.NumOfEachCardLeft[23]--; }
        public void NetworkSpawnCard24DeckPos0() { SpawnCard(24, 0); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos1() { SpawnCard(24, 1); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos2() { SpawnCard(24, 2); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos3() { SpawnCard(24, 3); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos4() { SpawnCard(24, 4); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos5() { SpawnCard(24, 5); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos6() { SpawnCard(24, 6); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos7() { SpawnCard(24, 7); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos8() { SpawnCard(24, 8); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos9() { SpawnCard(24, 9); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard24DeckPos10() { SpawnCard(24, 10); Deck.NumOfEachCardLeft[24]--; }
        public void NetworkSpawnCard25DeckPos0() { SpawnCard(25, 0); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos1() { SpawnCard(25, 1); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos2() { SpawnCard(25, 2); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos3() { SpawnCard(25, 3); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos4() { SpawnCard(25, 4); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos5() { SpawnCard(25, 5); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos6() { SpawnCard(25, 6); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos7() { SpawnCard(25, 7); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos8() { SpawnCard(25, 8); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos9() { SpawnCard(25, 9); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard25DeckPos10() { SpawnCard(25, 10); Deck.NumOfEachCardLeft[25]--; }
        public void NetworkSpawnCard26DeckPos0() { SpawnCard(26, 0); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos1() { SpawnCard(26, 1); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos2() { SpawnCard(26, 2); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos3() { SpawnCard(26, 3); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos4() { SpawnCard(26, 4); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos5() { SpawnCard(26, 5); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos6() { SpawnCard(26, 6); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos7() { SpawnCard(26, 7); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos8() { SpawnCard(26, 8); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos9() { SpawnCard(26, 9); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard26DeckPos10() { SpawnCard(26, 10); Deck.NumOfEachCardLeft[26]--; }
        public void NetworkSpawnCard27DeckPos0() { SpawnCard(27, 0); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos1() { SpawnCard(27, 1); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos2() { SpawnCard(27, 2); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos3() { SpawnCard(27, 3); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos4() { SpawnCard(27, 4); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos5() { SpawnCard(27, 5); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos6() { SpawnCard(27, 6); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos7() { SpawnCard(27, 7); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos8() { SpawnCard(27, 8); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos9() { SpawnCard(27, 9); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard27DeckPos10() { SpawnCard(27, 10); Deck.NumOfEachCardLeft[27]--; }
        public void NetworkSpawnCard28DeckPos0() { SpawnCard(28, 0); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos1() { SpawnCard(28, 1); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos2() { SpawnCard(28, 2); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos3() { SpawnCard(28, 3); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos4() { SpawnCard(28, 4); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos5() { SpawnCard(28, 5); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos6() { SpawnCard(28, 6); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos7() { SpawnCard(28, 7); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos8() { SpawnCard(28, 8); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos9() { SpawnCard(28, 9); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard28DeckPos10() { SpawnCard(28, 10); Deck.NumOfEachCardLeft[28]--; }
        public void NetworkSpawnCard29DeckPos0() { SpawnCard(29, 0); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos1() { SpawnCard(29, 1); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos2() { SpawnCard(29, 2); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos3() { SpawnCard(29, 3); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos4() { SpawnCard(29, 4); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos5() { SpawnCard(29, 5); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos6() { SpawnCard(29, 6); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos7() { SpawnCard(29, 7); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos8() { SpawnCard(29, 8); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos9() { SpawnCard(29, 9); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard29DeckPos10() { SpawnCard(29, 10); Deck.NumOfEachCardLeft[29]--; }
        public void NetworkSpawnCard30DeckPos0() { SpawnCard(30, 0); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos1() { SpawnCard(30, 1); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos2() { SpawnCard(30, 2); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos3() { SpawnCard(30, 3); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos4() { SpawnCard(30, 4); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos5() { SpawnCard(30, 5); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos6() { SpawnCard(30, 6); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos7() { SpawnCard(30, 7); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos8() { SpawnCard(30, 8); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos9() { SpawnCard(30, 9); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard30DeckPos10() { SpawnCard(30, 10); Deck.NumOfEachCardLeft[30]--; }
        public void NetworkSpawnCard31DeckPos0() { SpawnCard(31, 0); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos1() { SpawnCard(31, 1); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos2() { SpawnCard(31, 2); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos3() { SpawnCard(31, 3); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos4() { SpawnCard(31, 4); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos5() { SpawnCard(31, 5); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos6() { SpawnCard(31, 6); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos7() { SpawnCard(31, 7); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos8() { SpawnCard(31, 8); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos9() { SpawnCard(31, 9); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard31DeckPos10() { SpawnCard(31, 10); Deck.NumOfEachCardLeft[31]--; }
        public void NetworkSpawnCard32DeckPos0() { SpawnCard(32, 0); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos1() { SpawnCard(32, 1); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos2() { SpawnCard(32, 2); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos3() { SpawnCard(32, 3); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos4() { SpawnCard(32, 4); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos5() { SpawnCard(32, 5); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos6() { SpawnCard(32, 6); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos7() { SpawnCard(32, 7); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos8() { SpawnCard(32, 8); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos9() { SpawnCard(32, 9); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard32DeckPos10() { SpawnCard(32, 10); Deck.NumOfEachCardLeft[32]--; }
        public void NetworkSpawnCard33DeckPos0() { SpawnCard(33, 0); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos1() { SpawnCard(33, 1); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos2() { SpawnCard(33, 2); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos3() { SpawnCard(33, 3); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos4() { SpawnCard(33, 4); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos5() { SpawnCard(33, 5); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos6() { SpawnCard(33, 6); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos7() { SpawnCard(33, 7); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos8() { SpawnCard(33, 8); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos9() { SpawnCard(33, 9); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard33DeckPos10() { SpawnCard(33, 10); Deck.NumOfEachCardLeft[33]--; }
        public void NetworkSpawnCard34DeckPos0() { SpawnCard(34, 0); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos1() { SpawnCard(34, 1); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos2() { SpawnCard(34, 2); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos3() { SpawnCard(34, 3); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos4() { SpawnCard(34, 4); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos5() { SpawnCard(34, 5); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos6() { SpawnCard(34, 6); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos7() { SpawnCard(34, 7); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos8() { SpawnCard(34, 8); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos9() { SpawnCard(34, 9); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard34DeckPos10() { SpawnCard(34, 10); Deck.NumOfEachCardLeft[34]--; }
        public void NetworkSpawnCard35DeckPos0() { SpawnCard(35, 0); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos1() { SpawnCard(35, 1); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos2() { SpawnCard(35, 2); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos3() { SpawnCard(35, 3); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos4() { SpawnCard(35, 4); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos5() { SpawnCard(35, 5); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos6() { SpawnCard(35, 6); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos7() { SpawnCard(35, 7); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos8() { SpawnCard(35, 8); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos9() { SpawnCard(35, 9); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard35DeckPos10() { SpawnCard(35, 10); Deck.NumOfEachCardLeft[35]--; }
        public void NetworkSpawnCard36DeckPos0() { SpawnCard(36, 0); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos1() { SpawnCard(36, 1); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos2() { SpawnCard(36, 2); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos3() { SpawnCard(36, 3); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos4() { SpawnCard(36, 4); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos5() { SpawnCard(36, 5); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos6() { SpawnCard(36, 6); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos7() { SpawnCard(36, 7); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos8() { SpawnCard(36, 8); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos9() { SpawnCard(36, 9); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard36DeckPos10() { SpawnCard(36, 10); Deck.NumOfEachCardLeft[36]--; }
        public void NetworkSpawnCard37DeckPos0() { SpawnCard(37, 0); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos1() { SpawnCard(37, 1); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos2() { SpawnCard(37, 2); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos3() { SpawnCard(37, 3); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos4() { SpawnCard(37, 4); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos5() { SpawnCard(37, 5); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos6() { SpawnCard(37, 6); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos7() { SpawnCard(37, 7); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos8() { SpawnCard(37, 8); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos9() { SpawnCard(37, 9); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard37DeckPos10() { SpawnCard(37, 10); Deck.NumOfEachCardLeft[37]--; }
        public void NetworkSpawnCard38DeckPos0() { SpawnCard(38, 0); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos1() { SpawnCard(38, 1); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos2() { SpawnCard(38, 2); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos3() { SpawnCard(38, 3); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos4() { SpawnCard(38, 4); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos5() { SpawnCard(38, 5); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos6() { SpawnCard(38, 6); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos7() { SpawnCard(38, 7); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos8() { SpawnCard(38, 8); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos9() { SpawnCard(38, 9); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard38DeckPos10() { SpawnCard(38, 10); Deck.NumOfEachCardLeft[38]--; }
        public void NetworkSpawnCard39DeckPos0() { SpawnCard(39, 0); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos1() { SpawnCard(39, 1); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos2() { SpawnCard(39, 2); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos3() { SpawnCard(39, 3); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos4() { SpawnCard(39, 4); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos5() { SpawnCard(39, 5); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos6() { SpawnCard(39, 6); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos7() { SpawnCard(39, 7); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos8() { SpawnCard(39, 8); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos9() { SpawnCard(39, 9); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard39DeckPos10() { SpawnCard(39, 10); Deck.NumOfEachCardLeft[39]--; }
        public void NetworkSpawnCard40DeckPos0() { SpawnCard(40, 0); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos1() { SpawnCard(40, 1); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos2() { SpawnCard(40, 2); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos3() { SpawnCard(40, 3); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos4() { SpawnCard(40, 4); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos5() { SpawnCard(40, 5); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos6() { SpawnCard(40, 6); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos7() { SpawnCard(40, 7); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos8() { SpawnCard(40, 8); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos9() { SpawnCard(40, 9); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard40DeckPos10() { SpawnCard(40, 10); Deck.NumOfEachCardLeft[40]--; }
        public void NetworkSpawnCard41DeckPos0() { SpawnCard(41, 0); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos1() { SpawnCard(41, 1); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos2() { SpawnCard(41, 2); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos3() { SpawnCard(41, 3); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos4() { SpawnCard(41, 4); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos5() { SpawnCard(41, 5); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos6() { SpawnCard(41, 6); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos7() { SpawnCard(41, 7); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos8() { SpawnCard(41, 8); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos9() { SpawnCard(41, 9); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard41DeckPos10() { SpawnCard(41, 10); Deck.NumOfEachCardLeft[41]--; }
        public void NetworkSpawnCard42DeckPos0() { SpawnCard(42, 0); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos1() { SpawnCard(42, 1); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos2() { SpawnCard(42, 2); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos3() { SpawnCard(42, 3); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos4() { SpawnCard(42, 4); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos5() { SpawnCard(42, 5); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos6() { SpawnCard(42, 6); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos7() { SpawnCard(42, 7); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos8() { SpawnCard(42, 8); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos9() { SpawnCard(42, 9); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard42DeckPos10() { SpawnCard(42, 10); Deck.NumOfEachCardLeft[42]--; }
        public void NetworkSpawnCard43DeckPos0() { SpawnCard(43, 0); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos1() { SpawnCard(43, 1); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos2() { SpawnCard(43, 2); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos3() { SpawnCard(43, 3); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos4() { SpawnCard(43, 4); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos5() { SpawnCard(43, 5); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos6() { SpawnCard(43, 6); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos7() { SpawnCard(43, 7); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos8() { SpawnCard(43, 8); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos9() { SpawnCard(43, 9); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard43DeckPos10() { SpawnCard(43, 10); Deck.NumOfEachCardLeft[43]--; }
        public void NetworkSpawnCard44DeckPos0() { SpawnCard(44, 0); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos1() { SpawnCard(44, 1); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos2() { SpawnCard(44, 2); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos3() { SpawnCard(44, 3); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos4() { SpawnCard(44, 4); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos5() { SpawnCard(44, 5); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos6() { SpawnCard(44, 6); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos7() { SpawnCard(44, 7); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos8() { SpawnCard(44, 8); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos9() { SpawnCard(44, 9); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard44DeckPos10() { SpawnCard(44, 10); Deck.NumOfEachCardLeft[44]--; }
        public void NetworkSpawnCard45DeckPos0() { SpawnCard(45, 0); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos1() { SpawnCard(45, 1); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos2() { SpawnCard(45, 2); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos3() { SpawnCard(45, 3); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos4() { SpawnCard(45, 4); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos5() { SpawnCard(45, 5); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos6() { SpawnCard(45, 6); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos7() { SpawnCard(45, 7); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos8() { SpawnCard(45, 8); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos9() { SpawnCard(45, 9); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard45DeckPos10() { SpawnCard(45, 10); Deck.NumOfEachCardLeft[45]--; }
        public void NetworkSpawnCard46DeckPos0() { SpawnCard(46, 0); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos1() { SpawnCard(46, 1); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos2() { SpawnCard(46, 2); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos3() { SpawnCard(46, 3); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos4() { SpawnCard(46, 4); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos5() { SpawnCard(46, 5); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos6() { SpawnCard(46, 6); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos7() { SpawnCard(46, 7); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos8() { SpawnCard(46, 8); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos9() { SpawnCard(46, 9); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard46DeckPos10() { SpawnCard(46, 10); Deck.NumOfEachCardLeft[46]--; }
        public void NetworkSpawnCard47DeckPos0() { SpawnCard(47, 0); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos1() { SpawnCard(47, 1); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos2() { SpawnCard(47, 2); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos3() { SpawnCard(47, 3); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos4() { SpawnCard(47, 4); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos5() { SpawnCard(47, 5); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos6() { SpawnCard(47, 6); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos7() { SpawnCard(47, 7); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos8() { SpawnCard(47, 8); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos9() { SpawnCard(47, 9); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard47DeckPos10() { SpawnCard(47, 10); Deck.NumOfEachCardLeft[47]--; }
        public void NetworkSpawnCard48DeckPos0() { SpawnCard(48, 0); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos1() { SpawnCard(48, 1); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos2() { SpawnCard(48, 2); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos3() { SpawnCard(48, 3); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos4() { SpawnCard(48, 4); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos5() { SpawnCard(48, 5); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos6() { SpawnCard(48, 6); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos7() { SpawnCard(48, 7); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos8() { SpawnCard(48, 8); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos9() { SpawnCard(48, 9); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard48DeckPos10() { SpawnCard(48, 10); Deck.NumOfEachCardLeft[48]--; }
        public void NetworkSpawnCard49DeckPos0() { SpawnCard(49, 0); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos1() { SpawnCard(49, 1); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos2() { SpawnCard(49, 2); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos3() { SpawnCard(49, 3); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos4() { SpawnCard(49, 4); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos5() { SpawnCard(49, 5); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos6() { SpawnCard(49, 6); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos7() { SpawnCard(49, 7); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos8() { SpawnCard(49, 8); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos9() { SpawnCard(49, 9); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard49DeckPos10() { SpawnCard(49, 10); Deck.NumOfEachCardLeft[49]--; }
        public void NetworkSpawnCard50DeckPos0() { SpawnCard(50, 0); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos1() { SpawnCard(50, 1); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos2() { SpawnCard(50, 2); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos3() { SpawnCard(50, 3); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos4() { SpawnCard(50, 4); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos5() { SpawnCard(50, 5); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos6() { SpawnCard(50, 6); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos7() { SpawnCard(50, 7); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos8() { SpawnCard(50, 8); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos9() { SpawnCard(50, 9); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard50DeckPos10() { SpawnCard(50, 10); Deck.NumOfEachCardLeft[50]--; }
        public void NetworkSpawnCard51DeckPos0() { SpawnCard(51, 0); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos1() { SpawnCard(51, 1); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos2() { SpawnCard(51, 2); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos3() { SpawnCard(51, 3); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos4() { SpawnCard(51, 4); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos5() { SpawnCard(51, 5); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos6() { SpawnCard(51, 6); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos7() { SpawnCard(51, 7); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos8() { SpawnCard(51, 8); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos9() { SpawnCard(51, 9); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard51DeckPos10() { SpawnCard(51, 10); Deck.NumOfEachCardLeft[51]--; }
        public void NetworkSpawnCard52DeckPos0() { SpawnCard(52, 0); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos1() { SpawnCard(52, 1); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos2() { SpawnCard(52, 2); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos3() { SpawnCard(52, 3); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos4() { SpawnCard(52, 4); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos5() { SpawnCard(52, 5); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos6() { SpawnCard(52, 6); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos7() { SpawnCard(52, 7); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos8() { SpawnCard(52, 8); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos9() { SpawnCard(52, 9); Deck.NumOfEachCardLeft[52]--; }
        public void NetworkSpawnCard52DeckPos10() { SpawnCard(52, 10); Deck.NumOfEachCardLeft[52]--; }
        
        /*
         * for(int i = 0; i < 53; i++)
            {
                for(int j = 0; j < 11; j++)
                {
                    Console.WriteLine("        public void NetworkSpawnCard" + i + "DeckPos" + j + "() { SpawnCard(" + i + ", " + j + "); Deck.NumOfEachCardLeft[" + i + "]--; }");
                }
            }
         */

        #endregion

        public override void OnPreSerialization()
        {
            TotalDeckValue = localDeckValue;
            
            if (VRCPlayerApi.GetPlayerCount() < 2)
            {
                DealerValueDisplay.text = localDeckValue.ToString();
            }
            else
            {
                DealerValueDisplay.text = TotalDeckValue.ToString();
            }
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) return;

            localDeckValue = TotalDeckValue;

            if (localDeckValue > 21)
            {
                IsBust = true;
            }
            
            if (VRCPlayerApi.GetPlayerCount() < 2)
            {
                DealerValueDisplay.text = localDeckValue.ToString();
            }
            else
            {
                DealerValueDisplay.text = TotalDeckValue.ToString();
            }
        }

        public void Reset()
        {
            for (int i = 0; i < DeckAnimators.Length; i++)
            {
                if (DeckAnimators[i] != null)
                {
                    Destroy(DeckAnimators[i].gameObject);
                }
            }

            IsBust = false;

            for (int i = 0; i < DeckValue.Length; i++)
            {
                DeckValue[i] = 0;
            }

            NextDeckPosition = 0;
            localDeckValue = 0;
            NumOfAces = 0;

            if (Networking.IsOwner(gameObject))
            {
                TotalDeckValue = 0;
                localDeckValue = 0;
                RequestSerialization();
            }
            
            if (VRCPlayerApi.GetPlayerCount() < 2)
            {
                DealerValueDisplay.text = localDeckValue.ToString();
            }
            else
            {
                DealerValueDisplay.text = TotalDeckValue.ToString();
            }
        }
    }
}
