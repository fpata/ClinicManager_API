using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ClinicManager
{
    public static class AppConfigHelper
    {
        private static IConfiguration? _modernConfig;
        private static readonly Configuration? _legacyConfig;

        static AppConfigHelper()
        {
            try
            {
                if (IsRunningFromTest())
                {
                    return;
                }

                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                string fileName = $"app.{env}.config";
                string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    // Fallback to app.config
                    fileName = "app.config";
                    filePath = Path.Combine(AppContext.BaseDirectory, fileName);
                }

                if (File.Exists(filePath))
                {
                    var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = filePath };
                    _legacyConfig = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AppConfigHelper: {ex.Message}");
            }
        }

        public static void Initialize(IConfiguration configuration)
        {
            _modernConfig = configuration;
        }

        private static bool IsRunningFromTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var name = assemblies[i].FullName;
                if (name != null && (name.Contains("xunit") || name.Contains("Microsoft.TestPlatform") || name.Contains("testhost") || name.Contains("VisualStudio")))
                {
                    return true;
                }
            }
            return false;
        }

        public static string? GetAppSetting(string key)
        {
            // 1. Try modern Configuration (User Secrets, Env Vars, appsettings.json)
            if (_modernConfig != null)
            {
                var val = _modernConfig[key];
                if (!string.IsNullOrEmpty(val) && !val.Contains("_PLACEHOLDER"))
                {
                    return val;
                }
            }

            // 2. Try raw Environment Variable directly
            var envVal = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envVal) && !envVal.Contains("_PLACEHOLDER"))
            {
                return envVal;
            }

            // 3. Fall back to legacy XML config files
            if (_legacyConfig != null)
            {
                var val = _legacyConfig.AppSettings.Settings[key]?.Value;
                if (!string.IsNullOrEmpty(val) && !val.Contains("_PLACEHOLDER"))
                {
                    return val;
                }
            }

            var defaultVal = System.Configuration.ConfigurationManager.AppSettings[key];
            if (defaultVal != null && defaultVal.Contains("_PLACEHOLDER"))
            {
                return null;
            }
            return defaultVal;
        }

        public static string? GetConnectionString(string name)
        {
            // 1. Try modern Configuration connection strings
            if (_modernConfig != null)
            {
                var val = _modernConfig.GetConnectionString(name);
                if (!string.IsNullOrEmpty(val) && !val.Contains("_PLACEHOLDER"))
                {
                    return val;
                }

                // Try key directly
                val = _modernConfig[$"ConnectionStrings:{name}"];
                if (!string.IsNullOrEmpty(val) && !val.Contains("_PLACEHOLDER"))
                {
                    return val;
                }
            }

            // 2. Try raw Environment Variable directly
            var envVal = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(envVal) && !envVal.Contains("_PLACEHOLDER"))
            {
                return envVal;
            }

            envVal = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");
            if (!string.IsNullOrEmpty(envVal) && !envVal.Contains("_PLACEHOLDER"))
            {
                return envVal;
            }

            // 3. Fall back to legacy XML config files
            if (_legacyConfig != null)
            {
                var val = _legacyConfig.ConnectionStrings.ConnectionStrings[name]?.ConnectionString;
                if (!string.IsNullOrEmpty(val) && !val.Contains("_PLACEHOLDER"))
                {
                    return val;
                }
            }

            var defaultVal = System.Configuration.ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
            if (defaultVal != null && defaultVal.Contains("_PLACEHOLDER"))
            {
                return null;
            }
            return defaultVal;
        }
    }
}
