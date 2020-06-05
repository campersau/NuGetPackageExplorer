using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

using NuGetPackageExplorer.Types;

using NuGetPe;

namespace PackageExplorerViewModel
{
    public sealed class PublishPackageViewModel : ViewModelBase, IDisposable
    {
        private readonly MruPackageSourceManager _mruSourceManager;
        private readonly IPackageMetadata _package;
        private readonly string? _packageFilePath;
        private readonly ISettingsManager _settingsManager;
        private readonly IUIServices _uiServices;
        private readonly CredentialPublishProvider _credentialPublishProvider;
        private bool _canPublish = true;
        private bool _hasError;
        private string? _publishKeyOrPAT;
        private bool? _publishAsUnlisted = true;
        private bool? _appendV2ApiToUrl = true;
        private bool _showProgress;
        private string? _status;
        private bool _suppressReadingApiKey;

        public PublishPackageViewModel(
            MruPackageSourceManager mruSourceManager,
            ISettingsManager settingsManager,
            IUIServices uiServices,
            CredentialPublishProvider credentialPublishProvider,
            PackageViewModel viewModel)
        {
            if (viewModel is null)
                throw new ArgumentNullException(nameof(viewModel));
            _mruSourceManager = mruSourceManager ?? throw new ArgumentNullException(nameof(mruSourceManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
            _credentialPublishProvider = credentialPublishProvider ?? throw new ArgumentNullException(nameof(credentialPublishProvider));
            _package = viewModel.PackageMetadata;
            _packageFilePath = viewModel.GetCurrentPackageTempFile();
            SelectedPublishItem = _mruSourceManager.ActivePackageSource;
            PublishAsUnlisted = _settingsManager.PublishAsUnlisted;
            ChangePackageSourceCommand = new RelayCommand<NuGet.Configuration.PackageSource>(ChangePackageSource);
        }

        public ICommand ChangePackageSourceCommand { get; private set; }

        public string? PublishKeyOrPAT
        {
            get { return _publishKeyOrPAT; }
            set
            {
                if (_publishKeyOrPAT != value)
                {
                    _publishKeyOrPAT = value;
                    OnPropertyChanged(nameof(PublishKeyOrPAT));
                }
            }
        }

        public NuGet.Configuration.PackageSource SelectedPublishItem
        {
            get { return _mruSourceManager.ActivePackageSource; }
            set
            {
                if (value != null)
                {
                    if (_mruSourceManager.ActivePackageSource != value)
                    {
                        _mruSourceManager.ActivePackageSource = value;
                        OnPropertyChanged(nameof(SelectedPublishItem));
                    }

                    if (!_suppressReadingApiKey)
                    {
                        // when the selection change, we retrieve the API key for that source
                        try
                        {
                            var key = _settingsManager.ReadApiKey(value.Source);
                            if (!string.IsNullOrEmpty(key))
                            {
                                PublishKeyOrPAT = key;
                            }
                        }
                        catch (Exception e)
                        {
                            _uiServices.Show("Cannot read API key:\n" + e.Message, MessageLevel.Error);
                            PublishKeyOrPAT = null;
                        }
                    }
                }
            }
        }

        private void ChangePackageSource(NuGet.Configuration.PackageSource source)
        {
            if (SelectedPublishItem.Source != source.Source)
            {
                SelectedPublishItem = source;
            }
        }

        public ObservableCollection<NuGet.Configuration.PackageSource> PublishSources
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
                    OnPropertyChanged(nameof(PublishAsUnlisted));
                }
            }
        }

        public bool? AppendV2ApiToUrl
        {
            get { return _appendV2ApiToUrl; }
            set
            {
                if (_appendV2ApiToUrl != value)
                {
                    _appendV2ApiToUrl = value;
                    OnPropertyChanged(nameof(AppendV2ApiToUrl));
                }
            }
        }

        public string Id
        {
            get { return _package.Id; }
        }

        public string Version
        {
            get { return _package.Version.ToFullString(); }
        }

        public bool HasError
        {
            get { return _hasError; }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged(nameof(HasError));
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
                    OnPropertyChanged(nameof(ShowProgress));
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
                    OnPropertyChanged(nameof(CanPublish));
                }
            }
        }

        public string? Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public async Task PushPackage()
        {
            DiagnosticsClient.TrackEvent("PushPackage");

            ShowProgress = true;
            Status = (PublishAsUnlisted == true) ? "Publishing and unlisting package..." : "Publishing package...";
            HasError = false;
            CanPublish = false;

            _credentialPublishProvider.PersonalAccessToken = PublishKeyOrPAT;

            try
            {
                var repository = PackageRepositoryFactory.CreateRepository(SelectedPublishItem);
                var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

                await updateResource.Push(_packageFilePath, null, 999, false, s => PublishKeyOrPAT, s => PublishKeyOrPAT, AppendV2ApiToUrl != true, false, null, NullLogger.Instance);


                if (PublishAsUnlisted == true)
                {
                    await updateResource.Delete(Id, Version, s => PublishKeyOrPAT, s => true, AppendV2ApiToUrl != true, NullLogger.Instance);
                }

                HasError = false;
                Status = (PublishAsUnlisted == true) ? "Package published and unlisted successfully." : "Package published successfully.";
                if (PublishKeyOrPAT != null)
                {
                    _settingsManager.WriteApiKey(SelectedPublishItem.Source, PublishKeyOrPAT);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                Status = ex.Message;
            }
            finally
            {
                _credentialPublishProvider.PersonalAccessToken = null;

                var source = SelectedPublishItem;
                // add the publish url to the list
                _mruSourceManager.NotifyPackageSourceAdded(source);

                // this is to make sure the combo box doesn't goes blank after NotifyPackageSourceAdded
                try
                {
                    _suppressReadingApiKey = true;
                    SelectedPublishItem = source;
                }
                finally
                {
                    _suppressReadingApiKey = false;
                }

                ShowProgress = false;
                CanPublish = true;
            }
        }

        public void Dispose()
        {
            _settingsManager.PublishAsUnlisted = PublishAsUnlisted.GetValueOrDefault();
        }
    }
}
