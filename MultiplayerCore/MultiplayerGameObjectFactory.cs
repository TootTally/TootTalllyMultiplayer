﻿using System;
using System.Linq;
using TMPro;
using TootTallyCore;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using TootTallyGameModifiers;
using TootTallyMultiplayer.MultiplayerCore.InputPrompts;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyMultiplayer.MultiplayerSystem;

namespace TootTallyMultiplayer
{
    public static class MultiplayerGameObjectFactory
    {
        private static TMP_InputField _inputFieldPrefab;
        private static GameObject _userCardPrefab, _liveScorePrefab, _pointScorePrefab;
        private static Toggle _togglePrefab;

        private static bool _isInitialized;

        public static void Initialize()
        {
            if (_isInitialized) return;

            SetUserCardPrefab();
            SetInputFieldPrefab();
            SetLiveScorePrefab();
            SetPointScorePrefab();
            _isInitialized = true;
        }

        private static void SetUserCardPrefab()
        {
            _userCardPrefab = GameObject.Instantiate(GameModifierFactory.GetBorderedHorizontalBox(new Vector2(790, 72), 3));
            var teamChanger = GameObjectFactory.CreateCustomButton(_userCardPrefab.transform, Vector2.zero, new Vector2(25, 65), "R", "TeamChanger");
            var container = _userCardPrefab.transform.GetChild(0).gameObject;
            var horizontalLayout = container.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = horizontalLayout.childForceExpandWidth = false;

            var textName = GameObjectFactory.CreateSingleText(container.transform, $"Name", $"", Vector2.one / 2f, new Vector2(190, 75), Theme.colors.leaderboard.text);
            textName.alignment = TextAlignmentOptions.Left;

            var textState = GameObjectFactory.CreateSingleText(container.transform, $"State", $"", Vector2.one / 2f, new Vector2(190, 75), Theme.colors.leaderboard.text);
            textState.alignment = TextAlignmentOptions.Right;

            var textRank = GameObjectFactory.CreateSingleText(container.transform, $"Rank", $"", Vector2.one / 2f, new Vector2(190, 75), Theme.colors.leaderboard.text);
            textRank.alignment = TextAlignmentOptions.Right;

            var modBox = GameModifierFactory.GetBorderedHorizontalBox(new Vector2(70, 72), 0, _userCardPrefab.transform);
            var container2 = modBox.transform.GetChild(0).gameObject;
            var textModifiers = GameObjectFactory.CreateSingleText(container2.transform, $"Modifiers", $"", Vector2.one / 2f, new Vector2(70, 75), Theme.colors.leaderboard.text);
            textModifiers.alignment = TextAlignmentOptions.Center;

            teamChanger.transform.SetAsFirstSibling();
            GameObject.DontDestroyOnLoad(_userCardPrefab);
        }

        private static void SetLiveScorePrefab()
        {
            _liveScorePrefab = GameModifierFactory.GetBorderedHorizontalBox(new Vector2(160, 28), 2);

            var group = _liveScorePrefab.AddComponent<CanvasGroup>();
            group.alpha = .75f;

            var container = _liveScorePrefab.transform.GetChild(0).gameObject;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _liveScorePrefab.GetComponent<RectTransform>();
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(1, 0);
            var mask = GameObject.Instantiate(container, container.transform);
            mask.name = "Mask";
            mask.AddComponent<LayoutElement>().ignoreLayout = true;
            mask.AddComponent<Mask>().showMaskGraphic = false;
            _liveScorePrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(_liveScorePrefab);
        }

        private static void SetPointScorePrefab()
        {
            _pointScorePrefab = GameModifierFactory.GetBorderedHorizontalBox(new Vector2(200, 28), 2);

            var container = _pointScorePrefab.transform.GetChild(0).gameObject;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _pointScorePrefab.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1);
            rect.anchorMax = rect.anchorMin = new Vector2(.04f, .926f);
            _pointScorePrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(_pointScorePrefab);
        }

