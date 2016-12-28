using System;
using System.ComponentModel.Composition;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;
using NuGet.Protocol.Core.Types;
using NuGet.Packaging.Core;

namespace PackageExplorer
{
    [Export(typeof(IPackageChooser))]
    internal class PackageChooserService : IPackageChooser
    {
        // for select package dialog
        private PackageChooserDialog _dialog;
        private PackageChooserViewModel _viewModel;

        // for select plugin dialog
        private PackageChooserDialog _pluginDialog;
        private PackageChooserViewModel _pluginViewModel;

        [Import]
        public IPackageViewModelFactory ViewModelFactory { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public IPackageDownloader PackageDownloader { get; set; }

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        #region IPackageChooser Members

        public SourceRepository Repository { get { return _viewModel.ActiveRepository; } }

        public IPackageSearchMetadata SelectPackage(string searchTerm)
        {
            if (_dialog == null)
            {
                _viewModel = ViewModelFactory.CreatePackageChooserViewModel(null);
                _viewModel.PackageDownloadRequested += OnPackageDownloadRequested;
                _dialog = new PackageChooserDialog(_viewModel)
                         {
                             Owner = Window.Value
                         };
            }

            _dialog.ShowDialog(searchTerm);
            return _viewModel.SelectedPackage;
        }

        private async void OnPackageDownloadRequested(object sender, EventArgs e)
        {
            var repository = _viewModel.ActiveRepository;
            var packageInfo = _viewModel.SelectedPackage;
            if (packageInfo != null)
            {
                string selectedFilePath;
                int selectedIndex;

                string packageName = packageInfo.Identity.Id + "." + packageInfo.Identity.Version.ToString() + PackagingCoreConstants.NupkgExtension;
                string title = "Save " + packageName;
                const string filter = "NuGet package file (*.nupkg)|*.nupkg|All files (*.*)|*.*";

                bool accepted = UIServices.OpenSaveFileDialog(
                    title,
                    packageName,
                    null,
                    filter,
                    overwritePrompt: true,
                    selectedFilePath: out selectedFilePath,
                    selectedFilterIndex: out selectedIndex);

                if (accepted)
                {
                    if (selectedIndex == 1 &&
                        !selectedFilePath.EndsWith(PackagingCoreConstants.NupkgExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedFilePath += PackagingCoreConstants.NupkgExtension;
                    }

                    var downloadResource = await repository.GetResourceAsync<DownloadResource>();

                    await PackageDownloader.Download(selectedFilePath, downloadResource, packageInfo.Identity);
                }
            }
        }

        public SourceRepository PluginRepository { get { return _pluginViewModel.ActiveRepository; } }

        public IPackageSearchMetadata SelectPluginPackage()
        {
            if (_pluginDialog == null)
            {
                _pluginViewModel = ViewModelFactory.CreatePackageChooserViewModel(NuGetConstants.PluginFeedUrl);
                _pluginDialog = new PackageChooserDialog(_pluginViewModel)
                                {
                                    Owner = Window.Value
                                };
            }

            _pluginDialog.ShowDialog();
            return _pluginViewModel.SelectedPackage;
        }

        public void Dispose()
        {
            if (_dialog != null)
            {
                _dialog.ForceClose();
                _viewModel.Dispose();
            }
        }

        #endregion
    }
}