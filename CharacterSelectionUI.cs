using System.Collections.Generic;
using UnityEngine;

namespace GetALife.UI
{
    public class CharacterSelectionUI : MonoBehaviour
    {
        [SerializeField] private UIPanelController panelController;
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private GameObject characterCreationPanel;

        public void OnNewLifeSelected()
        {
            ShowCharacterCreation();
        }

        public void OnCustomLifeSelected()
        {
            ShowCharacterCreation();
        }

        private void ShowCharacterCreation()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.NavigateTo(characterCreationPanel, new List<GameObject>
            {
                characterSelectionPanel
            });
        }
    }
}