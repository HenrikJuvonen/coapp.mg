using Caliburn.Micro;

namespace CoApp.Mg.PackageManager.ViewModels
{
    public class AboutViewModel : Screen
    {
        public AboutViewModel()
        {
            DisplayName = "About CoApp.Mg";
        }

        public void ClickClose()
        {
            TryClose();
        }
    }
}
