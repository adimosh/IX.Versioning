// <copyright file="CsprojFileParser.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IX.StandardExtensions;
using IX.StandardExtensions.Globalization;
using IX.System.IO;

namespace IX.Versioning.Csproj
{
    /// <summary>
    /// A file parser for CSPROJ projects.
    /// </summary>
    /// <seealso cref="IX.Versioning.IVersioningFileParser" />
    public class CsprojFileParser : IVersioningFileParser
    {
        private static readonly Regex AssemblyVersionRegex = new Regex(@"^\s*\[\s*assembly\s*:\s*(?:global::)?(?:System.Reflection.)?AssemblyVersion(?:Attribute)?\(\s*""(?<versionString>.*)""\s*\)\s*\]\s*$");
        private static readonly Regex AssemblyFileVersionRegex = new Regex(@"^\s*\[\s*assembly\s*:\s*(?:global::)?(?:System.Reflection.)?AssemblyFileVersion(?:Attribute)?\(\s*""(?<versionString>.*)""\s*\)\s*\]\s*$");

        private readonly IFile file;
        private readonly IPath path;
        private readonly IDirectory directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsprojFileParser" /> class.
        /// </summary>
        /// <param name="file">The file provider.</param>
        /// <param name="path">The path provider.</param>
        /// <param name="directory">The directory.</param>
        /// <exception cref="global::System.ArgumentNullException"><paramref name="file" />
        /// or
        /// <paramref name="path" />
        /// or
        /// <paramref name="directory"/>
        /// is <c>null</c> (<c>Nothing</c> in Visual Basic).</exception>
        public CsprojFileParser(IFile file, IPath path, IDirectory directory)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        /// <summary>
        /// Processes the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="newMajorVersion">The new major version.</param>
        /// <param name="newMinorVersion">The new minor version.</param>
        /// <param name="newBuildVersion">The new build version.</param>
        /// <param name="newRevisionVersion">The new revision version.</param>
        /// <param name="newVersionSuffix">The new version suffix.</param>
        /// <param name="noRevision">If set to <c>true</c>, there is no revision in the final version.</param>
        /// <returns><c>true</c> if the document has been processed correctly, or <c>false</c> if it doesn't exist, or there is nothing to process.</returns>
        public bool ProcessFile(string fileName, int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false)
        {
            if (!this.file.Exists(fileName))
            {
                return false;
            }

            XDocument document;
            try
            {
                using (global::System.IO.Stream s = this.file.OpenRead(fileName))
                {
                    document = XDocument.Load(s, LoadOptions.PreserveWhitespace);
                }
            }
            catch
            {
                return false;
            }

            XElement root = document.Root;

            if (root == null)
            {
                return false;
            }

            XElement missingContainer = null;

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
            (var ReleaseVersion, var FileVersion, var AssemblyVersion, var FileVersionClassic, var AssemblyVersionClassic) = VersionElementsHelper.VersionStrings(newMajorVersion, newMinorVersion, newBuildVersion, newRevisionVersion, newVersionSuffix, noRevision);
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

            var isCore = root.Attribute("Sdk")?.Value?.InvariantCultureEqualsInsensitive("Microsoft.NET.Sdk") ?? false;

            if (isCore)
            {
                IEnumerable<XElement> xVersions = root.Descendants("PropertyGroup").Descendants("Version");
                EnsureCorrectOnlyOneVersion(
                    missingContainer,
                    root,
                    xVersions,
                    FileVersion,
                    "Version");

                IEnumerable<XElement> xFileVersions = root.Descendants("PropertyGroup").Descendants("FileVersion");
                EnsureCorrectOnlyOneVersion(
                    missingContainer,
                    root,
                    xFileVersions,
                    FileVersion,
                    "FileVersion");

                IEnumerable<XElement> xAssemblyVersions = root.Descendants("PropertyGroup").Descendants("AssemblyVersion");
                EnsureCorrectOnlyOneVersion(
                    missingContainer,
                    root,
                    xAssemblyVersions,
                    AssemblyVersion,
                    "AssemblyVersion");

                void EnsureCorrectOnlyOneVersion(XElement missingElementContainerBase, in XElement rootElementContainer, in IEnumerable<XElement> xElements, in string correctVersion, in string versionName)
                {
                    XElement xElement = xElements.FirstOrDefault();

                    if (xElement == null)
                    {
                        EnsureMissingContainerExists(missingElementContainerBase, rootElementContainer);

                        xElement = new XElement(versionName);

                        missingElementContainerBase.Add(xElement);

                        void EnsureMissingContainerExists(XElement missingElementContainer, in XElement rootContainer)
                        {
                            if (missingElementContainer != null)
                            {
                                return;
                            }

                            missingElementContainer = new XElement("PropertyGroup");

                            rootContainer.Add(missingElementContainer);
                        }
                    }

                    xElement.SetValue(correctVersion);

                    xElements.Where(p => p != xElement).ForEach(p => p.Remove());
                }

                using (global::System.IO.Stream s = this.file.Create(fileName))
                {
                    document.Save(s);
                }

                return true;
            }
            else
            {
                var folderPath = this.path.GetDirectoryName(fileName);

                var foundAssemblyAttributes = false;

                this.directory
                    .EnumerateFilesRecursively(folderPath, "*.cs")
#if NETSTANDARD1_0
                    .ForEach(filePath =>
#else
                    .ParallelForEach(filePath =>
#endif
                    {
                        var lines = new List<string>();
                        var found = false;
                        using (global::System.IO.StreamReader handle = this.file.OpenText(filePath))
                        {
                            while (!handle.EndOfStream)
                            {
                                var line = handle.ReadLine();

                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    Match versionMatch = AssemblyVersionRegex.Match(line);
                                    if (versionMatch.Success)
                                    {
                                        found = true;

                                        lines.Add($"[assembly: global::System.Reflection.AssemblyVersion(\"{AssemblyVersionClassic}\")]");
                                    }
                                    else
                                    {
                                        Match assemblyVersionMatch = AssemblyFileVersionRegex.Match(line);
                                        if (assemblyVersionMatch.Success)
                                        {
                                            found = true;

                                            lines.Add($"[assembly: global::System.Reflection.AssemblyFileVersion(\"{FileVersionClassic}\")]");
                                        }
                                        else
                                        {
                                            lines.Add(line);
                                        }
                                    }
                                }
                                else
                                {
                                    lines.Add(string.Empty);
                                }
                            }
                        }

                        if (found)
                        {
                            var finalText = string.Join(Environment.NewLine, lines);
                            using (global::System.IO.StreamWriter writeHandle = this.file.CreateText(filePath))
                            {
                                writeHandle.Write(finalText);
                            }

                            foundAssemblyAttributes = true;
                        }

                        lines.Clear();
                    });

                return foundAssemblyAttributes;
            }
        }

