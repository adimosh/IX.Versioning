// <copyright file="FileParserService.cs" company="Adrian Mos">
// Copyright (c) Adrian Mos with all rights reserved. Part of the IX Framework.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using IX.System.IO;
using IX.Versioning.Csproj;
using IX.Versioning.NuSpec;

namespace CsprojVersioning
{
    internal class FileParserService
    {
        private readonly IPath path;

        private readonly CsprojFileParser csprojParser;
        private readonly NuSpecFileParser nuspecParser;

        internal FileParserService(IFile file, IPath path, IDirectory directory)
        {
            this.path = path;

            this.csprojParser = new CsprojFileParser(file, path, directory);
            this.nuspecParser = new NuSpecFileParser(file, path, directory);
        }

        internal bool ProcessPaths(IEnumerable<string> paths, int majorVersion, int minorVersion, int buildVersion, int revisionVersion, string preReleaseVersion, bool release)
        {
            var finalPaths = new List<string>();
            foreach (var path in paths.Select(p => p.ToLowerInvariant()))
            {
                if (finalPaths.Contains(path))
                {
                    continue;
                }

                var extension = this.path.GetExtension(path);

                if (extension == ".csproj")
                {
                    var processResult = this.csprojParser.ProcessFile(path, majorVersion, minorVersion, buildVersion, revisionVersion, preReleaseVersion, release);
                    if (processResult)
                    {
                        finalPaths.Add(path.ToLowerInvariant());

                        var path2 = this.path.ChangeExtension(path, "nuspec");

                        var processResult2 = this.nuspecParser.ProcessFile(path2, majorVersion, minorVersion, buildVersion, revisionVersion, preReleaseVersion, release);
                        if (processResult2)
                        {
                            finalPaths.Add(path.ToLowerInvariant());
                        }
                    }
                }
                else if (extension == ".nuspec")
                {
                    var processResult = this.nuspecParser.ProcessFile(path, majorVersion, minorVersion, buildVersion, revisionVersion, preReleaseVersion, release);
                    if (processResult)
                    {
                        finalPaths.Add(path.ToLowerInvariant());

                        var path2 = this.path.ChangeExtension(path, "csproj");

                        var processResult2 = this.csprojParser.ProcessFile(path2, majorVersion, minorVersion, buildVersion, revisionVersion, preReleaseVersion, release);
                        if (processResult2)
                        {
                            finalPaths.Add(path.ToLowerInvariant());
                        }
                    }
                }
                else
                {
                    var processResult = this.nuspecParser.ProcessFile(path, majorVersion, minorVersion, buildVersion, revisionVersion, preReleaseVersion, release);
                    if (processResult)
                    {
                        finalPaths.Add(path.ToLowerInvariant());
                    }
                }
            }

            return finalPaths.Count > 0;
        }
    }
}