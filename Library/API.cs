﻿using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Security.Cryptography;
using Library.Models;
using static Library.Utils;
using System.Text;
using Library.Resources;
using System.Windows.Interop;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Media.TextFormatting;

namespace Library
{
    /// <summary>
    /// A class that provides access to the InfraKit API.
    /// </summary>
    public static class API
    {
        #region variables

        private static string auth = "https://app.infrakit.com/kuura/";
        private static string uri = API.auth + "v1/";
        private static Environment _selectedEnv = Environment.Production;
        public static Environment selectedEnv
        { 
            get
            {
                return API._selectedEnv;
            } 
        }

        /// <summary>
        /// The cached API key with the expiration date.
        /// </summary>
        private static (string key, DateTime expire)? api;

        /// <summary>
        /// The getter for the api key.
        /// </summary>
        /// <returns>The api key, or null if the key is not set jet, or null if the key is expired.</returns>
        private static string? apiKey
        {
            get
            {
                if (API.api is null)
                {
                    Log.write("api.key: Non-existent");
                    return null;
                }

                if (DateTime.Compare(DateTime.Now.ToUniversalTime(), API.api.Value.expire) > 0)
                {
                    var language = LibraryUtils.getRDict();
                    Log.write("api.key: Expired");
                    MessageBox.Show(
                        language["api.APIExpired.message"].ToString(),
                        language["api.APIExpired.caption"].ToString(),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    API.api = null;
                    return null;
                }

                return API.api.Value.key;
            }
        }

        /// <summary>
        /// A flag indicating whether a new thread should be created when errors occur.
        /// </summary>
        public static bool newErrorThread = false;

        /// <summary>
        /// The maximum amount of time that an error should be displayed for, when an <see cref="Utils.AutoClosingMessageBox"/> is uesed.
        /// </summary>
        public static TimeSpan maxErrorDisplayTime = new TimeSpan(0, 15, 0);

        #endregion variables

        /// <summary>
        /// Clears the cached API key.
        /// </summary>
        public static void clear()
        {
            API.api = null;
        }

        /// <summary>
        /// The type of envrionments which are supported.
        /// </summary>
        public enum Environment
        {
            Production,
            Beta
        }

        /// <summary>
        /// Changes the current environment to the specified environment and updates the API authentication URI and general URI accordingly.
        /// </summary>
        /// <param name="env">The target environment to switch to.</param>
        /// <returns>True if the environment was successfully changed; otherwise, false.</returns>
        public static bool changeEnvironment(Environment env)
        {
            if (API._selectedEnv == env) return false;

            API._selectedEnv = env;
            switch (env)
            {
                case Environment.Production:
                    API.auth = "https://app.infrakit.com/kuura/";
                    API.uri = API.auth + "v1/";
                    break;

                case Environment.Beta:
                    API.auth = "https://beta.infrakit.com/kuura/";
                    API.uri = API.auth + "v1/";
                    break;
            }

            return true;
        }

        #region documented endpoints

        #region authentication

        /// <summary>
        /// Sets the API key.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>True if the API key was set successfully, false otherwise.</returns>
        public static bool setAPIKey(string username, string password)
        {
            API.api = API.postApiLogIn(username, password);

            if(API.api is null) return false;

            return true;
        }

        /// <summary>
        /// Posts an API login request and returns the API key and expiration date.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A tuple containing the API key and expiration date, or null if the login failed.</returns>
        private static (string key, DateTime expire)? postApiLogIn(string username, string password)
        {
            try
            {
                string url = API.auth + "apilogin.json";

                var client = new RestClient(url);

                var request = new RestRequest();

                request.AddParameter("username", username);
                request.AddParameter("password", password);

                var response = client.Post(request);

                var json = response.Content;
                var values = (JObject)JsonConvert.DeserializeObject(json);

                if (values.SelectToken("status").Value<bool>() == true)
                {
                    var expire = values.SelectToken("expire").Value<Int64>();

                    DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    dt = dt.AddMilliseconds(expire);

                    return (values.SelectToken("apiKey").Value<string>(), dt);
                }

                long errorCode = values.SelectToken("errorCode").Value<long>();
                throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
            }
            catch (Exception e)
            {
                API.errorHandling(e, "api.authentication.postApiLogIn", v403:2);
                return null;
            }
        }

        #endregion authentication

        public static class Project
        {
            /// <summary>
            /// Posts the given APIProject to the specified API URI and returns the unique identifier (UUID) if the request is successful.
            /// </summary>
            /// <param name="project">The APIProject object to be posted.</param>
            /// <returns>The unique identifier (UUID) of the posted project if the request is successful; otherwise, null.</returns>
            public static Guid? post(APIProject project)
            {
                try
                {
                    string url = API.uri + "project";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddBody(JsonConvert.SerializeObject(project));

                    var response = client.Post(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var uuid = values.SelectToken("uuid").Value<string>();
                        return new Guid(uuid);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.project.post", v403: 4, v404: 4);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of all projects.
            /// </summary>
            /// <returns>A list of all projects, or null if an error occurred.</returns>
            public static List<Models.Project> get()
            {
                try
                {
                    string url = API.uri + "projects";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);

                    var json = response.Content;

                    var values = (JArray)JsonConvert.DeserializeObject(json);

                    var projects = new List<Models.Project>();
                    var count = 0;
                    foreach (var value in values)
                    {
                        if (!value.HasValues) continue;

                        try
                        {
                            projects.Add(Parse.project(value));
                        }
                        catch (InvalidCastException ex)
                        {
                            count++;
                            continue;
                        }
                    }

                    if (count > 0)
                    {
                        var captionKey = "api.project.get";
                        Log.write(captionKey + ": " + count + " projects could not be loaded");

                        var languages = LibraryUtils.getRDict();

                        var message = count + " " + languages["api.loadProjects"].ToString();

                        MessageBox.Show(
                            message,
                            languages[captionKey].ToString(),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }

                    return projects;
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.project.get");
                    return null;
                }
            }

            /// <summary>
            /// Gets the metadata for a specific project.
            /// </summary>
            /// <param name="uuid">The UUID of the project.</param>
            /// <returns>The metadata for the project, or null if an error occurred.</returns>
            public static Models.Project getMetadata(Guid uuid)
            {
                try
                {
                    string url = API.uri + "project/" + uuid;

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var project = values.SelectToken("project").Value<JToken>();
                        return Parse.project(project);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.project.getMetadata", uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of all folders in a specific project.
            /// </summary>
            /// <param name="uuid">The UUID of the project.</param>
            /// <param name="depth">
            /// Maximum depth for listed folders from project root.
            /// Depth 0 is the root folder.
            /// Negative value means that all project folders will be listed.
            /// </param>
            /// <returns>A list of all folders in the project, or null if an error occurred.</returns>
            public static List<Models.Folder> getFolders(Guid uuid, int depth)
            {
                try
                {
                    string url = API.uri + "project/" + uuid + "/folders";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);
                    request.AddParameter("depth", depth);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    var folders = new List<Models.Folder>();

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        foreach (var value in values.SelectToken("folders").Value<JArray>())
                        {
                            folders.Add(Parse.folder(value));
                        }

                        return folders;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.project.getFolders", uuid: uuid);
                    return null;
                }
            }
        }

        public static class User
        {
            // TODO: comment
            public static Models.User? get(Guid uuid)
            {
                try
                {
                    string url = API.uri + "user/" + uuid.ToString();

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    return Parse.user(values);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.user.getByOrganization", v400: 2, v403: 3, v404: 6, uuid: uuid);
                    return null;
                }
            }

            // TODO: comment
            public static List<Models.User>? getByOrganization(Guid organizationUuid)
            {
                try
                {
                    string url = API.uri + "users/by-organization/" + organizationUuid.ToString();

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        List<Models.User> users = new();

                        foreach (var value in values.SelectToken("users").Value<JArray>())
                        {
                            users.Add(Parse.user(value));
                        }

                        return users;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.user.getByOrganization", v404: 4, uuid: organizationUuid);
                    return null;
                }
            }
        }

        public static class Folder
        {
            /// <summary>
            /// Gets the contents of a folder, including the folder itself, its subfolders, and its documents.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <param name="includeParent">Whether to include the folder itself in the results.</param>
            /// <param name="showErrorMessages">Whether to show error messages if something goes wrong.</param>
            /// <returns>A tuple containing a list of folders and a list of documents, or null if an error occurred.</returns>
            public static (List<Models.Folder> folders, List<Models.Document> docs)? get(Guid uuid, bool includeParent = true, bool showErrorMessages = true)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid;

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        List<Models.Folder> folders = new();
                        List<Models.Document> documents = new();

                        if (includeParent)
                        {
                            folders.Add(Parse.folder(values.SelectToken("folder").Value<JToken>()));
                        }

                        foreach (var value in values.SelectToken("documents").Value<JArray>())
                        {
                            documents.Add(Parse.document(value));
                        }

                        foreach (var value in values.SelectToken("folders").Value<JArray>())
                        {
                            folders.Add(Parse.folder(value));
                        }

                        return (folders, documents);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    if (!showErrorMessages) return null;

                    API.errorHandling(e, "api.folder.get", uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Creates a new folder.
            /// </summary>
            /// <param name="parentUuid">The UUID of the parent folder.</param>
            /// <param name="name">The name of the new folder.</param>
            /// <returns>The UUID of the new folder, or null if an error occurred.</returns>
            public static Guid? post(Guid parentUuid, string name)
            {
                try
                {
                    string url = API.uri + "folder";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddParameter("parentUuid", parentUuid.ToString());
                    request.AddParameter("name", name);

                    var response = client.Post(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var uuid = values.SelectToken("uuid").Value<string>();
                        return new Guid(uuid);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.folder.post", uuid: parentUuid);
                    return null;
                }
            }

            /// <summary>
            /// Gets the metadata for a folder.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <param name="showErrorMessages">Whether to show error messages if something goes wrong.</param>
            /// <returns>The folder metadata, or null if an error occurred.</returns>
            public static Models.Folder? getMetadata(Guid uuid, bool showErrorMessages = true)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid + "/metadata";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var folder = values.SelectToken("folder").Value<JToken>();
                        return Parse.folder(folder);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    if (!showErrorMessages) return null;

                    API.errorHandling(e, "api.folder.getMetadata", uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of all documents in a folder.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <returns>A list of all documents in the folder, or null if an error occurred.</returns>
            public static List<Models.Document>? getDocuments(Guid uuid)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid + "/documents";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var documents = new List<Models.Document>();
                        foreach (var value in values.SelectToken("documents").Value<JArray>())
                        {
                            documents.Add(Parse.document(value));
                        }
                        return documents;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.folder.getDocuments", uuid: uuid, v404: 2);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of all subfolders in a folder.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <param name="includeParent">Whether to include the folder itself in the results.</param>
            /// <returns>A list of all subfolders in the folder, or null if an error occurred.</returns>
            public static List<Models.Folder>? getFolders(Guid uuid, bool includeParent = true)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid + "/folders";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var folders = new List<Models.Folder>();

                        if (includeParent)
                        {
                            folders.Add(Parse.folder(values.SelectToken("folder").Value<JToken>()));
                        }

                        foreach (var value in values.SelectToken("folders").Value<JArray>())
                        {
                            folders.Add(Parse.folder(value));
                        }

                        return folders;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.folder.getFolders", uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of all images in a folder.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <returns>A list of all images in the folder, or null if an error occurred.</returns>
            public static List<Image>? getImages(Guid uuid)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid + "/images";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var images = new List<Models.Image>();
                        foreach (var value in values.SelectToken("images").Value<JArray>())
                        {
                            images.Add(Parse.image(value));
                        }

                        return images;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.folder.getImages", uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Gets a list of properties for the specified folder.
            /// </summary>
            /// <param name="uuid">The UUID of the folder.</param>
            /// <returns>A list of properties, or null if an error occurred.</returns>
            public static List<PropertyDeclaration> getProperties(Guid uuid)
            {
                try
                {
                    string url = API.uri + "folder/" + uuid + "/properties";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddParameter("uuid", uuid);

                    var response = client.Get(request);

                    var json = response.Content;

                    var properties = new List<Models.PropertyDeclaration>();
                    foreach (var value in (JArray)JsonConvert.DeserializeObject(json))
                    {
                        properties.Add(Parse.propertyDeclaration(value.SelectToken("propertyDeclaration").Value<JObject>()));
                    }

                    return properties;
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.folder.getProperties", v400: 2, v403: 3);
                    return null;
                }
            }
        }

        public static class Group
        {
            // TODO: comment
            public static Guid? post(string name, Guid projectUuid)
            {
                try
                {
                    string url = API.uri + "group";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddParameter("projectUuid", projectUuid.ToString());
                    request.AddParameter("name", name);

                    var response = client.Post(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var group = values.SelectToken("group").Value<JToken>();
                        var uuid = group.SelectToken("uuid").Value<string>();
                        return new Guid(uuid);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.group.post", v400: 2, v403: 3, v409: 2);
                    return null;
                }
            }

            // TODO: comment
            public static bool addUsers(Guid groupUuid, List<Guid> users)
            {
                try
                {
                    string url = API.uri + "group/" + groupUuid.ToString() + "/users";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var tmp = JsonConvert.SerializeObject(users);

                    request.AddBody(JsonConvert.SerializeObject(users));

                    var response = client.Post(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        return true;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.group.addUsers", v400: 2, v403: 3, v409: 2);
                    return false;
                }
            }

            // TODO: comment
            public static bool addUser(Guid groupUuid, Guid userUuid)
            { // TODO: check functionallity
                try
                {
                    string url = API.uri + "group/" + groupUuid.ToString() + "/users/" + userUuid.ToString();

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Put(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        return true;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.group.addUsers", v400: 2, v403: 3, v409: 2);
                    return false;
                }
            }
        }

        public static class Equipment
        {
            /// <summary>
            /// Gets a list of all equipment in a project.
            /// </summary>
            /// <param name="uuid">The UUID of the project.</param>
            /// <returns>A list of all equipment in the project, or null if an error occurred.</returns>
            public static List<Models.Equipment>? get(Guid uuid)
            {
                try
                {
                    string url = API.uri + "equipment/by-project/" + uuid;

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Get(request);

                    var json = response.Content;

                    var values = (JArray)JsonConvert.DeserializeObject(json);

                    var equipments = new List<Models.Equipment>();
                    foreach (var value in values)
                    {
                        equipments.Add(Parse.equipment(value));
                    }

                    return equipments;
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.equipment.get", v400: 2, v403: 3, uuid: uuid);
                    return null;
                }
            }
        }

        public static class Document
        {
            #region getUploudURL

            /// <summary>
            /// The status which an document api request can have.
            /// </summary>
            public enum Status
            {
                Successful,
                Conflict,
                InvalidExtension,
                InvalidPath,
                InvalidFilename
            }

            /// <summary>
            /// Gets the URL for uploading a file to a document.
            /// </summary>
            /// <param name="source">The path to the file to upload.</param>
            /// <param name="target">The UUID of the folder to upload the file to.</param>
            /// <returns>
            /// A tuple of the status and a (Uri, Models.Document.Upload, Models.Document) tuple.
            /// The status indicates whether the operation was successful.
            /// The tuple contains the upload URL, the Models.Document.Upload object, and the Models.Document object.
            /// Null is returned if an error occurred
            /// </returns>
            private static (Status status, (Uri url, Models.Document doc, Models.Document.Upload param)? items)? getUploudURL(string source, Guid target)
            {
                try
                {
                    string url = API.uri + "document/async-upload";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var param = new Models.Document.Upload(source, target);
                    request.AddBody(JsonConvert.SerializeObject(param));

                    var response = client.Post(request);
                    var json = response.Content;

                    var value = (JObject)JsonConvert.DeserializeObject(json);

                    var uri = value.SelectToken("uploadUrl").Value<string>();

                    var document = Parse.document(value.SelectToken("document").Value<JToken>());

                    return (Status.Successful, (new Uri(uri), document, param));
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.getUploudURL", v404: 2, uuid: target);

                    var httpExeption = e as HttpRequestException;
                    if (httpExeption is null) return null;

                    if (httpExeption.StatusCode == HttpStatusCode.Conflict)
                    {
                        return (Status.Conflict, null);
                    }

                    #region check for forbidden file extensions

                    if (httpExeption.StatusCode != HttpStatusCode.Forbidden) return null;
                    
                    string fileExtension = Path.GetExtension(source);

                    if (!Utils.forbiddenFileExtensions.Contains(fileExtension)) return null;

                    return (Status.InvalidExtension, null);

                    #endregion check for forbidden file extensions
                }
            }

            /// <summary>
            /// Uploads a document to the API.
            /// </summary>
            /// <param name="source">The path to the file to upload.</param>
            /// <param name="target">The UUID of the folder to upload the file to.</param>
            /// <returns>
            /// A tuple of the status and a Models.Document tuple.
            /// The status indicates whether the operation was successful.
            /// The tuple contains the Models.Document object.
            /// Null is returned if an error occurred
            /// </returns>
            public static (Status status, Models.Document? doc)? upload(string source, Guid target)
            {
                try
                {
                    var upload = API.Document.getUploudURL(source, target);

                    if (!upload.HasValue) return null;

                    if(upload.Value.status != Status.Successful) return (upload.Value.status, null);

                    var (url, doc, param) = upload.Value.items.Value;

                    string md5Base64 = Convert.ToBase64String(API.Document.HexStringToHex(param.checksum));

                    HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
                    httpRequest.Method = "PUT";
                    httpRequest.Headers.Add("content-md5", md5Base64);
                    httpRequest.Headers.Add("content-length", param.size.ToString());
                    httpRequest.Headers.Add("content-type", param.contentType);

                    using (Stream dataStream = httpRequest.GetRequestStream())
                    {
                        var buffer = new byte[8000];
                        using (FileStream fileStream = new FileStream(source, FileMode.Open, FileAccess.Read))
                        {
                            int bytesRead = 0;
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                dataStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }

                    FileInfo fileInfo = new FileInfo(source);
                    long fileSizeInBytes = fileInfo.Length;
                    var timeout = (int)(fileSizeInBytes / 1024);
                    httpRequest.Timeout = Math.Max(timeout, httpRequest.Timeout);

                    HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;

                    return (Status.Successful, doc);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.upload", v404: 2, uuid: target);
                    return null;
                }
            }

            /// <summary>
            /// Converts an hex string into hex bytes
            /// </summary>
            /// <param name="inputHex">The hex string to convert</param>
            /// <returns>The hex bytes</returns>
            private static byte[] HexStringToHex(string inputHex)
            {
                var resultantArray = new byte[inputHex.Length / 2];
                for (var i = 0; i < resultantArray.Length; i++)
                {
                    resultantArray[i] = Convert.ToByte(inputHex.Substring(i * 2, 2), 16);
                }
                return resultantArray;
            }

            #endregion getUploudURL

            /// <summary>
            /// Gets the metadata for a document.
            /// </summary>
            /// <param name="uuid">The UUID of the document.</param>
            /// <returns>The document object, or null if an error occurred.</returns>
            public static Models.Document? getMetadata(Guid uuid)
            {
                try
                {
                    string url = API.uri + "document/" + uuid;

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddParameter("includeAllVersions", false);

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        return Parse.document(values.SelectToken("document").Value<JToken>());
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.getMetadata", v404: 3, uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Deletes a document.
            /// </summary>
            /// <param name="uuid">The UUID of the document.</param>
            /// <returns>True if the document was deleted successfully, false otherwise.</returns>
            public static bool delete(Guid uuid)
            {
                try
                {
                    string url = API.uri + "document/" + uuid;

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Delete(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        return values.SelectToken("deleted").Value<bool>();
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.delete", uuid: uuid);
                    return false;
                }
            }

            #region getDocumentURL

            /// <summary>
            /// Gets the URL for downloading a document.
            /// </summary>
            /// <param name="uuid">The UUID of the document.</param>
            /// <returns>A tuple containing the URL and document for the given document UUID, or null if the document cannot be found.</returns>
            public static (Uri uri, Models.Document doc)? getDownloadURL(Guid uuid)
            {
                try
                {
                    string url = API.uri + "document/" + uuid + "/async-download";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    var response = client.Post(request);
                    var json = response.Content;

                    var value = (JObject)JsonConvert.DeserializeObject(json);
                    string uri = value.SelectToken("downloadUrl").Value<string>();

                    var doc = Parse.document(value.SelectToken("document").Value<JToken>());

                    return (new Uri(uri), doc);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.getDownloadURL", v404: 3, uuid: uuid);
                    return null;
                }
            }

            /// <summary>
            /// Downloads a document to the specified target file path.
            /// </summary>
            /// <param name="source">The UUID of the document to download.</param>
            /// <param name="target">The file path to save the downloaded document to.</param>
            /// <param name="targetFileName">The file name of the document to be downloaded.</param>
            /// <returns>
            /// A tuple of the status and a Models.Document tuple.
            /// The status indicates whether the operation was successful.
            /// The tuple contains the Models.Document object.
            /// Null is returned if an error occurred
            /// </returns>
            public static (Status status, Models.Document? doc)? download(Guid source, string target, string targetFileName)
            {
                try
                {
                    #region invalid path / file name

                    var invalidFileNameChars = Path.GetInvalidFileNameChars();
                    var invalidPathChars     = Path.GetInvalidPathChars();

                    bool invalidFileName = targetFileName.IndexOfAny(invalidFileNameChars) >= 0;
                    var invalidPath      = targetFileName.IndexOfAny(invalidPathChars) >= 0;

                    if (invalidFileName || invalidPath)
                    {
                        var language = LibraryUtils.getRDict();

                        char[] invalidChars;
                        if (invalidFileName)
                        {
                            invalidChars = invalidFileNameChars;
                        }
                        else
                        {
                            invalidChars = invalidPathChars;
                        }

                        var message = new StringBuilder(language["file.invalidCharacters.message"].ToString());
                        message.Append(" [\"");
                        message.Append(target);
                        message.Append("\"]: (");
                        foreach (var c in invalidChars)
                        {
                            if (Char.IsControl(c) || Char.IsWhiteSpace(c))
                            {
                                message.AppendFormat("\\U{0:X4}, ", (int)c);
                            }
                            else
                            {
                                message.Append(c);
                                message.Append(", ");
                            }
                        }
                        message.Length -= 2;
                        message.Append(")");

                        Utils.Log.write("file.invalidCharacters: \"" + target + "\"");
                        MessageBox.Show(
                            message.ToString(),
                            language["file.invalidCharacters.caption"].ToString(),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );

                        return (Status.InvalidFilename, null);
                    }

                    #endregion invalid path / file name

                    #region invalid path length

                    if (target.Length > 260)
                    {
                        var language = Utils.Language.getRDict();
                        Utils.Log.write("file.pathToLong: \"" + target + "\"");
                        MessageBox.Show(
                            language["file.pathToLong.message"].ToString(),
                            language["file.pathToLong.caption"].ToString(),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );

                        return (Status.InvalidPath, null);
                    }

                    #endregion invalid path length

                    var download = API.Document.getDownloadURL(source);

                    if (!download.HasValue) return null;

                    var (uri, doc) = download.Value;

                    if (File.Exists(target))
                    {
                        var md5Target = doc.md5;
                        string md5Source;
                        using (var md5Stream = MD5.Create())
                        {
                            using (var stream = File.OpenRead(target))
                            {
                                var md5Result = md5Stream.ComputeHash(stream);
                                md5Source = Convert.ToHexString(md5Result);
                            }
                        }

                        if (md5Source.Equals(md5Target, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new HttpRequestException(null, null, HttpStatusCode.Conflict);
                        }
                    }

                    WebClient webClient = new WebClient();
                    var bytes = webClient.DownloadData(uri);

                    using (var fs = new FileStream(target, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                    }

                    return (Status.Successful, doc);
                }
                catch(HttpRequestException e)
                {
                    API.errorHandling(e, "api.document.download", v404: 3, uuid: source);
                    if (e.StatusCode == HttpStatusCode.Conflict)
                    {
                        return (Status.Conflict, null);
                    }
                    return null;
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.document.download", v404: 3, uuid: source);
                    return null;
                }
            }

            #endregion getDocumentURL
        }

        public static class Logpoint
        {
            /// <summary>
            /// Gets a list of logpoints from the specified time period, optionally filtered by project, equipment, or vehicle.
            /// </summary>
            /// <param name="fromTime">The start of the time period in Unix timestamp format.</param>
            /// <param name="projectUuid">The UUID of the project to filter by, or null to include all projects.</param>
            /// <param name="projectId">The ID of the project to filter by, or null to include all projects.</param>
            /// <param name="equipmentUuid">The UUID of the equipment to filter by, or null to include all equipment.</param>
            /// <param name="vehicleId">The ID of the vehicle to filter by, or null to include all vehicles.</param>
            /// <returns>A list of logpoints, or null if an error occurred.</returns>
            public static List<Models.Logpoint> get(long fromTime, Guid? projectUuid = null, int? projectId = null, Guid? equipmentUuid = null, int? vehicleId = null)
            {
                try
                {
                    string url = API.uri + "logpoints";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);
                    request.AddParameter("fromTime", fromTime);

                    if (projectUuid.HasValue)
                    {
                        request.AddParameter("projectUuid", projectUuid.Value);
                    }

                    if (projectId.HasValue)
                    {
                        request.AddParameter("projectId", projectId.Value);
                    }

                    if (equipmentUuid.HasValue)
                    {
                        request.AddParameter("equipmentUuid", equipmentUuid.Value);
                    }

                    if (vehicleId.HasValue)
                    {
                        request.AddParameter("vehicleId", vehicleId.Value);
                    }

                    var response = client.Get(request);
                    var json = response.Content;

                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    List<Models.Logpoint> logpoints = new();

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        foreach (var value in values.SelectToken("logpoints").Value<JArray>())
                        {
                            logpoints.Add(Parse.logpoint(value));
                        }

                        return logpoints;
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.logpoint.get", uuid: projectUuid);
                    return null;
                }
            }
        }

        public static class Property
        {
            // for getting the properties of an Folder see API.Folder.getProperties

            /// <summary>
            /// Gets a list of properties for the specified project.
            /// </summary>
            /// <param name="organizationUuid">The UUID of the organization that owns the project.</param>
            /// <param name="projektUuid">The UUID of the project.</param>
            /// <returns>A list of properties, or null if an error occurred.</returns>
            public static List<PropertyDeclaration> get(Guid organizationUuid, Guid projektUuid)
            {
                try
                {
                    string url = API.uri + "property";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddParameter("organizationUuid", organizationUuid);
                    request.AddParameter("projectUuid", projektUuid);

                    var response = client.Get(request);

                    var json = response.Content;

                    var properties = new List<Models.PropertyDeclaration>();
                    foreach (var value in (JArray)JsonConvert.DeserializeObject(json))
                    {
                        properties.Add(Parse.propertyDeclaration(value));
                    }

                    return properties;
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.property.getProject", v400: 2, v403: 3);
                    return null;
                }
            }

            /// <summary>
            /// Adds a new property to a project.
            /// </summary>
            /// <param name="property">The property to add.</param>
            /// <returns>The new property, or null if an error occurred.</returns>
            public static PropertyDeclaration add(PropertyDeclaration property)
            {
                try
                {
                    string url = API.uri + "property";

                    var client = new RestClient(url);

                    var request = new RestRequest();

                    request.AddHeader("Authorization", "Bearer " + API.apiKey);

                    request.AddBody(property.getJSON());

                    var response = client.Post(request);

                    var json = response.Content;
                    var values = (JObject)JsonConvert.DeserializeObject(json);

                    if (values.SelectToken("status").Value<bool>() == true)
                    {
                        var value = values.SelectToken("propertyDeclaration").Value<JToken>();
                        return Parse.propertyDeclaration(value);
                    }

                    long errorCode = values.SelectToken("errorCode").Value<long>();
                    throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
                }
                catch (Exception e)
                {
                    API.errorHandling(e, "api.property.addProject", v400: 2, v403: 3);
                    return null;
                }
            }
        }

        #endregion documented endpoints

        #region undocumented endpoints

        /// <summary>
        /// Deletes a folder from the Infrakit project with the specified project ID and folder ID.
        /// The method could be extended to also delete files or links
        /// </summary>
        /// <param name="projectID">The ID of the Infrakit project containing the file or folder to delete.</param>
        /// <param name="folderID">The ID of the file or folder to delete.</param>
        /// <returns>A boolean indicating whether the file or folder was successfully deleted.</returns>
        public static bool deleteFileFolder(int projectID, int folderID)
        {
            try
            {
                string url = "https://app.infrakit.com/kuura/api/1/file_folder/delete";

                var client = new RestClient(url);

                var request = new RestRequest();

                request.AddHeader("Authorization", "Bearer " + API.apiKey);

                #region setup parameters

                string parameters = "folders=" + folderID.ToString() + "&files=&links=&projectId=" + projectID.ToString();

                #endregion setup parameters

                request.AddParameter("application/x-www-form-urlencoded", parameters, ParameterType.RequestBody);
    
                var response = client.Post(request);
                var json = response.Content;

                var values = (JObject)JsonConvert.DeserializeObject(json);

                if (values.SelectToken("status").Value<bool>() == true)
                {
                    return true;
                }

                long errorCode = values.SelectToken("errorCode").Value<long>();
                throw new HttpRequestException(null, null, (HttpStatusCode)errorCode);
            }
            catch (Exception e)
            {
                API.errorHandling(e, "api.deleteFileFolder");
                return false;
            }
        }

        #endregion undocumented endpoints

        #region error handling

        /// <summary>
        /// Handles errors that occur in the API class.
        /// </summary>
        /// <param name="e">The exception that occurred.</param>
        /// <param name="captionKey">The key of the caption to display.</param>
        /// <param name="v400">The index of the 400 Bad Request error message.</param>
        /// <param name="v403">The index of the 403 Forbidden error message.</param>
        /// <param name="v404">The index of the 404 Not Found error message.</param>
        /// <param name="v409">The index of the 409 Conflict error message.</param>
        /// <param name="uuid">An optional UUID to include in the error message.</param>
        private static void errorHandling(Exception e, string captionKey, int v400 = 1, int v403 = 1, int v404 = 1, int v409 = 1, Guid? uuid = null)
        {
            var language = LibraryUtils.getRDict();
            var caption = language[captionKey].ToString();

            if(e.GetType() == typeof(WebException))
            {
                var webExeption = e as WebException;

                if(webExeption.Status == WebExceptionStatus.Timeout)
                {
                    Log.write(captionKey + ": api.timeout");
                    Utils.AutoClosingMessageBox.Show(
                        language["api.timeout"].ToString(),
                        caption,
                        MessageBoxImage.Error,
                        API.maxErrorDisplayTime,
                        API.newErrorThread
                    );
                }
                else
                {
                    Log.write(captionKey + ": WebException | " + webExeption.Status.ToString());
                    Utils.AutoClosingMessageBox.Show(
                        language["api.default"].ToString(),
                        caption,
                        MessageBoxImage.Error,
                        API.maxErrorDisplayTime,
                        API.newErrorThread
                    );
                }
                return;
            }

            if (!e.GetType().IsAssignableFrom(typeof(HttpRequestException)))
            {
                Log.write(captionKey + ": " + e.GetType() + " | " + e.Message);
                Utils.AutoClosingMessageBox.Show(
                    language["api.default"].ToString(),
                    caption,
                    MessageBoxImage.Error,
                    API.maxErrorDisplayTime,
                    API.newErrorThread
                );
                return;
            }

            var httpExeption = e as HttpRequestException;
            var statusCode = httpExeption.StatusCode;

            if (statusCode is null)
            {
                Log.write(captionKey + ": " + e.GetType() + " | " + e.Message);
                Utils.AutoClosingMessageBox.Show(
                    language["api.default"].ToString(),
                    caption,
                    MessageBoxImage.Error,
                    API.maxErrorDisplayTime,
                    API.newErrorThread
                );
                return;
            }

            string logMessage;
            string mBText;

            if (statusCode.HasValue)
            {
                logMessage = (int)statusCode.Value + " " + statusCode;
            }
            else
            {
                logMessage = e.Message;
            }

            switch (statusCode)
            {
                case HttpStatusCode.BadRequest:
                    mBText = language["api.400." + v400].ToString();
                    break;

                case HttpStatusCode.Unauthorized:
                    mBText = language["api.401"].ToString();
                    break;

                case HttpStatusCode.Forbidden:
                    mBText = language["api.403." + v403].ToString();
                    break;

                case HttpStatusCode.NotFound:
                    mBText = language["api.404." + v404].ToString();

                    if (uuid.HasValue)
                    {
                        logMessage += " (" + uuid + ")";
                        mBText += " (" + uuid + ")";
                    }
                    break;

                case HttpStatusCode.Conflict:
                    mBText = language["api.409." + v409].ToString();
                    break;

                case HttpStatusCode.InternalServerError:
                    mBText = language["api.500"].ToString();
                    break;

                default:
                    mBText = language["api.default"].ToString();
                    break;
            }

            Log.write(captionKey + ": " + logMessage);
            Utils.AutoClosingMessageBox.Show(
                (int)statusCode + ": " + mBText.ToString(),
                caption,
                MessageBoxImage.Error,
                API.maxErrorDisplayTime,
                API.newErrorThread
            );
        }

        #endregion error handling
    }
}