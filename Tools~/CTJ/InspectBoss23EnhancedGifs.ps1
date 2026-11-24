Add-Type -AssemblyName System.Drawing

$referenceFiles = @('강화 공중 전환', '강화 연사', '강화 추적', '강화 폭격')
$outputFolder = Join-Path $PSScriptRoot 'Boss23EnhancedGifReference'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null

foreach ($referenceName in $referenceFiles) {
    $inputFile = Join-Path 'C:\Users\USER\Pictures' ($referenceName + '.gif')
    $source = [System.Drawing.Image]::FromFile($inputFile)
    try {
        $dimension = [System.Drawing.Imaging.FrameDimension]::Time
        $count = $source.GetFrameCount($dimension)
        $delays = $source.GetPropertyItem(0x5100).Value
        $frameTimes = [double[]]::new($count)
        $elapsed = 0.0
        for ($i = 0; $i -lt $count; $i++) {
            $frameTimes[$i] = $elapsed
            $elapsed += [Math]::Max(0.01, [BitConverter]::ToInt32($delays, $i * 4) / 100.0)
        }

        $sheet = [System.Drawing.Bitmap]::new(1600, 1252)
        $drawing = [System.Drawing.Graphics]::FromImage($sheet)
        $font = [System.Drawing.Font]::new('Arial', 12)
        try {
            $drawing.Clear([System.Drawing.Color]::Black)
            for ($sample = 0; $sample -lt 16; $sample++) {
                $wanted = $sample * ($elapsed - 0.01) / 15.0
                $frame = 0
                while ($frame + 1 -lt $count -and $frameTimes[$frame + 1] -le $wanted) { $frame++ }
                [void]$source.SelectActiveFrame($dimension, $frame)
                $x = ($sample % 4) * 400
                $y = [int][Math]::Floor($sample / 4) * 313
                $drawing.DrawImage($source, $x, $y + 24, 400, 189)
                $label = ('t={0:F2}s frame={1}' -f $frameTimes[$frame], $frame)
                $drawing.DrawString($label, $font, [System.Drawing.Brushes]::White, $x + 4, $y + 2)
                # 아래에는 원본의 보스·플레이어가 있는 부분을 더 크게 보여줍니다.
                $drawing.DrawImage($source, [System.Drawing.Rectangle]::new($x, $y + 217, 400, 92),
                    [System.Drawing.Rectangle]::new(0, 260, $source.Width, 270), [System.Drawing.GraphicsUnit]::Pixel)
            }
            $sheet.Save((Join-Path $outputFolder ($referenceName + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $font.Dispose(); $drawing.Dispose(); $sheet.Dispose() }
        [pscustomobject]@{ Name = $referenceName; Frames = $count; Seconds = [Math]::Round($elapsed, 3); Width = $source.Width; Height = $source.Height }
    }
    finally { $source.Dispose() }
}

