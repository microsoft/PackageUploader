param ([String] $Version='1.0', [String] $Output='./PackageUploader.Application/bin/release/net6.0/publish/', [Boolean] $Zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release --self-contained -o $Output -p:Version=$Version -r linux-x64
if ($LASTEXITCODE -eq 0 -and $Zip) {
    Compress-Archive -Force -LiteralPath $Output/PackageUploader -DestinationPath $Output/PackageUploader.$Version.linux-x64.zip
}
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release --self-contained -o $Output -p:Version=$Version -r win-x64
if ($LASTEXITCODE -eq 0 -and $Zip) {
    Compress-Archive -Force -LiteralPath $Output/PackageUploader.exe -DestinationPath $Output/PackageUploader.$Version.win-x64.zip
}
exit $LASTEXITCODE;
