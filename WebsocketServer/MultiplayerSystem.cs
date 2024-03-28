using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyWebsocketLibs;
using WebSocketSharp;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer
{
    public class MultiplayerSystem : WebsocketManager
    {
        public Action OnWebSocketOpenCallback, OnWebSocketCloseCallback;
        public static JsonConverter[] _dataConverter = new JsonConverter[] { new SocketDataConverter() };

        public ConcurrentQueue<SocketSongInfo> _receivedSongInfo;
        public ConcurrentQueue<SocketLobbyInfo> _receivedLobbyInfo;
        public ConcurrentQueue<SocketOptionInfo> _receivedSocketOptionInfo;

        public Action<SocketSongInfo> OnSocketSongInfoReceived;
        public Action<SocketLobbyInfo> OnSocketLobbyInfoReceived;
        public Action<SocketOptionInfo> OnSocketOptionReceived;

        public string GetServerID => _id;

        public MultiplayerSystem(string serverID, bool isHost) : base(serverID, "wss://spec.toottally.com/mp/join/", PluginInfo.PLUGIN_VERSION)
        {
            ConnectionPending = true;

            _receivedSongInfo = new ConcurrentQueue<SocketSongInfo>();
            _receivedLobbyInfo = new ConcurrentQueue<SocketLobbyInfo>();
            _receivedSocketOptionInfo = new ConcurrentQueue<SocketOptionInfo>();


            ConnectToWebSocketServer(_url + serverID, TootTallyAccounts.Plugin.GetAPIKey, isHost);
        }

        public void SendSongHash(string filehash, float gamespeed, string modifiers)
        {
            SocketSetSongByHash socketSetSongByHash = new SocketSetSongByHash()
            {
                dataType = DataType.SetSong.ToString(),
                filehash = filehash,
                gamespeed = gamespeed,
                modifiers = modifiers
            };
            SendSongHash(socketSetSongByHash);
        }

        public void SendSetLobbyInfo(string name, string description, string password, int maxPlayer)
        {
            SocketSetLobbyInfo socketSetLobbyInfo = new SocketSetLobbyInfo()
            {
                dataType = DataType.SetLobbyInfo.ToString(),
                name = name,
                description = description,
                password = password,
                maxPlayer = maxPlayer
            };
            var json = JsonConvert.SerializeObject(socketSetLobbyInfo);
            SendToSocket(json);
        }

        public void SendSongHash(SocketSetSongByHash socketSetSongByHash)
        {
            var json = JsonConvert.SerializeObject(socketSetSongByHash);
            SendToSocket(json);
        }

        public void SendOptionInfo(OptionInfoType optionType, dynamic[] values = null)
        {
            SocketOptionInfo socketOptionInfo = new SocketOptionInfo()
            {
                dataType = DataType.OptionInfo.ToString(),
                optionType = optionType.ToString(),
                values = values
            };
            var json = JsonConvert.SerializeObject(socketOptionInfo);
            SendToSocket(json);
        }

        public void SendUserState(UserState state) =>
            SendOptionInfo(OptionInfoType.UpdateUserState, new dynamic[] { state.ToString() });

        public void SendUpdateScore(int score, int combo, int health, int tally) =>
            SendOptionInfo(OptionInfoType.UpdateScore, new dynamic[] { score, combo, health, tally});    

        public void UpdateStacks()
        {
            if (OnSocketSongInfoReceived != null && _receivedSongInfo.TryDequeue(out SocketSongInfo songInfo))
                OnSocketSongInfoReceived.Invoke(songInfo);
            if (OnSocketLobbyInfoReceived != null && _receivedLobbyInfo.TryDequeue(out SocketLobbyInfo lobbyInfo))
                OnSocketLobbyInfoReceived.Invoke(lobbyInfo);
            if (OnSocketOptionReceived != null && _receivedSocketOptionInfo.TryDequeue(out SocketOptionInfo option))
                OnSocketOptionReceived.Invoke(option);
        }

        protected override void OnDataReceived(object sender, MessageEventArgs e)
        {
            if (e.IsText)
            {
                SocketMessage message;
                try
                {
                    message = JsonConvert.DeserializeObject<SocketMessage>(e.Data, _dataConverter);
                }
                catch (Exception) { return; }

                if (message is SocketSongInfo && !IsHost)
                    _receivedSongInfo.Enqueue(message as SocketSongInfo);
                else if (message is SocketOptionInfo)
                    _receivedSocketOptionInfo.Enqueue(message as SocketOptionInfo);
                else if (message is SocketLobbyInfo)
                    _receivedLobbyInfo.Enqueue(message as SocketLobbyInfo);
            }
        }

        protected override void OnWebSocketOpen(object sender, EventArgs e)
        {
            TootTallyNotifManager.DisplayNotif($"Connected to multiplayer server.");
            OnWebSocketOpenCallback?.Invoke();
            base.OnWebSocketOpen(sender, e);
        }

        protected override void OnWebSocketClose(object sender, EventArgs e)
        {
            OnWebSocketCloseCallback?.Invoke();
            base.OnWebSocketClose(sender, e);
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                TootTallyNotifManager.DisplayNotif($"Disconnected from multiplayer server.");
                CloseWebsocket();
            }
        }

        public enum DataType
        {
            SongInfo,
            LobbyInfo,
            OptionInfo,
            SetSong,
            SetLobbyInfo,
        }

        public enum QuickChat
        {
            Hello,
            Welcome,
            GoodLuck,
            ReadyUp,
            BeRightBack,
            ImHere,
            NicePlay,
            GoodGame,
        }

        public enum OptionInfoType
        {
            //Events
            Refresh,
            UpdateUserInfo,

            //User Commands
            UpdateUserState,
            UpdateScore,
            SongFinished,
            FinalScore,
            QuickChat,

            //Host Commands
            GiveHost,
            KickFromLobby,
            StartTimer,
            StartGame,
            AbortGame,

            //Lobby Updates
            LobbyInfoChanged,
        }

        public class SocketMessage
        {
            public string dataType { get; set; }
        }

        public class SocketSetSongByHash : SocketMessage
        {
            public string filehash { get; set; }
            public float gamespeed { get; set; }
            public string modifiers { get; set; }
        }

        public class SocketSetLobbyInfo : SocketMessage
        {
            public string name;
            public string description;
            public string password;
            public int maxPlayer;
        }

        public class SocketOptionInfo : SocketMessage
        {
            public string optionType { get; set; }
            public dynamic[] values { get; set; }
        }

        public class SocketLobbyInfo : SocketMessage
        {
            public MultiplayerLobbyInfo lobbyInfo { get; set; }
        }

        public class SocketSongInfo : SocketMessage
        {
            public MultiplayerSongInfo songInfo { get; set; }
        }


        public class SocketDataConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(SocketMessage);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return Enum.Parse(typeof(DataType), jo["dataType"].Value<string>()) switch
                {
                    DataType.SongInfo => jo.ToObject<SocketSongInfo>(serializer),
                    DataType.LobbyInfo => jo.ToObject<SocketLobbyInfo>(serializer),
                    DataType.OptionInfo => jo.ToObject<SocketOptionInfo>(serializer),
                    _ => null,
                };
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
