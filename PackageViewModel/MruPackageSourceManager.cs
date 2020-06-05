using System;
using System.Collections.ObjectModel;
using System.Linq;
using NuGet.Configuration;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public sealed class MruPackageSourceManager : IDisposable
    {
        private const int MaxItem = 5;
        private readonly ISourceSettings _sourceSettings;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public MruPackageSourceManager(ISourceSettings sourceSettings)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _sourceSettings = sourceSettings;
            LoadDataFromSettings();
        }

        public PackageSource ActivePackageSource { get; set; }

        public ObservableCollection<PackageSource> PackageSources { get; } = new ObservableCollection<PackageSource>();

        public void Dispose()
        {
            _sourceSettings.SetSources(PackageSources.Select(packageSource => packageSource.Source));
            _sourceSettings.ActiveSource = ActivePackageSource;
        }

        private void LoadDataFromSettings()
        {
            var savedFiles = _sourceSettings.GetSources();
            for (var i = savedFiles.Count - 1; i >= 0; --i)
            {
                var s = savedFiles[i];
                if (s != null)
                {
                    AddSource(s);
                }
            }

            if (!string.IsNullOrEmpty(_sourceSettings.ActiveSource.Source))
            {
                AddSource(_sourceSettings.ActiveSource);
                ActivePackageSource = _sourceSettings.ActiveSource;
            }

            // if there is no source (this happens after upgrading), add the default source to it
            var defaultFeed = _sourceSettings.DefaultSource;
            if (PackageSources.Count == 0 || !PackageSources.Any(packageSource => packageSource.Source == defaultFeed.Source))
            {
                PackageSources.Insert(0, defaultFeed);
            }

            if (string.IsNullOrEmpty(ActivePackageSource?.Source))
            {
                // assign the active package source to the first one if it's not already assigned
                ActivePackageSource = PackageSources[0];
            }
        }

        public void NotifyPackageSourceAdded(PackageSource newSource)
        {
            AddSource(newSource);
        }

        private void AddSource(PackageSource newSource)
        {
            if (newSource == null)
            {
                throw new ArgumentNullException(nameof(newSource));
            }

            var index = IndexOfPackage(newSource);
            if (index == -1)
            {
                PackageSources.Insert(0, newSource);
            }
            else
            {
                PackageSources[index] = newSource;
                PackageSources.Move(index, 0);
            }

            if (PackageSources.Count > MaxItem)
            {
                PackageSources.RemoveAt(PackageSources.Count - 1);
            }

            var defaultSourceIndex = IndexOfPackage(_sourceSettings.DefaultSource);
            if (defaultSourceIndex != -1)
            {
                PackageSources.Move(defaultSourceIndex, 0);
            }
        }

        private int IndexOfPackage(PackageSource item)
        {
            for (var i = 0; i < PackageSources.Count; i++)
            {
                if (PackageSources[i].Source.Equals(item.Source, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
