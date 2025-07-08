using System;
using System.Collections.Generic;
using SonicBloom.Koreo;
using SonicBloom.Koreo.Players;
using UnityEngine;

namespace LetterMatch
{
    public class KoreographerAudioManager : MonoBehaviour
    {
        [Header("Debug")] public bool isMusicPlaying = false;

        [Header("Synchronization Settings")]
        public bool useSynchronizedPlayback = true;
        public float syncDelayAfterAllLines = 2f; // Delay after all lines are drawn before playing
        public bool autoPlayWhenAllLinesComplete = true;
        public bool syncToNextBeat = true; // Sync to the next beat when all lines are complete
        public float bpm = 120f; // Beats per minute for beat synchronization

        private Dictionary<string, SimpleMusicPlayer> channelMusicPlayers = new Dictionary<string, SimpleMusicPlayer>();
        private Dictionary<string, bool> completedLines = new Dictionary<string, bool>();
        private Dictionary<string, float> lineCompletionTimes = new Dictionary<string, float>();
        private Dictionary<string, float> lineStartTimes = new Dictionary<string, float>(); // Track when each line started
        private bool isSynchronizedPlaybackReady = false;
        private float synchronizedPlaybackStartTime = 0f;
        private Coroutine musicEndMonitorCoroutine;
        private float longestLineDrawingTime = 0f; // Track the longest line drawing time

        #region Unity Lifecycle

        void OnDestroy()
        {
            StopMusic();
        }

        #endregion

        #region Public Methods

        public bool IsMusicPlaying() => isMusicPlaying;

        public void SetupChannels(IEnumerable<LetterConfig> letterConfigs)
        {
            ClearExistingChannels();
            CreateChannelsFromConfigs(letterConfigs);
            InitializeSynchronization(letterConfigs);
        }

        public void PlayFullMusic()
        {
            if (useSynchronizedPlayback && !isSynchronizedPlaybackReady)
            {
                Debug.LogWarning("Synchronized playback not ready yet. Lines may not be complete.");
            }

            foreach (var musicPlayer in channelMusicPlayers.Values)
            {
                musicPlayer?.Play();
            }

            isMusicPlaying = true;
            synchronizedPlaybackStartTime = Time.time;
            
            // Start monitoring for when the longest track ends
            StartMusicEndMonitoring();
        }

        public void StopMusic()
        {
            Debug.Log($"StopMusic called. isMusicPlaying: {isMusicPlaying}");
            
            foreach (var kvp in channelMusicPlayers)
            {
                var musicPlayer = kvp.Value;
                if (musicPlayer != null)
                {
                    bool wasPlaying = musicPlayer.IsPlaying;
                    musicPlayer.Stop();
                    Debug.Log($"Stopped music player for channel {kvp.Key}. Was playing: {wasPlaying}");
                }
            }

            isMusicPlaying = false;
            
            // Stop the music end monitoring
            if (musicEndMonitorCoroutine != null)
            {
                StopCoroutine(musicEndMonitorCoroutine);
                musicEndMonitorCoroutine = null;
                Debug.Log("Music end monitoring coroutine stopped");
            }
            
            Debug.Log("StopMusic completed");
        }

        // Called when a line drawing starts
        public void OnLineStarted(string channelId)
        {
            if (!useSynchronizedPlayback) return;
            
            lineStartTimes[channelId] = Time.time;
            Debug.Log($"Line started for channel: {channelId}");
        }

