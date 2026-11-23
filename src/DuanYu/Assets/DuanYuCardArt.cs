using System.Globalization;
using System.Text;

namespace DuanYu.Assets;

internal static class DuanYuCardArt
{
    private const string Ink = "#101F2D";
    private const string Paper = "#F4EDDA";
    private const string Gold = "#E9C882";

    private enum Theme { Qi, Step, Sword, Scholar, Blocked, Foreign }
    private enum Scene { Finger, Ward, Vortex, Footwork, Meridian, Palm, Book, Rivers, Meditation, Temple, Sword, Lotus, Stars, Aid, Sleeve, Shockwave, Awakening, Renewal, Obstruction, BorrowedQi, Furnace, AncientStele }
    private readonly record struct Design(Scene Scene, Theme Theme, int Variant);
    private readonly record struct Palette(string Sky, string Horizon, string Mountain, string Accent, string Light);

    private static readonly IReadOnlyDictionary<string, Design> Designs = new Dictionary<string, Design>(StringComparer.Ordinal)
    {
        ["DY_B01"] = new(Scene.Finger, Theme.Scholar, 1),
        ["DY_B02"] = new(Scene.Ward, Theme.Scholar, 1),
        ["DY_B03"] = new(Scene.Vortex, Theme.Qi, 1),
        ["DY_B04"] = new(Scene.Footwork, Theme.Step, 1),
        ["DY_B05"] = new(Scene.Finger, Theme.Qi, 2),
        ["DY_C01"] = new(Scene.Vortex, Theme.Qi, 2),
        ["DY_C02"] = new(Scene.Palm, Theme.Qi, 1),
        ["DY_C03"] = new(Scene.Meridian, Theme.Scholar, 1),
        ["DY_C04"] = new(Scene.Finger, Theme.Scholar, 3),
        ["DY_C05"] = new(Scene.Palm, Theme.Scholar, 2),
        ["DY_C06"] = new(Scene.Finger, Theme.Step, 4),
        ["DY_C07"] = new(Scene.Sword, Theme.Sword, 1),
        ["DY_C08"] = new(Scene.Sword, Theme.Sword, 2),
        ["DY_C09"] = new(Scene.Meditation, Theme.Qi, 1),
        ["DY_C10"] = new(Scene.Meditation, Theme.Qi, 2),
        ["DY_C11"] = new(Scene.Footwork, Theme.Step, 2),
        ["DY_C12"] = new(Scene.Footwork, Theme.Step, 3),
        ["DY_C13"] = new(Scene.Footwork, Theme.Step, 4),
        ["DY_C14"] = new(Scene.Book, Theme.Scholar, 1),
        ["DY_C15"] = new(Scene.Ward, Theme.Qi, 2),
        ["DY_C16"] = new(Scene.Footwork, Theme.Qi, 5),
        ["DY_C17"] = new(Scene.Meridian, Theme.Qi, 2),
        ["DY_C18"] = new(Scene.Sword, Theme.Sword, 4),
        ["DY_C19"] = new(Scene.Book, Theme.Sword, 2),
        ["DY_C20"] = new(Scene.Palm, Theme.Scholar, 3),
        ["DY_C21"] = new(Scene.Renewal, Theme.Qi, 3),
        ["DY_C22"] = new(Scene.Renewal, Theme.Step, 13),
        ["DY_C23"] = new(Scene.Shockwave, Theme.Qi, 3),
        ["DY_U01"] = new(Scene.Vortex, Theme.Qi, 3),
        ["DY_U02"] = new(Scene.Vortex, Theme.Qi, 4),
        ["DY_U03"] = new(Scene.Rivers, Theme.Qi, 1),
        ["DY_U04"] = new(Scene.Meridian, Theme.Qi, 3),
        ["DY_U05"] = new(Scene.Ward, Theme.Qi, 3),
        ["DY_U06"] = new(Scene.Meridian, Theme.Qi, 4),
        ["DY_U07"] = new(Scene.Finger, Theme.Qi, 5),
        ["DY_U08"] = new(Scene.Shockwave, Theme.Qi, 1),
        ["DY_U09"] = new(Scene.Sword, Theme.Qi, 7),
        ["DY_U10"] = new(Scene.Sword, Theme.Sword, 3),
        ["DY_U11"] = new(Scene.Sword, Theme.Sword, 5),
        ["DY_U12"] = new(Scene.Sword, Theme.Sword, 6),
        ["DY_U13"] = new(Scene.Sword, Theme.Sword, 8),
        ["DY_U14"] = new(Scene.Finger, Theme.Sword, 6),
        ["DY_U15"] = new(Scene.Sword, Theme.Sword, 9),
        ["DY_U16"] = new(Scene.Lotus, Theme.Sword, 1),
        ["DY_U17"] = new(Scene.Shockwave, Theme.Qi, 2),
        ["DY_U18"] = new(Scene.Footwork, Theme.Step, 6),
        ["DY_U19"] = new(Scene.Stars, Theme.Step, 1),
        ["DY_U20"] = new(Scene.Footwork, Theme.Step, 7),
        ["DY_U21"] = new(Scene.Footwork, Theme.Step, 8),
        ["DY_U22"] = new(Scene.Sleeve, Theme.Qi, 1),
        ["DY_U23"] = new(Scene.Footwork, Theme.Step, 9),
        ["DY_U24"] = new(Scene.Footwork, Theme.Step, 10),
        ["DY_U25"] = new(Scene.Footwork, Theme.Step, 11),
        ["DY_U26"] = new(Scene.Book, Theme.Scholar, 3),
        ["DY_U27"] = new(Scene.BorrowedQi, Theme.Foreign, 1),
        ["DY_U28"] = new(Scene.Furnace, Theme.Qi, 1),
        ["DY_U29"] = new(Scene.Aid, Theme.Qi, 1),
        ["DY_U30"] = new(Scene.Aid, Theme.Scholar, 2),
        ["DY_U31"] = new(Scene.Finger, Theme.Qi, 7),
        ["DY_U32"] = new(Scene.Meridian, Theme.Qi, 5),
        ["DY_U33"] = new(Scene.Finger, Theme.Qi, 8),
        ["DY_U34"] = new(Scene.Stars, Theme.Step, 2),
        ["DY_U35"] = new(Scene.Renewal, Theme.Qi, 1),
        ["DY_U36"] = new(Scene.BorrowedQi, Theme.Foreign, 3),
        ["DY_U37"] = new(Scene.Furnace, Theme.Qi, 4),
        ["DY_U38"] = new(Scene.Book, Theme.Scholar, 6),
        ["DY_R01"] = new(Scene.Vortex, Theme.Qi, 5),
        ["DY_R02"] = new(Scene.Footwork, Theme.Step, 12),
        ["DY_R03"] = new(Scene.Sword, Theme.Sword, 10),
        ["DY_R04"] = new(Scene.Rivers, Theme.Qi, 2),
        ["DY_R05"] = new(Scene.Shockwave, Theme.Qi, 3),
        ["DY_R06"] = new(Scene.Finger, Theme.Qi, 9),
        ["DY_R07"] = new(Scene.Sword, Theme.Sword, 11),
        ["DY_R08"] = new(Scene.Meridian, Theme.Sword, 6),
        ["DY_R09"] = new(Scene.Sword, Theme.Sword, 12),
        ["DY_R10"] = new(Scene.Sword, Theme.Sword, 13),
        ["DY_R11"] = new(Scene.Temple, Theme.Step, 1),
        ["DY_R12"] = new(Scene.Book, Theme.Qi, 4),
        ["DY_R13"] = new(Scene.Finger, Theme.Qi, 10),
        ["DY_R14"] = new(Scene.Awakening, Theme.Scholar, 1),
        ["DY_R15"] = new(Scene.Temple, Theme.Qi, 2),
        ["DY_R16"] = new(Scene.Lotus, Theme.Scholar, 3),
        ["DY_R17"] = new(Scene.Aid, Theme.Step, 3),
        ["DY_R18"] = new(Scene.Ward, Theme.Sword, 4),
        ["DY_R19"] = new(Scene.Renewal, Theme.Scholar, 2),
        ["DY_R20"] = new(Scene.Lotus, Theme.Qi, 4),
        ["DY_R21"] = new(Scene.Sword, Theme.Sword, 14),
        ["DY_R22"] = new(Scene.Furnace, Theme.Qi, 5),
        ["DY_A01"] = new(Scene.AncientStele, Theme.Sword, 1),
        ["DY_T01"] = new(Scene.Sword, Theme.Sword, 21),
        ["DY_T02"] = new(Scene.Sword, Theme.Sword, 22),
        ["DY_T03"] = new(Scene.Sword, Theme.Sword, 23),
        ["DY_T04"] = new(Scene.Sword, Theme.Sword, 24),
        ["DY_T05"] = new(Scene.Sword, Theme.Sword, 25),
        ["DY_T06"] = new(Scene.Sword, Theme.Sword, 26),
        ["DY_S01"] = new(Scene.Obstruction, Theme.Blocked, 1),
        ["DY_S02"] = new(Scene.BorrowedQi, Theme.Foreign, 2)
    };

