Add-Type -AssemblyName System.Drawing
$root = 'D:\workspace\psychic-bassoon'
$manifest = Get-Content -LiteralPath "$root\output\creature-sprites\manifest.json" -Raw | ConvertFrom-Json
$dest = "$root\Assets\Textures\CreatureConcepts"
[IO.Directory]::CreateDirectory($dest) | Out-Null
$preview = [Drawing.Bitmap]::new(640, 320)
$pg = [Drawing.Graphics]::FromImage($preview)
$pg.Clear([Drawing.Color]::FromArgb(255, 43, 47, 57))
$pg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
$font = [Drawing.Font]::new('Arial', 11)
$template = Get-Content -LiteralPath "$root\Assets\Textures\hampterian.png.meta" -Raw
$index = 0
foreach ($entry in $manifest) {
    $src = [Drawing.Bitmap]::new($entry.path)
    $left = $src.Width; $top = $src.Height; $right = 0; $bottom = 0
    for ($y = 0; $y -lt $src.Height; $y++) {
        for ($x = 0; $x -lt $src.Width; $x++) {
            if ($src.GetPixel($x,$y).A -ge 128) {
                $left = [Math]::Min($left,$x); $right = [Math]::Max($right,$x)
                $top = [Math]::Min($top,$y); $bottom = [Math]::Max($bottom,$y)
            }
        }
    }
    $sw = $right-$left+1; $sh = $bottom-$top+1
    $scale = $entry.size / [Math]::Max($sw,$sh)
    $dw = [Math]::Max(1,[int][Math]::Round($sw*$scale)); $dh = [Math]::Max(1,[int][Math]::Round($sh*$scale))
    $sprite = [Drawing.Bitmap]::new(32,32)
    $dx = [int][Math]::Floor((32-$dw)/2); $dy = [int][Math]::Floor((32-$dh)/2)
    for ($y=0; $y -lt $dh; $y++) {
        for ($x=0; $x -lt $dw; $x++) {
            $sx = $left+[Math]::Min($sw-1,[int][Math]::Floor(($x+0.5)*$sw/$dw))
            $sy = $top+[Math]::Min($sh-1,[int][Math]::Floor(($y+0.5)*$sh/$dh))
            $color = $src.GetPixel($sx,$sy)
            if ($color.A -ge 128) { $sprite.SetPixel($dx+$x,$dy+$y,[Drawing.Color]::FromArgb(255,$color.R,$color.G,$color.B)) }
        }
    }
    $path = "$dest\$($entry.name).png"
    $sprite.Save($path,[Drawing.Imaging.ImageFormat]::Png)
    if (!(Test-Path -LiteralPath "$path.meta")) {
        $meta = $template -replace '(?m)^guid: .*',("guid: " + [Guid]::NewGuid().ToString('N'))
        $meta = $meta -replace '(?m)^    spriteID: .*',("    spriteID: " + [Guid]::NewGuid().ToString('N'))
        [IO.File]::WriteAllText("$path.meta",$meta)
    }
    $px = ($index % 5)*128; $py = [int][Math]::Floor($index/5)*160
    $pg.DrawImage($sprite,[Drawing.Rectangle]::new($px, $py, 128,128),0,0,32,32,[Drawing.GraphicsUnit]::Pixel)
    $pg.DrawString($entry.name,$font,[Drawing.Brushes]::White,$px+8,$py+133)
    Write-Output "$($entry.name): 32x32 RGBA, occupied $($dw)x$($dh)"
    $sprite.Dispose(); $src.Dispose(); $index++
}
$preview.Save("$root\output\creature-sprites\preview.png",[Drawing.Imaging.ImageFormat]::Png)
$font.Dispose(); $pg.Dispose(); $preview.Dispose()
