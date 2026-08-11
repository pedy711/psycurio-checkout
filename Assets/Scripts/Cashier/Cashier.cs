using System.Collections;
using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// The clickable cashier. A click triggers her wave; if she is already
    /// waving, the wave restarts from the top so every click visibly responds
    /// instead of silently queueing. Say() routes speech through her single
    /// balloon after the response delay — a therapist-controlled exposure
    /// parameter from step 11 onward; a newer Say cancels a pending one.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class Cashier : MonoBehaviour, IClickable
    {
        private static readonly int WaveTrigger = Animator.StringToHash("Wave");
        private const string WaveStateName = "Wave";

        [SerializeField] private SpeechBalloon balloon;
        [Min(0f)]
        [SerializeField] private float responseDelaySeconds = 0.5f;

        private Animator animator;
        private Coroutine pendingSpeech;

        public float ResponseDelaySeconds
        {
            get => responseDelaySeconds;
            set => responseDelaySeconds = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnClick()
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(WaveStateName))
            {
                animator.Play(WaveStateName, 0, 0f);
            }
            else
            {
                animator.SetTrigger(WaveTrigger);
            }
        }

        public void Say(string message)
        {
            if (pendingSpeech != null)
            {
                StopCoroutine(pendingSpeech);
            }
            pendingSpeech = StartCoroutine(SayAfterDelay(message));
        }

        private IEnumerator SayAfterDelay(string message)
        {
            if (responseDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(responseDelaySeconds);
            }
            balloon.Show(message);
            pendingSpeech = null;
        }
    }
}
