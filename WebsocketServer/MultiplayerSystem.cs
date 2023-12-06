using System;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyWebsocketLibs;

namespace TootTallyMultiplayer
{
    public class MultiplayerSystem : WebsocketManager
    {
        public Action OnWebSocketOpenCallback;

        public string GetServerID => _id;

        public MultiplayerSystem(string serverID, bool isHost) : base(serverID, "wss://spec.toottally.com/mp/join/", "1.0.0")
        {
            ConnectionPending = true;
            ConnectToWebSocketServer(_url + serverID,TootTallyAccounts.Plugin.GetAPIKey,  isHost);
        }

        protected override void OnWebSocketOpen(object sender, EventArgs e)
        {
            TootTallyNotifManager.DisplayNotif($"Connected to multiplayer server.");
            OnWebSocketOpenCallback?.Invoke();
            base.OnWebSocketOpen(sender, e);
        }

        public void Disconnect()
        {
            if (!IsHost)
                TootTallyNotifManager.DisplayNotif($"Disconnected from multiplayer server.");
            if (IsConnected)
                CloseWebsocket();
        }
    }
}