    public static string Render(string designId)
    {
        if (!Designs.TryGetValue(designId, out var design)) throw new ArgumentOutOfRangeException(nameof(designId), designId, "No card illustration is defined.");
        if (design.Scene == Scene.AncientStele) return AncientStele();
        var palette = design.Theme switch
        {
            Theme.Qi => new Palette("#0C2533", "#2C6866", "#235153", "#76D7BA", "#D7F6DA"),
            Theme.Step => new Palette("#142B47", "#3F8296", "#315B75", "#9BDFEA", "#E1F7F6"),
            Theme.Sword => new Palette("#202B40", "#526D76", "#2A485B", "#E5C17A", "#FFF0C1"),
            Theme.Blocked => new Palette("#301D31", "#71434C", "#472E40", "#DF8278", "#FFD4AD"),
            Theme.Foreign => new Palette("#242445", "#57618B", "#343A61", "#BA9CE8", "#EEE0FF"),
            _ => new Palette("#313A3A", "#85918A", "#4A686A", "#D3B785", "#FFF3D6")
        };
        bool rare = designId.StartsWith("DY_R", StringComparison.Ordinal);
        var body = new StringBuilder();
        body.Append($"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1000" height="760" viewBox="0 0 500 380">
            <defs>
            <linearGradient id="sky" x1="0%" y1="0%" x2="0%" y2="100%"><stop stop-color="{palette.Sky}"/><stop offset="1" stop-color="{palette.Horizon}"/></linearGradient>
            <linearGradient id="silk" x1="0%" y1="0%" x2="100%" y2="100%"><stop stop-color="#FFFAE8"/><stop offset=".55" stop-color="#DFE9DE"/><stop offset="1" stop-color="#709E9F"/></linearGradient>
            <linearGradient id="scroll" x1="0%" y1="0%" x2="100%" y2="100%"><stop stop-color="#FFF3CF"/><stop offset="1" stop-color="#C8B586"/></linearGradient>
            <radialGradient id="aura"><stop stop-color="{palette.Light}" stop-opacity=".52"/><stop offset=".5" stop-color="{palette.Accent}" stop-opacity=".13"/><stop offset="1" stop-color="{palette.Accent}" stop-opacity="0"/></radialGradient>
            </defs>
            <rect width="500" height="380" fill="url(#sky)"/>
            """);
        body.Append(Backdrop(design, palette));
        body.Append(design.Scene switch
        {
            Scene.Finger => Finger(design.Variant, palette),
            Scene.Ward => Ward(design.Variant, palette),
            Scene.Vortex => Vortex(design.Variant, palette),
            Scene.Footwork => Footwork(design.Variant, palette),
            Scene.Meridian => Meridian(design.Variant, palette),
            Scene.Palm => Palm(design.Variant, palette),
            Scene.Book => Book(design.Variant, palette),
            Scene.Rivers => Rivers(design.Variant, palette),
            Scene.Meditation => Meditation(design.Variant, palette),
            Scene.Temple => Temple(design.Variant, palette),
            Scene.Sword => Sword(design.Variant, palette),
            Scene.Lotus => Lotus(design.Variant, palette),
            Scene.Stars => Stars(design.Variant, palette),
            Scene.Aid => Aid(design.Variant, palette),
            Scene.Sleeve => Sleeve(palette),
            Scene.Shockwave => Shockwave(design.Variant, palette),
            Scene.Awakening => Awakening(palette),
            Scene.Renewal => Renewal(design.Variant, palette),
            Scene.Obstruction => Obstruction(palette),
            Scene.BorrowedQi => BorrowedQi(design.Variant, palette),
            Scene.Furnace => Furnace(palette),
            _ => throw new InvalidOperationException("Unknown card art scene.")
        });
        body.Append(Foreground(design, palette, rare));
        body.Append("</svg>");
        return body.ToString();
    }

    private static string Backdrop(Design design, Palette palette)
    {
        int moonX = 325 + design.Variant % 4 * 23;
        int moonY = 74 + design.Variant % 3 * 15;
        return $"""
            <circle cx="{moonX}" cy="{moonY}" r="53" fill="{Paper}" opacity=".09"/>
            <path d="M-20 249L38 179L63 196L119 101L151 149L177 132L245 244L307 152L336 182L396 97L422 141L458 131L524 244V380H-20Z" fill="{palette.Mountain}" opacity=".62"/>
            <path d="M53 221L119 101L103 172L112 163L93 218M344 194L396 97L381 166L394 157L369 216" fill="{Paper}" opacity=".08"/>
            <path d="M-15 274Q63 202 105 258L146 226L193 278L250 239L319 282L389 203L435 251L513 218V380H-15Z" fill="{Ink}" opacity=".34"/>
            <path d="M-10 267Q103 245 201 270T505 256L505 284Q369 307 213 285T-10 289Z" fill="{palette.Light}" opacity=".08"/>
            <path d="M-10 310Q133 279 256 308T514 298" fill="none" stroke="{palette.Light}" stroke-width="2" opacity=".2"/>
            <circle cx="265" cy="182" r="188" fill="url(#aura)"/>
            """;
    }

    private static string Foreground(Design design, Palette palette, bool rare)
    {
        var details = new StringBuilder();
        for (int index = 0; index < 32; index++)
        {
            int horizontal = 24 + (index * 73 + design.Variant * 19 + (int)design.Scene * 11) % 452;
            int vertical = 28 + (index * 47 + design.Variant * 13) % 310;
            details.Append($"<circle cx=\"{horizontal}\" cy=\"{vertical}\" r=\"{(index % 4 == 0 ? "1.5" : ".6")}\" fill=\"{palette.Light}\" opacity=\"{(index % 4 == 0 ? ".48" : ".18")}\"/>");
        }
        return $"""
            <path d="M0 345Q91 323 171 348Q286 366 365 340Q437 327 500 346V380H0Z" fill="{Ink}" opacity=".48"/>
            <path d="M-20 343Q103 322 219 349T525 336" fill="none" stroke="{palette.Accent}" stroke-width="1.4" opacity=".55"/>
            {details}
            <path d="M17 64V18H72M428 18H483V64M17 316V362H72M428 362H483V316" fill="none" stroke="{Gold}" stroke-width="1.5" opacity="{(rare ? ".9" : ".48")}"/>
            <path d="M24 47V25H47M453 25H476V47M24 333V355H47M453 355H476V333" fill="none" stroke="{Gold}" stroke-width=".8" opacity=".4"/>
            {(rare ? $"<path d=\"M239 18L250 12L261 18L250 24Z M239 362L250 356L261 362L250 368Z\" fill=\"{Gold}\"/><path d=\"M81 18H231M269 18H419M81 362H231M269 362H419\" stroke=\"{Gold}\" stroke-width=\".7\" opacity=\".6\"/>" : "")}
            """;
    }

    private static string Figure(string pose, double horizontal, double vertical, double scale, bool mirrored = false)
    {
        string arms = pose switch
        {
            "cast" => $"""
                <path d="M79 87Q49 101 20 130L40 158L94 120Z" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
                <path d="M119 88Q145 100 176 76L181 93Q151 124 125 124L108 108Z" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
                <path d="M174 76L191 67L205 66L205 70L192 74L207 73L207 77L190 81L181 93Z" fill="#E2BB91" stroke="{Ink}" stroke-width="1.4"/>
                <path d="M126 102L158 98M41 135L72 116" fill="none" stroke="#93B4AE" stroke-width="2"/>
                """,
            "step" => $"""
                <path d="M77 88Q37 110 9 98L18 136Q64 151 89 116M117 90Q151 69 163 40L183 55Q162 107 126 123" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
                <path d="M164 40L169 27L175 25L175 38L185 37L187 43L183 55" fill="#E2BB91" stroke="{Ink}" stroke-width="1.4"/>
                <path d="M20 123Q50 134 73 112M137 91L163 65" fill="none" stroke="#93B4AE" stroke-width="2"/>
                """,
            "read" => $"""
                <path d="M79 88Q49 111 58 146L97 139L91 115M119 89Q148 115 139 146L103 139L110 113" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
                <path d="M69 110L99 120L132 109L129 148L99 156L72 148Z" fill="url(#scroll)" stroke="{Ink}" stroke-width="2"/>
                <path d="M99 122V154M78 121L93 126M107 126L124 119M78 132L93 137M107 137L124 130" stroke="#5A6D63" stroke-width="1.5"/>
                """,
            _ => $"""
                <path d="M80 87Q43 112 45 144Q62 155 92 146L88 132L66 132L85 111M119 88Q155 110 156 145Q138 155 107 146L110 132L135 130L113 109" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
                <path d="M89 133Q101 125 113 133L118 141L109 145L92 145L82 140Z" fill="#E2BB91" stroke="{Ink}" stroke-width="1.4"/>
                """
        };
        string robe = pose == "step"
            ? "M79 85L119 85Q127 122 155 162L192 188Q144 212 116 190L97 161L77 202L20 204Q58 164 64 128Z"
            : pose == "sit"
                ? "M79 85L119 85Q132 118 143 157L174 181Q142 207 99 193Q54 206 24 181L58 154Q65 115 79 85Z"
                : "M79 85L119 85Q134 131 134 162L151 205Q106 223 48 206L64 159Q63 122 79 85Z";
        string shoes = pose == "step"
            ? "M113 185L138 201L157 195L164 202L143 214L127 209L103 190M49 191L32 216L11 222L8 215L22 206L29 185"
            : pose == "sit"
                ? "M45 176L27 186L32 194L76 195L92 184M108 184L126 195L169 194L174 186L155 176"
                : "M67 198L63 219L86 220L92 202M112 202L119 219L143 219L131 199";
        return $"""
            <g transform="translate({N(horizontal)} {N(vertical)}) scale({N(scale)}){(mirrored ? " translate(200 0) scale(-1 1)" : "")}">
            <path d="{shoes}" fill="{Ink}"/>
            <path d="{robe}" fill="url(#silk)" stroke="{Ink}" stroke-width="2.4" stroke-linejoin="round"/>
            <path d="M82 94L97 120L115 92L126 185L112 204L91 201L76 184Z" fill="#2A7275"/>
            <path d="M79 86L98 105L118 85L123 99L98 129L74 102Z" fill="{Paper}" stroke="#A4BCAD" stroke-width="1"/>
            <path d="M75 139Q100 148 127 138L130 148Q102 162 72 150Z" fill="{Ink}"/>
            <path d="M109 149Q131 166 166 157L155 171Q124 177 103 152" fill="#5FBBA7"/>
            <path d="M99 149V177L106 186L113 177V150" fill="{Gold}"/>
            <path d="M68 163L60 193M131 167L140 198M91 160L88 191M117 164L123 190" fill="none" stroke="#A1B8AA" stroke-width="1.5"/>
            {arms}
            <path d="M87 66L87 87L99 99L111 85L110 65Z" fill="#D6AE86" stroke="{Ink}" stroke-width="1.3"/>
            <path d="M79 39Q79 21 98 21Q118 21 119 40L115 65L104 78L93 77L82 65Z" fill="#E8C7A0" stroke="{Ink}" stroke-width="1.8"/>
            <path d="M79 48Q66 19 86 14Q90 1 104 5Q125 10 124 34L116 49L111 29Q99 40 84 35L83 52Z" fill="{Ink}"/>
            <path d="M91 15Q83 -3 96 -8Q114 -8 112 14Z" fill="{Ink}"/>
            <path d="M89 9L113 9" stroke="{Gold}" stroke-width="4"/>
            <path d="M91 6L73 -3M110 5L134 -1" stroke="#80C7BA" stroke-width="2"/>
            <path d="M85 50L93 48M105 48L114 49" stroke="{Ink}" stroke-width="1.8" stroke-linecap="round"/>
            <path d="M100 50L97 60L102 61M95 68Q101 71 107 67" fill="none" stroke="#9C765E" stroke-width="1.1"/>
            <path d="M79 33Q71 63 81 90M119 31Q130 63 119 91" fill="none" stroke="{Ink}" stroke-width="4"/>
            </g>
            """;
    }

    private static string Finger(int variant, Palette palette)
    {
        bool grand = variant >= 9;
        string trail = variant is 3 or 8
            ? $"<path d=\"M277 190Q351 102 471 94M277 190Q373 166 493 186M277 190Q375 223 464 276\" fill=\"none\" stroke=\"{palette.Accent}\" stroke-width=\"4\"/>"
            : $"<path d=\"M278 191L475 {(grand ? 64 : 137)}L329 200Z\" fill=\"{palette.Accent}\" opacity=\".55\"/><path d=\"M276 192L478 {(grand ? 61 : 137)}\" stroke=\"{palette.Light}\" stroke-width=\"{(grand ? 5 : 3)}\"/>";
        return $"""
            <path d="M-30 312Q77 233 182 242Q273 244 322 192Q224 286 136 283Q40 291-30 345Z" fill="{palette.Accent}" opacity=".24"/>
            <path d="M0 323Q131 235 253 218" fill="none" stroke="{Gold}" stroke-width="1.8" opacity=".75"/>
            {Figure("cast", 62, 110, 1.09)}
            <circle cx="284" cy="187" r="37" fill="url(#aura)"/>
            {trail}
            <path d="M290 177L287 196M279 185L299 191" stroke="{palette.Light}" stroke-width="2"/>
            {(variant == 6 ? MeridianDots(360, 235, palette) : "")}
            {(grand ? $"<path d=\"M355 18L372 75L359 117L394 129L378 174M439 23L420 53L438 92L413 131\" fill=\"none\" stroke=\"{Gold}\" stroke-width=\"2\" opacity=\".7\"/>" : "")}
            """;
    }

    private static string Ward(int variant, Palette palette)
    {
        var arcs = new StringBuilder();
        for (int index = 0; index < variant + 1; index++)
            arcs.Append($"<ellipse cx=\"250\" cy=\"193\" rx=\"{104 + index * 15}\" ry=\"{133 + index * 12}\" fill=\"none\" stroke=\"{(index == 0 ? palette.Light : palette.Accent)}\" stroke-width=\"{(index == 0 ? 3 : 1)}\" opacity=\"{(index == 0 ? ".9" : ".4")}\"/>");
        return $"""
            <ellipse cx="250" cy="193" rx="109" ry="139" fill="{palette.Accent}" opacity=".12"/>
            {arcs}
            <path d="M150 291Q250 322 350 291L324 325Q252 345 176 324Z" fill="{palette.Accent}" opacity=".55"/>
            {Figure("guard", 130, 92, 1.2)}
            <path d="M32 100L124 143L94 142M469 117L370 154L397 152M21 244L123 223L95 239" fill="none" stroke="{Gold}" stroke-width="3"/>
            <path d="M126 132L113 145L127 158M371 144L385 155L370 166M121 213L108 225L125 234" fill="none" stroke="{palette.Light}" stroke-width="2"/>
            """;
    }

    private static string Vortex(int variant, Palette palette)
    {
        var ribbons = new StringBuilder();
        for (int index = 0; index < 4 + variant; index++)
        {
            int rotation = index * 360 / (4 + variant);
            ribbons.Append($"<g transform=\"rotate({rotation} 280 183)\"><path d=\"M280 183C313 117 411 104 430 160C452 232 331 304 198 282C117 266 57 218 33 184C129 245 242 248 300 211C334 187 320 161 280 183Z\" fill=\"{palette.Accent}\" opacity=\".1\"/><path d=\"M280 183C337 127 402 149 382 204C358 269 175 289 63 221\" fill=\"none\" stroke=\"{(index % 2 == 0 ? Gold : palette.Accent)}\" stroke-width=\"{(index == 0 ? 3 : 1.5)}\" opacity=\".7\"/></g>");
        }
        return $"""
            {ribbons}
            <circle cx="280" cy="183" r="82" fill="url(#aura)"/>
            <path d="M276 128C229 149 239 217 280 234C326 216 330 157 287 140C317 179 294 207 277 206C253 192 258 164 276 128Z" fill="{palette.Light}" opacity=".83"/>
            {Figure(variant == 2 ? "cast" : "sit", 33, 187, .66)}
            <path d="M53 329Q102 310 158 329" fill="none" stroke="{Gold}" stroke-width="1.4"/>
            """;
    }

    private static string Footwork(int variant, Palette palette)
    {
        int angle = variant % 3 * 5 - 5;
        return $"""
            <path d="M-35 283Q99 331 202 228Q301 139 529 203L493 223Q294 170 217 252Q94 369-35 307Z" fill="{palette.Accent}" opacity=".34"/>
            <path d="M-18 286Q105 333 209 231Q329 142 521 210M14 310Q144 351 253 251Q354 180 514 233" fill="none" stroke="{palette.Light}" stroke-width="2" opacity=".82"/>
            <g opacity=".12" transform="translate(-92 29)">{Figure("step", 156, 99, 1.02)}</g>
            <g opacity=".19" transform="translate(-46 13)">{Figure("step", 156, 99, 1.02)}</g>
            <g transform="rotate({angle} 253 206)">{Figure("step", 156, 99, 1.02)}</g>
            <ellipse cx="197" cy="323" rx="62" ry="12" fill="none" stroke="{palette.Accent}" stroke-width="2"/>
            <ellipse cx="197" cy="323" rx="87" ry="18" fill="none" stroke="{palette.Accent}" stroke-width="1" opacity=".5"/>
            <path d="M83 235Q88 201 102 188Q112 215 83 235M388 283Q415 257 428 267Q422 288 388 283M371 83Q392 68 400 77Q397 94 371 83" fill="{palette.Light}" opacity=".72"/>
            {(variant is 2 or 10 or 12 ? Birds(palette.Light) : "")}
            {(variant is 5 or 11 ? $"<path d=\"M66 289L88 279L98 288L80 301Z M123 264L142 254L152 265L134 275Z\" fill=\"{Gold}\" opacity=\".7\"/>" : "")}
            """;
    }

    private static string Meridian(int variant, Palette palette)
    {
        string accent = variant == 3 ? "#E7A18E" : palette.Accent;
        var nodes = new StringBuilder();
        for (int index = 0; index < 6; index++)
        {
            int horizontal = index % 2 == 0 ? 225 - index * 8 : 275 + index * 8;
            int vertical = 132 + index * 24;
            nodes.Append($"<circle cx=\"{horizontal}\" cy=\"{vertical}\" r=\"6\" fill=\"{Gold}\"/><circle cx=\"{horizontal}\" cy=\"{vertical}\" r=\"12\" fill=\"none\" stroke=\"{accent}\" stroke-width=\"1.5\"/>");
        }
        return $"""
            <path d="M125 65L356 47L381 309L142 332Z" fill="{Ink}" fill-opacity=".55" stroke="{Gold}" stroke-width="1.2"/>
            <path d="M144 79L340 63L360 296L158 315Z" fill="none" stroke="{accent}" stroke-width=".8" opacity=".65"/>
            <path d="M233 90Q250 67 267 90L266 113L296 126L323 204L304 215L275 162L276 226L289 288L266 288L250 237L236 289L214 289L226 223L224 162L194 215L176 204L204 126L234 114Z" fill="{accent}" opacity=".18" stroke="{accent}" stroke-width="2"/>
            <path d="M250 94V239M250 132L225 132L209 180L193 228M250 132L283 156L303 204L315 252M225 132L250 180L209 228L234 280M283 156L250 204L266 280" fill="none" stroke="{accent}" stroke-width="2.5"/>
            {nodes}
            <circle cx="250" cy="204" r="18" fill="url(#aura)" stroke="{Gold}" stroke-width="1.5"/>
            <path d="M66 237L173 198L185 184L190 187L183 202L196 196L200 200L180 215L85 270Z" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
            <path d="M171 197L185 184L190 187L183 202L196 196L200 200L180 215Z" fill="#E8C7A0"/>
            {(variant == 4 ? $"<path d=\"M378 78Q435 157 397 252L384 231M397 252L418 239\" fill=\"none\" stroke=\"{Gold}\" stroke-width=\"2\"/>" : "")}
            """;
    }

    private static string Palm(int variant, Palette palette)
    {
        return $"""
            <g transform="translate(61 23) rotate(16 250 190)">
            <path d="M196 277L194 210L178 176Q174 164 184 161L213 184L212 114Q212 102 221 103L231 158L233 85Q235 74 243 82L248 155L259 91Q265 80 271 91L264 160L284 112Q293 105 296 116L279 183Q284 226 261 254L257 285Z" fill="url(#scroll)" stroke="{Ink}" stroke-width="3"/>
            <path d="M211 184Q236 172 254 186M211 208Q238 215 266 196M232 233Q250 226 252 208" fill="none" stroke="#9C8465" stroke-width="2"/>
            <path d="M192 263L264 257L287 331L162 331Z" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
            </g>
            <path d="M126 266C52 157 196 78 306 72C434 68 490 161 421 221" fill="none" stroke="{palette.Accent}" stroke-width="8" opacity=".45"/>
            <path d="M111 270C31 135 220 45 355 85C442 106 462 174 422 208" fill="none" stroke="{palette.Light}" stroke-width="2"/>
            {(variant == 3 ? Blossom(137, 139, 28, palette) : $"<path d=\"M366 108L411 81M390 153L453 140M397 188L447 207\" stroke=\"{Gold}\" stroke-width=\"2\"/>" )}
            """;
    }

    private static string Book(int variant, Palette palette)
    {
        var lines = new StringBuilder();
        for (int column = 0; column < 5; column++)
        {
            for (int row = 0; row < 5; row++)
            {
                int horizontal = 151 + column * 13;
                int vertical = 134 + row * 20 + column * 3;
                lines.Append($"<path d=\"M{horizontal} {vertical}h7m-6 4h5m-6 4h7\" stroke=\"#546C61\" stroke-width=\"1.4\" opacity=\".7\"/>");
            }
        }
        return $"""
            <path d="M93 111Q163 80 251 124Q340 78 415 109L397 294Q326 267 251 311Q169 269 106 292Z" fill="{Ink}" stroke="{Gold}" stroke-width="1.5"/>
            <path d="M111 101Q180 84 251 124L251 294Q179 254 119 274Z" fill="url(#scroll)" stroke="#BAA67D" stroke-width="2"/>
            <path d="M251 124Q326 85 398 102L386 274Q322 254 251 294Z" fill="url(#scroll)" stroke="#BAA67D" stroke-width="2"/>
            <path d="M125 113Q184 101 239 132M125 283Q184 271 244 306M258 306Q325 272 389 283" fill="none" stroke="#A59872" stroke-width="1"/>
            {lines}
            <g transform="translate(315 184)">
            <circle r="43" fill="none" stroke="#557A6C" stroke-width="1"/>
            <circle r="31" fill="none" stroke="#557A6C" stroke-width="2"/>
            {(variant == 2 ? SwordRays(0, 0, 6, 49, palette, true) : "<path d=\"M0-29C-40-4-18 31 0 30C41 4 17-29 0-29ZM0-29C18-10-18 12 0 30\" fill=\"#47766D\"/><circle cy=\"-13\" r=\"4\" fill=\"#F4EDDA\"/><circle cy=\"14\" r=\"4\" fill=\"#47766D\"/>")}
            </g>
            <path d="M251 138V293L263 315L274 303L268 132" fill="#B15D4E"/>
            <path d="M61 308L82 286L140 313L127 331Z" fill="{Ink}"/>
            <path d="M82 286L68 275L40 261L56 289L61 308Z" fill="{Paper}"/>
            {(variant == 3 ? Birds(palette.Light) : "")}
            """;
    }

    private static string Rivers(int variant, Palette palette)
    {
        return $"""
            <path d="M-15 158L79 81L127 176L179 114L227 220L283 131L317 158L368 72L419 160L474 114L525 205V380H-15Z" fill="{Ink}" opacity=".7"/>
            <path d="M77 133Q109 183 94 232Q101 281 254 307Q135 247 157 198L175 166Q209 221 214 248Q227 271 254 307Q269 265 305 226L326 182Q337 237 293 273L254 307Q334 266 382 215L393 141Q422 226 379 260Q351 292 254 307L352 380H152Z" fill="{palette.Accent}" opacity=".74"/>
            <path d="M85 159Q129 194 114 231Q133 280 254 307M177 185Q193 262 254 307M395 176Q416 247 254 307M324 206Q318 254 254 307M254 307L283 373" fill="none" stroke="{palette.Light}" stroke-width="2.5"/>
            <ellipse cx="254" cy="308" rx="83" ry="26" fill="url(#aura)" stroke="{Gold}" stroke-width="1.4"/>
            {Figure("sit", variant == 1 ? 38 : 325, 192, .62)}
            <path d="M15 334L81 304L140 331L143 350H13Z" fill="{Ink}"/>
            {Birds(palette.Light)}
            """;
    }

    private static string Meditation(int variant, Palette palette)
    {
        return $"""
            <circle cx="250" cy="169" r="104" fill="{palette.Accent}" opacity=".11"/>
            <circle cx="250" cy="169" r="115" fill="none" stroke="{Gold}" stroke-width="1.3" opacity=".55"/>
            <path d="M113 322Q250 268 390 322L368 344H131Z" fill="{Ink}"/>
            <ellipse cx="250" cy="314" rx="145" ry="24" fill="none" stroke="{palette.Accent}" stroke-width="1.8"/>
            {Figure("sit", 110, 73, 1.4)}
            <circle cx="250" cy="250" r="36" fill="url(#aura)"/>
            <circle cx="250" cy="250" r="12" fill="{palette.Light}" opacity=".85"/>
            <path d="M208 245Q250 221 291 245M215 257Q250 282 286 257" fill="none" stroke="{Gold}" stroke-width="2"/>
            {(variant == 2 ? $"<path d=\"M250 52V124M244 116L250 126L256 116\" fill=\"none\" stroke=\"{Gold}\" stroke-width=\"2\"/>" : "")}
            <path d="M65 306Q49 280 58 250Q69 227 61 208M430 301Q455 278 443 247Q431 225 444 203" fill="none" stroke="{palette.Light}" stroke-width="1.6" opacity=".65"/>
            """;
    }

    private static string Temple(int variant, Palette palette)
    {
        return $"""
            <path d="M60 145H439L383 123L342 77H160L117 122Z" fill="{Ink}"/>
            <path d="M42 145Q121 135 159 77M458 145Q380 132 342 77M160 77H342" fill="none" stroke="{Gold}" stroke-width="2"/>
            <path d="M97 148V334H120V148M382 148V334H406V148" fill="{Ink}"/>
            <path d="M127 156H376M120 165H382" stroke="{palette.Accent}" stroke-width="2" opacity=".6"/>
            <path d="M149 380L162 336H338L354 380M167 327L179 303H324L336 327M185 295L193 281H311L319 295" fill="{palette.Mountain}" stroke="{palette.Light}" stroke-width="1"/>
            {Figure(variant == 1 ? "step" : "sit", 153, 116, .97)}
            <path d="M9 229Q73 206 153 217M350 195Q409 173 503 192M-3 252Q91 229 146 246" fill="none" stroke="{palette.Light}" stroke-width="2" opacity=".5"/>
            {(variant == 1 ? Birds(palette.Light) : "<path d=\"M62 345Q51 310 68 278M68 295Q97 269 103 284Q96 305 68 307\" fill=\"none\" stroke=\"#A9CFA9\" stroke-width=\"3\"/>")}
            """;
    }

    private static string Sword(int variant, Palette palette)
    {
        bool token = variant >= 21;
        int style = token ? variant - 20 : variant;
        if (style <= 6)
        {
            string color = style switch { 1 => "#F1C67C", 2 => "#D6E99E", 3 => "#9FDDE9", 4 => "#9FD6B8", 5 => "#D5C4F0", _ => "#A3E1D6" };
            string energy = style switch
            {
                1 => $"<path d=\"M140 248L402 64L376 145L170 252Z\" fill=\"{color}\" opacity=\".5\"/><path d=\"M145 241L402 64L367 132Z\" fill=\"#FFF5D5\"/><path d=\"M121 220L169 266M117 245L153 275\" stroke=\"{color}\" stroke-width=\"4\"/>",
                2 => $"<path d=\"M140 235L428 93M150 247L437 104\" stroke=\"{color}\" stroke-width=\"3\"/><path d=\"M145 241L415 98L373 143Z\" fill=\"#F6FFE4\"/><circle cx=\"365\" cy=\"126\" r=\"29\" fill=\"none\" stroke=\"{color}\" stroke-width=\"1.5\"/><path d=\"M365 86V166M326 126H405\" stroke=\"{color}\" stroke-width=\"1\"/>",
                3 => $"<path d=\"M145 241L311 51L277 139ZM145 241L407 93L343 166ZM145 241L456 172L355 227Z\" fill=\"{color}\" opacity=\".62\"/><path d=\"M145 241L312 51M145 241L408 93M145 241L456 172\" stroke=\"#F1FFFF\" stroke-width=\"2.5\"/>",
                4 => $"<path d=\"M145 241L390 75L358 145Z\" fill=\"#EEFFDC\"/><path d=\"M301 95Q400 119 402 198Q361 259 279 278Q222 228 213 170Z\" fill=\"{color}\" opacity=\".13\" stroke=\"{color}\" stroke-width=\"4\"/><path d=\"M228 173L300 119L381 146L382 197L280 260Z\" fill=\"none\" stroke=\"{color}\" stroke-width=\"1\"/>",
                5 => $"<path d=\"M145 241C210 227 217 103 350 97L418 88C304 126 339 250 450 225C319 301 260 182 332 136C241 144 229 268 145 241Z\" fill=\"{color}\" opacity=\".6\"/><path d=\"M145 241C211 227 210 100 417 88\" fill=\"none\" stroke=\"#F7ECFF\" stroke-width=\"3\"/>",
                _ => $"<path d=\"M145 241C184 157 287 85 384 131C476 175 400 275 305 232C257 210 271 163 313 167C274 202 346 229 381 192C422 126 236 100 145 241Z\" fill=\"{color}\" opacity=\".61\"/><path d=\"M145 241C209 135 327 94 389 152C433 200 367 262 305 232\" fill=\"none\" stroke=\"#E7FFF1\" stroke-width=\"3\"/>"
            };
            return $"""
                <circle cx="279" cy="190" r="123" fill="none" stroke="{color}" stroke-width="1" opacity=".3"/>
                <circle cx="279" cy="190" r="142" fill="none" stroke="{Gold}" stroke-width=".6" opacity=".3"/>
                {energy}
                {(token ? SwordHand(color) : Figure("cast", 24, 198, .61))}
                <path d="M84 319Q146 328 200 308" fill="none" stroke="{Gold}" stroke-width="1.5"/>
                """;
        }
        int rays = style switch { 8 => 2, 13 => 3, 7 => 1, _ => 6 };
        return $"""
            <circle cx="250" cy="179" r="111" fill="{palette.Accent}" opacity=".09"/>
            <circle cx="250" cy="179" r="119" fill="none" stroke="{Gold}" stroke-width="1.5"/>
            <circle cx="250" cy="179" r="139" fill="none" stroke="{palette.Accent}" stroke-width=".8" opacity=".6"/>
            {SwordRays(250, 179, rays, 154, palette, false)}
            {Figure(style == 11 ? "sit" : "guard", 188, 218, .62)}
            <circle cx="250" cy="179" r="21" fill="{palette.Light}" opacity=".8"/>
            <path d="M245 159L251 178L269 184L250 190L245 207L239 189L223 184L240 178Z" fill="{Paper}"/>
            """;
    }

    private static string SwordHand(string color) => $"""
        <path d="M26 338L66 282L100 263L111 244L137 233L145 233L146 238L128 248L151 245L155 248L154 253L132 261L120 277L106 289L91 339Z" fill="#E8C7A0" stroke="{Ink}" stroke-width="2"/>
        <path d="M20 312L59 290L111 316L98 358H18Z" fill="url(#silk)" stroke="{Ink}" stroke-width="2"/>
        <path d="M38 312L88 337" stroke="#6A9C9F" stroke-width="4"/>
        <circle cx="145" cy="241" r="22" fill="{color}" opacity=".16"/>
        """;

    private static string SwordRays(int horizontal, int vertical, int count, int length, Palette palette, bool diagram)
    {
        var rays = new StringBuilder();
        for (int index = 0; index < count; index++)
        {
            int angle = count == 1 ? 35 : index * 360 / count + 15;
            rays.Append($"<g transform=\"translate({horizontal} {vertical}) rotate({angle})\"><path d=\"M-5-18L0-{length}L9-39L0-22Z\" fill=\"{(diagram ? "#527968" : palette.Light)}\"/><path d=\"M-11-33L11-33M0-20V-8\" stroke=\"{Gold}\" stroke-width=\"{(diagram ? 1 : 2)}\"/>{(diagram ? "" : $"<path d=\"M-15-25L-4-{length - 12}M17-40L7-{length - 27}\" stroke=\"{palette.Accent}\" stroke-width=\"1\" opacity=\".5\"/>")}</g>");
        }
        return rays.ToString();
    }

    private static string Lotus(int variant, Palette palette)
    {
        return $"""
            <ellipse cx="269" cy="294" rx="157" ry="29" fill="{Ink}" opacity=".4"/>
            <ellipse cx="269" cy="294" rx="148" ry="25" fill="none" stroke="{palette.Accent}" stroke-width="1.6"/>
            <ellipse cx="269" cy="302" rx="186" ry="37" fill="none" stroke="{palette.Accent}" stroke-width=".8" opacity=".5"/>
            <path d="M269 288Q202 301 150 254Q192 230 232 247Q184 187 203 160Q253 184 269 244Q279 185 331 160Q350 197 307 245Q353 226 390 253Q338 302 269 288Z" fill="#77AC96" stroke="{palette.Light}" stroke-width="1.2"/>
            {Blossom(269, 232, 85, palette)}
            {(variant == 1 ? "<path d=\"M37 246L113 223L156 200L164 199L169 204L151 219L171 214L176 219L152 236L126 245L53 279Z\" fill=\"url(#silk)\" stroke=\"#101F2D\" stroke-width=\"2\"/>" : Figure("sit", 69, 172, .65))}
            <path d="M259 131Q225 96 257 67Q290 98 259 131" fill="{palette.Light}" opacity=".77"/>
            <path d="M353 200Q363 162 389 166Q386 195 353 200M143 155Q108 155 108 125Q137 123 143 155" fill="{palette.Accent}" opacity=".65"/>
            """;
    }

    private static string Blossom(int horizontal, int vertical, int radius, Palette palette)
    {
        return $"""
            <g transform="translate({horizontal} {vertical}) scale({N(radius / 80d)})">
            <path d="M0 55Q-84 47-88-15Q-32-21 0 27Q32-21 88-15Q84 47 0 55Z" fill="#C7DCC9" stroke="{Ink}" stroke-width="1.4"/>
            <path d="M0 50Q-58 7-47-57Q-3-41 0 15Q3-41 47-57Q58 7 0 50Z" fill="{Paper}" stroke="#7A9A8A" stroke-width="1.4"/>
            <path d="M0 48Q-37-7 0-79Q37-7 0 48Z" fill="{palette.Light}" stroke="{Gold}" stroke-width="1.3"/>
            <path d="M0-51V39M-60 4L-14 39M60 4L14 39" fill="none" stroke="#89AC98" stroke-width="1"/>
            <ellipse cy="47" rx="18" ry="7" fill="{Gold}"/>
            </g>
            """;
    }

    private static string Stars(int variant, Palette palette)
    {
        var stars = new StringBuilder();
        (int Horizontal, int Vertical)[] points = [(85, 116), (145, 93), (205, 122), (251, 178), (301, 216), (353, 241), (420, 238)];
        foreach (var point in points)
        {
            stars.Append($"<g transform=\"translate({point.Horizontal} {point.Vertical})\"><circle r=\"16\" fill=\"{palette.Accent}\" opacity=\".15\"/><path d=\"M0-10L3-3L10 0L3 3L0 10L-3 3L-10 0L-3-3Z\" fill=\"{Gold}\"/></g>");
        }
        return $"""
            <path d="M85 116L145 93L205 122L251 178L301 216L353 241L420 238" fill="none" stroke="{Gold}" stroke-width="1.5" opacity=".8"/>
            {stars}
            <g transform="translate(254 273) rotate(-12)"><ellipse rx="146" ry="52" fill="{Ink}" opacity=".45"/><ellipse rx="146" ry="52" fill="none" stroke="{palette.Accent}" stroke-width="2"/><ellipse rx="120" ry="41" fill="none" stroke="{Gold}" stroke-width="1"/><path d="M0-36C-80-24-68 26 0 37C80 24 68-26 0-36ZM0-36C39-12-39 12 0 37" fill="{palette.Accent}" opacity=".5"/></g>
            {Figure("step", variant == 1 ? 155 : 113, 129, .9, variant == 2)}
            """;
    }

    private static string Aid(int variant, Palette palette)
    {
        bool sacrifice = variant == 3;
        return $"""
            <path d="M90 315Q220 302 385 320L417 350H59Z" fill="{Ink}"/>
            {Figure("guard", 264, 160, .78, true)}
            <ellipse cx="283" cy="212" rx="130" ry="137" fill="{palette.Accent}" opacity=".12" stroke="{palette.Accent}" stroke-width="3"/>
            <path d="M165 287Q280 353 396 273" fill="none" stroke="{palette.Light}" stroke-width="2"/>
            {Figure(sacrifice ? "cast" : "step", 71, 102, 1.07)}
            <path d="M287 173Q304 148 329 164Q346 181 324 198" fill="none" stroke="{Gold}" stroke-width="2"/>
            {(sacrifice ? "<path d=\"M32 133L152 187M15 218L149 219M84 95L166 159\" stroke=\"#DF9F86\" stroke-width=\"3\"/><path d=\"M145 175L159 187L145 200M141 210L154 219L143 230\" fill=\"none\" stroke=\"#FFE0BC\" stroke-width=\"2\"/>" : Birds(palette.Light))}
            """;
    }

    private static string Sleeve(Palette palette) => $"""
        <path d="M32 98Q111 83 178 162Q247 224 369 207L392 272Q288 328 199 294Q116 265 39 236Z" fill="url(#silk)" stroke="{Ink}" stroke-width="3"/>
        <path d="M54 119Q125 116 173 190Q247 271 367 233M49 156Q126 152 164 217Q212 290 319 281" fill="none" stroke="#8EB2AD" stroke-width="3"/>
        <path d="M338 210Q372 203 396 219L406 259Q383 275 356 279Z" fill="{Ink}"/>
        <path d="M356 228L381 220L397 209L416 204L421 208L406 220L427 216L430 221L412 233L391 247L365 251Z" fill="#E8C7A0" stroke="{Ink}" stroke-width="1.5"/>
        <path d="M404 213L470 140L445 197Z" fill="{palette.Light}"/>
        <path d="M397 208L464 129M418 223L477 162" stroke="{Gold}" stroke-width="1.5"/>
        <path d="M93 274Q215 367 389 291" fill="none" stroke="{palette.Accent}" stroke-width="2"/>
        """;

    private static string Shockwave(int variant, Palette palette)
    {
        var rings = new StringBuilder();
        for (int index = 0; index < variant + 2; index++)
        {
            int radius = 50 + index * 32;
            rings.Append($"<ellipse cx=\"313\" cy=\"185\" rx=\"{radius}\" ry=\"{radius + 19}\" fill=\"none\" stroke=\"{(index == 0 ? palette.Light : palette.Accent)}\" stroke-width=\"{(index == 0 ? 5 : 2)}\" opacity=\".65\"/>");
        }
        return $"""
            {rings}
            <path d="M263 161L335 107L309 175L402 187L317 204L339 267L268 211Z" fill="{palette.Light}" opacity=".84"/>
            {Figure("cast", 57, 119, 1.02)}
            <path d="M320 305L338 291L354 307L340 321ZM410 277L428 270L434 285L417 291ZM367 72L382 59L389 75L376 84Z" fill="{Ink}" stroke="{Gold}" stroke-width="1"/>
            <path d="M301 301L322 331M410 251L453 267M368 97L397 68" stroke="{Gold}" stroke-width="2"/>
            """;
    }

    private static string Awakening(Palette palette) => $"""
        <circle cx="310" cy="129" r="91" fill="{Paper}" opacity=".57"/>
        <path d="M311 38L291 96L319 128L281 172L293 220" fill="none" stroke="{Ink}" stroke-width="5"/>
        <path d="M290 95L239 78M317 128L374 101M281 172L229 188" fill="none" stroke="{Ink}" stroke-width="2"/>
        <path d="M35 318L110 284L238 306L258 335L91 347Z" fill="{Ink}"/>
        {Figure("read", 65, 116, .95)}
        <path d="M329 252Q342 216 366 230Q386 203 395 224Q396 245 371 255L381 271L362 268Q340 280 329 252Z" fill="{Gold}"/>
        <path d="M373 246L378 221M371 246L350 237" stroke="{Ink}" stroke-width="1.5"/>
        <path d="M281 294Q315 279 330 253" fill="none" stroke="{palette.Light}" stroke-width="1.4"/>
        """;

    private static string Renewal(int variant, Palette palette) => $"""
        <path d="M73 308Q119 284 184 310Q262 340 354 307L445 328V358H65Z" fill="{Ink}"/>
        <path d="M244 322Q259 247 246 197Q241 150 270 108" fill="none" stroke="{Gold}" stroke-width="6"/>
        <path d="M249 245Q166 238 161 176Q228 169 249 245Z" fill="#5DAE92" stroke="{palette.Light}" stroke-width="1.4"/>
        <path d="M251 204Q307 125 353 159Q331 220 251 204Z" fill="#92CBA5" stroke="{palette.Light}" stroke-width="1.4"/>
        <path d="M258 163Q229 113 270 81Q303 129 258 163Z" fill="{palette.Light}"/>
        <path d="M183 193L246 242M331 172L254 202" stroke="{Gold}" stroke-width="1.5"/>
        <path d="M154 275C83 219 124 105 221 83M335 100C418 157 397 267 299 296" fill="none" stroke="{palette.Accent}" stroke-width="3"/>
        <path d="M207 78L223 83L215 99M314 281L299 296L321 298" fill="none" stroke="{palette.Light}" stroke-width="2"/>
        <path d="M118 303L146 267L193 279L170 320Z" fill="url(#scroll)" stroke="{Gold}" stroke-width="1.5"/>
        {(variant == 1 ? "<path d=\"M337 289L363 273L389 303L370 322Z\" fill=\"#7B8D85\"/><path d=\"M350 264L358 253M383 282L395 269\" stroke=\"#C9D0AB\" stroke-width=\"2\"/>" : Birds(palette.Light))}
        """;

    private static string Obstruction(Palette palette) => $"""
        {Figure("sit", 140, 123, 1.08)}
        <ellipse cx="250" cy="229" rx="133" ry="96" fill="{Ink}" opacity=".28"/>
        <path d="M148 121C95 174 127 278 220 289M351 121C405 176 373 278 280 289" fill="none" stroke="{palette.Accent}" stroke-width="7" opacity=".78"/>
        <path d="M188 126L218 168L242 181M312 126L282 168L258 181M250 211V282" fill="none" stroke="{Gold}" stroke-width="4"/>
        <path d="M214 180L236 154L266 160L287 188L269 219L233 224L211 204Z" fill="#643847" stroke="{palette.Light}" stroke-width="2"/>
        <path d="M232 170L267 210M270 171L231 210" stroke="{palette.Accent}" stroke-width="7"/>
        <path d="M129 152L147 120L155 152M345 152L351 120L369 152" fill="none" stroke="{palette.Light}" stroke-width="3"/>
        <path d="M176 302L194 310M207 316L224 320M279 320L297 316M310 310L328 302" stroke="{Gold}" stroke-width="2" opacity=".7"/>
        """;

    private static string BorrowedQi(int variant, Palette palette) => $"""
        <circle cx="268" cy="190" r="110" fill="{Ink}" opacity=".25" stroke="{Gold}" stroke-width="1.3"/>
        <path d="M135 99C70 221 195 310 288 228C372 156 307 74 239 125" fill="none" stroke="#E7A483" stroke-width="17" opacity=".78"/>
        <path d="M406 286C470 160 343 76 250 156C164 231 231 310 298 257" fill="none" stroke="#8DDCC2" stroke-width="17" opacity=".82"/>
        <path d="M140 102C92 213 194 285 279 220M401 282C450 170 344 100 259 165" fill="none" stroke="{palette.Light}" stroke-width="3"/>
        <circle cx="269" cy="192" r="35" fill="{palette.Light}" stroke="{Gold}" stroke-width="3"/>
        <path d="M269 165L280 190L269 218L258 190Z" fill="{palette.Accent}"/>
        {(variant == 1 ? $"<path d=\"M38 267L109 246L164 288L91 315Z\" fill=\"url(#scroll)\" stroke=\"{Gold}\" stroke-width=\"2\"/><path d=\"M62 272L104 261M76 285L122 274M92 297L136 286\" stroke=\"{Ink}\" stroke-width=\"2\"/>" : $"<path d=\"M188 313L217 283L249 307L278 280L315 308L342 284\" fill=\"none\" stroke=\"{Gold}\" stroke-width=\"3\"/><circle cx=\"106\" cy=\"184\" r=\"8\" fill=\"#E7A483\"/><circle cx=\"426\" cy=\"205\" r=\"8\" fill=\"#8DDCC2\"/>")}
        """;

    private static string Furnace(Palette palette) => $"""
        <ellipse cx="255" cy="308" rx="128" ry="29" fill="{Ink}" opacity=".55"/>
        <path d="M169 189H339L317 298H191Z" fill="#426461" stroke="{Gold}" stroke-width="3"/>
        <path d="M182 205L155 181L140 206L177 241M326 205L353 181L368 206L332 241" fill="none" stroke="{Gold}" stroke-width="9"/>
        <ellipse cx="254" cy="189" rx="86" ry="23" fill="{Ink}" stroke="{Gold}" stroke-width="3"/>
        <path d="M202 184Q175 142 225 118Q214 83 258 60Q252 121 281 132Q313 94 315 127Q335 162 296 184Z" fill="{palette.Accent}" opacity=".9"/>
        <path d="M230 180Q218 148 254 126Q247 164 278 179Z" fill="{palette.Light}"/>
        <path d="M105 91L144 74L180 110L139 130ZM349 95L385 72L417 113L376 132Z" fill="url(#scroll)" stroke="{Gold}" stroke-width="2"/>
        <path d="M160 123L184 145M353 131L331 151M227 220L254 242L282 219M254 242V275" fill="none" stroke="{Gold}" stroke-width="3"/>
        <path d="M196 299L189 323M312 299L319 323" stroke="{Gold}" stroke-width="8"/>
        """;

    private static string AncientStele() => """
        <svg xmlns="http://www.w3.org/2000/svg" width="1000" height="760" viewBox="0 0 500 380">
          <defs>
            <linearGradient id="cave" x1="0" y1="0" x2="1" y2="1">
              <stop stop-color="#284050"/><stop offset=".6" stop-color="#192935"/><stop offset="1" stop-color="#121b29"/>
            </linearGradient>
            <linearGradient id="stone" x1=".1" y1=".05" x2=".8" y2=".92">
              <stop stop-color="#7d827a"/><stop offset=".42" stop-color="#4b655e"/><stop offset="1" stop-color="#25374a"/>
            </linearGradient>
            <linearGradient id="inscription" x1="0" y1="1" x2="0" y2="0">
              <stop stop-color="#c1ead1"/><stop offset=".34" stop-color="#78b299"/><stop offset=".72" stop-color="#99594d"/><stop offset="1" stop-color="#e4af7f"/>
            </linearGradient>
            <radialGradient id="light"><stop stop-color="#e8efd2" stop-opacity=".88"/><stop offset=".34" stop-color="#91d7b4" stop-opacity=".47"/><stop offset="1" stop-color="#5bae9e" stop-opacity="0"/></radialGradient>
          </defs>
          <rect width="500" height="380" fill="url(#cave)"/>
          <path d="M0 0H173L130 46L101 120L108 154L75 221L97 253L73 301L0 328Z" fill="#142533"/>
          <path d="M37 0H164L142 39L110 63L92 117L70 129L66 220L40 234L18 318L0 316V96Z" fill="#203644"/>
          <path d="M69 49L112 13L147 0L127 40L96 65L80 121L53 153L60 108Z" fill="#385061"/>
          <path d="M412 0H500V328L426 321L438 277L411 235L427 195L412 149L434 90L415 54Z" fill="#111e2c"/>
          <path d="M473 0H500V325L472 308L461 244L474 178L454 153L466 108L443 61Z" fill="#253442"/>
          <path d="M132 77L170 15L367 0L386 86L353 144L171 183Z" fill="#83918a" opacity=".09"/>
          <path d="M89 270L116 216L133 181L149 183L155 200L145 254L162 275Z" fill="#34515e"/>
          <path d="M361 235L374 196L392 173L403 177L412 221L394 257L426 271L344 279Z" fill="#29414f"/>
          <path d="M0 329L84 303L113 287L175 292L200 276L309 283L356 268L429 283L500 312V380H0Z" fill="#263745"/>
          <path d="M36 330L102 305L160 317L225 297L284 307L347 295L424 307L478 335L315 357L137 347Z" fill="#324752"/>
          <path d="M100 340L170 305L303 302L382 320L405 347L345 357L187 361Z" fill="#1a2637"/>
          <path d="M128 313L189 286L339 290L369 314L349 341L245 359L138 343Z" fill="#151e29" stroke="#111d2a" stroke-width="3" stroke-linejoin="round"/>
          <path d="M128 313L186 289L333 287L369 314L256 334L155 334Z" fill="#53655f"/>
          <path d="M133 318L199 297L316 298L338 313L254 330L158 330Z" fill="#647a6d"/>
          <path d="M153 335L256 334L369 314L349 341L256 352L139 343Z" fill="#2a3d47"/>
          <path d="M155 334L170 333L198 347L164 344M302 331L335 325L321 340L285 343" fill="#3e5656"/>
          <path d="M167 86L200 48L276 26L308 49L341 58L355 101L347 154L365 204L355 253L339 272L334 305L267 316L199 300L169 288L158 236L160 178L150 146Z" fill="#182532" stroke="#111d2a" stroke-width="4" stroke-linejoin="round"/>
          <path d="M167 86L200 48L276 26L307 47L316 82L304 141L324 200L320 246L298 268L300 302L267 316L196 302L169 288L158 236L160 178L150 146Z" fill="url(#stone)"/>
          <path d="M305 53L341 58L355 101L347 154L363 204L355 253L339 272L334 305L267 316L300 302L298 268L320 246L324 200L304 141L316 82Z" fill="#293e4b"/>
          <path d="M311 48L343 62L352 100L329 88L317 81M304 269L337 261L353 239L345 258L334 273L334 295L300 303Z" fill="#4c5d62"/>
          <path d="M168 84L201 47L274 28L294 40L246 43L223 64L191 68L175 101L163 146L158 132Z" fill="#aaa18b"/>
          <path d="M168 85L191 65L211 60L181 99L164 146L174 180L170 234L184 287L199 301L169 287L157 237L160 176L150 146Z" fill="#687573"/>
          <path d="M189 94L205 76L233 71L217 87L202 89L183 132Z" fill="#bba991" opacity=".45"/>
          <path d="M189 129L196 111L222 95L236 80L276 68L290 100L283 145L302 198L298 247L282 276L269 283L203 276L185 241L184 192L173 153Z" fill="#162631" opacity=".77"/>
          <path d="M190 134L205 109L239 93L268 76L282 91L280 145L295 196L294 237L276 266L202 271L192 241L191 192L181 155Z" fill="#334b4c"/>
          <path d="M190 134L205 109L239 93L268 76M192 241L202 271L275 266L294 237" stroke="#a8b396" stroke-width="1.7" opacity=".43" fill="none" stroke-linecap="round"/>
          <path d="M187 243L184 217L193 202L186 183L195 158M218 276L211 281L214 296L235 307M289 145L306 140L315 124L327 119M274 74L283 61L274 53L283 42" fill="none" stroke="#23343d" stroke-width="2.5" stroke-linejoin="round"/>
          <path d="M214 292L217 296L228 300M305 143L313 136L322 127M191 205L186 214" fill="none" stroke="#8a9d83" stroke-width="1.5" opacity=".55"/>
          <ellipse cx="235" cy="233" rx="91" ry="84" fill="url(#light)"/>
          <g fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path d="M221 219L208 206L205 174L194 164M224 214L220 187L211 167L214 144L208 136M230 211L228 184L221 166L229 145L225 124M237 212L242 176L239 146L245 126L244 108M242 215L258 182L251 162L262 134L259 115M246 222L271 198L267 179L277 153L275 125" stroke="#162f34" stroke-width="7"/>
            <path d="M221 219L208 206L205 174L194 164M224 214L220 187L211 167L214 144L208 136M230 211L228 184L221 166L229 145L225 124M237 212L242 176L239 146L245 126L244 108M242 215L258 182L251 162L262 134L259 115M246 222L271 198L267 179L277 153L275 125" stroke="url(#inscription)" stroke-width="2.6"/>
          </g>
          <g fill="#efc28d" opacity=".8">
            <path d="M194 160L198 164L194 168L190 164ZM208 132L212 136L208 140L204 136ZM225 120L229 124L225 128L221 124ZM244 104L248 108L244 112L240 108ZM259 111L263 115L259 119L255 115ZM275 121L279 125L275 129L271 125Z"/>
          </g>
          <path d="M229 207L248 215L251 234L237 251L218 247L210 229L214 217Z" fill="#a5d9b4" stroke="#c1dcc1" stroke-width="2"/>
          <path d="M230 213L244 221L245 234L235 244L221 241L217 230L221 220Z" fill="#315a57"/>
          <path d="M230 215L233 225L242 229L233 234L232 243L227 234L219 233L226 228Z" fill="#fbf2c7"/>
          <path d="M213 257L222 251L235 257L241 251L249 251L259 239" fill="none" stroke="#a0c8a5" stroke-width="2" opacity=".7"/>
          <path d="M152 303C184 294 199 282 215 268C184 288 163 288 140 292C121 294 105 303 96 314C111 308 140 305 152 303Z" fill="#a9dbbc" opacity=".36"/>
          <path d="M252 265C281 273 335 268 358 252C344 267 319 277 296 280C280 282 260 277 252 265Z" fill="#b3dac0" opacity=".32"/>
          <path d="M269 324L299 319L310 321L287 327ZM164 321L185 314L202 315L190 321Z" fill="#c0b88e" opacity=".52"/>
          <path d="M10 344L51 337L73 348L103 343L140 367L126 380H0ZM381 380L405 357L428 358L457 329L500 342V380Z" fill="#142230"/>
          <path d="M52 342L67 348L41 351L21 348ZM459 332L475 337L451 351L432 358Z" fill="#405663" opacity=".72"/>
          <path d="M35 289C87 281 106 287 127 282M375 299C421 286 461 297 510 281" fill="none" stroke="#91b7ac" stroke-width="5" opacity=".13"/>
          <path d="M183 62L187 57L174 62L179 66ZM320 234L329 224L333 230L328 239ZM147 210L153 201L156 205L153 211ZM370 236L377 228L379 233L375 238ZM129 281L138 276L139 280L131 284Z" fill="#a8b492" opacity=".65"/>
        </svg>
        """;

    private static string MeridianDots(int horizontal, int vertical, Palette palette) => $"""
        <g transform="translate({horizontal} {vertical})"><path d="M-35-42L-8-10L32-25L17 22L-12 40" fill="none" stroke="{palette.Accent}" stroke-width="1.5"/><g fill="{Gold}"><circle cx="-35" cy="-42" r="4"/><circle cx="-8" cy="-10" r="4"/><circle cx="32" cy="-25" r="4"/><circle cx="17" cy="22" r="4"/><circle cx="-12" cy="40" r="4"/></g></g>
        """;

    private static string Birds(string color) => $"""
        <path d="M54 89Q65 85 72 95Q75 83 87 80M93 71Q100 68 105 74Q108 66 115 64M399 76Q406 71 414 78Q417 70 427 70" fill="none" stroke="{color}" stroke-width="1.4" opacity=".7"/>
        """;

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
