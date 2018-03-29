// <copyright file="IVersioningFileParser.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

namespace IX.Versioning
{
    /// <summary>
    /// A file parser for versioning.
    /// </summary>
    public interface IVersioningFileParser
    {
        /// <summary>
        /// Processes a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="newMajorVersion">The new major version.</param>
        /// <param name="newMinorVersion">The new minor version.</param>
        /// <param name="newBuildVersion">The new build version.</param>
        /// <param name="newRevisionVersion">The new revision version.</param>
        /// <param name="newVersionSuffix">The new version suffix.</param>
        /// <param name="noRevision">If set to <c>true</c>, there is no revision in the final version.</param>
        /// <returns><c>true</c> if the document has been processed correctly, or <c>false</c> if it doesn't exist, or there is nothing to process.</returns>
        bool ProcessFile(string fileName, int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false);

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
        bool ProcessSolution(string fileOrFolderName, int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false);
    }
}