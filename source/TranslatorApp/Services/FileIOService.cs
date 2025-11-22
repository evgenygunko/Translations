namespace TranslatorApp.Services
{
    public interface IFileIOService
    {
        Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

        Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);
    }

    public class FileIOService : IFileIOService
    {
        public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
            => await File.ReadAllBytesAsync(path, cancellationToken);

        public async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
            => await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }
}