        private static void SetInputFieldPrefab()
        {
            var inputHolder = new GameObject("InputFieldHolder");
            var rectHolder = inputHolder.AddComponent<RectTransform>();
            rectHolder.anchoredPosition = Vector2.zero;
            rectHolder.sizeDelta = new Vector2(350, 50);
            var inputImageHolder = GameObject.Instantiate(inputHolder, inputHolder.transform);
            var inputTextHolder = GameObject.Instantiate(inputImageHolder, inputHolder.transform);
            inputImageHolder.name = "Image";
            inputTextHolder.name = "Text";

            _inputFieldPrefab = inputHolder.AddComponent<TMP_InputField>();

            rectHolder.anchorMax = rectHolder.anchorMin = Vector2.zero;

            _inputFieldPrefab.image = inputImageHolder.AddComponent<Image>();
            RectTransform rectImage = inputImageHolder.GetComponent<RectTransform>();

            rectImage.anchorMin = rectImage.anchorMax = rectImage.pivot = Vector2.zero;
            rectImage.anchoredPosition = new Vector2(0, 4);
            rectImage.sizeDelta = new Vector2(350, 2);

            RectTransform rectText = inputTextHolder.GetComponent<RectTransform>();
            rectText.anchoredPosition = rectText.anchorMin = rectText.anchorMax = rectText.pivot = Vector2.zero;
            rectText.sizeDelta = new Vector2(350, 50);

            _inputFieldPrefab.textComponent = GameObjectFactory.CreateSingleText(inputTextHolder.transform, $"TextLabel", "", Theme.colors.leaderboard.text);
            _inputFieldPrefab.textComponent.rectTransform.pivot = new Vector2(0, 0.5f);
            _inputFieldPrefab.textComponent.alignment = TextAlignmentOptions.Left;
            _inputFieldPrefab.textComponent.margin = new Vector4(5, 0, 0, 0);
            _inputFieldPrefab.textComponent.enableWordWrapping = true;
            _inputFieldPrefab.textViewport = _inputFieldPrefab.textComponent.rectTransform;

            GameObject.DontDestroyOnLoad(_inputFieldPrefab);
        }

        public static void SetTogglePrefab(HomeController __instance)
        {
            _togglePrefab = GameObject.Instantiate(__instance.set_tog_accessb_jumpscare);
            _togglePrefab.name = "MultiplayerTogglePrefab";
            _togglePrefab.onValueChanged = new Toggle.ToggleEvent();

            GameObject.DontDestroyOnLoad(_togglePrefab);
        }

        public static TMP_InputField CreateInputField(Transform canvasTransform, string name, Vector2 size, float fontSize, string text, bool isPassword)
        {
            var inputField = GameObject.Instantiate(_inputFieldPrefab, canvasTransform);
            inputField.name = name;
            inputField.GetComponent<RectTransform>().sizeDelta = size;
            inputField.transform.Find("Image").GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, 2);
            inputField.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = size;
            inputField.textComponent.GetComponent<RectTransform>().sizeDelta = size;
            inputField.textComponent.fontSize = fontSize;
            inputField.textComponent.overflowMode = TextOverflowModes.Ellipsis;
            inputField.text = text;
            inputField.inputType = isPassword ? TMP_InputField.InputType.Password : TMP_InputField.InputType.Standard;

