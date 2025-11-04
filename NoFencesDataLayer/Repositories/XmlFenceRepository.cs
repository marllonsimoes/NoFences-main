using NoFences.Core.Model;
using NoFences.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// XML file-based repository for FenceInfo.
    /// Stores each fence in its own directory with a metadata XML file.
    ///
    /// Structure:
    /// - AppData/Fences/
    ///   - {fence-guid}/
    ///     - __fence_metadata.xml
    /// </summary>
    public class XmlFenceRepository : IFenceRepository
    {
        private const string MetaFileName = "__fence_metadata.xml";
        private const string FencesSubfolder = "Fences";

        private readonly string basePath;
        private readonly XmlSerializer serializer;

        /// <summary>
        /// Creates a new XmlFenceRepository with default storage location.
        /// </summary>
        public XmlFenceRepository()
        {
            // Setup storage path in AppData
            AppEnvUtil.EnsureAppEnvironmentPathExists();
            AppEnvUtil.EnsureDirectoryExists(FencesSubfolder);
            basePath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), FencesSubfolder);

            serializer = new XmlSerializer(typeof(FenceInfo));

            Debug.WriteLine($"XmlFenceRepository: Initialized with base path: {basePath}");
        }

        /// <summary>
        /// Creates a new XmlFenceRepository with custom storage location.
        /// </summary>
        /// <param name="customBasePath">Custom path for fence storage</param>
        public XmlFenceRepository(string customBasePath)
        {
            if (string.IsNullOrEmpty(customBasePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(customBasePath));

            basePath = customBasePath;
            serializer = new XmlSerializer(typeof(FenceInfo));

            // Ensure directory exists
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            Debug.WriteLine($"XmlFenceRepository: Initialized with custom path: {basePath}");
        }

        public IEnumerable<FenceInfo> GetAll()
        {
            var fences = new List<FenceInfo>();

            try
            {
                if (!Directory.Exists(basePath))
                {
                    Debug.WriteLine("XmlFenceRepository: No fences directory found");
                    return fences;
                }

                foreach (var dir in Directory.EnumerateDirectories(basePath))
                {
                    try
                    {
                        var metaFile = Path.Combine(dir, MetaFileName);
                        if (!File.Exists(metaFile))
                        {
                            Debug.WriteLine($"XmlFenceRepository: Skipping {dir} - no metadata file");
                            continue;
                        }

                        var fence = LoadFromFile(metaFile);
                        if (fence != null)
                        {
                            fences.Add(fence);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"XmlFenceRepository: Error loading fence from {dir}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"XmlFenceRepository: Loaded {fences.Count} fences");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"XmlFenceRepository: Error in GetAll: {ex.Message}");
            }

            return fences;
        }

        public FenceInfo GetById(Guid id)
        {
            try
            {
                var fencePath = GetFencePath(id);
                var metaFile = Path.Combine(fencePath, MetaFileName);

                if (!File.Exists(metaFile))
                {
                    Debug.WriteLine($"XmlFenceRepository: Fence {id} not found");
                    return null;
                }

                return LoadFromFile(metaFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"XmlFenceRepository: Error loading fence {id}: {ex.Message}");
                return null;
            }
        }

        public bool Save(FenceInfo fence)
        {
            if (fence == null)
            {
                Debug.WriteLine("XmlFenceRepository: Cannot save null fence");
                return false;
            }

            try
            {
                var fencePath = GetFencePath(fence.Id);
                EnsureDirectoryExists(fencePath);

                var metaFile = Path.Combine(fencePath, MetaFileName);

                using (var writer = new StreamWriter(metaFile))
                {
                    serializer.Serialize(writer, fence);
                }

                Debug.WriteLine($"XmlFenceRepository: Saved fence {fence.Id} ({fence.Name})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"XmlFenceRepository: Error saving fence {fence.Id}: {ex.Message}");
                return false;
            }
        }

        public bool Delete(Guid id)
        {
            try
            {
                var fencePath = GetFencePath(id);

                if (!Directory.Exists(fencePath))
                {
                    Debug.WriteLine($"XmlFenceRepository: Fence {id} not found for deletion");
                    return false;
                }

                Directory.Delete(fencePath, recursive: true);
                Debug.WriteLine($"XmlFenceRepository: Deleted fence {id}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"XmlFenceRepository: Error deleting fence {id}: {ex.Message}");
                return false;
            }
        }

        public bool Exists(Guid id)
        {
            try
            {
                var fencePath = GetFencePath(id);
                var metaFile = Path.Combine(fencePath, MetaFileName);
                return File.Exists(metaFile);
            }
            catch
            {
                return false;
            }
        }

        public int Count()
        {
            try
            {
                if (!Directory.Exists(basePath))
                    return 0;

                return Directory.EnumerateDirectories(basePath)
                    .Count(dir => File.Exists(Path.Combine(dir, MetaFileName)));
            }
            catch
            {
                return 0;
            }
        }

        #region Private Helper Methods

        private FenceInfo LoadFromFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    return serializer.Deserialize(reader) as FenceInfo;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"XmlFenceRepository: Error deserializing {filePath}: {ex.Message}");
                return null;
            }
        }

        private string GetFencePath(Guid id)
        {
            return Path.Combine(basePath, id.ToString());
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        #endregion
    }
}
