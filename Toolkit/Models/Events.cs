using CoApp.Packaging.Common;

namespace CoApp.Mg.Toolkit.Models
{
    public class PackageEvent
    {
    }

    public class ActivityEvent
    {
        public CanonicalName CanonicalName { get; protected set; }
        public int Progress { get; protected set; }
        public string Message { get; protected set; }
    }

    public class ErrorEvent : ActivityEvent
    {
        public ErrorEvent(string message)
        {
            Message = message;
        }
    }

    public class DownloadEvent : ActivityEvent
    {
        public DownloadEvent(CanonicalName canonicalName, int progress)
        {
            CanonicalName = canonicalName;
            Progress = progress;
        }
    }

    public class InstallEvent : ActivityEvent
    {
        public InstallEvent(CanonicalName canonicalName, int progress)
        {
            CanonicalName = canonicalName;
            Progress = progress;
        }
    }

    public class RemoveEvent : ActivityEvent
    {
        public RemoveEvent(CanonicalName canonicalName, int progress)
        {
            CanonicalName = canonicalName;
            Progress = progress;
        }
    }
}
