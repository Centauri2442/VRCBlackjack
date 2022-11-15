
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ChipController : UdonSharpBehaviour
    {
        public bool UseChipSystem;
        public Controller[] BlackjackTables;
        [Space(20)]
        public int CurrentChipCount;
        public int StartingChips = 100;
        public int[] CurrentBet;
        [Space(20)]
        public AudioSource Audio;
        public AudioClip NotEnoughChipsClip;
        public AudioClip AddToBetClip;

        [HideInInspector] public bool isInGame;


        private void Start()
        {
            CurrentChipCount = StartingChips;
            CurrentBet = new int[BlackjackTables.Length];
        }

        private void Update()
        {
            Audio.transform.position = Networking.LocalPlayer.GetPosition();
        }

        public void AddChips(int value)
        {
            CurrentChipCount += value;
            Debug.Log($"Adding {value} chips!");
        }

        public void RemoveChips(int value)
        {
            CurrentChipCount -= value;
            Debug.Log($"Removing {value} chips!");
        }

        public void ResetChips()
        {
            CurrentChipCount = StartingChips;

            for (int i = 0; i < CurrentBet.Length; i++)
            {
                CurrentBet[i] = 0;
            }
        }

        public void AddToBet(int tableIndex,int value)
        {
            var totalBet = 0;

            foreach (var bet in CurrentBet)
            {
                totalBet += bet;
            }
            
            if (totalBet + value > CurrentChipCount)
            {
                NotEnoughChips();
                return;
            }

            CurrentBet[tableIndex] += value;
            
            Debug.Log($"Adding {value} chips to bet!");

            if (!Audio.isPlaying)
            {
                Audio.PlayOneShot(AddToBetClip);
            }
        }
        
        public void TakeFromBet(int tableIndex,int value)
        {
            if (CurrentBet[tableIndex] - value < 0)
            {
                NotEnoughChips();
            }
            
            Debug.Log($"Removing {value} chips from bet!");
            
            CurrentBet[tableIndex] -= value;
            Audio.PlayOneShot(AddToBetClip);
        }

        public void ResetBet(int tableIndex)
        {
            CurrentBet[tableIndex] = 0;
        }

        public void NotEnoughChips()
        {
            if (Audio.isPlaying) return;
            
            Debug.Log("Not enough chips!");
            
            Audio.PlayOneShot(NotEnoughChipsClip);
        }
    }
}
