using Caliburn.Micro;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace CoApp.Mg.Toolkit.ViewModels
{
    using Models;

    public class SummaryViewModel : Screen
    {
        public List<TreeViewItem> List { get; private set; }

        public bool ButtonsEnabled { get; private set; }

        public SummaryViewModel(IEnumerable<PackageModel> installed, IEnumerable<PackageModel> reinstalled, IEnumerable<PackageModel> removed)
        {
            DisplayName = "";

            ButtonsEnabled = true;

            List = new List<TreeViewItem>();

            if (installed != null && installed.Any())
                List.Add(new TreeViewItem { Header = "To be installed", ItemsSource = installed, IsExpanded = true });

            if (reinstalled != null && reinstalled.Any())
                List.Add(new TreeViewItem { Header = "To be reinstalled", ItemsSource = reinstalled, IsExpanded = true });

            if (removed != null && removed.Any())
                List.Add(new TreeViewItem { Header = "To be removed", ItemsSource = removed, IsExpanded = true });
        }
        
        public void Cancel()
        {
            TryClose(false);
        }

        public async void Apply()
        {
            ButtonsEnabled = false;
            NotifyOfPropertyChange(() => ButtonsEnabled);

            var result = await IoC.Get<CoAppService>().TryElevate();
            TryClose(result);
        }

        public string InstalledCount
        {
            get
            {
                var count = List.Where(n => (string)n.Header == "To be installed").SelectMany(n => n.Items.OfType<PackageModel>()).Count();

                if (count == 0)
                    return null;

                return string.Format("{0} package{1} will be installed", count, count > 1 ? "s" : null);
            }
        }

        public string ReinstalledCount
        {
            get
            {
                var count = List.Where(n => (string)n.Header == "To be reinstalled").SelectMany(n => n.Items.OfType<PackageModel>()).Count();

                if (count == 0)
                    return null;

                return string.Format("{0} package{1} will be reinstalled", count, count > 1 ? "s" : null);
            }
        }

        public string RemovedCount
        {
            get
            {
                var count = List.Where(n => (string)n.Header == "To be removed").SelectMany(n => n.Items.OfType<PackageModel>()).Count();

                if (count == 0)
                    return null;

                return string.Format("{0} package{1} will be removed", count, count > 1 ? "s" : null);
            }
        }

    }
}