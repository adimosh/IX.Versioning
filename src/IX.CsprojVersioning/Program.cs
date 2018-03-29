// <copyright file="Program.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IX.System.IO;
using IX.Versioning;

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

            var paths = args.Where(p => !string.IsNullOrWhiteSpace(p) && p != version && p != "Release").Distinct().ToArray();

            var documents = new Dictionary<string, XDocument>(paths.Length);

            (var success, var majorVersion, var minorVersion, var buildVersion, var revisionVersion, var versionSuffix) = VersionElementsHelper.GetVersionElementsFromString(version);

            if (!success)
            {
                // Version string not good
                return 2;
            }

            IDirectory directory = new Directory();

            if (paths.Length == 0)
            {
                paths = new[] { directory.GetCurrentDirectory() };
            }

            IFile file = new File();
            IPath path = new Path();

            var service = new FileParserService(file, path, directory);

            var result = service.ProcessPaths(paths, majorVersion, minorVersion, buildVersion, revisionVersion ?? 0, versionSuffix, release);
            if (!result)
            {
                // Specified path - failure
                return 4;
            }

            // Success
            return 0;
        }
    }
}