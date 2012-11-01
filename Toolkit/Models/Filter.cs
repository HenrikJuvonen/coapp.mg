using Caliburn.Micro;
using System.Xml.Serialization;

namespace CoApp.Mg.Toolkit.Models
{
    public class Filter : PropertyChangedBase
    {
        private string name;

        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        private string query;

        [XmlTextAttribute]
        public string Query
        {
            get
            {
                return query;
            }
            set
            {
                query = value;
                NotifyOfPropertyChange(() => Query);
            }
        }

        public Filter()
        {
        }

        public Filter(string name, string query)
        {
            Name = name;
            Query = query;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
