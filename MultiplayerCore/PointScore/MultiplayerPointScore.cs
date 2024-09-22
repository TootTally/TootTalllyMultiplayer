using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyGameModifiers;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerCore.PointScore
{
    public class MultiplayerPointScore : MonoBehaviour
    {
        private TMP_Text _positionText, _nameText, _percentText, _scoreText, _maxComboText;

        private Image _outlineImage;

        private string _name;
        private int _id, _score, _maxCombo, _position, _count;
        private float _percent;
        private int[] _noteTally;

        private TootTallyAnimation _animation;

        private bool _IsSelf => _id == TootTallyUser.userInfo.id;

        public int GetScore => _score;

        public void Initialize(int id, string name, int score, float percent, int maxCombo, int[] noteTally)
        {
            _id = id;
            if (_IsSelf)
                _outlineImage.color = new Color(.95f, .2f, .95f, .5f);
            _name = name;
            _score = score;
            _percent = percent;
            _maxCombo = maxCombo;
            _noteTally = noteTally;

            _nameText.text = _name;
            _scoreText.text = _score.ToString();
            _percentText.text = $"{_percent:0.00}%";
            _maxComboText.text = $"{_maxCombo}x";
        }

        public void Awake()
        {
            var container = transform.GetChild(0);
            _outlineImage = GetComponent<Image>();

            _positionText = GameObjectFactory.CreateSingleText(container, "Position", "#-");
            _positionText.rectTransform.sizeDelta = new Vector2(18, 0);

            _nameText = GameObjectFactory.CreateSingleText(container, "Name", "Unknown");
            _nameText.rectTransform.sizeDelta = new Vector2(66, 0);

            _percentText = GameObjectFactory.CreateSingleText(container, "Percent", "-%");
            _percentText.rectTransform.sizeDelta = new Vector2(30, 0);

            var vBox = GameModifierFactory.GetVerticalBox(new Vector2(60, 0), container);
            vBox.GetComponent<Image>().enabled = false;
            var vBoxLayout = vBox.GetComponent<VerticalLayoutGroup>();
            vBoxLayout.childForceExpandHeight = false;
            vBoxLayout.childControlHeight = true;
            _scoreText = GameObjectFactory.CreateSingleText(vBox.transform, "Score", "-");
            _scoreText.rectTransform.sizeDelta = new Vector2(0, 12);

            _maxComboText = GameObjectFactory.CreateSingleText(vBox.transform, "MaxCombo", "-");
            _maxComboText.rectTransform.sizeDelta = new Vector2(0, 4);

            SetTextProperties(_positionText, _nameText, _scoreText, _percentText, _maxComboText);

            _positionText.alignment = _nameText.alignment = TextAlignmentOptions.Left;

            _positionText.enableAutoSizing = _maxComboText.enableAutoSizing = false;
            _positionText.fontSize = 14;
            _maxComboText.fontSize = 8;

        }

        public void SetPosition(int position, int count)
        {
            if (_position != position || _count != count)
            {
                _animation?.Dispose();
                _animation = TootTallyAnimationManager.AddNewPositionAnimation(gameObject, new Vector3(0, -32 * (position - 1)), 1f, GetSecondDegreeAnimation(1.5f)); //Change position here
                _positionText.text = $"#{position}";
                _count = count;
                _position = position;
            }
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

        private static SecondDegreeDynamicsAnimation GetSecondDegreeAnimation(float speed) => new SecondDegreeDynamicsAnimation(speed, 1f, 1f);

    }
}
