using Caliburn.Micro;
using CoApp.Packaging.Common;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace CoApp.Mg.Toolkit.Models
{
    public class PackageModel : PropertyChangedBase
    {
        public PackageModel()
        {
        }

        public PackageModel(IPackage package)
        {
            Package = package;
            CanonicalName = package.CanonicalName;
        }

        public override string ToString()
        {
            return Package.CanonicalName.PackageName.Replace(Package.CanonicalName.PublicKeyToken, null).TrimEnd('-');
        }

        [XmlIgnore]
        public IPackage Package { get; set; }

        [XmlAttribute("CanonicalName")]
        public string CanonicalName { get; set; }

        public string Name { get { return Package.Name; } }
        public string Flavor { get { return Package.Flavor.Plain; } }
        public string Version { get { return Package.Version; } }
        public string Architecture { get { return Package.Architecture; } }
        public string Summary { get { return Package.PackageDetails.SummaryDescription; } }
        public string Description { get { return Package.PackageDetails.Description; } }
        public string Publisher { get { return Package.PackageDetails.Publisher.Name; } }

        private string VersionAsSortableString { get { return string.Format("{0:D8}{1:D8}{2:D8}{3:D8}", Package.Version.Major, Package.Version.Minor, Package.Version.Revision, Package.Version.Build); } }

        public string SortMark { get { return string.Format("{4};{0};{1};{2};{3}", Name, Flavor, VersionAsSortableString, Architecture, Mark); } }
        public string SortName { get { return string.Format("{0};{1};{2};{3};{4}", Name, Flavor, VersionAsSortableString, Architecture, Mark); } }
        public string SortFlavor { get { return string.Format("{1};{0};{2};{3};{4}", Name, Flavor, VersionAsSortableString, Architecture, Mark); } }
        public string SortVersion { get { return string.Format("{2};{0};{1};{3};{4}", Name, Flavor, VersionAsSortableString, Architecture, Mark); } }
        public string SortArchitecture { get { return string.Format("{3};{0};{1};{2};{4}", Name, Flavor, VersionAsSortableString, Architecture, Mark); } }

        public PackageMark Status
        {
            get
            {
                PackageMark status;

                if (Package == null)
                    return PackageMark.Unmarked;

                if (Package.IsInstalled)
                {
                    if (Package.IsWanted)
                    {
                        status = PackageMark.InstalledLocked;
                    }
                    else if (Package.InstalledNewest == Package && Package.UpdatePackages.Any())
                    {
                        status = PackageMark.InstalledUpdatable;
                    }
                    else
                    {
                        status = PackageMark.Installed;
                    }
                }
                else
                {
                    if (Package.IsWanted)
                    {
                        status = PackageMark.NotInstalledLocked;
                    }
                    else if (Package.PackageDetails.PublishDate.Date == DateTime.Today.Date)
                    {
                        status = PackageMark.NotInstalledNew;
                    }
                    else
                    {
                        status = PackageMark.NotInstalled;
                    }
                }

                return status;
            }
        }

        private PackageMark mark = PackageMark.Unmarked;

        [XmlAttribute("Mark")]
        public PackageMark Mark
        {
            get
            {
                return mark == PackageMark.Unmarked ? Status : mark;
            }
            set
            {
                if (!CanBeMarked(value))
                    return;

                mark = value;
                NotifyOfPropertyChange(() => Mark);
            }
        }

        public bool IsUnmarked
        {
            get
            {
                return mark == PackageMark.Unmarked;
            }
        }

        public bool IsLocked
        {
            get
            {
                return Package != null ? Package.IsWanted : false;
            }
        }

        public bool IsActive
        {
            get
            {
                return Package != null ? Package.IsActive : false;
            }
        }

        public bool IsCoreCoAppPackage
        {
            get
            {
                return Name == "coapp";
            }
        }

        public bool CanBeMarked(PackageMark mark)
        {
            if (Status == PackageMark.Unmarked)
                return true;

            if (this.mark == mark)
                return false;

            if (this.mark != PackageMark.Unmarked && mark != PackageMark.Unmarked)
                return false;

            switch (mark)
            {
                case PackageMark.MarkedForUpdate:
                    return !(Status != PackageMark.InstalledUpdatable);
                case PackageMark.MarkedForInstallation:
                    return !(Status != PackageMark.NotInstalled &&
                             Status != PackageMark.NotInstalledNew);
                case PackageMark.MarkedForReinstallation:
                case PackageMark.MarkedForRemoval:
                    return !(Status != PackageMark.Installed &&
                             Status != PackageMark.InstalledUpdatable &&
                             Status != PackageMark.Broken) &&
                           !(IsActive && IsCoreCoAppPackage);
            }
            return true;
        }
    }

    public enum PackageMark
    {
        NotInstalled,
        NotInstalledLocked,
        NotInstalledNew,
        Installed,
        InstalledUpdatable,
        InstalledLocked,
        Broken,
        Unmarked,
        MarkedForInstallation,
        MarkedForReinstallation,
        MarkedForUpdate,
        MarkedForRemoval
    }
}
