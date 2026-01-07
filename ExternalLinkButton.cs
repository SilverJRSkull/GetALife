using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GetALife.UI
{
    public class ExternalLinkButtons : MonoBehaviour
    {
        [Serializable]
        public class LinkDefinition
        {
            [SerializeField] private Button button;
            [SerializeField] private string url;

            public Button Button => button;
            public string Url => url;
        }

        [Header("Links")]
        [SerializeField] private List<LinkDefinition> links = new();

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            foreach (LinkDefinition link in links)
            {
                if (link?.Button == null)
                {
                    continue;
                }

                string url = link.Url;
                link.Button.onClick.AddListener(() => OpenLink(url));
            }
        }

        private void UnbindButtons()
        {
            foreach (LinkDefinition link in links)
            {
                if (link?.Button != null)
                {
                    link.Button.onClick.RemoveAllListeners();
                }
            }
        }

        private static void OpenLink(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                Application.OpenURL(url);
            }
        }
    }
}