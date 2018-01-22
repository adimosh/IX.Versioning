// <copyright file="VersionElementsHelper.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System.Text.RegularExpressions;

namespace IX.Versioning
{
    /// <summary>
    /// Helper class for version elements.
    /// </summary>
    public static class VersionElementsHelper
    {
        /// <summary>
        /// A regex to check the version suffix.
        /// </summary>
        private static readonly Regex SuffixRegex = new Regex("^(?<alphabeta>alpha|beta|prealpha|prebeta|pre-alpha|pre-beta)(?<number>\\d*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Versions the strings.
        /// </summary>
        /// <param name="newMajorVersion">The new major version.</param>
        /// <param name="newMinorVersion">The new minor version.</param>
        /// <param name="newBuildVersion">The new build version.</param>
        /// <param name="newRevisionVersion">The new revision version.</param>
        /// <param name="newVersionSuffix">The new version suffix.</param>
        /// <param name="noRevision">if set to <c>true</c>, no revision version is set.</param>
        /// <returns>System.String.</returns>
        public static (string ReleaseVersion, string FileVersion, string AssemblyVersion, string FileVersionClassic, string AssemblyVersionClassic) VersionStrings(int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false)
        {
            string suffix;
            if (string.IsNullOrWhiteSpace(newVersionSuffix))
            {
                suffix = string.Empty;
            }
            else
            {
                Match match = SuffixRegex.Match(newVersionSuffix);

                if (match.Success)
                {
                    var alphabeta = match.Groups["alphabeta"].Value;

                    if (string.IsNullOrWhiteSpace(alphabeta))
                    {
                        alphabeta = "alpha";
                    }

                    suffix = $"{alphabeta}{match.Groups["number"].Value}";
                }
                else
                {
                    suffix = "alpha";
                }
            }

            return (
                $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}{suffix}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}{suffix}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.{newRevisionVersion}{suffix}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}{suffix}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.0{suffix}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.{newRevisionVersion}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.0");
        }
    }
}