        // Called when a line drawing is completed
        public void OnLineCompleted(string channelId)
        {
            if (!useSynchronizedPlayback) return;

            completedLines[channelId] = true;
            lineCompletionTimes[channelId] = Time.time;
            
            // Calculate the line drawing duration
            if (lineStartTimes.ContainsKey(channelId))
            {
                float lineDrawingDuration = Time.time - lineStartTimes[channelId];
                Debug.Log($"Line {channelId} completed. Start time: {lineStartTimes[channelId]:F2}, End time: {Time.time:F2}, Duration: {lineDrawingDuration:F2}s");
                
                if (lineDrawingDuration > longestLineDrawingTime)
                {
                    longestLineDrawingTime = lineDrawingDuration;
                    Debug.Log($"New longest line drawing time: {longestLineDrawingTime:F2}s (from {channelId})");
                }
                else
                {
                    Debug.Log($"Line {channelId} duration ({lineDrawingDuration:F2}s) is not longer than current longest ({longestLineDrawingTime:F2}s)");
                }
            }
            else
            {
                Debug.LogWarning($"No start time found for line {channelId}");
            }

            Debug.Log($"Line completed for channel: {channelId}. Total completed: {GetCompletedLineCount()}/{GetTotalLineCount()}. Longest line time: {longestLineDrawingTime:F2}s");

            // Check if all lines are complete
            if (autoPlayWhenAllLinesComplete && AreAllLinesComplete())
            {
                StartSynchronizedPlayback();
            }
        }

        // Manually trigger synchronized playback
        public void StartSynchronizedPlayback()
        {
            if (!useSynchronizedPlayback) return;

            if (!AreAllLinesComplete())
            {
                Debug.LogWarning("Not all lines are complete yet. Starting playback anyway.");
            }

            isSynchronizedPlaybackReady = true;
            
            if (syncToNextBeat)
            {
                // Calculate delay to next beat
                float beatDelay = CalculateDelayToNextBeat();
                StartCoroutine(DelayedSynchronizedPlayback(beatDelay));
            }
            else
            {
                // Use fixed delay
                StartCoroutine(DelayedSynchronizedPlayback(syncDelayAfterAllLines));
            }
        }

        private float CalculateDelayToNextBeat()
        {
            float beatDuration = 60f / bpm; // Duration of one beat in seconds
            float currentTime = Time.time;
            float timeSinceLastBeat = currentTime % beatDuration;
            float delayToNextBeat = beatDuration - timeSinceLastBeat;
            
            // Add a small buffer to ensure we're on the beat
            return delayToNextBeat + 0.1f;
        }

        private System.Collections.IEnumerator DelayedSynchronizedPlayback(float delay)
        {
            Debug.Log($"Waiting {delay:F2} seconds before synchronized playback...");
            yield return new WaitForSeconds(delay);
            
            Debug.Log("Starting synchronized playback of all channels!");
            PlayFullMusic();
        }

        private void StartMusicEndMonitoring()
        {
            if (musicEndMonitorCoroutine != null)
            {
                StopCoroutine(musicEndMonitorCoroutine);
            }
            
            Debug.Log($"Starting music end monitoring. Longest line drawing time: {longestLineDrawingTime:F2} seconds");
            musicEndMonitorCoroutine = StartCoroutine(MonitorMusicEnd());
        }
        
        private System.Collections.IEnumerator MonitorMusicEnd()
        {
            // Use the longest line drawing time instead of music file duration
            float musicDuration = longestLineDrawingTime;
            
            if (musicDuration <= 0f)
            {
                Debug.LogWarning("Could not determine longest line drawing time. Using fallback duration of 30 seconds.");
                musicDuration = 30f;
            }
            
            Debug.Log($"Monitoring music end. Longest line drawing time: {musicDuration:F2} seconds");
            
            // Wait for the duration of the longest line drawing
            yield return new WaitForSeconds(musicDuration);
            
            // Stop all music
            Debug.Log("Longest line drawing time reached. Stopping all music.");
            StopMusic();
        }

        #endregion

        #region Private Methods

        private void ClearExistingChannels()
        {
            foreach (var musicPlayer in channelMusicPlayers.Values)
            {
                if (musicPlayer != null)
                {
                    musicPlayer.Stop();
                    Destroy(musicPlayer.gameObject);
                }
            }

            channelMusicPlayers.Clear();
            ResetSynchronization();
        }

