using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LetterMatch
{
    public class SynchronizationUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text statusText;
        public Button playButton;
        public Button resetButton;
        public Transform lineStatusContainer;
        public GameObject lineStatusPrefab;

        [Header("Settings")]
        public float updateInterval = 0.5f;

        private KoreographerAudioManager audioManager;
        private List<GameObject> lineStatusObjects = new List<GameObject>();
        private float lastUpdateTime = 0f;

        void Start()
        {
            audioManager = FindObjectOfType<KoreographerAudioManager>();
            
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetButtonClicked);
        }

        void Update()
        {
            if (audioManager == null) return;

            // Update UI at intervals to avoid excessive updates
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateUI();
                lastUpdateTime = Time.time;
            }
        }

        void UpdateUI()
        {
            // Update status text
            if (statusText != null)
            {
                statusText.text = audioManager.GetSynchronizationStatus();
            }

            // Update play button
            if (playButton != null)
            {
                bool canPlay = audioManager.IsSynchronizedPlaybackReady() && !audioManager.IsMusicPlaying();
                playButton.interactable = canPlay;
                playButton.GetComponentInChildren<Text>().text = canPlay ? "Play Music!" : "Wait...";
            }

            // Update reset button
            if (resetButton != null)
            {
                bool canReset = !audioManager.IsMusicPlaying();
                resetButton.interactable = canReset;
            }

            // Update line status indicators
            UpdateLineStatusIndicators();
        }

        void UpdateLineStatusIndicators()
        {
            if (lineStatusContainer == null || lineStatusPrefab == null) return;

            var lineStatus = audioManager.GetAllLineStatus();
            
            // Create or update status objects
            int index = 0;
            foreach (var kvp in lineStatus)
            {
                string channelId = kvp.Key;
                bool isCompleted = kvp.Value;

                // Create new status object if needed
                if (index >= lineStatusObjects.Count)
                {
                    GameObject statusObj = Instantiate(lineStatusPrefab, lineStatusContainer);
                    lineStatusObjects.Add(statusObj);
                }

                // Update status object
                GameObject statusObject = lineStatusObjects[index];
                if (statusObject != null)
                {
                    var statusUI = statusObject.GetComponent<LineStatusUI>();
                    if (statusUI != null)
                    {
                        statusUI.UpdateStatus(channelId, isCompleted);
                    }
                }

                index++;
            }

            // Remove extra objects
            while (lineStatusObjects.Count > lineStatus.Count)
            {
                int lastIndex = lineStatusObjects.Count - 1;
                if (lineStatusObjects[lastIndex] != null)
                {
                    Destroy(lineStatusObjects[lastIndex]);
                }
                lineStatusObjects.RemoveAt(lastIndex);
            }
        }

        void OnPlayButtonClicked()
        {
            if (audioManager != null)
            {
                audioManager.ForceStartSynchronizedPlayback();
            }
        }

        void OnResetButtonClicked()
        {
            if (audioManager != null)
            {
                audioManager.ResetSynchronizationState();
            }
        }
    }

    // Helper component for individual line status
    public class LineStatusUI : MonoBehaviour
    {
        public Text channelText;
        public Image statusIcon;
        public Color completedColor = Color.green;
        public Color pendingColor = Color.red;

        public void UpdateStatus(string channelId, bool isCompleted)
        {
            if (channelText != null)
            {
                channelText.text = channelId;
            }

            if (statusIcon != null)
            {
                statusIcon.color = isCompleted ? completedColor : pendingColor;
            }
        }
    }
} 