            return inputField;
        }

        public static GameObject CreateLiveScoreCard(Transform canvasTransform, Vector2 position, string name)
        {
            var liveScoreObject = GameObject.Instantiate(_liveScorePrefab, canvasTransform);
            liveScoreObject.SetActive(true);
            liveScoreObject.GetComponent<RectTransform>().anchoredPosition = position;
            liveScoreObject.name = name;
            return liveScoreObject;
        }

        public static GameObject CreatePointScoreCard(Transform canvasTransform, Vector2 position, string name)
        {
            var pointScoreObject = GameObject.Instantiate(_pointScorePrefab, canvasTransform);
            pointScoreObject.SetActive(true);
            pointScoreObject.GetComponent<RectTransform>().anchoredPosition = position;
            pointScoreObject.name = name;
            return pointScoreObject;
        }

        public static MultiplayerCard CreateUserCard(Transform canvasTransform, Action<dynamic[]> changeTeam)
        {
            var userCard = GameObject.Instantiate(_userCardPrefab, canvasTransform).AddComponent<MultiplayerCard>();
            userCard.Init(changeTeam);
            return userCard;
        }

        public static GameObject CreatePasswordInputPrompt(Transform canvasTransform, string titleText, Action<string> OnConfirm, Action OnCancel)
        {
            var borderedBox = GameModifierFactory.GetBorderedVerticalBox(new Vector2(520, 235), 4, canvasTransform);
            var promptRect = borderedBox.GetComponent<RectTransform>();
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = Vector2.one / 2f;

            var borderedBoxContainer = borderedBox.transform.GetChild(0).gameObject;
            var vlayout = borderedBoxContainer.GetComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 21;
            vlayout.padding = new RectOffset(5, 5, 35, 5);
            var title = GameObjectFactory.CreateSingleText(borderedBoxContainer.transform, "TitleText", titleText);
            title.fontSize = 26;
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.sizeDelta = new Vector2(0, 32);

            var inputHorizontalBox = GameModifierFactory.GetHorizontalBox(new Vector2(0, 30), borderedBoxContainer.transform);
            var hlayout = inputHorizontalBox.GetComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 8f;
            hlayout.padding = new RectOffset(0, 0, 3, 0);
            var inputFieldLabel = GameObjectFactory.CreateSingleText(inputHorizontalBox.transform, "InputFieldLabel", "Password:");
            inputFieldLabel.rectTransform.sizeDelta = new Vector2(115, 30);
            inputFieldLabel.alignment = TextAlignmentOptions.BottomRight;
            var inputField = CreateInputField(inputHorizontalBox.transform, "InputField", new Vector2(275, 30), 24, "", true);

            var buttonHorizontalBox = GameModifierFactory.GetHorizontalBox(new Vector2(0, 66), borderedBoxContainer.transform);
            var hLayout2 = buttonHorizontalBox.GetComponent<HorizontalLayoutGroup>();
            hLayout2.spacing = 100f;
            hLayout2.childControlHeight = hLayout2.childForceExpandHeight = false;
            var confirmButton = GameObjectFactory.CreateCustomButton(buttonHorizontalBox.transform, Vector2.zero, new Vector2(170, 65), "Confirm", "ConfirmButton", delegate { OnConfirm?.Invoke(inputField.text); });
            var cancelButton = GameObjectFactory.CreateCustomButton(buttonHorizontalBox.transform, Vector2.zero, new Vector2(170, 65), "Cancel", "CancelButton", OnCancel);

            return borderedBox;
        }

        public static LobbySettingsInputPrompt CreateLobbySettingsInputPrompt(Transform canvasTransform, MultiplayerController controller)
        {
            var lobbySettings = new LobbySettingsInputPrompt(canvasTransform, controller);
            lobbySettings.gameObject.transform.localScale = new Vector3(0, 0, 1);
            return lobbySettings;
        }

        public static CustomPopup CreateQuickChatPopup(Transform buttonTransform, Transform popupTransform, Action<QuickChat> OnBtnClickCallback)
        {
            var customPopup = new CustomPopup("Quick Chat", buttonTransform, Vector2.zero, new Vector2(64, 64), AssetManager.GetSprite("Bubble.png"), popupTransform, new Vector2(450, 700), 38, new Vector2(32, 32));
            var quickChatContainer = customPopup.popupBox.transform.GetChild(0).gameObject;

            var nullColor = new Color(0, 0, 0, 0);
            for (int height = 0; height < 3; height++)
            {
                var hContainer = GameModifierFactory.GetHorizontalBox(new Vector2(0, 205), quickChatContainer.transform);
                hContainer.GetComponent<HorizontalLayoutGroup>().spacing = 20;
                for (int width = 0; width < 2; width++)
                {
                    var buttonContainer = GameModifierFactory.GetVerticalBox(new Vector2(190, 0), hContainer.transform);
                    var buttonLayout = buttonContainer.GetComponent<VerticalLayoutGroup>();
                    buttonLayout.spacing = 1;
                    for (int i = 0; i < 4; i++)
                    {
                        var index = (height * 8) + (width * 4) + i;
                        var quickChatOption = QuickChatToTextDic.ElementAt(index).Key;
                        var btnText = string.Concat(quickChatOption.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                        var btn = GameObjectFactory.CreateCustomButton(buttonContainer.transform, Vector2.zero, new Vector2(0, 36), btnText, $"QCButton{quickChatOption}", delegate { OnBtnClickCallback(quickChatOption); });
                        var colors = btn.button.colors;
                        colors.normalColor = nullColor;
                        btn.button.colors = colors;
                    }
                }
            }

            return customPopup;
        }

        public static Toggle CreateToggle(Transform canvasTransform, string name, Vector2 size, string text)
        {
            var toggle = GameObject.Instantiate(_togglePrefab, canvasTransform);
            RectTransform rect = toggle.GetComponent<RectTransform>();
            rect.pivot = Vector3.zero;
            rect.anchoredPosition = Vector3.zero;
            rect.sizeDelta = size;

            var label = GameObjectFactory.CreateSingleText(toggle.transform, $"{name}Label", text, Vector2.zero, new Vector2(250, 0), Theme.colors.leaderboard.text, GameObjectFactory.TextFont.Multicolore);
            label.alignment = TextAlignmentOptions.Left;
            label.fontStyle = FontStyles.Underline;
            label.enableWordWrapping = false;
            label.rectTransform.anchoredPosition = new Vector2(20, 0);
            label.rectTransform.anchorMax = label.rectTransform.anchorMin = new Vector2(1, .5f);
            label.fontSize = 28;
            label.text = text;
            toggle.name = name;
            toggle.isOn = false;

            return toggle;
        }
    }
}
