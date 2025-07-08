using DG.Tweening;
using LetterMatch;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetterMatch
{
    public class LetterMatchController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Letter Info")] 
        public string letterId;
        public string channelId;
        public MusicChannelType channelType;
        public LetterType letterType;

        [Header("References")] 
        public GameObject spineChild;
        public LetterConfig letterConfig;
        public ColorPalette colorPalette;

        [Header("State")] 
        public bool isMatched = false;
        public bool isLocked = false;
        public bool isDragging = false;

        private SkeletonGraphic skeletonGraphic;
        private Image imageComponent;
        private CanvasGroup spineCanvasGroup;
        private Canvas canvas;
        private RectTransform rectTransform;
        private LetterMatchController matchedPair;
        private KoreographerAudioManager audioManager;
        private Vector2 originalPosition;

        void Awake()
        {
            imageComponent = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        void Start()
        {
            InitializeSpine();
            InitializeAudioManager();
            UpdateColorFromPalette();
            originalPosition = rectTransform.anchoredPosition;
        }

        void InitializeSpine()
        {
            if (spineChild != null)
            {
                skeletonGraphic = spineChild.GetComponent<SkeletonGraphic>();
                spineCanvasGroup = spineChild.GetComponent<CanvasGroup>();
                if (spineCanvasGroup == null)
                    spineCanvasGroup = spineChild.AddComponent<CanvasGroup>();

                spineCanvasGroup.alpha = 0f;
                spineCanvasGroup.interactable = false;
                spineCanvasGroup.blocksRaycasts = false;
            }
        }

        void InitializeAudioManager()
        {
            audioManager = FindObjectOfType<KoreographerAudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("SimpleMusicPlayerAudioManager not found in scene!");
            }
        }

        #region Setters and Getters
        
        public void SetId(string id) => letterId = id;
        public void SetChannelId(string id) => channelId = id;
        public void SetChannelType(MusicChannelType type) => channelType = type;
        public void SetLetterType(LetterType type) => letterType = type;

        public void SetLetterConfig(LetterConfig config)
        {
            letterConfig = config;
            UpdateColorFromPalette();
        }

        public void SetSpineChild(GameObject spine)
        {
            spineChild = spine;
            InitializeSpine();
        }

        public string GetId() => letterId;
        public string GetChannelId() => channelId;
        public MusicChannelType GetChannelType() => channelType;
        public LetterType GetLetterType() => letterType;
        public GameObject GetSpineChild() => spineChild;
        
        #endregion

        #region Visual Control Methods

        public void HideImage() => imageComponent.enabled = false;
        public void ShowImage() => imageComponent.enabled = true;

        public void HideSpine()
        {
            if (spineCanvasGroup != null)
            {
                spineCanvasGroup.alpha = 0f;
                spineCanvasGroup.interactable = false;
                spineCanvasGroup.blocksRaycasts = false;
            }
        }

        public void ShowSpine()
        {
            if (spineCanvasGroup != null)
            {
                spineCanvasGroup.alpha = 1f;
                spineCanvasGroup.interactable = true;
                spineCanvasGroup.blocksRaycasts = true;
            }
        }

        #endregion

        #region Animation Methods

        public void PlayKickAnimation()
        {
            if (letterConfig == null) return;
            ShowSpine();
            skeletonGraphic?.AnimationState?.SetAnimation(0, letterConfig.kickAnimationName, false);
        }

        public void PlayPianoAnimation()
        {
            if (letterConfig == null) return;
            ShowSpine();
            skeletonGraphic?.AnimationState?.SetAnimation(0, letterConfig.pianoAnimationName, false);
        }

        public void PlayOtherAnimation()
        {
            if (letterConfig == null) return;
            ShowSpine();
            skeletonGraphic?.AnimationState?.SetAnimation(0, letterConfig.otherAnimationName, false);
        }

        public void PlayCompletionAnimation()
        {
            if (letterConfig == null) return;
            ShowSpine();
            skeletonGraphic?.AnimationState?.SetAnimation(0, letterConfig.completionAnimationName, false);
        }

        #endregion

        #region Audio Control Methods

        public void PlayChannelMusic()
        {
            if (audioManager != null && !string.IsNullOrEmpty(channelId))
            {
                var musicPlayer = audioManager.GetMusicPlayer(channelId);
                if (musicPlayer != null)
                {
                    musicPlayer.Play();
                }
            }
        }

        public void StopChannelMusic()
        {
            if (audioManager != null && !string.IsNullOrEmpty(channelId))
            {
                var musicPlayer = audioManager.GetMusicPlayer(channelId);
                if (musicPlayer != null)
                {
                    musicPlayer.Stop();
                }
            }
        }

        public void PauseChannelMusic()
        {
            if (audioManager != null && !string.IsNullOrEmpty(channelId))
            {
                var musicPlayer = audioManager.GetMusicPlayer(channelId);
                if (musicPlayer != null)
                {
                    musicPlayer.Pause();
                }
            }
        }

        #endregion

        #region Drag and Drop Handlers

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isLocked || isMatched) return;

            DOTween.Kill(rectTransform);
            transform.rotation = Quaternion.identity;

            isDragging = true;
            //transform.SetAsLastSibling();

            ShowSpine();
            PlayChannelMusic();

            var drawingController = LetterMatchGameManager.Instance?.drawingController;
            if (drawingController != null)
            {
                drawingController.OnLetterDragStart(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isLocked) return;

            // Don't move the letter - keep it in original position
           // rectTransform.anchoredPosition = originalPosition;

            var drawingController = LetterMatchGameManager.Instance?.drawingController;
            if (drawingController != null)
            {
                drawingController.OnLetterDragging(this, eventData.position);
            }

            // Check for line intersection with other letters
            CheckForLineIntersection(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging || isLocked) return;

            isDragging = false;
            StopChannelMusic();
            HideSpine();

            // Final check for matching when drawing ends
            bool wasMatched = CheckForMatchingOnDragEnd(eventData.position);

            var drawingController = LetterMatchGameManager.Instance?.drawingController;
            if (drawingController != null)
            {
                // If no match was made, clear the drawing line
                if (!wasMatched)
                {
                    drawingController.ClearCurrentLine();
                }

                drawingController.OnLetterDragEnd(this);
            }
        }

        #endregion

        #region Matching Logic

        void CheckForLineIntersection(Vector2 currentMousePosition)
        {
            var spawner = LetterMatchGameManager.Instance?.letterSpawner;
            if (spawner == null || letterConfig == null) return;

            string oppositeId = letterConfig.GetOppositeLetter(letterId);
            LetterMatchController targetLetter = null;

            // Find the opposite letter
            if (letterType == LetterType.Capital)
            {
                targetLetter = spawner.smallLetters.Find(l => l != null && l.GetId() == oppositeId);
            }
            else
            {
                targetLetter = spawner.capitalLetters.Find(l => l != null && l.GetId() == oppositeId);
            }

            if (targetLetter == null || targetLetter.isMatched || targetLetter.isLocked) return;

            // Check if mouse is over the target letter
            if (IsMouseOverLetter(currentMousePosition, targetLetter))
            {
                Debug.Log($"Line intersection detected: {letterId} -> {targetLetter.GetId()}");
                MatchWith(targetLetter);
            }
        }

        bool CheckForMatchingOnDragEnd(Vector2 endPosition)
        {
            var spawner = LetterMatchGameManager.Instance?.letterSpawner;
            if (spawner == null || letterConfig == null) return false;

            string oppositeId = letterConfig.GetOppositeLetter(letterId);
            LetterMatchController targetLetter = null;

            // Find the opposite letter
            if (letterType == LetterType.Capital)
            {
                targetLetter = spawner.smallLetters.Find(l => l != null && l.GetId() == oppositeId);
            }
            else
            {
                targetLetter = spawner.capitalLetters.Find(l => l != null && l.GetId() == oppositeId);
            }

            if (targetLetter == null || targetLetter.isMatched || targetLetter.isLocked) return false;

            // Check if the line ends over the target letter
            if (IsMouseOverLetter(endPosition, targetLetter))
            {
                Debug.Log($"Drag end match: {letterId} -> {targetLetter.GetId()}");
                MatchWith(targetLetter);
                return true;
            }

            return false;
        }

        bool IsMouseOverLetter(Vector2 mousePosition, LetterMatchController letter)
        {
            if (letter == null) return false;

            // Convert mouse position to local position of the letter
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    letter.rectTransform, mousePosition, canvas.worldCamera, out Vector2 localPoint))
            {
                // Check if the point is within the letter's bounds
                Rect rect = letter.rectTransform.rect;
                return rect.Contains(localPoint);
            }

            return false;
        }

        void MatchWith(LetterMatchController other)
        {
            if (isMatched || other.isMatched) return; // Prevent double matching

            isMatched = true;
            isLocked = true;
            other.isMatched = true;
            other.isLocked = true;

            matchedPair = other;
            other.matchedPair = this;

            // Stop ALL music through the audio manager
            StopAllMusic();

            // Play completion animations
            PlayCompletionAnimation();
            other.PlayCompletionAnimation();

            var gameManager = LetterMatchGameManager.Instance;
            if (gameManager != null)
            {
                var matchData = new LetterMatchData(letterId, letterConfig?.capitalLetter ?? "",
                    letterConfig?.smallLetter ?? "", channelId, channelType);
                gameManager.OnLetterMatched(matchData);
            }
        }

        void StopAllMusic()
        {
            if (audioManager != null)
            {
                audioManager.StopMusic();
            }
        }

        #endregion

        #region Color and Visual Updates

        public void SetMatchData(LetterMatchData letterMatchData)
        {
            // Implementation for setting match data if needed
        }

        public void SetColor(Color color)
        {
            if (imageComponent != null)
            {
                imageComponent.color = color;
            }

            if (skeletonGraphic != null)
            {
                skeletonGraphic.color = color;
            }
        }

        public void UpdateColorFromPalette()
        {
            if (colorPalette == null) return;

            string id = letterId.ToUpper();
            if (string.IsNullOrEmpty(id)) return;

            int index = id[0] - 'A';
            Color color = colorPalette.GetColor(index);
            SetColor(color);
        }

        #endregion

        void OnDestroy()
        {
            // Stop individual channel music when this letter is destroyed
            StopChannelMusic();
        }
    }
}