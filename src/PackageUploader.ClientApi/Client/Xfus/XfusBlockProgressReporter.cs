using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus;

internal class XfusBlockProgressReporter
{
    private readonly ILogger _logger;

    public int BlocksToUpload { get; }
    public int BlocksLeftToUpload { get; set; }
    public int PercentComplete { get; private set; } = -1;
    public long BytesUploaded { get; set; }
    public long TotalBlockBytes { get; }

    public XfusBlockProgressReporter(ILogger logger, int blocksToUpload, long totalBlockBytes)
    {
        _logger = logger;
        BlocksToUpload = blocksToUpload;
        BlocksLeftToUpload = blocksToUpload;
        TotalBlockBytes = totalBlockBytes;
    }

    public void ReportProgress()
    {
        var ratio = (float)BlocksLeftToUpload / (float)BlocksToUpload;
        var percentage = 100 - (int)Math.Round(100 * ratio);

        if (percentage > PercentComplete)
        {
            PercentComplete = percentage;
            _logger.LogInformation($"Upload {percentage}% complete.");
        }
    }
}
