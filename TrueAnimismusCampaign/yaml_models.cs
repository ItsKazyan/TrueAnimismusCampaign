//using Mono.Cecil.Cil;
//using MonoMod.Cil;
//using MonoMod.RuntimeDetour;
//using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
//using Quintessential.Settings;
//using SDL2;
using System;
using System.IO;
//using System.Linq;
using System.Collections.Generic;
using System.Globalization;
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
using Tip = class_215;
//using Font = class_1;

/////////////////////////////////////////////////////////////////////////////////////////////////
// advanced.yaml

public static class ModelHelpersTAC
{
	static NumberStyles style = NumberStyles.Any;
	static NumberFormatInfo format = CultureInfo.InvariantCulture.NumberFormat;

	public static float FloatFromString(string str, float defaulF = 0f)
	{
		if (!string.IsNullOrEmpty(str))
		{
			return float.Parse(str, style, format);
		}
		else
		{
			return defaulF;
		}
	}

	public static Vector2 Vector2FromString(string pos, float defaultX = 0f, float defaultY = 0f)
	{
		float x = FloatFromString(pos?.Split(',')[0], defaultX);
		float y = FloatFromString(pos?.Split(',')[1], defaultY);
		return new Vector2(x, y);
	}

	public static Color HexColor(int hex)
	{
		return Color.FromHex(hex);
	}

	public static Color ColorWhite => Color.White;
}

public class CampaignModelTAC
{
	public CreditsModelTAC Credits;
	public List<int> SigmarStoryUnlocks;
	public List<string> SigmarsGardens;
	public List<CharacterModelTAC> Characters;
	public List<CutsceneModelTAC> Cutscenes;
	public List<DocumentModelTAC> Documents;
	public List<PuzzleModelTAC> Puzzles;

	public void LoadDocuments()
	{
		foreach (var document in this.Documents)
		{
			document.AddDocumentFromModel();
		}
	}
}
public class CreditsModelTAC
{
	public string PositionOffset;
	public List<List<string>> Texts;
}
public class CharacterModelTAC
{
	public string ID, Name, SmallPortrait, LargePortrait;
	public int Color;
	public bool IsOnLeft;

	Texture actorSmall, actorLarge;

	public class_230 FromModel()
	{
		if (!string.IsNullOrEmpty(this.SmallPortrait))
		{
			this.actorSmall ??= class_235.method_615(this.SmallPortrait); // if null, load the texture
		}
		if (!string.IsNullOrEmpty(this.LargePortrait))
		{
			this.actorLarge ??= class_235.method_615(this.LargePortrait); // if null, load the texture
		}

		if (!string.IsNullOrEmpty(this.SmallPortrait))
			actorSmall = class_235.method_615(this.SmallPortrait);
		if (!string.IsNullOrEmpty(this.LargePortrait))
			actorLarge = class_235.method_615(this.LargePortrait);

		return new class_230(class_134.method_253(this.Name, string.Empty), actorLarge, actorSmall, ModelHelpersTAC.HexColor(this.Color), this.IsOnLeft);
	}
}
public class CutsceneModelTAC
{
	public string ID, Location, Background, Music;
}
public class DocumentModelTAC
{
	public string ID, Texture;
	public List<DrawItemModelTAC> DrawItems;

	public void AddDocumentFromModel()
	{
		Texture base_texture = class_238.field_1989.field_85.field_570; // letter-5
		if (!string.IsNullOrEmpty(this.Texture))
		{
			base_texture = class_235.method_615(this.Texture);
		}
		List<Document.DrawItem> drawItems = new();

		if (this.DrawItems != null)
		{
			foreach (var drawItem in this.DrawItems)
			{
				drawItems.Add(drawItem.FromModel());
			}
		}
		new Document(this.ID, base_texture, drawItems);
	}
}
public class DrawItemModelTAC
{
	public string Position, Texture, Rotation, Scale, Alpha, Font, Color, Align, LineSpacing, ColumnWidth;
	public bool Handwritten;

	public Document.DrawItem FromModel()
	{
		bool isImageItem = !string.IsNullOrEmpty(this.Texture);

		// image AND text properties
		Color color = isImageItem ? ModelHelpersTAC.ColorWhite : DocumentScreen.field_2410;
		if (!string.IsNullOrEmpty(this.Color)) color = ModelHelpersTAC.HexColor(int.Parse(this.Color));
		Vector2 position = ModelHelpersTAC.Vector2FromString(this.Position);

		if (isImageItem)
		{
			return new Document.DrawItem(
				position,
				class_235.method_615(this.Texture),
				color,
				ModelHelpersTAC.FloatFromString(this.Scale, 1f),
				ModelHelpersTAC.FloatFromString(this.Rotation),
				ModelHelpersTAC.FloatFromString(this.Alpha, 1f)
			);
		}
		else // isTextItem
		{
			return new Document.DrawItem(
				position,
				Document.DrawItem.getFont(this.Font),
				color,
				Document.DrawItem.getAlignment(this.Align),
				ModelHelpersTAC.FloatFromString(this.LineSpacing, 1f),
				ModelHelpersTAC.FloatFromString(this.ColumnWidth, float.MaxValue),
				this.Handwritten
			);
		}
	}
}
//////////////////////////////////////////////////
public class PuzzleModelTAC
{
	public string ID, Music;
	public bool NoStoryPanel = false;
	public int OutputMultiplier = 1;
	public TipModelTAC Tip = null;
	public CabinetModelTAC Cabinet;
}

