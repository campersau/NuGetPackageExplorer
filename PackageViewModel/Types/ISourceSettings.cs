using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NuGet.Configuration;

namespace NuGetPackageExplorer.Types
{
    public interface ISourceSettings
    {
        PackageSource DefaultSource { get; }
        PackageSource ActiveSource { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<PackageSource> GetSources();

        void SetSources(IEnumerable<string> sources);
    }
}
