namespace DuanYu.Assets;

internal static class DuanYuSupportPortraits
{
    internal static string? Render(string designId) => designId switch
    {
        "DY_U29" => HelpingHand(),
        "DY_U30" => FellowTravelers(),
        "DY_R17" => TakeTheBlow(),
        "DY_R11" => StandInTheWind(),
        "DY_R15" => GuardTheEmber(),
        _ => null
    };

    private static string Picture(string top, string bottom, string details, string painting) => $$"""
        <svg xmlns="http://www.w3.org/2000/svg" width="1000" height="760" viewBox="0 0 500 380">
        <defs>
          <linearGradient id="sky" x1="0%" y1="0%" x2="20%" y2="100%">
            <stop stop-color="{{top}}"/><stop offset="1" stop-color="{{bottom}}"/>
          </linearGradient>
          {{details}}
        </defs>
        <rect width="500" height="380" fill="url(#sky)"/>
        {{painting}}
        </svg>
        """;

    private static string HelpingHand() => Picture("#102d3a", "#526f69", """
        <linearGradient id="silk" x1="11%" y1="0%" x2="91%" y2="100%">
          <stop stop-color="#e3cc93"/><stop offset=".6" stop-color="#8b9c86"/><stop offset="1" stop-color="#456261"/>
        </linearGradient>
        """, """
        <path d="M0 310L46 207L72 214L98 122L136 132L147 37L244 0H0ZM500 107L429 94L400 123L384 173L402 196L408 280L363 332L377 380H500Z" fill="#173039"/>
        <path d="M0 292L52 235L81 242L109 152L148 158L163 73L212 40L238 7M501 118L439 110L410 133L398 177L412 210L431 275L406 310L380 336" fill="none" stroke="#55776f" stroke-width="5"/>
        <path d="M43 277Q116 253 175 282T410 254M22 318Q121 286 171 315T363 308M7 350Q91 332 148 344M315 360Q382 340 497 327" fill="none" stroke="#98b2a3" stroke-width="4" opacity=".37"/>
        <path d="M-5 9L208 79L242 127L205 177L153 185L122 152L-5 152Z" fill="#283e42" stroke="#213539" stroke-width="5"/>
        <path d="M-5 5L205 75L235 123L204 163L155 175L130 149L-8 137Z" fill="url(#silk)"/>
        <path d="M-4 33L89 59L143 114L216 103M-6 93L76 102L134 140L225 124M169 161L196 136L228 121" fill="none" stroke="#627f77" stroke-width="6"/>
        <path d="M206 127L237 121L254 132L281 158L278 179L259 194L231 191L214 168L194 154Z" fill="#cfa37b" stroke="#263940" stroke-width="4"/>
        <path d="M205 138L238 128L269 158L265 171L251 175L232 156L213 154M223 181L245 183L259 178" fill="none" stroke="#edc394" stroke-width="4"/>
        <path d="M506 372L347 341L274 268L250 213L279 179L318 176L363 225L505 260Z" fill="#202e38" stroke="#243a3f" stroke-width="5"/>
        <path d="M356 232L316 187L280 193L260 221L282 273L349 332L506 364V286Z" fill="#5d6860"/>
        <path d="M267 231L288 218L309 200M281 257L310 244L337 225M314 289L356 269L387 257M352 312L390 295L456 298" fill="none" stroke="#a2a08a" stroke-width="5"/>
        <path d="M265 218L247 198L240 183L245 169L262 159L285 165L301 185L289 210Z" fill="#b98460" stroke="#283b3d" stroke-width="4"/>
        <path d="M243 183Q260 181 273 168L284 172L280 180L263 197M262 205Q282 206 290 187" fill="none" stroke="#eac298" stroke-width="4"/>
        <path d="M141 213C188 158 237 210 293 156Q337 112 387 125C330 137 321 167 296 185C250 223 188 196 145 232Z" fill="#9bd1ae" opacity=".63"/>
        <path d="M126 230C176 173 243 221 296 165Q340 123 401 123M313 169Q356 134 413 134M95 240L128 228" fill="none" stroke="#d1e4b9" stroke-width="3"/>
        """);

