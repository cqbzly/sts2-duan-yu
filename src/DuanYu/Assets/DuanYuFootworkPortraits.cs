namespace DuanYu.Assets;

internal static class DuanYuFootworkPortraits
{
    internal static string? Render(string designId) => designId switch
    {
        "DY_B04" => FirstRipple(),
        "DY_C11" => StepIntoTheVoid(),
        "DY_C12" => Sidestep(),
        "DY_C13" => Weave(),
        "DY_C16" => RetreatAndAdvance(),
        "DY_U18" => ShadowStep(),
        "DY_U19" => HeavenAndEarthStep(),
        "DY_U20" => CrossTheAttack(),
        "DY_U21" => TurnInTheGap(),
        "DY_U23" => WindWithoutTrace(),
        "DY_U24" => UnfetteredWalk(),
        "DY_U25" => HalfStepBack(),
        "DY_U34" => SevenStarsBackward(),
        "DY_R02" => LingboSteps(),
        _ => null
    };

    private static string Picture(string top, string bottom, string detail, string painting) => $$"""
        <svg xmlns="http://www.w3.org/2000/svg" width="1000" height="760" viewBox="0 0 500 380">
        <defs>
          <linearGradient id="field" x1="0%" y1="0%" x2="60%" y2="100%">
            <stop stop-color="{{top}}"/><stop offset="1" stop-color="{{bottom}}"/>
          </linearGradient>
          {{detail}}
        </defs>
        <rect width="500" height="380" fill="url(#field)"/>
        {{painting}}
        </svg>
        """;

    private static string FirstRipple() => Picture("#102a37", "#456d70", """
        <linearGradient id="robe" x1="5%" y1="20%" x2="85%" y2="100%">
          <stop stop-color="#687972"/><stop offset=".66" stop-color="#c1bf9d"/><stop offset="1" stop-color="#f3d9a3"/>
        </linearGradient>
        """, """
        <path d="M-8 36C107 6 247 42 370 29L513 3V101C383 59 244 87 118 124L0 124Z" fill="#12232c"/>
        <path d="M0 128C99 133 121 106 235 100S399 56 512 88V130C388 90 329 128 233 135S88 181 0 180Z" fill="#578382" opacity=".58"/>
        <path d="M0 220Q75 215 110 192T251 175M339 155Q411 133 510 132M21 300Q81 270 124 275M388 266Q448 247 517 249M7 350Q111 317 200 338T417 342" fill="none" stroke="#83a6a1" stroke-width="3" opacity=".48"/>
        <path d="M-16 306L49 278L111 288L140 321L115 359L-10 362ZM363 321L410 294L478 306L519 352L488 382H381Z" fill="#182b31"/>
        <path d="M-5 313L49 282L108 290L131 317L75 301L18 321M366 323L411 298L475 310L504 348L464 325L413 313" fill="#748477"/>
        <ellipse cx="299" cy="255" rx="114" ry="32" fill="#36565c"/>
        <path d="M191 253C211 220 393 212 410 249C424 280 237 292 193 266M227 254C226 236 354 225 376 248C397 270 247 281 228 259M263 252C277 241 334 237 348 250C339 257 288 267 263 257" fill="none" stroke="#aec9bb" stroke-width="3"/>
        <path d="M33-10H334Q320 68 271 130Q225 170 209 214Q158 182 149 137L90 166Q94 117 77 83Z" fill="#2b454a"/>
        <path d="M56-10H323Q303 48 283 86Q250 129 206 151Q193 172 188 186Q160 167 161 119Q123 129 96 150Q102 108 80 60Z" fill="url(#robe)"/>
        <path d="M88 4Q134 36 147 117L119 127Q120 64 88 4M217 13Q187 85 175 146Q205 114 226 57L242 21M295 25Q283 79 250 112L259 118Q287 90 295 25" fill="#3d5353" opacity=".52"/>
        <path d="M204 137Q231 142 262 112L270 170Q282 184 287 200L298 230L317 243Q326 251 316 258L279 258Q269 255 266 243L253 212Q222 189 215 167Z" fill="#15212b" stroke="#242a2c" stroke-width="3"/>
        <path d="M216 151Q237 148 258 132M222 166Q247 167 265 155M230 182Q257 183 269 168" fill="none" stroke="#829485" stroke-width="5"/>
        <path d="M270 223L281 249L309 250M242 191L260 202" fill="none" stroke="#577576" stroke-width="3"/>
        <path d="M276 254Q261 237 258 223M322 252L340 236M320 257L361 252M297 259L303 269" fill="none" stroke="#d4e2bb" stroke-width="3" stroke-linecap="round"/>
        <path d="M43 365Q126 344 209 358M347 301Q409 280 477 285" fill="none" stroke="#abc8bd" stroke-width="2" opacity=".6"/>
        """);

