using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    public sealed class PackageChooserViewModel : ViewModelBase, IDisposable
    {
        private const int ShowLatestVersionPageSize = 15;
        private readonly string _fixedPackageSource;
        private int _beginPackage;
        private int? _lastPackage;
        private CancellationTokenSource _currentCancellationTokenSource;
        private IQueryContext<IPackageSearchMetadata> _currentQuery;
        private string _currentSearch;
        private string _currentTypingSearch;
        private int _endPackage;
        private bool _hasError;
        private bool _isEditable = true;
        private SourceRepository _packageRepository;
        private MruPackageSourceManager _packageSourceManager;
        private bool _showPrereleasePackages;
        private bool _autoLoadPackages;
        private string _statusContent;
        private PackageInfoViewModel _selectedPackageViewModel;

        public PackageChooserViewModel(
            MruPackageSourceManager packageSourceManager,
            bool showPrereleasePackages,
            bool autoLoadPackages,
            string fixedPackageSource)
        {
            if (packageSourceManager == null)
            {
                throw new ArgumentNullException("packageSourceManager");
            }

            _showPrereleasePackages = showPrereleasePackages;
            _fixedPackageSource = fixedPackageSource;
            _autoLoadPackages = autoLoadPackages;
            Packages = new ObservableCollection<PackageInfoViewModel>();
            SearchCommand = new RelayCommand<string>(Search, CanSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch, CanClearSearch);
            NavigationCommand = new RelayCommand<string>(NavigationCommandExecute, NavigationCommandCanExecute);
            LoadedCommand = new RelayCommand(async () => await LoadPackages());
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            CancelCommand = new RelayCommand(CancelCommandExecute, CanCancelCommandExecute);
            _packageSourceManager = packageSourceManager;
        }

        public SourceRepository ActiveRepository
        {
            get
            {
                return _packageRepository;
            }
        }

        public string CurrentTypingSearch
        {
            get { return _currentTypingSearch; }
            set
            {
                if (_currentTypingSearch != value)
                {
                    _currentTypingSearch = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackageInfoViewModel SelectedPackageViewModel
        {
            get { return _selectedPackageViewModel; }
            set
            {
                if (_selectedPackageViewModel != value)
                {
                    if (_selectedPackageViewModel != null)
                    {
                        _selectedPackageViewModel.OnDeselected();
                    }

                    _selectedPackageViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public IPackageSearchMetadata SelectedPackage
        {
            get
            {
                return _selectedPackageViewModel == null ? null : _selectedPackageViewModel.EffectiveSelectedPackage;
            }
        }

        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                if (_isEditable != value)
                {
                    _isEditable = value;
                    OnPropertyChanged();
                    NavigationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowPrereleasePackages
        {
            get { return _showPrereleasePackages; }
            set
            {
                if (_showPrereleasePackages != value)
                {
                    _showPrereleasePackages = value;
                    OnPropertyChanged();

                    OnShowPrereleasePackagesChange();
                }
            }
        }

        public bool AutoLoadPackages
        {
            get { return _autoLoadPackages; }
            set
            {
                if (_autoLoadPackages != value)
                {
                    _autoLoadPackages = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> PackageSources
        {
            get { return _packageSourceManager.PackageSources; }
        }

        public bool AllowsChangingPackageSource
        {
            get { return _fixedPackageSource == null; }
        }

        public string PackageSource
        {
            get { return _fixedPackageSource ?? _packageSourceManager.ActivePackageSource; }
            private set
            {
                if (_fixedPackageSource != null)
                {
                    throw new InvalidOperationException(
                        "Cannot set active package source when fixed package source is used.");
                }
                _packageSourceManager.ActivePackageSource = value;
                OnPropertyChanged();
            }
        }

        private CancellationTokenSource CurrentCancellationTokenSource
        {
            get { return _currentCancellationTokenSource; }
            set
            {
                _currentCancellationTokenSource = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        public int BeginPackage
        {
            get { return _beginPackage; }
            private set
            {
                if (_beginPackage != value)
                {
                    _beginPackage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EndPackage
        {
            get { return _endPackage; }
            private set
            {
                if (_endPackage != value)
                {
                    _endPackage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusContent
        {
            get { return _statusContent; }
            set
            {
                if (_statusContent != value)
                {
                    _statusContent = value;
                    OnPropertyChanged();
                }
            }
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

        public ObservableCollection<PackageInfoViewModel> Packages { get; private set; }

        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public event EventHandler LoadPackagesCompleted = delegate { };
        public event EventHandler OpenPackageRequested = delegate { };
        public event EventHandler PackageDownloadRequested = delegate { };

        private async void OnShowPrereleasePackagesChange()
        {
            await LoadPackages();
        }

        private SourceRepository GetPackageRepository()
        {
            if (_packageRepository == null)
            {
                _packageRepository = PackageRepositoryFactory.CreateRepository(PackageSource);
            }

            return _packageRepository;
        }

        private void ResetPackageRepository()
        {
            _packageRepository = null;
        }

        internal async Task LoadPage(CancellationToken token)
        {
            Debug.Assert(_currentQuery != null);

            IsEditable = false;
            //ShowMessage(Resources.LoadingMessage, false);
            ClearPackages(isErrorCase: false);

            if (token == CancellationToken.None)
            {
                CurrentCancellationTokenSource = new CancellationTokenSource();
                token = CurrentCancellationTokenSource.Token;
            }

            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            try
            {
                IList<IPackageSearchMetadata> packageInfos = await QueryPackages(token);

                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                ShowPackages(packageInfos, _currentQuery.CurrentPage * ShowLatestVersionPageSize, _currentQuery.CurrentPage * ShowLatestVersionPageSize + ShowLatestVersionPageSize);
            }
            catch (OperationCanceledException)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                UpdatePageNumber(_currentQuery.CurrentPage * ShowLatestVersionPageSize, _currentQuery.CurrentPage * ShowLatestVersionPageSize + ShowLatestVersionPageSize);
            }
            catch (Exception exception)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                string errorMessage = exception.Message;

                var queryException = exception as DataServiceQueryException;
                if (queryException != null && queryException.Response != null)
                {
                    errorMessage = errorMessage + ". The remote server returned status code: " + queryException.Response.StatusCode + ".";
                }

                ShowMessage(errorMessage, true);
                ClearPackages(isErrorCase: true);
            }

            RestoreUI();
        }

        private async Task<IList<IPackageSearchMetadata>> QueryPackages(CancellationToken token)
        {
            var result = await _currentQuery.GetItemsForCurrentPage(token);

            token.ThrowIfCancellationRequested();

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private async Task LoadPackages()
        {
            IsEditable = false;
            ClearPackages(isErrorCase: true);

            CurrentCancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            var repository = GetPackageRepository();
            var searchResource = await repository.GetResourceAsync<PackageSearchResource>();
            var filter = new SearchFilter();
            filter.IncludePrerelease = ShowPrereleasePackages;

            _currentQuery = new ShowLatestVersionQueryContext<IPackageSearchMetadata>(searchResource, _currentSearch, filter, ShowLatestVersionPageSize);

            await LoadPage(CurrentCancellationTokenSource.Token);
        }

        private async void Search(string searchTerm)
        {
            searchTerm = searchTerm ?? CurrentTypingSearch ?? String.Empty;
            searchTerm = searchTerm.Trim();
            if (_currentSearch != searchTerm)
            {
                _currentSearch = searchTerm;
                await LoadPackages();
                CurrentTypingSearch = _currentSearch;
            }
        }

        private bool CanSearch(string searchTerm)
        {
            return IsEditable && !String.IsNullOrEmpty(searchTerm);
        }

        private async void ClearSearch()
        {
            CurrentTypingSearch = _currentSearch = String.Empty;
            await LoadPackages();
        }

        private bool CanClearSearch()
        {
            return IsEditable && !String.IsNullOrEmpty(_currentSearch);
        }

        private async void ChangePackageSource(string source)
        {
            if (PackageSource != source)
            {
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);
                PackageSource = source;

                ResetPackageRepository();
                await LoadPackages();
            }
            else
            {
                await LoadPackages();
            }
        }

        private void UpdatePageNumber(int beginPackage, int endPackage)
        {
            BeginPackage = beginPackage;
            EndPackage = endPackage;
        }

        private void ClearPackages(bool isErrorCase)
        {
            Packages.Clear();
            if (isErrorCase)
            {
                UpdatePageNumber(0, 0);
            }
        }

        private void ShowPackages(
            IEnumerable<IPackageSearchMetadata> packages, int beginPackage, int endPackage)
        {
            Packages.Clear();
            Packages.AddRange(packages.Select(p => new PackageInfoViewModel(p, ShowPrereleasePackages, _packageRepository, this)));
            UpdatePageNumber(beginPackage, endPackage);
        }

        private void ShowMessage(string message, bool isError)
        {
            StatusContent = message;
            HasError = isError;
        }

        private void ClearMessage()
        {
            ShowMessage(String.Empty, isError: false);
        }

        public void OnAfterShow()
        {
            CurrentTypingSearch = _currentSearch;
        }

        private void RestoreUI()
        {
            IsEditable = true;
            CurrentCancellationTokenSource = null;
            LoadPackagesCompleted(this, EventArgs.Empty);
        }

        internal void OnOpenPackage()
        {
            OpenPackageRequested(this, EventArgs.Empty);
        }

        internal void OnDownloadPackage()
        {
            PackageDownloadRequested(this, EventArgs.Empty);
        }

        #region CancelCommand

        private void CancelCommandExecute()
        {
            if (CurrentCancellationTokenSource != null)
            {
                CurrentCancellationTokenSource.Cancel();
                ClearMessage();
                IsEditable = true;
            }
        }

        private bool CanCancelCommandExecute()
        {
            return !IsEditable && CurrentCancellationTokenSource != null;
        }

        #endregion

        #region NavigationCommand

        private bool NavigationCommandCanExecute(string action)
        {
            if (!IsEditable)
            {
                return false;
            }

            switch (action)
            {
                case "First":
                    return CanMoveFirst();

                case "Previous":
                    return CanMovePrevious();

                case "Next":
                    return CanMoveNext();

                default:
                    throw new ArgumentOutOfRangeException("action");
            }
        }

        private async void NavigationCommandExecute(string action)
        {
            switch (action)
            {
                case "First":
                    await MoveFirst();
                    break;

                case "Previous":
                    await MovePrevious();
                    break;

                case "Next":
                    await MoveNext();
                    break;
            }
        }    

        private Task MoveNext()
        {
            bool canMoveNext = _currentQuery.MoveNext();
            if (canMoveNext)
            {
                return LoadPage(CancellationToken.None);
            }
            else
            {
                _lastPackage = EndPackage;
            }

            return Task.FromResult(0);
        }

        private Task MovePrevious()
        {
            bool canMovePrevious = _currentQuery.MovePrevious();
            if (canMovePrevious)
            {
                return LoadPage(CancellationToken.None);
            }      

            return Task.FromResult(0);
        }

        private Task MoveFirst()
        {
            _currentQuery.MoveFirst();
            return LoadPage(CancellationToken.None);
        }

        private bool CanMoveNext()
        {
            return !_lastPackage.HasValue || EndPackage < _lastPackage;
        }

        private bool CanMovePrevious()
        {
            return BeginPackage > 1;
        }

        private bool CanMoveFirst()
        {
            return BeginPackage > 1;
        }

        #endregion

        public void Dispose()
        {
            if (_packageSourceManager != null)
            {
                _packageSourceManager.Dispose();
                _packageSourceManager = null;
            }

            if (CurrentCancellationTokenSource != null)
            {
                CurrentCancellationTokenSource.Dispose();
            }
        }
    }
}