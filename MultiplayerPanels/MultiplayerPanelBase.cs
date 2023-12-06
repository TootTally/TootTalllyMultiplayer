using UnityEngine;

namespace TootTallyMultiplayer.MultiplayerPanels
{
    public abstract class MultiplayerPanelBase
    {
        public MultiplayerController controller;
        public GameObject canvas, panel, panelFG;
        public Vector2 GetPanelPosition => panel.GetComponent<RectTransform>().anchoredPosition;

        public MultiplayerPanelBase(GameObject canvas, MultiplayerController controller, string name)
        {
            this.canvas = canvas;
            this.controller = controller;
            panel = canvas.transform.Find($"{name}BG").gameObject;
            panelFG = panel.transform.Find($"{name}FG").gameObject;
        }
    }
}
