using Gamefreak130.Data;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.SimIFace;
using System;
using System.Collections.Generic;
using System.Xml;
using static Sims3.UI.OptionsDialog;

namespace Gamefreak130.Broadcaster
{
    public static class Bootstrap
    {
        [Tunable]
        private static readonly bool kCJackB;
        private static List<string> xmlMusicEntryNames;
        private static List<XmlDocument> xmlMusicEntriesList;
        private static XmlDocument mergedMusicEntries;
        private static bool mOptionsInjectionHandled;

        static Bootstrap()
        {
            xmlMusicEntryNames = new List<string>();
            xmlMusicEntriesList = new List<XmlDocument>();
            World.OnStartupAppEventHandler += OnStartupApp;
        }

        private static void OnStartupApp(object sender, EventArgs e)
        {
            if (Simulator.LoadXML(typeof(Bootstrap).Assembly.GetName().Name) is XmlDocument xmlDocument && xmlDocument.DocumentElement != null && string.Equals(xmlDocument.DocumentElement.Name, "Broadcaster"))
            {
                foreach (XmlNode xmlMusicEntriesNode in xmlDocument.DocumentElement.ChildNodes)
                {
                    XmlElement xmlMusicEntries = xmlMusicEntriesNode as XmlElement;
                    if (xmlMusicEntries != null && xmlMusicEntries.HasAttribute("value") && string.Equals(xmlMusicEntries.Name, "XMLMusicEntries"))
                    {
                        string xmlName = xmlMusicEntries.GetAttribute("value");
                        if (xmlName.Length > 0 && !xmlMusicEntryNames.Contains(xmlName))
                        {
                            xmlMusicEntryNames.Add(xmlName);
                            XmlDocument xmlDoc = Simulator.LoadXML(xmlName);

                            if (xmlDoc != null)
                            {
                                xmlMusicEntriesList.Add(xmlDoc);
                                foreach (XmlNode stationNode in xmlMusicEntries.ChildNodes)
                                {
                                    XmlElement station = stationNode as XmlElement;
                                    if (station != null && station.HasAttribute("value") && string.Equals(station.Name, "Station"))
                                    {
                                        XmlElement workoutElement = station["IsWorkoutStation"];
                                        bool isWorkoutStation = workoutElement != null && workoutElement.GetAttribute("value") == "1";
                                        XmlElement danceElement = station["IsSlowDanceStation"];
                                        bool isSlowDanceStation = danceElement != null && danceElement.GetAttribute("value") == "1";
                                        AddStationIfNeeded(station.GetAttribute("value"), isWorkoutStation, isSlowDanceStation);
                                    }
                                }
                            }
                        }
                    }
                }

                Simulator.AddObject(new Common.RepeatingFunctionTask(new Common.GenericDelegate<bool>(InjectOptions)));
            }
        }

        private static void AddStationIfNeeded(string station, bool isWorkoutStation, bool isSlowDanceStation)
        {
            ProductVersion version = ProductVersion.Undefined;
            string translationKey = station;
            string playlistKey = station;
            if (Enum.IsDefined(typeof(GameStations), station))
            {
                version = (ProductVersion)Enum.Parse(typeof(GameStations), station, true);
                switch (station)
                {
                    case "Beach_Party":
                    case "Island_Life":
                        translationKey = translationKey.Replace("_", "");
                        playlistKey = playlistKey.Replace("_", "");
                        break;
                    case "Future":
                        translationKey += "World";
                        break;
                    case "Superhero":
                        translationKey = "Epic";
                        break;
                    case "Western":
                        translationKey = "Spaghetti_" + translationKey;
                        break;
                }
            }
            string text = "Gameplay/Excel/Stereo/Stations:" + translationKey;
            if (!GameUtils.IsInstalled(version) && !StereoStationData.sStereoStationDictionary.ContainsKey(text))
            {
                StereoStationData data = new StereoStationData(text, $"Stereo_{playlistKey}", $"Stereo_Wired_{playlistKey}",
                                                               $"Stereo_{playlistKey}_Virtual", $"Stereo_Wired_{playlistKey}_Virtual",
                                                               Sims3.SimIFace.CAS.FavoriteMusicType.Custom, isWorkoutStation, false, isSlowDanceStation,
                                                               WorldName.Undefined, ProductVersion.BaseGame);
                StereoStationData.sStereoStationDictionary.Add(text, data);
            }
        }

