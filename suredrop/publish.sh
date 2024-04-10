dotnet publish suredrop.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# zip the release into an archive
rm -f /Users/rob/Documents/src/publickeyserver/suredrop/publish/suredrop-linux-x64.zip
cd /Users/rob/Documents/src/publickeyserver/suredrop/bin/Release/net8.0/linux-x64
zip -r /Users/rob/Documents/src/publickeyserver/suredrop/publish/suredrop-linux-x64.zip publish

# cp to suredrop.org
scp -i "/Volumes/Rob's Private Documents/private/sdrop-sydney.pem"  /Users/rob/Documents/src/publickeyserver/suredrop/publish/suredrop-linux-x64.zip ubuntu@ec2-3-25-92-141.ap-southeast-2.compute.amazonaws.com:~

# cp to publickeyserver.org
scp -i "/Volumes/Rob's Private Documents/private/sdrop-sydney.pem"  /Users/rob/Documents/src/publickeyserver/suredrop/publish/suredrop-linux-x64.zip ubuntu@ec2-3-27-231-6.ap-southeast-2.compute.amazonaws.com:~
