using System.Collections;
using TMPro;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// The cashier's single world-space speech bubble: billboards to the fixed
    /// camera, fades in/out, sizes to its text, and shows exactly one message
    /// at a time — a new Show replaces the current one, so speech can never
    /// stack. Display time scales with message length.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SpeechBalloon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [Tooltip("The fixed scene camera; assigned by scene wiring.")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float fadeSeconds = 0.15f;
        [SerializeField] private float minimumShowSeconds = 2.5f;
        [SerializeField] private float perCharacterSeconds = 0.05f;

        private CanvasGroup canvasGroup;
        private Coroutine activeShow;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }
            canvasGroup.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (canvasGroup.alpha <= 0f && activeShow == null)
            {
                return;
            }

            // World-space UI reads correctly when its forward axis points
            // away from the viewer.
            transform.rotation = Quaternion.LookRotation(transform.position - viewCamera.transform.position);
        }

        public void Show(string message)
        {
            if (activeShow != null)
            {
                StopCoroutine(activeShow);
            }
            activeShow = StartCoroutine(ShowRoutine(message));
        }

        /// <summary>Fades out immediately, cancelling a running show.</summary>
        public void Hide()
        {
            if (activeShow != null)
            {
                StopCoroutine(activeShow);
            }
            activeShow = StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            yield return Fade(canvasGroup.alpha, 0f);
            activeShow = null;
        }

        private IEnumerator ShowRoutine(string message)
        {
            messageText.text = message;
            yield return Fade(canvasGroup.alpha, 1f);
            yield return new WaitForSeconds(minimumShowSeconds + message.Length * perCharacterSeconds);
            yield return Fade(1f, 0f);
            activeShow = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (var t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