    private static string StepIntoTheVoid() => Picture("#142b22", "#709174", """
        <radialGradient id="light" cx="68%" cy="43%" r="65%">
          <stop stop-color="#f1d696"/><stop offset=".49" stop-color="#94a77a"/><stop offset="1" stop-color="#454d36" stop-opacity="0"/>
        </radialGradient>
        """, """
        <rect width="500" height="380" fill="url(#light)"/>
        <path d="M0 279Q101 235 182 275T369 285Q457 258 513 272V380H0Z" fill="#9cac85" opacity=".42"/>
        <path d="M21 94L109 108L144 199L102 212L97 328L26 369L-5 352V82ZM378 44L500 26V147L463 158L459 277L396 312L409 211L366 154Z" fill="#24382e"/>
        <path d="M0 87L112 103L147 191L102 173L76 126L30 120L9 157L-7 156ZM367 147L370 68L430 34L500 29V64L426 67L395 87L391 129Z" fill="#4c6450"/>
        <path d="M0 147L34 136L54 168L38 261L18 282L12 343M128 197L98 181L89 238L62 276L55 338M384 147L410 108L439 97L420 179L435 194L419 247L418 287M491 70L462 125L443 149L437 183" stroke="#829072" stroke-width="4" fill="none" opacity=".7"/>
        <path d="M158 229L211 214L235 224L215 232L190 258L174 246ZM276 156L320 142L352 149L327 158L306 176L288 168Z" fill="#414d3c"/>
        <path d="M157 228L207 214L232 223L207 224L173 236M276 155L320 142L351 149L322 148L285 163" fill="#b4b98b"/>
        <path d="M163 233C206 159 267 198 306 136C329 109 355 101 375 88" fill="none" stroke="#f4edbf" stroke-width="3" opacity=".51"/>
        <path d="M283 117Q263 125 247 143L225 157L218 154L245 129L258 104Z" fill="#243431"/>
        <path d="M268 129L286 132Q309 123 326 99L334 107Q321 144 294 150L280 148Q268 169 253 175L240 173L251 165L263 142L246 146L227 158Q217 157 218 150L243 130Z" fill="#162925"/>
        <path d="M260 99Q266 91 279 94L290 109L286 135L267 146L247 140L222 146L202 142L218 131L231 118L250 117Z" fill="#e4d3a2"/>
        <path d="M213 141L239 132L252 128L270 112L260 136L246 143L223 141M277 100L288 109L283 132L269 139" fill="#889c7f"/>
        <path d="M271 99Q258 91 260 82Q261 73 268 72Q280 68 287 77L290 87L282 94L281 102Z" fill="#967d57"/>
        <path d="M263 89Q252 79 262 69Q273 65 282 70L286 77L274 77L270 85L267 94Z" fill="#263329"/>
        <path d="M262 74Q251 77 246 70L237 72L246 83L261 82M256 114Q233 118 219 109L200 109Q213 124 235 123L256 121" fill="#324a40"/>
        <path d="M320 154L335 150M320 169L333 165M20 283L101 257M75 293L133 281M360 283L409 272M331 301L415 302" fill="none" stroke="#e4d6a0" stroke-width="2" opacity=".46"/>
        """);

    private static string Sidestep() => Picture("#241e20", "#593a35", """
        <linearGradient id="blade" y1="0%" x1="0%" y2="90%" x2="94%">
          <stop stop-color="#494b48"/><stop offset=".5" stop-color="#86998c"/><stop offset=".76" stop-color="#f8d8a3"/><stop offset="1" stop-color="#665749"/>
        </linearGradient>
        """, """
        <path d="M371 11L485-12L501 365L301 382Z" fill="#803f2a"/>
        <path d="M402 31L459 18L473 291L335 354Z" fill="#d48945"/>
        <path d="M386 104L476 90M365 186L478 164M344 281L484 242M431 27L405 320" fill="none" stroke="#693c30" stroke-width="7"/>
        <path d="M-37 342L138 310L283 334L486 310L521 380H-7Z" fill="#38282a"/>
        <path d="M48 369L193 339L394 337M180 378L314 356L490 342" stroke="#b8835c" stroke-width="3" fill="none" opacity=".52"/>
        <path d="M213 154Q178 128 181 112Q194 97 207 104Q221 113 222 126L227 134L222 137L218 146L228 162Z" fill="#b58c69"/>
        <path d="M183 133L173 127L175 110Q184 95 196 96Q211 97 219 110L205 111L191 128L201 149L193 154Z" fill="#252b29"/>
        <path d="M207 151Q223 150 242 174L259 208L309 238L330 234L319 254L288 253L242 239L227 229L235 265L218 279L177 265L152 239L160 209L184 166Z" fill="#a9aba0" stroke="#2c3030" stroke-width="4"/>
        <path d="M155 243Q213 233 228 188L235 196L226 249L243 282L224 324L203 334L198 318L201 284L168 303L134 319L107 317L107 309L147 287Z" fill="#263137"/>
        <path d="M167 212L182 224L208 215L223 177Q203 158 189 179ZM232 182L251 220L303 244L290 248L244 233L226 210" fill="#e6d4aa"/>
        <path d="M182 226L211 222L230 235L207 243L181 243L159 240M195 187L182 207M169 259L196 251" stroke="#52656a" stroke-width="4" fill="none"/>
        <path d="M-49 66L13 31L295 261L307 284Q290 308 230 294L-14 144Z" fill="#1c2026"/>
        <path d="M-39 56L13 20L284 252L301 278Q285 291 243 286L-13 132Z" fill="url(#blade)" stroke="#dfbd8e" stroke-width="3"/>
        <path d="M-5 80L259 268L292 278M62 91L243 237M88 111L107 124L99 134M154 176L180 192L171 201" fill="none" stroke="#534e46" stroke-width="3" opacity=".7"/>
        <path d="M234 202L277 219M251 190L280 198M281 229L310 239" fill="none" stroke="#e3ab69" stroke-width="3"/>
        """);

