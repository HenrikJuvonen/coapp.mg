using Caliburn.Micro;

namespace CoApp.Mg.Toolkit.Models
{
    public class Activity : PropertyChangedBase
    {
        public PackageModel PackageModel { get; set; }

        private int progress;
        public int Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
                NotifyOfPropertyChange(() => Progress);
            }
        }
    }
}
