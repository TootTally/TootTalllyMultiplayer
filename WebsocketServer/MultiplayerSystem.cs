using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                ISocketMessage message;
                try
                {
                    message = JsonConvert.DeserializeObject<ISocketMessage>(e.Data, _dataConverter);
                }
                catch (Exception) { return; }

                if (message is SocketSongInfo && !IsHost)
                    _receivedSongInfo.Enqueue((SocketSongInfo)message);
                else if (message is SocketOptionInfo)
                    _receivedSocketOptionInfo.Enqueue((SocketOptionInfo)message);
                else if (message is SocketLobbyInfo)
                    _receivedLobbyInfo.Enqueue((SocketLobbyInfo)message);
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
            //Introductions
            Hello,
            Welcome,
            Bye,
            Leave,

            //SelfState
            Wait,
            ImReady,
            ReadyUp,
            GoodLuck,

            //Opinions
            GoodChart,
            BadChart,
            Yes,
            No,

            //Requests
            TooFast,
            TooSlow,
            TooHard,
            TooEasy,

            //PostGame
            NicePlay,
            GoodGame,
            CloseOne,
            Rematch,

            //Emotions - Demands
            Laugh,
            Enjoy,
            WantHost,
            GiveHost
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
            Quit,

            //Host Commands
            GiveHost,
            KickFromLobby,
            StartTimer,
            StartGame,
            AbortGame,

            //Lobby Updates
            LobbyInfoChanged,
        }

        public interface ISocketMessage
        {
            public string dataType { get; }
        }

        public struct SocketSetSongByHash : ISocketMessage
        {
            public string filehash { get; set; }
            public float gamespeed { get; set; }
            public string modifiers { get; set; }

            public string dataType => DataType.SetSong.ToString();
        }

        public struct SocketSetLobbyInfo : ISocketMessage
        {
            public string name;
            public string description;
            public string password;
            public int maxPlayer;
            public string dataType => DataType.SetSong.ToString();
        }

        public struct SocketOptionInfo : ISocketMessage
        {
            public string optionType { get; set; }
            public dynamic[] values { get; set; }
            public string dataType => DataType.OptionInfo.ToString();
        }

        public struct SocketLobbyInfo : ISocketMessage
        {
            public MultiplayerLobbyInfo lobbyInfo { get; set; }
            public string dataType => DataType.LobbyInfo.ToString();
        }

        public struct SocketSongInfo : ISocketMessage
        {
            public MultiplayerSongInfo songInfo { get; set; }
            public string dataType => DataType.SongInfo.ToString();
        }


        public class SocketDataConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(ISocketMessage);

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

        public static readonly Dictionary<QuickChat, string> QuickChatToTextDic = new Dictionary<QuickChat, string>()
        {
            {QuickChat.Hello, "Hello!" },
            {QuickChat.Welcome, "Welcome!" },
            {QuickChat.Bye, "Bye bye!" },
            {QuickChat.Leave, "I have to leave." },

            {QuickChat.Wait, "Wait for me." },
            {QuickChat.ImReady, "I'm ready!" },
            {QuickChat.ReadyUp, "Ready up!" },
            {QuickChat.GoodLuck, "Good luck!" },

            {QuickChat.GoodChart, "I like this chart." },
            {QuickChat.BadChart, "I don't like this chart." },
            {QuickChat.Yes, "Yes." },
            {QuickChat.No, "No." },

            {QuickChat.TooFast, "I think the game speed is too fast." },
            {QuickChat.TooSlow, "I think the game speed is too slow." },
            {QuickChat.TooHard, "I think the song is too hard." },
            {QuickChat.TooEasy, "I think the song is too easy." },

            {QuickChat.NicePlay, "Nice play!" },
            {QuickChat.GoodGame, "Good game." },
            {QuickChat.CloseOne, "That was a close match!" },
            {QuickChat.Rematch, "Rematch!" },

            {QuickChat.Laugh, "Ahah!" },
            {QuickChat.Enjoy, "Im enjoying this lobby!" },
            {QuickChat.WantHost, "Can I have host?" },
            {QuickChat.GiveHost, "Who want to be host?" }
        };
    }
}
