using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerCore.PointScore
{
    public class MultiplayerPointScore : MonoBehaviour
    {
        private TMP_Text _positionText, _nameText, _percentText, _scoreText, _maxComboText;

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
            {
                var outline = gameObject.AddComponent<Outline>();
                outline.effectDistance = Vector2.one * 2f;
                outline.effectColor = new Color(.2f, .2f, .2f);
            }
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
            _positionText = GameObjectFactory.CreateSingleText(gameObject.transform, "Position", "#-", Color.white);
            _positionText.rectTransform.sizeDelta = new Vector2(18, 0);

            _nameText = GameObjectFactory.CreateSingleText(gameObject.transform, "Name", "Unknown", Color.white);
            _nameText.rectTransform.sizeDelta = new Vector2(66, 0);

            _percentText = GameObjectFactory.CreateSingleText(gameObject.transform, "Percent", "-%", Color.white);
            _percentText.rectTransform.sizeDelta = new Vector2(30, 0);

            var vBox = MultiplayerGameObjectFactory.AddVerticalBox(gameObject.transform);
            vBox.GetComponent<Image>().enabled = false;
            var vBoxLayout = vBox.GetComponent<VerticalLayoutGroup>();
            vBoxLayout.childControlHeight = vBoxLayout.childForceExpandHeight = false;
            var vBoxRect = vBox.GetComponent<RectTransform>();
            vBoxRect.sizeDelta = new Vector2(60, 0);
            _scoreText = GameObjectFactory.CreateSingleText(vBox.transform, "Score", "-", Color.white);
            _scoreText.rectTransform.sizeDelta = new Vector2(0, 12);

            _maxComboText = GameObjectFactory.CreateSingleText(vBox.transform, "MaxCombo", "-", Color.white);
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
                _animation = TootTallyAnimationManager.AddNewPositionAnimation(gameObject, new Vector3(0, -32 * (count - position)), 1f, GetSecondDegreeAnimation(1.5f)); //Change position here
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
