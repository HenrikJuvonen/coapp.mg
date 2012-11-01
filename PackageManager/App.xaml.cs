using System.IO;
using System.Runtime;

namespace CoApp.Mg.PackageManager
{
    using Toolkit;

    public partial class App
    {
        public App()
        {
            Directory.CreateDirectory(MgConstants.AppDataPath);
            ProfileOptimization.SetProfileRoot(MgConstants.AppDataPath);
            ProfileOptimization.StartProfile("Startup.Profile");
        }
    }
}
