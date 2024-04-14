﻿//using Mono.Cecil.Cil;
//using MonoMod.Cil;
using MonoMod.RuntimeDetour;
//using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
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
//using PartTypes = class_191;
using Texture = class_256;
using Song = class_186;
//using Tip = class_215;
using Font = class_1;

public sealed class JournalLoader
{
    private static JournalModelTAC journal_model;

    private static IDetour hook_JournalScreen_method_1040;

    private static List<CampaignItem> journal_items = new();

    public static IEnumerable<CampaignItem> journalItems() => journal_items;

    static Dictionary<string, Dictionary<int, Vector2>> customPreviewPositions = new();

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // journal stuff
    public static void loadJournalModel()
    {
        string filepath;
        if (!MainClass.findModMetaFilepath("TrueAnimismusCampaign", out filepath) || !File.Exists(filepath + "/Puzzles/TAC.journal_TAC.yaml"))
        {
            Logger.Log("[TrueAnimismusCampaign] Could not find 'TAC.journal_TAC.yaml' in the folder '" + filepath + "/Puzzles/'");
            throw new Exception("modifyCampaignTAC: Journal data is missing.");
        }
        using (StreamReader streamReader = new StreamReader(filepath + "/Puzzles/TAC.journal_TAC.yaml"))
        {
            journal_model = YamlHelper.Deserializer.Deserialize<JournalModelTAC>(streamReader);
        }

        customPreviewPositions = journal_model.GetPreviewPositions();
    }

    public static void modifyJournals(Campaign campaign_self)
    {
        CampaignChapter[] campaignChapters = campaign_self.field_2309;
        int maxChapter = campaignChapters.Length - 1;

        HashSet<int> chaptersToRemove = new();

        foreach (var volume in journal_model.Volumes)
        {
            var volumeIndex = volume.FromChapter;
            if (volumeIndex > maxChapter || volumeIndex < 0)
            {
                Logger.Log("[TrueAnimismusCampaign] Invalid FromChapter value for journal page '" + volume.Title + "', ignoring.'");
                continue;
            }
            if (chaptersToRemove.Contains(volumeIndex))
            {
                Logger.Log("[TrueAnimismusCampaign] Already consumed chapter '" + volumeIndex + "' for the journal, ignoring.'");
                continue;
            }

            var chapter = campaignChapters[volumeIndex];
            var items = chapter.field_2314;

            List<CampaignItem> itemForJournal = new();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.field_2324 == CampaignLoader.typePuzzle && item.field_2325.method_1085())
                {
                    itemForJournal.Add(item);
                }
            }
            if (itemForJournal.Count < 5)
            {
                Logger.Log("[TrueAnimismusCampaign] Insufficient puzzles in chapter '" + volumeIndex + "' to make a journal page, ignoring.'");
                continue;
            }

            var newJournalVolume = new JournalVolume()
            {
                field_2569 = volume.Title,
                field_2570 = volume.Description,
                field_2571 = new Puzzle[5]
            };

            for (int i = 0; i < 5; i++)
            {
                journal_items.Add(itemForJournal[i]);
                newJournalVolume.field_2571[i] = itemForJournal[i].field_2325.method_1087();
            }

            Array.Resize(ref JournalVolumes.field_2572, JournalVolumes.field_2572.Length + 1);
            JournalVolumes.field_2572[JournalVolumes.field_2572.Length - 1] = newJournalVolume;

