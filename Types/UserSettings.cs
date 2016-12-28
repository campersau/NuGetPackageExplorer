using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using NuGet.Resources;
using NuGet;

namespace NuGetPackageExplorer.Types
{
    public class UserSettings : ISettings
    {
        private readonly XDocument _config;
        private readonly string _configLocation;
        private readonly IFileSystem _fileSystem;

        public UserSettings(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _configLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet",
                                           "NuGet.Config");
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configLocation);
        }

        #region ISettings Members

        public string GetValue(string section, string key, bool isPath)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            IDictionary<string, string> kvps = GetValues(section);
            string value;
            if (kvps == null || !kvps.TryGetValue(key, out value))
            {
                return null;
            }
            return value;
        }

        public IDictionary<string, string> GetValues(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            try
            {
                XElement sectionElement = _config.Root.Element(section);
                if (sectionElement == null)
                {
                    return null;
                }

                var kvps = new Dictionary<string, string>();
                foreach (XElement e in sectionElement.Elements("add"))
                {
                    string key = e.GetOptionalAttributeValue("key");
                    string value = e.GetOptionalAttributeValue("value");
                    if (!String.IsNullOrEmpty(key) && value != null)
                    {
                        kvps.Add(key, value);
                    }
                }

                return kvps;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(NuGetResources.UserSettings_UnableToParseConfigFile, e);
            }
        }

        public void SetValue(string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            XElement sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            foreach (XElement e in sectionElement.Elements("add"))
            {
                string tempKey = e.GetOptionalAttributeValue("key");

                if (tempKey == key)
                {
                    e.SetAttributeValue("value", value);
                    Save(_config);
                    return;
                }
            }

            var addElement = new XElement("add");
            addElement.SetAttributeValue("key", key);
            addElement.SetAttributeValue("value", value);
            sectionElement.Add(addElement);
            Save(_config);
        }

        public bool DeleteValue(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            XElement sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                return false;
            }

            XElement elementToDelete = null;
            foreach (XElement e in sectionElement.Elements("add"))
            {
                if (e.GetOptionalAttributeValue("key") == key)
                {
                    elementToDelete = e;
                    break;
                }
            }
            if (elementToDelete == null)
            {
                return false;
            }
            elementToDelete.Remove();
            Save(_config);

            return true;
        }

        public IList<SettingValue> GetValues(string section, bool isPath)
        {
            throw new NotImplementedException();
        }

        public IList<SettingValue> GetNestedValues(string section, string subsection)
        {
            throw new NotImplementedException();
        }

        public void SetValues(string section, IList<SettingValue> values)
        {
            throw new NotImplementedException();
        }

        public void UpdateSections(string section, IList<SettingValue> values)
        {
            throw new NotImplementedException();
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSection(string section)
        {
            throw new NotImplementedException();
        }

        #endregion

        private void Save(XDocument document)
        {
            _fileSystem.AddFile(_configLocation, document.Save);
        }
    }
}