using Caliburn.Micro;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CoApp.Mg.PackageManager.ViewModels
{
    using Toolkit;
    using Toolkit.Models;
    using Toolkit.ViewModels;

    [Export(typeof(AppViewModel))]
    public class AppViewModel : Screen, IHandle<PackageEvent>
    {
        private readonly IEventAggregator events;
        private readonly IWindowManager windowManager;

        [ImportingConstructor]
        public AppViewModel(PackageViewModel packageModel, IWindowManager windowManager, IEventAggregator events)
        {
            DisplayName = "CoApp.Mg";

            this.events = events;
            this.windowManager = windowManager;

            events.Subscribe(this);

            PackageViewModel = packageModel;
        }

        public PackageViewModel PackageViewModel { get; private set; }

        public bool IsReady
        {
            get
            {
                return !PackageViewModel.IsBusy;
            }
        }

        public async Task Reload()
        {
            await PackageViewModel.QueryPackages();
        }
        
        public bool CanReload
        {
            get
            {
                return !PackageViewModel.IsBusy;
            }
        }

        public async void Apply()
        {
            var installed = PackageViewModel.Packages.Where(n => n.Mark == PackageMark.MarkedForInstallation).OrderBy(n => n.SortName);
            var reinstalled = PackageViewModel.Packages.Where(n => n.Mark == PackageMark.MarkedForReinstallation).OrderBy(n => n.SortName);
            var removed = PackageViewModel.Packages.Where(n => n.Mark == PackageMark.MarkedForRemoval).OrderBy(n => n.SortName);
            
            if (windowManager.ShowDialog(new SummaryViewModel(installed, reinstalled, removed)) != true)
                return;

            if (installed.Any())
            {
                DownloadPackages(installed);

                if (windowManager.ShowDialog(new DownloadViewModel(installed, events)) != true)
                    return;
            }

            ApplyChanges(installed, reinstalled, removed);

            if (windowManager.ShowDialog(new ApplyViewModel(removed.Union(reinstalled).Union(installed), events)) != true)
                return;

            await UnmarkAll();
            await Reload();
        }
        
        public async void DownloadPackages(IEnumerable<PackageModel> packages)
        {
            await IoC.Get<CoAppService>().DownloadPackages(packages.Select(n => n.Package).ToArray());
        }

        public async void ApplyChanges(IEnumerable<PackageModel> installed, IEnumerable<PackageModel> reinstalled, IEnumerable<PackageModel> removed)
        {
            try
            {
                await IoC.Get<CoAppService>().ApplyChanges(
                    removed.Select(n => n.Package).ToArray(),
                    reinstalled.Select(n => n.Package).ToArray(),
                    installed.Select(n => n.Package).ToArray());
            }
            catch
            {
                events.Publish(new ErrorEvent("Error occurred."));
            }
        }


        public bool CanApply
        {
            get
            {
                return !PackageViewModel.Packages.All(n => n.IsUnmarked);
            }
        }

        public void Properties()
        {
            PackageViewModel.Properties();
        }

        public bool CanProperties
        { 
            get
            {
                return PackageViewModel.CanProperties;
            }
        }

        public async void ReadMarkings()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Markings (*.xml)|*.xml";

            if (dialog.ShowDialog() != true)
                return;

            var path = dialog.FileName;

            try
            {
                var reader = new XmlSerializer(typeof(List<PackageModel>), new XmlRootAttribute("Packages"));
                var file = new StreamReader(path);
                var packages = (List<PackageModel>)reader.Deserialize(file);
                file.Close();

                await Task.Factory.StartNew(() => Parallel.ForEach(PackageViewModel.Packages, package =>
                {
                    var p = packages.FirstOrDefault(n => n.CanonicalName == package.Package.CanonicalName);

                    if (p != null && !p.IsUnmarked)
                    {
                        package.Mark = p.Mark;
                    }
                }));

                events.Publish(new PackageEvent());
            }
            catch
            {
            }
        }

        public bool CanReadMarkings
        {
            get
            {
                return IsReady;
            }
        }

        public void SaveMarkingsAs()
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Markings (*.xml)|*.xml";

            if (dialog.ShowDialog() != true)
                return;

            var path = dialog.FileName;

            var packages = PackageViewModel.Packages.Where(n => !n.IsUnmarked).ToList();
            
            try
            {
                var writer = new XmlSerializer(typeof(List<PackageModel>), new XmlRootAttribute("Packages"));
                var file = new StreamWriter(path);
                writer.Serialize(file, packages);
                file.Close();
            }
            catch
            {
            }
        }

        public bool CanSaveMarkingsAs
        {
            get
            {
                return !PackageViewModel.Packages.All(n => n.IsUnmarked);
            }
        }

        public void Exit()
        {
            TryClose();
        }

        public async Task UnmarkAll()
        {
            await Task.Factory.StartNew(() => Parallel.ForEach(PackageViewModel.Packages, package => package.Mark = PackageMark.Unmarked));
            events.Publish(new PackageEvent());
        }

        public bool CanUnmarkAll
        {
            get
            {
                return PackageViewModel.Packages.Any(n => !n.IsUnmarked);
            }
        }

        public void Unmark()
        {
            PackageViewModel.Unmark();
        }

        public bool CanUnmark
        {
            get
            {
                return PackageViewModel.CanUnmark;
            }
        }

        public void MarkForInstallation()
        {
            PackageViewModel.MarkForInstallation();
        }

        public bool CanMarkForInstallation
        {
            get
            {
                return PackageViewModel.CanMarkForInstallation;
            }
        }

        public void MarkForReinstallation()
        {
            PackageViewModel.MarkForReinstallation();
        }

        public bool CanMarkForReinstallation
        {
            get
            {
                return PackageViewModel.CanMarkForReinstallation;
            }
        }

        public void MarkForUpdate()
        {
            PackageViewModel.MarkForUpdate();
        }

        public bool CanMarkForUpdate
        {
            get
            {
                return PackageViewModel.CanMarkForUpdate;
            }
        }

        public void MarkForRemoval()
        {
            PackageViewModel.MarkForRemoval();
        }

        public bool CanMarkForRemoval
        {
            get
            {
                return PackageViewModel.CanMarkForRemoval;
            }
        }

        public async void LockPackage()
        {
            if (PackageViewModel.SelectedPackage != null)
            {
                await IoC.Get<CoAppService>().TogglePackageLock(PackageViewModel.SelectedPackage.Package);
                await PackageViewModel.UpdateSelectedPackage();
                events.Publish(new PackageEvent());
            }
        }

        public bool CanLockPackage
        {
            get
            {
                return PackageViewModel.SelectedPackage != null;
            }
        }

        public bool IsLocked
        {
            get
            {
                return PackageViewModel.SelectedPackage != null ? PackageViewModel.SelectedPackage.IsLocked : false;
            }
        }
        
        public void Options()
        {
            windowManager.ShowDialog(new OptionsViewModel(PackageViewModel.Options, events));
        }

        public void Repositories()
        {
            windowManager.ShowDialog(new RepositoriesViewModel(events));
        }

        public void Filters()
        {
            windowManager.ShowDialog(new FiltersViewModel(PackageViewModel, events));
        }

        public void Contents()
        {
            Process.Start(MgConstants.HelpContents);
        }

        public void About()
        {
            windowManager.ShowDialog(new AboutViewModel());
        }
        
        public string Status
        {
            get
            {
                if (IsReady)
                {
                    var total = PackageViewModel.Packages.Count();

                    var installed = PackageViewModel.Packages.Count(n =>
                        n.Status == PackageMark.Installed ||
                        n.Status == PackageMark.InstalledLocked ||
                        n.Status == PackageMark.InstalledUpdatable);

                    var broken = PackageViewModel.Packages.Count(n =>
                        n.Status == PackageMark.Broken);

                    var toinstall = PackageViewModel.Packages.Count(n =>
                        n.Mark == PackageMark.MarkedForInstallation);
                    
                    var toremove = PackageViewModel.Packages.Count(n =>
                        n.Mark == PackageMark.MarkedForRemoval);

                    return string.Format("{0} packages listed, {1} installed, {2} broken, {3} to install, {4} to remove",
                        total, installed, broken, toinstall, toremove);
                }
                else
                {
                    return "Loading package information";
                }
            }
        }

        public void Handle(PackageEvent message)
        {
            NotifyOfPropertyChange(() => CanReadMarkings);
            NotifyOfPropertyChange(() => CanSaveMarkingsAs);
            NotifyOfPropertyChange(() => CanUnmark);
            NotifyOfPropertyChange(() => CanMarkForInstallation);
            NotifyOfPropertyChange(() => CanMarkForReinstallation);
            NotifyOfPropertyChange(() => CanMarkForUpdate);
            NotifyOfPropertyChange(() => CanMarkForRemoval);
            NotifyOfPropertyChange(() => CanLockPackage);
            NotifyOfPropertyChange(() => CanApply);
            NotifyOfPropertyChange(() => CanReload);
            NotifyOfPropertyChange(() => CanProperties);
            NotifyOfPropertyChange(() => CanUnmarkAll);
            NotifyOfPropertyChange(() => IsReady);
            NotifyOfPropertyChange(() => IsLocked);
            NotifyOfPropertyChange(() => Status);
        }
    }
}
