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

        internal IEnumerable<(string, XDocument)> ProcessPaths(IEnumerable<string> paths, string version, string majorVersion, string minorVersion, string buildVersion, string revisionVersion, string preReleaseVersion, bool release)
        {
            var finalPaths = new List<(string, XDocument)>();
            foreach (var path in paths.Select(p => p.ToLowerInvariant()))
            {
                if (finalPaths.Any(p => p.Item1 == path))
                {
                    continue;
                }

                var extension = this.path.GetExtension(path);

                if (extension == "csproj")
                {
                    (bool, XDocument) processResult = ProcessCsprojFile(path);
                    if (processResult.Item1)
                    {
                        finalPaths.Add((path, processResult.Item2));

                        var path2 = this.path.ChangeExtension(path, "nuspec");

                        (bool, XDocument) processResult2 = ProcessNuspecFile(path2);
                        if (processResult2.Item1)
                        {
                            finalPaths.Add((path2, processResult2.Item2));
                        }
                    }
                }
                else if (extension == "nuspec")
                {
                    (bool, XDocument) processResult = ProcessNuspecFile(path);
                    if (processResult.Item1)
                    {
                        finalPaths.Add((path, processResult.Item2));

                        var path2 = this.path.ChangeExtension(path, "csproj");

                        (bool, XDocument) processResult2 = ProcessCsprojFile(path2);
                        if (processResult2.Item1)
                        {
                            finalPaths.Add((path2, processResult2.Item2));
                        }
                    }
                }
                else
                {
                    (bool, XDocument) processResult = ProcessCsprojFile(path);
                    if (processResult.Item1)
                    {
                        finalPaths.Add((path, processResult.Item2));
                    }
                }

                // .csproj file handling
                (bool, XDocument) ProcessCsprojFile(string fileName)
                {
                    if (!this.file.Exists(fileName))
                    {
                        return (false, null);
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
                        return (false, null);
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

                    return (true, document);
                }

                // .nuspec file handling
                (bool, XDocument) ProcessNuspecFile(string fileName)
                {
                    if (!this.file.Exists(fileName))
                    {
                        return (false, null);
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
                        return (false, null);
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

                    return (true, document);
                }
            }

            return finalPaths;
        }
    }
}