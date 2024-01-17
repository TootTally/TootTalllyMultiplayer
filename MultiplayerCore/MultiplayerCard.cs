using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Image containerImage;
        public Transform container;
        private Color _defaultColor, _defaultContainerColor;

        public void InitTexts()
        {
            container = transform.GetChild(0);
            textName = container.Find("Name").GetComponent<TMP_Text>();
            textState = container.Find("State").GetComponent<TMP_Text>();
            textRank = container.Find("Rank").GetComponent<TMP_Text>();

            image = gameObject.GetComponent<Image>();
            _defaultColor = image.color;

            containerImage = container.GetComponent<Image>();
            _defaultContainerColor = containerImage.color;
        }

        public void ResetImageColor()
        {
            image.color = _defaultColor;
            containerImage.color = _defaultContainerColor;
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
