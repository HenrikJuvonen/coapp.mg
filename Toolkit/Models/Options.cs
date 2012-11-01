using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CoApp.Mg.Toolkit.Models
{
    public class Options : PropertyChangedBase
    {
        public Options()
        {
        }

        public void Load()
        {
            try
            {
                var path = MgConstants.AppDataPath + "Options.xml";

                var reader = new XmlSerializer(typeof(Options), new XmlRootAttribute("Options"));
                var file = new StreamReader(path);
                var options = (Options)reader.Deserialize(file);
                file.Close();

                AskToConfirmChangesThatAlsoAffectOtherPackages = options.AskToConfirmChangesThatAlsoAffectOtherPackages;
                ClickingOnTheStatusIconMarksTheMostLikelyAction = options.ClickingOnTheStatusIconMarksTheMostLikelyAction;
            }
            catch
            {
                AskToConfirmChangesThatAlsoAffectOtherPackages = true;
            }
        }

        public void Save()
        {
            try
            {
                var path = MgConstants.AppDataPath;
                Directory.CreateDirectory(path);

                var writer = new XmlSerializer(typeof(Options), new XmlRootAttribute("Options"));
                var file = new StreamWriter(path + "Options.xml");
                writer.Serialize(file, this);
                file.Close();
            }
            catch
            {
            }
        }
        
        private bool askToConfirmChangesThatAlsoAffectOtherPackages;
        public bool AskToConfirmChangesThatAlsoAffectOtherPackages
        {
            get
            {
                return askToConfirmChangesThatAlsoAffectOtherPackages;
            }
            set
            {
                askToConfirmChangesThatAlsoAffectOtherPackages = value;
                NotifyOfPropertyChange(() => AskToConfirmChangesThatAlsoAffectOtherPackages);
            }
        }

        private bool clickingOnTheStatusIconMarksTheMostLikelyAction;
        public bool ClickingOnTheStatusIconMarksTheMostLikelyAction
        {
            get
            {
                return clickingOnTheStatusIconMarksTheMostLikelyAction;
            }
            set
            {
                clickingOnTheStatusIconMarksTheMostLikelyAction = value;
                NotifyOfPropertyChange(() => ClickingOnTheStatusIconMarksTheMostLikelyAction);
            }
        }
    }
}
