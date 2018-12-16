using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace GoogleDomainsDynamicDNSUpdater
{
    public class Domain : INotifyPropertyChanged
    {
        private Timer timer;
        private static readonly HttpClient client = new HttpClient();

        public delegate void ErrorOccuredEventHandler(string error);
        public event ErrorOccuredEventHandler ErrorOccured;

        public event PropertyChangedEventHandler PropertyChanged;

        public Domain()
        {
            timer = new Timer()
            {
                AutoReset = true,
                Enabled = false,
            };
            timer.Elapsed += new ElapsedEventHandler(UpdateDomainAsync);
        }

        private bool _initialized = false;
        [XmlIgnore]
        public bool Initialized
        {
            get
            {
                return _initialized;
            }
            set
            {
                if (_initialized != value)
                {
                    _initialized = value;
                    UpdateTimer();
                }
            }
        }

        private bool _enabled = false;
        [XmlElement("Enabled")]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                    UpdateTimer();
                }
            }
        }

        [XmlElement("DomainUrl")]
        public string DomainUrl { get; set; } = string.Empty;

        private double _updateInterval = 1;
        [XmlElement("UpdateInterval")]
        public double UpdateInterval
        {
            get
            {
                return _updateInterval;
            }
            set
            {
                if (_updateInterval != value)
                {
                    _updateInterval = value;
                    UpdateTimer();
                }
            }
        }

        [XmlElement("Username")]
        public string Username { get; set; } = string.Empty;

        [XmlElement("Entropy")]
        public byte[] Entropy { get; set; } = new byte[0];

        [XmlElement("CipherText")]
        public byte[] Ciphertext { get; set; } = new byte[0];

        [XmlIgnore]
        public string Password
        {
            get
            {
                if (Entropy.Length == 0 || Ciphertext.Length == 0)
                {
                    // The password was never set. Returing an empty string.
                    return string.Empty;
                }

                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Ciphertext, Entropy, DataProtectionScope.CurrentUser));
            }
            set
            {
                // Data to protect. Convert a string to a byte[] using Encoding.UTF8.GetBytes().
                byte[] plaintext = Encoding.UTF8.GetBytes(value);

                // Generate additional entropy (will be used as the Initialization vector)
                Entropy = new byte[20];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(Entropy);
                }

                Ciphertext = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
            }
        }

        /// <summary>
        /// Get the user's password from the protected data
        /// </summary>
        /// <returns>The password</returns>
        public SecureString GetSecurePassword()
        {
            if (Entropy.Length == 0 || Ciphertext.Length == 0)
            {
                // The password was never set. Returing an empty string.
                return new SecureString();
            }

            SecureString securePassword = new SecureString();
            byte[] bytes = ProtectedData.Unprotect(Ciphertext, Entropy, DataProtectionScope.CurrentUser);
            foreach (byte b in bytes)
            {
                securePassword.AppendChar(Convert.ToChar(b));
            }

            return securePassword;
        }

        /// <summary>
        /// Configure the timer for updates
        /// </summary>
        private void UpdateTimer()
        {
            if (timer != null)
            {
                timer.Interval = UpdateInterval * 60 * 60 * 1000; // Hours to ms
                timer.Enabled = Initialized && Enabled;
            }

            // Send out an update right away
            UpdateDomain();
        }

        /// <summary>
        /// Send an update to google domains with our current IP address using
        /// the rest api: https://support.google.com/domains/answer/6147083?hl=en
        /// </summary>
        private void UpdateDomain()
        {
            if (Initialized && Enabled)
            {
                UpdateDomainAsync(null, null);
            }
        }

        /// <summary>
        /// Send an update to google domains with our current IP address using
        /// the rest api: https://support.google.com/domains/answer/6147083?hl=en
        /// </summary>
        /// <param name="sender">The timer that caused this event.</param>
        /// <param name="eventArgs">Timer event args/</param>
        private async void UpdateDomainAsync(object sender, ElapsedEventArgs eventArgs)
        {
            try
            {
                const string url = "https://domains.google.com/nic/update";

                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{Username}:{Password}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var values = new Dictionary<string, string>
                {
                    { "hostname", DomainUrl },
                };

                    var content = new FormUrlEncodedContent(values);
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!responseString.ToLower().Contains("good") && !responseString.ToLower().Contains("nochg"))
                    {
                        // The client encountered a failure case.
                        HandleError($"Unexpected response: {responseString}");
                    }
                }
            }
            catch (Exception e)
            {
                HandleError(e.Message);
            }
        }

        void HandleError(string error)
        {
            // Stop the timer when an error is encoutered
            Enabled = false;
            ErrorOccured?.Invoke(error);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}