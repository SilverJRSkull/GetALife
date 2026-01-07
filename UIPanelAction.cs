using System.Collections.Generic;
using UnityEngine;

namespace GetALife.UI
{
    public class UIPanelAction : MonoBehaviour
    {
        [SerializeField] private UIPanelController panelController;
        [SerializeField] private List<GameObject> panelsToShow = new List<GameObject>();
        [SerializeField] private List<GameObject> panelsToHide = new List<GameObject>();

        public void Apply()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.ShowPanels(panelsToShow);
            panelController.HidePanels(panelsToHide);
        }
    }
}