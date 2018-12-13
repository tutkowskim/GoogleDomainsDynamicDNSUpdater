using System;
using System.IO;

namespace GoogleDomainsDynamicDNSUpdater
{
    /// <summary>
    /// Static class that resolves the path to file assets.
    /// </summary>
    internal static class Assets
    {
        /// <summary>
        /// The install directory of this application.
        /// </summary>
        public static string InstallDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// The logo for this application.
        /// </summary>
        public static string IconImagePath
        {
            get
            {
                return Path.Combine(InstallDirectory, "IconWhite.png");
            }
        }

        /// <summary>
        /// The user specific configuration file for this application.
        /// </summary>
        public static string ConfigurationFile
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoogleDomainsDynamicDNSUpdater", "Config.xml");
            }
        }
    }
}