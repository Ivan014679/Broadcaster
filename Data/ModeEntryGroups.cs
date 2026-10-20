using System.Collections.Generic;

namespace Gamefreak130.Data
{
    internal static class ModeEntryGroups
    {
        private static readonly Dictionary<MusicModes, string> Modes;

        static ModeEntryGroups()
        {
            Modes = new Dictionary<MusicModes, string>();
            Modes.Add(MusicModes.Stereo, ModeEntries.RadioStation);
            Modes.Add(MusicModes.Build, ModeEntries.BuildMode);
            Modes.Add(MusicModes.Buy, ModeEntries.BuyMode);
            Modes.Add(MusicModes.Map, ModeEntries.MapMode);
            Modes.Add(MusicModes.CAS, ModeEntries.CASMode);
        }

        public static int Count
        {
            get { return Modes.Count; }
        }

        public static string GetModeName(MusicModes mode)
        {
            return Modes[mode];
        }

        public static bool ContainsValue(string modeName)
        {
            return Modes.ContainsValue(modeName);
        }

        public static bool TryGetMusicMode(MusicModes mode, out string modeName)
        {
            return Modes.TryGetValue(mode, out modeName);
        }
    }
}
