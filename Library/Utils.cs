﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;

namespace Library
{
    /// <summary>
    /// A class containing utility methods for the application.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// A flag indicating whether or not logging is enabled.
        /// </summary>
        public static bool logs = true;

        /// <summary>
        /// The currently active user.
        /// </summary>
        public static string activeUser { get; internal set; }

        #region folder tree

        public class FolderViewModel
        {
            public string name { get; set; }
            public Guid uuid { get; set; }
            public ObservableCollection<FolderViewModel> children { get; set; }

            public static FolderViewModel? BuildFolderTree(List<Models.Folder> folders)
            {
                var lookup = folders.ToDictionary(f => f.uuid, f => new FolderViewModel
                {
                    name = f.name,
                    uuid = f.uuid,
                    children = new ObservableCollection<FolderViewModel>()
                });

                FolderViewModel? roots = null;

                foreach (var folder in folders)
                {
                    if (folder.parentFolderUuid == null)
                    {
                        roots = lookup[folder.uuid];
                    }
                    else if (lookup.TryGetValue(folder.parentFolderUuid.Value, out var parent))
                    {
                        parent.children.Add(lookup[folder.uuid]);
                    }
                }

                return roots;
            }
        }

        /// <summary>
        /// Adds child tree nodes to the specified tree view item.
        /// </summary>
        /// <param name="parent">The tree view item to add the child nodes to.</param>
        /// <param name="folderStructure">The list of folders to add as child nodes.</param>
        /// <returns>The parent tree view item.</returns>
        public static TreeViewItem addChildrenTreeNodes(this TreeViewItem parent, List<Models.Folder> folderStructure)
        {
            #region get child folders

            var childFolders = new List<Models.Folder>();

            foreach (var folder in folderStructure)
            {
                if (folder.parentFolderUuid is not null && folder.parentFolderUuid.Equals((Guid)parent.ToolTip))
                {
                    childFolders.Add(folder);
                }
            }

            #endregion get child folders

            foreach (var childFolder in childFolders)
            {
                var child = new TreeViewItem();
                child.Header = childFolder.name;
                child.Tag = childFolder.path + "/" + childFolder.name;
                child.ToolTip = childFolder.uuid;

                parent.Items.Add(addChildrenTreeNodes(child, folderStructure));
            }

            return parent;
        }

        #endregion folder tree

        /// <summary>
        /// Array of forbidden file extensions that cannot be uploaded to Infrakit.
        /// </summary>
        public static readonly string[] forbiddenFileExtensions =
        {
            ".bat",
            ".cmd",
            ".dll",
            ".exe",
            ".jar",
            ".js",
            ".msi",
            ".sh",
            ".so",
            ".vbs"
        };

        #region log in

