using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.PlusExtensions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrispPlus.Patches
{
    [HarmonyPatch]
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "World", "Custom Hall Door")]
    internal class GenerateDoorTexPatches
    {
        internal static Texture2D wallTextureForDoor;

        [HarmonyPatch(typeof(RoomController))]
        [HarmonyPatch("GenerateDoorTextures")]
        [HarmonyPrefix]
        static void GenerateDoorTexturesPrefix(RoomController __instance)
        {
            if (__instance.name == "NullRoom") { wallTextureForDoor = null; return; }
            wallTextureForDoor = __instance.wallTex;
        }

        [HarmonyPatch(typeof(RoomController))]
        [HarmonyPatch("GenerateDoorTextures")]
        [HarmonyPostfix]
        static void GenerateDoorTexturesPostfix()
        {
            wallTextureForDoor = null;
        }

        static Texture2D originalOpen;
        static Texture2D originalClose;

        [HarmonyPatch(typeof(EnvironmentController))]
        [HarmonyPatch("GetDoorMaterials")]
        [HarmonyPrefix]
        static void GetDoorMaterialsPrefix(StandardDoorMats doorMats)
        {
            if (wallTextureForDoor == null) return;
            if (doorMats != CrispyPlugin.Instance.hallwayDoorMat) return;
            originalOpen = doorMats.openPosterData.baseTexture;
            originalClose = doorMats.shutPosterData.baseTexture;
            // this is ugly. yuck
            doorMats.openPosterData.baseTexture = CrispyPlugin.Instance.GetOrGenerateColoredTexture(wallTextureForDoor, true);
            doorMats.shutPosterData.baseTexture = CrispyPlugin.Instance.GetOrGenerateColoredTexture(wallTextureForDoor, false);
            ((ExtendedPosterObject)doorMats.openPosterData).mirroredPoster.baseTexture = CrispyPlugin.Instance.GetOrGenerateColoredTexture(wallTextureForDoor, true);
            ((ExtendedPosterObject)doorMats.shutPosterData).mirroredPoster.baseTexture = CrispyPlugin.Instance.GetOrGenerateColoredTexture(wallTextureForDoor, false);
        }
        [HarmonyPatch(typeof(EnvironmentController))]
        [HarmonyPatch("GetDoorMaterials")]
        [HarmonyPostfix]
        static void GetDoorMaterialsPostfix(StandardDoorMats doorMats)
        {
            if (wallTextureForDoor == null) return;
            if (doorMats != CrispyPlugin.Instance.hallwayDoorMat) return;
            doorMats.openPosterData.baseTexture = originalOpen;
            doorMats.shutPosterData.baseTexture = originalClose;
            // this is ugly. yuck
            doorMats.openPosterData.baseTexture = originalOpen;
            doorMats.shutPosterData.baseTexture = originalClose;
            ((ExtendedPosterObject)doorMats.openPosterData).mirroredPoster.baseTexture = originalOpen;
            ((ExtendedPosterObject)doorMats.shutPosterData).mirroredPoster.baseTexture = originalClose;
        }
    }
}
