using Gelf.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace PaderbornUniversity.SILab.Hip.Webservice.Logging
{
    public static class LoggerFactoryExtensions
        {
        public static ILoggerFactory AddHipLogger (this ILoggerFactory loggerFactory, HipLoggingConfig config)
            {
            return loggerFactory.AddGelf (new GelfLoggerOptions
                {
                Host = config.Host,
                Port = config.Port,
                LogSource = config.LogSource
                });
            }
        }
    }
