using System.Windows.Controls;

namespace CoApp.Mg.PackageManager.Views
{
    using ViewModels;

    public partial class PackageView
    {
        public PackageView()
        {
            InitializeComponent();
        }

        public void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is Button)
            {
                e.Handled = true;
                return;
            }

            var packageViewModel = (PackageViewModel)DataContext;

            if (packageViewModel == null || packageViewModel.SelectedPackage == null)
                return;

            if (packageViewModel.SelectedPackage.IsLocked)
                e.Handled = true;
        }
    }
}
