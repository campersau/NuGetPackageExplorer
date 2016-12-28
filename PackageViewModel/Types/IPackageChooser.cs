using System;
using NuGet.Protocol.Core.Types;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageChooser : IDisposable
    {
        SourceRepository Repository { get; }
        IPackageSearchMetadata SelectPackage(string searchTerm);
        SourceRepository PluginRepository { get; }
        IPackageSearchMetadata SelectPluginPackage();
    }
}