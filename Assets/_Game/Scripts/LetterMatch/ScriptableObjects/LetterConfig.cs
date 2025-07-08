using SonicBloom.Koreo;
using UnityEngine;

namespace LetterMatch
{
    [System.Serializable]
    public class LetterConfig
    {
        [Header("Letter Info")] public string capitalLetter;
        public string smallLetter;
        public string letterId; 

        [Header("Music Channel")] public string channelId;
        public MusicChannelType channelType;
        [SerializeField] public Koreography koreography;

        [Header("Animation Names")] public string kickAnimationName = "kick";
        public string pianoAnimationName = "piano";
        public string otherAnimationName = "other";
        public string completionAnimationName = "completion";
        public string idleAnimationName = "idle";

        [Header("Visual Settings")] public Color letterColor = Color.white;
        public float scale = 1f;
        public bool useCustomSprite = false;
        public Sprite customCapitalSprite;
        public Sprite customSmallSprite;

        [Header("Audio Settings")] public float channelVolume = 1f;
        public float sampleVolume = 0.8f;
        public bool loopChannel = false;
        public bool loopSample = false;

        public LetterConfig()
        {
            letterId = System.Guid.NewGuid().ToString();
        }

        public LetterConfig(string capital, string small, string channel, MusicChannelType type)
        {
            capitalLetter = capital;
            smallLetter = small;
            channelId = channel;
            channelType = type;
            letterId = $"{capital}_{small}";
        }

        public bool IsCapitalLetter(string letter)
        {
            return letter == capitalLetter;
        }

        public bool IsSmallLetter(string letter)
        {
            return letter == smallLetter;
        }

        public string GetOppositeLetter(string letter)
        {
            if (IsCapitalLetter(letter))
                return smallLetter;
            else if (IsSmallLetter(letter))
                return capitalLetter;
            return letter;
        }

        public bool MatchesLetter(string letter)
        {
            return IsCapitalLetter(letter) || IsSmallLetter(letter);
        }
    }
}