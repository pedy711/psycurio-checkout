using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Head-and-eyes look-at toward the patient camera via Animator IK,
    /// weight-smoothed so toggling eye contact reads as her naturally turning
    /// toward or away — an exposure-intensity control, not a light switch.
    /// Requires the IK Pass flag on the Animator layer (set by CashierSetup).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CashierEyeContact : MonoBehaviour
    {
        [SerializeField] private Transform lookTarget;
        [SerializeField] private bool eyeContact = true;
        [Tooltip("Seconds to blend the gaze in or out.")]
        [SerializeField] private float blendSeconds = 0.6f;

        private Animator animator;
        private float weight;

        public bool EyeContact
        {
            get => eyeContact;
            set => eyeContact = value;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            weight = eyeContact ? 1f : 0f;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            var target = eyeContact ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime / blendSeconds);

            animator.SetLookAtWeight(weight, 0.1f, 0.9f, 1f, 0.5f);
            if (weight > 0f && lookTarget != null)
            {
                animator.SetLookAtPosition(lookTarget.position);
            }
        }
    }
}