    private static string Weave() => Picture("#17182e", "#5f475d", """
        <linearGradient id="stream" x1="0%" y1="100%" x2="100%" y2="0%">
          <stop stop-color="#cb684c" stop-opacity="0"/><stop offset=".37" stop-color="#e49b64"/><stop offset="1" stop-color="#f0c887" stop-opacity=".26"/>
        </linearGradient>
        """, """
        <path d="M-8 324L185 122L345 100L501 224L501 380H-8Z" fill="#383047"/>
        <path d="M0 352L197 159L326 126L510 258M-5 375L202 194L313 158L504 290M35 383L205 233L314 201L505 321M103 390L229 269L325 239L499 348" fill="none" stroke="#7d5b67" stroke-width="3"/>
        <path d="M190 148V25L209 18L223 33V175L210 195L189 186ZM298 145V-10H316L329 4V159L319 175L303 169Z" fill="#171727"/>
        <path d="M195 31L200 183M303 0L318 162" fill="none" stroke="#ad7161" stroke-width="4"/>
        <path d="M84 304C252 334 368 265 337 234C305 200 103 257 126 211C145 175 385 172 371 110" fill="none" stroke="#241c2c" stroke-width="31" opacity=".63"/>
        <path d="M4 348C100 288 321 316 346 247C371 180 132 258 126 213C122 179 360 170 380 103" fill="none" stroke="url(#stream)" stroke-width="21"/>
        <path d="M6 353C86 289 333 301 347 245C365 180 124 252 126 211C129 172 367 169 378 108" fill="none" stroke="#f7d199" stroke-width="3"/>
        <path d="M227 227Q229 211 243 205L271 199L299 182L312 185L282 207L268 210L273 218L306 221L341 202L347 209L318 233L280 239L268 253L250 268L225 263L238 251L241 235Z" fill="#251e2c"/>
        <path d="M271 195Q282 186 297 185L290 196L270 211Q258 230 231 243Q203 249 189 241L174 248Q167 239 182 224L215 212Q232 180 260 190Z" fill="#dcc1a0"/>
        <path d="M190 236Q214 227 239 223L257 201L270 196L253 222L239 238L216 246L188 244M224 216L241 199L249 198" fill="#968a77"/>
        <path d="M258 192Q254 180 263 172Q277 163 287 174L290 184L298 190L287 191L283 198L270 200Z" fill="#a7805f"/>
        <path d="M263 190Q251 184 256 174Q267 160 283 167L287 175Q275 172 270 178L268 192L247 187L232 179L244 178Z" fill="#24222a"/>
        <path d="M-2 365L2 74L43 39L75 70L83 346L56 374Z" fill="#211924"/>
        <path d="M38 62L47 349L26 351L19 78Z" fill="#573640"/>
        <path d="M450 308L444 22L478-9L507 2V374L474 360Z" fill="#251c2b"/>
        <path d="M464 32L478 9L486 359L467 332Z" fill="#63424a"/>
        <path d="M166 264L197 255M113 261L86 264M383 166L408 153M356 284L382 271" fill="none" stroke="#e1a171" stroke-width="3" opacity=".72"/>
        """);

    private static string RetreatAndAdvance() => Picture("#211b2b", "#68627c", """
        <linearGradient id="deflection" x1="12%" y1="100%" x2="93%" y2="10%">
          <stop stop-color="#629a96" stop-opacity="0"/><stop offset=".35" stop-color="#a6d3c1"/><stop offset=".71" stop-color="#f6dfa4"/><stop offset="1" stop-color="#bc7d47" stop-opacity="0"/>
        </linearGradient>
        """, """
        <path d="M0 225L75 169L134 165L172 192L222 164L271 177L319 149L375 161L418 139L506 158V380H0Z" fill="#3f465f"/>
        <path d="M0 251L103 205L155 224L192 199L255 218L306 183L382 194L427 160L506 168V380H0Z" fill="#303342"/>
        <path d="M0 289C91 260 207 290 320 214L501 180V212L345 251C225 326 91 335 0 327Z" fill="#221d2c"/>
        <path d="M-10 290C89 275 187 299 321 222L512 182L501 199L334 237C208 312 84 315 -8 311Z" fill="#82646b"/>
        <path d="M0 296C99 281 198 302 318 226L500 188M49 300L48 313M86 298L85 314M126 296L130 310M165 291L173 305M209 281L218 294M252 269L260 282M288 249L299 261M346 220L354 233M399 209L410 220M445 199L449 209" fill="none" stroke="#d5af92" stroke-width="3"/>
        <path d="M18 307L36 371M158 301L165 370M322 235L331 316M433 208L435 274" fill="none" stroke="#261f30" stroke-width="13"/>
        <path d="M18 304L28 365M160 303L158 359M323 236L330 308" fill="none" stroke="#9b8082" stroke-width="2"/>
        <path d="M215 254Q163 273 122 256Q93 241 109 212Q132 165 224 158Q299 152 394 72Q367 119 316 156Q242 203 172 195Q127 192 125 218Q119 244 154 244L220 234Z" fill="url(#deflection)"/>
        <path d="M185 254Q137 262 114 235Q107 225 118 206C162 153 248 198 371 100" fill="none" stroke="#f8dca5" stroke-width="3" opacity=".85"/>
        <path d="M219 256L236 225L259 214L274 194L285 201L274 225L251 246L249 257L269 269L264 277L243 275L234 266L222 273L201 284L183 281L183 275L204 268Z" fill="#24232e"/>
        <path d="M219 206L231 183L253 177L247 190L231 214L239 230L225 254Q204 265 182 260L165 247L187 239L183 227Z" fill="#dbc5a6" stroke="#2b3542" stroke-width="3"/>
        <path d="M198 209L187 218L191 225L202 221L215 213L218 199Q207 197 198 209Z" fill="#b48b6d"/>
        <path d="M191 205Q201 188 215 197L220 201L215 212L207 207L200 214L187 216L177 208Z" fill="#25303b"/>
        <path d="M189 235L221 215L232 224L213 232L196 250L171 248M217 247L230 231" fill="none" stroke="#819b91" stroke-width="5"/>
        <path d="M248 184L268 171M335 132L369 103M100 233L84 223M68 245L111 249M405 76L436 57" fill="none" stroke="#fff0ba" stroke-width="2" opacity=".7"/>
        """);

