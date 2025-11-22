using FFMpegCore;
using FFMpegCore.Enums;

namespace TranslatorApp.Services
{
    public interface IFFMpegWrapper
    {
        Task<bool> ExtractAudioAsync(string inputFilePath, string outputFilePath);
    }

    public class FFMpegWrapper : IFFMpegWrapper
    {
        public async Task<bool> ExtractAudioAsync(string inputFilePath, string outputFilePath)
        {
            return await FFMpegArguments
                .FromFileInput(inputFilePath)
                .OutputToFile(outputFilePath, true, options => options
                    .DisableChannel(Channel.Video))
                .ProcessAsynchronously();
        }
    }
}