        /// <summary>
        /// Logs the user into the application.
        /// </summary>
        /// <returns>True if the user was successfully logged in, false otherwise.</returns>
        public static bool logIn()
        {
            InputWindows.LogInWindow iw = new();

            bool? loggedIn = iw.ShowDialog();

            if (!loggedIn.HasValue || !loggedIn.Value)
            {
                Application.Current.Shutdown();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Encrypts the given string.
        /// </summary>
        /// <param name="sensitiveData">Data which should be encrypted</param>
        /// <returns>An protected / encrypted string</returns>
        public static string? protectData(string sensitiveData)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(sensitiveData);
                byte[] protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                string protectedDataAsString = Convert.ToBase64String(protectedData);
                return protectedDataAsString;
            }
            catch (CryptographicException e)
            {
                Log.write("utils.error.protectData: " + e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Decrypts the given string.
        /// </summary>
        /// <param name="protectedDataAsString">Data which should be decrypted</param>
        /// <returns>An unprotected / decrypted string</returns>
        public static string? unprotectData(string protectedDataAsString)
        {
            if (protectedDataAsString.Equals("")) return null;

            try
            {
                byte[] protectedData = Convert.FromBase64String(protectedDataAsString);
                byte[] data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
                string sensitiveData = Encoding.UTF8.GetString(data);
                return sensitiveData;
            }
            catch (CryptographicException e)
            {
                Log.write("utils.error.unprotectData: " + e.ToString());
                return null;
            }
        }

        #endregion log in

        #region resources

        /// <summary>
        /// A class for managing application resources.
        /// </summary>
        public static class Resources
        {
            /// <summary>
            /// The path to the resources directory.
            /// </summary>
            public static readonly string directory = Path.Combine(Environment.CurrentDirectory, "Resources");

            /// <summary>
            /// The path to the resources XML document.
            /// </summary>
            public static string path = Path.Combine(Resources.directory, "Resources.xml");

            /// <summary>
            /// Creates the default XML document.
            /// </summary>
            public static void create()
            {
                XmlDocument doc = new XmlDocument();
                var root = doc.CreateElement("body");
                doc.AppendChild(root);

                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

                doc.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Gets the resources XML document.
            /// </summary>
            /// <returns>The resources XML document.</returns>
            public static XmlDocument get()
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(Utils.Resources.path);

                return doc;
            }

            /// <summary>
            /// Gets the body element of an XML document.
            /// </summary>
            /// <param name="doc">The XML document. If null, the default XML document will be used.</param>
            /// <returns>The body element of the XML document, or null if the document is empty or does not contain a body element.</returns>
            public static XmlNode getBody(XmlDocument? doc = null)
            {
                if (doc is null)
                {
                    doc = Utils.Resources.get();
                }

                return doc.DocumentElement.SelectSingleNode("/body"); ;
            }

            /// <summary>
            /// Gets a resource from the resources XML document.
            /// </summary>
            /// <param name="name">The name of the resource to get.</param>
            /// <param name="doc">The XML document. If null, the default XML document will be used.</param>
            /// <returns>The resource, or null if the resource does not exist.</returns>
            public static XmlNode get(string name, XmlDocument? doc = null)
            {
                var node = Utils.Resources.getBody(doc);

                node = node.SelectSingleNode(name);

                return node;
            }

            /// <summary>
            /// Sets a resources in the resources XML document.
            /// </summary>
            /// <param name="name">The name of the resources to set.</param>
            /// <param name="parameter">The new value of the resources.</param>
            public static void set(string name, string parameter = "")
            {
                var resources = Resources.get();

                XmlNode node = resources.DocumentElement.SelectSingleNode("/body/" + name);

                node.RemoveAll();

                node.AppendChild(resources.CreateTextNode(parameter));

                resources.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Sets a resource in the resources XML document.
            /// </summary>
            /// <param name="resources">The resources XML document to set the resource in.</param>
            /// <param name="parameter">The resource to set.</param>
            public static void set(XmlDocument resources, XmlNode parameter)
            {
                if (parameter is null) { return; }

                XmlNode node = Resources.getBody(resources);

                node.RemoveAll();

                node.AppendChild(parameter);

                resources.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Adds a resource to the resources XML document.
            /// </summary>
            /// <param name="resources">The resources XML document to add the resource to.</param>
            /// <param name="parameter">The resource to add.</param>
            public static void add(XmlDocument resources, XmlNode parameter)
            {
                if (parameter is null) { return; }

                XmlNode node = Resources.getBody(resources);

                node.AppendChild(parameter);

                resources.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Edits a resource in the resources XML document.
            /// </summary>
            /// <param name="resources">The resources XML document to edit the resource in.</param>
            /// <param name="name">The name of the resource to edit.</param>
            /// <param name="parameter">The new resource value.</param>
            public static void edit(XmlDocument resources, XmlNode parameter, string name)
            {
                if (parameter is null) { return; }

                XmlNode node = Resources.get(name, doc: resources);

                node.RemoveAll();

                foreach (var child in parameter.ChildNodes)
                {
                    var childNode = (child as XmlNode).Clone();
                    node.AppendChild(childNode);
                }

                foreach (var attribute in parameter.Attributes)
                {
                    var attributeNode = (attribute as XmlAttribute).Clone() as XmlAttribute;

                    node.Attributes.Append(attributeNode);
                }

                resources.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Removes a resource from the resources XML document.
            /// </summary>
            /// <param name="name">The name of the resource to remove.</param>
            public static void remove(string name)
            {
                var resources = Utils.Resources.get();

                XmlNode parent = Resources.getBody(resources);

                XmlNode remove = Resources.get(name, resources);

                parent.RemoveChild(remove);

                resources.secureSave(Resources.directory, Resources.path);
            }

            /// <summary>
            /// Provides static methods for getting, setting, and adding attributes of resources in an XML document.
            /// </summary>
            public static class Attribute
            {
                /// <summary>
                /// Gets an attribute of a resource in the resource XML document.
                /// </summary>
                /// <param name="resource">The name of the resource to get the attribute of.</param>
                /// <param name="name">The name of the attribute to get.</param>
                /// <returns>The value of the attribute, or null if the attribute does not exist.</returns>
                public static string get(string resource, string name)
                {
                    var resources = Resources.get();
                    var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + resource);

                    return settingNode.Attributes[name].Value;
                }

                /// <summary>
                /// Sets an attribute of a resource in the resource XML document.
                /// </summary>
                /// <param name="resource">The name of the resource to set the attribute of.</param>
                /// <param name="name">The name of the attribute to set.</param>
                /// <param name="parameter">The new value of the attribute.</param>
                public static void set(string resource, string name, string parameter = "")
                {
                    var resources = Resources.get();
                    var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + resource);

                    settingNode.Attributes.RemoveAll();

                    var node = resources.CreateAttribute(name);
                    node.Value = parameter;
                    settingNode.Attributes.Append(node);

                    resources.secureSave(Resources.directory, Resources.path);
                }

                /// <summary>
                /// Adds an attribute of a resource in the resource XML document.
                /// </summary>
                /// <param name="resource">The name of the resource to add the attribute to.</param>
                /// <param name="name">The name of the attribute to add.</param>
                /// <param name="parameter">The new value of the attribute.</param>
                public static void add(string resource, string name, string parameter = "")
                {
                    var resources = Resources.get();
                    var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + resource);

                    var node = resources.CreateAttribute(name);
                    node.Value = parameter;
                    settingNode.Attributes.Append(node);

                    resources.secureSave(Resources.directory, Resources.path);
                }
            }
        }

        #endregion resources

        #region settings

        /// <summary>
        /// A class for managing application settings.
        /// </summary>
        public static class Settings
        {
            /// <summary>
            /// The path to the settings resources XML document.
            /// </summary>
            public static readonly string path = Path.Combine(Resources.directory, "Settings.xml");

            /// <summary>
            /// Gets a setting from the settings XML document.
            /// </summary>
            /// <param name="name">The name of the setting to get.</param>
            /// <returns>The value of the setting, or null if the setting does not exist.</returns>
            public static XmlDocument get()
            {
                XmlDocument doc = new XmlDocument();

                var save = false;
                XmlNode root;
                if (!File.Exists(Settings.path))
                {
                    root = doc.CreateElement("body");
                    doc.AppendChild(root);

                    XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                    doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

                    save = true;
                }
                else
                {
                    doc.Load(Settings.path);
                    root = doc.SelectSingleNode("body");
                }

                if (root.SelectSingleNode("username") is null)
                {
                    var username = doc.CreateElement("username");
                    root.AppendChild(username);

                    save = true;
                }

                if (root.SelectSingleNode("password") is null)
                {
                    var password = doc.CreateElement("password");
                    root.AppendChild(password);

                    save = true;
                }

                if (root.SelectSingleNode("language") is null)
                {
                    var language = doc.CreateElement("language");
                    root.AppendChild(language);

                    var languageText = doc.CreateTextNode("en");
                    language.AppendChild(languageText);

                    save = true;
                }

                if (save)
                {
                    doc.secureSave(Resources.directory, Settings.path);
                }

                return doc;
            }

            /// <summary>
            /// Gets a setting from the settings XML document.
            /// </summary>
            /// <param name="name">The name of the setting to get.</param>
            /// <returns>The value of the setting, or null if the setting does not exist.</returns>
            public static string get(string name)
            {
                var resources = Settings.get();

                XmlNode node = resources.DocumentElement.SelectSingleNode("/body/" + name);

                return node.InnerText;
            }

            /// <summary>
            /// Sets a setting in the settings XML document.
            /// </summary>
            /// <param name="name">The name of the setting to set.</param>
            /// <param name="parameter">The new value of the setting.</param>
            public static void set(string name, string parameter = "")
            {
                var resources = Settings.get();

                XmlNode node = resources.DocumentElement.SelectSingleNode("/body/" + name);

                node.RemoveAll();

                node.AppendChild(resources.CreateTextNode(parameter));

                resources.secureSave(Resources.directory, Settings.path);
            }

            /// <summary>
            /// Override a setting in the settings XML document.
            /// </summary>
            /// <param name="name">The name of the setting to override.</param>
            /// <param name="parameter">The new value of the setting.</param>
            public static void @override(string name, string parameter = "")
            {
                var resources = Settings.get();

                XmlNode? node = resources.DocumentElement.SelectSingleNode("/body/" + name);

                if(node is null)
                {
                    node = resources.CreateElement(name);

                    XmlNode? body = resources.DocumentElement.SelectSingleNode("/body");

                    if(body is null)
                    {
                        body = resources.CreateElement("body");
                        resources.AppendChild(body);
                    }

                    body.AppendChild(node);
                }
                else
                {
                    node.RemoveAll();
                }

                node.AppendChild(resources.CreateTextNode(parameter));

                resources.secureSave(Resources.directory, Settings.path);
            }

            /// <summary>
            /// Gets an attribute of a setting in the settings XML document.
            /// </summary>
            /// <param name="setting">The name of the setting to get the attribute of.</param>
            /// <param name="name">The name of the attribute to get.</param>
            /// <returns>The value of the attribute, or null if the attribute does not exist.</returns>
            public static string getAttribute(string setting, string name)
            {
                var resources = Settings.get();
                var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + setting);

                return settingNode.Attributes[name].Value;
            }

            /// <summary>
            /// Sets an attribute of a setting in the settings XML document.
            /// </summary>
            /// <param name="setting">The name of the setting to set the attribute of.</param>
            /// <param name="name">The name of the attribute to set.</param>
            /// <param name="parameter">The new value of the attribute.</param>
            public static void setAttribute(string setting, string name, string parameter = "")
            {
                var resources = Settings.get();
                var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + setting);

                settingNode.Attributes.RemoveAll();

                var node = resources.CreateAttribute(name);
                node.Value = parameter;
                settingNode.Attributes.Append(node);

                resources.secureSave(Resources.directory, Settings.path);
            }

            /// <summary>
            /// Adds an attribute of a setting in the settings XML document.
            /// </summary>
            /// <param name="setting">The name of the setting to add the attribute to.</param>
            /// <param name="name">The name of the attribute to add.</param>
            /// <param name="parameter">The new value of the attribute.</param>
            public static void addAttribute(string setting, string name, string parameter = "")
            {
                var resources = Settings.get();
                var settingNode = resources.DocumentElement.SelectSingleNode("/body/" + setting);

                var node = resources.CreateAttribute(name);
                node.Value = parameter;
                settingNode.Attributes.Append(node);

                resources.secureSave(Resources.directory, Settings.path);
            }
        }

        #endregion settings

        #region languages

        /// <summary>
        /// Sets the resource dictionary for the specified window to the specified language.
        /// </summary>
        /// <param name="w">The window to set the resource dictionary for.</param>
        /// <param name="language">The language to set the resource dictionary to.</param>
        public static void setRDict(this Window w, string? language = null)
        {
            if (language is null)
            {
                language = Language.get();
            }

            w.Resources.MergedDictionaries.Add(Language.getRDict(language));
            w.Resources.MergedDictionaries.Add(LibraryUtils.getRDict(language));
        }

        /// <summary>
        /// A class for managing the application language.
        /// </summary>
        public static class Language
        {
            /// <summary>
            /// The path to the languages directory.
            /// </summary>
            internal static readonly string directory = "..\\Resources\\Languages\\";

            /// <summary>
            /// Sets the language for the application.
            /// </summary>
            /// <param name="parameter">The language to set.</param>
            public static void set(string parameter)
            {
                Settings.set("language", parameter);
            }

            /// <summary>
            /// Gets the language setting.
            /// </summary>
            /// <returns>The language setting.</returns>
            public static string get()
            {
                try
                {
                    return Settings.get("language");
                }
                catch (Exception ex)
                {
                    Log.write("error.loading.language: " + ex.GetType() + " | " + ex.Message);

                    Settings.@override("language", "en");

                    var languages = LibraryUtils.getRDict();
                    MessageBox.Show(
                        languages["error.loading.message"].ToString(),
                        languages["error.loading.caption"].ToString(),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

                    return "en";
                }
            }

            /// <summary>
            /// Gets the resource dictionary for the specified language.
            /// </summary>
            /// <param name="language">The language to get the resource dictionary for.</param>
            /// <returns>The resource dictionary for the specified language, or null if the resource dictionary does not exist.</returns>
            public static ResourceDictionary getRDict(string? language = null)
            {
                if(language == null)
                {
                    language = Language.get();
                }

                return Language.getRDict(language, Language.directory);
            }

            /// <summary>
            /// Gets the general resource dictionary for the specified language.
            /// </summary>
            /// <param name="language">The language to get the general resource dictionary for.</param>
            /// <param name="path">The path to the directory containing the resource dictionaries. If path is null, then the default directory is used.</param>
            /// <returns>The general resource dictionary for the specified language, or null if the resource dictionary does not exist.</returns>
            internal static ResourceDictionary getRDict(string language, string path)
            {
                ResourceDictionary dict = new ResourceDictionary();

                switch (language)
                {
                    case "de":
                        dict.Source = new Uri(path + "German.xaml", UriKind.RelativeOrAbsolute);
                        break;

                    case "fr":
                        dict.Source = new Uri(path + "French.xaml", UriKind.RelativeOrAbsolute);
                        break;

                    case "fi":
                        dict.Source = new Uri(path + "Finnish.xaml", UriKind.RelativeOrAbsolute);
                        break;

                    case "pl":
                        dict.Source = new Uri(path + "Polish.xaml", UriKind.RelativeOrAbsolute);
                        break;

                    case "en":
                    default:
                        dict.Source = new Uri(path + "English.xaml", UriKind.RelativeOrAbsolute);
                        break;
                }

                return dict;
            }

            /// <summary>
            /// Gets the message for the specified key from the resource dictionary for the current language.
            /// </summary>
            /// <param name="key">The key of the message to get.</param>
            /// <returns>The message, or null if the message does not exist.</returns>
            public static string? getMessage(string key)
            {
                var rDict = Language.getRDict();

                return rDict[key].ToString();
            }

            /// <summary>
            /// Method to convert a timestamp (date only) to a string.
            /// </summary>
            /// <param name="dt">Timestamp</param>
            /// <param name="utc">Optional time zone information</param>
            /// <returns>Date of timestamp as string</returns>
            /// <remarks>
            /// This method covers language-specific formatting.
            /// </remarks>
            public static string getFormatedDate(DateTime dt, TimeZoneInfo? utc = null)
            {
                if (utc != null)
                {
                    dt = TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Utc, utc);
                }

                switch (Language.get())
                {
                    case "de":
                        return dt.ToString("ddd. dd.MM.yyyy", CultureInfo.CreateSpecificCulture("de-DE"));

                    case "fr":
                        return dt.ToString("ddd. dd/MM/yyyy", CultureInfo.CreateSpecificCulture("fr-FR"));

                    case "fi":
                        return dt.ToString("ddd. dd.MM.yyyy", CultureInfo.CreateSpecificCulture("fi-FI"));

                    case "pl":
                        return dt.ToString("ddd. dd.MM.yyyy", CultureInfo.CreateSpecificCulture("pl-PL"));

                    case "en":
                    default:
                        return dt.ToString("ddd. MM/dd/yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                }
            }

            /// <summary>
            /// Method to convert a timestamp (date and time) to a string.
            /// </summary>
            /// <param name="dt">Timestamp</param>
            /// <param name="utc">Optional time zone information</param>
            /// <returns>Date and time of timestamp as string</returns>
            /// <remarks>
            /// This method covers language-specific formatting.
            /// </remarks>
            public static string getFormatedDateTime(DateTime dt, TimeZoneInfo? utc)
            {
                if (utc != null)
                {
                    dt = TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Utc, utc);
                }

                switch (Language.get())
                {
                    case "de":
                        return dt.ToString("ddd. dd.MM.yyyy HH:mm (UTCzzz)", CultureInfo.CreateSpecificCulture("de-DE"));

                    case "fr":
                        return dt.ToString("ddd. dd/MM/yyyy HH:mm (UTCzzz)", CultureInfo.CreateSpecificCulture("fr-FR"));

                    case "fi":
                        return dt.ToString("ddd. dd.MM.yyyy HH:mm (UTCzzz)", CultureInfo.CreateSpecificCulture("fi-FI"));

                    case "pl":
                        return dt.ToString("ddd. dd.MM.yyyy HH:mm (UTCzzz)", CultureInfo.CreateSpecificCulture("pl-PL"));

                    case "en":
                    default:
                        return dt.ToString("ddd. MM/dd/yyyy hh:mm tt (UTCzzz)", CultureInfo.CreateSpecificCulture("en-US"));
                }
            }
        }

        #endregion languages

        #region log

        /// <summary>
        /// A class for logging application messages.
        /// </summary>
        public static class Log
        {
            /// <summary>
            /// The path to the logging directory.
            /// </summary>
            public static string directory = Path.Combine(Resources.directory, "Logs");

            /// <summary>
            /// Writes a log message to the log file for the current date.
            /// </summary>
            /// <param name="log">The log message to write.</param>
            public static void write(string log)
            {
                try
                {
                    if (!Utils.logs) return;

                    var now = DateTime.Now;

                    if (!Directory.Exists(Log.directory))
                    {
                        Directory.CreateDirectory(Log.directory);
                    }

                    var source = Path.Combine(Log.directory, now.ToString("yyyyMMdd") + ".log");

                    using (StreamWriter sw = File.AppendText(source))
                    {
                        sw.WriteLine(now.ToString("HH:mm:ss") + ": " + log);
                    }
                }
                catch
                {
                    var languages = LibraryUtils.getRDict();
                    MessageBox.Show(
                        languages["error.saving.message"].ToString(),
                        languages["error.saving.caption"].ToString(),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }

            /// <summary>
            /// Gets the log protocol for the specified date.
            /// </summary>
            /// <param name="date">The date to get the log protocol for.</param>
            /// <returns>A list of XML nodes representing the log protocol, or null if the log protocol does not exist for the specified date.</returns>
            public static List<XmlNode> get(DateOnly date)
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(Path.Combine(Log.directory, date.ToString("yyyyMMdd") + ".xml"));

                XmlNode body = doc.DocumentElement.SelectSingleNode("/body");

                if (!body.HasChildNodes) return null;

                var nodes = new List<XmlNode>();

                foreach (var child in body.ChildNodes)
                {
                    nodes.Add(child as XmlNode);
                }

                return nodes;
            }
        }

        #endregion log

        public static void secureSave(this XmlDocument resources, string directory, string path)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                resources.Save(path);
            }
            catch (Exception ex)
            {
                Log.write("utils.error.saving: " + ex.GetType() + " | " + ex.Message);

                var languages = LibraryUtils.getRDict();
                MessageBox.Show(
                    languages["error.saving.message"].ToString(),
                    languages["error.saving.caption"].ToString(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// A class that displays a message box that automatically closes after a specified timeout.
        /// </summary>
        public class AutoClosingMessageBox
        {
            /// <summary>
            /// The timer that controls the automatic closing of the message box.
            /// </summary>
            private Timer _timeoutTimer;

            /// <summary>
            /// The caption of the message box.
            /// </summary>
            private string _caption;

            /// <summary>
            /// Initializes a new instance of the AutoClosingMessageBox class.
            /// It displays a message box with the specified text, caption, and image.
            /// The message box will close after the specified timeout.
            /// </summary>
            /// <param name="text">The message to display.</param>
            /// <param name="caption">The title of the message box.</param>
            /// <param name="image">The icon to display in the message box.</param>
            /// <param name="timeout">The time after which the message box will close.</param>
            private AutoClosingMessageBox(string text, string caption, MessageBoxImage image, TimeSpan timeout)
            {
                _caption = caption;

                _timeoutTimer = new Timer(
                    this.OnTimerElapsed,
                    null,
                    timeout,
                    timeout
                );

                using (_timeoutTimer)
                    MessageBox.Show(
                        text,
                        caption,
                        MessageBoxButton.OK,
                        image);
            }

            /// <summary>
            /// Displays an auto-closing message box with the specified text, caption, and image.
            /// The message box will close after the specified timeout.
            /// If the 'newThread' parameter is true, it creates and starts a new thread to display the message box.
            /// </summary>
            /// <param name="text">The message to display.</param>
            /// <param name="caption">The title of the message box.</param>
            /// <param name="image">The icon to display in the message box.</param>
            /// <param name="timeout">The time after which the message box will close. If not specified, defaults to 15 minutes.</param>
            /// <param name="newThread">Whether to display the message box in a new thread. If not specified, defaults to false.</param>
            public static void Show(string text, string caption, MessageBoxImage image, TimeSpan? timeout = null, bool newThread = false)
            {
                if(!timeout.HasValue)
                {
                    timeout = new TimeSpan(0, 15, 0);
                }

                if(newThread)
                {
                    var thread = new Thread(() =>
                    {
                        new AutoClosingMessageBox(text, caption, image, timeout.Value);
                    });
                    thread.Start();
                }
                else
                {
                    new AutoClosingMessageBox(text, caption, image, timeout.Value);
                }
            }

            /// <summary>
            /// The timer elapsed event handler.
            /// </summary>
            /// <param name="state">The state object.</param>
            private void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }

            private const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        }
    }
}