    private static string ShadowStep() => Picture("#0c1728", "#1c5261", """
        <linearGradient id="wash" x1="20%" y1="90%" x2="94%" y2="24%">
          <stop stop-color="#447e8e" stop-opacity="0"/><stop offset=".52" stop-color="#569ba0"/><stop offset="1" stop-color="#d3d7ab" stop-opacity=".36"/>
        </linearGradient>
        """, """
        <path d="M24 380L206 264L482 23L500 123L315 298L167 380Z" fill="url(#wash)"/>
        <path d="M2 378L164 306L393 123L506 69M83 382L268 282L457 141L512 118M213 363L344 267L482 179" fill="none" stroke="#78bbb3" stroke-width="4" opacity=".47"/>
        <g fill="#122531">
          <path d="M59 380L127-26L142-22L84 380ZM173 390L134-5L147-4L196 384ZM306 380L338-9L356-7L327 380ZM426 390L411-19L428-4L448 380ZM481 378L467-10L500-7L509 378Z"/>
          <path d="M97 234Q145 164 185 164L142 185L119 218L159 191L134 220L100 245ZM143 100L98 46L61 55L95 29L142 83L153 94L183 61L221 43L182 74L153 109ZM335 84L293 58L245 71L289 49L339 78L364 45L401 25L369 53L342 88ZM321 278L269 237L228 259L253 224L273 227L325 263L358 219L398 202L361 229L325 287Z"/>
        </g>
        <path d="M129 9L74 374M343 8L314 375M422 6L439 365M94 326L76 324M116 207L95 204M333 111L346 112M327 205L343 207M436 248L419 250" fill="none" stroke="#38626c" stroke-width="3" opacity=".84"/>
        <path d="M122 289L209 207L255 161L289 149L323 113L358 112L320 135L297 168L258 191L237 225L159 277Z" fill="#a0d0bf" opacity=".33"/>
        <path d="M141 279L207 221L229 205L228 190L250 170L269 165L265 150L280 130L303 125L306 140L288 143L285 171L270 190L246 209L239 229L199 260L177 260L205 238L201 232Z" fill="#101c2a"/>
        <path d="M230 191Q234 177 254 165L264 158L266 147L282 140L294 147L286 160L275 165L268 190L246 208L222 213L197 229L183 232L195 211Z" fill="#8ea6a2"/>
        <path d="M224 200L266 177L279 156L287 151L277 179L257 197L239 205L204 216Z" fill="#dce4bc"/>
        <path d="M288 114Q295 113 302 120L306 130L302 139L292 144L283 139L277 131L279 121Z" fill="#728781"/>
        <path d="M276 133Q269 124 277 115Q287 107 296 114L300 120L290 119Q285 121 283 133L279 139L265 134L252 122L268 126Z" fill="#152331"/>
        <path d="M13 380L54-11H72L38 380ZM458 390L455 17L475-12L485 390Z" fill="#0e1621"/>
        <path d="M34 165L16 123L-3 116L17 109L35 140L65 100L89 88L67 108L54 145L75 124L87 125L51 159ZM464 232L425 203L406 210L435 189L467 210L512 170L509 195Z" fill="#172c35"/>
        """);

    private static string HeavenAndEarthStep() => Picture("#142332", "#35495e", """
        <radialGradient id="floor" cx="43%" cy="43%" r="56%">
          <stop stop-color="#334f58"/><stop offset=".7" stop-color="#243345"/><stop offset="1" stop-color="#161e2d"/>
        </radialGradient>
        """, """
        <path d="M11 101L177 8L385 28L501 145L461 285L295 380L87 357L-5 238Z" fill="#142130" stroke="#72878b" stroke-width="3"/>
        <path d="M28 108L181 26L372 45L476 151L438 273L289 359L103 339L18 233Z" fill="url(#floor)" stroke="#beac85" stroke-width="4"/>
        <path d="M55 128L190 56L354 71L445 159L412 252L281 328L118 311L42 229Z" fill="none" stroke="#687f80" stroke-width="2"/>
        <path d="M48 132L138 173M177 51L211 116M361 61L322 118M458 144L375 168M425 249L352 235M290 341L269 279M113 323L154 261M29 227L124 216" fill="none" stroke="#84928b" stroke-width="3"/>
        <path d="M167 132L197 116L238 112L278 119L305 138L321 159L327 189L318 216L299 238L267 250L230 252L194 245L164 227L151 199L150 173Z" fill="#213242" stroke="#ada182" stroke-width="4"/>
        <path d="M234 114C298 125 327 169 319 202C307 235 271 252 239 254C202 248 188 224 218 207C250 187 281 180 278 154C275 136 256 122 234 114Z" fill="#7d8580"/>
        <ellipse cx="256" cy="220" rx="10" ry="6" fill="#1f3142"/>
        <ellipse cx="238" cy="149" rx="10" ry="6" fill="#7d8580"/>
        <path d="M127 275Q186 225 240 189Q284 161 347 119" fill="none" stroke="#b5e1c4" stroke-width="3" opacity=".7"/>
        <path d="M118 270L153 250L186 254L148 280Z" fill="#202432"/>
        <path d="M111 261L147 243L179 249L140 272Z" fill="#e2bc75" stroke="#5d564f" stroke-width="3"/>
        <path d="M122 264L140 272L179 249L178 257L140 280L123 271Z" fill="#887752"/>
        <path d="M130 254L150 247M137 259L157 251" fill="none" stroke="#786444" stroke-width="2"/>
        <path d="M330 122L371 102L407 114L367 137Z" fill="#272535"/>
        <path d="M322 113L366 95L401 106L357 130Z" fill="#eee1b3" stroke="#765b4f" stroke-width="3"/>
        <path d="M337 112L361 102M343 117L369 107M353 121L380 109" fill="none" stroke="#8d836c" stroke-width="2"/>
        <path d="M203 228Q230 198 247 189L259 191L244 209L229 227L218 239L200 237L194 231Z" fill="#19232e" stroke="#a9c4a6" stroke-width="2"/>
        <path d="M245 183Q256 161 280 145L289 151L275 169L264 188L253 194L241 189Z" fill="#d1c1a3" stroke="#26313c" stroke-width="3"/>
        <path d="M19 234L101 347L290 369M183 13L372 34L486 141" fill="none" stroke="#d0ad79" stroke-width="2" opacity=".65"/>
        """);

