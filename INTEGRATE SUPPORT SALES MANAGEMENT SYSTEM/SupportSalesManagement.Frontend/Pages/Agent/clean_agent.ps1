$f = "c:\Users\mukim\source\repos\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM\INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM\SupportSalesManagement.Frontend\Pages\Agent\AgentDashboard.razor"
$lines = Get-Content $f -Encoding UTF8
Write-Host ("Total lines before: " + $lines.Count)

# Find the old duplicate div start (old top-level space-y-6 div)
$oldStart = ($lines | Select-String 'space-y-6 p-6 pb-20' | Select-Object -First 1).LineNumber
# Find @code { line
$codeStart = ($lines | Select-String '^@code \{' | Select-Object -First 1).LineNumber
Write-Host ("Old duplicate HTML starts at: " + $oldStart)
Write-Host ("@code starts at: " + $codeStart)

# Keep lines before oldStart and from codeStart onwards
$kept = ($lines[0..($oldStart - 2)]) + ($lines[($codeStart - 1)..($lines.Count - 1)])
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($f, $kept, $utf8NoBom)
Write-Host ("Total lines after: " + $kept.Count)
Write-Host "Done!"
