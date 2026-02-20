#if POLYFILL_ZLOGGER_INTEGRATION
using UnityEngine;

using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

using ZLogger;
using ZLogger.Unity;
using ZLogger.Providers;
using Utf8StringInterpolation;

// Unity does not find MonoBehaviour scripts with file-scoped namespace (namespace X;). Use block namespace.
namespace xpTURN.Polyfill.Samples
{
    public class TestZLogger : MonoBehaviour
    {
        ILogger logger;

        int frame = 0;

        void Start()
        {
            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddZLoggerUnityDebug(); // log to UnityDebug

                // output to  yyyy-MM-dd_*.log, roll by 1MB or changed date
                logging.AddZLoggerRollingFile(options =>
                {
                    options.FilePathSelector = (dt, index) => $"Logs/ZLogger/Client_{dt:yyyy-MM-dd-HH-mm-ss}_{index}.log";
                    options.RollingInterval = RollingInterval.Day;
                    options.RollingSizeKB = 1024 * 1024;
                    options.UsePlainTextFormatter(formatter =>
                    {
                        formatter.SetPrefixFormatter($"{0}|{1:short}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                        formatter.SetSuffixFormatter($" ({0})", (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));
                        formatter.SetExceptionFormatter((writer, ex) => Utf8String.Format(writer, $"{ex.Message}"));
                    });
                });
            });

            logger = loggerFactory.CreateLogger("Client");
            logger.ZLogInformation($"Start Logging!");
        }

        void Update()
        {
            ++frame;
            logger?.ZLogInformation($"Update Frame: {frame}");
        }
    }
}
#endif