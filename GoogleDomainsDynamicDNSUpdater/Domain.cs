﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace GoogleDomainsDynamicDNSUpdater
{
    public class Domain
    {
        private Timer timer;
        private static readonly HttpClient client = new HttpClient();

        public Domain()
        {
            timer = new Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = 1 * 1000 * 60 * 60, // 1 hour
            };
            timer.Elapsed += new ElapsedEventHandler(UpdateDomainAsync);
        }

        [XmlElement("Enabled")]
        public bool Enabled
        {
            get
            {
                if (timer != null)
                {
                    return timer.Enabled;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (timer != null)
                {
                    if (timer.Enabled != value)
                    {
                        timer.Enabled = value;

                        if (value == true)
                        {
                            UpdateDomain();
                        }
                    }

                }
            }
        }

        [XmlElement("DomainUrl")]
        public string DomainUrl { get; set; } = string.Empty;

        [XmlElement("UpdateInterval")]
        public double UpdateInterval
        {
            get
            {
                if (timer != null)
                {
                    return timer.Interval / 1000 / 60 / 60; // Convert ms to hours
                }
                else
                {
                    return -1;
                }
            }
            set
            {
                if (timer != null)
                {
                    timer.Interval = value * 60 * 60 * 100; // Convert hours to ms
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
        /// Send an update to google domains with our current IP address
        /// </summary>
        private void UpdateDomain()
        {
            UpdateDomainAsync(null, null);
        }

        /// <summary>
        /// Send an update to google domains with our current IP address using
        /// the rest api: https://support.google.com/domains/answer/6147083?hl=en
        /// </summary>
        /// <param name="sender">The timer that caused this event.</param>
        /// <param name="e">Timer event args/</param>
        private async void UpdateDomainAsync(object sender, ElapsedEventArgs e)
        {
            var url = "https://domains.google.com/nic/update";

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
                var RequestUri = response.RequestMessage.RequestUri;
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine(RequestUri);
                Console.WriteLine(responseString);
            }
        }
    }
}