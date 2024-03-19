namespace aoe3_auto_packager
{
    internal class Program
    {
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

            Console.WriteLine($"Creating bar file to [{dataDir}] with data source from [{sourceDir}]");

            var files = Directory.EnumerateFiles(sourceDir, "*.xml", SearchOption.AllDirectories);
            List<Task> tasks = [];
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var targetFile = Path.Combine(dataDir, relative);

                var task = XMBFile.CreateXMBFileALZ4(file, targetFile + ".xmb");
                tasks.Add(task);
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }

            string suffix = "";
            string gitDir = Path.Combine(sourceDir, ".git");
            if (Directory.Exists(gitDir))
            {
                try {
                    string head = File.ReadAllText(Path.Combine(gitDir, "HEAD"));
                    if (head.StartsWith("ref:"))
                    {
                        string headFile = head.Substring(4).Trim();
                        suffix = "-" + File.ReadAllText(Path.Combine(gitDir, headFile)).Trim();
                    }
                } catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            BarFile.Create(dataDir, "Data_generated" + suffix).Wait();

            var cost = (DateTime.Now - begin).Milliseconds;
            Console.WriteLine($"Finished. Time Cost: {cost} ms");
        }
    }
}
