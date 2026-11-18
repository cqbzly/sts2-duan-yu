using System.Globalization;

namespace DuanYu.Assets;

internal static class DuanYuArt
{
    private const string Ink = "#102D33";
    private const string Jade = "#4EBDA8";
    private const string Gold = "#E6C47B";
    private const string Paper = "#F2ECD7";

    public static string Character(bool seated = false)
    {
        string robe = seated
            ? "M84 129 Q52 155 47 207 L23 254 Q71 278 115 261 Q153 277 205 254 L167 210 Q172 159 144 131Z"
            : "M84 129 Q51 152 49 202 L22 248 L67 235 L58 301 L176 301 L152 233 L205 244 L167 188 Q171 152 144 131Z";
        return Wrap(240, 320, $"""
            <ellipse cx="119" cy="305" rx="79" ry="10" fill="{Ink}" opacity=".22"/>
            <path d="M89 274L84 309L111 309L118 275M133 274L139 309L165 309L153 273" fill="{Ink}"/>
            <path d="{robe}" fill="#215B65" stroke="{Ink}" stroke-width="5" stroke-linejoin="round"/>
            <path d="M94 132L119 171L137 132L153 282L78 282Z" fill="{Jade}"/>
            <path d="M93 130L117 160L139 130L147 146L119 187L82 146Z" fill="{Paper}"/>
            <path d="M93 172L86 271M141 171L148 271M73 223L65 271M157 224L170 274" fill="none" stroke="#8AD2BB" stroke-width="3" opacity=".65"/>
            <path d="M79 198Q115 213 157 196L159 212Q119 225 77 213Z" fill="{Ink}"/>
            <path d="M116 202L125 202L130 238L120 246L112 238Z" fill="{Gold}"/>
            <path d="M64 165L29 215L61 229L100 181" fill="#317E7E" stroke="{Ink}" stroke-width="4"/>
            <path d="M157 163L181 188L209 167L218 177L190 211L160 201" fill="#317E7E" stroke="{Ink}" stroke-width="4"/>
            <path d="M204 166L213 151L218 149L217 161L227 155L230 160L217 177Z" fill="#E9C6A4" stroke="{Ink}" stroke-width="2"/>
            <path d="M210 143L222 116M217 147L234 135" stroke="{Jade}" stroke-width="3" stroke-linecap="round"/>
            <path d="M91 89Q86 53 119 51Q154 53 147 92L144 117Q130 138 115 132Q95 127 91 106Z" fill="#E9C6A4" stroke="{Ink}" stroke-width="4"/>
            <path d="M89 92Q76 51 101 41Q112 22 139 40Q164 55 149 91L139 65Q108 77 93 68Z" fill="{Ink}"/>
            <path d="M105 40Q96 17 119 12Q143 15 135 41Z" fill="{Ink}"/>
            <path d="M103 33L138 32L138 42L104 44Z" fill="{Gold}"/>
            <path d="M106 29L85 22M132 30L157 18" stroke="{Jade}" stroke-width="4" stroke-linecap="round"/>
            <path d="M101 91L111 89M130 89L140 91" stroke="{Ink}" stroke-width="3" stroke-linecap="round"/>
            <path d="M119 95L116 108L122 109M111 119Q121 124 131 117" fill="none" stroke="#AA7258" stroke-width="2" stroke-linecap="round"/>
            <path d="M87 70Q74 106 82 143M147 67Q164 108 153 145" fill="none" stroke="{Ink}" stroke-width="7"/>
            """);
    }

