using System.Collections.Generic;
using UnityEngine;

namespace GetALife.UI
{
    public class UIPanelController : MonoBehaviour
    {
        [SerializeField] private List<GameObject> trackedPanels = new List<GameObject>();

        private readonly Stack<List<GameObject>> history = new Stack<List<GameObject>>();

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
            List<GameObject> currentPanels = GetActiveTrackedPanels();
            if (currentPanels.Count == 0)
            {
                currentPanels = GetActivePanels(panelsToHide);
            }

            if (currentPanels.Count > 0 && (currentPanels.Count != 1 || currentPanels[0] != panelToShow))
            {
                history.Push(currentPanels);
            }

            RegisterPanel(panelToShow);
            RegisterPanels(panelsToHide);

            if (panelToShow != null)
            {
                panelToShow.SetActive(true);
            }

            SetPanelsActive(panelsToHide, false);
        }

        public void NavigateToPanels(List<GameObject> panelsToShow, List<GameObject> panelsToHide)
        {
            List<GameObject> currentPanels = GetActiveTrackedPanels();
            if (currentPanels.Count == 0)
            {
                currentPanels = GetActivePanels(panelsToHide);
            }

            if (currentPanels.Count > 0 && !AreSamePanels(currentPanels, panelsToShow))
            {
                history.Push(currentPanels);
            }

            RegisterPanels(panelsToShow);
            RegisterPanels(panelsToHide);

            SetPanelsActive(panelsToShow, true);
            SetPanelsActive(panelsToHide, false);
        }

        public void GoBack()
        {
            if (history.Count == 0)
            {
                return;
            }

            List<GameObject> previousPanels = history.Pop();
            SetPanelsActive(trackedPanels, false);
            SetPanelsActive(previousPanels, true);
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

        private List<GameObject> GetActiveTrackedPanels()
        {
            List<GameObject> activePanels = new List<GameObject>();
            for (int i = 0; i < trackedPanels.Count; i++)
            {
                if (trackedPanels[i] != null && trackedPanels[i].activeSelf)
                {
                    activePanels.Add(trackedPanels[i]);
                }
            }

            return activePanels;
        }

        private static bool AreSamePanels(List<GameObject> leftPanels, List<GameObject> rightPanels)
        {
            HashSet<GameObject> leftSet = BuildPanelSet(leftPanels);
            HashSet<GameObject> rightSet = BuildPanelSet(rightPanels);
            if (leftSet.Count != rightSet.Count)
            {
                return false;
            }

            foreach (GameObject panel in leftSet)
            {
                if (!rightSet.Contains(panel))
                {
                    return false;
                }
            }

            return true;
        }

        private static HashSet<GameObject> BuildPanelSet(List<GameObject> panels)
        {
            HashSet<GameObject> panelSet = new HashSet<GameObject>();
            if (panels == null)
            {
                return panelSet;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null)
                {
                    panelSet.Add(panels[i]);
                }
            }

            return panelSet;
        }

        private static List<GameObject> GetActivePanels(List<GameObject> panels)
        {
            List<GameObject> activePanels = new List<GameObject>();
            if (panels == null)
            {
                return activePanels;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && panels[i].activeSelf)
                {
                    activePanels.Add(panels[i]);
                }
            }

            return activePanels;
        }
    }
}