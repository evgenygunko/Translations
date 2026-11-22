using FFMpegCore;

namespace TranslatorApp.Services
{
    public interface IFFMpegWrapper
    {
        Task<FFMpegResult> ExtractAudioAsync(string inputFilePath, string outputFilePath);
    }

    public class FFMpegWrapper : IFFMpegWrapper
    {
        public Task<FFMpegResult> ExtractAudioAsync(string inputFilePath, string outputFilePath)
        {
            return FFMpegArguments
                .FromFileInput(inputFilePath)
                .OutputToFile(outputFilePath, true, options => options
                    .DisableVideo())
                .ProcessAsynchronously();
        }
    }
}