    public static string Icon(bool locked = false, bool outline = false)
    {
        string color = locked ? "#59696A" : Jade;
        return Wrap(128, 128, $"""
            <circle cx="64" cy="64" r="59" fill="{(outline ? "#F2ECD7" : Ink)}" stroke="{Gold}" stroke-width="5"/>
            <path d="M23 110Q21 80 45 72L81 72Q108 82 105 110Z" fill="{color}"/>
            <path d="M47 75L64 99L81 75L70 73L64 85L57 73Z" fill="{Paper}"/>
            <path d="M44 42Q42 22 64 23Q87 24 85 43L82 67Q65 87 47 67Z" fill="{(locked ? "#A8A79D" : "#E9C6A4")}"/>
            <path d="M44 48Q35 24 52 21L53 12L73 12L78 23Q94 29 84 49L77 34Q61 45 47 35Z" fill="{Ink}"/>
            <path d="M52 18L76 18" stroke="{Gold}" stroke-width="5"/>
            <path d="M51 51L58 50M71 50L78 51" stroke="{Ink}" stroke-width="3"/>
            <path d="M58 66Q65 70 73 65" fill="none" stroke="#A97457" stroke-width="2"/>
            """);
    }

    public static string Energy(int size)
    {
        return Wrap(size, size, $"""
            <g transform="scale({Number(size / 128d)})">
            <circle cx="64" cy="64" r="57" fill="{Ink}" stroke="{Gold}" stroke-width="5"/>
            <circle cx="64" cy="64" r="43" fill="#245C62" stroke="{Jade}" stroke-width="3"/>
            <path d="M70 18C16 52 35 85 64 107C117 74 90 39 70 18Z" fill="{Jade}"/>
            <path d="M66 36C43 53 49 75 64 89C87 72 82 52 66 36Z" fill="{Paper}"/>
            </g>
            """);
    }

    public static string Card(string designId) => DuanYuCardArt.Render(designId);

