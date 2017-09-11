// <copyright file="Program.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

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

                paths = directory.EnumerateFiles(".", "*.csproj").ToArray();
            }

            IFile file = new File();
            IPath path = new Path();

            var service = new XmlFileParserService(file, path);

            IEnumerable<(string, XDocument)> processedPaths = service.ProcessPaths(paths, version, majorVersion, minorVersion, buildVersion, revisionVersion, prereleaseVersion, release);
            if (!processedPaths.Any())
            {
                // Specified path - failure
                return 4;
            }

            processedPaths.ForEach(p =>
            {
                using (System.IO.Stream s = file.Create(p.Item1))
                {
                    using (var writer = XmlWriter.Create(s, new XmlWriterSettings { OmitXmlDeclaration = true, }))
                    {
                        p.Item2.Save(writer);
                    }
                }
            });

            // Success
            return 0;
        }
    }
}