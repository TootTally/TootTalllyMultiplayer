using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer
{
    public class MultiplayerCard : MonoBehaviour
    {
        public MultiplayerUserInfo user;
        public TMP_Text textName, textState, textRank, textModifiers;
        public GameObject teamChanger;
        public Image image;
        public Image containerImage;
        public Transform container;
        private Button _teamChangerButton;
        private Text _teamChangerText;
        private Color _defaultColor, _defaultContainerColor;
        private MultiplayerController _controller;
        public static readonly int TEAM_COUNT = Enum.GetNames(typeof(MultiplayerTeamState)).Length;

        public void Init(MultiplayerController controller)
        {
            _controller = controller;
            container = transform.GetChild(1);
            textName = container.Find("Name").GetComponent<TMP_Text>();
            textState = container.Find("State").GetComponent<TMP_Text>();
            textRank = container.Find("Rank").GetComponent<TMP_Text>();
            textModifiers = transform.GetChild(2).Find("Container/Modifiers").GetComponent<TMP_Text>();

            teamChanger = transform.GetChild(0).gameObject;
            _teamChangerText = teamChanger.gameObject.GetComponentInChildren<Text>();
            _teamChangerButton = teamChanger.GetComponent<Button>();
            _teamChangerButton.onClick.AddListener(() =>
            {
                controller.ChangeTeam(new dynamic[] { (user.team + 1) % TEAM_COUNT, user.id });
            });

            image = gameObject.GetComponent<Image>();
            _defaultColor = image.color;

            containerImage = container.GetComponent<Image>();
            _defaultContainerColor = containerImage.color;
        }

        public void UpdateTeamColor(int team)
        {
            user.team = team;
            switch (team)
            {
                case (int)MultiplayerTeamState.Red:
                    UpdateTeamColor(new Color(1, 0, 0), "R");
                    break;
                case (int)MultiplayerTeamState.Blue:
                    UpdateTeamColor(new Color(0, 0, 1), "B");
                    break;
            }
        }

        private void UpdateTeamColor(Color mainColor, string text)
        {
            _teamChangerButton.colors = new ColorBlock
            {
                normalColor = mainColor,
                highlightedColor = new Color(mainColor.r + 0.3f, mainColor.g + 0.3f, mainColor.b + 0.3f),
                pressedColor = mainColor,
                disabledColor = mainColor,
                colorMultiplier = 1,
            };
            _teamChangerText.text = text;
        }

        public void UpdateUserCard(MultiplayerUserInfo user, string state)
        {
            this.user = user;
            image.color = _defaultColor;
            containerImage.color = _defaultContainerColor;
            textName.text = user.username;
            textState.text = state ?? user.state;
            textRank.text = $"#{user.rank}";
            UpdateMods(user.mods);
            UpdateTeamColor(user.team);
        }

        public void UpdateMods(string mods)
        {
            user.mods = mods;
            textModifiers.text = mods;
        }
    }
}
