using System;
using System.IO;
using Serilog;

namespace MangaSauceBot
{
    public static class DotEnv
    {
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Warning("No .env file found");
                return;
            }
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split(
                    '=',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Log.Information("Setting env var {Key}", parts[0]);
                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }

        public static string Get(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        public static long? GetAsLong(string key)
        {
            var value = Get(key);
            if(value != null && long.TryParse(value.Trim(), out var output)) 
            {
                return output;
            }
            return null;
        }

        public static int? GetAsInt(string key)
        {
            var value = Get(key);
            if (value != null && int.TryParse(value.Trim(), out var output))
            {
                return output;
            }
            return null;
        }

        public static bool GetAsBool(string key)
        {
            var value = Get(key);
            bool.TryParse(value, out var result);
            return result;
        }
    }
}