    private static string CrossTheAttack() => Picture("#3b252c", "#b47554", """
        <linearGradient id="cloth" x1="15%" y1="60%" x2="100%" y2="24%">
          <stop stop-color="#65474c"/><stop offset=".4" stop-color="#926c61"/><stop offset="1" stop-color="#eac38c"/>
        </linearGradient>
        """, """
        <path d="M48 370L48 182Q49 85 170 24Q245 4 324 16L403 62L500 156V380Z" fill="#663f3d"/>
        <path d="M159 380L154 203Q166 111 250 79Q327 63 369 111L402 168L410 380Z" fill="#e0a477"/>
        <path d="M173 380L175 208Q180 132 253 103Q325 94 356 126L389 175L391 380Z" fill="#aa6d56"/>
        <path d="M186 216L174 356L247 380L345 380L403 344L386 201L328 167L255 179Z" fill="#8b554b"/>
        <path d="M9 101L167 56M18 143L137 105M7 199L104 171M14 240L97 216M5 293L100 272M399 164L498 163M405 209L499 210M417 265L498 253" fill="none" stroke="#bf896b" stroke-width="3" opacity=".52"/>
        <path d="M74-40L224-14L479 376H376Z" fill="#2c2028" opacity=".48"/>
        <path d="M-16 300L26 334L167 217L208 179L160 193ZM492 262L451 226L340 251L318 271L357 274Z" fill="#473239"/>
        <path d="M-18 300L28 331L148 232L199 189L164 196Z" fill="#6f5d54"/>
        <path d="M22 325L149 222L191 194M352 265L446 233L490 259" fill="none" stroke="#e5a976" stroke-width="4"/>
        <path d="M222 234L233 271L210 304L186 314L183 323L211 325L233 312L261 285L274 249L299 263L305 298L316 316L336 312L342 300L326 298L323 253L297 221L268 217Z" fill="#352b35" stroke="#412d32" stroke-width="3"/>
        <path d="M263 163Q272 155 285 173L275 200L299 219Q323 233 337 225L324 246Q299 247 272 237L251 253L222 275L202 257L191 230L206 218L229 198L234 178Z" fill="url(#cloth)" stroke="#523845" stroke-width="3"/>
        <path d="M281 167L277 152L263 145L249 151L239 169L247 179L260 184L269 176L276 178Z" fill="#c1966f"/>
        <path d="M245 172L235 166L238 147Q249 136 267 141L275 149L261 149L252 164L249 173L236 180L221 175L231 169Z" fill="#322b36"/>
        <path d="M240 178Q231 210 210 227L197 234L210 247L231 234L246 214L258 189M277 203L297 222L320 233" fill="none" stroke="#dfb882" stroke-width="5"/>
        <path d="M233 236Q250 218 256 205L273 214L267 227L247 243Z" fill="#59474a"/>
        <path d="M196 190L210 181M208 194L222 189M328 271L302 276M316 277L296 289" fill="none" stroke="#f5c848" stroke-width="3" opacity=".7"/>
        """);

