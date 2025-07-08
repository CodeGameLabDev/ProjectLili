using UnityEngine;
using UnityEngine.UI;

namespace LetterMatch
{
    public class HitPoint : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float pulseSpeed = 2f;
        public float pulseScale = 1.5f;
        
        [Header("State")]
        public string channelId;
        public float spawnTime;
        public bool isAnimating = false;
        
        private RectTransform rectTransform;
        private Image circleImage;
        private Vector3 originalScale;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            circleImage = GetComponent<Image>();
            originalScale = rectTransform.localScale;
        }
        
        public void Initialize(string channel, float time, Color color)
        {
            channelId = channel;
            spawnTime = time;
            
            if (circleImage != null)
            {
                circleImage.color = color;
            }
        }
        
        public void TriggerPulse()
        {
            if (!isAnimating)
            {
                isAnimating = true;
                StartCoroutine(PulseAnimation());
            }
        }
        
        private System.Collections.IEnumerator PulseAnimation()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < 1f / pulseSpeed)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime * pulseSpeed;
                float scale = Mathf.Lerp(1f, pulseScale, Mathf.Sin(progress * Mathf.PI));
                rectTransform.localScale = originalScale * scale;
                yield return null;
            }
            
            rectTransform.localScale = originalScale;
            isAnimating = false;
        }
        
        public void ResetAnimation()
        {
            isAnimating = false;
            rectTransform.localScale = originalScale;
            StopAllCoroutines();
        }
    }
} 