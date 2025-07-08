using System.Collections.Generic;
using SonicBloom.Koreo;
using UnityEngine;

namespace LetterMatch
{
    [CreateAssetMenu(fileName = "LevelConfig_", menuName = "LetterMatch/Level Config")]
    public class LetterMatchLevelConfig : ScriptableObject
    {
        [Header("Level Info")]
        public string levelId;
        public string levelName;
        public string description;
    
        [Header("Music Settings")]
        public float musicBPM = 120f;
    
        [Header("Letters Configuration")]
        public List<LetterConfig> letters = new List<LetterConfig>();
    
        [Header("Visual Settings")]
        public float letterSpacing = 50f;
        public float maxSize = 1.5f;
        public Vector2 capitalRowPosition = new Vector2(0, 100f);
        public Vector2 smallRowPosition = new Vector2(0, -100f);
    
        [Header("Gameplay Settings")]
        public float matchThreshold = 50f;
        public float drawingSpeed = 1f;
        public bool requireExactMatch = false;
    
        public LetterConfig GetLetterConfig(string letterId)
        {
            return letters.Find(l => l.capitalLetter == letterId || l.smallLetter == letterId);
        }

    }
} 