    public static string Symbol(string key, int size = 128)
    {
        string normalizedKey = key.ToUpperInvariant().Replace("_OUTLINE", "").Replace("_BIG", "");
        int variant = normalizedKey switch
        {
            "CQBZLY_DY_RELIC01" => 0,
            "CQBZLY_DY_RELIC02" or "CQBZLY_DY_POWER_R12" => 1,
            "CQBZLY_DY_RELIC03" => 2,
            "CQBZLY_DY_RELIC04" => 3,
            "CQBZLY_DY_RELIC05" or "CQBZLY_DY_POWER_R08" => 6,
            "CQBZLY_DY_RELIC06" => 4,
            "CQBZLY_DY_RELIC07" => 7,
            "CQBZLY_DY_RELIC08" or "CQBZLY_DY_VERMILION_WARD" => 5,
            "CQBZLY_DY_QI" or "CQBZLY_DY_POWER_U01" or "CQBZLY_DY_POWER_R01" => 8,
            "CQBZLY_DY_LINGBO" or "CQBZLY_DY_POWER_R02" or "CQBZLY_DY_POWER_U23" => 9,
            "CQBZLY_DY_QI_BLADE" or "CQBZLY_DY_POWER_U14" or "CQBZLY_DY_POWER_R07" => 10,
            "CQBZLY_DY_POWER_U05" or "CQBZLY_DY_POWER_R18" => 11,
            _ => 12
        };
        string shape = variant switch
        {
            0 => "<path d=\"M42 29Q64 13 86 29L95 73L64 108L33 73Z\" fill=\"#4EBDA8\" stroke=\"#E6C47B\" stroke-width=\"4\"/><circle cx=\"64\" cy=\"42\" r=\"8\" fill=\"#102D33\"/>",
            1 => "<path d=\"M24 34L97 24L105 90L32 102Z\" fill=\"#F2ECD7\" stroke=\"#E6C47B\" stroke-width=\"4\"/><path d=\"M43 42L85 36M44 58L87 52M45 74L88 68\" stroke=\"#245C62\" stroke-width=\"5\"/>",
            2 => "<path d=\"M33 77Q62 35 91 49L100 86L75 102L21 98Z\" fill=\"#4EBDA8\" stroke=\"#E6C47B\" stroke-width=\"4\"/><path d=\"M21 98L100 86\" stroke=\"#F2ECD7\" stroke-width=\"7\"/>",
            3 => "<circle cx=\"64\" cy=\"60\" r=\"33\" fill=\"none\" stroke=\"#A67B48\" stroke-width=\"9\"/><g fill=\"#E6C47B\" stroke=\"#76522C\" stroke-width=\"2\"><circle cx=\"64\" cy=\"27\" r=\"9\"/><circle cx=\"87\" cy=\"37\" r=\"9\"/><circle cx=\"97\" cy=\"60\" r=\"9\"/><circle cx=\"87\" cy=\"83\" r=\"9\"/><circle cx=\"64\" cy=\"93\" r=\"9\"/><circle cx=\"41\" cy=\"83\" r=\"9\"/><circle cx=\"31\" cy=\"60\" r=\"9\"/><circle cx=\"41\" cy=\"37\" r=\"9\"/></g><path d=\"M64 100V116M58 112H70\" stroke=\"#4EBDA8\" stroke-width=\"4\"/>",
            4 => "<path d=\"M24 43L46 24L64 39L82 24L104 43L93 91L64 106L35 91Z\" fill=\"#E6C47B\"/><path d=\"M42 58H86M46 73H82\" stroke=\"#102D33\" stroke-width=\"7\"/>",
            5 => "<ellipse cx=\"64\" cy=\"76\" rx=\"35\" ry=\"24\" fill=\"#B95B53\"/><circle cx=\"43\" cy=\"52\" r=\"14\" fill=\"#E6C47B\"/><circle cx=\"85\" cy=\"52\" r=\"14\" fill=\"#E6C47B\"/><circle cx=\"43\" cy=\"52\" r=\"5\" fill=\"#102D33\"/><circle cx=\"85\" cy=\"52\" r=\"5\" fill=\"#102D33\"/>",
            6 => "<path d=\"M24 25H104V103H24Z\" fill=\"#F2ECD7\" stroke=\"#E6C47B\" stroke-width=\"4\"/><circle cx=\"64\" cy=\"65\" r=\"25\" fill=\"#245C62\"/><path d=\"M64 65V31M64 65L94 48M64 65L94 83M64 65V99M64 65L34 83M64 65L34 48\" stroke=\"#4EBDA8\" stroke-width=\"5\"/><circle cx=\"64\" cy=\"65\" r=\"7\" fill=\"#E6C47B\"/>",
            7 => "<circle cx=\"64\" cy=\"64\" r=\"35\" fill=\"none\" stroke=\"#B89A64\" stroke-width=\"11\"/><path d=\"M64 99V30M64 61L43 42M64 76L84 56\" stroke=\"#F2ECD7\" stroke-width=\"5\"/><path d=\"M66 56Q69 27 90 29Q94 51 66 56Z\" fill=\"#4EBDA8\"/><path d=\"M61 76Q38 80 36 60\" fill=\"none\" stroke=\"#9D7651\" stroke-width=\"6\"/>",
            8 => "<path d=\"M69 20C20 38 21 91 61 106C103 105 112 59 81 42C61 30 38 46 44 68C48 83 72 87 80 68\" fill=\"none\" stroke=\"#4EBDA8\" stroke-width=\"12\" stroke-linecap=\"round\"/><circle cx=\"64\" cy=\"63\" r=\"7\" fill=\"#F2ECD7\"/>",
            9 => "<path d=\"M21 42H74Q101 42 92 25M17 63H91Q116 63 108 45M25 84H69Q97 84 88 101\" fill=\"none\" stroke=\"#8FE6E2\" stroke-width=\"9\" stroke-linecap=\"round\"/><path d=\"M31 109Q47 96 66 109\" fill=\"none\" stroke=\"#4EBDA8\" stroke-width=\"5\"/>",
            10 => "<path d=\"M30 97L86 25L102 20L100 38L44 104Z\" fill=\"#8FE6C7\" stroke=\"#E6C47B\" stroke-width=\"3\"/><path d=\"M24 83L48 105M22 110L36 96\" stroke=\"#E6C47B\" stroke-width=\"8\"/><path d=\"M59 34L66 23M91 70L105 62\" stroke=\"#F2ECD7\" stroke-width=\"4\"/>",
            11 => "<path d=\"M64 21L102 37L94 80L64 108L34 80L26 37Z\" fill=\"#245C62\" stroke=\"#E6C47B\" stroke-width=\"6\"/><path d=\"M64 39V89M43 61H85\" stroke=\"#4EBDA8\" stroke-width=\"8\"/>",
            _ => "<path d=\"M64 95Q24 88 24 54Q51 51 64 80Q77 51 104 54Q104 88 64 95Z\" fill=\"#4EBDA8\"/><path d=\"M64 89Q37 63 64 26Q91 63 64 89Z\" fill=\"#F2ECD7\" stroke=\"#E6C47B\" stroke-width=\"3\"/>"
        };
        return Wrap(size, size, $"<g transform=\"scale({Number(size / 128d)})\"><circle cx=\"64\" cy=\"64\" r=\"59\" fill=\"{Ink}\" stroke=\"{Jade}\" stroke-width=\"3\"/>{shape}</g>");
    }