    private static string TurnInTheGap() => Picture("#1c2929", "#9a785c", """
        <linearGradient id="sky" x1="0%" y1="0%" x2="50%" y2="100%">
          <stop stop-color="#f0cb80"/><stop offset=".65" stop-color="#c58f62"/><stop offset="1" stop-color="#594d48"/>
        </linearGradient>
        """, """
        <path d="M183 0L299 0L346 324L268 380L181 339Z" fill="url(#sky)"/>
        <path d="M0 2L182 0L162 152L197 327L171 380H0ZM301 0H500V380L366 373L325 288L332 184Z" fill="#283530"/>
        <path d="M106 0L181 0L163 153L197 326L176 375L147 342L145 264L104 198ZM302 0H387L373 98L352 203L357 288L393 379L365 372L325 287L332 182Z" fill="#78644b"/>
        <path d="M151 2L131 106L136 178L166 249L161 307L176 358M321 0L339 100L345 168L342 242L368 331M375 12L360 95L353 120M42 65L89 88L103 141L85 185L107 248M434 26L425 129L443 178L427 212L457 295" fill="none" stroke="#ac8764" stroke-width="4" opacity=".47"/>
        <path d="M141 371L193 309L233 320L278 304L313 325L346 309L401 380Z" fill="#493f37"/>
        <path d="M207 345L242 321L277 328L263 342L226 359M275 370L285 347L329 336L319 364" fill="#9e8160"/>
        <path d="M138 163L193 191L208 201L172 190L104 177ZM354 183L307 194L286 209L325 209L381 196Z" fill="#1f2d2e"/>
        <path d="M147 170L182 188L202 201M354 189L309 198L288 207" fill="none" stroke="#d9bd8c" stroke-width="3"/>
        <path d="M227 274C271 271 330 230 300 209C274 194 219 227 201 215C188 208 185 195 203 186" fill="none" stroke="#ffdc9b" stroke-width="7" opacity=".64"/>
        <path d="M231 237L220 252L219 283L201 311L185 317L192 326L215 319L239 296L244 266L257 271L268 292L288 304L300 296L295 289L282 284L277 253L268 236Z" fill="#2c3533" stroke="#4c4638" stroke-width="3"/>
        <path d="M241 201L255 192L273 201L282 213L285 215L277 223L269 220L261 224L255 234L238 225L225 227L213 247L199 246L208 224Z" fill="#bea283" stroke="#5e574b" stroke-width="3"/>
        <path d="M259 216Q267 224 264 237L277 251Q261 262 244 257L225 241L219 225L245 209Z" fill="#e1d0a4"/>
        <path d="M222 232L245 238L262 230L267 240L247 251L225 244M244 209L255 205L267 214" fill="none" stroke="#6d8177" stroke-width="5"/>
        <path d="M244 199Q245 184 256 180Q272 174 282 188L280 196L273 199L267 209L253 205Z" fill="#af805d"/>
        <path d="M251 204L241 197L240 188Q248 172 264 173L279 183L269 184L260 188L254 198L246 206L228 208L222 201Z" fill="#303934"/>
        <path d="M150 113L175 101L168 125ZM333 118L365 100L356 126ZM291 132L305 145L298 160ZM196 153L209 149L205 168Z" fill="#e4b77d"/>
        <path d="M176 214L193 218M297 228L322 216M206 268L181 277M280 273L312 260" fill="none" stroke="#eed3a1" stroke-width="3" opacity=".7"/>
        """);
    private static string WindWithoutTrace() => Picture("#223739", "#87958b", """
        <linearGradient id="air" x1="5%" y1="94%" x2="96%" y2="10%">
          <stop stop-color="#b4c4b2" stop-opacity=".2"/><stop offset=".62" stop-color="#d5dac1"/><stop offset="1" stop-color="#f4e3af" stop-opacity=".18"/>
        </linearGradient>
        """, """
        <path d="M0 175Q92 107 204 134Q304 160 393 98Q440 75 503 92V160Q416 164 363 200Q305 238 207 223L0 269Z" fill="#738e88"/>
        <path d="M-15 201Q113 153 177 171T330 161Q399 120 514 115M1 234Q147 185 219 202Q312 224 406 173L508 150" fill="none" stroke="#acb4aa" stroke-width="5" opacity=".57"/>
        <path d="M-1 341Q62 286 134 303Q218 324 319 222Q360 183 428 187Q471 186 506 171V385H0Z" fill="#2d423d"/>
        <path d="M-12 359Q95 304 171 323Q231 330 305 246Q344 200 411 194Q465 193 517 181" fill="none" stroke="#687d61" stroke-width="12"/>
        <path d="M29 378Q39 353 75 337M59 377Q79 334 131 329M77 378Q107 346 143 346M151 381Q188 367 217 348M183 362Q208 329 251 313M240 347Q256 304 291 280M298 299Q313 262 349 236M333 272Q356 230 401 212M389 247Q414 213 466 204M433 270Q472 226 508 215" fill="none" stroke="#a1a383" stroke-width="3" opacity=".62"/>
        <path d="M-21 198C130 283 293 131 515 72C340 156 194 320 14 232Z" fill="url(#air)" opacity=".62"/>
        <path d="M-1 217C179 274 316 119 501 89M55 242C165 245 210 213 266 190M314 143Q352 121 430 97" fill="none" stroke="#dfdec6" stroke-width="4" opacity=".78"/>
        <path d="M126 237Q215 195 274 207L260 217Q212 243 196 256Q171 250 137 261L91 275Z" fill="#b96d55" stroke="#493d3a" stroke-width="4"/>
        <path d="M111 266Q154 242 187 239Q219 234 255 211L233 231Q203 255 195 255Q171 248 153 256Z" fill="#e8bb8c"/>
        <path d="M340 150Q360 127 394 130L366 145ZM291 180Q309 156 342 159L323 171ZM398 218Q424 198 444 201L427 214ZM136 198L146 192L170 195L159 202Z" fill="#e2cda5"/>
        """);

    private static string UnfetteredWalk() => Picture("#486573", "#efe0aa", """
        <linearGradient id="cloud" x1="0%" y1="0%" x2="5%" y2="100%">
          <stop stop-color="#f7e5ad"/><stop offset="1" stop-color="#a9bfac"/>
        </linearGradient>
        """, """
        <path d="M-10 158C48 146 75 182 133 153C189 118 209 152 242 122Q270 94 326 115L510 133V380H-10Z" fill="#c3c8ac" opacity=".72"/>
        <path d="M-8 226Q61 187 125 211Q160 219 183 199Q224 171 281 182Q338 197 367 168Q418 134 507 152V380H0Z" fill="url(#cloud)"/>
        <path d="M0 259Q57 227 129 249Q177 272 245 218Q272 198 316 209Q376 227 429 186Q459 162 520 181M-1 294Q91 266 180 293Q236 306 306 256Q358 213 438 242M192 182Q244 140 299 159M395 124Q436 114 465 132" stroke="#fbebbd" stroke-width="4" fill="none" opacity=".58"/>
        <path d="M0 319L71 284L156 264L217 230L257 223L332 250L382 292L449 303L500 350V380H0Z" fill="#344e49"/>
        <path d="M0 337L68 302L167 282L229 247L261 240L310 257L373 303L430 325L449 380H-1Z" fill="#739073"/>
        <path d="M-8 380L86 332L163 320L216 289L258 253L235 251L179 287L72 306L8 332Z" fill="#c3ac7a"/>
        <path d="M1 355L83 323L180 303L216 277L242 257M126 321L156 315M170 308L179 306M213 288L223 280M249 261L260 255" fill="none" stroke="#f4d39b" stroke-width="3" opacity=".7"/>
        <path d="M189 241L202 234L229 243L218 254L196 261H183L178 256Z" fill="#50574b"/>
        <path d="M212 229L221 214L236 210L249 218L257 233L274 239L266 248L242 241L218 248L204 262L188 260L191 250Z" fill="#233638"/>
        <path d="M213 211L218 199L233 197L245 209L246 224L261 234L246 242L234 230L203 246L191 244L200 229Z" fill="#e4d4a5"/>
        <path d="M211 219L230 207L238 215L230 230L206 239L194 240M219 231L241 216L245 226" fill="none" stroke="#819683" stroke-width="4"/>
        <path d="M214 204Q207 196 213 186Q222 176 234 184L237 196L232 203L222 205Z" fill="#9d7c56"/>
        <path d="M213 199L207 193L208 185L220 177L232 180L237 185L227 187L221 197L220 205L197 207L182 215L169 206L193 197Z" fill="#273b3a"/>
        <path d="M341 91Q354 86 365 95Q374 83 387 85M391 110Q401 104 412 111Q422 102 431 104" fill="none" stroke="#3e5152" stroke-width="3"/>
        <path d="M3 380Q20 353 57 344L43 358Q84 348 110 356Q62 360 47 380M318 362Q349 331 392 335L377 342L420 350L421 369L402 380Z" fill="#41544a"/>
        """);

