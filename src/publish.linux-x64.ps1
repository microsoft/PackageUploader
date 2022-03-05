param ([Version] $Version='1.0', [String] $Output='./PackageUploader.Application/bin/release/net6.0/linux-x64/publish/', [Boolean] $Zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release --self-contained -o $Output -p:Version=$Version -r linux-x64
if ($LASTEXITCODE -eq 0 -and $Zip) {
    Compress-Archive -Force -LiteralPath $Output/PackageUploader -DestinationPath $Output/PackageUploader.$Version.linux-x64.zip
}
exit $LASTEXITCODE;
