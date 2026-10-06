using System;
using System.IO;

namespace DevMonitor
{
    internal static class SensorGuard
    {
        public static string BlockedStep(string sensor)
        {
            string path = MarkerPath(sensor);
            if (!File.Exists(path)) return null;
            try
            {
                string step = File.ReadAllText(path).Trim();
                return step.Length > 0 ? step : "неизвестный шаг";
            }
            catch (IOException)
            {
                return "неизвестный шаг";
            }
            catch (UnauthorizedAccessException)
            {
                return "неизвестный шаг";
            }
        }

        public static void Enter(string sensor, string step)
        {
            try
            {
                Directory.CreateDirectory(UserDataPaths.Directory);
                File.WriteAllText(MarkerPath(sensor), step);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static void Leave(string sensor)
        {
            try
            {
                File.Delete(MarkerPath(sensor));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string MarkerPath(string sensor)
        {
            return Path.Combine(UserDataPaths.Directory, "sensor-" + sensor + ".pending");
        }
    }
}
