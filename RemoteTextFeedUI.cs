using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace GetALife.UI
{
    public class RemoteTextFeedUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Remote Content")]
        [SerializeField] private string contentUrl;
        [SerializeField] private TMP_Text contentLabel;
        [SerializeField] private bool fetchOnEnable = true;
        [SerializeField] private bool showErrorsInLabel = true;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField, Min(1)] private float refreshIntervalSeconds = 60f;

        [Header("Caching")]
        [Tooltip("Adds a timestamp query parameter to force a fresh fetch every time.")]
        [SerializeField] private bool cacheBustEveryRequest = true;

        private Coroutine fetchRoutine;
        private Coroutine refreshLoopRoutine;
        private bool wasMenuActive;

        private void OnEnable()
        {
            if (fetchOnEnable)
                Refresh();

            wasMenuActive = IsMenuActive();
            refreshLoopRoutine = StartCoroutine(AutoRefreshLoop());
        }

        private void OnDisable()
        {
            if (fetchRoutine != null)
            {
                StopCoroutine(fetchRoutine);
                fetchRoutine = null;
            }

            if (refreshLoopRoutine != null)
            {
                StopCoroutine(refreshLoopRoutine);
                refreshLoopRoutine = null;
            }
        }

        public void Refresh()
        {
            if (fetchRoutine != null)
                StopCoroutine(fetchRoutine);

            fetchRoutine = StartCoroutine(FetchContent());
        }

        private IEnumerator AutoRefreshLoop()
        {
            while (true)
            {
                bool isMenuActive = IsMenuActive();

                // Menu just opened → refresh immediately
                if (isMenuActive && !wasMenuActive)
                    Refresh();

                if (isMenuActive)
                {
                    yield return new WaitForSecondsRealtime(refreshIntervalSeconds);

                    if (IsMenuActive())
                        Refresh();
                }
                else
                {
                    yield return null;
                }

                wasMenuActive = isMenuActive;
            }
        }

        private IEnumerator FetchContent()
        {
            if (string.IsNullOrWhiteSpace(contentUrl))
                yield break;

            string resolvedUrl = contentUrl;

            if (cacheBustEveryRequest)
                resolvedUrl = AppendCacheBuster(resolvedUrl);

            using (UnityWebRequest request = UnityWebRequest.Get(resolvedUrl))
            {
                // Strong cache prevention (client + proxies + CDN)
                request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                request.SetRequestHeader("Pragma", "no-cache");
                request.SetRequestHeader("Expires", "0");
                request.SetRequestHeader("Accept", "text/plain");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (contentLabel != null)
                    {
                        contentLabel.richText = false;
                        string payload = request.downloadHandler.text ?? string.Empty;

                        if (LooksLikeHtml(payload))
                        {
                            Debug.LogWarning(
                                $"[RemoteTextFeedUI] HTML received. URL must point to raw text.\n{resolvedUrl}"
                            );
                            contentLabel.text = "Update feed URL must point to raw text content.";
                        }
                        else
                        {
                            contentLabel.text = payload;
                        }
                    }
                }
                else
                {
                    Debug.LogError(
                        $"[RemoteTextFeedUI] Failed to load {resolvedUrl}: {request.error}"
                    );

                    if (showErrorsInLabel && contentLabel != null)
                        contentLabel.text = "Unable to load updates.";
                }
            }

            fetchRoutine = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (contentLabel == null)
                return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                contentLabel,
                eventData.position,
                eventData.pressEventCamera
            );

            if (linkIndex == -1)
                return;

            string linkId = contentLabel.textInfo.linkInfo[linkIndex].GetLinkID();
            if (!string.IsNullOrWhiteSpace(linkId))
                Application.OpenURL(linkId);
        }

        private static string AppendCacheBuster(string url)
        {
            long ticks = DateTime.UtcNow.Ticks;

            if (url.Contains("?"))
                return $"{url}&t={ticks}";

            return $"{url}?t={ticks}";
        }

        private static bool LooksLikeHtml(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.TrimStart();
            return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMenuActive()
        {
            if (mainMenuPanel == null)
                return false;

            return mainMenuPanel.activeInHierarchy;
        }
    }
}