        private void ResetSynchronization()
        {
            completedLines.Clear();
            lineCompletionTimes.Clear();
            lineStartTimes.Clear();
            isSynchronizedPlaybackReady = false;
            synchronizedPlaybackStartTime = 0f;
        }

        private void InitializeSynchronization(IEnumerable<LetterConfig> letterConfigs)
        {
            ResetSynchronization();
            
            foreach (var letterConfig in letterConfigs)
            {
                if (IsValidLetterConfig(letterConfig))
                {
                    completedLines[letterConfig.channelId] = false;
                    lineCompletionTimes[letterConfig.channelId] = 0f;
                    lineStartTimes[letterConfig.channelId] = 0f;
                }
            }
        }

        private void CreateChannelsFromConfigs(IEnumerable<LetterConfig> letterConfigs)
        {
            foreach (var letterConfig in letterConfigs)
            {
                if (IsValidLetterConfig(letterConfig) && !IsChannelAlreadyRegistered(letterConfig.channelId))
                {
                    CreateMusicPlayerChannel(letterConfig);
                }
            }
        }

        private bool IsValidLetterConfig(LetterConfig letterConfig)
        {
            return !string.IsNullOrEmpty(letterConfig.channelId)
                   && letterConfig.koreography != null;
        }

        private bool IsChannelAlreadyRegistered(string channelId)
        {
            return channelMusicPlayers.ContainsKey(channelId);
        }

        private void CreateMusicPlayerChannel(LetterConfig letterConfig)
        {
            var playerObject = new GameObject($"MusicPlayer_{letterConfig.channelId}");
            playerObject.transform.SetParent(transform);

            var musicPlayer = playerObject.AddComponent<SimpleMusicPlayer>();

            musicPlayer.LoadSong(letterConfig.koreography, autoPlay: false);

            channelMusicPlayers[letterConfig.channelId] = musicPlayer;

            Koreographer.Instance.RegisterForEvents(letterConfig.channelId,
                (evt => OnBeat(letterConfig.channelId, evt)));

            Debug.Log($"Created SimpleMusicPlayer for channel: {letterConfig.channelId}");
        }

        private void OnBeat(string channelId, KoreographyEvent koreoEvent)
        {
            Debug.Log($"[SimpleMusicPlayer] Beat event for channel {channelId}");

            // If synchronized playback is enabled and music is not playing, only allow hit point spawning during drawing
            if (useSynchronizedPlayback && !isMusicPlaying)
            {
                // Allow hit point spawning during drawing, but don't animate existing hit points
                if (DrawingController.Instance.IsDrawing())
                {
                    DrawingController.Instance.SpawnMusicCircleOnLine(channelId);
                }
                return;
            }

            if (!IsMusicPlaying() && DrawingController.Instance.IsDrawing())
            {
                DrawingController.Instance.SpawnMusicCircleOnLine(channelId);
            }
            else
            {
                // Get the current music time from the music player for this channel
                float currentMusicTime = GetCurrentMusicTimeForChannel(channelId);
                if (currentMusicTime > 0f)
                {
                    // Animate only hit points that should be active at the current time for this specific channel
                    DrawingController.Instance.AnimateHitPointsAtTime(currentMusicTime, channelId);
                }
                else
                {
                    // Fallback to the old method if we can't get the current time
                    DrawingController.Instance.AnimateHitPointsForChannel(channelId);
                }
            }
        }
        
        public void AnimateHitPointsDuringPlayback(float currentMusicTime)
        {
            DrawingController.Instance.AnimateHitPointsAtTime(currentMusicTime);
        }

        private bool AreAllLinesComplete()
        {
            return GetCompletedLineCount() >= GetTotalLineCount();
        }

        private int GetCompletedLineCount()
        {
            int count = 0;
            foreach (var completed in completedLines.Values)
            {
                if (completed) count++;
            }
            return count;
        }

