param ($output='./PackageUploader.Application/bin/release/net6.0/win-x64/publish/', $zip=$false)
dotnet publish ./PackageUploader.Application/PackageUploader.Application.csproj -c release -r win-x64 -o $output --self-contained
if ($LASTEXITCODE -eq 0 -and $zip) {
    Compress-Archive -Force -LiteralPath $output/PackageUploader.exe -DestinationPath $output/PackageUploader.win-x64.zip
}
exit $LASTEXITCODE;