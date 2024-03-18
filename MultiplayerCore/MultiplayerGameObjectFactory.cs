using System;
using TMPro;
using TootTallyCore;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using TootTallySettings.TootTallySettingsObjects;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer
{
    public static class MultiplayerGameObjectFactory
    {
        private static TMP_InputField _inputFieldPrefab;
        private static GameObject _userCardPrefab, _liveScorePrefab, _pointScorePrefab;

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
            _userCardPrefab = GameObject.Instantiate(GetBorderedHorizontalBox(new Vector2(700, 72), 3));
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

            GameObject.DontDestroyOnLoad(_userCardPrefab);
        }

        private static void SetLiveScorePrefab()
        {
            _liveScorePrefab = GetBorderedHorizontalBox(new Vector2(160, 28), 2);

            var group = _liveScorePrefab.AddComponent<CanvasGroup>();
            group.alpha = .75f;

            var container = _liveScorePrefab.transform.GetChild(0).gameObject;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _liveScorePrefab.GetComponent<RectTransform>();
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(1,0);
            var mask = GameObject.Instantiate(container, container.transform);
            mask.name = "Mask";
            mask.AddComponent<LayoutElement>().ignoreLayout = true;
            mask.AddComponent<Mask>().showMaskGraphic = false;
            _liveScorePrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(_liveScorePrefab);
        }

        private static void SetPointScorePrefab()
        {
            _pointScorePrefab = GetBorderedHorizontalBox(new Vector2(200, 28), 2);

            var container = _pointScorePrefab.transform.GetChild(0).gameObject;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _pointScorePrefab.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0,1);
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

        public static TMP_InputField CreateInputField(Transform canvasTransform, string name, Vector2 size, float fontSize, string text, bool isPassword)
        {
            var inputField = GameObject.Instantiate(_inputFieldPrefab, canvasTransform);
            inputField.name = name;
            inputField.GetComponent<RectTransform>().sizeDelta = size;
            inputField.transform.Find("Image").GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, 2);
            inputField.transform.Find("Text").GetComponent<RectTransform>().sizeDelta = size;
            inputField.textComponent.GetComponent<RectTransform>().sizeDelta = size;
            inputField.textComponent.fontSize = fontSize;
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
            var pointScoreObject = GameObject.Instantiate(_pointScorePrefab,canvasTransform);
            pointScoreObject.SetActive(true);
            pointScoreObject.GetComponent <RectTransform>().anchoredPosition = position;
            pointScoreObject.name = name;
            return pointScoreObject;
        }

        public static MultiplayerCard CreateUserCard(Transform canvasTransform)
        {
            var userCard = GameObject.Instantiate(_userCardPrefab, canvasTransform).AddComponent<MultiplayerCard>();
            userCard.InitTexts();
            return userCard;
        }

        public static GameObject CreatePasswordInputPrompt(Transform canvasTransform, string titleText, Action<string> OnConfirm, Action OnCancel)
        {
            var borderedBox = GetBorderedVerticalBox(new Vector2(900,250), 4, canvasTransform);
            var promptRect = borderedBox.GetComponent<RectTransform>();
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = Vector2.one / 2f;

            var borderedBoxContainer = borderedBox.transform.GetChild(0).gameObject;
            var title = GameObjectFactory.CreateSingleText(borderedBoxContainer.transform, "TitleText", titleText);
            title.rectTransform.sizeDelta = new Vector2(0, 65);

            var inputHorizontalBox = GetHorizontalBox(new Vector2(0, 65), borderedBoxContainer.transform);
            var hlayout = inputHorizontalBox.GetComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 8f;
            hlayout.padding = new RectOffset(0, 0, 3, 0);
            var inputFieldLabel = GameObjectFactory.CreateSingleText(inputHorizontalBox.transform, "InputFieldLabel", "Password:");
            inputFieldLabel.rectTransform.sizeDelta = new Vector2(115, 65);
            inputFieldLabel.alignment = TextAlignmentOptions.BottomRight;
            var inputField = CreateInputField(inputHorizontalBox.transform, "InputField", new Vector2(275, 30), 24, "", true);

            var buttonHorizontalBox = GetHorizontalBox(new Vector2(0, 100), borderedBoxContainer.transform);
            var hLayout2 = buttonHorizontalBox.GetComponent<HorizontalLayoutGroup>();
            hLayout2.spacing = 100f;
            hLayout2.childControlHeight = hLayout2.childForceExpandHeight = false;
            var confirmButton = GameObjectFactory.CreateCustomButton(buttonHorizontalBox.transform, Vector2.zero, new Vector2(170, 65), "Confirm", "ConfirmButton", delegate { OnConfirm?.Invoke(inputField.text); });
            var cancelButton = GameObjectFactory.CreateCustomButton(buttonHorizontalBox.transform, Vector2.zero, new Vector2(170, 65), "Cancel", "CancelButton", OnCancel);

            return borderedBox;
        }

        public static GameObject GetVerticalBox(Vector2 size, Transform parent = null)
        {
            var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("verticalbox"), parent);
            box.GetComponent<RectTransform>().sizeDelta = size;
            return box;
        }
        public static GameObject GetHorizontalBox(Vector2 size, Transform parent = null)
        {
            var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("horizontalbox"), parent);
            box.GetComponent<RectTransform>().sizeDelta = size;
            return box;
        }

        public static GameObject GetBorderedVerticalBox(Vector2 size, int bordersize, Transform parent = null)
        {
            var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("borderedverticalbox"), parent);
            box.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(bordersize, bordersize, bordersize, bordersize);
            box.GetComponent<RectTransform>().sizeDelta = size + (Vector2.one * 2f * bordersize);
            return box;
        }

        public static GameObject GetBorderedHorizontalBox(Vector2 size, int bordersize, Transform parent = null)
        {
            var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("borderedhorizontalbox"), parent);
            box.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(bordersize, bordersize, bordersize, bordersize);
            box.GetComponent<RectTransform>().sizeDelta = size + (Vector2.one * 2f * bordersize);
            return box;
        }
    }
}
