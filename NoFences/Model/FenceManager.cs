using NoFencesService.Util;
using System;
using System.IO;
using System.Xml.Serialization;
using NoFences.View.Fences.Handlers;

namespace NoFences.Model
{
    public class FenceManager
    {
        private const string MetaFileName = "__fence_metadata.xml";

        private readonly string basePath;
        private readonly FenceHandlerFactory _fenceHandlerFactory;

        public FenceManager(FenceHandlerFactory fenceHandlerFactory)
        {
            _fenceHandlerFactory = fenceHandlerFactory;
            AppEnvUtil.EnsureAppEnvironmentPathExists();
            AppEnvUtil.EnsureDirectoryExists("Fences");
            basePath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), "Fences");
        }

        public void LoadFences()
        {
            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var metaFile = Path.Combine(dir, MetaFileName);
                var serializer = new XmlSerializer(typeof(FenceInfo));
                var reader = new StreamReader(metaFile);
                var fence = serializer.Deserialize(reader) as FenceInfo;
                reader.Close();

                new FenceWindow(fence, _fenceHandlerFactory).Show();
            }
        }

        public void CreateFence(string name)
        {
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = name,
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };
            CreateFence(name, fenceInfo);
        }

        public void CreateFence(string name, FenceInfo fenceInfo)
        {
            UpdateFence(fenceInfo);
            new FenceWindow(fenceInfo, _fenceHandlerFactory).Show();
        }


        public void RemoveFence(FenceInfo info)
        {
            Directory.Delete(GetFolderPath(info), true);
            if (Directory.Exists(Path.Combine(basePath, info.Id.ToString()))) {
                Console.WriteLine($"Failed to delete {info.Id}");
            }
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            var path = GetFolderPath(fenceInfo);
            EnsureDirectoryExists(path);

            var metaFile = Path.Combine(path, MetaFileName);
            var serializer = new XmlSerializer(typeof(FenceInfo));
            var writer = new StreamWriter(metaFile);
            serializer.Serialize(writer, fenceInfo);
            writer.Close();
        }

        public void EnsureDirectoryExists(string dir)
        {
            var di = new DirectoryInfo(dir);
            if (!di.Exists)
                di.Create();
        }

        private string GetFolderPath(FenceInfo fenceInfo)
        {
            return Path.Combine(basePath, fenceInfo.Id.ToString());
        }
    }
}
