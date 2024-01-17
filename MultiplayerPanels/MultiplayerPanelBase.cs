using UnityEngine;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public abstract class MultiplayerPanelBase
    {
        public MultiplayerController controller;
        public GameObject canvas, panel, header, center, footer;
        public GameObject headerLeft, headerCenter, headerRight;
        public Vector2 GetPanelPosition => panel.GetComponent<RectTransform>().anchoredPosition;

        public MultiplayerPanelBase(GameObject canvas, MultiplayerController controller, string name)
        {
            this.canvas = canvas;
            this.controller = controller;
            panel = canvas.transform.Find($"{name}").gameObject;

            //Header
            header = panel.transform.GetChild(0).gameObject;
            headerLeft = header.transform.GetChild(0).gameObject;
            headerCenter = header.transform.GetChild(1).gameObject;
            headerRight = header.transform.GetChild(2).gameObject;

            //Center
            center = panel.transform.GetChild(1).gameObject;

            //Footer
            footer = panel.transform.GetChild(2).gameObject;
        }
    }
}
