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
            _centerContainer = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(500, 0), center.transform);
            _centerContainer.GetComponent<VerticalLayoutGroup>().spacing = 50;

            var titleText = GameObjectFactory.CreateSingleText(headerCenter.transform, "TitleText", "Create Lobby");
            titleText.enableAutoSizing = true;
            var defaultLobbyName = Plugin.Instance.SavedLobbyTitle.Value == "" ? $"{TootTallyUser.userInfo.username}'s Lobby" : Plugin.Instance.SavedLobbyTitle.Value;

            var nameHBox = MultiplayerGameObjectFactory.GetHorizontalBox(new Vector2(0, 55), _centerContainer.transform);
            var hlayout = nameHBox.GetComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 8f;
            hlayout.childAlignment = TextAnchor.MiddleLeft;

            var descHBox = GameObject.Instantiate(nameHBox, _centerContainer.transform);
            var passwordHBox = GameObject.Instantiate(nameHBox, _centerContainer.transform);
            var maxCountHBox = GameObject.Instantiate(nameHBox, _centerContainer.transform);

            var nameLabel = GameObjectFactory.CreateSingleText(nameHBox.transform, "NameLabel", "Name:");
            nameLabel.rectTransform.sizeDelta = new Vector2(140, 55);
            nameLabel.alignment = TextAlignmentOptions.BottomLeft;
            _lobbyName = MultiplayerGameObjectFactory.CreateInputField(nameHBox.transform, "LobbyNameInputField", new Vector2(300, 30), 24,  defaultLobbyName, false);

            var descLabel = GameObject.Instantiate(nameLabel, descHBox.transform);
            descLabel.name = "DescLabel"; descLabel.text = "Description:";
            _lobbyDescription = MultiplayerGameObjectFactory.CreateInputField(descHBox.transform, "LobbyDescriptionInputField", new Vector2(300, 30), 24, Plugin.Instance.SavedLobbyDesc.Value, false);

            var passLabel = GameObject.Instantiate(nameLabel, passwordHBox.transform);
            passLabel.name = "PasswordLabel"; passLabel.text = "Password:";
            _lobbyPassword = MultiplayerGameObjectFactory.CreateInputField(passwordHBox.transform, "LobbyPasswordInputField", new Vector2(300, 30), 24, "", true);

            var maxPlayerLabel = GameObject.Instantiate(nameLabel, maxCountHBox.transform);
            maxPlayerLabel.name = "MaxPlayerLabel"; maxPlayerLabel.text = "Max Player:";
            _lobbyMaxPlayer = MultiplayerGameObjectFactory.CreateInputField(maxCountHBox.transform, "LobbyMaxPlayerInputField", new Vector2(300, 30), 24, Plugin.Instance.SavedLobbyMaxPlayer.Value.ToString(), false);

            GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Back", "CreateBackButton", OnBackButtonClick);
            GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Create", "CreateLobbyButton", OnCreateButtonClick);
        }

        private void OnBackButtonClick()
        {
            if (controller.IsRequestPending) return;

            controller.MoveToMain();
            MultiplayerManager.UpdateMultiplayerState(MultiplayerController.MultiplayerState.Home);


        }

        public static bool ValidateInput(string name, string desc, string pass, string maxPlayer)
        {
            bool isValid = true;

            if (!int.TryParse(maxPlayer, out int value))
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("MaxPlayer must be a number.");
            }
            else if (value <= 1 || value >= 33)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("MaxPlayer must be between 2 and 32.");
            }

            if (name.Length > 32)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Lobby name has to be\nshorter than 32 characters");
            }

            if (desc.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Description has to be\nshorter than 32 characters");
            }

            if (pass.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Password has to be\nshorter than 32 characters");
            }

            if (isValid)
            {
                Plugin.Instance.SavedLobbyTitle.Value = name;
                Plugin.Instance.SavedLobbyDesc.Value = desc;
                Plugin.Instance.SavedLobbyMaxPlayer.Value = value;
            }

            return isValid;
        }

        private void OnCreateButtonClick()
        {
            if (IsRequestPending || !ValidateInput(_lobbyName.text, _lobbyDescription.text, _lobbyPassword.text, _lobbyMaxPlayer.text) || controller.IsConnected || controller.IsConnectionPending) return;

            IsRequestPending = true;
            TootTallyNotifManager.DisplayNotif("Creating lobby...");
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.CreateMultiplayerServerRequest(_lobbyName.text, _lobbyDescription.text, _lobbyPassword.text, int.Parse(_lobbyMaxPlayer.text), serverCode =>
            {
                if (serverCode != null)
                {
                    Plugin.LogInfo(serverCode);
                    controller.ConnectToLobby(serverCode, _lobbyPassword.text);
                }
                else
                    TootTallyNotifManager.DisplayNotif("Lobby creation failed.");
                IsRequestPending = false;
            }));
        }
    }
}
