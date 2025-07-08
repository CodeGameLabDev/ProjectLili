using System;
using UnityEngine;

namespace LetterMatch
{
    [Serializable]
    public class LetterMatchData
    {
        [Header("Letter Info")]
        public string letterId;
        public string capitalLetter;
        public string smallLetter;
    
        [Header("Music Channel")]
        public string channelId;
        public MusicChannelType channelType;
    
        [Header("Position")]
        public Vector2 capitalPosition;
        public Vector2 smallPosition;
    
        [Header("State")]
        public bool isMatched = false;
        public bool isLocked = false;
        public float matchTime = 0f;
    
        [Header("Animation")]
        public string kickAnimationName = "kick";
        public string pianoAnimationName = "piano";
        public string otherAnimationName = "other";
        public string completionAnimationName = "completion";
    
        public LetterMatchData(string id, string capital, string small, string channel, MusicChannelType type)
        {
            letterId = id;
            capitalLetter = capital;
            smallLetter = small;
            channelId = channel;
            channelType = type;
        }
    
        public void SetMatched(bool matched, float time = 0f)
        {
            isMatched = matched;
            if (matched)
                matchTime = time;
        }
    
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    
        public bool IsComplete()
        {
            return isMatched && isLocked;
        }
    }

    public enum MusicChannelType
    {
        Kick,
        Piano,
        Bass,
        Synth,
        Percussion,
        Other
    }
}