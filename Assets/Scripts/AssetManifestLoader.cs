using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public static class AssetManifestLoader
    {
        public static AssetManifest Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AssetManifest>(json);
        }
        
        public static AssetManifest LoadChunkManifest()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "levels", "level_path_bg_manifest.json");
            var manifest = Load(path);

            if (manifest != null && manifest.assets != null)
            {
                manifest.assets = manifest.assets.OrderBy(x => x.index).ToList();
            }

            return manifest;
        }
    }
    
    
    
    public class AssetManifest
    {
        public List<AssetMetadata> assets;
    }

    public class AssetMetadata
    {
        public string address;
        public int width;
        public int height;
        public int index;
    }
}