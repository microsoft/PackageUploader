param ([version] [Parameter(Mandatory)] $Version, [String] $Output='./PackageUploader.ClientApi/bin/release/')
dotnet pack ./PackageUploader.ClientApi/PackageUploader.ClientApi.csproj -c release --include-source --include-symbols -o $Output -p:Version=$Version
exit $LASTEXITCODE;