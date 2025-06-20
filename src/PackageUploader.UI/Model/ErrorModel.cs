

namespace PackageUploader.UI.Model;

public class ErrorModel
{
    public string MainMessage { get; set; } = string.Empty;
    public string DetailMessage { get; set; } = string.Empty;
    public Type? OriginPage { get; set; } = null;
    public string LogsPath { get; set; } = string.Empty;
}
