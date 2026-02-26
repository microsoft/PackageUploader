namespace PackageUploader.GDKStubExe;

/// <summary>
/// Marker type used to locate the GDKStubExe assembly at runtime
/// for embedded resource extraction.
/// </summary>
public static class GDKStubExeResources
{
    /// <summary>
    /// Returns the assembly containing the embedded GDKStubExe.zip resource.
    /// </summary>
    public static System.Reflection.Assembly Assembly => typeof(GDKStubExeResources).Assembly;
}
