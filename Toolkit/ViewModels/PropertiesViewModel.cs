using Caliburn.Micro;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CoApp.Mg.Toolkit.ViewModels
{
    using Models;

    [Export(typeof(PropertiesViewModel))]
    public class PropertiesViewModel : Screen
    {
        public PackageModel Package { get; private set; }
        public IEnumerable<PackageModel> Dependencies { get; private set; }
        public IEnumerable<PackageModel> Dependants { get; private set; }
        public IEnumerable<TreeViewItem> InstalledFiles { get; private set; }
        
        [ImportingConstructor]
        public PropertiesViewModel(PackageModel package, IEnumerable<PackageModel> dependencies, IEnumerable<PackageModel> dependants)
        {
            DisplayName = "Properties";
            
            Package = package;
            Dependencies = dependencies;
            Dependants = dependants.OrderBy(n => n.SortName);

            if (Package.Package.IsInstalled)
            {
                LoadInstalledFiles(package);
            }
        }

        private async void LoadInstalledFiles(PackageModel package)
        {
            var path = await IoC.Get<CoAppService>().GetPackageDirectory(package.Package);

            InstalledFiles = GetItems(path);
            NotifyOfPropertyChange(() => InstalledFiles);
        }

        private IEnumerable<TreeViewItem> GetItems(string path)
        {
            var info = new DirectoryInfo(path);

            var items = new List<TreeViewItem>();

            foreach (var directory in info.GetDirectories())
            {
                items.Add(new TreeViewItem { Header = directory.Name, ItemsSource = GetItems(directory.FullName), IsExpanded = true });
            }

            foreach (var file in info.GetFiles())
            {
                items.Add(new TreeViewItem { Header = file.Name });
            }

            return items;
        }

        public void ClickClose()
        {
            TryClose();
        }
    }
}