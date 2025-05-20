# Script to verify project dependencies against approved list
param(
    [string]$ApprovedDepsPath = ".github/approved-dependencies.csv",
    [switch]$StrictMode = $true
)

function Get-ProjectDependencies {
    $csprojFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse
    $dependencies = @{}
    
    foreach ($file in $csprojFiles) {
        $content = Get-Content $file.FullName
        $packageRefs = $content | Select-String -Pattern "<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""" -AllMatches
        
        foreach ($match in $packageRefs.Matches) {
            $packageName = $match.Groups[1].Value
            $version = $match.Groups[2].Value
            
            if (-not $dependencies.ContainsKey($packageName)) {
                $dependencies[$packageName] = $version
            }
        }
    }
    
    return $dependencies
}

function Get-ApprovedDependencies {
    if (-not (Test-Path $ApprovedDepsPath)) {
        Write-Error "Approved dependencies file not found at: $ApprovedDepsPath"
        exit 1
    }
    
    $approvedDeps = @{}
    $lines = Get-Content $ApprovedDepsPath | Where-Object { -not $_.StartsWith('#') -and $_.Trim() -ne '' }
    
    foreach ($line in $lines) {
        $parts = $line -split ','
        if ($parts.Count -ge 2) {
            $packageName = $parts[0].Trim()
            $approvedDeps[$packageName] = @{
                MinVersion = $parts[1].Trim()
                MaxVersion = if ($parts.Count -ge 3) { $parts[2].Trim() } else { "" }
                Justification = if ($parts.Count -ge 4) { $parts[3].Trim() } else { "" }
            }
        }
    }
    
    return $approvedDeps
}

# Get current and approved dependencies
$currentDeps = Get-ProjectDependencies
$approvedDeps = Get-ApprovedDependencies

# Check for unapproved dependencies
$unapprovedDeps = @{}
foreach ($dep in $currentDeps.Keys) {
    if (-not $approvedDeps.ContainsKey($dep)) {
        $unapprovedDeps[$dep] = $currentDeps[$dep]
    }
}

# Report findings
if ($unapprovedDeps.Count -gt 0) {
    Write-Host "UNAPPROVED DEPENDENCIES FOUND:" -ForegroundColor Red
    foreach ($dep in $unapprovedDeps.Keys) {
        Write-Host "  $dep : $($unapprovedDeps[$dep])" -ForegroundColor Red
    }
    
    if ($StrictMode) {
        Write-Host "Dependency check failed. Remove unapproved dependencies or update the approved list." -ForegroundColor Red
        exit 1
    } else {
        Write-Host "Warning: Unapproved dependencies detected." -ForegroundColor Yellow
    }
} else {
    Write-Host "All dependencies are approved." -ForegroundColor Green
}

# Optional: Check versions against constraints
# This could be expanded to verify semantic versioning constraints
