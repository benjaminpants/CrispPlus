using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CrispPlus.Patches
{
    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("UpdateHudColor")]
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "Hud", "Hud Darkening Fix")]
    class HudDarkenFix
    {
        // we use __state here to avoid either calling GetComponent to a variable holder class AND to avoid using static variables
        // https://harmony.pardeike.net/articles/patching-prefix.html
        static void Prefix(float ___colorValue, float ___colorTargetValue, out bool __state)
        {
            __state = (___colorValue != ___colorTargetValue);
        }

        static void Postfix(Image[] ___spritesToDarken, Color ___darkColor, float ___colorValue, float ___colorTargetValue, bool __state)
        {
            if ((___colorValue == ___colorTargetValue) && __state)
            {
                for (int i = 0; i < ___spritesToDarken.Length; i++)
                {
                    ___spritesToDarken[i].color = Color.Lerp(___darkColor, Color.white, ___colorTargetValue);
                }
            }
        }
    }
}
