using System;
using System.Collections.Generic;
using UnityEngine;

namespace LetterMatch
{
    [System.Serializable]
    public class MusicMarker
    {
        public float time;
        public MusicMarkerType type;
        public string channelId;
        public string description;
    }

    [System.Serializable]
    public class LevelMusicData
    {
        public string levelId;
        public string musicEventPath;
        public List<MusicChannel> musicChannels = new List<MusicChannel>();
        public List<MusicMarker> musicMarkers = new List<MusicMarker>();
    }

    [System.Serializable]
    public class MusicChannel
    {
        public string channelId;
        public MusicChannelType channelType;
        public string eventPath;
        public float volume = 1f;
    }

    public enum MusicMarkerType
    {
        Kick,
        Piano,
        Bass,
        Synth,
        Percussion,
        Other
    }
} 