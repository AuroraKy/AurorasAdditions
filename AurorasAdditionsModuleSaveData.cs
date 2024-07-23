using System.Collections.Generic;

namespace Celeste.Mod.AurorasAdditions {
    public class AurorasAdditionsModuleSaveData : EverestModuleSaveData
    {

        // sessions saved when using "save and quit to map" (taken from Collabutils2)
        // - vanilla session, in XML format (all annotations are set up to (de)serialize properly into XML)
        public Dictionary<string, string> SessionsPerLevel = new Dictionary<string, string>();
        // - mod sessions saved before collab utils 1.4.8, or for mods that don't support the save data async API (serialized in YAML format)
        public Dictionary<string, Dictionary<string, string>> ModSessionsPerLevel = new Dictionary<string, Dictionary<string, string>>();
        // - mod sessions for mods that support the save data async API (using DeserializeSession / SerializeSession)
        // in binary format, converted to base64 for more efficient saving (instead of a byte[] that gets serialized to a list of numbers)
        public Dictionary<string, Dictionary<string, string>> ModSessionsPerLevelBinary = new Dictionary<string, Dictionary<string, string>>();

        public int MusicVolumeMemory = 5;
    }
}