    private static string HalfStepBack() => Picture("#2e2830", "#62434a", """
        <radialGradient id="floor" cx="44%" cy="41%" r="65%">
          <stop stop-color="#aa8565"/><stop offset=".72" stop-color="#644850"/><stop offset="1" stop-color="#3f313d"/>
        </radialGradient>
        """, """
        <path d="M0 31H500V380H0Z" fill="url(#floor)"/>
        <path d="M0 73L500 35M-15 174L514 132M0 280L507 239M-1 377L501 338M119 57L159 365M280 53L321 349M401 39L473 344" fill="none" stroke="#453339" stroke-width="6" opacity=".62"/>
        <path d="M0 84L500 47M-3 184L496 143M0 292L482 252M108 57L147 369M271 55L309 345M391 48L459 347" fill="none" stroke="#b78a69" stroke-width="3" opacity=".37"/>
        <path d="M-30 312C125 283 215 236 301 145Q385 67 498 83Q429 95 393 122C241 219 217 298 3 352Z" fill="#402b35" opacity=".77"/>
        <path d="M-10 324Q94 321 199 254T411 110Q451 93 501 96" fill="none" stroke="#ef9d67" stroke-width="5"/>
        <path d="M232-24H459Q438 36 384 77L353 120L287 137L290 94L259 72L243 100L187 79Q217 43 232-24Z" fill="#32434a"/>
        <path d="M251-10H423Q403 34 365 67L344 109L311 117L318 81L280 61L260 75L228 65Q249 33 251-10Z" fill="#c4b298"/>
        <path d="M247 53Q286 15 288-8M268 58Q313 42 332 7M341 96Q389 49 403 4" fill="none" stroke="#758585" stroke-width="6"/>
        <path d="M286 92L306 111L299 143L270 175L234 175L227 162L252 144L259 115Z" fill="#292833" stroke="#282730" stroke-width="4"/>
        <path d="M284 113Q281 142 260 151L242 164M257 120L284 122M252 136L278 138" fill="none" stroke="#7b9994" stroke-width="4"/>
        <path d="M222 195Q187 185 145 205L148 217L180 216L211 207ZM210 217Q179 222 158 239L167 248Q191 244 222 228Z" fill="#d2a47d" opacity=".52"/>
        <path d="M236 174Q213 185 212 206Q209 221 232 231M226 180Q194 191 191 220Q187 243 211 256" fill="none" stroke="#f4d6a8" stroke-width="3" opacity=".86"/>
        <path d="M171 263L153 275M150 256L133 264M143 241L105 252" fill="none" stroke="#edce9f" stroke-width="3" opacity=".58"/>
        """);

    private static string SevenStarsBackward() => Picture("#151d32", "#48636f", """
        <linearGradient id="tiles" x1="0%" y1="0%" x2="70%" y2="100%">
          <stop stop-color="#304051"/><stop offset="1" stop-color="#142636"/>
        </linearGradient>
        """, """
        <path d="M-86 288L210-18L503 46L530 141L220 390Z" fill="#132536"/>
        <path d="M-11 255L216 6L492 63L501 108L211 366Z" fill="url(#tiles)"/>
        <path d="M19 254L218 9L483 65M47 274L248 29M79 284L280 39M112 305L313 47M147 322L348 53M176 344L389 61M211 361L429 69M245 342L462 75M279 310L495 81M118 125L382 179M77 171L335 229M33 221L275 282" fill="none" stroke="#879599" stroke-width="3" opacity=".49"/>
        <path d="M-10 257L208 357L506 92L512 111L213 377L-5 281Z" fill="#969a8c"/>
        <path d="M-4 269L210 371L511 104L503 123L211 390L-1 289Z" fill="#1b2736"/>
        <path d="M299 301L441 168L500 179V380H369Z" fill="#293b45"/>
        <path d="M316 305L446 181L501 192M347 323L472 183M376 337L497 193M412 360L500 243" fill="none" stroke="#667f80" stroke-width="3" opacity=".48"/>
        <path d="M335 105L281 119L273 166L220 181L224 225L171 256L160 299" stroke="#a0ccbe" stroke-width="5" stroke-linejoin="round" fill="none" opacity=".55"/>
        <path d="M335 105L281 119L273 166L220 181L224 225L171 256L160 299" stroke="#def0cb" stroke-width="2" fill="none"/>
        <path d="M335 95L338 102L345 105L338 108L335 115L332 108L325 105L332 102ZM281 109L284 116L291 119L284 122L281 129L278 122L271 119L278 116ZM273 156L276 163L283 166L276 169L273 176L270 169L263 166L270 163ZM220 171L223 178L230 181L223 184L220 191L217 184L210 181L217 178ZM224 214L227 222L235 225L227 228L224 236L221 228L213 225L221 222ZM171 244L175 252L183 256L175 260L171 268L167 260L159 256L167 252ZM160 282L165 294L177 299L165 304L160 316L155 304L143 299L155 294Z" fill="#e2eac8"/>
        <path d="M191 267L206 281L191 298L179 300L173 290L180 278L162 284L152 280L166 269Z" fill="#172634"/>
        <path d="M201 246Q212 246 218 257L209 274L192 279L175 277L160 284L150 275L168 263L173 253Z" fill="#c7ceb1" stroke="#455758" stroke-width="3"/>
        <path d="M213 244L217 247L207 262L191 270L171 270M199 253L184 254L176 263" fill="none" stroke="#72867e" stroke-width="3"/>
        <path d="M211 249Q202 238 209 229L226 228L234 239L228 251L218 254Z" fill="#b3906c"/>
        <path d="M206 243L203 232L212 223L226 223L231 231L219 230L213 238L211 244L203 249L189 244Z" fill="#1c2b39"/>
        <path d="M342 289L375 268M333 312L351 302M31 271L27 284M64 289L61 304M399 196L409 204M431 166L441 173" fill="none" stroke="#aebeae" stroke-width="3" opacity=".68"/>
        """);

