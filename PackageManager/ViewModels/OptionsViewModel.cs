using Caliburn.Micro;

namespace CoApp.Mg.PackageManager.ViewModels
{
    using Toolkit.Models;

    public class OptionsViewModel : Screen
    {
        public OptionsViewModel(Options options, IEventAggregator events)
        {
            DisplayName = "Options";

            Options = options;

            this.events = events;
            events.Subscribe(this);
        }

        private IEventAggregator events;

        public Options Options { get; private set; }

        public void ClickClose()
        {
            Options.Save();
            events.Unsubscribe(this);
            TryClose();
        }
    }
}