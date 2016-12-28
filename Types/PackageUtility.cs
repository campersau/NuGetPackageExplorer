using System;
using System.IO;

namespace NuGetPackageExplorer.Types
{
    internal static class PackageUtility
    {
        internal static bool IsManifest(string path)
        {
            return Path.GetExtension(path).Equals(NuGet.Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
