# build and publish the surepack tool for all platforms
dotnet publish -c Release -p:PublishSingleFile=true -r osx-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r osx-arm64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true

dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r win-x86 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r win-arm64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true

dotnet publish -c Release -p:PublishSingleFile=true -r linux-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r linux-arm64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r linux-arm --self-contained -p:IncludeNativeLibrariesForSelfExtract=true

# copy the files to the publish directory
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/osx-x64/publish/surepack ./publish/osx-x64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/osx-arm64/publish/surepack ./publish/osx-arm64/surepack

cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/win-x64/publish/surepack.exe ./publish/win-x64/surepack.exe
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/win-x86/publish/surepack.exe ./publish/win-x86/surepack.exe
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/win-arm64/publish/surepack.exe ./publish/win-arm64/surepack.exe

cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/linux-x64/publish/surepack ./publish/linux-x64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/linux-arm64/publish/surepack ./publish/linux-arm64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/linux-arm/publish/surepack ./publish/linux-arm/surepack



