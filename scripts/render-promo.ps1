param([string]$ArtPath)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repositoryRoot = Split-Path $PSScriptRoot -Parent
if (-not $ArtPath) { $ArtPath = Join-Path $repositoryRoot 'artifacts/card-art' }
$design = Get-Content -LiteralPath (Join-Path $repositoryRoot 'data/cards.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$cardsById = @{}
foreach ($card in $design.cards) { $cardsById[$card.id] = $card }
$outputDirectory = Join-Path $repositoryRoot 'assets'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$ink = [System.Drawing.ColorTranslator]::FromHtml('#102A32')
$paper = [System.Drawing.ColorTranslator]::FromHtml('#F5EEDB')
$gold = [System.Drawing.ColorTranslator]::FromHtml('#DDBB78')
$muted = [System.Drawing.ColorTranslator]::FromHtml('#A2BEB8')

function New-Canvas([int]$Width, [int]$Height) {
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear($ink)
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Draw-Text($Graphics, [string]$Text, [float]$Size, $Color, [float]$Left, [float]$Top, [float]$Width, [float]$Height, [switch]$Serif) {
    $fontFamily = if ($Serif) { 'SimSun' } else { 'Microsoft YaHei' }
    $font = New-Object System.Drawing.Font($fontFamily, $Size, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $rectangle = New-Object System.Drawing.RectangleF($Left, $Top, $Width, $Height)
    $format = New-Object System.Drawing.StringFormat
    $format.Trimming = [System.Drawing.StringTrimming]::None
    try {
        $measured = $Graphics.MeasureString($Text, $font, [System.Drawing.SizeF]::new($Width, 10000), $format)
        if ($measured.Height -gt $Height + 2) { throw "Text does not fit its layout: $Text" }
        $Graphics.DrawString($Text, $font, $brush, $rectangle, $format)
    }
    finally { $font.Dispose(); $brush.Dispose(); $format.Dispose() }
}

function Draw-Image($Graphics, [string]$CardId, [int]$Left, [int]$Top, [int]$Width, [int]$Height) {
    $imagePath = Join-Path $ArtPath "$CardId.png"
    $image = [System.Drawing.Image]::FromFile($imagePath)
    try { $Graphics.DrawImage($image, $Left, $Top, $Width, $Height) }
    finally { $image.Dispose() }
}

function Draw-Fill($Graphics, $Color, [int]$Left, [int]$Top, [int]$Width, [int]$Height) {
    $brush = New-Object System.Drawing.SolidBrush($Color)
    try { $Graphics.FillRectangle($brush, $Left, $Top, $Width, $Height) }
    finally { $brush.Dispose() }
}

function Draw-Line($Graphics, $Color, [float]$Width, [int]$Left, [int]$Top, [int]$Right, [int]$Bottom) {
    $pen = New-Object System.Drawing.Pen($Color, $Width)
    try { $Graphics.DrawLine($pen, $Left, $Top, $Right, $Bottom) }
    finally { $pen.Dispose() }
}

$cover = New-Canvas 1024 1024
try {
    Draw-Image $cover.Graphics 'DY_R03' 0 250 1024 778
    $fadeRectangle = New-Object System.Drawing.Rectangle(0, 240, 1024, 450)
    $fade = New-Object System.Drawing.Drawing2D.LinearGradientBrush($fadeRectangle, $ink, [System.Drawing.Color]::Transparent, [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    try { $cover.Graphics.FillRectangle($fade, $fadeRectangle) } finally { $fade.Dispose() }
    Draw-Fill $cover.Graphics ([System.Drawing.Color]::FromArgb(225, $ink)) 0 855 1024 169
    Draw-Line $cover.Graphics $gold 2 64 64 960 64
    Draw-Text $cover.Graphics 'DUAN YU  /  CHARACTER MOD' 23 $gold 68 88 890 44
    Draw-Text $cover.Graphics '大理世子' 42 $muted 65 153 700 66 -Serif
    Draw-Text $cover.Graphics '段 誉' 146 $paper 55 214 850 190 -Serif
    Draw-Text $cover.Graphics '北冥积气 · 凌波游身 · 六脉破局' 31 $paper 68 421 890 55
    Draw-Fill $cover.Graphics ([System.Drawing.ColorTranslator]::FromHtml('#943F38')) 832 177 115 115
    Draw-Text $cover.Graphics '六脉' 40 $paper 845 196 100 80 -Serif
    Draw-Text $cover.Graphics '86 种卡牌    8 件遗物    2 个事件' 30 $paper 67 882 900 57
    Draw-Text $cover.Graphics "v$($design.version)  /  public-beta  /  cqbzly" 21 $muted 69 949 900 40
    $cover.Bitmap.Save((Join-Path $outputDirectory 'workshop-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $cover.Graphics.Dispose(); $cover.Bitmap.Dispose() }

$gallery = New-Canvas 1576 1810
try {
    Draw-Text $gallery.Graphics '大理世子 · 段誉' 52 $paper 62 42 1400 80 -Serif
    Draw-Text $gallery.Graphics '北冥 / 凌波 / 六脉   —   卡面精选' 25 $gold 66 130 1400 50
    Draw-Line $gallery.Graphics $gold 1 65 196 1511 196
    $featured = @('DY_R01', 'DY_R02', 'DY_R03', 'DY_T01', 'DY_T03', 'DY_T06')
    for ($index = 0; $index -lt $featured.Count; $index++) {
        $card = $cardsById[$featured[$index]]
        $left = 64 + ($index % 3) * 496
        $top = 240 + [Math]::Floor($index / 3) * 700
        $accent = switch ($index % 3) { 0 { [System.Drawing.ColorTranslator]::FromHtml('#70B99F') } 1 { [System.Drawing.ColorTranslator]::FromHtml('#80BBD0') } 2 { $gold } }
        Draw-Fill $gallery.Graphics $accent $left $top 456 650
        Draw-Fill $gallery.Graphics $paper ($left + 3) ($top + 3) 450 644
        Draw-Fill $gallery.Graphics $ink ($left + 3) ($top + 3) 450 79
        Draw-Text $gallery.Graphics $card.name 31 $paper ($left + 72) ($top + 20) 365 57 -Serif
        $costBrush = New-Object System.Drawing.SolidBrush($accent)
        try { $gallery.Graphics.FillEllipse($costBrush, ($left + 16), ($top + 14), 48, 48) } finally { $costBrush.Dispose() }
        Draw-Text $gallery.Graphics ([string]$card.cost) 29 $ink ($left + 28) ($top + 15) 42 50
        Draw-Image $gallery.Graphics $card.id ($left + 4) ($top + 82) 448 340
        Draw-Fill $gallery.Graphics $ink ($left + 4) ($top + 422) 448 42
        Draw-Text $gallery.Graphics "$($card.rarity) · $($card.type)" 20 $accent ($left + 22) ($top + 426) 414 37
        Draw-Text $gallery.Graphics $card.text 25 $ink ($left + 24) ($top + 481) 408 154
    }
    Draw-Line $gallery.Graphics $gold 1 65 1683 1511 1683
    Draw-Text $gallery.Graphics '原创程序化插画 · 全 86 种卡牌均有独立构图参数' 23 $paper 66 1710 1430 45
    Draw-Text $gallery.Graphics '画册排版示意；游戏内费用、名称与说明使用原生卡牌界面。' 20 $muted 66 1753 1430 40
    $gallery.Bitmap.Save((Join-Path $outputDirectory 'card-gallery.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $gallery.Graphics.Dispose(); $gallery.Bitmap.Dispose() }

$coverInfo = Get-Item -LiteralPath (Join-Path $outputDirectory 'workshop-preview.png')
if ($coverInfo.Length -ge 1000000) { throw 'Workshop cover exceeds 1 MB.' }
Write-Output "Created assets/workshop-preview.png ($($coverInfo.Length) bytes) and assets/card-gallery.png."
