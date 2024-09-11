using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CrispPlus
{
    [Serializable]
    public class SearchCriteria
    {
        public string searchType = "Name";
        public string[] searchValues = new string[0];

        public T[] PerformSearch<T>() where T : UnityEngine.Object
        {
            switch (searchType)
            {
                case "Name":
                    return Resources.FindObjectsOfTypeAll<T>().Where(x => searchValues.Contains(x.name)).ToArray();
            }
            //CrispyPlugin.Log.LogWarning("Unimplemented search type:" + searchType);
            return new T[0];
        }

        public RoomAsset[] PerformSearch()
        {
            RoomAsset[] standardSearchResults = PerformSearch<RoomAsset>();
            if (standardSearchResults.Length != 0) return standardSearchResults;
            switch (searchType)
            {
                case "RoomFunction":
                    return Resources.FindObjectsOfTypeAll<RoomAsset>().Where(x => (x.roomFunctionContainer != null && searchValues.Contains(x.roomFunctionContainer.name))).ToArray();
                case "Character":
                    return NPCMetaStorage.Instance.Find(x => searchValues.Contains(EnumExtensions.GetExtendedName<Character>((int)x.character))).value.potentialRoomAssets.Select(x => x.selection).ToArray();
            }
            CrispyPlugin.Log.LogWarning("Unimplemented search type:" + searchType);
            return new RoomAsset[0];
        }
    }

    public static class SearchCriteriaExtensions
    {
        public static T[] PerformSearch<T>(this SearchCriteria[] me) where T : UnityEngine.Object
        {
            return me.SelectMany(x => x.PerformSearch<T>()).Distinct().ToArray();
        }

        public static RoomAsset[] PerformSearch(this SearchCriteria[] me)
        {
            return me.SelectMany(x => x.PerformSearch()).Distinct().ToArray();
        }
    }

    public class RoomOverride
    {
        public SearchCriteria[] searchCriterias = new SearchCriteria[0];
        public string textureName = "";
        public SerializableColor color = new SerializableColor();
    }
}
