// <copyright file="XmlFileParserService.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IX.StandardExtensions;
using IX.System.IO;

namespace CsprojVersioning
{
    internal class XmlFileParserService
    {
        private readonly IFile file;
        private readonly IPath path;

        internal XmlFileParserService(IFile file, IPath path)
        {
            this.file = file;
            this.path = path;
        }

        internal IEnumerable<string> ProcessPaths(IEnumerable<string> paths, string version, string majorVersion, string minorVersion, string buildVersion, string revisionVersion, string preReleaseVersion, bool release)
        {
            var finalPaths = new List<string>();
            foreach (var path in paths.Select(p => p.ToLowerInvariant()))
            {
                if (finalPaths.Contains(path))
                {
                    continue;
                }

                var extension = this.path.GetExtension(path);

                if (extension == "csproj")
                {
                    if (ProcessCsprojFile(path))
                    {
                        finalPaths.Add(path);

                        var path2 = this.path.ChangeExtension(path, "nuspec");

                        if (ProcessNuspecFile(path2))
                        {
                            finalPaths.Add(path2);
                        }
                    }
                }
                else if (extension == "nuspec")
                {
                    if (ProcessNuspecFile(path))
                    {
                        finalPaths.Add(path);

                        var path2 = this.path.ChangeExtension(path, "csproj");

                        if (ProcessCsprojFile(path2))
                        {
                            finalPaths.Add(path2);
                        }
                    }
                }
                else
                {
                    if (ProcessCsprojFile(path))
                    {
                        finalPaths.Add(path);
                    }
                }

                // .csproj file handling
                bool ProcessCsprojFile(string fileName)
                {
                    if (!this.file.Exists(fileName))
                    {
                        return false;
                    }

                    XDocument document;
                    try
                    {
                        using (System.IO.Stream s = this.file.OpenRead(fileName))
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
                        release ? $"{majorVersion}.{minorVersion}.{buildVersion}{(string.IsNullOrWhiteSpace(preReleaseVersion) ? string.Empty : $"-{preReleaseVersion}")}" : version,
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

                    return true;
                }

                // .nuspec file handling
                bool ProcessNuspecFile(string fileName)
                {
                    if (!this.file.Exists(fileName))
                    {
                        return false;
                    }

                    XDocument document;
                    try
                    {
                        using (System.IO.Stream s = this.file.OpenRead(fileName))
                        {
                            document = XDocument.Load(s, LoadOptions.PreserveWhitespace);
                        }
                    }
                    catch
                    {
                        return false;
                    }

                    XElement root = document.Descendants("package").FirstOrDefault();

                    XElement missingContainer = root.Descendants("metadata").FirstOrDefault();
                    if (missingContainer == null)
                    {
                        missingContainer = new XElement("metadata");
                    }

                    root.Add(missingContainer);

                    IEnumerable<XElement> xVersions = missingContainer.Descendants("version");
                    EnsureCorrectOnlyOneVersion(
                        xVersions,
                        release ? $"{majorVersion}.{minorVersion}.{buildVersion}{(string.IsNullOrWhiteSpace(preReleaseVersion) ? string.Empty : $"-{preReleaseVersion}")}" : version,
                        "version");

                    void EnsureCorrectOnlyOneVersion(IEnumerable<XElement> xElements, string correctVersion, string versionName)
                    {
                        XElement xElement = xElements.FirstOrDefault();

                        if (xElement == null)
                        {
                            xElement = new XElement(versionName);

                            missingContainer.Add(xElement);
                        }

                        xElement.SetValue(correctVersion);

                        xElements.Where(p => p != xElement).ForEach(p => p.Remove());
                    }

                    return true;
                }
            }

            return finalPaths;
        }
    }
}