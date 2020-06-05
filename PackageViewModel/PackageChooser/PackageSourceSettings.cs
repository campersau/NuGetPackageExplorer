using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal class PackageSourceSettings : ISourceSettings
    {
        private readonly ISettingsManager _settingsManager;

        public PackageSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;
        }

        public IList<NuGet.Configuration.PackageSource> GetSources()
        {
            var sources = _settingsManager.GetPackageSources();

            // migrate nuget v1 feed to v2 feed
            for (var i = 0; i < sources.Count; i++)
            {
                MigrateOfficialNuGetSource(sources[i]);
            }

            return sources;
        }

        public void SetSources(IEnumerable<string> sources)
        {
            _settingsManager.SetPackageSources(sources);
        }

        public NuGet.Configuration.PackageSource DefaultSource => NuGetConstants.DefaultFeedPackageSource;

        public NuGet.Configuration.PackageSource ActiveSource
        {
            get
            {
                var packageSource = _settingsManager.ActivePackageSource;
                MigrateOfficialNuGetSource(packageSource);
                return packageSource;
            }
            set { _settingsManager.ActivePackageSource = value; }
        }

        private static void MigrateOfficialNuGetSource(NuGet.Configuration.PackageSource source)
        {
            if (NuGetConstants.V2LegacyFeedUrl.Equals(source.Source, StringComparison.OrdinalIgnoreCase) ||
                NuGetConstants.V2FeedUrl.Equals(source.Source, StringComparison.OrdinalIgnoreCase))
            {
                source.Source = NuGetConstants.DefaultFeedUrl;
            }
        }
    }
}
