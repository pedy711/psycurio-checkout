using UnityEngine;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// Starts the idle animation at a random phase so queued bystanders don't
    /// breathe in eerie unison.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class IdleOffset : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Animator>().Play("Idle", 0, Random.value);
        }
    }
}
