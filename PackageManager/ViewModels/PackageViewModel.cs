using Caliburn.Micro;
using CoApp.Packaging.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Serialization;

namespace CoApp.Mg.PackageManager.ViewModels
{
    using Toolkit;
    using Toolkit.Models;
    using Toolkit.ViewModels;

    [Export(typeof(PackageViewModel))]
    public class PackageViewModel : PropertyChangedBase, IHandle<PackageEvent>
    {
        [ImportingConstructor]
        public PackageViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            this.events = events;
            this.windowManager = windowManager;

            events.Subscribe(this);

            Options = new Options();
            Options.Load();

            Packages = new BindableCollection<PackageModel>();
            PackagesView = CollectionViewSource.GetDefaultView(Packages);
            PackagesView.SortDescriptions.Add(new SortDescription("SortName", ListSortDirection.Ascending));

            try
            {
                var path = MgConstants.AppDataPath + "Filters.xml";

                var reader = new XmlSerializer(typeof(BindableCollection<Filter>), new XmlRootAttribute("Filters"));
                var file = new StreamReader(path);
                Filters = (BindableCollection<Filter>)reader.Deserialize(file);
                file.Close();
            }
            catch
            {
                Filters = new BindableCollection<Filter>(new[] { 
                    new Filter("All", ""), 
                    new Filter("Newest", "newest"), 
                    new Filter("Installed", "installed"), 
                    new Filter("Locked", "locked") });
            }
            SelectedFilter = Filters.FirstOrDefault();
            
            QueryPackages();
        }

        private readonly IEventAggregator events;
        private readonly IWindowManager windowManager;

        public BindableCollection<PackageModel> Packages { get; private set; }
        public ICollectionView PackagesView { get; private set; }

        public BindableCollection<Filter> Filters { get; private set; }

        public Options Options { get; private set; }

        public bool IsBusy { get; private set; }

        private void UpdateFilter()
        {
            var operand = QueryParser.Parse(Query);

            if (!string.IsNullOrEmpty(Query) && operand == null)
                return;

            PackagesView.Filter = new System.Predicate<object>(o =>
            {
                return QueryMatcher.Match((PackageModel)o, operand);
            });
        }

        private Filter selectedFilter;
        public Filter SelectedFilter
        {
            get
            {
                return selectedFilter;
            }
            set
            {
                selectedFilter = value;

                if (selectedFilter != null)
                {
                    query = selectedFilter.Query;
                    UpdateFilter();
                    NotifyOfPropertyChange(() => Query);
                }

                NotifyOfPropertyChange(() => SelectedFilter);
            }
        }

        private string query = "";
        public string Query
        {
            get
            {
                return query;
            }
            set
            {
                query = value;
                UpdateFilter();
                SelectedFilter = null;
                NotifyOfPropertyChange(() => Query);
            }
        }

        private PackageModel selectedPackage;
        public PackageModel SelectedPackage
        {
            get
            {
                return selectedPackage;
            }
            set
            {
                selectedPackage = value;
                NotifyOfPropertyChange(() => SelectedPackage);
                NotifyOfPropertyChange(() => SelectedPackageDescription);
                events.Publish(new PackageEvent());
            }
        }

        public string SelectedPackageDescription
        {
            get
            {
                if (SelectedPackage != null)
                    return SelectedPackage.Description;
                else
                    return "No package is selected.";
            }
        }

        public async Task UpdateSelectedPackage()
        {
            SelectedPackage.Package = await IoC.Get<CoAppService>().GetPackage(SelectedPackage.Package);
            SelectedPackage.NotifyOfPropertyChange(() => SelectedPackage.Mark);
            events.Publish(new PackageEvent());
        }

        public async Task QueryPackages()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            events.Publish(new PackageEvent());

            var oldPackages = Packages.ToArray();
            
            Packages.Clear();

            var packages = Enumerable.Empty<IPackage>();

            try
            {
                packages = await IoC.Get<CoAppService>().QueryPackages();
            }
            catch
            {
            }

