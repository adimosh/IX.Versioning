using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using IX.StandardExtensions;
using IX.System.IO;

namespace CsprojVersioning
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                // No arguments
                return 1;
            }

            var version = args.LastOrDefault(p => !string.IsNullOrWhiteSpace(p));

            if (version == null)
            {
                // No version
                return 1;
            }

            var release = args.Any(p => p == "Release");

            IFile file = new File();

            string[] paths = args.Where(p => !string.IsNullOrWhiteSpace(p) && p != version && p != "Release").Distinct().ToArray();

            var documents = new Dictionary<string, XDocument>(paths.Length);

            string majorVersion;
            string minorVersion;
            string buildVersion;
            string revisionVersion;
            string prereleaseVersion;

            var versionRegex = new Regex($@"^(?<{nameof(majorVersion)}>\d{{1,}}).(?<{nameof(minorVersion)}>\d{{1,}}).(?<{nameof(buildVersion)}>\d{{1,}})(?:.(?<{nameof(revisionVersion)}>\d{{1,}}))?(?:-(?<{nameof(prereleaseVersion)}>[\d\w]{{1,}}))?$");

            Match versionMatch = versionRegex.Match(version);

            if (!versionMatch.Success)
            {
                // Version string not good
                return 2;
            }

            majorVersion = versionMatch.Groups[nameof(majorVersion)]?.Value;
            minorVersion = versionMatch.Groups[nameof(minorVersion)]?.Value;
            buildVersion = versionMatch.Groups[nameof(buildVersion)]?.Value;
            revisionVersion = versionMatch.Groups[nameof(revisionVersion)]?.Value;
            prereleaseVersion = versionMatch.Groups[nameof(prereleaseVersion)]?.Value;

            if (string.IsNullOrWhiteSpace(majorVersion) || string.IsNullOrWhiteSpace(minorVersion) || string.IsNullOrWhiteSpace(buildVersion))
            {
                // Version string not good
                return 2;
            }

            if (paths.Length == 0)
            {
                IDirectory directory = new Directory();

                if (directory.EnumerateFiles(".", "*.csproj").Any(p => !ProcessXmlFile(p)))
                {
                    // All search - failure
                    return 3;
                }
            }
            else
            {
                if (paths.Any(p => !ProcessXmlFile(p)))
                {
                    // Specified path - failure
                    return 4;
                }
            }

            bool ProcessXmlFile(string fileName)
            {
                if (!file.Exists(fileName))
                {
                    return false;
                }

                XDocument document;
                try
                {
                    using (System.IO.Stream s = file.OpenRead(fileName))
                    {
                        document = XDocument.Load(s, LoadOptions.PreserveWhitespace);
                    }
                }
                catch
                {
                    return false;
                }

                XElement root = document.Descendants("Project").FirstOrDefault();

                XElement missingContainer = null;

                IEnumerable<XElement> xVersions = root.Descendants("PropertyGroup").Descendants("Version");
                EnsureCorrectOnlyOneVersion(
                    xVersions,
                    release ?
                        $"{majorVersion}.{minorVersion}.{buildVersion}{(string.IsNullOrWhiteSpace(prereleaseVersion) ? string.Empty : $"-{prereleaseVersion}")}" :
                        version,
                    "Version");

                IEnumerable<XElement> xFileVersions = root.Descendants("PropertyGroup").Descendants("FileVersion");
                EnsureCorrectOnlyOneVersion(xFileVersions, version, "FileVersion");

                IEnumerable<XElement> xAssemblyVersions = root.Descendants("PropertyGroup").Descendants("AssemblyVersion");
                EnsureCorrectOnlyOneVersion(xAssemblyVersions, $"{majorVersion}.{minorVersion}.{buildVersion}.0", "AssemblyVersion");

                void EnsureCorrectOnlyOneVersion(IEnumerable<XElement> xElements, string correctVersion, string versionName)
                {
                    XElement xElement = xElements.FirstOrDefault();

                    if (xElement == null)
                    {
                        EnsureMissingContainerExists();

                        xElement = new XElement(versionName);

                        missingContainer.Add(xElement);

                        void EnsureMissingContainerExists()
                        {
                            if (missingContainer != null)
                            {
                                return;
                            }

                            missingContainer = new XElement("PropertyGroup");

                            root.Add(missingContainer);
                        }
                    }

                    xElement.SetValue(correctVersion);

                    xElements.Where(p => p != xElement).ForEach(p => p.Remove());
                }

                documents.Add(fileName, document);

                return true;
            }

            documents.ForEach(p =>
            {
                using (System.IO.Stream s = file.Create(p.Key))
                {
                    using (var writer = XmlWriter.Create(s, new XmlWriterSettings { OmitXmlDeclaration = true, }))
                    {
                        p.Value.Save(writer);
                    }
                }
            });

            // Success
            return 0;
        }
    }
}