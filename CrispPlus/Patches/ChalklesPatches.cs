using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CrispPlus.Patches
{
    [HarmonyPatch(typeof(ChalkFace))]
    [HarmonyPatch("UpdateSprite")]
    class ChalklesPatches
    {
        static bool Prefix(ChalkFace __instance, float ___charge, float ___setTime, Color ___spriteColor, SpriteRenderer ___chalkRenderer)
        {
            if (CrispyPlugin.Instance.chalklesType.Value == ChalklesAnimationType.Vanilla) return true;
            if (CrispyPlugin.Instance.chalklesType.Value.HasFlag(ChalklesAnimationType.Choppy))
            {
                ___spriteColor.a = Mathf.Round(((___charge / ___setTime) / 0.1f)) * 0.1f;
            }
            else
            {
                ___spriteColor.a = 1f;
            }
            ___chalkRenderer.color = ___spriteColor;
            if (!CrispyPlugin.Instance.chalklesType.Value.HasFlag(ChalklesAnimationType.Dither)) return false;
            int nearestFrame = Mathf.RoundToInt((___charge / ___setTime) * (CrispyPlugin.Instance.chalklesSprites.Length - 1));
            ___chalkRenderer.sprite = CrispyPlugin.Instance.chalklesSprites[nearestFrame];
            return false;
        }
    }
}
