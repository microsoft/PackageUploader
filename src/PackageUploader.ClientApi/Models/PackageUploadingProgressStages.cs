namespace PackageUploader.ClientApi.Models;

public enum PackageUploadingProgressStages
{
    NotStarted,
    ComputingDeltas,
    UploadingPackage,
    ProcessingPackage,
    UploadingSupplementalFiles,
    Done
}