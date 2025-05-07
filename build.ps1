#!/usr/bin/env pwsh
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [switch]$SkipCpp = $false,
    [switch]$SkipNet = $false,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$cppDir = Join-Path $rootDir "src\cpp"
$cppBuildDir = Join-Path $cppDir "build"
$netSln = Join-Path $rootDir "MinjaSharp.sln"

# Ensure we have CMake installed
function Check-Cmake {
    try {
        $cmakeVersion = cmake --version
        Write-Host "Found CMake: $cmakeVersion"
    }
    catch {
        Write-Error "CMake is required to build the C++ shim but was not found. Please install CMake and add it to your PATH."
        exit 1
    }
}

# Ensure we have .NET SDK installed
function Check-DotNet {
    try {
        $dotnetVersion = dotnet --version
        Write-Host "Found .NET SDK: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK is required but was not found. Please install .NET SDK and add it to your PATH."
        exit 1
    }
}

# Build the C++ shim
function Build-CppShim {
    Write-Host "Building C++ shim ($Configuration)..."
    
    # Create and enter build directory
    if (-not (Test-Path $cppBuildDir)) {
        New-Item -ItemType Directory -Path $cppBuildDir | Out-Null
    }
    Push-Location $cppBuildDir
    
    try {
        # Configure CMake
        cmake -DCMAKE_BUILD_TYPE=$Configuration ..
        
        # Build with CMake
        cmake --build . --config $Configuration
        
        if (-not $?) {
            Write-Error "Failed to build C++ shim"
            exit 1
        }
        
        Write-Host "C++ shim built successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

# Build .NET projects
function Build-DotNet {
    Write-Host "Building .NET projects ($Configuration)..."
    
    # Restore packages
    dotnet restore $netSln
    
    # Build solution
    dotnet build $netSln --configuration $Configuration --no-restore
    
    if (-not $?) {
        Write-Error "Failed to build .NET projects"
        exit 1
    }
    
    Write-Host ".NET projects built successfully" -ForegroundColor Green
}

# Run tests
function Run-Tests {
    Write-Host "Running tests..."
    
    dotnet test $netSln --configuration $Configuration --no-build
    
    if (-not $?) {
        Write-Error "Tests failed"
        exit 1
    }
    
    Write-Host "Tests passed successfully" -ForegroundColor Green
}

# Main build process
Write-Host "Starting MinjaSharp build process..."

# Check prerequisites
if (-not $SkipCpp) {
    Check-Cmake
}

if (-not $SkipNet) {
    Check-DotNet
}

# Build C++ shim
if (-not $SkipCpp) {
    Build-CppShim
}

# Build .NET projects
if (-not $SkipNet) {
    Build-DotNet
}

# Run tests
if (-not $SkipTests) {
    Run-Tests
}

Write-Host "MinjaSharp build completed successfully!" -ForegroundColor Green
