using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NuGet;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGetPackageExplorer.Types;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer
{
    [Export(typeof(IPackageDownloader))]
    internal class PackageDownloader : IPackageDownloader
    {
        private ProgressDialog _progressDialog;
        private readonly object _progressDialogLock = new object();

        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        #region IPackageDownloader Members

        public async Task Download(string targetFilePath, DownloadResource downloadResource, PackageIdentity packageIdentity)
        {
            string sourceFilePath = await DownloadWithProgress(downloadResource, packageIdentity);
            if (!String.IsNullOrEmpty(sourceFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
            }
        }

        public async Task<IPackage> Download(DownloadResource downloadResource, PackageIdentity packageIdentity)
        {
            string tempFilePath = await DownloadWithProgress(downloadResource, packageIdentity);
            return (tempFilePath == null) ? null : new ZipPackage(tempFilePath);
        }

        private async Task<string> DownloadWithProgress(DownloadResource downloadResource, PackageIdentity packageIdentity)
        {
            string progressDialogText = Resources.Resources.Dialog_DownloadingPackage;
            if (packageIdentity.HasVersion)
            {
                progressDialogText = String.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, packageIdentity.Version);
            }
            else
            {
                progressDialogText = String.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, string.Empty);
            }

            _progressDialog = new ProgressDialog
                              {
                                  Text = progressDialogText,
                                  WindowTitle = Resources.Resources.Dialog_Title,
                                  ShowTimeRemaining = true,
                                  CancellationText = "Canceling download..."
                              };
            _progressDialog.ShowDialog(MainWindow.Value);

            // polling for Cancel button being clicked
            var cts = new CancellationTokenSource();
            var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(200)
                        };

            timer.Tick += (o, e) =>
                          {
                              if (_progressDialog.CancellationPending)
                              {
                                  timer.Stop();
                                  cts.Cancel();
                              }
                          };
            timer.Start();

            try
            {
                downloadResource.Progress += OnReportProgress;

                using (var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, NuGet.Configuration.NullSettings.Instance, NuGet.Common.NullLogger.Instance, cts.Token))
                {
                    if (result.Status == DownloadResourceResultStatus.Cancelled)
                    {
                        throw new TaskCanceledException();
                    }
                    if (result.Status == DownloadResourceResultStatus.NotFound)
                    {
                        throw new Exception(String.Format("Package '{0}' not found", packageIdentity.Id + packageIdentity.Version.ToString()));
                    }

                    string tempFilePath = Path.GetTempFileName();

                    using (var fileStream = File.OpenWrite(tempFilePath))
                    {
                        await result.PackageStream.CopyToAsync(fileStream);
                    }

                    return tempFilePath;
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                OnError(exception);
                return null;
            }
            finally
            {
                downloadResource.Progress -= OnReportProgress;

                timer.Stop();

                // close progress dialog when done
                lock (_progressDialogLock)
                {
                    _progressDialog.Close();
                    _progressDialog = null;
                }

                MainWindow.Value.Activate();
            }
        }

        private void OnReportProgress(object sender, PackageProgressEventArgs args)
        {
            if (_progressDialog != null)
            {
                // report progress must be done via UI thread
                UIServices.BeginInvoke(() =>
                {
                    lock (_progressDialogLock)
                    {
                        if (_progressDialog != null)
                        {
                            int percentComplete = (int)(args.Complete * 100);
                            string description = String.Format(
                                CultureInfo.CurrentCulture,
                                "Downloaded {0}%",
                                percentComplete);

                            _progressDialog.ReportProgress(percentComplete);
                        }
                    }
                });
            }
        }

        #endregion

        private void OnError(Exception error)
        {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }
    }
}