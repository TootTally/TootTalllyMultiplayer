using TMPro;
using TootTallyCore;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using TootTallySettings.TootTallySettingsObjects;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyCore.APIServices.SerializableClass;

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
            _userCardPrefab = GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxhorizontal"));
            var horizontalLayout = _userCardPrefab.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = horizontalLayout.childForceExpandWidth = false;

            _userCardPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(705, 75);

            var textName = GameObjectFactory.CreateSingleText(_userCardPrefab.transform, $"Name", $"", Color.white);
            textName.rectTransform.sizeDelta = new Vector2(190, 75);
            textName.alignment = TextAlignmentOptions.Left;

            var textState = GameObjectFactory.CreateSingleText(_userCardPrefab.transform, $"State", $"", Color.white);
            textState.rectTransform.sizeDelta = new Vector2(190, 75);
            textState.alignment = TextAlignmentOptions.Right;

            var textRank = GameObjectFactory.CreateSingleText(_userCardPrefab.transform, $"Rank", $"", Color.white);
            textRank.rectTransform.sizeDelta = new Vector2(190, 75);
            textRank.alignment = TextAlignmentOptions.Right;

            var outline = _userCardPrefab.AddComponent<Outline>();
            outline.effectDistance = Vector2.one * 3f;

            GameObject.DontDestroyOnLoad(_userCardPrefab);
        }

        private static void SetLiveScorePrefab()
        {
            _liveScorePrefab = AddHorizontalBox(null);

            var group = _liveScorePrefab.AddComponent<CanvasGroup>();
            group.alpha = .75f;

            var layout = _liveScorePrefab.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _liveScorePrefab.GetComponent<RectTransform>();
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(1,0);
            rect.sizeDelta = new Vector2(160, 30);
            var mask = GameObject.Instantiate(_liveScorePrefab, _liveScorePrefab.transform);
            mask.name = "Mask";
            mask.AddComponent<LayoutElement>().ignoreLayout = true;
            mask.AddComponent<Mask>().showMaskGraphic = false;
            _liveScorePrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(_liveScorePrefab);
        }

        private static void SetPointScorePrefab()
        {
            _pointScorePrefab = AddHorizontalBox(null);

            var layout = _pointScorePrefab.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childForceExpandWidth = false;
            var rect = _pointScorePrefab.GetComponent<RectTransform>();
            rect.pivot = Vector2.zero;
            rect.anchorMax = rect.anchorMin = new Vector2(.04f, .5f);
            rect.sizeDelta = new Vector2(200, 30);
            _pointScorePrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(_pointScorePrefab);
        }

        private static void SetInputFieldPrefab()
        {
            var inputHolder = new GameObject("InputFieldHolder");
            var rectHolder = inputHolder.AddComponent<RectTransform>();
            rectHolder.anchoredPosition = Vector2.zero;
            rectHolder.sizeDelta = new Vector2(350, 50);
            var inputImageHolder = Object.Instantiate(inputHolder, inputHolder.transform);
            var inputTextHolder = Object.Instantiate(inputImageHolder, inputHolder.transform);
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

            Object.DontDestroyOnLoad(_inputFieldPrefab);
        }

        public static TMP_InputField CreateInputField(Transform canvasTransform, string name, Vector2 size, float fontSize, string text, bool isPassword)
        {
            var inputField = Object.Instantiate(_inputFieldPrefab, canvasTransform);
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

        public static GameObject CreateUserCard(Transform canvasTransform, string username, string state, int rank)
        {
            var userCard = GameObject.Instantiate(_userCardPrefab, canvasTransform);
            userCard.transform.Find("Name").GetComponent<TMP_Text>().text = username;
            userCard.transform.Find("State").GetComponent<TMP_Text>().text = state;
            userCard.transform.Find("Rank").GetComponent<TMP_Text>().text = $"#{rank}";
            return userCard;
        }

        public static GameObject AddVerticalBox(Transform parent) => GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxvertical"), parent);
        public static GameObject AddHorizontalBox(Transform parent) => GameObject.Instantiate(AssetBundleManager.GetPrefab("containerboxhorizontal"), parent);

    }
}
