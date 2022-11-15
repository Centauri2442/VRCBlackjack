
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Random = UnityEngine.Random;

namespace CentauriCore.Blackjack
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(Collider))]
    public class AudioController : UdonSharpBehaviour
    {
        public bool isActive;
        [Space]
        public AudioSource Audio;
        [Space] 
        public AudioClip StartMatch;
        public AudioClip StartActivePlay;
        public AudioClip[] DrawCard;
        public AudioClip SplitDeck;
        public AudioClip Stand;
        public AudioClip WinMatch;
        public AudioClip LoseMatch;
        public AudioClip DrawMatch;
        public AudioClip DealerWantToHit;
        public AudioClip Bust;
        public AudioClip TakeControl;
        public AudioClip ReleaseControl;
        public AudioClip[] FlipCard;
        public AudioClip UIInteract;

        private Transform AudioTransform;
        private Collider col;


        private void Start()
        {
            AudioTransform = Audio.transform;
            col = GetComponent<Collider>();
        }

        private void Update()
        {
            if (!isActive) return;
            
            AudioTransform.position = col.ClosestPoint(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
        }

        public void PlayStartMatch()
        {
            if (!isActive) return;
            
            Audio.PlayOneShot(StartMatch);
        }

        public void PlayStartActivePlay()
        {
            if (!isActive) return;
            Audio.PlayOneShot(StartActivePlay);
        }
        
        public void PlayDrawCard()
        {
            if (!isActive) return;
            Audio.PlayOneShot(DrawCard[Random.Range(0, DrawCard.Length)]);
        }

        public void PlaySplitDeck()
        {
            if (!isActive) return;
            Audio.PlayOneShot(SplitDeck);
        }

        public void PlayStand()
        {
            if (!isActive) return;
            Audio.PlayOneShot(Stand);
        }

        public void PlayWinMatch()
        {
            if (!isActive) return;
            Audio.PlayOneShot(WinMatch);
        }

        public void PlayLoseMatch()
        {
            if (!isActive) return;
            Audio.PlayOneShot(LoseMatch);
        }

        public void PlayDrawMatch()
        {
            if (!isActive) return;
            Audio.PlayOneShot(DrawMatch);
        }

        public void PlayDealerWantToHit()
        {
            if (!isActive) return;
            Audio.PlayOneShot(DealerWantToHit);
        }

        public void PlayBust()
        {
            if (!isActive) return;
            Audio.PlayOneShot(Bust);
        }

        public void PlayTakeControl()
        {
            if (!isActive) return;
            Audio.PlayOneShot(TakeControl);
        }

        public void PlayReleaseControl()
        {
            if (!isActive) return;
            Audio.PlayOneShot(ReleaseControl);
        }

        public void PlayFlipCard()
        {
            if (!isActive) return;
            Audio.PlayOneShot(FlipCard[Random.Range(0, FlipCard.Length)]);
        }

        public void PlayUIInteract()
        {
            Audio.PlayOneShot(UIInteract);
        }
    }
}
