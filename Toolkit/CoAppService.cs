using Caliburn.Micro;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model.Atom;
using CoApp.Toolkit.Extensions;
using CoApp.Toolkit.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoApp.Mg.Toolkit
{
    using Models;

    [Export(typeof(CoAppService))]
    public class CoAppService
    {
        public bool IsElevated { get; private set; }

        private CancellationTokenSource cts;

        private CanonicalName currentDownload;
        private readonly ManualResetEvent waitDownload = new ManualResetEvent(true);
        private readonly List<string> downloads = new List<string>();

        private readonly PackageManager pkm = new PackageManager();

        [ImportingConstructor]
        private CoAppService(IEventAggregator events)
        {
            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) =>
            {
                events.Publish(new InstallEvent(name, progress));
            });

            CurrentTask.Events += new PackageInstalled(name =>
            {
                events.Publish(new InstallEvent(name, 100));
            });

            CurrentTask.Events += new PackageRemoveProgress((name, progress) =>
            {
                events.Publish(new RemoveEvent(name, progress));
            });

            CurrentTask.Events += new PackageRemoved(name =>
            {
                events.Publish(new RemoveEvent(name, 100));
            });

            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) =>
            {
                var decodedUrl = remoteLocation.UrlDecode();

                try
                {
                    CanonicalName result = new CanonicalName(decodedUrl);
                    events.Publish(new DownloadEvent(result, progress));
                }
                catch
                {
                    if (!downloads.Contains(decodedUrl))
                    {
                        downloads.Add(decodedUrl);
                    }
                }
            });
            
            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) =>
            {
                var decodedUrl = remoteLocation.UrlDecode();

                try
                {
                    CanonicalName result = new CanonicalName(decodedUrl);
                    events.Publish(new DownloadEvent(result, 100));

                    if (currentDownload.PackageName == result.PackageName)
                        waitDownload.Set();
                }
                catch
                {
                    if (downloads.Contains(decodedUrl))
                    {
                        downloads.Remove(decodedUrl);
                    }
                }
            });
        }

        public async Task<bool> TryElevate()
        {
            for (var i = 0; i < 5 && !IsElevated; i++)
            {
                try
                {
                    await pkm.Elevate().ContinueWith(t => IsElevated = true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return IsElevated;
        }

        public Task<IEnumerable<Feed>> GetFeeds()
        {
            return pkm.Feeds;
        }

        public Task AddFeed(string feedLocation)
        {
            return pkm.AddSystemFeed(feedLocation);
        }

        public Task RemoveFeed(string feedLocation)
        {
            return pkm.RemoveSystemFeed(feedLocation);
        }

        public Task<IEnumerable<string>> GetTrustedPublishers()
        {
            return pkm.TrustedPublishers;
        }

        public Task AddTrustedPublisher(string publicKeyToken)
        {
            return pkm.AddTrustedPublisher(publicKeyToken);
        }

        public Task RemoveTrustedPublisher(string publicKeyToken)
        {
            return pkm.RemoveTrustedPublisher(publicKeyToken);
        }
        
        public Task<IEnumerable<Package>> QueryPackages()
        {            
            return pkm.QueryPackages("*", null, null, null);
        }
        
        public Task DownloadPackages(IEnumerable<IPackage> packages)
        {
            cts = new CancellationTokenSource();

            var impl = new PackageManagerResponseImpl();
            return Task.Factory.StartNew(() =>
            {
                foreach (var p in packages)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    waitDownload.WaitOne();

                    impl.RequireRemoteFile(p.CanonicalName, p.RemoteLocations, PackageManagerSettings.CoAppCacheDirectory + "\\packages", true);
                    currentDownload = p.CanonicalName;
                    waitDownload.Reset();
                }
            }, cts.Token);
        }

        public Task ApplyChanges(IEnumerable<IPackage> removed, IEnumerable<IPackage> reinstalled, IEnumerable<IPackage> installed)
        {
            cts = new CancellationTokenSource();

            return Task.Factory.StartNew(() =>
            {
                RemovePackages(removed).Wait();
                ReinstallPackages(reinstalled).Wait();
                InstallPackages(installed).Wait();
            }, cts.Token);
        }

        public Task TogglePackageLock(IPackage package)
        {
            return Task.Factory.StartNew(() =>
            {
                pkm.SetPackageWanted(package.CanonicalName, !package.IsWanted);
            });
        }

        public Task<Package> GetPackage(IPackage package)
        {
            return pkm.GetPackage(package.CanonicalName, true);
        }

        public async Task<string> GetPackageDirectory(IPackage package)
        {
            AtomItem atomItem = null;

            try
            {
                atomItem = await pkm.GetAtomItem(package.CanonicalName);
            }
            catch
            {
                return null;
            }

            return string.Format(@"{0}\program files{1}\{2}\{3}\",
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                package.Architecture == "x64" ? " (x64)" : package.Architecture == "x86" ? " (x86)" : string.Empty,
                atomItem.Model.Vendor.MakeSafeFileName(),
                package.CanonicalName.PackageName);
        }

        public IEnumerable<IPackage> GetDependencies(IPackage package)
        {
            var result = Enumerable.Empty<IPackage>();

            foreach (var n in package.Dependencies)
            {
                result = result.Union(new[] { n }).Union(GetDependencies(n));
            }

            return result;
        }

        public void CancelTask()
        {
            cts.Cancel();
        }

        private Task RemovePackages(IEnumerable<IPackage> packages)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var p in packages)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    pkm.RemovePackage(p.CanonicalName, true).Wait();
                }
            }, cts.Token);
        }

        private Task ReinstallPackages(IEnumerable<IPackage> packages)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var p in packages)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    pkm.RemovePackage(p.CanonicalName, true).Wait();
                    pkm.Install(p.CanonicalName, false, true).Wait();
                }
            }, cts.Token);
        }

        private Task InstallPackages(IEnumerable<IPackage> packages)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (var p in packages)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    pkm.Install(p.CanonicalName, false, true).Wait();
                }
            }, cts.Token);
        }
    }
}