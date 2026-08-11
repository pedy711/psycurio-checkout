using System;
using System.Collections.Generic;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// One play session's worth of SUDS ratings — the unit written to disk as
    /// JSON. JsonUtility needs a top-level object, so the entry list lives in
    /// this wrapper rather than being serialized bare.
    /// </summary>
    [Serializable]
    public sealed class SessionRecord
    {
        public string sessionStartedIso;
        public string sessionEndedIso;
        public List<SudsEntry> entries = new List<SudsEntry>();
    }
}
