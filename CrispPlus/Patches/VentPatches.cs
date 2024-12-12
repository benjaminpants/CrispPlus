using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrispPlus.Patches
{
    [HarmonyPatch(typeof(VentController))]
    [HarmonyPatch("Update")]
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "World", "Vent Light Fix")]
    static class VentControllerUpdatePatch
    {

        private class HasVentLighting : MonoBehaviour
        {
            public Dictionary<Renderer, Texture> oldLightMaps;
        }

        static void Postfix(List<VentTravelStatus> ___ventTravelers)
        {
            foreach (VentTravelStatus vt in ___ventTravelers)
            {
                if (vt.state == VentState.Traveling)
                {
                    if (!vt.overrider.entity.GetComponent<HasVentLighting>())
                    {
                        SwitchEntityRenderers(vt.overrider.entity, CrispyPlugin.Instance.ventLightmap, true);
                    }
                }
                else if (vt.state == VentState.Falling)
                {
                    if (vt.overrider.entity.TryGetComponent<HasVentLighting>(out HasVentLighting comp))
                    {
                        SwitchEntityRenderers(vt.overrider.entity, CrispyPlugin.Instance.ventLightmap, false);
                        foreach (KeyValuePair<Renderer, Texture> kvp in comp.oldLightMaps)
                        {
                            kvp.Key.material.SetTexture("_LightMap", kvp.Value);
                        }
                        GameObject.Destroy(comp);
                    }
                }
            }
        }

        static void SwitchEntityRenderers(Entity ent, Texture2D lightmap, bool addComp)
        {
            Dictionary<Renderer, Texture> oldMaps = new Dictionary<Renderer, Texture>();
            Renderer[] renderers = ent.GetComponentsInChildren<Renderer>();
            foreach (Renderer render in renderers)
            {
                if (render.materials.Length > 1)
                {
                    CrispyPlugin.Log.LogWarning("Renderer: " + render.name + " of Entity " + ent + " has more than one material in a single renderer!");
                    continue;
                }
                if (render.material.HasProperty("_LightMap"))
                {
                    oldMaps.Add(render, render.material.GetTexture("_LightMap"));
                    render.material.SetTexture("_LightMap", lightmap);
                }
            }
            if (addComp)
            {
                HasVentLighting comp = ent.gameObject.AddComponent<HasVentLighting>();
                comp.oldLightMaps = oldMaps;
            }
        }
    }
}
