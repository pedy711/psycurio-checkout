using System;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// The therapist-controlled exposure intensity settings at a moment in time.
    /// Public fields (not properties) because instances are serialized with
    /// Unity's JsonUtility from the Unity-side session logger.
    /// </summary>
    [Serializable]
    public sealed class ExposureSettings
    {
        public bool eyeContact = true;
        public float responseDelaySeconds;
        public int bystanderCount;
        public float ambientNoiseLevel;

        /// <summary>Frozen copy for a session-log entry; live settings keep changing.</summary>
        public ExposureSettings Snapshot()
        {
            return (ExposureSettings)MemberwiseClone();
        }
    }
}
