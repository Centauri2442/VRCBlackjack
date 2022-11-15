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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DeckManager : UdonSharpBehaviour
    {
        [Header("Deck Manager")]
        [Space(20)]
        [UdonSynced] public int NumberOfDecks = 2;
        public GameObject[] CardReferences = new GameObject[53];
        /*
         * 0 - Empty Card
         * 1/4 - 2
         * 5/8 - 3
         * 9/12 - 4
         * 13/16 - 5
         * 17/20 - 6
         * 21/24 - 7
         * 25/28 - 8
         * 19/32 - 9
         * 33/48 - 10
         * 49/52 - 11
         */
        public int[] NumOfEachCardLeft;


        private void Start()
        {
            InitializeDeck();
        }

        public void AddDeck()
        {
            NumberOfDecks++;
            RequestSerialization();
        }
        
        public void SubtractDeck()
        {
            if (NumberOfDecks <= 2f)
            {
                NumberOfDecks = 2;
                RequestSerialization();
                return;
            }
            
            NumberOfDecks--;
            RequestSerialization();
        }

        public void InitializeDeck()
        {
            NumOfEachCardLeft = new int[CardReferences.Length];

            for (int i = 0; i < NumOfEachCardLeft.Length; i++)
            {
                if (i == 0)
                {
                    NumOfEachCardLeft[i] = 999;
                }
                else
                {
                    NumOfEachCardLeft[i] = NumberOfDecks;
                }
            }
        }
    }
}
