# Script to build each configuration of UnityExplorer.csproj

# Change to the directory containing the .csproj file
Push-Location D:\UnityExplorer\src

# Define the list of configurations to build
$configurations = @(
    "BIE_Cpp_CoreCLR",
    "BIE5_Mono",
    "BIE6_Mono",
    "ML_Cpp_net6",
    "ML_Cpp_net6_interop",
    "ML_Mono",
    "STANDALONE_Mono"
)

# Iterate through each configuration and build
foreach ($config in $configurations) {
    Write-Host "============================================="
    Write-Host "Building configuration: $($config)"
    Write-Host "============================================="

    # Run the build command
    dotnet build UnityExplorer.csproj -c $($config)

    # Check the exit code of the last command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "---------------------------------------------"
        Write-Host "Build FAILED for configuration: $($config)" -ForegroundColor Red
        Write-Host "---------------------------------------------"
        # Optional: uncomment the line below to stop on the first failure
        # break
    } else {
        Write-Host "---------------------------------------------"
        Write-Host "Build SUCCESSFUL for configuration: $($config)" -ForegroundColor Green
        Write-Host "---------------------------------------------"
    }

    Write-Host "" # Add a blank line for readability
}

# Return to the original directory
Pop-Location

Write-Host "Finished building all configurations." 