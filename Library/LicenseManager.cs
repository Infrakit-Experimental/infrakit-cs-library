using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace Library
{
    public sealed class License
    {
        public static string directory = Path.Combine(Utils.Resources.directory, "License");
        public static string appDirectory = $"{Utils.Resources.appDirectory}/License";

        public static License? manager;

        private readonly RSA publicKey;
        private readonly Assembly assembly;

        public License(Assembly assembly)
        {
            this.assembly = assembly;
            this.publicKey = this.loadPublicKey();

            if (!Directory.Exists(License.directory))
            {
                Directory.CreateDirectory(License.directory);
            }
        }

        private RSA loadPublicKey()
        {
            //var assembly = Assembly.GetEntryAssembly();
            //var resourceName = $"{assembly.GetName().Name}.Resources.License.public_key.pem";

            //using Stream stream = assembly.GetManifestResourceStream(resourceName);
            //if (stream == null)
            //    // TODO: display message
            //    throw new Exception($"Public Key Ressource '{resourceName}' nicht gefunden.");

            var resourceName = this.assembly
                .GetManifestResourceNames()
                .Single(r => r.EndsWith("public_key.pem"));

            using var stream = this.assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Public Key Resource not found");

            using StreamReader reader = new StreamReader(stream);
            string publicKeyPem = reader.ReadToEnd();

            RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return rsa;
        }

        public bool validateLicense(bool shutDown = true)
        {
            bool isValid = false;

            while (!isValid)
            {
                isValid = this.isValid();

                if (isValid) break;

                var languages = LibraryUtils.getRDict();
                MessageBoxResult confirmation = MessageBox.Show(
                    languages["license.new.message"].ToString(),
                    languages["license.new.caption"].ToString(),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (confirmation != MessageBoxResult.Yes) break;

                this.addLicense();
            }

            if (!isValid && shutDown)
            {
                Application.Current.Shutdown();
            }

            return isValid;
        }

        private bool isValid()
        {
            LicenseData? data = this.loadLicense();

            if (data is null)
            {
                // TODO: display message
                return false;
            }

            return this.verifyLicense(data);
        }

        private LicenseData? loadLicense()
        {
            string[] licenseFiles = Directory.GetFiles(License.directory, "*.lic");

            if (licenseFiles.Length == 0)
            {
                // TODO: display message
                return null;
            }

            if (licenseFiles.Length > 1)
            {
                // TODO: display message
                return null;
            }

            var licensePath = licenseFiles[0];

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Load outer structure
            var licenseJson = File.ReadAllText(licensePath);
            var licensePackage = JsonSerializer.Deserialize<LicensePackage>(licenseJson, options);
            if (licensePackage == null)
            {
                // TODO: display message
                return null;
            }

            // Parse inner data from the string
            var licenseData = JsonSerializer.Deserialize<LicenseData>(licensePackage.data);
            if (licenseData == null)
            {
                // TODO: display message
                return null;
            }

            LicenseData? data = JsonSerializer.Deserialize<LicenseData>(licensePackage.data);

            if (data == null)
            {
                // TODO: display message
                return null;
            }

            return data;
        }

        private bool verifyLicense(LicenseData data)
        {
            if (!this.hasData(data))
            {
                // TODO: display message
                return false;
            }

            string customer = data.customer;
            DateTime begins = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data.begins)).UtcDateTime;
            DateTime expires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data.expires)).UtcDateTime;

            if (!this.inTimeSlot(begins, expires))
            {
                // TODO: display message
                return false;
            }

            return true;
        }

        private bool hasData(LicenseData data)
        {
            if (data.begins == null) return false;

            if (data.expires == null) return false;

            return true;
        }

        private bool inTimeSlot(DateTime begins, DateTime expires)
        {
            Models.Time? ikTime = API.getTime();

            if (ikTime is null) return false;

            DateTime utcNow = ikTime.utc;

            #pragma warning disable CS8629
            if (utcNow < begins) return false;

            if (utcNow > expires) return false;
            #pragma warning restore CS8629

            return true;
        }

        private void addLicense()
        {
            var languages = LibraryUtils.getRDict();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = languages["license.select.title"].ToString(),
                Filter = languages["license.select.filter"].ToString() + " (*.lic)|*.lic",
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == false) return;

            string oldLicensesDirectory = Path.Combine(License.directory, "Old Licences");

            foreach (string file in Directory.GetFiles(License.directory, "*", SearchOption.TopDirectoryOnly))
            {
                string destinationPath = Path.Combine(
                    oldLicensesDirectory,
                    Path.GetFileName(file)
                );

                // Falls dort bereits eine Datei existiert, eindeutigen Namen erzeugen
                if (File.Exists(destinationPath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    destinationPath = Path.Combine(
                        oldLicensesDirectory,
                        $"{fileNameWithoutExt}_{timeStamp}{extension}"
                    );
                }

                File.Move(file, destinationPath);
            }

            string sourceFilePath = openFileDialog.FileName;
            string fileName = Path.GetFileName(sourceFilePath);
            string targetFilePath = Path.Combine(License.directory, fileName);

            File.Copy(sourceFilePath, targetFilePath, overwrite: true);
        }

        private sealed class LicensePackage
        {
            public string data { get; set; }
            public string signature { get; set; }

        }

        private sealed class LicenseData
        {
            public string customer { get; set; }
            public string begins { get; set; }
            public string expires { get; set; }
            public string product { get; set; }
        }
    }

}