            chaptersToRemove.Add(volumeIndex);
            Logger.Log("[TrueAnimismusCampaign] Converted chapter " + volumeIndex + " into a journal page.");
        }

        CampaignChapter[] newCampaignChapters = new CampaignChapter[campaignChapters.Length - chaptersToRemove.Count];

        int j = 0;
        for (int i = 0; i < campaignChapters.Length; i++)
        {
            if (!chaptersToRemove.Contains(i))
            {
                newCampaignChapters[j] = campaignChapters[i];
                j++;
            }
        }

        campaign_self.field_2309 = newCampaignChapters;
    }

    public static void Load()
    {
        hook_JournalScreen_method_1040 = new Hook(MainClass.PrivateMethod<JournalScreen>("method_1040"), OnJournalScreen_Method_1040);
    }
    private delegate void orig_JournalScreen_method_1040(JournalScreen screen_self, Puzzle puzzle, Vector2 basePosition, bool isLargePuzzle);


    public static void Unload()
    {
        hook_JournalScreen_method_1040.Dispose();
    }

    private static void OnJournalScreen_Method_1040(orig_JournalScreen_method_1040 orig, JournalScreen screen_self, Puzzle puzzle, Vector2 basePosition, bool isLargePuzzle)
    {
        if (!journal_items.Select(x => x.field_2325.method_1087()).Contains(puzzle))
        {
            orig(screen_self, puzzle, basePosition, isLargePuzzle);
            return;
        }
        var item = journal_items.Where(x => x.field_2325.method_1087() == puzzle).First();
        bool puzzleSolved = GameLogic.field_2434.field_2451.method_573(puzzle);
        Font crimson_15 = class_238.field_1990.field_2144;
        bool authorExists = puzzle.field_2768.method_1085();
        string authorName() => puzzle.field_2768.method_1087();
        string displayString = authorExists ? string.Format("{0} ({1})", puzzle.field_2767, authorName()) : (string)puzzle.field_2767;

        Texture moleculeBackdrop = isLargePuzzle ? class_238.field_1989.field_88.field_894 : class_238.field_1989.field_88.field_895;
        Texture divider = isLargePuzzle ? class_238.field_1989.field_88.field_892 : class_238.field_1989.field_88.field_893;
        Texture solvedCheckbox = puzzleSolved ? class_238.field_1989.field_96.field_879 : class_238.field_1989.field_96.field_882;
        class_135.method_290(displayString, basePosition + new Vector2(9f, -19f), crimson_15, class_181.field_1718, (enum_0)0, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), null, int.MaxValue, false, true);
        Vector2 vector2_1 = basePosition + new Vector2(moleculeBackdrop.field_2056.X - 27, -23f);
        class_135.method_272(solvedCheckbox, vector2_1);
        class_135.method_272(divider, basePosition + new Vector2(isLargePuzzle ? 7f : 7f, -34f));
        class_135.method_272(moleculeBackdrop, basePosition);

        Bounds2 bounds2 = Bounds2.WithSize(basePosition, moleculeBackdrop.field_2056.ToVector2());
        bool mouseHover = bounds2.Contains(Input.MousePos());
        var puzzleID = puzzle.field_2766;

        Vector2 moleculeOffset = isLargePuzzle ? new Vector2(470f, 365f) : new Vector2(280f, 200f);
        Texture textureFromMolecule(Molecule molecule, Vector2 offset) => Editor.method_928(molecule, false, mouseHover, offset, isLargePuzzle, (Maybe<float>)struct_18.field_1431).method_1351().field_937;
        Texture textureFromIndex(int i, Vector2 offset) => textureFromMolecule(puzzle.field_2771[i].field_2813, offset);

        if (customPreviewPositions.ContainsKey(puzzleID))
        {
            foreach (var kvp in customPreviewPositions[puzzleID])
            {
                class_135.method_272(textureFromIndex(kvp.Key, moleculeOffset), bounds2.Min + kvp.Value);
            }
        }
        else
        {
            var molecules = puzzle.field_2771.Select(x => x.field_2813).OrderByDescending(x => x.method_1100().Count);
            Texture moleculeTexture = textureFromMolecule(molecules.First(), moleculeOffset);
            Vector2 vector2_4 = (moleculeTexture.field_2056.ToVector2() / 2).Rounded();
            class_135.method_272(moleculeTexture, bounds2.Center.Rounded() - vector2_4 + new Vector2(2f, 2f));
        }
        if (mouseHover && Input.IsLeftClickPressed())
        {
            Song song = item.field_2328;
            Sound fanfare = item.field_2329;
            Maybe<class_264> maybeStoryPanel = item.field_2327;

            GameLogic.field_2434.method_946(new PuzzleInfoScreen(puzzle, song, fanfare, maybeStoryPanel));
            class_238.field_1991.field_1821.method_28(1f);
        }

    }
}



