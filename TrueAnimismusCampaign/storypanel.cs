using Mono.Cecil.Cil;
using MonoMod.Cil;
//using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
//using Quintessential.Settings;
//using SDL2;
using System;
//using System.IO;
using System.Linq;
using System.Collections.Generic;
//using System.Globalization;
//using System.Reflection;

namespace TrueAnimismusCampaign;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using AtomTypes = class_175;
//using PartTypes = class_191;
using Texture = class_256;
//using Song = class_186;
//using Tip = class_215;
//using Font = class_1;

public static class StoryPanelPatcher
{
    static Texture puzzle_frame_animismus;
    private static Puzzle optionsUnlock = null;
    public const string optionsID = "tac-options";

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // helpers
    public static void setOptionsUnlock(Puzzle puzzle)
    {
        if (optionsUnlock == null) optionsUnlock = puzzle;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // public functions
    public static void LoadContent()
    {
        string path = "textures/";
        puzzle_frame_animismus = class_235.method_615(path + "puzzle_info/frame_buttons_animismus");
    }

    public static void Load()
    {
        On.class_172.method_480 += new On.class_172.hook_method_480(AddCharactersToDictionary);
    }

    public static void PostLoad()
    {
        //IL.StoryPanel.method_2175 += skipDrawingTheReturnButton;
        On.class_135.method_272 += hotswapPuzzleFrameTexture;
        On.OptionsScreen.method_50 += hotswapOptionsStorypanel;
        On.StoryPanel.method_2172 += customStorypanelUnlocks;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // hooking
    private static void AddCharactersToDictionary(On.class_172.orig_method_480 orig)
    {
        orig();
        Logger.Log("[TrueAnimismusCampaign] Adding vignette actors.");
        // add the new characters
        foreach (CharacterModelTAC character in CampaignLoader.getModel().Characters)
        {
            class_172.field_1670.Add(character.ID, character.FromModel());
        }
    }

    private static void hotswapPuzzleFrameTexture(On.class_135.orig_method_272 orig, Texture texture, Vector2 position)
    {
        if (CampaignLoader.CurrentCampaignIsTAC())
        {
            if (texture == class_238.field_1989.field_95.field_638)
            {
                texture = puzzle_frame_animismus;
            }
        }
        orig(texture, position);
        return;
    }

    public static void hotswapOptionsStorypanel(On.OptionsScreen.orig_method_50 orig, OptionsScreen screen_self, float timeDelta)
    {
        if (CampaignLoader.CurrentCampaignIsTAC())
        {
            var screen_dyn = new DynamicData(screen_self);
            var currentStoryPanel = screen_dyn.Get<StoryPanel>("field_2680");
            var stringArray = new DynamicData(currentStoryPanel).Get<string[]>("field_4093");
            if (!stringArray.Any(x => x.Contains("Harriet") || x.Contains("Eleanor")))
            {
                var class264 = new class_264("options-tac");
                class264.field_2090 = optionsID;
                screen_dyn.Set("field_2680", new StoryPanel((Maybe<class_264>)class264, false));
            }
        }
        orig(screen_self, timeDelta);
    }

    static Tuple<int, LocString>[] SigmarStoryUnlocks;

    public static void CreateSigmarStoryUnlocks(List<int> unlocks)
    {
        SigmarStoryUnlocks = new Tuple<int, LocString>[unlocks.Count + 1];

        for (int i = 0; i < unlocks.Count; i++)
        {
            int k = unlocks[i];
            string msg = "Win " + k + (k == 1 ? " game" : " games");
            SigmarStoryUnlocks[i] = Tuple.Create(k, class_134.method_253(msg, string.Empty));
        }
        SigmarStoryUnlocks[unlocks.Count] = Tuple.Create(int.MaxValue, LocString.field_2597);
    }

    public static void customStorypanelUnlocks(On.StoryPanel.orig_method_2172 orig, StoryPanel panel_self, float timeDelta, Vector2 pos, int index, Tuple<int, LocString>[] tuple)
    {
        if (CampaignLoader.CurrentCampaignIsTAC() && tuple.Length == 2 && tuple[0].Item2 == class_134.method_253("Complete the prologue", string.Empty))
        {
            // then we're doing the options code while in the TAC campaign
            // hijack the inputs so we draw it our way
            // Thanks, RMC
            bool flag = GameLogic.field_2434.field_2451.method_573(optionsUnlock);
            index = flag ? 1 : 0;
            tuple = new Tuple<int, LocString>[2]
            {
                Tuple.Create(1, class_134.method_253("Complete the prologue", string.Empty)),
                Tuple.Create(int.MaxValue, LocString.field_2597)
            };
        }
        else if (CampaignLoader.CurrentCampaignIsTAC() && tuple.Length == 7 && tuple[0].Item2 == class_134.method_253("Win 1 game", string.Empty))
        {
            // then we're doing the solitaire code while in the TAC campaign
            // hijack the inputs so we draw it our way
            // Thanks, RMC
            tuple = SigmarStoryUnlocks;
        }

        orig(panel_self, timeDelta, pos, index, tuple);
    }
}