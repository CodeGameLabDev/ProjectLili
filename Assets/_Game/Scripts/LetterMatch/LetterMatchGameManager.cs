using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LetterMatch
{
    public class LetterMatchGameManager : Singleton<LetterMatchGameManager>
    {
        [TabGroup("References")]
        public LetterMatchSpawner letterSpawner;
        public KoreographerAudioManager audioManager;
        public DrawingController drawingController;
    
        [TabGroup("Settings")]
        public string currentLevel = "ABC";
        public float musicBPM = 120f;
        public float syncThreshold = 0.1f;
    
        [TabGroup("Debug"), ReadOnly]
        public bool isGameActive = false;
        public bool isMusicPlaying = false;
        public float currentMusicTime = 0f;
        public List<LetterMatchData> matchedLetters = new List<LetterMatchData>();
    
        private Coroutine musicSyncCoroutine;
        public LetterMatchLevelConfig currentLevelConfig;

        void Start()
        {
            InitializeGame();
        }
    
        void InitializeGame()
        {
            if (letterSpawner == null || audioManager == null || drawingController == null)
            {
                Debug.LogError("LetterMatchGameManager: Missing required references!");
                return;
            }
        
            SetupLevel(currentLevel);
        }
    
        [Button("Setup Level", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        public void SetupLevel(string levelId)
        {
            currentLevel = levelId;
            ClearLevel();
        
            letterSpawner.SpawnLetters(currentLevel);

            // Setup audio channels for this level
            audioManager.SetupChannels(letterSpawner.currentLevelConfig.letters);
        
            isGameActive = true;
            Debug.Log($"Level {levelId} setup complete!");
        }
    
        [Button("Start Music Sync", ButtonSizes.Medium), GUIColor(0.8f, 1f, 0.4f)]
        public void StartMusicSync()
        {
            if (musicSyncCoroutine != null)
                StopCoroutine(musicSyncCoroutine);
            
            musicSyncCoroutine = StartCoroutine(MusicSyncRoutine());
            isMusicPlaying = true;
        }
    
        [Button("Stop Music", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        public void StopMusic()
        {
            if (musicSyncCoroutine != null)
                StopCoroutine(musicSyncCoroutine);
            
            audioManager.StopMusic();
            isMusicPlaying = false;
            isGameActive = false;
        }
    
        IEnumerator MusicSyncRoutine()
        {
            audioManager.PlayFullMusic();
        
            while (isMusicPlaying && isGameActive)
            {
            
                // Check if all letters are matched
                if (CheckAllLettersMatched())
                {
                    OnLevelComplete();
                    break;
                }
            
                yield return new WaitForSeconds(0.016f); // ~60 FPS sync
            }
        }

    
        void TriggerKickAnimation(MusicMarker marker)
        {
            // Find corresponding letter and trigger kick animation
            var letter = letterSpawner.GetLetterByChannel(marker.channelId);
            if (letter != null)
            {
                letter.PlayKickAnimation();
            }
        }
    
        void TriggerPianoAnimation(MusicMarker marker)
        {
            var letter = letterSpawner.GetLetterByChannel(marker.channelId);
            if (letter != null)
            {
                letter.PlayPianoAnimation();
            }
        }
    
        void TriggerOtherAnimation(MusicMarker marker)
        {
            var letter = letterSpawner.GetLetterByChannel(marker.channelId);
            if (letter != null)
            {
                letter.PlayOtherAnimation();
            }
        }
    
        public void OnLetterMatched(LetterMatchData letterData)
        {
            if (!matchedLetters.Contains(letterData))
            {
                matchedLetters.Add(letterData);
                Debug.Log($"Letter pair {letterData.capitalLetter}-{letterData.smallLetter} matched! Total: {matchedLetters.Count}/{letterSpawner.GetTotalLetterPairs()} pairs");
            
                // Don't play individual channel music - wait for all pairs to be matched
                // musicController.PlayChannel(letterData.channelId);
            
                // Check if all letters are matched and trigger completion
                if (CheckAllLettersMatched())
                {
                    OnLevelComplete();
                }
            }
        }
    
        bool CheckAllLettersMatched()
        {
            return matchedLetters.Count >= letterSpawner.GetTotalLetterPairs();
        }
    
        void OnLevelComplete()
        {
            Debug.Log("Level Complete! All letter pairs matched! Playing full music with animations!");
            isGameActive = false;
            isMusicPlaying = false;
        
            // Stop any current music
            audioManager.StopMusic();
        
            // Play full music with all channels
            audioManager.PlayFullMusic();
        
            // Trigger completion animations
            StartCoroutine(LevelCompleteSequence());
        }
    
        IEnumerator LevelCompleteSequence()
        {
            // Wait for music to start
            yield return new WaitForSeconds(0.5f);
        
            // Trigger completion animations for all letters
            foreach (var letter in letterSpawner.GetAllLetters())
            {
                letter.PlayCompletionAnimation();
                yield return new WaitForSeconds(0.2f);
            }
        }
    
        void ClearLevel()
        {
            matchedLetters.Clear();
            isGameActive = false;
            isMusicPlaying = false;
            currentMusicTime = 0f;
        }
    
        public bool IsGameActive() => isGameActive;
        public bool IsMusicPlaying() => isMusicPlaying;
  
    }
} 