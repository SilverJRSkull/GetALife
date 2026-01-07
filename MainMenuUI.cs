#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GetALife.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private UIPanelController panelController;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private GameObject settingsPanel;

        private void OnEnable()
        {
            EnsureInitialPanels();
        }

        private void EnsureInitialPanels()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.ShowPanels(new System.Collections.Generic.List<GameObject>
            {
                mainMenuPanel
            });

            panelController.HidePanels(new System.Collections.Generic.List<GameObject>
            {
                characterSelectionPanel,
                settingsPanel
            });
        }

        public void OnPlayClicked()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.NavigateTo(characterSelectionPanel, new System.Collections.Generic.List<GameObject>
            {
                mainMenuPanel
            });
        }

        public void OnSettingsClicked()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.NavigateTo(settingsPanel, new System.Collections.Generic.List<GameObject>
            {
                mainMenuPanel
            });
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}