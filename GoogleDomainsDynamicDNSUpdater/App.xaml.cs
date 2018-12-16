using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using log4net.Config;

namespace GoogleDomainsDynamicDNSUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon TrayIcon;
        private ConfigWindow ConfigWindow;
        private ObservableCollection<Domain> Domains;

        /// <summary>
        /// Startup and initialize the application.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Call the base startup code
            base.OnStartup(e);

            // Setup logging
            XmlConfigurator.Configure(new System.IO.FileInfo(Assets.LoggingConfigurationFile));

            // Load configuration from disk and save it when it changes
            Domains = new ObservableCollection<Domain>();
            Domains.CollectionChanged += Domains_CollectionChanged;
            foreach(Domain domain in LoadConfiguration(Assets.ConfigurationFile))
            {
                Domains.Add(domain);
                domain.Initialized = true;
            }

            // Initialize Tray Icon
            System.Drawing.Icon icon;
            using (Stream iconStream = GetResourceStream(new Uri("pack://application:,,,/GoogleDomainsDynamicDNSUpdater;component/Assets/IconWhite.ico")).Stream)
            {
                icon = new System.Drawing.Icon(iconStream);
            }

            TrayIcon = new System.Windows.Forms.NotifyIcon()
            {
                Icon = icon,
                ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] {
                    new System.Windows.Forms.MenuItem("Edit Configuration", EditConfig),
                    new System.Windows.Forms.MenuItem("Exit", Close)
                }),
                Visible = true
            };
            TrayIcon.MouseClick += EditConfig;
        }

        /// <summary>
        /// Configure new domains
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Domains_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs eventArgs)
        {
            if (eventArgs.NewItems != null)
            {
                foreach (Domain domain in eventArgs.NewItems)
                {
                    domain.ErrorOccured += Domain_ErrorOccured;
                    domain.Initialized = true;
                }
            }
        }

        /// <summary>
        /// Handle a domain update error.
        /// </summary>
        /// <param name="error"></param>
        private void Domain_ErrorOccured(string error)
        {
            ToastManager.Toast("An error occured!", error, EditConfig);
        }

        /// <summary>
        /// Event handler to edit the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void EditConfig(object sender, object eventArgs)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (ConfigWindow == null)
                {
                    ConfigWindow = new ConfigWindow(Domains);
                    ConfigWindow.Closing += delegate { ConfigWindow = null; };
                    ConfigWindow.Show();
                }
                else
                {
                    if (ConfigWindow.WindowState == WindowState.Minimized)
                    {
                        ConfigWindow.WindowState = WindowState.Normal;
                    }
                    ConfigWindow.Topmost = true;
                    ConfigWindow.Focus();
                }
            });
        }

        /// <summary>
        /// Event handler to close the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            TrayIcon.Visible = false;

            Shutdown();
        }

        /// <summary>
        /// Override OnExit to save the configuration before closing the application
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            // Save the configuration before closeing
            SaveConfiguration(Assets.ConfigurationFile, Domains);

            // Call the base impl
            base.OnExit(e);
        }

        /// <summary>
        /// Helper method to save the configuration
        /// </summary>
        /// <param name="path">The file to save the configuration to.</param>
        /// <param name="domains">The domains save</param>
        private static void SaveConfiguration(string path, ObservableCollection<Domain> domains)
        {
            // Create the directory if it doesn't exist
            try
            {
                Directory.GetParent(path).Create();

                // Save out the config file
                var serializer = new XmlSerializer(typeof(ObservableCollection<Domain>));
                using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(stream, domains);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed to save the configuration to {0} due to the exception {1}", path, e.Message));
            }
        }

        /// <summary>
        /// Helper method to load the configuration
        /// </summary>
        /// <param name="path">The file to load the configuration from.</param>
        /// <returns>Domains from a config file</returns>
        private static ObservableCollection<Domain> LoadConfiguration(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ObservableCollection<Domain>));
                using (var stream = File.OpenRead(path))
                {
                    return (ObservableCollection<Domain>)(serializer.Deserialize(stream));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed to load the configuration from {0} due to the exception {1}", path, e.Message));
                return new ObservableCollection<Domain>();
            }
        }
    }
}
