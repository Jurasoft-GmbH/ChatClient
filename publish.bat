if exist CodeAnalyzeClient.zip del CodeAnalyzeClient.zip
dotnet publish -c Release -p:PublishProfile=Portable
dotnet publish -c Release -p:PublishProfile=LinuxArm64
dotnet publish -c Release -p:PublishProfile=LinuxX64
dotnet publish -c Release -p:PublishProfile=MacArm64
dotnet publish -c Release -p:PublishProfile=MacX64
dotnet publish -c Release -p:PublishProfile=WinArm64
dotnet publish -c Release -p:PublishProfile=WinX64
powershell -Command "Compress-Archive -Path .\bin\publish\* -DestinationPath CodeAnalyzeClient.zip"