        private int GetTotalLineCount()
        {
            return completedLines.Count;
        }

        #endregion

        #region Public Utility Methods

        public SimpleMusicPlayer GetMusicPlayer(string channelId)
        {
            return channelMusicPlayers.TryGetValue(channelId, out var player) ? player : null;
        }
        
        public float GetCurrentMusicTimeForChannel(string channelId)
        {
            var musicPlayer = GetMusicPlayer(channelId);
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                int currentSampleTime = musicPlayer.GetSampleTimeForClip(musicPlayer.GetCurrentClipName());
                return currentSampleTime / 44100f; // Convert to seconds (assuming 44.1kHz sample rate)
            }
            return 0f;
        }

        public float GetTotalMusicDurationForChannel(string channelId)
        {
            var musicPlayer = GetMusicPlayer(channelId);
            if (musicPlayer != null)
            {
                int totalSampleTime = musicPlayer.GetTotalSampleTimeForClip(musicPlayer.GetCurrentClipName());
                return totalSampleTime / 44100f; // Convert to seconds (assuming 44.1kHz sample rate)
            }
            return 0f;
        }

        public float GetLongestMusicDuration()
        {
            float longestDuration = 0f;
            foreach (var channelId in channelMusicPlayers.Keys)
            {
                float duration = GetTotalMusicDurationForChannel(channelId);
                if (duration > longestDuration)
                {
                    longestDuration = duration;
                }
            }
            return longestDuration;
        }

        public bool IsLineCompleted(string channelId)
        {
            return completedLines.TryGetValue(channelId, out bool completed) && completed;
        }

        public float GetLineCompletionTime(string channelId)
        {
            return lineCompletionTimes.TryGetValue(channelId, out float time) ? time : 0f;
        }

        public bool IsSynchronizedPlaybackReady() => isSynchronizedPlaybackReady;

        public float GetSynchronizedPlaybackStartTime() => synchronizedPlaybackStartTime;
        
        public float GetLongestLineDrawingTime() => longestLineDrawingTime;
        
        // Force stop music (for debugging)
        public void ForceStopMusic()
        {
            Debug.Log("Force stopping music");
            StopMusic();
        }

        // Get synchronization status for UI
        public string GetSynchronizationStatus()
        {
            if (!useSynchronizedPlayback)
                return "Synchronized playback disabled";

            int completed = GetCompletedLineCount();
            int total = GetTotalLineCount();
            
            if (completed == 0)
                return $"Waiting for lines... (0/{total})";
            else if (completed < total)
                return $"Lines completed: {completed}/{total}";
            else if (isSynchronizedPlaybackReady && !isMusicPlaying)
                return "Ready to play! Click to start.";
            else if (isMusicPlaying)
                return "Playing synchronized music!";
            else
                return "All lines complete!";
        }

        // Get list of all channels and their completion status
        public Dictionary<string, bool> GetAllLineStatus()
        {
            return new Dictionary<string, bool>(completedLines);
        }

        // Force start synchronized playback (for UI button)
        public void ForceStartSynchronizedPlayback()
        {
            if (useSynchronizedPlayback)
            {
                StartSynchronizedPlayback();
            }
        }

        // Reset synchronization (for restart)
        public void ResetSynchronizationState()
        {
            Debug.Log("Resetting synchronization state");
            completedLines.Clear();
            lineCompletionTimes.Clear();
            lineStartTimes.Clear();
            isSynchronizedPlaybackReady = false;
            synchronizedPlaybackStartTime = 0f;
            longestLineDrawingTime = 0f; // Reset the longest line drawing time
            
            // Re-initialize with current channels
            foreach (var channelId in channelMusicPlayers.Keys)
            {
                completedLines[channelId] = false;
                lineCompletionTimes[channelId] = 0f;
                lineStartTimes[channelId] = 0f;
            }
            
            Debug.Log("Synchronization state reset complete");
        }

        #endregion
    }
}