    public static string Event(string key)
    {
        bool chamber = !key.Contains("frog", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("toad", StringComparison.OrdinalIgnoreCase)
            && !key.EndsWith("EVENT02", StringComparison.OrdinalIgnoreCase);
        string scenery = chamber ? $"""
            <path d="M0 0H500V380H0Z" fill="#152F36"/>
            <path d="M55 380V111Q250 -62 445 111V380H381V141Q250 24 119 141V380Z" fill="#467274"/>
            <path d="M118 380V145Q250 30 382 145V380Z" fill="#0C222A"/>
            <path d="M0 331L210 267H290L500 331V380H0Z" fill="#648B87"/>
            <path d="M168 293L211 189L318 208L288 312Z" fill="{Paper}" stroke="{Gold}" stroke-width="6"/>
            <path d="M220 211L295 225M209 236L288 250M200 262L277 276" stroke="#245C62" stroke-width="7"/>
            """ : $"""
            <rect width="500" height="380" fill="#153D37"/>
            <path d="M15 0L75 380M110 0L159 380M431 0L390 380M499 0L445 380" stroke="#265B4F" stroke-width="34"/>
            <ellipse cx="250" cy="322" rx="180" ry="37" fill="#4D8B69"/>
            <ellipse cx="251" cy="246" rx="91" ry="65" fill="#B95548"/>
            <ellipse cx="192" cy="193" rx="37" ry="32" fill="{Gold}"/>
            <ellipse cx="307" cy="193" rx="37" ry="32" fill="{Gold}"/>
            <circle cx="198" cy="194" r="13" fill="{Ink}"/>
            <circle cx="302" cy="194" r="13" fill="{Ink}"/>
            <path d="M204 261Q251 284 300 261" fill="none" stroke="{Ink}" stroke-width="7"/>
            <path d="M139 302Q104 261 135 226M352 302Q393 257 363 230" fill="none" stroke="{Jade}" stroke-width="12"/>
            """;
        return Wrap(500, 380, scenery);
    }

    public static string SelectBackground()
    {
        return Wrap(1600, 900, $"""
            <defs><linearGradient id="sky" x2="0" y2="1"><stop stop-color="#0C252D"/><stop offset="1" stop-color="#407F79"/></linearGradient></defs>
            <rect width="1600" height="900" fill="url(#sky)"/>
            <circle cx="1010" cy="260" r="185" fill="{Paper}" opacity=".15"/>
            <path d="M0 650L220 300L414 530L660 190L840 450L1080 220L1330 480L1600 270V900H0Z" fill="#234F55"/>
            <path d="M0 850L260 570L500 735L820 480L1090 720L1320 540L1600 750V900H0Z" fill="#102F38"/>
            <path d="M0 786Q403 691 790 797T1600 757M0 847Q497 765 914 847T1600 828" fill="none" stroke="{Jade}" stroke-width="4" opacity=".35"/>
            <g fill="none" stroke="{Gold}" stroke-width="3" opacity=".5"><circle cx="1050" cy="434" r="237"/><circle cx="1050" cy="434" r="218"/></g>
            """);
    }

    private static string Wrap(int width, int height, string body) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{body}</svg>";

    private static string Number(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);

}
