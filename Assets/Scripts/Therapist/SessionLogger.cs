using System;
using System.Globalization;
using System.IO;
using System.Linq;
using PsyCurio.Shop.Domain;
using UnityEngine;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// Appends every confirmed SUDS rating — timestamp, score, a frozen
    /// snapshot of the therapist's exposure settings and the items on the
    /// counter — to a per-session JSON file in Application.persistentDataPath.
    /// Written after every rating as well as on quit/pause: the brief asks
    /// for on-exit, and the superset is crash-safe on Android where OnQuit
    /// is not guaranteed.
    /// </summary>
    public sealed class SessionLogger : MonoBehaviour
    {
        [SerializeField] private TherapistPanel therapistPanel;
        [SerializeField] private ShopController controller;
        [SerializeField] private SudsPrompt sudsPrompt;

        private SessionRecord record;
        private string filePath;

        private void Awake()
        {
            record = new SessionRecord { sessionStartedIso = Iso(DateTime.UtcNow) };
            var directory = Path.Combine(Application.persistentDataPath, "sessions");
            Directory.CreateDirectory(directory);
            filePath = Path.Combine(directory, $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }

        private void OnEnable()
        {
            sudsPrompt.Confirmed += AddRating;
        }

        private void OnDisable()
        {
            sudsPrompt.Confirmed -= AddRating;
        }

        public void AddRating(int suds)
        {
            record.entries.Add(new SudsEntry
            {
                timestampIso = Iso(DateTime.UtcNow),
                suds = suds,
                settings = therapistPanel.CurrentSettings.Snapshot(),
                itemsOnCounter = controller.Basket.Items.Select(item => item.DisplayName).ToList()
            });
            WriteToDisk();
        }

        private void OnApplicationQuit()
        {
            WriteToDisk();
        }

        private void OnApplicationPause(bool paused)
        {
            // The Android lifecycle: pause is the only reliable exit signal.
            if (paused)
            {
                WriteToDisk();
            }
        }

        private void WriteToDisk()
        {
            record.sessionEndedIso = Iso(DateTime.UtcNow);
            File.WriteAllText(filePath, JsonUtility.ToJson(record, prettyPrint: true));
        }

        private static string Iso(DateTime utc)
        {
            // Invariant culture: in a custom format the bare ':' is the
            // *current* culture's time separator, which would corrupt the
            // session JSON on locales that use another character.
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }
    }
}
