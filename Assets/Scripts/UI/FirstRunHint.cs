using System.Collections;
using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop.Ui
{
    /// <summary>
    /// The one-line hint shown at session start, fading out after the first
    /// successful interaction. Shown every session on purpose: a PlayerPrefs
    /// once-ever flag would hide it from a reviewer's second run.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FirstRunHint : MonoBehaviour
    {
        [SerializeField] private ClickRouter router;
        [SerializeField] private float fadeSeconds = 0.7f;

        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            router.ClickDispatched += HandleFirstInteraction;
        }

        private void OnDisable()
        {
            router.ClickDispatched -= HandleFirstInteraction;
        }

        private void HandleFirstInteraction()
        {
            router.ClickDispatched -= HandleFirstInteraction;
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            for (var t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                group.alpha = 1f - t / fadeSeconds;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
