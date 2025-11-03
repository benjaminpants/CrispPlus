using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CrispPlus
{
    public static class MapTweaksHandler
    {
        public static List<RoomOverride> overrides = new List<RoomOverride>();
        public static Dictionary<string, Material> mapMaterials = new Dictionary<string, Material>();

        public static void LoadOverridesFolder(string path)
        {
            string[] jsons = Directory.GetFiles(path, "*.json");
            List<RoomOverride> overrides = new List<RoomOverride>();
            Texture2D[] textures = AssetLoader.TexturesFromFolder(path, "*.png");
            Dictionary<string, Texture2D> textureMap = new Dictionary<string, Texture2D>();
            textures.Do(x => textureMap.Add(x.name, x));
            for (int i = 0; i < jsons.Length; i++)
            {
                RoomOverride over = JsonConvert.DeserializeObject<RoomOverride>(File.ReadAllText(jsons[i]));
                if (over != null) overrides.Add(over);
            }
            foreach (RoomOverride over in overrides)
            {
                MapTweaksHandler.overrides.Add(over);
                if (mapMaterials.ContainsKey(over.textureName)) continue;
                mapMaterials.Add(over.textureName, ObjectCreators.CreateMapTileShader(textureMap[over.textureName]));
            }
        }

        public static void ApplyReplacements()
        {
            foreach (RoomOverride over in overrides)
            {
                RoomAsset[] assets = over.searchCriterias.PerformSearch();
                if (assets.Length == 0) continue; //saves time
                Material roomMat = null;
                if (!string.IsNullOrEmpty(over.textureName))
                {
                    roomMat = mapMaterials[over.textureName];
                }
                assets.Do(x =>
                {
                    x.mapMaterial = (roomMat == null) ? x.mapMaterial : roomMat;
                    x.color = Color.Lerp(x.color, over.color, over.color.A / 255f);
                });
            }
        }
    }
}
