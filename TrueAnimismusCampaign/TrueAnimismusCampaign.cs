//using Mono.Cecil.Cil;
//using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

//using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
using Quintessential.Settings;
using SDL2;
using System;
//using System.IO;
//using System.Linq;
using System.Collections.Generic;
//using System.Globalization;
using System.Reflection;

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

// SOLITAIRE_ICON_TEMP - these lines will be removed once custom campaign icons are actually implemented in quintessential

public class MainClass : QuintessentialMod
{
	
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	private static IDetour hook_Sim_method_1835;

	static Texture iconSolitaire, iconSolitaireSmall;
	public static List<class_259> customSolitaires = new(); // SOLITAIRE_ICON_DEBUG

	// settings
	//public static bool DisplayMetalsRemaining = true;
	public override Type SettingsType => typeof(MySettings);
	public class MySettings
	{
		//[SettingsLabel("Display 'metals remaining' in the new Sigmar's Garden.")]
		//public bool DisplayMetalsRemaining = true;
	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		//var SET = (MySettings)Settings;
		//DisplayMetalsRemaining = SET.DisplayMetalsRemaining;
	}

	public static bool findModMetaFilepath(string name, out string filepath)
	{
		filepath = "<missing mod directory>";
		foreach (ModMeta mod in QuintessentialLoader.Mods)
		{
			if (mod.Name == name)
			{
				filepath = mod.PathDirectory;
				return true;
			}
		}
		return false;
	}

	public override void LoadPuzzleContent()
	{
		//PolymerInput.LoadContent();
		StoryPanelPatcher.LoadContent();
		AssignProxies.LoadContent();
		ProductionManager.initializeProductionTextureBank();
		
		//string path = "textures/puzzle_select/"; // SOLITAIRE_ICON_TEMP
		//iconSolitaire = class_235.method_615(path + "icon_rmc_solitaire"); // SOLITAIRE_ICON_TEMP
		//iconSolitaireSmall = class_235.method_615(path + "icon_rmc_solitaire_small"); // SOLITAIRE_ICON_TEMP

		//------------------------- HOOKING -------------------------//
		hook_Sim_method_1835 = new Hook(PrivateMethod<Sim>("method_1835"), OnSimMethod1835);
		if (!AssignProxies.getTrueAnProxiesAssigned()) {AssignProxies.YellowBoxHook();} //Let us not screw with the error code if you've already seen the special error
	}

	private delegate void orig_Sim_method_1835(Sim self);
	private static void OnSimMethod1835(orig_Sim_method_1835 orig, Sim sim_self)
	{
		AssignProxies.My_Method_1835(sim_self);
		orig(sim_self);
	}

	public override void Unload() {
		hook_Sim_method_1835.Dispose();
		SigmarGardenPatcher.Unload();
		AssignProxies.Unload();
	}

	public override void Load()
	{
		Settings = new MySettings();
		CampaignLoader.Load();
		//JournalLoader.loadJournalModel();
		Document.Load();
		CutscenePatcher.Load();
		//JournalLoader.Load();
		StoryPanelPatcher.Load();
	}
	public void LoadContent()
	{
		LoadAllCustomSounds();
	}

	public override void PostLoad()
	{
		SigmarGardenPatcher.PostLoad();
		CampaignLoader.modifyCampaign();
		ProductionManager.PostLoad();
		StoryPanelPatcher.PostLoad();
		AssignProxies.PostLoad();
		On.SolutionEditorScreen.method_2107 += hotswapPartDescriptions;
	}

