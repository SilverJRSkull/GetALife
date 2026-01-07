using System.Collections.Generic;
using UnityEngine;

namespace GetALife.UI
{
    public class UIPanelController : MonoBehaviour
    {
        [SerializeField] private List<GameObject> trackedPanels = new List<GameObject>();

        private readonly Stack<GameObject> history = new Stack<GameObject>();

        public void ShowOnly(GameObject panelToShow, List<GameObject> panelsToHide)
        {
            NavigateTo(panelToShow, panelsToHide);
        }

        public void ShowPanels(List<GameObject> panelsToShow)
        {
            RegisterPanels(panelsToShow);
            SetPanelsActive(panelsToShow, true);
        }

        public void HidePanels(List<GameObject> panelsToHide)
        {
            RegisterPanels(panelsToHide);
            SetPanelsActive(panelsToHide, false);
        }

        public void NavigateTo(GameObject panelToShow, List<GameObject> panelsToHide)
        {
            GameObject currentPanel = GetActiveTrackedPanel();
            if (currentPanel == null)
            {
                currentPanel = GetActivePanel(panelsToHide);
            }

            if (currentPanel != null && currentPanel != panelToShow)
            {
                history.Push(currentPanel);
            }

            RegisterPanel(panelToShow);
            RegisterPanels(panelsToHide);

            if (panelToShow != null)
            {
                panelToShow.SetActive(true);
            }

            SetPanelsActive(panelsToHide, false);
        }

        public void GoBack()
        {
            if (history.Count == 0)
            {
                return;
            }

            GameObject previousPanel = history.Pop();
            SetPanelsActive(trackedPanels, false);
            if (previousPanel != null)
            {
                previousPanel.SetActive(true);
            }
        }

        private void RegisterPanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            if (!trackedPanels.Contains(panel))
            {
                trackedPanels.Add(panel);
            }
        }

        private void RegisterPanels(List<GameObject> panels)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                RegisterPanel(panels[i]);
            }
        }

        private void SetPanelsActive(List<GameObject> panels, bool isActive)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null)
                {
                    panels[i].SetActive(isActive);
                }
            }
        }

        private GameObject GetActiveTrackedPanel()
        {
            for (int i = 0; i < trackedPanels.Count; i++)
            {
                if (trackedPanels[i] != null && trackedPanels[i].activeSelf)
                {
                    return trackedPanels[i];
                }
            }

            return null;
        }

        private GameObject GetActivePanel(List<GameObject> panels)
        {
            if (panels == null)
            {
                return null;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && panels[i].activeSelf)
                {
                    return panels[i];
                }
            }

            return null;
        }
    }
}