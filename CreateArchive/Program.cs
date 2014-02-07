using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace CreateArchive
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = args[0];
            var filename = args[1];

            if (File.Exists(filename))
                File.Delete(filename);

            using (var archive = ZipFile.Open(filename, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file) != ".pdb")
                    {
                        System.Console.WriteLine(file.Replace(dir, ""));
                        archive.CreateEntryFromFile(file, file.Replace(dir, ""));
                    }
                }
            }
        }
    }
}
