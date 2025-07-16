#!/bin/bash

# Check if version argument is provided
if [ -z "$1" ]; then
    echo "Usage: $0 [version]"
    echo "Version format: major[.minor[.build[.revision]]]"
    echo "Examples: 1.0, 1.0.0, 1.0.0.0"
    exit 1
fi

VER=$1

# Validate version format (major[.minor[.build[.revision]]])
if ! [[ "$VER" =~ ^[0-9]+(\.[0-9]+){0,3}$ ]]; then
    echo "Error: Invalid version format '$VER'"
    echo "Version must be in format: major[.minor[.build[.revision]]]"
    echo "Each part must be a number between 0 and 65535"
    echo "Examples:"
    echo "  1"
    echo "  1.0"
    echo "  1.0.0"
    echo "  1.0.0.0"
    exit 1
fi

# Additional validation: check that each part is <= 65535
IFS='.' read -ra VERSION_PARTS <<< "$VER"
for part in "${VERSION_PARTS[@]}"; do
    if [ "$part" -gt 65535 ]; then
        echo "Error: Version part '$part' exceeds maximum value of 65535"
        exit 1
    fi
done

cd surepack

# Array of platforms to build for
platforms=(
    # Windows
    "win-x64"          # Windows 64-bit (most common Windows desktop/server)
    "win-x86"          # Windows 32-bit (legacy systems, older hardware)
    "win-arm64"        # Windows on ARM (Surface Pro X, ARM-based laptops)
    
    # macOS
    "osx-x64"          # Intel-based Macs (pre-2020 MacBooks, iMacs)
    "osx-arm64"        # Apple Silicon Macs (M1/M2/M3 MacBooks, Mac Mini, iMac, Mac Studio)
    
    # Linux - Standard glibc
    "linux-x64"        # Linux 64-bit (Ubuntu, Debian, CentOS, RHEL - most servers/desktops)
    "linux-arm"        # Linux ARM 32-bit (Raspberry Pi 2/3, older ARM devices)
    "linux-arm64"      # Linux ARM 64-bit (Raspberry Pi 4, AWS Graviton, modern ARM servers)
    
    # Linux - musl libc (Alpine Linux, minimal containers)
    "linux-musl-x64"   # Alpine Linux 64-bit (Docker containers, minimal deployments)
    "linux-musl-arm"   # Alpine Linux ARM 32-bit (containerized IoT devices)
    "linux-musl-arm64" # Alpine Linux ARM 64-bit (containerized ARM servers)
    
)

# Build for each platform
for PLATFORM in "${platforms[@]}"; do
    echo "========================================="
    echo "Building version $VER for $PLATFORM..."
    echo "========================================="
    
    if dotnet publish -c Release \
        -p:PublishSingleFile=true \
        -r $PLATFORM \
        --self-contained \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:AssemblyVersion=$VER \
        -p:FileVersion=$VER \
        -p:InformationalVersion=$VER \
        -o ../Deploy/$VER/$PLATFORM; then
        echo "✓ Successfully built $PLATFORM (v$VER)"
        
        # Remove .pdb and .xml files from the output directory
        echo "Cleaning up .pdb and .xml files..."
        find ../Deploy/$VER/$PLATFORM -type f \( -name "*.pdb" -o -name "*.xml" \) -delete
        
    else
        echo "✗ Failed to build $PLATFORM (v$VER)"
        # Optionally exit on first failure
        # exit 1
    fi
    echo
done

echo "Build process completed for version $VER!"
echo "All builds are located in: ../Deploy/$VER/"

# Copy latest versions to the latest folder
echo "========================================="
echo "Copying latest versions to latest folder..."
echo "========================================="

# Create latest directory if it doesn't exist
mkdir -p ../Deploy/latest

# Copy each platform build to the latest folder
for PLATFORM in "${platforms[@]}"; do
    if [ -d "../Deploy/$VER/$PLATFORM" ]; then
        echo "Copying $PLATFORM to latest folder..."
        
        # Remove existing platform folder in latest if it exists
        rm -rf ../Deploy/latest/$PLATFORM
        
        # Copy the new version to latest
        cp -r ../Deploy/$VER/$PLATFORM ../Deploy/latest/
        
        echo "✓ $PLATFORM copied to latest"
    else
        echo "⚠ $PLATFORM build not found, skipping copy to latest"
    fi
done

echo
echo "Latest versions copied to: ../Deploy/latest/"
echo "Build and deployment process completed!"