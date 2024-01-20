using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyMultiplayer.APIService;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerCreatePanel : MultiplayerPanelBase
    {
        private GameObject _centerContainer;

        private TMP_InputField _lobbyName, _lobbyDescription, _lobbyPassword, _lobbyMaxPlayer;

        public bool IsRequestPending;
        public MultiplayerCreatePanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "CreateLayout")
        {
            panel.transform.localPosition = new Vector2(0, 2000);
            _centerContainer = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(300, 0), center.transform);
            _centerContainer.GetComponent<VerticalLayoutGroup>().spacing = 50;

            var titleText = GameObjectFactory.CreateSingleText(headerCenter.transform, "TitleText", "Create Lobby");
            titleText.enableAutoSizing = true;
            _lobbyName = MultiplayerGameObjectFactory.CreateInputField(_centerContainer.transform, "LobbyNameInputField", new Vector2(300, 30), 24, $"{TootTallyUser.userInfo.username}'s Lobby", false);
            _lobbyDescription = MultiplayerGameObjectFactory.CreateInputField(_centerContainer.transform, "LobbyDescriptionInputField", new Vector2(300, 30), 24, "Welcome to my lobby!", false);
            _lobbyPassword = MultiplayerGameObjectFactory.CreateInputField(_centerContainer.transform, "LobbyPasswordInputField", new Vector2(300, 30), 24, "", true);
            _lobbyMaxPlayer = MultiplayerGameObjectFactory.CreateInputField(_centerContainer.transform, "LobbyMaxPlayerInputField", new Vector2(300, 30), 24, "16", false);

            GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Back", "CreateBackButton", OnBackButtonClick);
            GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Create", "CreateLobbyButton", OnCreateButtonClick);
        }

        private void OnBackButtonClick()
        {
            if (controller.IsRequestPending) return;

            controller.MoveToMain();
            MultiplayerManager.UpdateMultiplayerState(MultiplayerController.MultiplayerState.Home);


        }

        private bool ValidateInput()
        {
            bool isValid = true;

            if (!int.TryParse(_lobbyMaxPlayer.text, out int value))
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("MaxPlayer must be a number.");
            }

            if (_lobbyName.text.Length > 32)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Lobby name has to be\nshorter than 32 characters");
            }

            if (_lobbyDescription.text.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Description has to be\nshorter than 32 characters");
            }

            if (_lobbyPassword.text.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Password has to be\nshorter than 32 characters");
            }

            return isValid;
        }

        private void OnCreateButtonClick()
        {
            if (IsRequestPending || !ValidateInput() || controller.IsConnected || controller.IsConnectionPending) return;

            IsRequestPending = true;
            TootTallyNotifManager.DisplayNotif("Creating lobby...");
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.CreateMultiplayerServerRequest(_lobbyName.text, _lobbyDescription.text, _lobbyPassword.text, int.Parse(_lobbyMaxPlayer.text), serverCode =>
            {
                if (serverCode != null)
                {
                    Plugin.LogInfo(serverCode);
                    controller.ConnectToLobby(serverCode);
                }
                else
                    TootTallyNotifManager.DisplayNotif("Lobby creation failed.");
                IsRequestPending = false;
            }));
        }
    }
}
