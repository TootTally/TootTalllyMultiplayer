using System;
using System.Collections.Generic;

namespace TootTallyMultiplayer.APIService
{
    public static class MultSerializableClasses
    {
        [Serializable]
        public class APICreateSubmission
        {
            public string apiKey;
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
            //public string password;
            public int state;
            public List<MultiplayerUserInfo> players;
            public MultiplayerSongInfo songInfo;
        }

        [Serializable]
        public class MultiplayerSongInfo
        {
            public string difficulty;
            public string download;
            public string fileHash;
            public float gameSpeed;
            public string modifiers;
            public int songID;
            public string songName;
            public string songShortName;
            public string trackRef;
        }

        [Serializable]
        public class MultiplayerUserInfo
        {
            public string country;
            public int id;
            public int rank;
            public int team;
            public string username;
        }

    }
}
