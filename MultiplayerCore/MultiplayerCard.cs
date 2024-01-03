using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TootTallyCore.Graphics;
using TootTallyMultiplayer.MultiplayerPanels;
using UnityEngine;
using UnityEngine.UI;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer
{
    public class MultiplayerCard : MonoBehaviour
    {
        public MultiplayerUserInfo user;
        public TMP_Text textName, textState, textRank;
        public Image image;
        private Color _defaultColor;

        public void InitTexts()
        {
            textName = transform.Find("Name").GetComponent<TMP_Text>();
            textState = transform.Find("State").GetComponent<TMP_Text>();
            textRank = transform.Find("Rank").GetComponent<TMP_Text>();
            image = gameObject.GetComponent<Image>();
            _defaultColor = image.color;
        }

        public void ResetImageColor()
        {
            image.color = _defaultColor;
        }

        public void UpdateUserInfo(MultiplayerUserInfo user, string state = null)
        {
            this.user = user;
            ResetImageColor();
            SetTexts(user.username, state ?? user.state, user.rank);
        }

        public void SetTexts(string username, string state, int rank)
        {
            textName.text = username;
            textState.text = state;
            textRank.text = $"#{rank}";
        }
    }
}