public class TipModelTAC
{
	public string ID, Title, Description, Solution, Texture, SolutionOffset;
	Texture loadedTexture;

	public Tip FromModel()
	{
		Maybe<Texture> image = (Maybe<Texture>)struct_18.field_1431;

		if (!string.IsNullOrEmpty(this.Texture))
		{
			this.loadedTexture ??= class_235.method_615(this.Texture); // if null, load the texture
			image = this.loadedTexture;
		}

		return new Tip()
		{
			field_1899 = this.ID,
			field_1900 = class_134.method_253(this.Title ?? "<Untitled Tip>", string.Empty),
			field_1901 = class_134.method_253(this.Description ?? "<Description Missing>", string.Empty),
			field_1902 = this.Solution ?? "speedbonder",
			field_1903 = image,
			field_1904 = ModelHelpersTAC.Vector2FromString(this.SolutionOffset),
		};
	}


}

public class CabinetModelTAC
{
	public bool ExpandLeft, ExpandRight;
	public List<ConduitModelTAC> Conduits;
	public List<VialHolderModelTAC> VialHolders;
	public List<OverlayModelTAC> Overlays;

	public void ModifyCabinet(Puzzle puzzle)
	{
		var puzzleID = puzzle.field_2766;
		if (!puzzle.field_2779.method_1085())
		{
			Logger.Log("[TrueAnimismusCampaign] Puzzle '" + puzzleID + "' is not a production puzzle - ignoring the cabinet data.");
			return;
		}

		var productionData = puzzle.field_2779.method_1087();
		productionData.field_2075 = !this.ExpandLeft;
		productionData.field_2076 = !this.ExpandRight;

		if (this.Conduits != null)
		{
			productionData.field_2072 = new class_117[this.Conduits.Count];
			for (int i = 0; i < this.Conduits.Count; i++)
			{
				productionData.field_2072[i] = this.Conduits[i].FromModel();
			}
		}

		if (this.Overlays != null)
		{
			ProductionManager.AddOverlaysForPuzzle(puzzleID, this.Overlays);
		}

		if (this.VialHolders != null)
		{
			productionData.field_2073 = new class_128[this.VialHolders.Count];
			for (int i = 0; i < this.VialHolders.Count; i++)
			{
				productionData.field_2073[i] = this.VialHolders[i].FromModel();
			}
		}

		// fix and update the cabinet bounding box
		puzzle.method_1247();
	}


	public class ConduitModelTAC
	{
		public string Position1, Position2;
		public List<string> Hexes;

		public class_117 FromModel()
		{
			int Q1 = int.Parse(this.Position1.Split(',')[0]);
			int R1 = int.Parse(this.Position1.Split(',')[1]);
			int Q2 = int.Parse(this.Position2.Split(',')[0]);
			int R2 = int.Parse(this.Position2.Split(',')[1]);
			var hexList = new HexIndex[this.Hexes.Count];
			for (int j = 0; j < this.Hexes.Count; j++)
			{
				var hex = this.Hexes[j];
				hexList[j] = new HexIndex(int.Parse(hex.Split(',')[0]), int.Parse(hex.Split(',')[1]));
			}
			return new class_117(Q1, R1, Q2, R2, hexList);
		}
	}
	public class VialHolderModelTAC
	{
		public string Position;
		public bool TopSide;
		public List<VialModelTAC> Vials;

		public class_128 FromModel()
		{
			var vials = new Tuple<Texture, Texture>[this.Vials.Count];

			for (int j = 0; j < this.Vials.Count; j++)
			{
				var vial = this.Vials[j];
				vials[j] = Tuple.Create(ProductionManager.fetchTexture(vial.TextureSim), ProductionManager.fetchTexture(vial.TextureGif));
			}

			return new class_128(int.Parse(this.Position.Split(',')[0]), int.Parse(this.Position.Split(',')[1]), this.TopSide, vials);
		}
	}
	public class VialModelTAC
	{
		public string TextureSim, TextureGif;
	}
	public class OverlayModelTAC
	{
		public string Texture, Position;
	}
}

/////////////////////////////////////////////////////////////////////////////////////////////////
// journal.yaml

public class JournalModelTAC
{
	public List<JournalVolumeModelTAC> Volumes;
	public List<JournalPreviewModelTAC> Previews;

	public Dictionary<string, Dictionary<int, Vector2>> GetPreviewPositions()
	{
		Dictionary<string, Dictionary<int, Vector2>> dict = new();

		foreach (var preview in Previews)
		{
			var tuple = preview.FromModel();
			dict.Add(tuple.Item1, tuple.Item2);
		}

		return dict;
	}
}
public class JournalVolumeModelTAC
{
	public int FromChapter;
	public string Title, Description;
}

public class JournalPreviewModelTAC
{
	public string ID;
	public List<JournalPreviewItemModelTAC> Items;

	public Tuple<string, Dictionary<int, Vector2>> FromModel()
	{
		Dictionary<int, Vector2> items = new();
		foreach (var item in Items)
		{
			var tuple = item.FromModel();
			items.Add(tuple.Item1, tuple.Item2);
		}
		
		return Tuple.Create(ID, items);
	}
}

public class JournalPreviewItemModelTAC
{
	public int Index;
	public string Position;

	public Tuple<int, Vector2> FromModel()
	{
		return Tuple.Create(Index, ModelHelpersTAC.Vector2FromString(Position));
	}
}