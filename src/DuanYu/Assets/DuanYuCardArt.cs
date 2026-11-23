namespace DuanYu.Assets;

internal static class DuanYuCardArt
{
    public static string Render(string designId)
    {
        if (designId == "DY_A01") return AncientStele();

        return DuanYuSwordPortraits.Render(designId)
            ?? DuanYuFootworkPortraits.Render(designId)
            ?? DuanYuQiPortraits.Render(designId)
            ?? DuanYuTechniquePortraits.Render(designId)
            ?? DuanYuLotusPortraits.Render(designId)
            ?? DuanYuSupportPortraits.Render(designId)
            ?? throw new ArgumentOutOfRangeException(nameof(designId), designId, "No card illustration is defined.");
    }

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
}