            Packages.AddRange(packages.Select(n => new PackageModel(n)));
            
            // Retain old markings
            await Task.Factory.StartNew(() => Parallel.ForEach(Packages, package =>
            {
                var oldPackage = oldPackages.FirstOrDefault(n => n.Package.CanonicalName == package.Package.CanonicalName);

                if (oldPackage != null && !oldPackage.IsUnmarked)
                    package.Mark = oldPackage.Mark;
            }));

            IsBusy = false;
            events.Publish(new PackageEvent());
        }

        public void Properties()
        {
            windowManager.ShowDialog(new PropertiesViewModel(SelectedPackage, GetDependencies(SelectedPackage), GetDependants(SelectedPackage)));
        }

        public bool CanProperties
        {
            get
            {
                return SelectedPackage != null;
            }
        }

        public bool TryMark()
        {
            if (SelectedPackage == null || SelectedPackage.IsLocked)
                return false;

            if (SelectedPackage.Status == PackageMark.NotInstalled || SelectedPackage.Status == PackageMark.NotInstalledNew)
            {
                if (SelectedPackage.IsUnmarked)
                    MarkForInstallation();
                else
                    Unmark();

                return true;
            }

            return false;
        }
        
        public void Unmark()
        {
            switch (SelectedPackage.Mark)
            {
                case PackageMark.MarkedForInstallation:
                    {
                        var updated = Packages.Where(n => n.Mark == PackageMark.MarkedForUpdate && n.Package.UpdatePackages.Contains(SelectedPackage.Package));

                        var installed = GetDependants(SelectedPackage).Where(n => n.Mark == PackageMark.MarkedForInstallation);

                        if (!MarkRelated(installed: installed, updated: updated, unmark: true))
                            return;
                    }
                    break;
                case PackageMark.MarkedForUpdate:
                    {
                        var updated = Packages.Where(k => k.Package == SelectedPackage.Package.UpdatePackages.FirstOrDefault(n => n.Version == SelectedPackage.Package.UpdatePackages.Max(m => m.Version)));

                        var installed = Enumerable.Empty<PackageModel>();

                        foreach (var u in updated)
                            installed = installed.Union(GetDependants(u).Where(n => n.Mark == PackageMark.MarkedForInstallation));

                        if (!MarkRelated(installed: installed, updated: updated, unmark: true))
                            return;
                    }
                    break;
                case PackageMark.MarkedForRemoval:
                    {
                        var removed = GetDependencies(SelectedPackage).Where(n => n.Mark == PackageMark.MarkedForRemoval);

                        if (!MarkRelated(removed: removed, unmark: true))
                            return;
                    }
                    break;
            }

            SelectedPackage.Mark = PackageMark.Unmarked;

            events.Publish(new PackageEvent());
        }

        public bool CanUnmark
        {
            get
            {
                return SelectedPackage != null && SelectedPackage.CanBeMarked(PackageMark.Unmarked);
            }
        }

        public void MarkForInstallation()
        {
            var installed = GetDependencies(SelectedPackage).Where(n => n.CanBeMarked(PackageMark.MarkedForInstallation));

            if (!MarkRelated(installed: installed))
                return;

            SelectedPackage.Mark = PackageMark.MarkedForInstallation;

            events.Publish(new PackageEvent());
        }

        public bool CanMarkForInstallation
        {
            get
            {
                return SelectedPackage != null && SelectedPackage.CanBeMarked(PackageMark.MarkedForInstallation);
            }
        }

        public void MarkForReinstallation()
        {
            var installed = GetDependencies(SelectedPackage).Where(n => n.CanBeMarked(PackageMark.MarkedForInstallation));

            if (!MarkRelated(installed: installed))
                return;

            SelectedPackage.Mark = PackageMark.MarkedForReinstallation;

            events.Publish(new PackageEvent());
        }

        public bool CanMarkForReinstallation
        {
            get
            {
                return SelectedPackage != null && SelectedPackage.CanBeMarked(PackageMark.MarkedForReinstallation);
            }
        }

        public void MarkForUpdate()
        {
            var update = Packages.FirstOrDefault(k => k.Package == SelectedPackage.Package.UpdatePackages.FirstOrDefault(n => n.Version == SelectedPackage.Package.UpdatePackages.Max(m => m.Version)));

            if (update != null)
            {
                var installed = GetDependencies(update).Where(n => n.CanBeMarked(PackageMark.MarkedForInstallation)).Union(new[] { update });

                if (!MarkRelated(installed: installed))
                    return;

                SelectedPackage.Mark = PackageMark.MarkedForUpdate;

                events.Publish(new PackageEvent());
            }
        }

        public bool CanMarkForUpdate
        {
            get
            {
                return SelectedPackage != null && SelectedPackage.CanBeMarked(PackageMark.MarkedForUpdate);
            }
        }

        public void MarkForRemoval()
        {
            var removed = GetDependants(SelectedPackage).Where(n => n.CanBeMarked(PackageMark.MarkedForRemoval));

            if (!MarkRelated(removed: removed))
                return;

            SelectedPackage.Mark = PackageMark.MarkedForRemoval;

            events.Publish(new PackageEvent());
        }

        public bool CanMarkForRemoval
        {
            get
            {
                return SelectedPackage != null && SelectedPackage.CanBeMarked(PackageMark.MarkedForRemoval);
            }
        }

        public void MarkButtonClick(object source)
        {
            var element = source as FrameworkElement;

            if (element != null)
            {
                SelectedPackage = (PackageModel)element.ToolTip;

                if (Options.ClickingOnTheStatusIconMarksTheMostLikelyAction && TryMark())
                    return;

                if (SelectedPackage == null || SelectedPackage.IsLocked)
                    return;

                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
        }

        public void Handle(PackageEvent message)
        {
            NotifyOfPropertyChange(() => CanUnmark);
            NotifyOfPropertyChange(() => CanMarkForInstallation);
            NotifyOfPropertyChange(() => CanMarkForReinstallation);
            NotifyOfPropertyChange(() => CanMarkForUpdate);
            NotifyOfPropertyChange(() => CanMarkForRemoval);
            NotifyOfPropertyChange(() => CanProperties);
            NotifyOfPropertyChange(() => IsBusy);
        }

        private bool MarkRelated(IEnumerable<PackageModel> installed = null, IEnumerable<PackageModel> updated = null, IEnumerable<PackageModel> removed = null, bool unmark = false)
        {
            if ((installed != null && installed.Any()) || (updated != null && updated.Any()) || (removed != null && removed.Any()))
            {
                if (Options.AskToConfirmChangesThatAlsoAffectOtherPackages)
                {
                    var canProceed = windowManager.ShowDialog(new MarkAdditionalChangesViewModel(installed, updated, removed));

                    if (canProceed != true)
                        return false;
                }

                if (installed != null) foreach (var p in installed)
                    p.Mark = unmark ? PackageMark.Unmarked : PackageMark.MarkedForInstallation;

                if (updated != null) foreach (var p in updated)
                    p.Mark = unmark ? PackageMark.Unmarked : PackageMark.MarkedForInstallation;

                if (removed != null) foreach (var p in removed)
                    p.Mark = unmark ? PackageMark.Unmarked : PackageMark.MarkedForRemoval;
            }
            return true;
        }

        private IEnumerable<PackageModel> GetDependencies(PackageModel package)
        {
            var dependencies = IoC.Get<CoAppService>().GetDependencies(package.Package).OrderBy(n => n.CanonicalName);

            foreach (var n in dependencies)
            {
                var m = Packages.FirstOrDefault(k => k.Package == n);

                if (m != null)
                    yield return m;
            }
        }

        private IEnumerable<PackageModel> GetDependants(PackageModel package)
        {
            foreach (var n in Packages)
            {
                if (n.Package.Dependencies.Contains(package.Package))
                    yield return n;
            }
        }
    }
}