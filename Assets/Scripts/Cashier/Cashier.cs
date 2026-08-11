using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// The clickable cashier. A click triggers her wave; if she is already
    /// waving, the wave restarts from the top so every click visibly responds
    /// instead of silently queueing. Speech arrives with the register step.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class Cashier : MonoBehaviour, IClickable
    {
        private static readonly int WaveTrigger = Animator.StringToHash("Wave");
        private const string WaveStateName = "Wave";

        private Animator animator;

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
    }
}
