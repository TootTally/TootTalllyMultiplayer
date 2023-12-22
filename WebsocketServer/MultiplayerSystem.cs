using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyMultiplayer.APIService;
using TootTallyWebsocketLibs;
using WebSocketSharp;

namespace TootTallyMultiplayer
{
    public class MultiplayerSystem : WebsocketManager
    {
        public Action OnWebSocketOpenCallback;
        public static JsonConverter[] _dataConverter = new JsonConverter[] { new SocketDataConverter() };

        public ConcurrentQueue<SocketSongInfo> _receivedSongInfo;
        public ConcurrentQueue<SocketOptionInfo> _receivedSocketOptionInfo;

        public Action<SocketSongInfo> OnSocketSongInfoReceived;
        public Action<SocketOptionInfo> OnSocketOptionReceived;

        public string GetServerID => _id;

        public MultiplayerSystem(string serverID, bool isHost) : base(serverID, "wss://spec.toottally.com/mp/join/", "1.0.0")
        {
            ConnectionPending = true;
            ConnectToWebSocketServer(_url + serverID, TootTallyAccounts.Plugin.GetAPIKey, isHost);
        }

        public void SendSongHash(string filehash)
        {
            SocketSetSongByHash socketSetSongByHash = new SocketSetSongByHash()
            {
                dataType = DataType.SetSong.ToString(),
                filehash = filehash
            };
            SendToSocket(JsonConvert.SerializeObject(socketSetSongByHash));
        }

        public void UpdateStacks()
        {
            if (OnSocketSongInfoReceived != null && _receivedSongInfo.TryDequeue(out SocketSongInfo songInfo))
                OnSocketSongInfoReceived.Invoke(songInfo);
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
            }
        }

        protected override void OnWebSocketOpen(object sender, EventArgs e)
        {
            TootTallyNotifManager.DisplayNotif($"Connected to multiplayer server.");
            OnWebSocketOpenCallback?.Invoke();
            base.OnWebSocketOpen(sender, e);
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
            OptionInfo,
            SetSong
        }

        public enum LobbyOptionType
        {
            LobbyInfoChanged,
            TitleChanged,
            PasswordChanged,
            ModifierChanged,
            GameSpeedChanged,
        }

        public class SocketMessage
        {
            public string dataType { get; set; }
        }

        public class SocketSetSongByHash : SocketMessage
        {
            public string filehash { get; set; }
        }

        public class SocketOptionInfo : SocketMessage
        {
            public LobbyOptionType optionType { get; set; }
            public object[] values { get; set; }
        }

        public class SocketSongInfo : SocketMessage
        {
            public MultSerializableClasses.MultiplayerSongInfo songInfo { get; set; }
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
