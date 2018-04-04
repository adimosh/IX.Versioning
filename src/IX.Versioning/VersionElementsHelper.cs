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

        private static readonly Regex VersionRegex = new Regex(@"^(?<majorVersion>\d{1,}).(?<minorVersion>\d{1,}).(?<buildVersion>\d{1,})(?:.(?<revisionVersion>\d{1,}))?(?:-(?<prereleaseVersion>[\d-\w]{1,}))?$");

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
        public static (string releaseVersion, string packageVersion, string fileVersion, string assemblyVersion) VersionStrings(int newMajorVersion, int newMinorVersion, int newBuildVersion, int newRevisionVersion = 0, string newVersionSuffix = null, bool noRevision = false)
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

                    suffix = $"-{alphabeta}{match.Groups["number"].Value}";
                }
                else
                {
                    suffix = "-alpha";
                }
            }

            return (
                $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}{suffix}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}{suffix}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.{newRevisionVersion}{suffix}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.{newRevisionVersion}",
                noRevision ?
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}" :
                    $"{newMajorVersion}.{newMinorVersion}.{newBuildVersion}.0");
        }

        /// <summary>
        /// Gets the version elements from an input string.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The version elements, as a tuple.</returns>
        public static (bool success, int majorVersion, int minorVersion, int buildVersion, int? revisionVersion, string versionSuffix) GetVersionElementsFromString(string version)
        {
            Match versionMatch = VersionRegex.Match(version);

            if (!versionMatch.Success)
            {
                return (false, 0, 0, 0, null, null);
            }

            var majorVersion = versionMatch.Groups["majorVersion"]?.Value;
            var minorVersion = versionMatch.Groups["minorVersion"]?.Value;
            var buildVersion = versionMatch.Groups["buildVersion"]?.Value;
            var revisionVersion = versionMatch.Groups["revisionVersion"]?.Value;
            var prereleaseVersion = versionMatch.Groups["prereleaseVersion"]?.Value;

            if (string.IsNullOrWhiteSpace(majorVersion) || string.IsNullOrWhiteSpace(minorVersion) || string.IsNullOrWhiteSpace(buildVersion))
            {
                return (false, 0, 0, 0, null, null);
            }

            if (!int.TryParse(majorVersion, out var newMajorVersion) ||
                !int.TryParse(minorVersion, out var newMinorVersion) ||
                !int.TryParse(buildVersion, out var newBuildVersion))
            {
                return (false, 0, 0, 0, null, null);
            }

            if (!int.TryParse(revisionVersion, out var newRevisionVersion))
            {
                return (true, newMajorVersion, newMinorVersion, newBuildVersion, null, prereleaseVersion);
            }
            else
            {
                return (true, newMajorVersion, newMinorVersion, newBuildVersion, newRevisionVersion, prereleaseVersion);
            }
        }
    }
}