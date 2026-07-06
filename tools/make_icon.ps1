Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param([float]$x, [float]$y, [float]$w, [float]$h, [float]$r)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$size)

    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $bgPath = New-RoundedRectPath 0 0 $size $size ($size * 0.22)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0,0)),
        (New-Object System.Drawing.Point($size,$size)),
        [System.Drawing.Color]::FromArgb(255, 86, 96, 245),
        [System.Drawing.Color]::FromArgb(255, 124, 58, 237))
    $g.FillPath($bgBrush, $bgPath)

    $unit = $size / 6.0
    $gap = $unit * 0.16
    $r = $unit * 0.28

    $blocks = @(
        @{ Row=1; Col=1; Color=[System.Drawing.Color]::FromArgb(255,255,82,82) },
        @{ Row=1; Col=2; Color=[System.Drawing.Color]::FromArgb(255,255,214,0) },
        @{ Row=2; Col=2; Color=[System.Drawing.Color]::FromArgb(255,76,175,80) },
        @{ Row=3; Col=2; Color=[System.Drawing.Color]::FromArgb(255,41,121,255) },
        @{ Row=3; Col=3; Color=[System.Drawing.Color]::FromArgb(255,255,152,0) },
        @{ Row=3; Col=1; Color=[System.Drawing.Color]::FromArgb(255,0,188,168) }
    )

    foreach ($b in $blocks) {
        $x = $b.Col * $unit + $gap
        $y = $b.Row * $unit + $gap
        $w = $unit - ($gap * 2)
        $h = $unit - ($gap * 2)
        $path = New-RoundedRectPath $x $y $w $h $r
        $brush = New-Object System.Drawing.SolidBrush($b.Color)
        $g.FillPath($brush, $path)

        $hl = New-RoundedRectPath ($x + $w*0.12) ($y + $h*0.1) ($w*0.5) ($h*0.32) ($r*0.6)
        $hlBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70,255,255,255))
        $g.FillPath($hlBrush, $hl)
    }

    $g.Dispose()
    return $bmp
}

function Get-PngBytes {
    param([System.Drawing.Bitmap]$bmp)
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return ,$ms.ToArray()
}

$sizes = @(16,32,48,64,128,256)
$images = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $bytes = Get-PngBytes $bmp
    $images += [PSCustomObject]@{ Size = $s; Bytes = $bytes }
    $bmp.Dispose()
}

$outPath = Join-Path $PSScriptRoot "..\BlockBlast\Assets\icon.ico"
$fs = [System.IO.File]::Open($outPath, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$images.Count)

$offset = 6 + (16 * $images.Count)
foreach ($img in $images) {
    $sizeByte = if ($img.Size -ge 256) { 0 } else { $img.Size }
    $bw.Write([byte]$sizeByte)
    $bw.Write([byte]$sizeByte)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$img.Bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $img.Bytes.Length
}

foreach ($img in $images) {
    $bw.Write($img.Bytes)
}

$bw.Flush()
$bw.Close()
$fs.Close()

Write-Host "Icon written to $outPath"
