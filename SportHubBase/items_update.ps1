$path = "c:\Users\DeepSeek\source\repos\SniperTormenta\SportHub\SportHubBase\TournamentWindow.xaml"
$lines = Get-Content $path
$count = $lines.Count
# Keep header part (lines 1..443, index 0..442)
$part1 = $lines[0..442]
# Content to replace Table + Legend
$part2 = '                    <!-- Polymorphic Results Content -->'
$part3 = '                    <ContentControl Grid.Row="1" Content="{Binding CurrentResults}" />'
# Keep footer part (lines 855..End, index 854..End)
$part4 = $lines[854..($count-1)]

$newContent = $part1 + $part2 + $part3 + $part4
$newContent | Set-Content $path -Encoding UTF8
Write-Host "Update done. Lines before: $count. Lines after: $($newContent.Count)"
