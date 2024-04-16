namespace aoe3_auto_packager
{
    internal class Program
    {
        private static readonly HashSet<string> s_extensions = ["blueprint", "physics", "tactics", "xml"];

        static void Main(string[] args)
        {
            var begin = DateTime.Now;

            Dictionary<string, string> arguments = [];
            foreach (var arg in args)
            {
                var kv = arg.Split('=');
                if (kv.Length == 2)
                {
                    arguments.Add(kv[0].Trim().ToLower(), kv[1].Trim());
                }
            }

            string sourceDir = arguments.GetValueOrDefault("source", "xml_data_source");
            string dataDir = arguments.GetValueOrDefault("data", "Data");
            string suffix = arguments.GetValueOrDefault("suffix", "generated");

            Console.WriteLine($"Creating bar file based on [{dataDir}] with data source from [{sourceDir}]");

            List<string> files = [];
            CollectXmlFiles(sourceDir, files);
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

            var cost = (DateTime.Now - begin).Milliseconds;
            Console.WriteLine($"Finished. Time Cost: {cost} ms");
        }

        private static void CollectXmlFiles(string current, List<string> container)
        {
            string[] directDirectories = Directory.GetDirectories(current);
            foreach (var dir in directDirectories)
            {
                if (Path.GetFileName(dir)!.StartsWith('.'))
                {
                    continue;
                }

                CollectXmlFiles(dir, container);
            }

            string[] files = Directory.GetFiles(current);
            foreach(var file in files)
            {
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
