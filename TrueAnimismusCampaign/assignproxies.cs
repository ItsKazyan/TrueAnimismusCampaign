using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
//using Quintessential.Settings;
//using SDL2;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
//using System.Globalization;
using System.Reflection;
using TrueAnimismus;
using System.Dynamic;

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

public class AssignProxies
{
    const string DisprolevelID = "tac-ch1-7-disproportionated-salt";
    const string NoProxyError = "No proxy available for this alchemical prime. Resume to assign proxies automatically.";

    public static Texture yellow_error_highlight;

    /////////////////////////////////////////////////////////////////////////////////////////////////
    // helpers
    private static void SetTrueAnProxiesAssigned() => GameLogic.field_2434.field_2451.field_1929.method_858("TAC-ProxiesAssigned", true.method_453());
    public static bool GetTrueAnProxiesAssigned() => GameLogic.field_2434.field_2451.field_1929.method_862(new delegate_384<bool>(bool.TryParse), "TAC-ProxiesAssigned").method_1090(false);

    private static Vector2 HexGraphicalOffset(HexIndex hex) => class_187.field_1742.method_492(hex);

    public static void LoadContent()
    {
        string path = "textures/";
        yellow_error_highlight = class_235.method_615(path + "highlight_yellow");
    }


    public static void PostLoad() { }

    public static void My_Method_1835(Sim sim_self)
    {//So I should probably be doing this in method_1829()
     // it's the one that makes the sim stop if you put a retract instruction on a non-piston etc., so I think it's less computationally intensive
     // But whatever
        DynamicData sim_dyn = new DynamicData(sim_self);
        var solutionEditorBase = sim_self.field_3818;
        var solution = solutionEditorBase.method_502();
        var field3919 = solution.field_3919;
        var struct122List1 = sim_self.field_3826;

        //Only do the custom error if we're in the first Dispro puzzle and we haven't seen it before:
        if (solution.method_1934().field_2766 != DisprolevelID) { return; }
        if (GetTrueAnProxiesAssigned()) { return; }

        List<Vector2> vector2List = new();
        List<Sim.struct_122> struct122List2 = new();
        foreach (var part in field3919.Where(x => x.method_1159() == Glyphs.Disproportion || x.method_1159() == Glyphs.DisproportionR))
        {
            HexIndex hexOutputHi;
            if (part.method_1159() == Glyphs.Disproportion)
            { hexOutputHi = new HexIndex(-1, 0); }
            else { hexOutputHi = new HexIndex(1, 0); }
            vector2List.Add(HexGraphicalOffset(part.method_1184(hexOutputHi))); //The irises that can make Red Vitae
        }
        if (vector2List.Count == 0)
            return;

        foreach (Part disproglyph in solution.field_3919.Where(
            x => x.method_1159() == Glyphs.Disproportion || x.method_1159() == Glyphs.DisproportionR))
        {
            if (!solutionEditorBase.method_507().method_481(disproglyph).field_2743) { continue; }

            //Understood all that up there?
            //Don't worry
            //TL;DR If the code gets here that means there's a Glyph of Disproportion firing
            //PULL THE LEVER, KRONK

            SetTrueAnProxiesAssigned();
            HexIndex hexOutputHi;
            if (disproglyph.method_1159() == Glyphs.Disproportion)
            { hexOutputHi = new HexIndex(-1, 0); }
            else { hexOutputHi = new HexIndex(1, 0); }

            Vector2 wheretodraw = class_187.field_1742.method_492(disproglyph.method_1184(hexOutputHi));
            solutionEditorBase.method_518(0f, (string)class_134.method_253(NoProxyError, string.Empty), new Vector2[1] { wheretodraw });
            return;
        }
    }

    private static ILHook ybhook;
    public static void YellowBoxHook()
    {
        ybhook = new ILHook
            (
                typeof(SolutionEditorScreen).GetMethod("method_50", BindingFlags.Public | BindingFlags.Instance),
                YellowBox
            );
    }

    private static void YellowBox(ILContext il)
    {
        // Have to muck around in the middle of the error-drawing code so it's time for another Cursor Moment
        var gremlin = new ILCursor(il);

        //Here-ish
        gremlin.Goto(600);

        // Here exactly
        if (gremlin.TryGotoNext(MoveType.After,
        x => x.MatchLdsfld(out _),
        x => x.MatchLdfld(out _),
        x => x.MatchLdfld(out _),
        x => x.MatchLdfld(out _),
        x => x.MatchStloc(25)
            //25th local variable in the method--where the red box texture is being loaded into
            ))
            //Gimme that for a sec
            gremlin.Emit(OpCodes.Ldloc, 25);
        //I also need the class with the error message, which is local variable number [checks disassmbler] 5
        gremlin.Emit(OpCodes.Ldloc, 5);

        //Logger.Log("gremlin.EmitDelegate<Action<Texture, class_232>>((tex,ermes) => ");
        gremlin.EmitDelegate<Func<Texture, class_232, Texture>>((tex, ermes) =>
            {
                if (ermes.field_1964 == NoProxyError)
                { tex = AssignProxies.yellow_error_highlight; }
                return tex;
            });
        //And now this is the texture
        gremlin.Emit(OpCodes.Stloc, 25);
    }
    public static void Unload() => ybhook?.Dispose();
}
