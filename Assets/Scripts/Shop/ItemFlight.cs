using System;
using System.Collections;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// Flies a freshly spawned counter copy from the shelf to its slot along
    /// an eased parabolic arc, then reports landing. Added by CounterSlots at
    /// spawn time and removes itself on arrival, so a landed item is
    /// indistinguishable from an instantly placed one.
    /// </summary>
    public sealed class ItemFlight : MonoBehaviour
    {
        private const float FlightSeconds = 0.45f;
        private const float ArcHeight = 0.35f;

        public void Begin(Vector3 fromWorld, Vector3 toLocal, Action onLanded)
        {
            StartCoroutine(Fly(fromWorld, toLocal, onLanded));
        }

        private IEnumerator Fly(Vector3 fromWorld, Vector3 toLocal, Action onLanded)
        {
            var parent = transform.parent;
            var fromLocal = parent.InverseTransformPoint(fromWorld);

            for (var t = 0f; t < FlightSeconds; t += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, t / FlightSeconds);
                var position = Vector3.Lerp(fromLocal, toLocal, progress);
                // Parabolic lift on top of the straight path; zero at both ends.
                position.y += ArcHeight * 4f * progress * (1f - progress);
                transform.localPosition = position;
                yield return null;
            }

            transform.localPosition = toLocal;
            onLanded?.Invoke();
            Destroy(this);
        }
    }
}