    private static string FellowTravelers() => Picture("#647983", "#deb384", """
        <radialGradient id="evening" cx="65%" cy="45%" r="68%">
          <stop stop-color="#ffe0a3"/><stop offset=".47" stop-color="#c49777"/><stop offset="1" stop-color="#756d72" stop-opacity="0"/>
        </radialGradient>
        """, """
        <rect width="500" height="380" fill="url(#evening)"/>
        <path d="M0 152Q66 128 138 159Q180 179 248 153Q326 115 380 148Q432 166 503 142V322H0Z" fill="#a88573"/>
        <path d="M0 184Q92 165 156 197Q242 236 307 187Q356 153 432 177L500 172V337H0Z" fill="#7f766c"/>
        <path d="M0 245Q75 215 134 243L229 257L370 239L502 192V380H0Z" fill="#566966"/>
        <path d="M-8 377Q67 296 188 279Q307 266 376 225Q411 205 455 217Q407 225 383 246Q322 299 193 326Q144 339 117 383Z" fill="#c8a27b"/>
        <path d="M21 379Q109 319 207 310Q320 293 379 242Q414 212 454 216" fill="none" stroke="#ecd1a0" stroke-width="4"/>
        <path d="M0 216L40 193L82 214V256H0ZM0 214L42 184L90 216L76 221L41 200L8 224ZM89 219L120 202L152 219V263H89ZM85 219L119 194L157 220L147 224L118 209L96 226Z" fill="#394b4e"/>
        <path d="M36 229H50V244H36ZM114 227H125V240H114Z" fill="#e7bb76"/>
        <path d="M149 295L169 280L195 276L202 291L186 302L151 308L138 306Z" fill="#766657"/>
        <path d="M162 284L161 263L164 250L178 244L191 256L192 275L202 292L193 299L178 281L172 297L157 304L149 297Z" fill="#283d48"/>
        <path d="M164 255L156 235L163 214L177 208L192 218L197 252L192 267L175 268L157 257Z" fill="#dbccb2" stroke="#495c59" stroke-width="3"/>
        <path d="M169 216Q159 209 161 198Q168 188 180 193L188 200L185 211L178 219Z" fill="#3b4247"/>
        <path d="M161 205Q153 209 144 206L139 212L149 217L162 214M159 235L170 243L191 240" fill="none" stroke="#617c71" stroke-width="5"/>
        <path d="M235 266L230 246L238 225L254 222L260 242L254 263L269 279L261 286L248 280L242 261L234 276L218 278L216 270Z" fill="#422f38"/>
        <path d="M230 235L222 215L226 200L244 189L258 199L266 225L262 246L232 246Z" fill="#96664f" stroke="#5f4d47" stroke-width="3"/>
        <path d="M227 198L219 196L224 186L236 175L250 183L263 192L248 195L237 191Z" fill="#c5a879"/>
        <path d="M225 210L242 217L260 209L262 217L244 225L225 219Z" fill="#d7a473"/>
        <path d="M185 226L198 218L219 224L212 236L190 240Z" fill="#ecd4a1" stroke="#735e51" stroke-width="3"/>
        <path d="M193 227L208 225M194 232L208 231M266 305L295 293M294 322L315 307M222 343L267 337" fill="none" stroke="#f4d8a4" stroke-width="2"/>
        <path d="M0 379Q19 329 53 330L39 347L69 347L47 361L87 364L75 380M390 378Q414 328 447 331L435 351L464 339L477 356L511 350V381" fill="#303f43"/>
        """);

