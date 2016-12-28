using System.Threading.Tasks;
using NuGet;
using NuGet.Protocol.Core.Types;
using NuGet.Packaging.Core;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageDownloader
    {
        Task<IPackage> Download(DownloadResource downloadResource, PackageIdentity identity);
        Task Download(string targetFilePath, DownloadResource downloadResource, PackageIdentity identity);
    }
}