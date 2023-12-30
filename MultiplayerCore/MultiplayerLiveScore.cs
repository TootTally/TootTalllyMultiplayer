using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TootTallyAccounts;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public class MultiplayerLiveScore : MonoBehaviour
    {
        private MultiplayerLiveScoreController _controller;

        private TMP_Text _positionText, _nameText, _scoreText, _comboText;
        private TootTallyAnimation _positionAnimation;
        private GameObject _rainbow1, _rainbow2;

        private int _id;
        private string _name;
        private int _score, _health, _combo, _position, _count;
        private bool _IsSelf => _id == TootTallyUser.userInfo.id;

        public int GetScore => _score;

        public void Initialize(int id, string name, MultiplayerLiveScoreController controller)
        {
            _id = id;
            if (_IsSelf)
            {
                var outline = gameObject.AddComponent<Outline>();
                outline.effectDistance = Vector2.one * 2f;
                outline.effectColor = new Color(.2f, .2f, .2f);
            }

            _name = name;
            _controller = controller;
        }

        public void Awake()
        {
            var mask = transform.Find("Mask");

            _rainbow1 = GameObjectFactory.CreateImageHolder(mask, Vector2.zero, new Vector2(180, 60), AssetManager.GetSprite("rainbow2.png"), "Rainbow1");
            _rainbow1.transform.localPosition = new Vector3(-270, 0, 10);
            _rainbow1.AddComponent<LayoutElement>().ignoreLayout = true;
            var r1Image = _rainbow1.GetComponent<Image>();
            r1Image.preserveAspect = false;
            r1Image.color = new Color(1, 1, 1, .85f);
            _rainbow1.SetActive(false);

            _rainbow2 = GameObjectFactory.CreateImageHolder(mask, Vector2.zero, new Vector2(180, 60), AssetManager.GetSprite("rainbow2.png"), "Rainbow2");
            _rainbow2.transform.localPosition = new Vector3(-90, 0, 10);
            _rainbow2.AddComponent<LayoutElement>().ignoreLayout = true;
            var r2Image = _rainbow2.GetComponent<Image>();
            r2Image.color = new Color(1, 1, 1, .85f);
            r2Image.preserveAspect = false;
            _rainbow2.SetActive(false);

            LeanTween.moveLocalX(_rainbow1, -90, 2).setLoopClamp();
            LeanTween.moveLocalX(_rainbow2, 90, 2).setLoopClamp();

            

            _positionText = GameObjectFactory.CreateSingleText(gameObject.transform, "Position", "#-", Color.white);
            _positionText.rectTransform.sizeDelta = new Vector2(18, 0);

            _nameText = GameObjectFactory.CreateSingleText(gameObject.transform, "Name", "Unknown", Color.white);
            _nameText.rectTransform.sizeDelta = new Vector2(66, 0);

            var vBox = MultiplayerGameObjectFactory.AddVerticalBox(gameObject.transform);
            vBox.GetComponent<Image>().enabled = false;
            var vBoxLayout = vBox.GetComponent<VerticalLayoutGroup>();
            vBoxLayout.childControlHeight = vBoxLayout.childForceExpandHeight = false;
            var vBoxRect = vBox.GetComponent<RectTransform>();
            vBoxRect.sizeDelta = new Vector2(60, 0);
            _scoreText =  GameObjectFactory.CreateSingleText(vBox.transform, "Score", "-", Color.white);
            _scoreText.rectTransform.sizeDelta = new Vector2(0, 12);

            _comboText = GameObjectFactory.CreateSingleText(vBox.transform, "Combo", "-", Color.white);
            _comboText.rectTransform.sizeDelta = new Vector2(0, 4);

            SetTextProperties(_positionText, _nameText, _scoreText, _comboText);

            _positionText.alignment = _nameText.alignment = TextAlignmentOptions.Left;

            _positionText.enableAutoSizing = _comboText.enableAutoSizing = false;
            _positionText.fontSize = 14;
            _comboText.fontSize = 8;

        }

        public void UpdateScore(int score, int combo, int health)
        {
            _score = score;
            _combo = combo;
            _health = health;

            //I think Ill keep it for your own score too...
            var shouldDisplayRainbow = /*!_IsSelf &&*/ health >= 100;
            _rainbow1.SetActive(shouldDisplayRainbow);
            _rainbow2.SetActive(shouldDisplayRainbow);

            _nameText.text = _name;
            _scoreText.text = _score.ToString();
            _comboText.text = $"{_combo}x";
        }
        
        public void SetPosition(int position, int count)
        {
            if (_position != position || _count != count)
            {
                _positionAnimation?.Dispose();
                _positionAnimation = TootTallyAnimationManager.AddNewPositionAnimation(gameObject, new Vector3(0, 32 * (count-position)), 1f, GetSecondDegreeAnimation(1.5f));
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

        public void Dispose()
        {
            LeanTween.cancel(_rainbow1);
            LeanTween.cancel(_rainbow2);
            GameObject.DestroyImmediate(gameObject);
            _controller.Remove(_id);
        }

        private static SecondDegreeDynamicsAnimation GetSecondDegreeAnimation(float speed) => new SecondDegreeDynamicsAnimation(speed, 1f, 1f);
    }
}