    private static string TakeTheBlow() => Picture("#2b1d31", "#6e3b39", """
        <radialGradient id="flare" cx="38%" cy="40%" r="68%">
          <stop stop-color="#dba56d"/><stop offset=".48" stop-color="#984b3e"/><stop offset="1" stop-color="#4b2632" stop-opacity="0"/>
        </radialGradient>
        """, """
        <rect width="500" height="380" fill="url(#flare)"/>
        <path d="M0 0H123L166 145L144 170L122 138L96 147L75 117L25 123L0 107ZM500 0H365L355 36L419 103L432 175L484 210L500 191ZM0 301L81 290L136 289L153 310L245 310L348 335L500 298V380H0Z" fill="#2e2730"/>
        <path d="M0 117L19 136L76 128L99 158L122 153L146 182M8 318L85 305L142 312L165 329M365 10L381 52L420 84L464 119" fill="none" stroke="#bc7a62" stroke-width="4"/>
        <path d="M149 249L132 209L145 180L155 191L162 218L185 227L196 206L209 212L210 258L188 278L151 274Z" fill="#35323a"/>
        <path d="M149 183L144 167Q149 154 163 157L176 171L173 183L163 193Z" fill="#b38165"/>
        <path d="M149 181L141 175L141 160Q150 148 164 153L172 162L160 161L156 178Z" fill="#312a33"/>
        <path d="M237 239L262 223L293 244L315 299L330 320L318 330L293 321L271 278L258 280L235 318L206 330L188 325L196 312L217 297Z" fill="#292e39"/>
        <path d="M233 150L265 135L292 143L307 166L316 200L346 220L347 242L323 242L300 230L292 258L270 269L255 254L231 260L203 249L192 226L205 191Z" fill="#b8b59d" stroke="#4c4849" stroke-width="4"/>
        <path d="M206 216L233 203L258 180L281 162L295 168L279 187L258 211L240 237L216 248L201 234Z" fill="#e6c995"/>
        <path d="M233 244L255 221L275 211L293 228L280 245L264 248L261 259L245 244ZM291 171L308 208L336 226L333 235L303 225L280 190" fill="#707d77"/>
        <path d="M246 150Q232 142 232 129Q235 114 254 112L267 119L273 131L265 144L256 153Z" fill="#d0a277"/>
        <path d="M237 139L229 134L225 120L238 111L252 108L265 118L250 121L243 132L246 145L232 149L215 139Z" fill="#302d35"/>
        <path d="M338 79Q393 115 392 166Q396 224 347 266L326 255Q381 208 370 161Q365 122 323 102Z" fill="#90af9c"/>
        <path d="M339 82Q386 123 380 170Q389 227 335 258M344 111Q374 147 369 178Q374 218 348 235" fill="none" stroke="#f6dda4" stroke-width="5"/>
        <path d="M506 91L405 150L389 157L404 136L500 68ZM493 213L404 183L390 174L413 178L502 195Z" fill="#c87b62"/>
        <path d="M405 149L382 165L397 146M401 179L381 166L407 170M384 156L382 140M385 176L398 194M367 148L357 140M417 141L433 128" fill="none" stroke="#fed79a" stroke-width="4" stroke-linecap="round"/>
        """);

    private static string StandInTheWind() => Picture("#263748", "#9cae9c", """
        <linearGradient id="haze" x1="40%" y1="0%" x2="63%" y2="100%">
          <stop stop-color="#95aba3" stop-opacity=".45"/><stop offset="1" stop-color="#ddd2ac"/>
        </linearGradient>
        """, """
        <path d="M-10 127Q57 100 136 129T286 113Q376 87 511 104V380H-10Z" fill="url(#haze)"/>
        <path d="M-11 194Q95 135 176 178T353 155L508 130M-10 241Q94 182 167 223T352 202Q407 169 511 160M75 291Q140 249 233 266M313 279Q417 213 508 228" fill="none" stroke="#e0d5b4" stroke-width="5" opacity=".58"/>
        <path d="M0 361L23 311L63 295L121 262L157 241L220 246L249 263L219 301L213 359L238 382H0Z" fill="#354c53"/>
        <path d="M18 324L78 303L123 273L165 253L214 255L235 265L209 276L156 268L118 290L70 315Z" fill="#89a292"/>
        <path d="M19 357L63 326L90 338L116 309L151 298L166 280L193 281M57 363L101 346M164 312L178 291L210 282" fill="none" stroke="#62807a" stroke-width="4"/>
        <path d="M194 236L216 192L228 187L232 211L220 245L225 255L217 261L201 257L203 244L196 248L182 255L174 251Z" fill="#273945"/>
        <path d="M226 148Q248 141 253 157L245 179L277 197L327 198L306 209L271 211L242 202L229 222L229 242L204 246L195 233L202 208L191 187L203 172L211 152Z" fill="#e6d3aa" stroke="#536967" stroke-width="4"/>
        <path d="M223 157L235 165L228 187L213 212L202 231M235 188L250 188L279 205L301 205" fill="none" stroke="#8e9b82" stroke-width="5"/>
        <path d="M225 150Q214 143 216 132Q221 116 235 118L249 129L245 139L238 150L232 156Z" fill="#bb956f"/>
        <path d="M218 141L208 133L211 121L226 113L242 119L247 125L234 124L226 130L226 143L218 151L193 154L178 164L183 152L207 143Z" fill="#304550"/>
        <path d="M209 187C263 164 289 129 356 127Q405 127 464 90Q434 138 369 148C307 146 270 193 217 200Z" fill="#c07560" stroke="#6e6f61" stroke-width="3"/>
        <path d="M243 183Q301 149 338 141Q399 144 444 109M418 178L470 160M68 229L116 213M265 252L318 226" fill="none" stroke="#edd5a6" stroke-width="3" opacity=".77"/>
        """);

