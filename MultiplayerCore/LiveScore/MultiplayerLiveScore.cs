using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public class MultiplayerLiveScore : MonoBehaviour
    {
        private MultiplayerLiveScoreController _controller;

        private TMP_Text _positionText, _nameText, _scoreText, _comboText;
        private TootTallyAnimation _positionAnimation;
        private GameObject _rainbowMask;
        private Image _rainbow1, _rainbow2, _outlineImage;
        private RectTransform _rainbowMaskRect;
        private CanvasGroup _canvasGroup;
        private int _lastTween;

        private int _id;
        private string _name;
        private int _score, _health, _combo, _position, _count;
        private bool _IsSelf => _id == TootTallyUser.userInfo.id;

        public int GetScore => _score;

        public void Initialize(int id, string name, MultiplayerLiveScoreController controller)
        {
            _id = id;
            if (_IsSelf)
                _outlineImage.color = new Color(.95f, .2f, .95f, .5f);
            _name = name;
            _controller = controller;
        }

        public void Awake()
        {
            _lastTween = 0;
            var container = transform.GetChild(0);
            _rainbowMask = container.Find("Mask").gameObject;
            _rainbowMaskRect = _rainbowMask.GetComponent<RectTransform>();
            _rainbowMaskRect.anchorMin = _rainbowMaskRect.anchorMax = _rainbowMaskRect.pivot = new Vector2(0, 1);

            _canvasGroup = GetComponent<CanvasGroup>();
            _outlineImage = GetComponent<Image>();

            var rainbowHolder1 = GameObjectFactory.CreateImageHolder(_rainbowMask.transform, Vector2.zero, new Vector2(180, 60), AssetManager.GetSprite("rainbow.png"), "Rainbow1");
            var rect = rainbowHolder1.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rainbowHolder1.transform.localPosition = new Vector3(-180, 0, 10);
            rainbowHolder1.AddComponent<LayoutElement>().ignoreLayout = true;
            _rainbow1 = rainbowHolder1.GetComponent<Image>();
            _rainbow1.preserveAspect = false;
            _rainbow1.color = new Color(1, 1, 1, .25f);

            var rainbowHolder2 = GameObject.Instantiate(rainbowHolder1, _rainbowMask.transform);
            rainbowHolder2.name = "Rainbow2";
            rainbowHolder2.transform.localPosition = new Vector3(0, 0, 10);
            _rainbow2 = rainbowHolder2.GetComponent<Image>();

            LeanTween.moveLocalX(rainbowHolder1, 0, 2).setLoopClamp();
            LeanTween.moveLocalX(rainbowHolder2, 180, 2).setLoopClamp();

            _positionText = GameObjectFactory.CreateSingleText(container, "Position", "#-");
            _positionText.rectTransform.sizeDelta = new Vector2(18, 0);

            _nameText = GameObjectFactory.CreateSingleText(container, "Name", "Unknown");
            _nameText.rectTransform.sizeDelta = new Vector2(66, 0);

            var vBox = MultiplayerGameObjectFactory.GetVerticalBox(new Vector2(60, 0), container);
            vBox.GetComponent<Image>().enabled = false;
            var vBoxLayout = vBox.GetComponent<VerticalLayoutGroup>();
            vBoxLayout.childForceExpandHeight = false;
            vBoxLayout.childControlHeight = true;
            _scoreText = GameObjectFactory.CreateSingleText(vBox.transform, "Score", "-");
            _scoreText.rectTransform.sizeDelta = new Vector2(0, 12);

            _comboText = GameObjectFactory.CreateSingleText(vBox.transform, "Combo", "-");
            _comboText.rectTransform.sizeDelta = new Vector2(0, 4);

            SetTextProperties(_positionText, _nameText, _scoreText, _comboText);

            _positionText.alignment = _nameText.alignment = TextAlignmentOptions.Left;

            _positionText.enableAutoSizing = _comboText.enableAutoSizing = false;
            _positionText.fontSize = 14;
            _comboText.fontSize = 8;

        }

        private int _previousHealth;

        public void UpdateScore(int score, int combo, int health)
        {
            _score = score;
            _combo = combo;
            _health = health;

            _rainbowMaskRect.sizeDelta = new Vector2(160 * (health / 100f), 30);

            if (_previousHealth != _health && (_previousHealth == 100 || _health == 100))
                _rainbow1.color = _rainbow2.color = new Color(1, 1, 1, health == 100 ? .85f : .25f);


            _previousHealth = health;
            _nameText.text = _name;
            _scoreText.text = _score.ToString();
            _comboText.text = $"{_combo}x";
        }

        public void SetPosition(int position, int count)
        {
            if (_position != position || _count != count)
            {
                _positionAnimation?.Dispose();
                _positionAnimation = TootTallyAnimationManager.AddNewPositionAnimation(gameObject, new Vector3(0, 32 * (count - position)), 1f, GetSecondDegreeAnimation(1.5f));
                _positionText.text = $"#{position}";
                _count = count;
                _position = position;
            }
        }

        public void SetIsVisible(bool visible, bool animate = true)
        {
            if (!animate)
            {
                _canvasGroup.alpha = visible ? .8f : 0;
                return;
            }

            if (_lastTween != 0)
                LeanTween.cancel(_lastTween);
            _lastTween = LeanTween.alphaCanvas(_canvasGroup, visible ? .8f : 0, .14f).setOnComplete(() => _lastTween = 0).id;
        }

        private static void SetTextProperties(params TMP_Text[] texts)
        {
            foreach (TMP_Text t in texts)
            {
                t.enableWordWrapping = false;
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.Right;
                t.enableAutoSizing = true;
                t.fontSizeMin = 8;
                t.fontSizeMax = 12;
            }
        }

        public void Dispose()
        {
            LeanTween.cancel(_rainbow1.gameObject);
            LeanTween.cancel(_rainbow2.gameObject);
            GameObject.DestroyImmediate(gameObject);
            _controller.Remove(_id);
        }

        private static SecondDegreeDynamicsAnimation GetSecondDegreeAnimation(float speed) => new SecondDegreeDynamicsAnimation(speed, 1f, 1f);
    }
}
