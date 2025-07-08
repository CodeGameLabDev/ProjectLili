using UnityEngine;
using SonicBloom.Koreo;
using SonicBloom.Koreo.Players;

namespace LetterMatch
{
    public class KoreographerSetup : MonoBehaviour
    {
        [Header("Koreographer Configuration")]
        public Koreography koreography;
        public SimpleMusicPlayer musicPlayer;
        
        [Header("Beat Track Configuration")]
        public string beatEventID = "Beat";
        public float beatInterval = 0.5f; // Default 120 BPM
        
        [Header("Setup Instructions")]
        [TextArea(5, 10)]
        public string setupInstructions = 
            "1. Create a Koreography asset for your music\n" + 
            "2. Add a Custom Event Track with EventID = 'Beat'\n" +
            "3. Place beat markers at appropriate times\n" +
            "4. Assign the Koreography to this component\n" +
            "5. Ensure Koreographer singleton is in the scene";
        
        void Start()
        {
            SetupKoreographer();
        }
        
        void SetupKoreographer()
        {
            // Ensure Koreographer singleton exists
            if (Koreographer.Instance == null)
            {
                Debug.LogError("KoreographerSetup: No Koreographer singleton found in scene!");
                Debug.Log("Please add the Koreographer prefab to your scene.");
                return;
            }
            
            // Setup music player if provided
            if (musicPlayer != null && koreography != null)
            {
                musicPlayer.LoadSong(koreography, 0, false);
                Debug.Log("KoreographerSetup: Music player configured with Koreography");
            }
            
            // Validate beat track exists
            if (koreography != null)
            {
                var beatTrack = koreography.GetTrackByID(beatEventID);
                if (beatTrack == null)
                {
                    Debug.LogWarning($"KoreographerSetup: No track found with EventID '{beatEventID}'");
                    Debug.Log("Please create a Custom Event Track with this EventID for beat detection");
                }
                else
                {
                    Debug.Log($"KoreographerSetup: Beat track '{beatEventID}' found with {beatTrack.GetAllEvents().Count} events");
                }
            }
            
            Debug.Log("KoreographerSetup: Setup complete");
        }
        
        [ContextMenu("Create Beat Track")]
        void CreateBeatTrack()
        {
            if (koreography == null)
            {
                Debug.LogError("No Koreography assigned!");
                return;
            }
            
            // Create a new Custom Event Track
            var beatTrack = new KoreographyTrack();
            beatTrack.EventID = beatEventID;
            
            // Add the track to the Koreography
            koreography.AddTrack(beatTrack);
            
            Debug.Log($"Created beat track with EventID: {beatEventID}");
        }
        
        [ContextMenu("Generate Beat Markers")]
        void GenerateBeatMarkers()
        {
            if (koreography == null)
            {
                Debug.LogError("No Koreography assigned!");
                return;
            }
            
            var beatTrack = koreography.GetTrackByID(beatEventID);
            if (beatTrack == null)
            {
                Debug.LogError($"No beat track found with EventID: {beatEventID}");
                return;
            }
            
            beatTrack.RemoveAllEvents();
            
            // Generate beat markers based on BPM
            float songLength = koreography.SourceClip.length;
            float currentTime = 0f;
            
            while (currentTime < songLength)
            {
                var beatEvent = new KoreographyEvent();
                beatEvent.StartSample = (int)(currentTime * koreography.SampleRate);
                beatEvent.EndSample = beatEvent.StartSample + 1;
                
                beatTrack.AddEvent(beatEvent);
                currentTime += beatInterval;
            }
            
            Debug.Log($"Generated {beatTrack.GetAllEvents().Count} beat markers");
        }
        
        public Koreography GetKoreography()
        {
            return koreography;
        }
        
        public string GetBeatEventID()
        {
            return beatEventID;
        }
    }
} 