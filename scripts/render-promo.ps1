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
    Draw-Text $gallery.Graphics "原创程序化插画 · 全 $($design.cards.Count) 种卡牌均有独立构图参数" 23 $paper 66 1710 1430 45
    Draw-Text $gallery.Graphics '画册排版示意；游戏内费用、名称与说明使用原生卡牌界面。' 20 $muted 66 1753 1430 40
    $gallery.Bitmap.Save((Join-Path $outputDirectory 'card-gallery.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $gallery.Graphics.Dispose(); $gallery.Bitmap.Dispose() }

Write-Output 'Created assets/card-gallery.png.'
