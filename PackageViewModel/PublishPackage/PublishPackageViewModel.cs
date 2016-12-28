using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NuGet;
using NuGetPackageExplorer.Types;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    public sealed class PublishPackageViewModel : ViewModelBase, IObserver<int>, IDisposable
    {
        private readonly MruPackageSourceManager _mruSourceManager;
        private readonly IPackageMetadata _package;
        private readonly string _packageFilePath;
        private readonly ISettingsManager _settingsManager;
        private bool _canPublish = true;
        private bool _hasError;
        private string _publishKey;
        private bool? _publishAsUnlisted = true;
        private string _selectedPublishItem;
        private bool _showProgress;
        private string _status;
        private bool _suppressReadingApiKey;
        private SourceRepository _repository;

        public PublishPackageViewModel(
            MruPackageSourceManager mruSourceManager,
            ISettingsManager settingsManager,
            PackageViewModel viewModel)
        {
            _mruSourceManager = mruSourceManager;
            _settingsManager = settingsManager;
            _package = viewModel.PackageMetadata;
            _packageFilePath = viewModel.GetCurrentPackageTempFile();
            SelectedPublishItem = _mruSourceManager.ActivePackageSource;
            PublishAsUnlisted = _settingsManager.PublishAsUnlisted;
        }

        public string PublishKey
        {
            get { return _publishKey; }
            set
            {
                if (_publishKey != value)
                {
                    _publishKey = value;
                    OnPropertyChanged();
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string PublishUrl
        {
            get { return _mruSourceManager.ActivePackageSource; }
            set
            {
                if (_mruSourceManager.ActivePackageSource != value)
                {
                    _mruSourceManager.ActivePackageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedPublishItem
        {
            get { return _selectedPublishItem; }
            set
            {
                if (_selectedPublishItem != value)
                {
                    _selectedPublishItem = value;
                    OnPropertyChanged();

                    if (value != null)
                    {
                        // store the selected source into settings
                        PublishUrl = value;

                        if (!_suppressReadingApiKey)
                        {
                            // when the selection change, we retrieve the API key for that source
                            string key = _settingsManager.ReadApiKey(value);
                            if (!String.IsNullOrEmpty(key))
                            {
                                PublishKey = key;
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> PublishSources
        {
            get { return _mruSourceManager.PackageSources; }
        }

        public bool? PublishAsUnlisted
        {
            get { return _publishAsUnlisted; }
            set
            {
                if (_publishAsUnlisted != value)
                {
                    _publishAsUnlisted = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Id
        {
            get { return _package.Id; }
        }

        public string Version
        {
            get { return _package.Version.ToString(); }
        }

        public bool HasError
        {
            get { return _hasError; }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowProgress
        {
            get { return _showProgress; }
            set
            {
                if (_showProgress != value)
                {
                    _showProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanPublish
        {
            get { return _canPublish; }
            set
            {
                if (_canPublish != value)
                {
                    _canPublish = value;
                    OnPropertyChanged();
                }
            }
        }

        public SourceRepository Repository
        {
            get
            {
                if (_repository == null ||
                    !PublishUrl.Equals(_repository.PackageSource.Source, StringComparison.OrdinalIgnoreCase))
                {
                    _repository = PackageRepositoryFactory.CreateRepository(PublishUrl);
                }
                return _repository;
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        #region IObserver<int> Members

        public void OnCompleted()
        {
            ShowProgress = false;
            HasError = false;
            Status = (PublishAsUnlisted == true) ? "Package published and unlisted successfully." : "Package published successfully.";
            _settingsManager.WriteApiKey(PublishUrl, PublishKey);
            CanPublish = true;
        }

        public void OnError(Exception error)
        {
            ShowProgress = false;
            HasError = true;
            Status = error.Message;
            CanPublish = true;
        }

        public void OnNext(int value)
        {
        }

        #endregion

        public async Task PushPackage()
        {
            ShowProgress = true;
            Status = (PublishAsUnlisted == true) ? "Publishing and unlisting package..." : "Publishing package...";
            HasError = false;
            CanPublish = false;

            try
            {
                var updateResource = await Repository.GetResourceAsync<PackageUpdateResource>();

                await updateResource.Push(_packageFilePath, null, 999, false, (s) => PublishKey, (s) => PublishKey, NuGet.Common.NullLogger.Instance);

                if (PublishAsUnlisted == true)
                {
                    await updateResource.Delete(Id, Version, (s) => PublishKey, (s) => true, NuGet.Common.NullLogger.Instance);
                }

                OnCompleted();
            }
            catch (Exception exception)
            {
                OnError(exception);
            }
            finally
            {
                // add the publish url to the list
                _mruSourceManager.NotifyPackageSourceAdded(PublishUrl);

                // this is to make sure the combo box doesn't goes blank after publishing
                try
                {
                    _suppressReadingApiKey = true;
                    SelectedPublishItem = PublishUrl;
                }
                finally
                {
                    _suppressReadingApiKey = false;
                }
            }
        }

        public void Dispose()
        {
            _settingsManager.PublishAsUnlisted = (bool)PublishAsUnlisted;
        }
    }
}