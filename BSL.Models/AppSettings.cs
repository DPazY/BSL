using BSL.Models.Enum;

namespace BSL.Models
{
    public record AppSettings(
        string WorkDirectory,
        string FileWatcherDirectory,
        ProcessedFileAction ProcessedFileAction,
        double PrefetchThreshold = 150,
        string FileExtension = ".xml"
        );
    
}
