using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NuGetPackageExplorer.Types;
using NuGetPe;
using OSVersionHelper;
using PackageExplorer.Properties;
using Windows.Storage;

namespace PackageExplorer
{
    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly object _lockObject = new object();

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>([CallerMemberName] string? name = null)
        {
            lock (_lockObject)
            {
                object value;
                try
                {
                    if (WindowsVersionHelper.HasPackageIdentity)
                    {
                        value = GetValueFromLocalSettings<T>(name!)!;
                    }
                    else
                    {
                        value = Settings.Default[name];
                        if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                        {
                            value = sc.Cast<string>().ToArray();
                        }
                    }

                    if (value is T t)
                    {
                        return t;
                    }
                }
                catch (ConfigurationErrorsException)
                {
                    // Corrupt settings file
                    Settings.Default.Reset();

                    // Try getting it again
                    value = Settings.Default[name];
                    if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                    {
                        value = sc.Cast<string>().ToArray();
                    }

                    if (value is T t)
                    {
                        return t;
                    }
                }
                catch (UnauthorizedAccessException)
                { }
                catch (IOException)
                {
                    // not much we can do if we can't read/write the settings file
                }

                return default!;
            }
        }

        // Don't load these types inline
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? GetValueFromLocalSettings<T>(string name)
        {
            object value;
            var settings = ApplicationData.Current.LocalSettings;
            value = settings.Values[name];
            if (typeof(T) == typeof(List<string>) && value is string str)
            {
                value = JsonConvert.DeserializeObject<List<string>>(str);
            }

            return value;
        }

        private void SetValue(object? value, string? name = null, [CallerMemberName] string? propertyName = null)
        {
            name ??= propertyName;

            lock (_lockObject)
            {
                try
                {
                    if (WindowsVersionHelper.HasPackageIdentity)
                    {
                        SetValueInLocalSettings(value, name!);
                    }
                    else
                    {
                        if (value is List<string> list)
                        {
                            var sc = new StringCollection();
                            sc.AddRange(list.ToArray());
                            value = sc;
                        }
                        Settings.Default[name] = value;
                    }
                }
                catch (UnauthorizedAccessException)
                { }
                catch (IOException)
                {
                    // not much we can do if we can't read/write the settings file
                }
            }

            OnPropertyChanged(propertyName);
        }

        // Don't load these types inline
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetValueInLocalSettings(object? value, string name)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (value is List<string> list)
            {
                value = JsonConvert.SerializeObject(list);
            }
            settings.Values[name] = value;
        }

        private NuGet.Configuration.ISettings? _nuGetSettings;
        public NuGet.Configuration.ISettings NuGetSettings
        {
            get
            {
                if (_nuGetSettings == null)
                {
                    _nuGetSettings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
                }
                return _nuGetSettings;
            }
        }

        public IList<string> GetMruFiles()
        {
            return GetValue<List<string>>("MruFiles") ?? new List<string>();
        }

        public void SetMruFiles(IEnumerable<string> files)
        {
            SetValue(files.ToList(), "MruFiles");
        }

        public IList<NuGet.Configuration.PackageSource> GetPackageSources()
        {
            var list = new List<NuGet.Configuration.PackageSource>();
            foreach (var packageSource in NuGet.Configuration.SettingsUtility.GetEnabledSources(NuGetSettings))
            {
                list.Add(packageSource);
            }

            var legacySources = GetValue<List<string>>("MruPackageSources");
            if (legacySources != null)
            {
                foreach (var legacySource in legacySources)
                {
                    if (!list.Any(packageSource => packageSource.Source == legacySource))
                    {
                        list.Add(new NuGet.Configuration.PackageSource(legacySource));
                    }
                }
            }

            return list;
        }

        public void SetPackageSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "MruPackageSources");
        }

        public NuGet.Configuration.PackageSource ActivePackageSource
        {
            get
            {
                var feedSource = GetValue<string>("PackageSource");
                foreach (var packageSource in GetPackageSources())
                {
                    if (packageSource.Source == feedSource)
                    {
                        return packageSource;
                    }
                }
                return NuGetConstants.DefaultFeedPackageSource;
            }
            set => SetValue(value.Source, "PackageSource");
        }

        public IList<NuGet.Configuration.PackageSource> GetPublishSources()
        {
            var list = new List<NuGet.Configuration.PackageSource>();
            foreach (var packageSource in NuGet.Configuration.SettingsUtility.GetEnabledSources(NuGetSettings))
            {
                list.Add(packageSource);
            }

            var legacySources = GetValue<List<string>>("PublishPackageSources");
            if (legacySources != null)
            {
                foreach (var legacySource in legacySources)
                {
                    if (!list.Any(packageSource => packageSource.Source == legacySource))
                    {
                        list.Add(new NuGet.Configuration.PackageSource(legacySource));
                    }
                }
            }

            return list;
        }

        public void SetPublishSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "PublishPackageSources");
        }

        public NuGet.Configuration.PackageSource ActivePublishSource
        {
            get
            {
                var publishSource = GetValue<string>("PublishPackageLocation");
                foreach (var packageSource in GetPublishSources())
                {
                    if (packageSource.Source == publishSource)
                    {
                        return packageSource;
                    }
                }
                return NuGetConstants.NuGetPublishFeedPackageSource;
            }
            set => SetValue(value.Source, "PublishPackageLocation");
        }

        public string? ReadApiKey(string source)
        {
            return NuGet.Configuration.SettingsUtility.GetDecryptedValueForAddItem(NuGetSettings, NuGet.Configuration.ConfigurationConstants.ApiKeys, source);
        }

        public void WriteApiKey(string source, string apiKey)
        {
            NuGet.Configuration.SettingsUtility.SetEncryptedValueForAddItem(NuGetSettings, NuGet.Configuration.ConfigurationConstants.ApiKeys, source, apiKey);
        }

        public bool ShowPrereleasePackages
        {
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public bool AutoLoadPackages
        {
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public bool PublishAsUnlisted
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }

        public string SigningCertificate
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string? TimestampServer
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string SigningHashAlgorithmName
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public int FontSize
        {
            get => GetValue<int?>() ?? 12;
            set => SetValue(value);
        }

        public bool ShowTaskShortcuts
        {
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public string WindowPlacement
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public double PackageChooserDialogWidth
        {
            get => GetValue<double?>() ?? 630;
            set => SetValue(value);
        }

        public double PackageChooserDialogHeight
        {
            get => GetValue<double?>() ?? 450;
            set => SetValue(value);
        }

        public double PackageContentHeight
        {
            get => GetValue<double?>() ?? 400;
            set => SetValue(value);
        }

        public double ContentViewerHeight
        {
            get => GetValue<double?>() ?? 400;
            set => SetValue(value);
        }

        public bool WordWrap
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }

        public bool ShowLineNumbers
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }
    }
}
