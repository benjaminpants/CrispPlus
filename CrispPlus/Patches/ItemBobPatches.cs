using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrispPlus.Patches
{

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.crispyplus", "World", "Pixel-Locked Item Bobbing")]
    [HarmonyPatch(typeof(PickupBobValue))]
    [HarmonyPatch("Update")]
    class ItemBobPatches
    {
        static float gridScale = 10f / 256f; //how do i keep misremembering this
        static void Postfix()
        {
            PickupBobValue.bobVal = Mathf.Round(PickupBobValue.bobVal / gridScale) * gridScale;
        }
    }
}
