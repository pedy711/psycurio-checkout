using System.Collections.Generic;
using NUnit.Framework;
using PsyCurio.Shop.Domain;
using UnityEngine;

namespace PsyCurio.Shop.Domain.Tests
{
    /// <summary>
    /// The domain assembly cannot reference JsonUtility (no engine references),
    /// so the serialization contract of the session records is pinned down here,
    /// in the test assembly, which does have engine access.
    /// </summary>
    public sealed class SessionRecordJsonTests
    {
        private static SessionRecord SampleRecord()
        {
            return new SessionRecord
            {
                sessionStartedIso = "2026-08-11T10:00:00Z",
                sessionEndedIso = "2026-08-11T10:20:00Z",
                entries = new List<SudsEntry>
                {
                    new SudsEntry
                    {
                        timestampIso = "2026-08-11T10:05:00Z",
                        suds = 45,
                        settings = new ExposureSettings
                        {
                            eyeContact = true,
                            responseDelaySeconds = 1.5f,
                            bystanderCount = 2,
                            ambientNoiseLevel = 0.6f
                        },
                        itemsOnCounter = new List<string> { "Coffee", "Coffee", "Bread" }
                    },
                    new SudsEntry
                    {
                        timestampIso = "2026-08-11T10:15:00Z",
                        suds = 30,
                        settings = new ExposureSettings(),
                        itemsOnCounter = new List<string>()
                    }
                }
            };
        }

        [Test]
        public void SessionRecord_RoundTripsThroughJsonUtility()
        {
            var original = SampleRecord();

            var json = JsonUtility.ToJson(original, prettyPrint: true);
            var restored = JsonUtility.FromJson<SessionRecord>(json);

            Assert.That(restored.sessionStartedIso, Is.EqualTo(original.sessionStartedIso));
            Assert.That(restored.sessionEndedIso, Is.EqualTo(original.sessionEndedIso));
            Assert.That(restored.entries.Count, Is.EqualTo(2));
            Assert.That(restored.entries[0].suds, Is.EqualTo(45));
            Assert.That(restored.entries[0].timestampIso, Is.EqualTo("2026-08-11T10:05:00Z"));
            Assert.That(restored.entries[0].settings.eyeContact, Is.True);
            Assert.That(restored.entries[0].settings.responseDelaySeconds, Is.EqualTo(1.5f));
            Assert.That(restored.entries[0].settings.bystanderCount, Is.EqualTo(2));
            Assert.That(restored.entries[0].settings.ambientNoiseLevel, Is.EqualTo(0.6f));
            Assert.That(restored.entries[0].itemsOnCounter, Is.EqualTo(new List<string> { "Coffee", "Coffee", "Bread" }));
            Assert.That(restored.entries[1].suds, Is.EqualTo(30));
            Assert.That(restored.entries[1].itemsOnCounter, Is.Empty);
        }

        [Test]
        public void SessionRecord_SerializesAllExpectedFieldNames()
        {
            var json = JsonUtility.ToJson(SampleRecord());

            foreach (var key in new[]
                     {
                         "sessionStartedIso", "sessionEndedIso", "entries", "timestampIso",
                         "suds", "settings", "eyeContact", "responseDelaySeconds",
                         "bystanderCount", "ambientNoiseLevel", "itemsOnCounter"
                     })
            {
                Assert.That(json, Does.Contain($"\"{key}\""), $"JSON should contain field '{key}'");
            }
        }

        [Test]
        public void ExposureSettings_Snapshot_IsIndependentOfLiveSettings()
        {
            var live = new ExposureSettings
            {
                eyeContact = true,
                responseDelaySeconds = 2f,
                bystanderCount = 1,
                ambientNoiseLevel = 0.3f
            };

            var snapshot = live.Snapshot();
            live.eyeContact = false;
            live.responseDelaySeconds = 0f;
            live.bystanderCount = 3;
            live.ambientNoiseLevel = 1f;

            Assert.That(snapshot.eyeContact, Is.True);
            Assert.That(snapshot.responseDelaySeconds, Is.EqualTo(2f));
            Assert.That(snapshot.bystanderCount, Is.EqualTo(1));
            Assert.That(snapshot.ambientNoiseLevel, Is.EqualTo(0.3f));
        }
    }
}
