using SuperSystems.UnityTools;
using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyMultiplayer.APIService;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerCreatePanel : MultiplayerPanelBase
    {
        private GameObject _centerContainer;
        private TMP_InputField _lobbyName, _lobbyDescription, _lobbyPassword, _lobbyMaxPlayer;
        private Toggle _autoRotateToggle, _teamsToggle, _freemodToggle;

        public bool IsRequestPending;
        public MultiplayerCreatePanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "CreateLayout")
        {
            panel.transform.localPosition = new Vector2(0, 2000);
            _centerContainer = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(500, 0), center.transform);
            var centerLayout = _centerContainer.GetComponent<VerticalLayoutGroup>();
            centerLayout.spacing = 40;
            centerLayout.childAlignment = TextAnchor.UpperCenter;

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

            //Other Settings
            var _otherSettingsContainer = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(245, 0), _centerContainer.transform);
            var otherLayout = _otherSettingsContainer.GetComponent<VerticalLayoutGroup>();
            otherLayout.childAlignment = TextAnchor.UpperLeft;
            otherLayout.childForceExpandWidth = otherLayout.childControlWidth = false;

            _autoRotateToggle = MultiplayerGameObjectFactory.CreateToggle(_otherSettingsContainer.transform, "AutorotateToggle", new Vector2(60, 60), "autorotate");
            _teamsToggle = MultiplayerGameObjectFactory.CreateToggle(_otherSettingsContainer.transform, "TeamsToggle", new Vector2(60, 60), "teams");
            _freemodToggle = MultiplayerGameObjectFactory.CreateToggle(_otherSettingsContainer.transform, "FreemodToggle", new Vector2(60, 60), "freemod");

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
            else if (value < 2 || value > 32)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("MaxPlayer must be between 2 and 32.");
            }

            if (name.Length > 32)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Lobby name has to be\n32 characters or shorter");
            }

            if (desc.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Description has to be\n100 characters or shorter");
            }

            if (pass.Length > 100)
            {
                isValid = false;
                TootTallyNotifManager.DisplayNotif("Password has to be\n100 characters or shorter");
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
            var apiSubmission = new APICreateSubmission()
            {
                name = _lobbyName.text,
                description = _lobbyDescription.text,
                password = _lobbyPassword.text,
                maxPlayer = int.Parse(_lobbyMaxPlayer.text),
                autorotate = _autoRotateToggle.isOn,
                teams = _teamsToggle.isOn,
                freemod = _freemodToggle.isOn,
                version = PluginInfo.PLUGIN_VERSION
            };
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.CreateMultiplayerServerRequest(apiSubmission, serverCode =>
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
