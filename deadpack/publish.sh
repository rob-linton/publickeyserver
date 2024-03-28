dotnet publish -c Release -p:PublishSingleFile=true -r osx-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r linux-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true

cp /Users/rob/Documents/src/publickeyserver/deadpack/bin/Release/net8.0/linux-x64/publish/deadpack ./publish/linux-x64/deadpack
cp /Users/rob/Documents/src/publickeyserver/deadpack/bin/Release/net8.0/osx-x64/publish/deadpack ./publish/osx-x64/deadpack
cp /Users/rob/Documents/src/publickeyserver/deadpack/bin/Release/net8.0/win-x64/publish/deadpack.exe ./publish/win-x64/deadpack.exe
