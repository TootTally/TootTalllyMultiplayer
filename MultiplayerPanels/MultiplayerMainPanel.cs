using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyGameModifiers;
using TootTallyMultiplayer.APIService;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public class MultiplayerMainPanel : MultiplayerPanelBase
    {
        public GameObject lobbyListContainer, lobbyInfoContainer;
        public GameObject searchPanel, searchLeft, searchCenter, searchRight;
        private GameObject _currentInputPrompt;
        private List<GameObject> _lobbyInfoRowsList;
        private ConcurrentDictionary<string, int> _savedCodeToPing;
        private string _lastSelectedLobbyID;

        private Slider _slider;
        private ScrollableSliderHandler _scrollingHandler;

        private static EventTrigger.Entry _pointerExitLobbyContainerEvent;

        private TMP_Text _lobbyPlayerListText;
        private TMP_Text _noLobbyText;
        private TMP_InputField _searchInputField;

        private static CustomButton _connectButton, _createLobbyButton, _refreshLobbyButton, _shutdownButton, _forceConnectButton;
        private static TootTallyAnimation _connectButtonScaleAnimation;

        private static MultiplayerLobbyInfo _selectedLobby;
        private static GameObject _hoveredLobbyContainer;
        private static GameObject _selectedLobbyContainer;
        private static float _previousLobbyCount;

        public MultiplayerMainPanel(GameObject canvas, MultiplayerController controller) : base(canvas, controller, "MainLayout")
        {
            //Search Panel
            searchPanel = panel.transform.Find("Search").gameObject;
            searchLeft = searchPanel.transform.GetChild(0).gameObject;
            searchCenter = searchPanel.transform.GetChild(1).gameObject;
            searchRight = searchPanel.transform.GetChild(2).gameObject;

            lobbyListContainer = center.transform.Find("Left/LobbyContainer").gameObject;
            lobbyInfoContainer = center.transform.Find("Right/InfoContainer").gameObject;

            _lobbyInfoRowsList = new List<GameObject>();
            _savedCodeToPing = new ConcurrentDictionary<string, int>();

            GameObjectFactory.CreateClickableImageHolder(headerLeft.transform, Vector2.zero, new Vector2(72, 72), AssetManager.GetSprite("gtfo.png"), "LobbyBackButton", MultiplayerManager.ExitMultiplayer);

            var titleText = GameObjectFactory.CreateSingleText(headerCenter.transform, "TitleText", "TootTally Multiplayer");
            titleText.enableAutoSizing = true;
            var serverText = GameObjectFactory.CreateSingleText(headerRight.transform, "ServerText", "Server: Toronto");
            serverText.fontSize = 40;

            var searchHLayout = searchLeft.GetComponent<HorizontalLayoutGroup>();
            searchHLayout.childAlignment = TextAnchor.MiddleLeft;
            searchHLayout.childControlHeight = searchHLayout.childForceExpandHeight = false;
            searchHLayout.spacing = 8f;
            searchHLayout.padding = new RectOffset(-12, 0, 0, 0);
            var searchText = GameObjectFactory.CreateSingleText(searchLeft.transform, "SearchText", "Search:");
            searchText.alignment = TextAlignmentOptions.MidlineRight;
            searchText.rectTransform.sizeDelta = new Vector2(90, 30);
            _searchInputField = MultiplayerGameObjectFactory.CreateInputField(searchLeft.transform, "SearchInput", new Vector2(350, 36), 24, "", false);
            _searchInputField.onValueChanged.AddListener(controller.UpdateSearchFilter);

            _slider = new GameObject("ContainerSlider", typeof(Slider)).GetComponent<Slider>();
            _slider.gameObject.SetActive(true);
            _slider.onValueChanged.AddListener(OnSliderValueChangeScrollContainer);
            _scrollingHandler = _slider.gameObject.AddComponent<ScrollableSliderHandler>();
            _scrollingHandler.enabled = false;

            _lobbyPlayerListText = GameObjectFactory.CreateSingleText(lobbyInfoContainer.transform, "LobbyDetailInfoText", "");
            _lobbyPlayerListText.rectTransform.sizeDelta = new Vector2(0, 620);
            _lobbyPlayerListText.enableAutoSizing = true;
            _lobbyPlayerListText.fontSizeMax = 32;
            _lobbyPlayerListText.alignment = TextAlignmentOptions.TopLeft;

            _pointerExitLobbyContainerEvent = new EventTrigger.Entry();
            _pointerExitLobbyContainerEvent.eventID = EventTriggerType.PointerExit;
            _pointerExitLobbyContainerEvent.callback.AddListener((data) => OnMouseExitClearLobbyDetails());

            _noLobbyText = GameObjectFactory.CreateSingleText(lobbyListContainer.transform, "NoLobbyText", "No lobby found.\nTry creating your own lobby!");
            _noLobbyText.rectTransform.sizeDelta = Vector2.one * 200f;
            _noLobbyText.fontSize = 32;
            _noLobbyText.color = new Color(1, 1, 1, .5f);

            _createLobbyButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Create", "LobbyCreateButton", OnCreateLobbyButtonClick);
            _refreshLobbyButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Refresh", "RefreshLobbyButton", OnRefreshLobbyButtonClick);

            _connectButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Connect", "LobbyConnectButton", OnConnectButtonClick);
            _connectButton.gameObject.SetActive(false);
            if (controller.IsDevMode) EnableDevMode();
        }

        private void EnableDevMode()
        {
            _forceConnectButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Force Connect", "FCButton", OnForceConnectButtonClick);
            _shutdownButton = GameObjectFactory.CreateCustomButton(footer.transform, Vector2.zero, new Vector2(150, 75), "Shutdown", "SDButton", OnShutdownButtonClick);
            ToggleEnableDevButtons(false);
        }

        private void ToggleEnableDevButtons(bool isEnabled)
        {
            if (!controller.IsDevMode) return;

            _forceConnectButton.button.enabled = isEnabled;
            _shutdownButton.button.enabled = isEnabled;
        }

        bool debugPassword = false;
        public void DisplayLobbyDebug()
        {
            MultiplayerManager.StopRecursiveRefresh();
            DisplayLobby(new MultiplayerLobbyInfo() { id = "AAAAA", maxPlayerCount = 16, players = new List<MultiplayerUserInfo>(), songInfo = new MultiplayerSongInfo(), state = "SelectingSong", title = $"TEST LOBBY{UnityEngine.Random.Range(1,200)}", hasPassword = debugPassword }, true);
            debugPassword = !debugPassword;
            UpdateScrolling(_lobbyInfoRowsList.Count);
            _noLobbyText.gameObject.SetActive(false);
        }

        public void ShowServerDownText()
        {
            _noLobbyText.gameObject.SetActive(true);
            _noLobbyText.text = "Server is currently under maintenance.\nTry again later.";
            ClearLobbyDetailsText();
        }

        public void ShowNoLobbyText()
        {
            _noLobbyText.gameObject.SetActive(true);
            _noLobbyText.text = "No lobby found.\nTry creating your own lobby!";
            ClearLobbyDetailsText();

        }

        public void SetupForLobbyDisplay()
        {
            _selectedLobby.code = "";
            _noLobbyText.gameObject.SetActive(false);
        }

        public void FinalizeLobbyDisplay()
        {
            if (_selectedLobby.code == "" && _currentInputPrompt != null)
                DestroyInputPrompt();
        }

        public void DisplayLobby(MultiplayerLobbyInfo lobbyinfo) => DisplayLobby(lobbyinfo, true);

        public void DisplayLobby(MultiplayerLobbyInfo lobbyInfo, bool shouldAnimate)
        {
            var lobbyContainer = GameModifierFactory.GetHorizontalBox(new Vector2(0, 120), lobbyListContainer.transform);
            lobbyContainer.GetComponent<Image>().enabled = true;
            _lobbyInfoRowsList.Add(lobbyContainer);
            var button = lobbyContainer.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) => OnMouseEnterDisplayLobbyDetails(lobbyInfo, lobbyContainer));
            button.triggers.Add(pointerEnterEvent);

            EventTrigger.Entry pointerClickEvent = new EventTrigger.Entry();
            pointerClickEvent.eventID = EventTriggerType.PointerClick;
            pointerClickEvent.callback.AddListener((data) => OnMouseClickSelectLobby(lobbyInfo, lobbyContainer, true));
            button.triggers.Add(pointerClickEvent);

            button.triggers.Add(_pointerExitLobbyContainerEvent);
            var test = GameModifierFactory.GetVerticalBox(new Vector2(lobbyInfo.hasPassword ? 1020 : 1084, 0), lobbyContainer.transform);
            var tLayout = test.GetComponent<VerticalLayoutGroup>();
            tLayout.childForceExpandHeight = tLayout.childControlHeight = true;

            var t1 = GameObjectFactory.CreateSingleText(test.transform, "LobbyName", lobbyInfo.title);
            t1.fontStyle = FontStyles.Bold;
            t1.fontSize = 28;

            var t2 = GameObjectFactory.CreateSingleText(test.transform, "LobbyState", lobbyInfo.state);
            if (lobbyInfo.state == "Playing")
                t2.text += $": {lobbyInfo.songInfo.songShortName}";
            t2.fontSizeMax = 36; t2.fontSizeMin = 18;

            t1.alignment = t2.alignment = TextAlignmentOptions.Left;

            var lockedIcon = GameObjectFactory.CreateImageHolder(lobbyContainer.transform, Vector2.zero, Vector2.one * 64f, AssetManager.GetSprite("lock.png"), "LockedLobbyIcon");
            lockedIcon.SetActive(lobbyInfo.hasPassword);

            var test2 = GameModifierFactory.GetVerticalBox(new Vector2(90, 0), lobbyContainer.transform);
            var t2Layout = test2.GetComponent<VerticalLayoutGroup>();
            t2Layout.childForceExpandHeight = t2Layout.childControlHeight = true;

            var t3 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyCount", $"{lobbyInfo.players.Count}/{lobbyInfo.maxPlayerCount}");
            t3.fontSize = 32;
            t3.fontStyle = FontStyles.Bold;

            var t4 = GameObjectFactory.CreateSingleText(test2.transform, "LobbyPing", $"-ms");
            t3.alignment = t4.alignment = TextAlignmentOptions.Right;

            if (shouldAnimate)
            {
                lobbyContainer.transform.eulerAngles = new Vector3(270, 0, 0);
                TootTallyAnimationManager.AddNewEulerAngleAnimation(lobbyContainer, Vector3.zero, 2f, new SecondDegreeDynamicsAnimation(1.25f, 1f, 1f));
            }

            if (_lastSelectedLobbyID == lobbyInfo.id)
                OnMouseClickSelectLobby(lobbyInfo, lobbyContainer, false);

            if (_savedCodeToPing.ContainsKey(lobbyInfo.id))
                t4.text = $"{_savedCodeToPing[lobbyInfo.id]}ms";
            else
                Plugin.Instance.StartCoroutine(SendPing("68.183.206.69", ping =>
                {
                    if (!_savedCodeToPing.TryAdd(lobbyInfo.id, ping))
                        Plugin.LogInfo($"Server ID {lobbyInfo.id} was already pinged.");
                    t4.text = $"{ping}ms";
                }));
        }

        private static IEnumerator<WaitForSeconds> SendPing(string address, Action<int> callback)
        {
            WaitForSeconds waitTime = new WaitForSeconds(.01f);
            Ping pingSender = new Ping(address);
            while (!pingSender.isDone)
            {
                yield return waitTime;
            }
            callback(pingSender.time);
        }

        private float _posYJumpValue = 125f;
        private float _posYOffset = 0f;

        public void OnSliderValueChangeScrollContainer(float value)
        {
            var gridPanelRect = lobbyListContainer.GetComponent<RectTransform>();
            gridPanelRect.anchoredPosition = new Vector2(gridPanelRect.anchoredPosition.x, value * (_lobbyInfoRowsList.Count - 5f) * _posYJumpValue + _posYOffset);
        }

        public void UpdateScrolling(int lobbyCount)
        {
            var enableScrolling = lobbyCount > 4;
            if (!enableScrolling && _scrollingHandler.enabled)
            {
                _scrollingHandler.ResetAcceleration();
                _slider.value = 0;
            }
            _scrollingHandler.enabled = enableScrolling;
            center.transform.Find("Left").GetComponent<HorizontalLayoutGroup>().enabled = !enableScrolling; //only need this to initialize, else it causes scrolling bugs
            _scrollingHandler.accelerationMult = enableScrolling ? 16f / lobbyCount : 1f;

            if (_previousLobbyCount != 0 && _slider.value != 0 && enableScrolling)
                _slider.value *= _previousLobbyCount / lobbyCount;

            _previousLobbyCount = lobbyCount;
        }

        private string _hoverLobbyID;

        public void OnMouseEnterDisplayLobbyDetails(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer)
        {
            _lobbyPlayerListText.text = "<u>Player List</u>\n";
            lobbyInfo.players.ForEach(u => _lobbyPlayerListText.text += $"{u.username}\n");

            if ((_selectedLobbyContainer == null || _hoveredLobbyContainer != lobbyContainer) && lobbyContainer != _selectedLobbyContainer)
            {
                if (_hoverLobbyID != lobbyInfo.id && (_selectedLobby.code == "" || _hoverLobbyID != _selectedLobby.id))
                    controller.GetInstance.sfx_hover.Play();
                _hoveredLobbyContainer = lobbyContainer;
                _hoverLobbyID = lobbyInfo.id;
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(.15f, .15f, .15f, .58f);
            }

        }

        public void OnMouseExitClearLobbyDetails()
        {
            if (_selectedLobby.code != "")
                OnMouseEnterDisplayLobbyDetails(_selectedLobby, _selectedLobbyContainer);
            else
                ClearLobbyDetailsText();

            if (_hoveredLobbyContainer != null)
                _hoveredLobbyContainer.GetComponent<Image>().color = new Color(0, 0, 0, .58f);
            _hoverLobbyID = "";
            _hoveredLobbyContainer = null;
        }

        public void ClearLobbyDetailsText()
        {
            _lobbyPlayerListText.text = "-";
        }

        public void OnMouseClickSelectLobby(MultiplayerLobbyInfo lobbyInfo, GameObject lobbyContainer, bool animateConnect)
        {
            if (_selectedLobby.code == lobbyInfo.code) return;

            if (_selectedLobbyContainer != null)
                _selectedLobbyContainer.GetComponent<Image>().color = new Color(0, 0, 0, .58f);

            _selectedLobby = lobbyInfo;
            _lastSelectedLobbyID = lobbyInfo.id;
            _selectedLobbyContainer = lobbyContainer;

            _selectedLobbyContainer.GetComponent<Image>().color = new Color(0, .35f, 0, .58f);
            _hoveredLobbyContainer = null;

            _connectButtonScaleAnimation?.Dispose();
            ToggleEnableDevButtons(true);
            if (lobbyInfo.players.Count < lobbyInfo.maxPlayerCount)
            {
                _connectButton.gameObject.SetActive(true);
                _connectButton.gameObject.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
                if (animateConnect)
                {
                    _connectButton.transform.localScale = Vector2.zero;
                    _connectButtonScaleAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_connectButton.gameObject, Vector3.one, 1f, new SecondDegreeDynamicsAnimation(2.5f, 0.98f, 1.1f));
                    controller.GetInstance.sfx_hover.Play();
                }
                else
                    _connectButton.transform.localScale = Vector3.one;

            }
        }

        public void OnShutdownButtonClick()
        {
            if (_selectedLobby.code == "" || !controller.IsDevMode) return;

            Plugin.LogInfo($"Shutting down lobby {_selectedLobby.code}");
            TootTallyNotifManager.DisplayNotif($"Shutting down lobby {_selectedLobby.code}");
            Plugin.Instance.StartCoroutine(MultiplayerAPIService.ShutdownMultiplayerServer(_selectedLobby.code, controller.RefreshAllLobbyInfo));
        }

        public void ClearAllLobby()
        {
            _selectedLobby.code = ""; _selectedLobbyContainer = null; _hoveredLobbyContainer = null;
            _connectButton.gameObject.SetActive(false);
            ToggleEnableDevButtons(false);
            _lobbyInfoRowsList.ForEach(GameObject.DestroyImmediate);
            _lobbyInfoRowsList.Clear();
        }

        public void OnCreateLobbyButtonClick()
        {
            _scrollingHandler.enabled = false;
            MultiplayerManager.UpdateMultiplayerState(MultiplayerController.MultiplayerState.CreatingLobby);
            controller.MoveToCreate();
        }

        public void OnConnectButtonClick()
        {
            if (_selectedLobby.code == "") return;

            if (_selectedLobby.hasPassword)
                ShowPasswordInputPrompt();
            else
                controller.ConnectToLobby(_selectedLobby.code);
        }

        public void OnForceConnectButtonClick()
        {
            if (_selectedLobby.code == "" || !controller.IsDevMode) return;

            controller.ConnectToLobby(_selectedLobby.code, "", true);
        }

        public void ShowPasswordInputPrompt()
        {
            _currentInputPrompt = MultiplayerGameObjectFactory.CreatePasswordInputPrompt(canvas.transform, "Enter lobby password", OnConfirmPasswordInputPrompt, OnCancelInputPrompt);
        }

        public void OnConfirmPasswordInputPrompt(string password)
        {
            controller.ConnectToLobby(_selectedLobby.code, password);
        }

        public void OnCancelInputPrompt()
        {
            if (controller.IsConnectionPending) return;
            DestroyInputPrompt();
        }

        public void DestroyInputPrompt()
        {
            GameObject.DestroyImmediate(_currentInputPrompt);
            _currentInputPrompt = null;
        }

        public void OnLobbyConnectSuccess()
        {
            if (_currentInputPrompt != null)
                DestroyInputPrompt();
            _lastSelectedLobbyID = null; _selectedLobby.code = "";

        }

        public void OnLobbyDisconnectError()
        {
            if (_currentInputPrompt != null)
            {
                TootTallyNotifManager.DisplayNotif("Password is incorrect.");
                DestroyInputPrompt();
                return;
            }
            TootTallyNotifManager.DisplayNotif("Unexpected error occured.");
        }

        public void OnRefreshLobbyButtonClick()
        {
            _refreshLobbyButton.button.enabled = false;
            controller.RefreshAllLobbyInfo();
        }

        public void ShowRefreshLobbyButton() => _refreshLobbyButton.button.enabled = true;
    }
}
