using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CrispPlus.Patches
{

    public class HudManagerCrisp : MonoBehaviour
    {
        public Image spriteCursorItem;
        public Image spriteCursor;
        public Image reticle;
        public Sprite currentSprite = null;

        public void SetCursorSprite(Sprite spr)
        {
            if (spr == currentSprite) return;
            currentSprite = spr;
            if (spr == null)
            {
                spriteCursor.enabled = false;
                spriteCursorItem.enabled = false;
                reticle.enabled = true;
            }
            else
            {
                spriteCursor.enabled = true;
                spriteCursorItem.enabled = true;
                spriteCursorItem.sprite = spr;
                reticle.enabled = false;
            }
        }
    }

    public static class CoreGameExtensionsCrisp
    {
        public static HudManagerCrisp GetCrispHud(this CoreGameManager me, int player)
        {
            return me.GetHud(player).GetComponent<HudManagerCrisp>();
        }
    }

    public class ConditionalPatchReticleEnabled : ConditionalPatch
    {
        private string _mod;

        private string _category;

        private string _name;

        public ConditionalPatchReticleEnabled()
        {
            _mod = "mtm101.rulerp.baldiplus.crispyplus";
            _category = "Hud";
            _name = "Item Use Indicator";
        }

        public override bool ShouldPatch()
        {
            if (!Chainloader.PluginInfos.ContainsKey(_mod))
            {
                Debug.LogWarning("ConditionalPatchConfig can NOT find mod with name:" + _mod);
                return false;
            }

            BaseUnityPlugin baseUnityPlugin = Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().First((BaseUnityPlugin x) => x.Info == Chainloader.PluginInfos[_mod]);
            baseUnityPlugin.Config.TryGetEntry(new ConfigDefinition(_category, _name), out ConfigEntry<ItemReticleType> entry);
            if (entry == null)
            {
                Debug.LogWarning($"Cannot find config with: ({_mod}) {_category}, {_name}");
                return false;
            }

            return entry.Value != ItemReticleType.Disabled;
        }
    }

    [HarmonyPatch(typeof(PlayerClick))]
    [HarmonyPatch("Update")]
    [ConditionalPatchReticleEnabled]
    class PlayerClickPatch
    {

        static bool TryItem(IItemAcceptor acceptor, ItemManager itmMan, int slot)
        {
            if (acceptor.ItemFits(itmMan.items[slot].itemType))
            {
                Singleton<CoreGameManager>.Instance.GetCrispHud(itmMan.pm.playerNumber).SetCursorSprite(itmMan.items[slot].itemSpriteSmall);
                if (!CrispyPlugin.Instance.itemReticleAllowLeftClick.Value) return true;
                if (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true) & Time.timeScale != 0f)
                {
                    itmMan.selectedItem = slot;
                    itmMan.UpdateSelect();
                    itmMan.UseItem();
                }
                return true;
            }
            return false;
        }

        static void Postfix(PlayerClick __instance, RaycastHit ___hit)
        {
            if ((___hit.transform == null) || (__instance.pm.plm.Entity.InteractionDisabled))
            {
                Singleton<CoreGameManager>.Instance.GetCrispHud(__instance.pm.playerNumber).SetCursorSprite(null);
                return;
            }
            IItemAcceptor[] acceptors = ___hit.transform.GetComponents<IItemAcceptor>();
            if (acceptors.Length == 0)
            {
                Singleton<CoreGameManager>.Instance.GetCrispHud(__instance.pm.playerNumber).SetCursorSprite(null);
                return;
            }
            ItemManager itmMan = Singleton<CoreGameManager>.Instance.GetPlayer(__instance.pm.playerNumber).itm;
            for (int i = 0; i < acceptors.Length; i++)
            {
                IItemAcceptor acceptor = acceptors[i];
                if (CrispyPlugin.Instance.itemReticleDisplay.Value == ItemReticleType.Enabled)
                {
                    if (TryItem(acceptor, itmMan, itmMan.selectedItem)) return;
                    Singleton<CoreGameManager>.Instance.GetCrispHud(__instance.pm.playerNumber).SetCursorSprite(null);
                    return;
                }    
                // brute force it with every item in our inventory
                for (int j = 0; j < itmMan.items.Length; j++)
                {
                    if (TryItem(acceptor, itmMan, j)) return;
                }
            }
            Singleton<CoreGameManager>.Instance.GetCrispHud(__instance.pm.playerNumber).SetCursorSprite(null);
        }
    }
}
