using System;
using System.IO;

namespace DevMonitor
{
    internal static class UserDataPaths
    {
        public static readonly string Directory = AppIdentity.DataDirectory;

        public static string Settings
        {
            get { return Path.Combine(Directory, "settings.txt"); }
        }

        public static string Position
        {
            get { return Path.Combine(Directory, "position.txt"); }
        }

        public static string History
        {
            get { return Path.Combine(Directory, "data"); }
        }
    }
}
