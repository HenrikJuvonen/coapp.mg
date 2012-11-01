using Caliburn.Micro;
using System;
using System.IO;
using System.Xml.Serialization;

namespace CoApp.Mg.PackageManager.ViewModels
{
    using Toolkit;
    using Toolkit.Models;

    public class FiltersViewModel : Screen
    {
        public BindableCollection<Filter> Filters { get; private set; }

        public FiltersViewModel(PackageViewModel packageViewModel, IEventAggregator events)
        {
            DisplayName = "Filters";

            Filters = packageViewModel.Filters;

            this.events = events;
            events.Subscribe(this);
        }

        private IEventAggregator events;

        public void ClickClose()
        {
            try
            {
                var path = MgConstants.AppDataPath;
                Directory.CreateDirectory(path);

                var writer = new XmlSerializer(typeof(BindableCollection<Filter>), new XmlRootAttribute("Filters"));
                var file = new StreamWriter(path + "Filters.xml");
                writer.Serialize(file, Filters);
                file.Close();
            }
            catch
            {
            }

            events.Unsubscribe(this);
            TryClose();
        }

        public void Add()
        {
            Filters.Add(new Filter("New filter", ""));
        }

        public void Remove()
        {
            Filters.Remove(SelectedItem);
        }

        private Filter selectedItem;
        public Filter SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                selectedItem = value;
                NotifyOfPropertyChange(() => SelectedItem);
            }
        }
    }
}
