using System.Threading.Channels;

namespace BSL.App.Service
{
    public record FileProcessingItem(string FilePath);

    public class FileProcessingQueue
    {
        private readonly Channel<FileProcessingItem> channel =
            Channel.CreateUnbounded<FileProcessingItem>();

        public async Task Add(FileProcessingItem item)
            => await channel.Writer.WriteAsync(item);

        public async Task<FileProcessingItem> Get(CancellationToken cancellationToken)
            => await channel.Reader.ReadAsync(cancellationToken);
    }
}
