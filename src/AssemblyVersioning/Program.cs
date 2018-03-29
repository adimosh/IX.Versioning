using IX.System.IO;
using IX.Versioning.Csproj;
using IX.Versioning.NuSpec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyVersioning
{
    public class Program
    {
        static void Main(string[] args)
        {
            IFile file = new File();
            IPath path = new Path();
            IDirectory directory = new Directory();

            CsprojFileParser csprojParser = new CsprojFileParser(file, path, directory);
            NuSpecFileParser nuspecParser = new NuSpecFileParser(file, path, directory);


        }
    }
}
