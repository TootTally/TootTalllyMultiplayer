using System;
using TMPro;
using TootTallyAccounts;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer
{
    public class MultiplayerCard : MonoBehaviour
    {
        public MultiplayerUserInfo user;
        public int hostId = 0;
        public TMP_Text textName, textState, textRank;
        public GameObject teamChanger;
        public Image image;
        public Image containerImage;
        public Transform container;
        private Color _defaultColor, _defaultContainerColor;

        public void Init(Action<dynamic[]> changeTeam)
        {
            container = transform.GetChild(1);
            textName = container.Find("Name").GetComponent<TMP_Text>();
            textState = container.Find("State").GetComponent<TMP_Text>();
            textRank = container.Find("Rank").GetComponent<TMP_Text>();

            teamChanger = transform.GetChild(0).gameObject;
            var teamCount = Enum.GetNames(typeof(MultiplayerTeamState)).Length - 1; // todo remove -1 when grist sets default team
            teamChanger.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (TootTallyUser.userInfo.id == hostId)
                    changeTeam(new dynamic[] { (user.team + 1) % 2, user.id });
                else
                    changeTeam(new dynamic[] { (user.team + 1) % 2 });
            });

            image = gameObject.GetComponent<Image>();
            _defaultColor = image.color;

            containerImage = container.GetComponent<Image>();
            _defaultContainerColor = containerImage.color;
        }

        private void UpdateTeamColor(MultiplayerTeamState team)
        {
            switch (team)
            {
                case MultiplayerTeamState.None:
                    UpdateTeamColor(new Color(1, 1, 1), ""); break;
                case MultiplayerTeamState.Red:
                    UpdateTeamColor(new Color(1, 0, 0), "R"); break;
                case MultiplayerTeamState.Blue:
                    UpdateTeamColor(new Color(0, 0, 1), "B"); break;
            }
        }

        private void UpdateTeamColor(Color mainColor, string text)
        {
            teamChanger.gameObject.GetComponent<Button>().colors = new ColorBlock
            {
                normalColor = mainColor,
                highlightedColor = new Color(mainColor.r + 0.3f, mainColor.g + 0.3f, mainColor.b + 0.3f),
                pressedColor = mainColor,
                disabledColor = mainColor,
                colorMultiplier = 1,
            };
            teamChanger.gameObject.GetComponentInChildren<Text>().text = text;
        }

        public void UpdateUserCard(MultiplayerUserInfo user, string state = null)
        {
            this.user = user;
            image.color = _defaultColor;
            containerImage.color = _defaultContainerColor;
            textName.text = user.username;
            textState.text = state ?? user.state;
            textRank.text = $"#{user.rank}";
            UpdateTeamColor((MultiplayerTeamState)user.team);
        }
    }
}
