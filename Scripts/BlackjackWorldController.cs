
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BlackjackWorldController : UdonSharpBehaviour
    {
        public GameObject TablePrefab;
        public ChipController ChipController;
        public Controller[] BlackjackTables;
        public GameObject[] TableGameObjects;
        public Material[] TableCameraMaterials;
        public Texture[] TableRenderTextures;


        private void Start() // Ensures that if chip system is not active, tables will not be allowed to use it
        {
            for (int i = 0; i < BlackjackTables.Length; i++)
            {
                BlackjackTables[i].TableIndex = i;

                if (!ChipController.UseChipSystem)
                {
                    BlackjackTables[i].UsingChips = false;
                }
            }
        }
    }
}
