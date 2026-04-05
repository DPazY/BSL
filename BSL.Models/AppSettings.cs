using BSL.Models.Enum;

namespace BSL.Models
{
    public record AppSettings(
        string WorkDirectory,
        string FileWatcherDirectory,
        ProcessedFileAction ProcessedFileAction,
        double PrefetchThreshold = 5.0,
        string FileExtension = ".xml"
        );
    
}
