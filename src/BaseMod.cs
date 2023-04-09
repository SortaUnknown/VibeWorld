using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Music;
using RWCustom;
using System.IO;
using BepInEx;
using System.Security.Permissions;

#pragma warning disable CS0618 //Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 //Type or member is obsolete

namespace VibeWorld
{
    [BepInPlugin("HelloThere.VibeWorld", "Vibe World", "1.4.2")]
    public class BaseMod : BaseUnityPlugin
    {
        public enum SongMode
        {
            Default,
            GeneralMode,
            IntelligentMode,
            EchoMode
        }

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(ModsInitPatch);
            On.RainWorldGame.ctor += new On.RainWorldGame.hook_ctor(GameCtorPatch);
            On.RainWorldGame.RawUpdate += new On.RainWorldGame.hook_RawUpdate(RawUpdatePatch);
            On.RegionGate.Update += new On.RegionGate.hook_Update(GateUpdatePatch);
        }

        private static readonly Dictionary<string, string[]> regionSongList = new Dictionary<string, string[]>();

        private static bool filesChecked = false;

        private static int songOrder = 0;

        private static SongMode songMode;

        private static bool echoMode = false;

        private static bool mscActive = false;

        private static bool gateOpen = false;

        public static readonly string[] modes =
        {
            "Intelligent Mode",
            "Default",
            "General Mode",
            "Echo Mode"
        };

        private static readonly string[] calmSongs =
        {
            "NA_01 - Proxima",
            "NA_08 - Dark Sus",
            "NA_09 - Interest Pad",
            "NA_11 - Reminiscence"
        };

        private static string[] echoSongs =
        {
            "NA_32 - Else1",
            "NA_33 - Else2",
            "NA_34 - Else3",
            "NA_35 - Else4",
            "NA_36 - Else5",
            "NA_37 - Else6",
            "NA_38 - Else7"
        };

        private static string[] intelligentSongs;

        private static string[] generalSongs;

        static void ModsInitPatch(On.RainWorld.orig_OnModsInit orig, RainWorld instance)
        {
            orig.Invoke(instance);

            MachineConnector.SetRegisteredOI("HelloThere.VibeWorld", new VibeConfig());

            bool mscEnabled = MachineConnector.IsThisModActive("moreslugcats");

            if (!mscActive)
            {
                if (mscEnabled)
                {
                    List<string> tList = echoSongs.ToList();
                    tList.Add("NA_42 - Else8");
                    echoSongs = tList.ToArray();
                    mscActive = true;
                }
            }
            else
            {
                if (!mscEnabled)
                {
                    List<string> tList = echoSongs.ToList();
                    tList.Remove("NA_42 - Else8");
                    echoSongs = tList.ToArray();
                    mscActive = false;
                }
            }
        }

