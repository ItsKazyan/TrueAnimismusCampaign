//using Mono.Cecil.Cil;
//using MonoMod.Cil;
//using MonoMod.RuntimeDetour;
//using MonoMod.Utils;
using Quintessential;
using Quintessential.Serialization;
//using Quintessential.Settings;
//using SDL2;
using System;
using System.IO;
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
using PartTypes = class_191;
//using Texture = class_256;
using Song = class_186;
//using Tip = class_215;
//using Font = class_1;

public static class CampaignLoader
{
    const string FirstPuzzleID = "tac-ch1-1-bacon-and-antimony";
    private static Campaign campaign_self;
    private static CampaignModelTAC campaign_model;

    public const enum_129 typePuzzle = (enum_129)0;
    public const enum_129 typeCutscene = (enum_129)1;
    public const enum_129 typeDocument = (enum_129)2;
    public const enum_129 typeSolitaire = (enum_129)3;

    public static bool CurrentCampaignIsTAC() => campaign_self == Campaigns.field_2330;
    public static CampaignModelTAC getModel() => campaign_model;

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // helpers
    private static void patchCampaign(Campaign campaign)
    {
        foreach (CampaignChapter campaignChapter in campaign.field_2309)
        {
            foreach (CampaignItem campaignItem in campaignChapter.field_2314)
            {
                string field2322 = campaignItem.field_2322;
                if (campaignItem.field_2325.method_1085())
                {
                    Puzzle puzzle = campaignItem.field_2325.method_1087();
                    puzzle.field_2766 = field2322;
                    Array.Resize(ref Puzzles.field_2816, Puzzles.field_2816.Length + 1);
                    Puzzles.field_2816[Puzzles.field_2816.Length - 1] = puzzle;
                    foreach (PuzzleInputOutput puzzleInputOutput in puzzle.field_2770.Union(puzzle.field_2771))
                    {
                        if (!puzzleInputOutput.field_2813.field_2639.method_1085())
                            puzzleInputOutput.field_2813.field_2639 = (Maybe<LocString>)class_134.method_253("Molecule", string.Empty);
                    }
                }
            }
        }
    }
    public static void Load()
    {
        // load campaign model
        string filepath;
        if (!MainClass.findModMetaFilepath("TrueAnimismusCampaign", out filepath) || !File.Exists(filepath + "/Puzzles/TAC.advanced.yaml"))
        {
            Logger.Log("[TrueAnimismusCampaign] Could not find 'TAC.advanced.yaml' in the folder '" + filepath + "/Puzzles/'");
            throw new Exception("modifyCampaignTAC: Campaign data is missing.");
        }
        using (StreamReader streamReader = new StreamReader(filepath + "/Puzzles/TAC.advanced.yaml"))
        {
            campaign_model = YamlHelper.Deserializer.Deserialize<CampaignModelTAC>(streamReader);
        }

        // hooking
        On.Solution.method_1958 += Solution_Method_1958;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // tips
    static void initializeTips()
    {
        // manually load the puzzle file needed for tips
        string subpath = "/Puzzles/tac-ch0-0-modding-sandbox.puzzle.yaml";
        string filepath;
        if (!MainClass.findModMetaFilepath("TrueAnimismusCampaign", out filepath) || !File.Exists(filepath + subpath))
        {
            Logger.Log("[TrueAnimismusCampaign] Could not find 'tac-ch0-0-modding-sandbox.puzzle.yaml in the folder '" + filepath + "/Puzzles/'");
            throw new Exception("LoadPuzzleContent: Tip data is missing.");
        }
        var tipsPuzzle = PuzzleModel.FromModel(YamlHelper.Deserializer.Deserialize<PuzzleModel>(File.ReadAllText(filepath + subpath)));

        Array.Resize(ref Puzzles.field_2816, Puzzles.field_2816.Length + 1);
        Puzzles.field_2816[Puzzles.field_2816.Length - 1] = tipsPuzzle;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // puzzle-loader functions
    static Dictionary<string, Action<Puzzle>> LevelLoaders = new()
    {
        {FirstPuzzleID, LoadFirstPuzzle }
    };

    static void LoadFirstPuzzle(Puzzle puzzle) => StoryPanelPatcher.setOptionsUnlock(puzzle);

    #region polymer input/output puzzle loaders

    static void LoadPolymerOutputs(Puzzle puzzle) // REMOVE THIS ONCE QUINTESSENTIAL FIXES THE BUG
    {
        for (int i = 0; i < puzzle.field_2771.Length; i++)
        {
            var output = puzzle.field_2771[i];
            output.field_2813 = MoleculeEditorScreen.method_1133(output.field_2813, class_181.field_1716);
        }
    }

    #endregion

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // main functions

    public static void modifyCampaign()
    {
        // get song resources
        var songList = new Dictionary<string, Song>()
        {
            {"Map", class_238.field_1992.field_968},
            {"Solitaire", class_238.field_1992.field_969},
            {"Solving1", class_238.field_1992.field_970},
            {"Solving2", class_238.field_1992.field_971},
            {"Solving3", class_238.field_1992.field_972},
            {"Solving4", class_238.field_1992.field_973},
            {"Solving5", class_238.field_1992.field_974},
            {"Solving6", class_238.field_1992.field_975},
            {"Story1", class_238.field_1992.field_976},
            {"Story2", class_238.field_1992.field_977},
            {"Title", class_238.field_1992.field_978},
        };
        var fanfareList = new Dictionary<string, Sound>()
        {
            {"Map", class_238.field_1991.field_1832},
            {"Solitaire", class_238.field_1991.field_1832},
            {"Solving1", class_238.field_1991.field_1830},
            {"Solving2", class_238.field_1991.field_1831},
            {"Solving3", class_238.field_1991.field_1832},
            {"Solving4", class_238.field_1991.field_1833},
            {"Solving5", class_238.field_1991.field_1834},
            {"Solving6", class_238.field_1991.field_1835},
            {"Story1", class_238.field_1991.field_1832},
            {"Story2", class_238.field_1991.field_1832},
            {"Title", class_238.field_1991.field_1832},
        };

        // initialize tip and vial resources
        initializeTips();

        // fetch campaign data
        Logger.Log(QuintessentialLoader.AllCampaigns);
        foreach (Campaign campaign in QuintessentialLoader.AllCampaigns)
        {
            Logger.Log("Checking for campaign.QuintTitle == \"Decardination\"...");
            if (campaign.QuintTitle == "Decardination")
            {
                campaign_self = campaign;
                patchCampaign(campaign_self);
                break;
            }
        }
        Dictionary<string, PuzzleModelTAC> puzzleDictionary = new Dictionary<string, PuzzleModelTAC>();
        List<string> documentIDList = new List<string>();
        Dictionary<string, string> cutsceneIDList = new Dictionary<string, string>();
        List<string> sigmarsGardensIDList = new List<string>();
        StoryPanelPatcher.CreateSigmarStoryUnlocks(campaign_model.SigmarStoryUnlocks);

        foreach (PuzzleModelTAC puzzle in campaign_model.Puzzles)
        {
            puzzleDictionary.Add(puzzle.ID, puzzle);
        }
        var DocumentModels = campaign_model.Documents;
        campaign_model.LoadDocuments();
        foreach (var document in DocumentModels)
        {
            documentIDList.Add(document.ID);
        }
        var CutsceneModels = campaign_model.Cutscenes;
        CutscenePatcher.LoadCutscenes(campaign_model.Cutscenes);
        foreach (var cutscene in campaign_model.Cutscenes)
        {
            bool blank = string.IsNullOrEmpty(cutscene.Music);
            cutsceneIDList.Add(cutscene.ID, blank ? "Story1" : cutscene.Music);
        }
        foreach (var garden in campaign_model.SigmarsGardens)
        {
            sigmarsGardensIDList.Add(garden);
        }
        ////////////////////////////////////////
        // modify the campaign using the data //
        ////////////////////////////////////////
        Logger.Log("[TrueAnimismusCampaign] Modifying campaign levels.");
        CampaignChapter[] campaignChapters = campaign_self.field_2309;
        foreach (var campaignChapter in campaignChapters)
        {
            if (campaignChapter.field_2310 == 1) campaignChapter.field_2321 = true;

            foreach (var campaignItem in campaignChapter.field_2314)
            {
                // convert unlock requirement as necessary
                if (campaignItem.field_2326.GetType() == typeof(class_243))
                {
                    class_243 stringUnlockRequirement = (class_243)campaignItem.field_2326;
                    int n;
                    if (int.TryParse(stringUnlockRequirement.field_2005, out n))
                    {
                        campaignItem.field_2326 = new class_265(n);
                    }
                }
                // modifiy puzzle data as necessary
                if (campaignItem.field_2325.method_1085())
                {
                    Puzzle puzzle = campaignItem.field_2325.method_1087();
                    string puzzleID = puzzle.field_2766;

                    // run hard-coded stuff
                    if (LevelLoaders.ContainsKey(puzzleID)) LevelLoaders[puzzleID](puzzle);

                    // modify using stuff in advanced.yaml

                    //campaignItem.field_2324 defaults to (enum_129)0, i.e. a puzzle
                    if (cutsceneIDList.Keys.Contains(puzzleID))
                    {
                        // change item into a cutscene
                        campaignItem.field_2324 = typeCutscene;
                        campaignItem.field_2328 = songList[cutsceneIDList[puzzleID]];
                    }
                    else if (documentIDList.Contains(puzzleID))
                    {
                        // change item into a document
                        campaignItem.field_2324 = typeDocument;
                    }
                    else if (sigmarsGardensIDList.Contains(puzzleID))
                    {
                        // change item into a Sigmars Garden
                        campaignItem.field_2324 = typeSolitaire;
                        MainClass.customSolitaires.Add(campaignItem.field_2326); // SOLITAIRE_ICON_TEMP
                    }
                    else // keep it as a puzzle
                    {
                        if (puzzleDictionary.ContainsKey(puzzleID))
                        {
                            LoadPolymerOutputs(puzzle);
                            PuzzleModelTAC puzzleModel = puzzleDictionary[puzzleID];

                            puzzle.field_2780 = puzzleModel.OutputMultiplier;

                            if (puzzleModel.NoStoryPanel)
                            {
                                campaignItem.field_2327 = (Maybe<class_264>)struct_18.field_1431;
                            }

                            if (!string.IsNullOrEmpty(puzzleModel.Music) && songList.Keys.Contains(puzzleModel.Music))
                            {
                                campaignItem.field_2328 = songList[puzzleModel.Music];
                                campaignItem.field_2329 = fanfareList[puzzleModel.Music];
                            }

                            if (puzzleModel.Tip != null)
                            {
                                puzzle.field_2769 = puzzleModel.Tip.FromModel();
                            }

                            if (puzzleModel.Cabinet != null)
                            {
                                puzzleModel.Cabinet.ModifyCabinet(puzzle);
                            }
                        }
                    }
                }
            }
        }

        //JournalLoader.modifyJournals(campaign_self);
    }

    public static Maybe<Solution> Solution_Method_1958(On.Solution.orig_method_1958 orig, string filePath)
    {
        foreach (var dir in QuintessentialLoader.ModContentDirectories)
        {
            try
            {
                return orig(Path.Combine(dir, filePath));
            }
            catch (Exception) { }
        }

        return orig(filePath);
    }
}