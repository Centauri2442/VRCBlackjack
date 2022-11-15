
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Controller : UdonSharpBehaviour
    {
        [UdonSynced] public bool UsingChips;
        public ChipController ChipController;
        [Space]
        [UdonSynced] public bool GameActive;
        [HideInInspector] public bool localGameActive;
        [UdonSynced] public bool ManualPlay = false;
        [Space] 
        public AudioController AudioController;
        [Space] 
        public Dealer Dealer;
        public Player[] Players;
        [SerializeField] private Player[] ActivePlayers;
        public DeckManager Deck;
        [Space] 
        [UdonSynced] public bool HelpersEnabled;

        [HideInInspector] public bool CanChangeSettings;
        [HideInInspector] public int TableIndex;
        public Camera Camera;
        public Renderer CameraRenderer;
        [HideInInspector] public Material RendererMaterial;
        [HideInInspector] public Texture RenderTexture;

        private VRCPlayerApi _localPlayer;


        public void _TakeOwnership()
        {
            Networking.SetOwner(_localPlayer, gameObject);
            Networking.SetOwner(_localPlayer, Dealer.gameObject);
            Networking.SetOwner(_localPlayer, Deck.gameObject);
        }

        public void _ToggleManualPlay()
        {
            ManualPlay = !ManualPlay;
            RequestSerialization();
        }

        public void _ActivateHelpers()
        {
            HelpersEnabled = true;
            RequestSerialization();
        }
        
        public void _DeactivateHelpers()
        {
            HelpersEnabled = false;
            RequestSerialization();
        }

        public void _ActivateChips()
        {
            if (ChipController == null || !ChipController.UseChipSystem) return;

            UsingChips = true;
            RequestSerialization();
        }
        
        public void _DeactivateChips()
        {
            if (ChipController == null || !ChipController.UseChipSystem) return;

            UsingChips = false;
            RequestSerialization();
        }

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;

            for (int i = 0; i < Players.Length; i++)
            {
                Players[i].PlayerNum = i;
            }
        }

        public void GlobalStartMatch()
        {
            bool canStart = false;

            foreach (var player in Players)
            {
                if (player.IsActive)
                {
                    canStart = true;
                }
            }

            if (!canStart) return;
            
            if (!GameActive)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetDelayStart));
            }
        }

        public void ResetEverything()
        {
            Dealer.ResetEverything();

            foreach (var player in Players)
            {
                player.ResetPlayer();
            }
        }

        public void ResetDelayStart()
        {
            if (localGameActive || GameActive) return;
            localGameActive = true;
            
            Debug.Log("Resetting Table for Match Start!");
            
            ResetEverything();
            SendCustomEventDelayedFrames(nameof(StartMatch), 5);
        }

        public void StartMatch()
        {
            Deck.InitializeDeck();

            AudioController.PlayStartMatch();
            
            if (!Networking.IsOwner(gameObject))
            {
                var count = 0;
                for (int i = 0; i < Players.Length; i++)
                {
                    if (Players[i].IsActive)
                    {
                        count++;
                    }
                }

                if (count < 1)
                {
                    ActivePlayers = null;
                    return;
                }
                
                for (int i = 0; i < Players.Length; i++)
                {
                    if (!Players[i].IsActive)
                    {
                        Players[i].LockPlayer();
                    }
                }
                return;
            }
            
            GameActive = true;

            ActivePlayers = new Player[Players.Length];

            var counter = 0;
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i].IsActive)
                {
                    ActivePlayers[counter] = Players[i];
                    counter++;
                }
                else
                {
                    Players[i].LockPlayer();
                }
            }

            if (counter < 1)
            {
                ActivePlayers = null;
                return;
            }

            ActivePlayers = new Player[counter];
            
            counter = 0;
            
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i].IsActive)
                {
                    ActivePlayers[counter] = Players[i];
                    counter++;
                }
                
                Players[i].PlayerUI.DealerUIAnimator.CrossFadeInFixedTime("Active.PlayerDealerIdleOff", 0f);
            }
            
            for (int i = 0; i < ActivePlayers.Length; i++)
            {
                ActivePlayers[i].SendCustomEventDelayedSeconds("GlobalStartFirstCard", (i+1)/2f);
                ActivePlayers[i].SendCustomEventDelayedSeconds("GlobalStartSecondCard", (i + 2 + ActivePlayers.Length)/2f);

            }
            
            Dealer.SendCustomEventDelayedSeconds("GlobalStartFirstCard", (ActivePlayers.Length+1)/2f);
            Dealer.SendCustomEventDelayedSeconds("GlobalStartSecondCard", (ActivePlayers.Length+1));
            
            SendCustomEventDelayedSeconds(nameof(StartActivePlay), (ActivePlayers.Length) + 2f);
            SendCustomEventDelayedSeconds(nameof(GameActiveRefresh), (ActivePlayers.Length) + 2f);
        }

        public void StartActivePlay()
        {
            foreach (var player in ActivePlayers)
            {
                player.SendCustomNetworkEvent(NetworkEventTarget.All, "StartTurn");
            }
        }

        public void GameActiveRefresh()
        {
            if (!GameActive) return;
            
            var isReady = true;

            foreach (var player in ActivePlayers)
            {
                if (!player.HasFinishedTurn)
                {
                    isReady = false;
                }
            }

            if (!isReady)
            {
                SendCustomEventDelayedSeconds(nameof(GameActiveRefresh), 2f);
                return;
            }
            
            Dealer.StartDealerPlay();
        }

        public void FinishMatch()
        {
            GameActive = false;
            RequestSerialization();
            Dealer.RequestSerialization();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GlobalFinishMatch));
        }

        public void ResetTable()
        {
            if (Networking.IsOwner(gameObject))
            {
                GameActive = false;
                RequestSerialization();
            }
            
            foreach (var player in Players)
            {
                player.ForceResetPlayer();
            }
        }

        private bool justReset = false;
        public void ForceGlobalReset()
        {
            if (justReset || !Networking.IsOwner(gameObject)) return;
            justReset = true;
            
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetTable));
            SendCustomEventDelayedSeconds(nameof(ResetReset), 2f);
        }

        public void ForceGlobalResetNoTimer()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetTable));
        }

        public void ResetReset()
        {
            justReset = false;
        }

        public void GlobalFinishMatch()
        {
            localGameActive = false;
            
            foreach (var player in Players)
            {
                player.SetWinState();
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ResetTable();
        }

        public override void OnPreSerialization()
        {
            localGameActive = GameActive;
        }

        public override void OnDeserialization()
        {
            localGameActive = GameActive;
        }

        public void ResetChips()
        {
            if (ChipController == null) return;
            ChipController.ResetChips();
        }
    }
}
