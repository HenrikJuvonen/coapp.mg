using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CoApp.Mg.Toolkit.ViewModels
{
    using Models;

    public class ApplyViewModel : Screen, IHandle<ActivityEvent>
    {
        private readonly IEventAggregator events;

        public BindableCollection<Activity> List { get; set; }

        public string Header { get; set; }
        public int Progress { get; set; }
        public string CurrentStatus { get; set; }

        public string CloseButtonText { get; set; }
        public Visibility ProgressVisibility { get; set; }
        public Visibility SuccessVisibility { get; set; }
        public Visibility ErrorVisibility { get; set; }

        public ApplyViewModel(IEnumerable<PackageModel> packages, IEventAggregator events)
        {
            DisplayName = "";

            CloseButtonText = "Cancel";
            ProgressVisibility = Visibility.Visible;
            SuccessVisibility = Visibility.Collapsed;
            ErrorVisibility = Visibility.Collapsed;

            this.events = events;
            events.Subscribe(this);
            
            List = new BindableCollection<Activity>(packages.Where(n => n != null).Select(n => new Activity { PackageModel = n, Progress = 0 }));

            string header = null;

            var a0 = packages.Any(n => n.Mark == PackageMark.MarkedForInstallation);
            var a1 = packages.Any(n => n.Mark == PackageMark.MarkedForReinstallation);
            var a2 = packages.Any(n => n.Mark == PackageMark.MarkedForRemoval);

            if (a0) header = "Installing";
            if (a1) header = header == null ? "Reinstalling" : header + ", reinstalling";
            if (a2) header = header == null ? "Removing" : header + " and removing";

            Header = header + " packages";

            if (List.All(n => n.Progress == 100))
                Finish();
        }

        public void ClickClose(object sender, EventArgs e)
        {
            IoC.Get<CoAppService>().CancelTask();
            events.Unsubscribe(this);
            TryClose(true);
        }

        public void Finish()
        {
            CloseButtonText = "Close";
            ProgressVisibility = Visibility.Collapsed;
            SuccessVisibility = Visibility.Visible;

            NotifyOfPropertyChange(() => CloseButtonText);
            NotifyOfPropertyChange(() => ProgressVisibility);
            NotifyOfPropertyChange(() => SuccessVisibility);
        }

        public void Error()
        {
            CloseButtonText = "Close";
            ProgressVisibility = Visibility.Collapsed;
            ErrorVisibility = Visibility.Visible;

            NotifyOfPropertyChange(() => CloseButtonText);
            NotifyOfPropertyChange(() => ProgressVisibility);
            NotifyOfPropertyChange(() => ErrorVisibility);
        }

        public void Handle(ActivityEvent message)
        {
            if (message is DownloadEvent)
                return;

            if (message is ErrorEvent)
            {
                Error();
                return;
            }

            var package = List.FirstOrDefault(n => n.PackageModel.Package.CanonicalName.PackageName == message.CanonicalName.PackageName);

            if (package == null)
                return;

            var progress = message.Progress;

            if (progress < package.Progress)
                return;

            if (package.PackageModel.Mark == PackageMark.MarkedForReinstallation)
            {
                if (message is InstallEvent) progress = 50 + progress / 2;
                if (message is RemoveEvent) progress = progress / 2;
                CurrentStatus = "Reinstalling " + package.PackageModel;
            }
            else
            {
                if (message is InstallEvent) CurrentStatus = "Installing " + package.PackageModel;
                if (message is RemoveEvent) CurrentStatus = "Removing " + package.PackageModel;
            }

            package.Progress = progress;

            NotifyOfPropertyChange(() => CurrentStatus);

            Progress = (int)List.Average(n => n.Progress);
            NotifyOfPropertyChange(() => Progress);

            if (List.All(n => n.Progress == 100))
                Finish();
        }
    }
}