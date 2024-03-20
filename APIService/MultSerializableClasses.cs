using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

namespace TootTallyMultiplayer.APIService
{
    public static class MultSerializableClasses
    {
        [Serializable]
        public class APICreateSubmission
        {
            public string name;
            public string description;
            public string password;
            public int maxPlayer;
        }

        [Serializable]
        public class APIMultiplayerInfo
        {
            public int count { get; set; }
            public List<MultiplayerLobbyInfo> lobbies { get; set; }
        }

        [Serializable]
        public class MultiplayerLobbyInfo
        {
            public string code;
            public string id;
            public int maxPlayerCount;
            public string title;
            public bool hasPassword;
            public string state;
            public List<MultiplayerUserInfo> players;
            public MultiplayerSongInfo songInfo;
        }

        [Serializable]
        public class MultiplayerSongInfo
        {
            public float difficulty;
            public string charter;
            public string download;
            public string mirror;
            public string fileHash;
            public float gameSpeed;
            public string modifiers;
            public int songID;
            public string songName;
            public string songShortName;
            public string trackRef;
            public float[] speed_diffs;
        }

        [Serializable]
        public class MultiplayerUserInfo
        {
            public string country;
            public int id;
            public int rank;
            public int team;
            public string username;
            public string state;
        }

        public enum UserState
        {
            None,
            Ready,
            NotReady,
            NoSong,
            SelectingSong,
            Loading,
            Playing,
            Host,
        }
    }
}
