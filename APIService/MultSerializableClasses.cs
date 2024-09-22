﻿using System;
using System.Collections.Generic;

namespace TootTallyMultiplayer.APIService
{
    public static class MultSerializableClasses
    {
        [Serializable]
        public struct APIShutdownSubmission
        {
            public string lobby_code;
        }

        [Serializable]
        public struct APICreateSubmission
        {
            public string name;
            public string description;
            public string password;
            public int maxPlayer;
            public string version;
            public bool autorotate;
            public bool teams;
            public bool freemod;
        }

        [Serializable]
        public struct APIMultiplayerInfo
        {
            public int count { get; set; }
            public List<MultiplayerLobbyInfo> lobbies { get; set; }
        }

        [Serializable]
        public struct MultiplayerLobbyInfo
        {
            public bool autorotate;
            public string code;
            public string description;
            public string id;
            public int maxPlayerCount;
            public string title;
            public bool hasPassword;
            public string state;
            public bool teams;
            public bool freemod;
            public List<MultiplayerUserInfo> players;
            public MultiplayerSongInfo songInfo;
        }

        [Serializable]
        public struct MultiplayerSongInfo
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
        public struct MultiplayerUserInfo
        {
            public string country;
            public int id;
            public int rank;
            public int team;
            public string username;
            public string state;
            public string mods;
        }

        public enum MultiplayerTeamState
        {
            Red = 0,
            Blue = 1,
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
            Spectating,
        }
    }
}
