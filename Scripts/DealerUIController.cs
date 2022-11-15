
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DealerUIController : UdonSharpBehaviour
    {
        public bool Running;
        public Controller BlackjackController;
        public Dealer DealerController;
        private DeckManager Deck;
        [Space]
        #region UI Stuff

        public GameObject AutoCanvas;
        public Animator AutoCanvasAnimator;
        public Button[] AutoCanvasButtons;
        
        public GameObject OwnerUI;
        public GameObject ManualOwnerUI;
        public GameObject ManualOwnerSettings;
        public GameObject ManualNotOwnerUI;
        public GameObject OwnerSlider;
        public Slider UIHeightSlider;
        public Animator UIHeightAnimator;
        public TextMeshProUGUI DeckCount;
        public TextMeshProUGUI DeckCountManual;
        [Space]
        public GameObject NotOwnerUI;
        [Space]
        public Animator TableUIAnimator;
        [Space] 
        public GameObject ChipToggle;
        public GameObject ChipOn;
        public GameObject ChipOff;
        
        public GameObject DeckChanger;
        public GameObject DeckSubtractArrow;
        
        public GameObject HelpersChanger;
        public GameObject HelpersOn;
        public GameObject HelpersOff;
        [Space]
        public GameObject ChipToggleManual;
        public GameObject ChipOnManual;
        public GameObject ChipOffManual;
        
        public GameObject DeckChangerManual;
        public GameObject DeckSubtractArrowManual;
        
        public GameObject HelpersChangerManual;
        public GameObject HelpersOnManual;
        public GameObject HelpersOffManual;
        [Space]
        public GameObject DealerValuesCanvas;
        public TextMeshProUGUI DealerValues;

        #endregion

        private bool isOwner;
        private bool autoOff;
        private bool autoFullyOff;


        private void Start()
        {
            BlackjackController = DealerController.GameController;
            Deck = BlackjackController.Deck;
            
            SendCustomEventDelayedFrames(nameof(DelayedStart), 20);
        }

        public void DelayedStart() // Delayed start helps ensure proper script initialization before they are disabled.
        {
            ChipToggle.SetActive(BlackjackController.CanChangeSettings && BlackjackController.UsingChips);
            DeckChanger.SetActive(BlackjackController.CanChangeSettings);
            HelpersChanger.SetActive(BlackjackController.CanChangeSettings);
            
            ChipToggleManual.SetActive(ChipToggle.activeSelf);
            DeckChangerManual.SetActive(DeckChanger.activeSelf);
            HelpersChangerManual.SetActive(HelpersChanger.activeSelf);

            if (ChipToggle.activeSelf)
            {
                if (BlackjackController.UsingChips)
                {
                    ChipOff.SetActive(false);
                    ChipOn.SetActive(true);
                    
                    ChipOffManual.SetActive(false);
                    ChipOnManual.SetActive(true);
                }
                else
                {
                    ChipOff.SetActive(true);
                    ChipOn.SetActive(false);
                    
                    ChipOffManual.SetActive(true);
                    ChipOnManual.SetActive(false);
                }
            }
            
            if (HelpersChanger.activeSelf)
            {
                if (BlackjackController.HelpersEnabled)
                {
                    HelpersOff.SetActive(false);
                    HelpersOn.SetActive(true);
                    
                    HelpersOffManual.SetActive(false);
                    HelpersOnManual.SetActive(true);
                }
                else
                {
                    HelpersOff.SetActive(true);
                    HelpersOn.SetActive(false);
                    
                    HelpersOffManual.SetActive(true);
                    HelpersOnManual.SetActive(false);
                }
            }
        }

        public void Update()
        {
            isOwner = Networking.IsOwner(BlackjackController.gameObject);

            if (Running)
            {
                #region UI Stuff

            var gameState = BlackjackController.localGameActive;
            ManualOwnerUI.SetActive(isOwner && BlackjackController.ManualPlay && !gameState);
            OwnerSlider.SetActive(isOwner && BlackjackController.ManualPlay && gameState);
            
            DeckSubtractArrow.SetActive(BlackjackController.Deck.NumberOfDecks > 2);
            DeckSubtractArrowManual.SetActive(DeckSubtractArrow.activeSelf);
            
            NotOwnerUI.SetActive(!isOwner && !gameState);
            ManualNotOwnerUI.SetActive(!isOwner && !gameState && BlackjackController.ManualPlay);

            DeckCount.text = Deck.NumberOfDecks.ToString();
            DeckCountManual.text = DeckCount.text;
            ManualOwnerSettings.SetActive(ManualOwnerUI.activeSelf);

            
            var dealerVal = DealerController.GetDeckValue();
            DealerValuesCanvas.SetActive(!BlackjackController.localGameActive && dealerVal > 0);
            DealerValues.text = dealerVal.ToString();
            

            if (BlackjackController.ManualPlay)
            {
                if (!autoOff)
                {
                    autoOff = true;
                    OwnerUI.SetActive(true);
                    SendCustomEventDelayedSeconds(nameof(SetLayerToMirrorReflection), 0.75f);
                }
                
                if (isOwner)
                {
                    AutoCanvasAnimator.SetBool("Active", false);
                }
                else
                {
                    AutoCanvas.layer = 18;
                    autoFullyOff = true;
                }

                if (autoFullyOff)
                {
                    OwnerUI.SetActive(isOwner && !BlackjackController.ManualPlay && !gameState);
                }

                ChipOff.SetActive(!ChipOffManual.activeSelf);
                ChipOn.SetActive(!ChipOnManual.activeSelf);
                
                HelpersOff.SetActive(!HelpersOffManual.activeSelf);
                HelpersOn.SetActive(!HelpersOnManual.activeSelf);
            }
            else
            {
                autoFullyOff = false;
                autoOff = false;
                AutoCanvas.layer = 0;
                AutoCanvasAnimator.SetBool("Active", true);
                OwnerUI.SetActive(isOwner && !BlackjackController.ManualPlay && !gameState);
                
                ChipOffManual.SetActive(!ChipOff.activeSelf);
                ChipOnManual.SetActive(!ChipOn.activeSelf);
                
                HelpersOffManual.SetActive(!HelpersOff.activeSelf);
                HelpersOnManual.SetActive(!HelpersOn.activeSelf);
            }
            
            var sliderVal = UIHeightSlider.value;

            if (sliderVal < 0.99f)
            {
                UIHeightAnimator.SetFloat("Height", sliderVal);
            }
            else
            {
                UIHeightAnimator.SetFloat("Height", 0.99f);
            }

            foreach (var button in AutoCanvasButtons)
            {
                button.interactable = !autoOff;
            }

            #endregion
            }
            else
            {
                ManualOwnerUI.SetActive(false);
                OwnerSlider.SetActive(false);
            
                DeckSubtractArrow.SetActive(false);
                DeckSubtractArrowManual.SetActive(false);
            
                NotOwnerUI.SetActive(false);
                ManualNotOwnerUI.SetActive(false);

                ManualOwnerSettings.SetActive(false);

                DealerValuesCanvas.SetActive(false);
            }
        }

        public void SetLayerToMirrorReflection() // Used for delayed event to disable auto canvas
        {
            AutoCanvas.layer = 18;
            autoFullyOff = true;
        }

        public void ResetTableUI()
        {
            TableUIAnimator.SetTrigger("Reset");
        }

        public void Bust()
        {
            TableUIAnimator.ResetTrigger("Reset");
            TableUIAnimator.SetTrigger("Bust");
        }
    }
}
