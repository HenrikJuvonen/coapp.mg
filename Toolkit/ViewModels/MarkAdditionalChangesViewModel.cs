using Caliburn.Micro;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace CoApp.Mg.Toolkit.ViewModels
{
    using Models;

    public class MarkAdditionalChangesViewModel : Screen
    {
        public List<TreeViewItem> List { get; private set; }

        public MarkAdditionalChangesViewModel(IEnumerable<PackageModel> installed, IEnumerable<PackageModel> updated, IEnumerable<PackageModel> removed)
        {
            DisplayName = "";

            List = new List<TreeViewItem>();

            if (installed != null && installed.Any())
                List.Add(new TreeViewItem { Header = "To be installed", ItemsSource = installed.OrderBy(n => n.SortName), IsExpanded = true });

            if (updated != null && updated.Any())
                List.Add(new TreeViewItem { Header = "To be updated", ItemsSource = updated.OrderBy(n => n.SortName), IsExpanded = true });

            if (removed != null && removed.Any())
                List.Add(new TreeViewItem { Header = "To be removed", ItemsSource = removed.OrderBy(n => n.SortName), IsExpanded = true });
        }

        public void Cancel()
        {
            TryClose(false);
        }

        public void Mark()
        {
            TryClose(true);
        }
    }
}