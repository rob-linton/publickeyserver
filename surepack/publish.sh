dotnet publish -c Release -p:PublishSingleFile=true -r osx-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r osx-arm64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r linux-x64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -p:PublishSingleFile=true -r linux-arm64 --self-contained -p:IncludeNativeLibrariesForSelfExtract=true

cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/linux-x64/publish/surepack ./publish/linux-x64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/linux-arm64/publish/surepack ./publish/linux-arm64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/osx-x64/publish/surepack ./publish/osx-x64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/osx-arm64/publish/surepack ./publish/osx-arm64/surepack
cp /Users/rob/Documents/src/publickeyserver/surepack/bin/Release/net8.0/win-x64/publish/surepack.exe ./publish/win-x64/surepack.exe
