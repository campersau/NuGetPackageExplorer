using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal class PublishSourceSettings : ISourceSettings
    {
        private readonly ISettingsManager _settingsManager;

        public PublishSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;
        }

        #region ISourceSettings Members

        public IList<NuGet.Configuration.PackageSource> GetSources()
        {
            var sources = _settingsManager.GetPublishSources();
            for (var i = 0; i < sources.Count; i++)
            {
                MigrateOfficialNuGetSource(sources[i]);
            }

            return sources;
        }

        public void SetSources(IEnumerable<string> sources)
        {
            _settingsManager.SetPublishSources(sources);
        }

        public NuGet.Configuration.PackageSource DefaultSource => NuGetConstants.NuGetPublishFeedPackageSource;

        public NuGet.Configuration.PackageSource ActiveSource
        {
            get
            {
                var packageSource = _settingsManager.ActivePublishSource;
                MigrateOfficialNuGetSource(packageSource);
                return packageSource;
            }
            set { _settingsManager.ActivePublishSource = value; }
        }

        #endregion

        private static void MigrateOfficialNuGetSource(NuGet.Configuration.PackageSource source)
        {
            if (NuGetConstants.V2LegacyNuGetPublishFeed.Equals(source.Source, StringComparison.OrdinalIgnoreCase))
            {
                source.Source = NuGetConstants.NuGetPublishFeed;
            }
        }
    }
}