    public static Sound sigmarMusic, emptySound;
	public static void LoadAllCustomSounds()
	{
		foreach (var dir in QuintessentialLoader.ModContentDirectories)
		{
			string musicPath = Path.Combine(dir, "Content/music/clock-ticking.ogg");
			Logger.Log("[TrueAnimismusCampaign] Music path: " + musicPath);
			if (File.Exists(musicPath))
			{
				sigmarMusic = new Sound()
				{
					field_4060 = Path.GetFileNameWithoutExtension(musicPath),
					field_4061 = class_158.method_375(musicPath)
				};
				break;
			}
			musicPath = Path.Combine(dir, "Content/music/empty.ogg");
			Logger.Log("[TrueAnimismusCampaign] Empty sound path: " + musicPath);
			if (File.Exists(musicPath))
			{
				emptySound = new Sound()
				{
					field_4060 = Path.GetFileNameWithoutExtension(musicPath),
					field_4061 = class_158.method_375(musicPath)
				};
				break;
			}
		}

		// add entry to the volume dictionary
		var field = typeof(class_11).GetField("field_52", BindingFlags.Static | BindingFlags.NonPublic);
		var dictionary = (Dictionary<string, float>)field.GetValue(null);
		dictionary.Add("empty", 0.2f);

		//The clock counts as music, so there's probably a while bunch of other methods I have to dig into to make work, ugh, TODO.

		// modify the method that reenables sounds after they are triggered
		void Method_540(On.class_201.orig_method_540 orig, class_201 class201_self)
		{
			orig(class201_self);
			sigmarMusic.field_4062 = false;
		}
		On.class_201.method_540 += Method_540;
	}

	// public static Texture ChooseCustomIconLarge(On.CampaignItem.orig_method_826 orig, CampaignItem item_self) // SOLITAIRE_ICON_TEMP
	// {
	// 	return item_self.field_2324 == CampaignLoader.typeSolitaire && customSolitaires.Contains(item_self.field_2326) ? iconSolitaire : orig(item_self);
	// }
	// public static Texture ChooseCustomIconSmall(On.CampaignItem.orig_method_827 orig, CampaignItem item_self) // SOLITAIRE_ICON_TEMP
	// {
	// 	return item_self.field_2324 == CampaignLoader.typeSolitaire && customSolitaires.Contains(item_self.field_2326) ? iconSolitaireSmall : orig(item_self);
	// }

    private void hotswapPartDescriptions(On.SolutionEditorScreen.orig_method_2107 orig, SolutionEditorScreen SES_self, string param_5715, string param_5716, Maybe<SDL.enum_160> param_5717)
    {
        if (CampaignLoader.currentCampaignIsTAC() && 
				(/*Harriet levels*/
				SES_self.method_502().method_1934().field_2766 == "tac-ch1-4-chelating-steam" ||
				SES_self.method_502().method_1934().field_2766 == "tac-ch4-25-synthesis-via-hope") && 
			param_5716 == class_191.field_1771.field_1530 /*berlo wheel description*/)
			{
				param_5715 = class_134.method_253("Spinning Paint Palette", string.Empty);
				param_5716 = class_134.method_253("By using the spinning paint palette with the glyph of duplication, neutral salt can be turned into any of the four cardinal elements.", string.Empty);
			}
		orig(SES_self, param_5715, param_5716, param_5717);
    }

	public void SES_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen SES_self, float param_5703)
	{
		Logger.Log(SES_self.method_502().method_1934().field_2766);
		//"Spinning Paint Palette" 
		string original_name = class_191.field_1771.field_1529; // Van Berlo's Wheel
		string original_desc = class_191.field_1771.field_1530; // Van Berlo's Wheel's descirption
		if (SES_self.method_502().method_1934().field_2766 == "tac-ch1-4-chelating-steam")
		{
			new DynamicData(class_191.field_1771).Set("field_1529","Spinning Paint Palette");
			new DynamicData(class_191.field_1771).Set("field_1530","By using the spinning paint palette with the glyph of duplication, neutral salt can be turned into any of the four cardinal elements.");
			orig(SES_self, param_5703);
			// put the original names back before anything else can see what we did
			new DynamicData(class_191.field_1771).Set("field_1529",original_name);
			new DynamicData(class_191.field_1771).Set("field_1530",original_desc);
		}
		orig(SES_self, param_5703);
	}
}