        private static bool InjectOptions()
        {
            try
            {
                if (sDialog != null)
                {
                    if (!mOptionsInjectionHandled)
                    {
                        XmlDocument xmlMerged = LoadAndMergeMusicEntries();
                        if (xmlMerged != null && xmlMerged.DocumentElement is XmlElement xmlElement)
                        {
                            foreach (XmlNode musicSelectionNode in xmlElement.ChildNodes)
                            {
                                XmlElement modeElement = musicSelectionNode as XmlElement;
                                if (modeElement != null)
                                {
                                    switch (modeElement.Name)
                                    {
                                        case ModeEntries.RadioStation:
                                            sDialog.LoadSongData(xmlElement, ModeEntries.RadioStation, (int) MusicModes.Stereo);
                                            break;
                                        case ModeEntries.BuildMode:
                                            sDialog.LoadSongData(xmlElement, ModeEntries.BuildMode, (int) MusicModes.Build);
                                            break;
                                        case ModeEntries.BuyMode:
                                            sDialog.LoadSongData(xmlElement, ModeEntries.BuyMode, (int) MusicModes.Buy);
                                            break;
                                        case ModeEntries.MapMode:
                                            sDialog.LoadSongData(xmlElement, ModeEntries.MapMode, (int) MusicModes.Map);
                                            break;
                                        case ModeEntries.CASMode:
                                            sDialog.LoadSongData(xmlElement, ModeEntries.CASMode, (int) MusicModes.CAS);
                                            break;
                                    }
                                }
                            }
                            if (sDialog.mItemGridGenreButtons.Count > 0)
                            {
                                sDialog.mItemGridGenreButtons.SelectedItem = 0;
                                string text = sDialog.mItemGridGenreButtons.InternalGrid.CellTags[0, 0] as string;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    sDialog.mCurrentGenre = text;
                                    sDialog.UpdateTable();
                                }
                            }
                        }
                        mOptionsInjectionHandled = true;
                    }
                }
                else
                {
                    mOptionsInjectionHandled = false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static XmlDocument LoadAndMergeMusicEntries()
        {
            if (mergedMusicEntries != null)
            {
                return mergedMusicEntries;
            }

            if (xmlMusicEntriesList != null && xmlMusicEntriesList.Count > 0)
            {
                XmlDocument xmlMerged = new XmlDocument();
                XmlElement mergedRoot = xmlMerged.CreateElement("MusicSelection");
                xmlMerged.AppendChild(mergedRoot);

                foreach (XmlDocument xmlMusicEntries in xmlMusicEntriesList)
                {
                    if (xmlMusicEntries.DocumentElement is XmlElement xmlElement)
                    {
                        foreach (XmlNode musicSelectionNode in xmlElement.ChildNodes)
                        {
                            XmlElement modeElement = musicSelectionNode as XmlElement;
                            if (modeElement != null && ModeEntryGroups.ContainsValue(modeElement.Name))
                            {
                                XmlElement modeEntryElement = mergedRoot[modeElement.Name];
                                if (modeEntryElement == null)
                                {
                                    modeEntryElement = xmlMerged.CreateElement(modeElement.Name);
                                    mergedRoot.AppendChild(modeEntryElement);
                                }

                                foreach (XmlNode genreNode in modeElement.ChildNodes)
                                {
                                    XmlElement genreElement = genreNode as XmlElement;
                                    if (genreElement != null && genreElement.HasAttribute("name"))
                                    {
                                        string genreName = genreElement.GetAttribute("name");
                                        XmlElement genreMerged = null;
                                        foreach (XmlNode genreMaster in modeEntryElement.ChildNodes)
                                        {
                                            if (genreMaster is XmlElement && string.Equals((genreMaster as XmlElement).GetAttribute("name"), genreName))
                                            {
                                                genreMerged = genreMaster as XmlElement;
                                                break;
                                            }
                                        }

                                        if (genreMerged == null)
                                        {
                                            genreMerged = xmlMerged.ImportNode(genreElement, false) as XmlElement;
                                            modeEntryElement.AppendChild(genreMerged);
                                        }

                                        foreach (XmlNode entryNode in genreElement.ChildNodes)
                                        {
                                            XmlElement entryElement = entryNode as XmlElement;
                                            if (entryElement != null && entryElement.HasAttribute("songkey"))
                                            {
                                                XmlNode clonedEntry = xmlMerged.ImportNode(entryNode, true);
                                                genreMerged.AppendChild(clonedEntry);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                mergedMusicEntries = xmlMerged;
                xmlMusicEntryNames.Clear();
                xmlMusicEntriesList.Clear();
                return mergedMusicEntries;
            }
            return null;
        }
    }
}
