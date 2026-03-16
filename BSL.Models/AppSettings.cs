using BSL.Models.Enum;

namespace BSL.Models
{
    public record AppSettings(
        string WorkDirectory,
        string FileWatcherDirectory,
        ProcessedFileAction ProcessedFileAction,
        string FileExtension = ".xml"
        );
}
