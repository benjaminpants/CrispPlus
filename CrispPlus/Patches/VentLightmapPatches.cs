using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CrispPlus.Patches
{

    internal class ConditionalPatchVent : ConditionalPatchConfig
    {
        public ConditionalPatchVent(string mod, string category, string name) : base(mod, category, name)
        {
        }

        public override bool ShouldPatch()
        {
            return base.ShouldPatch() && new ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Light Fix").ShouldPatch();
        }
    }


    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("InitializeLighting")]
    [ConditionalPatchVent("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Lighting")]
    static class InitializeLightingPatch
    {
        static void Postfix(EnvironmentController __instance)
        {
            __instance.gameObject.AddComponent<VentLightingHandler>();
            if (VentLightingHandler.Instance == null) return;
            VentLightingHandler.Instance.InitializeVentLights();
            VentLightingHandler.Instance.ApplyLighting();
        }
    }

    // this is kind of a hack but who cares
    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("BeginPlay")]
    [ConditionalPatchVent("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Lighting")]
    static class BeginPlayPatch
    {
        static void Postfix()
        {
            if (VentLightingHandler.Instance == null) return;
            VentLightingHandler.Instance.DoVentLighting();
        }
    }

    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("UpdateQueuedLightChanges")]
    [ConditionalPatchVent("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Lighting")]
    static class UpdateQueuedLightChangesPatch
    {
        static void Prefix(EnvironmentController __instance, Queue<Cell> ___lightSourcesToRegenerate, Queue<LightController> ___lightControllersToUpdate, out bool __state)
        {
            if (__instance.lightingOverride)
            {
                __state = false;
                return;
            }
            if (___lightSourcesToRegenerate.Count == 0)
            {
                __state = false;
                return;
            }
            if (___lightControllersToUpdate.Count == 0)
            {
                __state = false;
                return;
            }
            __state = true;
        }

        static void Postfix(bool __state)
        {
            if (__state)
            {
                if (VentLightingHandler.Instance == null) return;
                VentLightingHandler.Instance.DoVentLighting();
            }
        }
    }


    [HarmonyPatch(typeof(VentController))]
    [HarmonyPatch("Initialize")]
    [ConditionalPatchVent("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Lighting")]
    static class VentAddToHandlerPatch
    {
        static void Postfix(VentController __instance)
        {
            VentLightingHandler.Instance.vents.Add(__instance);
        }
    }

    class VentLightingHandler : MonoBehaviour
    {

        static FieldInfo _points = AccessTools.Field(typeof(VentController), "points");

        public List<VentController> vents = new List<VentController>();

        public static VentLightingHandler Instance;

        void Awake()
        {
            Instance = this;
        }

        public void InitializeVentLights()
        {
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = CrispyPlugin.Instance.ventColor;
            }
            CrispyPlugin.Instance.ventLightmap.SetPixels(0, 0, 256, 256, colors);
        }

        public void ApplyLighting()
        {
            CrispyPlugin.Instance.ventLightmap.Apply();
        }

        void DoIndividualVentLight(IntVector2[] points, Color lightColor, int startingNumber, int increment)
        {
            IntVector2 currentPosition = points[startingNumber];
            int currentPoint = startingNumber;
            float currentLerpValue = 1f;
            while (currentLerpValue >= 0.1f)
            {
                if (((currentPoint + increment) < 0) || ((currentPoint + increment) >= points.Length)) break; //get outta here
                CrispyPlugin.Instance.ventLightmap.SetPixel(currentPosition.x, currentPosition.z, Color.Lerp(CrispyPlugin.Instance.ventColor, lightColor, currentLerpValue));
                currentLerpValue /= 1.25f;
                //currentPoint += increment;
                Direction dir = Direction.Null;
                if (points[currentPoint].x > points[currentPoint + increment].x)
                {
                    dir = Direction.West;
                }
                else if (points[currentPoint].x < points[currentPoint + increment].x)
                {
                    dir = Direction.East;
                }
                else if (points[currentPoint].z > points[currentPoint + increment].z)
                {
                    dir = Direction.South;
                }
                else if (points[currentPoint].z < points[currentPoint + increment].z)
                {
                    dir = Direction.North;
                }
                currentPosition += dir.ToIntVector2();
                if (currentPosition == points[currentPoint + increment])
                {
                    currentPoint += increment;
                }
            }
        }

        public void DoVentLighting()
        {
            foreach (VentController vent in vents)
            {
                IntVector2[] points = (IntVector2[])_points.GetValue(vent);
                DoIndividualVentLight(points, Singleton<CoreGameManager>.Instance.lightMapTexture.GetPixel(points[0].x, points[0].z), 0, 1);
                DoIndividualVentLight(points, Singleton<CoreGameManager>.Instance.lightMapTexture.GetPixel(points[points.Length - 1].x, points[points.Length - 1].z), points.Length - 1, -1);
            }
            ApplyLighting();
        }
    }
}
