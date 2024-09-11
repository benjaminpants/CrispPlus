using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CrispPlus.Patches
{

    class ItemSlotGlobalPatch
    {
        static FieldInfo _itemIcon = AccessTools.Field(typeof(ItemSlotsManager), "itemIcon");

        static MethodInfo _SlideItemCustom = AccessTools.Method(typeof(ItemSlotGlobalPatch), "SlideItemCustom");
        static IEnumerator SlideItemCustom(ItemSlotsManager instance, ItemSlider itemSlider, int start, int end)
        {
            if (CrispyPlugin.slotAnimation == ItemSlotAnimationType.Instant)
            {
                if (end >= 0)
                {
                    ((Image[])_itemIcon.GetValue(instance))[end].enabled = true;
                }
                yield break;
            }
            bool isEntering = (start == -1);
            itemSlider.rect.gameObject.SetActive(true);

            // teleport instantly to final x position
            if (isEntering)
            {
                itemSlider.position = (float)((end + 1) * 40);
            }
            else
            {
                itemSlider.position = (float)((start + 1) * 40);
            }
            Vector2 position = Vector2.zero;
            position.x = Mathf.Round(itemSlider.position);
            itemSlider.rect.anchoredPosition = position;
            Vector2 targetPos;
            Vector2 startingPos;
            if (CrispyPlugin.slotAnimation == ItemSlotAnimationType.VanillaFixed)
            {
                if (isEntering)
                {
                    startingPos = Vector2.zero;
                    targetPos = itemSlider.rect.anchoredPosition;
                }
                else
                {
                    targetPos = Vector2.zero;
                    startingPos = itemSlider.rect.anchoredPosition;
                }
            }
            else
            {
                if (CrispyPlugin.slotAnimation == ItemSlotAnimationType.TopToBottom)
                {
                    if (isEntering)
                    {
                        targetPos = itemSlider.rect.anchoredPosition;
                        startingPos = targetPos + Vector2.up * 40f;
                    }
                    else
                    {
                        startingPos = itemSlider.rect.anchoredPosition;
                        targetPos = startingPos + Vector2.down * 40f;
                    }
                }
                else
                {
                    if (isEntering)
                    {
                        targetPos = itemSlider.rect.anchoredPosition;
                        startingPos = targetPos + Vector2.down * 40f;
                    }
                    else
                    {
                        startingPos = itemSlider.rect.anchoredPosition;
                        targetPos = startingPos + Vector2.up * 40f;
                    }
                }
            }
            float time = 0f;
            float speed = CrispyPlugin.Instance.itemAnimationSpeed.Value;
            while (time < 1f)
            {
                time += Time.deltaTime * speed;
                itemSlider.rect.anchoredPosition = Vector2.Lerp(startingPos, targetPos, time);
                yield return null;
            }
            itemSlider.rect.anchoredPosition = targetPos;
            itemSlider.rect.gameObject.SetActive(false);
            if (end >= 0)
            {
                ((Image[])_itemIcon.GetValue(instance))[end].enabled = true;
            }
            yield break;
        }


        static MethodInfo _SlideItem = AccessTools.Method(typeof(ItemSlotsManager), "SlideItem");
        public static IEnumerable<CodeInstruction> GlobalTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool hasPatched = CrispyPlugin.slotAnimation == ItemSlotAnimationType.Vanilla;
            CodeInstruction[] codeInstructions = instructions.ToArray();
            for (int i = 0; i < codeInstructions.Length; i++)
            {
                CodeInstruction instruction = codeInstructions[i];
                if (hasPatched)
                {
                    yield return instruction;
                    continue;
                }
                if ((instruction.opcode == OpCodes.Call) && ((MethodInfo)instruction.operand == _SlideItem))
                {
                    yield return new CodeInstruction(OpCodes.Call, _SlideItemCustom);
                    hasPatched = true;
                    continue;
                }
                yield return instruction;
            }
            yield break;
        }
    }

    [HarmonyPatch(typeof(ItemSlotsManager))]
    [HarmonyPatch("LoseItem")]
    class LoseItemPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] codeInstructions = ItemSlotGlobalPatch.GlobalTranspiler(instructions).ToArray();
            for (int i = 0; i < codeInstructions.Length; i++)
            {
                yield return codeInstructions[i];
            }
            yield break;
        }
    }

    [HarmonyPatch(typeof(ItemSlotsManager))]
    [HarmonyPatch("CollectItem")]
    class CollectItemPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] codeInstructions = ItemSlotGlobalPatch.GlobalTranspiler(instructions).ToArray();
            for (int i = 0; i < codeInstructions.Length; i++)
            {
                yield return codeInstructions[i];
            }
            yield break;
        }
    }
}