        static void RawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame instance, float dt)
        {
            orig.Invoke(instance, dt);

            //Vibe World only has use in Story Mode
            if (!instance.IsStorySession) return;

            MusicPlayer musicPlayer = instance.manager.musicPlayer;

            //Fade out regular songs and force engage Echo Mode in Depths or Rubicon for ambience
            //Forced Echo Mode will not work in safari, since there are no "players"
            if (instance.Players.Count > 0 && (instance.Players[0].Room.name == "SB_E05" || instance.world.region.name == "HR") && !echoMode)
            {
                musicPlayer.song.FadeOut(100);
                echoMode = true;
            }
            
            //Playlist song selector code
            if (musicPlayer != null && musicPlayer.song == null && instance.clock > 30)
            {
                string[] thisRegionList;
                string newSong;
                if (echoMode)
                {
                    thisRegionList = echoSongs;
                }
                else
                {
                    switch (songMode)
                    {
                        case SongMode.IntelligentMode:
                            thisRegionList = intelligentSongs;
                            break;
                        case SongMode.GeneralMode:
                            thisRegionList = generalSongs;
                            break;
                        default:
                            if (regionSongList.TryGetValue(Custom.LegacyRootFolderDirectory() + "Playlists" + Path.DirectorySeparatorChar + instance.world.region.name + ".txt", out thisRegionList)) { }
                            //Fallback to hardcoded song list if no valid playlist for the region is found
                            else
                            {
                                Debug.Log("VibeWorld:  No valid song found!");
                                thisRegionList = calmSongs;
                            }
                            break;
                    }
                }
                if (songOrder >= thisRegionList.Length) { songOrder = 0; }
                if (VibeConfig.randomValue.Value) { newSong = thisRegionList[(int)Random.Range(0f, thisRegionList.Length - 0.1f)]; }
                else { newSong = thisRegionList[songOrder]; }

                Song song = new Song(musicPlayer, newSong, MusicPlayer.MusicContext.StoryMode)
                {
                    playWhenReady = true,
                    volume = 1,
                    fadeInTime = 40.0f
                };
                musicPlayer.song = song;
                songOrder++;
            }
        }

        static void GameCtorPatch(On.RainWorldGame.orig_ctor orig, RainWorldGame instance, ProcessManager manager)
        {
            orig.Invoke(instance, manager);

            //Vibe World only has use in Story Mode
            if (!instance.IsStorySession) return;

            songMode = StringToMode(VibeConfig.modeValue.Value);

            echoMode = false;

            if (songMode == SongMode.EchoMode) { echoMode = true; }

            //On first game start, read playlist files and fill out dictionaries/lists
            if (!filesChecked)
            {
                string path = Custom.LegacyRootFolderDirectory() + "Playlists" + Path.DirectorySeparatorChar;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    if (!file.Contains(".txt"))
                    {
                        Debug.Log("VibeWorld:  " + file + " file is not a text file, skipping...");
                        continue;
                    }
                    string[] songList = File.ReadAllLines(file);
                    Debug.Log("VibeWorld:  Adding: " + file + " songs to list...");
                    regionSongList.Add(file, songList);
                }
                if (File.Exists(path + "general.txt")) { generalSongs = File.ReadAllLines(path + "general.txt"); }
                else
                {
                    Debug.Log("VibeWorld:  General playlist not found! This may cause problems if you are using General Mode.");
                    generalSongs = calmSongs;
                }
                filesChecked = true;
            }

            AnalyzeRegionMusic(instance, instance.GetStorySession.characterStats.name);
        }

        static void GateUpdatePatch(On.RegionGate.orig_Update orig, RegionGate instance, bool eu)
        {
            orig.Invoke(instance, eu);

            //If the song mode demands per region playlists, fade out the current song the first frame the gate is open and analyze the newly available region
            //TODO: incompatible with unintended methods to switch regions such as Warp Mod
            if (!gateOpen && instance.mode == RegionGate.Mode.MiddleOpen && songMode != SongMode.EchoMode && songMode != SongMode.GeneralMode)
            {
                Song playerSong = instance.room.game.manager.musicPlayer.song;
                if (playerSong != null) { playerSong.FadeOut(100f); }

                AnalyzeRegionMusic(instance.room.game, instance.room.game.GetStorySession.characterStats.name);
                gateOpen = true;
            }
            else if (gateOpen && instance.mode != RegionGate.Mode.MiddleOpen) gateOpen = false;
        }

        public static void AnalyzeRegionMusic(RainWorldGame game, SlugcatStats.Name slugName)
        {
            if (songMode == SongMode.IntelligentMode)
            {
                //Cycle through the entire region's rooms using a RoomSettings object as a proxy to check for the presence of music triggers in the room, then add this trigger's song as a string to the list
                //End result is a list with all the songs that are supposed to play in the region
                List<string> songList = new List<string>();
                RoomSettings settingsProxy;
                foreach (AbstractRoom room in game.world.abstractRooms)
                {
                    settingsProxy = new RoomSettings(room.name, game.world.region, false, false, game.StoryCharacter);
                    foreach (EventTrigger trigger in settingsProxy.triggers)
                    {
                        if (trigger.slugcats.Contains(slugName) && trigger.tEvent.type == TriggeredEvent.EventType.MusicEvent)
                        {
                            songList.Add((trigger.tEvent as MusicEvent).songName);
                        }
                    }
                }
                //Clean up list, remove empty strings, remove duplicates
                songList.RemoveAll(x => x == string.Empty);
                intelligentSongs = songList.Distinct().ToArray();
            }
        }

        static SongMode StringToMode(string input)
        {
            switch (input)
            {
                case "General Mode":
                    return SongMode.GeneralMode;
                case "Intelligent Mode":
                    return SongMode.IntelligentMode;
                case "Echo Mode":
                    return SongMode.EchoMode;
                default:
                    return SongMode.Default;
            }
        }
    }
}
