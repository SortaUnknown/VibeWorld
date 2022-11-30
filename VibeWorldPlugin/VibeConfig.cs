﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Music;
using RWCustom;
using System.IO;
using OptionalUI;
using BepInEx;

namespace VibeWorld
{
    public class VibeConfig : OptionInterface
    {
        public VibeConfig() : base(plugin: BaseMod.instance)
        { }

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[1];
            Tabs[0] = new OpTab("Vibe World");
            OpComboBox modeBox = new OpComboBox(new Vector2(100, 300), 125f, "modeKey", BaseMod.modes)
            {
                allowEmpty = false,
                description = "Select between the different playlist modes."
            };
            OpCheckBox checkBox = new OpCheckBox(new Vector2(100, 175), "randomKey")
            {
                description = "If checked, randomly plays songs from the selected playlist. Else, cycles through the playlist's songs by order"
            };
            OpLabelLong megaLabel = new OpLabelLong(new Vector2(250, 50), new Vector2(300, 300), "Default: Plays each region's playlist when you're in them.\nGeneral Mode: Only plays the general playlist.\nIntelligent Mode: Like Default, but automatically detects the songs that are supposed to play on each region and generates its own playlist with them.\nEcho Mode: Beats to a s c e n d to.");
            OpLabelLong megaLabelInit = new OpLabelLong(new Vector2(100, 200), new Vector2(300, 300), "All your playlists must be stored at Rain World/Playlists. Each playlist must be a plain text file, with each line being the title of each song that you want the playlist to have, as displayed in Rain World/Assets/Futile/Resources/Music/Songs (without the extension). To create a region-specific playlist, simply name the file the acronym of the region (Example: CC.txt for a Chimney Canopy playlist). To make a general playlist, name the file general.txt");
            OpLabel checkLabel = new OpLabel(100, 200, "Select Song Randomly");
            Tabs[0].AddItems(modeBox, checkBox, megaLabel, megaLabelInit, checkLabel);
        }

        public override void ConfigOnChange()
        {
            base.ConfigOnChange();

            BaseMod.songMode = StringToMode(config["modeKey"]);
            BaseMod.randomSelect = bool.Parse(config["randomKey"]);
        }

        public BaseMod.SongMode StringToMode(string input)
        {
            switch (input)
            {
                case "General Mode":
                    return BaseMod.SongMode.GeneralMode;
                case "Intelligent Mode":
                    return BaseMod.SongMode.IntelligentMode;
                case "Echo Mode":
                    return BaseMod.SongMode.EchoMode;
                default:
                    return BaseMod.SongMode.Default;
            }
        }
    }
}
