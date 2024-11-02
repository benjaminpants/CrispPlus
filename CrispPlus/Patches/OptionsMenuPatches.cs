using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CrispPlus.Patches
{
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "Hud", "Options Menu Checkmark Fix")]
    [HarmonyPatch(typeof(OptionsMenu))]
    [HarmonyPatch("Awake")]
    class ReplaceAllInitialChecks
    {
        static void Prefix()
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();

            foreach (Image image in images)
            {
                if (image.sprite == null) continue;
                if (image.sprite.name == "YCTP_IndicatorsSheet_0" && image.rectTransform.sizeDelta == (Vector2.one * 32f))
                {
                    image.sprite = CrispyPlugin.assetMan.Get<Sprite>("checkMark");
                }
            }
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "Hud", "Options Menu Checkmark Fix")]
    [HarmonyPatch(typeof(CustomOptionsCategory))]
    [HarmonyPatch("checkMark")]
    [HarmonyPatch(MethodType.Getter)]
    class ReplaceGeneratedChecks
    {
        static bool Prefix(ref Sprite __result)
        {
            __result = CrispyPlugin.assetMan.Get<Sprite>("checkMark");
            return false;
        }
    }
}
