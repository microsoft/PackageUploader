param ([version] [Parameter(Mandatory)] $Version, [String] $Output='./PackageUploader.Application/bin/release/net6.0/win-x64/publish/', [Boolean] $Zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release -r win-x64 -o $Output --self-contained /p:Version=$Version
if ($LASTEXITCODE -eq 0 -and $Zip) {
    Compress-Archive -Force -LiteralPath $Output/PackageUploader.exe -DestinationPath $Output/PackageUploader.$Version.win-x64.zip
}
exit $LASTEXITCODE;