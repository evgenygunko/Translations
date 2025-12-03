using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using TranslatorApp.Exceptions;

namespace TranslatorApp.Services
{
    public interface ILaunchDarklyService
    {
        void Initialize(string sdkKey);

        bool GetBooleanFlag(string flagKey, bool defaultValue = false);

        string GetStringFlag(string flagKey, string defaultValue = "");

        bool IsInitialized { get; }
    }

    public class LaunchDarklyService : ILaunchDarklyService, IDisposable
    {
        private ILdClient? _client;
        private bool _disposed;

        public bool IsInitialized => _client?.Initialized ?? false;

        public void Initialize(string sdkKey)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrWhiteSpace(sdkKey))
            {
                throw new ArgumentException("SDK key cannot be null or empty.", nameof(sdkKey));
            }

            try
            {
                var ldConfig = Configuration.Default(sdkKey);
                _client = new LdClient(ldConfig);
            }
            catch (Exception ex)
            {
                throw new LaunchDarklyInitializationException("An error occurred while initializing LaunchDarkly.", ex);
            }
        }

        public bool GetBooleanFlag(string flagKey, bool defaultValue = false)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_client == null || !_client.Initialized)
            {
                // Log the error but continue with default value
                Console.WriteLine($"LaunchDarkly client must be initialized before getting flags. Returning default value '{defaultValue}'.");
                return defaultValue;
            }

            try
            {
                var context = Context.New(ContextKind.Default, "TranslationApp");
                return _client.BoolVariation(flagKey, context, defaultValue);
            }
            catch (Exception ex)
            {
                throw new LaunchDarklyGetFlagException($"Failed to get boolean flag '{flagKey}': {ex.Message}", ex);
            }
        }

        public string GetStringFlag(string flagKey, string defaultValue = "")
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_client == null || !_client.Initialized)
            {
                // Log the error but continue with default value
                Console.WriteLine($"LaunchDarkly client must be initialized before getting flags. Returning default value '{defaultValue}'.");
                return defaultValue;
            }

            try
            {
                var context = Context.New(ContextKind.Default, "TranslationApp");
                return _client.StringVariation(flagKey, context, defaultValue);
            }
            catch (Exception ex)
            {
                throw new LaunchDarklyGetFlagException($"Failed to get string flag '{flagKey}': {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    (_client as IDisposable)?.Dispose();
                    _client = null;
                }

                _disposed = true;
            }
        }
    }
}
