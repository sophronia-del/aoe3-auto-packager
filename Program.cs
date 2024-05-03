using System.Collections.Immutable;
using System.Linq;

namespace aoe3_auto_packager
{
    internal class Program
    {
        private static readonly HashSet<string> s_extensions = ["blueprint", "physics", "tactics", "xml"];

        static void Main(string[] args)
        {
            var begin = DateTime.Now;
            Console.WriteLine($"Start at ${DateTime.Now}");

            Dictionary<string, List<string>> arguments = [];
            foreach (var arg in args)
            {
                var kv = arg.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    List<string>? collector = arguments.GetValueOrDefault(key);
                    if (collector == null)
                    {
                        collector = [];
                        arguments.Add(key, collector);
                    }
                    collector.Add(kv[1].Trim());
                }
            }

            string sourceDir = arguments.GetValueOrDefault("source", ["xml_data_source"])[0];
            string dataDir = arguments.GetValueOrDefault("data", ["Data"])[0];
            string suffix = arguments.GetValueOrDefault("suffix", ["generated"])[0];

            List<string> rawFilter = arguments.GetValueOrDefault("filter", []);
            var pathFilter = rawFilter.
                Select(s => Path.Join(sourceDir, s.Replace('/', '\\'))).
                ToImmutableSortedSet();

            Console.WriteLine($"Creating bar file based on [{dataDir}] with data source from [{sourceDir}], using file filter [{string.Join(',', pathFilter)}]");

            List<string> files = [];
            CollectXmlFiles(sourceDir, files, pathFilter);
            List<Task> tasks = [];
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                Console.WriteLine($"Collected XML file: {relative}");
                var targetFile = Path.Combine(dataDir, relative);

                var task = XMBFile.CreateXMBFileALZ4(file, targetFile + ".xmb");
                tasks.Add(task);
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }

            if (string.IsNullOrEmpty(suffix))
            {
                BarFile.Create(dataDir, "Data").Wait();
            }
            else
            {
                BarFile.Create(dataDir, "Data_" + suffix).Wait();
            }

            var cost = (DateTime.Now - begin).TotalMilliseconds;
            Console.WriteLine($"Finished. Time Cost: {cost} ms");
        }

        private static void CollectXmlFiles(string current, List<string> container, ISet<string> pathFilter)
        {
            string[] directDirectories = Directory.GetDirectories(current);
            foreach (var dir in directDirectories)
            {
                if (Path.GetFileName(dir)!.StartsWith('.'))
                {
                    continue;
                }

                CollectXmlFiles(dir, container, pathFilter);
            }

            string[] files = Directory.GetFiles(current);
            foreach(var file in files)
            {
                if (pathFilter.Count > 0 && !pathFilter.Contains(file))
                {
                    continue;
                }

                int last = file.LastIndexOf('.');
                if (last >= 0 && last < file.Length - 1)
                {
                    string extension = file[(last + 1)..];
                    if (s_extensions.Contains(extension))
                    {
                        container.Add(file);
                    }
                }
            }
        }
    }
}
