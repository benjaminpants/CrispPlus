using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CrispPlus
{
    public static class MapTweaksHandler
    {
        public static void LoadFolder(string path)
        {
            string[] jsons = Directory.GetFiles(path, "*.json");
            Texture2D[] textures = AssetLoader.TexturesFromFolder(path, "*.png");
            Dictionary<string, Texture2D> textureMap = new Dictionary<string, Texture2D>();
            textures.Do(x => textureMap.Add(x.name, x));
            List<RoomOverride> overrides = new List<RoomOverride>();
            for (int i = 0; i < jsons.Length; i++)
            {
                RoomOverride over = JsonConvert.DeserializeObject<RoomOverride>(File.ReadAllText(jsons[i]));
                if (over != null) overrides.Add(over);
            }
            foreach (RoomOverride over in overrides)
            {
                RoomAsset[] assets = over.searchCriterias.PerformSearch();
                if (assets.Length == 0) continue; //saves time and resources(we don't have to create a useless asset)
                Material roomMat = ObjectCreators.CreateMapTileShader(textureMap[over.textureName]);
                assets.Do(x =>
                {
                    x.mapMaterial = roomMat;
                    x.color = Color.Lerp(x.color, over.color, over.color.A / 255f);
                });
            }
        }
    }
}
