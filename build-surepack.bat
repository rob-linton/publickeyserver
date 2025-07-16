@echo off
if ["%1"]==[""] goto usage

set VER=%1

cd surepack

echo namespace suredrop { public class Publish { public static string Version = "%VER%";}} > publish.cs

REM *** win-x86 ******************************************************************************************

set PLATFORM=win-x64
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%

set PLATFORM=osx-x64
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%

set PLATFORM=linux-x64
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%

set PLATFORM=linux-musl-x64
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%

set PLATFORM=linux-arm
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%

set PLATFORM=linux-arm64
dotnet publish -c Release -p:PublishSingleFile=true -r %PLATFORM% --self-contained -p:IncludeNativeLibrariesForSelfExtract=true -o ../Deploy/%PLATFORM%





goto :eof

:usage
@echo Usage: %0 [version] 


:eof