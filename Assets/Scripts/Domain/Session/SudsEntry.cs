using System;
using System.Collections.Generic;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// One SUDS rating (Subjective Units of Distress, 0–100) with the context it
    /// was given in. JsonUtility-serializable: public fields only.
    /// </summary>
    [Serializable]
    public sealed class SudsEntry
    {
        public string timestampIso;
        public int suds;
        public ExposureSettings settings;
        public List<string> itemsOnCounter = new List<string>();
    }
}
