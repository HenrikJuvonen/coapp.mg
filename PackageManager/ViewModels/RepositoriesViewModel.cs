using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoApp.Mg.PackageManager.ViewModels
{
    using Toolkit;

    public class RepositoriesViewModel : Screen
    {
        public RepositoriesViewModel(IEventAggregator events)
        {
            DisplayName = "Repositories";

            Feeds = new BindableCollection<string>();
            Publishers = new BindableCollection<string>();

            this.events = events;
            events.Subscribe(this);

            Load();
        }

        private async void Load()
        {
            var coapp = IoC.Get<CoAppService>();

            Feeds.AddRange((await coapp.GetFeeds()).Select(n => n.Location));
            Publishers.AddRange(await coapp.GetTrustedPublishers());
        }

        private IEventAggregator events;

        public BindableCollection<string> Feeds { get; private set; }
        public BindableCollection<string> Publishers { get; private set; }

        private string feedLocation;
        public string FeedLocation
        {
            get
            {
                return feedLocation;
            }
            set
            {
                feedLocation = value;
                NotifyOfPropertyChange(() => FeedLocation);
                NotifyOfPropertyChange(() => CanAddFeed);
            }
        }

        private string publicKeyToken;
        public string PublicKeyToken
        {
            get
            {
                return publicKeyToken;
            }
            set
            {
                publicKeyToken = value;
                NotifyOfPropertyChange(() => PublicKeyToken);
                NotifyOfPropertyChange(() => CanAddPublisher);
            }
        }

        private string selectedFeed;
        public string SelectedFeed
        {
            get
            {
                return selectedFeed;
            }
            set
            {
                selectedFeed = value;
                NotifyOfPropertyChange(() => SelectedFeed);
                NotifyOfPropertyChange(() => CanAddFeed);
                NotifyOfPropertyChange(() => CanRemoveFeed);
            }
        }

        private string selectedPublisher;
        public string SelectedPublisher
        {
            get
            {
                return selectedPublisher;
            }
            set
            {
                selectedPublisher = value;
                NotifyOfPropertyChange(() => SelectedPublisher);
                NotifyOfPropertyChange(() => CanAddPublisher);
                NotifyOfPropertyChange(() => CanRemovePublisher);
            }
        }

        public void ClickClose()
        {
            events.Unsubscribe(this);
            TryClose();
        }

        public async void AddFeed()
        {
            var result = await IoC.Get<CoAppService>().TryElevate();

            if (!result)
                return;

            try
            {
                await IoC.Get<CoAppService>().AddFeed(FeedLocation);
            }
            catch
            {
                return;
            }

            Publishers.Add(FeedLocation);
            FeedLocation = null;
        }

        public bool CanAddFeed
        {
            get
            {
                if (Feeds.Contains(FeedLocation))
                    return false;

                Uri result;
                if (Uri.TryCreate(FeedLocation, UriKind.Absolute, out result))
                {
                    return File.Exists(result.AbsolutePath) || result.IsWellFormedOriginalString();
                }

                return false;
            }
        }

        public async void RemoveFeed()
        {
            var result = await IoC.Get<CoAppService>().TryElevate();

            if (!result)
                return;

            try
            {
                await IoC.Get<CoAppService>().RemoveFeed(SelectedFeed);
            }
            catch
            {
                return;
            }

            Feeds.Remove(SelectedFeed);
            SelectedFeed = null;

            if (!Feeds.Any())
                Feeds.AddRange((await IoC.Get<CoAppService>().GetFeeds()).Select(n => n.Location));
        }

        public bool CanRemoveFeed
        {
            get
            {
                return SelectedFeed != null;
            }
        }

        public async void AddPublisher()
        {
            var result = await IoC.Get<CoAppService>().TryElevate();

            if (!result)
                return;

            try
            {
                await IoC.Get<CoAppService>().AddTrustedPublisher(PublicKeyToken);
            }
            catch
            {
                return;
            }
            Publishers.Add(PublicKeyToken);
            PublicKeyToken = null;
        }

        public bool CanAddPublisher
        {
            get
            {
                return PublicKeyToken != null && !Publishers.Contains(PublicKeyToken) && PublicKeyToken.Length == 16 && Regex.IsMatch(PublicKeyToken, @"^[a-fA-F0-9]+$");
            }
        }

        public async void RemovePublisher()
        {
            var result = await IoC.Get<CoAppService>().TryElevate();

            if (!result)
                return;

            try
            {
                await IoC.Get<CoAppService>().RemoveTrustedPublisher(SelectedPublisher);
            }
            catch
            {
                return;
            }

            Publishers.Remove(SelectedPublisher);
            SelectedPublisher = null;

            if (!Publishers.Any())
                Publishers.AddRange(await IoC.Get<CoAppService>().GetTrustedPublishers());
        }

        public bool CanRemovePublisher
        {
            get
            {
                return SelectedPublisher != null;
            }
        }
    }
}
