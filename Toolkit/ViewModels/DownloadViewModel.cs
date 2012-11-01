using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoApp.Mg.Toolkit.ViewModels
{
    using Models;

    public class DownloadViewModel : Screen, IHandle<DownloadEvent>
    {
        private readonly IEventAggregator events;

        public BindableCollection<Activity> List { get; set; }

        public int Progress { get; set; }
        public string CurrentStatus { get; set; }

        public DownloadViewModel(IEnumerable<PackageModel> downloaded, IEventAggregator events)
        {
            DisplayName = "";

            events.Subscribe(this);
            this.events = events;

            List = new BindableCollection<Activity>(downloaded.Where(n => n != null).Select(n => new Activity { PackageModel = n, Progress = 0 }));

            if (List.All(n => n.Progress == 100))
                Finish();
        }

        public void Cancel(object sender, EventArgs e)
        {
            IoC.Get<CoAppService>().CancelTask();
            events.Unsubscribe(this);
            TryClose(false);
        }

        public void Finish()
        {
            events.Unsubscribe(this);
            TryClose(true);
        }

        public void Handle(DownloadEvent message)
        {
            var package = List.FirstOrDefault(n => n.PackageModel.Package.CanonicalName.PackageName == message.CanonicalName.PackageName);

            if (package == null)
                return;

            package.Progress = message.Progress;

            CurrentStatus = "Downloading file " + (List.IndexOf(package) + 1) + " of " + List.Count;
            NotifyOfPropertyChange(() => CurrentStatus);

            Progress = (int)List.Average(n => n.Progress);
            NotifyOfPropertyChange(() => Progress);

            if (List.All(n => n.Progress == 100))
                Finish();
        }
    }
}