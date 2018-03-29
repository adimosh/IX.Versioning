// <copyright file="NuSpecFileParser.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IX.StandardExtensions;
using IX.System.IO;

namespace IX.Versioning.NuSpec
{
    /// <summary>
    /// A file parser for NUSPEC projects.
    /// </summary>
    /// <seealso cref="IX.Versioning.IVersioningFileParser" />
    public class NuSpecFileParser : IVersioningFileParser
    {
        private readonly IFile file;
        private readonly IPath path;
        private readonly IDirectory directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuSpecFileParser" /> class.
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
        public NuSpecFileParser(IFile file, IPath path, IDirectory directory)
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

            XElement root = document.Descendants("package").FirstOrDefault();

            XElement missingContainer = root.Descendants("metadata").FirstOrDefault();
            if (missingContainer == null)
            {
                missingContainer = new XElement("metadata");
            }

            root.Add(missingContainer);

            (var releaseVersion, var fileVersion, var assemblyVersion, var fileVersionClassic, var assemblyVersionClassic) = VersionElementsHelper.VersionStrings(newMajorVersion, newMinorVersion, newBuildVersion, newRevisionVersion, newVersionSuffix, noRevision);

            IEnumerable<XElement> xVersions = missingContainer.Descendants("version");
            EnsureCorrectOnlyOneVersion(
                xVersions,
                fileVersion,
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

            using (global::System.IO.Stream s = this.file.Create(fileName))
            {
                document.Save(s);
            }

            return true;
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

            this.directory.EnumerateFilesRecursively(path, "*.nuspec").
#if NETSTANDARD1_0
                ForEach
#else
                ParallelForEach
#endif
#pragma warning disable SA1110 // Opening parenthesis or bracket should be on declaration line
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
                (csprojFile =>
                {
                    this.ProcessFile(csprojFile, newMajorVersion, newMinorVersion, newBuildVersion, newRevisionVersion, newVersionSuffix, noRevision);
                });
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
#pragma warning restore SA1110 // Opening parenthesis or bracket should be on declaration line

            return found;
        }
    }
}