    private static string GuardTheEmber() => Picture("#221c24", "#503b34", """
        <radialGradient id="ember" cx="42%" cy="70%" r="57%">
          <stop stop-color="#eaaaa0"/><stop offset=".34" stop-color="#a55e3b"/><stop offset="1" stop-color="#62352c" stop-opacity="0"/>
        </radialGradient>
        <linearGradient id="steel" x1="9%" y1="94%" x2="69%" y2="0%">
          <stop stop-color="#f2c88c"/><stop offset=".53" stop-color="#a7a09a"/><stop offset="1" stop-color="#697c84"/>
        </linearGradient>
        """, """
        <rect width="500" height="380" fill="url(#ember)"/>
        <path d="M0 304V73L44 56L65 5L143 0L158 51L197 65L192 322ZM500 0H408L393 42L423 83L413 296L500 314Z" fill="#292730"/>
        <path d="M16 298L16 89L53 73L76 22L128 16L149 64L179 80V308M498 111L444 86L430 304" fill="none" stroke="#90634f" stroke-width="7" opacity=".53"/>
        <path d="M0 345L46 314L124 308L167 317L213 312L316 329L361 307L500 313V380H0Z" fill="#382a2f"/>
        <path d="M9 373L97 337L142 342L185 329M325 364L394 341L477 350" fill="none" stroke="#855b4b" stroke-width="5"/>
        <path d="M60 275Q139 243 210 266L221 323Q143 348 64 321Z" fill="#281d23" stroke="#68453a" stroke-width="4"/>
        <ellipse cx="141" cy="276" rx="78" ry="27" fill="#a26041"/>
        <path d="M71 282L96 262L123 271L143 254L170 267L190 263L208 280L167 296L119 291Z" fill="#6c3c33"/>
        <path d="M103 285L128 274L141 282L159 270L189 282L158 292L128 287L115 292Z" fill="#e79858"/>
        <path d="M132 273Q123 261 136 248Q151 237 155 217Q168 237 157 255Q151 260 153 273Z" fill="#ffc987"/>
        <path d="M226 299L272 271L367 273L416 299L403 332L311 352L236 335Z" fill="#4a4950" stroke="#2c2c32" stroke-width="4"/>
        <path d="M234 302L276 278L366 280L403 301L350 318L285 327Z" fill="#8e7671"/>
        <path d="M284 310L249 297L296 287L371 288L386 299L364 307Z" fill="#c1a68a"/>
        <path d="M187 303L201 297L222 311L222 324L204 325L190 316L177 322L170 314Z" fill="#694f42" stroke="#26252c" stroke-width="4"/>
        <path d="M213 307L390 151L443 116L423 157L232 326Z" fill="url(#steel)" stroke="#3d3a3d" stroke-width="4"/>
        <path d="M224 310L412 146L438 121M253 305L425 153" fill="none" stroke="#f1c78d" stroke-width="3"/>
        <path d="M289 284L292 254M299 287L322 271M279 279L268 257M148 232L142 212M173 244L185 227M98 250L90 236" fill="none" stroke="#edb276" stroke-width="3" stroke-linecap="round"/>
        """);
}
