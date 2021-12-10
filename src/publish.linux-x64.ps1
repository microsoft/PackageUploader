param ([version] [Parameter(Mandatory)] $Version, [String] $Output='./PackageUploader.Application/bin/release/net6.0/linux-x64/publish/', [Boolean] $Zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release -r linux-x64 -o $Output --self-contained -p:Version=$Version
if ($LASTEXITCODE -eq 0 -and $Zip) {
    Compress-Archive -Force -LiteralPath $Output/PackageUploader -DestinationPath $Output/PackageUploader.$Version.linux-x64.zip
}
exit $LASTEXITCODE;