param ($output='./PackageUploader.Application/bin/release/net6.0/linux-x64/publish/', $zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release -r linux-x64 -o $output --self-contained
if ($LASTEXITCODE -eq 0 -and $zip) {
    Compress-Archive -Force -LiteralPath $output/PackageUploader -DestinationPath $output/PackageUploader.linux-x64.zip
}
exit $LASTEXITCODE;