    private static string LingboSteps() => Picture("#0d2337", "#508780", """
        <radialGradient id="water" cx="68%" cy="36%" r="77%">
          <stop stop-color="#87b3a0"/><stop offset=".53" stop-color="#3b6d75"/><stop offset="1" stop-color="#122c41"/>
        </radialGradient>
        <linearGradient id="arc" x1="3%" y1="99%" x2="95%" y2="2%">
          <stop stop-color="#80c2b2" stop-opacity=".08"/><stop offset=".43" stop-color="#c4e1bc"/><stop offset=".84" stop-color="#f3e3ac"/><stop offset="1" stop-color="#add9c4" stop-opacity=".3"/>
        </linearGradient>
        """, """
        <rect width="500" height="380" fill="url(#water)"/>
        <path d="M0 73Q112 36 194 60Q292 88 373 40L506 14M0 117Q101 81 204 101Q319 121 504 62M1 262Q75 238 141 241M383 204Q449 173 512 185M292 333Q387 307 505 267" fill="none" stroke="#98bdb2" stroke-width="3" opacity=".46"/>
        <path d="M0 349L53 323L101 337L116 367L86 386H0ZM405 376L419 341L455 313L500 300V380Z" fill="#213846"/>
        <path d="M0 355L55 330L96 342L104 364L66 353L31 361ZM424 352L454 322L508 305L499 323L464 333Z" fill="#6b8e8e"/>
        <path d="M54 303C124 325 156 263 207 221C275 165 370 238 416 160C440 121 413 75 376 63C438 67 458 127 428 174C374 258 276 183 224 235C168 281 122 348 54 303Z" fill="url(#arc)" opacity=".74"/>
        <path d="M56 303C133 323 173 250 212 222C278 173 381 234 418 165Q447 108 379 65" fill="none" stroke="#eeedbf" stroke-width="4"/>
        <ellipse cx="124" cy="289" rx="60" ry="13" fill="none" stroke="#a9d7bd" stroke-width="3" opacity=".55"/>
        <ellipse cx="218" cy="222" rx="49" ry="11" fill="none" stroke="#c3e3c6" stroke-width="3" opacity=".67"/>
        <ellipse cx="363" cy="164" rx="73" ry="15" fill="none" stroke="#f1e7b9" stroke-width="4" opacity=".83"/>
        <path d="M118 259L137 248L153 259L147 278L127 284L104 297L94 294L113 279L102 274L85 278L89 271L103 263Z" fill="#7cafad" opacity=".68"/>
        <path d="M126 252L115 246L120 233L130 227L140 232L142 242L134 250L145 254L153 266L133 273L120 261L106 267L91 264L105 255Z" fill="#a7c9b6" opacity=".62"/>
        <path d="M204 181L221 161L241 167L248 181L248 204L264 216L253 223L225 216L219 207L196 229L179 231L183 221L201 201Z" fill="#305c66" opacity=".87"/>
        <path d="M212 171L195 186L171 184L162 192L180 201L207 194L231 190L252 194L257 184L245 174L231 168L233 153L224 143L213 146L205 154L206 165Z" fill="#91bfb0" opacity=".79"/>
        <path d="M323 104L347 104L365 125L386 126L403 118L413 124L409 134L381 148L363 148L352 139L336 155L321 165L300 181L281 182L282 172L307 156L316 140L318 125Z" fill="#14333e"/>
        <path d="M339 84L353 78L374 88L394 87L411 78L420 82L406 99L383 106L369 102L353 124L332 133L307 130L284 145L255 133L275 126L291 113L315 112L325 95Z" fill="#e1d7a8" stroke="#435e61" stroke-width="3"/>
        <path d="M321 118L337 103L353 97L364 89L378 91L363 110L346 122L320 133L292 128L271 136M379 97L398 94L410 86" fill="none" stroke="#839b8d" stroke-width="5"/>
        <path d="M348 85Q334 74 337 62Q349 49 360 54L372 62L378 76L365 80L359 91Z" fill="#ad9164"/>
        <path d="M342 77Q329 69 335 57Q345 45 360 49L368 57L359 61L352 58L347 70L345 79L327 82L303 73L316 69L331 72Z" fill="#182f3e"/>
        <path d="M294 178L319 172M306 185L335 182M409 160L438 154M401 170L432 167M151 292L163 290M115 305L144 300M239 243L261 240" fill="none" stroke="#f3eabc" stroke-width="2" opacity=".73"/>
        """);
}
