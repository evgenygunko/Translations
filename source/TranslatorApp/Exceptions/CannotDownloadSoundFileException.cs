namespace TranslatorApp.Exceptions
{
    public class CannotDownloadSoundFileException : Exception
    {
        public CannotDownloadSoundFileException()
        {
        }

        public CannotDownloadSoundFileException(string message)
            : base(message)
        {
        }

        public CannotDownloadSoundFileException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
