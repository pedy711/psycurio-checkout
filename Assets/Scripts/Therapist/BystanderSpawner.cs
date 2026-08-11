using System.Collections.Generic;
using UnityEngine;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// Keeps 0–3 greybox bystanders standing on the queue anchors. They are
    /// deliberately inert — no collider, no highlight — so they add social
    /// presence without ever looking interactive.
    /// </summary>
    public sealed class BystanderSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] queueAnchors = new Transform[0];
        [SerializeField] private GameObject bystanderPrefab;

        private readonly List<GameObject> spawned = new List<GameObject>();

        public int MaxCount => queueAnchors.Length;

        public void SetCount(int count)
        {
            count = Mathf.Clamp(count, 0, queueAnchors.Length);

            while (spawned.Count < count)
            {
                var anchor = queueAnchors[spawned.Count];
                var bystander = Instantiate(bystanderPrefab, anchor);
                bystander.name = $"Bystander_{spawned.Count}";
                spawned.Add(bystander);
            }
            while (spawned.Count > count)
            {
                var last = spawned[spawned.Count - 1];
                spawned.RemoveAt(spawned.Count - 1);
                Destroy(last);
            }
        }
    }
}