        /// <summary>
        /// Processes the solution.
        /// </summary>
        /// <param name="fileOrFolderName">Name of the file or folder.</param>
        /// <param name="newMajorVersion">The new major version.</param>
        /// <param name="newMinorVersion">The new minor version.</param>
        /// <param name="newBuildVersion">The new build version.</param>
        /// <param name="newRevisionVersion">The new revision version.</param>
        /// <param name="newVersionSuffix">The new version suffix.</param>
        /// <param name="noRevision">If set to <c>true</c>, there is no revision in the final version.</param>
        /// <returns><c>true</c> if the solution has been processed correctly, or <c>false</c> if it doesn't exist, or there is nothing to process.</returns>
        public bool ProcessSolution(string fileOrFolderName, int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false)
        {
            string path;

            if (this.file.Exists(fileOrFolderName ?? throw new ArgumentNullException(nameof(fileOrFolderName))))
            {
                path = this.path.GetDirectoryName(fileOrFolderName);
            }
            else if (this.directory.Exists(fileOrFolderName))
            {
                path = fileOrFolderName;
            }
            else
            {
                return false;
            }

            var found = false;

            this.directory
                .EnumerateFilesRecursively(path, "*.csproj")
#if NETSTANDARD1_0
                    .ForEach(filePath =>
#else
                    .ParallelForEach(filePath =>
#endif
                    {
                        this.ProcessFile(filePath, newMajorVersion, newMinorVersion, newBuildVersion, newRevisionVersion, newVersionSuffix, noRevision);
                    });

            return found;
        }
    }
}