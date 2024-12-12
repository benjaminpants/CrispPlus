using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CrispPlus.Patches;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrispPlus
{
    public enum ItemSlotAnimationType
    {
        Vanilla,
        VanillaFixed,
        BottomToTop,
        TopToBottom,
        Instant
    }

    public enum ItemReticleType
    {
        Disabled,
        Enabled,
        EnabledAlways
    }


    [BepInPlugin("mtm101.rulerp.baldiplus.crispyplus", "Crispy+", "2.0.0.0")]
    public class CrispyPlugin : BaseUnityPlugin
    {
        internal static AssetManager assetMan = new AssetManager();
        internal static ManualLogSource Log;

        public static CrispyPlugin Instance;

        public static ItemSlotAnimationType slotAnimation => Instance.slotAnimConfig.Value;
        public ConfigEntry<ItemSlotAnimationType> slotAnimConfig;
        public ConfigEntry<bool> itemReticleAllowLeftClick;
        public ConfigEntry<ItemReticleType> itemReticleDisplay;

        public Texture2D ventLightmap;

        ConfigEntry<bool> greenLockerFixEnabled;
        ConfigEntry<bool> dietBSODAChangeEnabled;
        ConfigEntry<bool> mapTweaksEnabled;
        ConfigEntry<bool> enforceSpriteConsistency;
        ConfigEntry<bool> optionsMenuCheckmarkFix;
        public ConfigEntry<float> itemAnimationSpeed;

        ConfigEntry<bool> ventLightFixEnabled;

        ConfigEntry<float> ventBrightnessMultiplier;

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.crispyplus");
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEarlyEnumerator(), false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), true);
            Log = Logger;
            Instance = this;
            slotAnimConfig = Config.Bind("Hud",
                "Item Slot Animation Type",
                ItemSlotAnimationType.BottomToTop,
                @"The slot animation type.
Vanilla - The animation seen in base game, disables the mod's patches entirely.
VanillaFixed - Fixes the vanilla animation so the time the animation takes is consistent regardless of slot.
BottomToTop - Items appear from the bottom and leave from the top.
TopToBottom - Items appear from the top and leave from the bottom.
Instant - Items appear instantiously in the inventory like pre-0.6.");
            itemReticleDisplay = Config.Bind("Hud",
                "Item Use Indicator",
                ItemReticleType.Disabled,
                @"Whether or not a special indicator will be displayed when you are hovering over something that accepts an item in your inventory.
Disabled - The indicator is disabled entirely.
Enabled - The indicator is only shown when you are holding the item.
EnabledAlways - The indicator is shown regardless of which item you are holding.");
            itemReticleAllowLeftClick = Config.Bind("Hud",
                "Item Use Indicator Interact Bind",
                true,
                "Whether or not left click will use the item when the Item Use Indicator is visible. Does nothing if the indicator is disabled.");
            greenLockerFixEnabled = Config.Bind("World",
                "Green Locker Stretch Fix",
                true,
                "Makes the green locker use an alternate, wider texture so it's texture does not appear stretched.");
            dietBSODAChangeEnabled = Config.Bind("Items",
                "Diet BSODA Spray",
                true,
                "Makes the diet BSODA get its own spray texture.");
            mapTweaksEnabled = Config.Bind("Map",
                "Room Tweaks Enabled",
                true,
                "Determines if the room tweaks system is enabled. If you wish to disable a specific tweak, simply delete its associated json file in StreamingAssets/Modded/mtm101.rulerp.baldiplus.crispyplus/MapPack");
            enforceSpriteConsistency = Config.Bind("Items",
                "Enforce Sprite Consistency",
                true,
                "If enabled, all modded items will be forced to adhere to the base game sprite resolutions. (64x64 for big sprites and 32x32 for small sprites)");
            itemAnimationSpeed = Config.Bind("Hud",
                "Item Slot Animation Speed",
                5f,
                "The speed of the item slot animation, calculated as 1/x. Higher values make the animation faster.");
            optionsMenuCheckmarkFix = Config.Bind("Hud",
                "Options Menu Checkmark Fix",
                true,
                "Replaces the checkmark texture in the options menu to be a bigger, proper 32x32 texture, instead of a stretched 24x24 texture.");

            Config.Bind("World",
                "Pixel-Locked Item Bobbing",
                false,
                "If enabled, the item/pickup bobbing animation will be pixel locked, making it look choppier.");

            ventLightFixEnabled = Config.Bind("World",
                "Vent Light Fix",
                true,
                "Fixes character lighting in vents.");

            Config.Bind("World",
                "Vent Lighting",
                false,
                "Adds additional lighting to vents, copying the light at the vent entrances and exits. (If your game is lagging, turn this off!)\nRequires Vent Light Fix to work.");

            ventBrightnessMultiplier = Config.Bind("World",
                "Vent Brightness",
                1f,
                "The multiplier for the vent brightness.\nRequires Vent Light Fix to work.");
            harmony.PatchAllConditionals();
        }


        // vent fieldInfos
        static FieldInfo _ventPieceBendPrefab = AccessTools.Field(typeof(Structure_Vent), "ventPieceBendPrefab");
        static FieldInfo _ventPieceStraightPrefab = AccessTools.Field(typeof(Structure_Vent), "ventPieceStraightPrefab");
        static FieldInfo _ventPieceVerticalBendPrefab = AccessTools.Field(typeof(Structure_Vent), "ventPieceStraightPrefab");
        public Color ventColor;

        IEnumerator LoadEarlyEnumerator()
        {
            yield return 1 + (ventLightFixEnabled.Value ? 1 : 0);
            yield return "Loading misc changes...";
            if (dietBSODAChangeEnabled.Value)
            {
                ItemMetaStorage.Instance.FindByEnum(Items.DietBsoda).value.item.GetComponentInChildren<SpriteRenderer>().sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "DietBsodaSprite.png"), 12f);
            }
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            if (greenLockerFixEnabled.Value)
            {
                Texture2D greenLockerFixed = AssetLoader.TextureFromMod(this, "Locker_Green_Fixed.png");
                materials.First(x => x.name == "Locker_Green").SetMainTexture(greenLockerFixed);
            }
            if (ventLightFixEnabled.Value)
            {
                yield return "Loading vent fix...";
                // get and load the appropiate vent color incase mystman12 changes it (future proofing, everyone!)
                Structure_Vent[] foundVents = Resources.FindObjectsOfTypeAll<Structure_Vent>().Where(x => x.GetInstanceID() >= 0).ToArray();
                ventColor = ((Transform)_ventPieceStraightPrefab.GetValue(foundVents[0])).GetComponent<MeshRenderer>().material.GetColor("_TextureColor");//0.6887 0.6887 0.5425 0;
                ventColor.a = 1f;
                ventColor *= ventBrightnessMultiplier.Value;
                ventLightmap = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                ventLightmap.name = "VentLightMap";
                Color[] colors = new Color[256 * 256];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = ventColor;
                }
                ventLightmap.SetPixels(0, 0, 256, 256, colors);
                ventLightmap.Apply();
                for (int i = 0; i < foundVents.Length; i++)
                {
                    UpdateVentRenderer((Transform)_ventPieceStraightPrefab.GetValue(foundVents[i]));
                    UpdateVentRenderer((Transform)_ventPieceBendPrefab.GetValue(foundVents[i]));
                    UpdateVentRenderer((Transform)_ventPieceVerticalBendPrefab.GetValue(foundVents[i]));
                }
                Material ventMat = materials.First(x => x.GetInstanceID() >= 0 && x.name == "Vent_Inside");
                ventMat.SetColor("_TextureColor", new Color(1f, 1f, 1f, 0f));
                ventMat.SetTexture("_LightMap", ventLightmap);
            }
        }

        void UpdateVentRenderer(Transform transfm)
        {
            Material mat = transfm.GetComponent<MeshRenderer>().material;
            mat.SetColor("_TextureColor", new Color(1f,1f,1f,0f));
            mat.SetTexture("_LightMap", ventLightmap);
            transfm.MarkAsNeverUnload();
        }

        IEnumerator LoadEnumerator()
        {
            List<ItemObject> objectsWithInvalidSpriteSizes = new List<ItemObject>();
            Dictionary<Sprite, Sprite> spriteReplacements = new Dictionary<Sprite, Sprite>();
            Dictionary<Sprite, Sprite> spriteBigReplacements = new Dictionary<Sprite, Sprite>();
            List<Sprite> smallSpritesToCreate = new List<Sprite>();
            List<Sprite> bigSpritesToCreate = new List<Sprite>();
            ItemObject[] objects = ItemMetaStorage.Instance.All().SelectMany(x => x.itemObjects).ToArray();
            foreach (ItemObject obj in objects)
            {
                if (!obj.addToInventory) continue;
                if ((obj.itemSpriteSmall.texture.width > 32) || obj.itemSpriteSmall.texture.height > 32)
                {
                    objectsWithInvalidSpriteSizes.Add(obj);
                    smallSpritesToCreate.Add(obj.itemSpriteSmall);
                }
                if ((obj.itemSpriteLarge.texture.width > 64) || obj.itemSpriteLarge.texture.height > 64)
                {
                    objectsWithInvalidSpriteSizes.Add(obj);
                    bigSpritesToCreate.Add(obj.itemSpriteLarge);
                }
            }
            smallSpritesToCreate = smallSpritesToCreate.Distinct().ToList();
            bigSpritesToCreate = bigSpritesToCreate.Distinct().ToList();
            objectsWithInvalidSpriteSizes = objectsWithInvalidSpriteSizes.Distinct().ToList();

            if (!enforceSpriteConsistency.Value)
            {
                smallSpritesToCreate.Clear();
                bigSpritesToCreate.Clear();
                objectsWithInvalidSpriteSizes.Clear();
            }

            // figure out all the replacements we gotta do
            string mapPath = Path.Combine(AssetLoader.GetModPath(this), "MapPack");
            yield return objectsWithInvalidSpriteSizes.Count + bigSpritesToCreate.Count + smallSpritesToCreate.Count + 3;
            yield return "Loading...";
            for (int i = 0; i < smallSpritesToCreate.Count; i++)
            {
                yield return "Creating Small Sprite: " + smallSpritesToCreate[i];
                Texture2D scaled = new Texture2D(32, 32, smallSpritesToCreate[i].texture.format, false);
                scaled.name = smallSpritesToCreate[i].texture.name + "_CrispySScaled";
                scaled.filterMode = FilterMode.Point;
                Graphics.ConvertTexture(smallSpritesToCreate[i].texture, scaled);
                scaled.ReadPixels(new Rect(0f,0f,32f,32f), 0, 0, false); // unsure if necessary?
                spriteReplacements.Add(smallSpritesToCreate[i], AssetLoader.SpriteFromTexture2D(scaled, 25f));
            }
            for (int i = 0; i < bigSpritesToCreate.Count; i++)
            {
                yield return "Creating Big Sprite: " + bigSpritesToCreate[i];
                Texture2D scaled = new Texture2D(64, 64, bigSpritesToCreate[i].texture.format, false);
                scaled.name = bigSpritesToCreate[i].texture.name + "_CrispyBScaled";
                scaled.filterMode = FilterMode.Point;
                Graphics.ConvertTexture(bigSpritesToCreate[i].texture, scaled);
                scaled.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0, false); // unsure if necessary?
                spriteBigReplacements.Add(bigSpritesToCreate[i], AssetLoader.SpriteFromTexture2D(scaled, 50f));
            }
            // apply the corrected sprites
            for (int i = 0; i < objectsWithInvalidSpriteSizes.Count; i++)
            {
                yield return "Correcting Item Sprites: " + LocalizationManager.Instance.GetLocalizedText(objectsWithInvalidSpriteSizes[i].nameKey);
                if (spriteReplacements.ContainsKey(objectsWithInvalidSpriteSizes[i].itemSpriteSmall)) objectsWithInvalidSpriteSizes[i].itemSpriteSmall = spriteReplacements[objectsWithInvalidSpriteSizes[i].itemSpriteSmall];
                if (spriteBigReplacements.ContainsKey(objectsWithInvalidSpriteSizes[i].itemSpriteLarge)) objectsWithInvalidSpriteSizes[i].itemSpriteLarge = spriteBigReplacements[objectsWithInvalidSpriteSizes[i].itemSpriteLarge];
            }
            yield return "Loading and applying misc tweaks...";
            // do this here incase we can snag any prefabs that do the same thing
            if (optionsMenuCheckmarkFix.Value)
            {
                assetMan.Add<Sprite>("checkMark", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "32xCheckmark.png"));
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
            yield return "Loading Map Tweaks...";
            if (mapTweaksEnabled.Value)
            {
                MapTweaksHandler.LoadFolder(mapPath);
            }
            if (DateTime.Now.Day == 1 && DateTime.Now.Month == 4)
            {
                Resources.FindObjectsOfTypeAll<Texture2D>().Do(x => x.filterMode = FilterMode.Trilinear);
                TMP_FontAsset ass = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name.Contains("Lib"));
                Resources.FindObjectsOfTypeAll<TMP_Text>().Do(x => x.font = ass);
            }
            yield return "Modifying prefabs...";
            FieldInfo _reticle = AccessTools.Field(typeof(HudManager), "reticle");
            FieldInfo _retOn = AccessTools.Field(typeof(HudManager), "retOn");
            Resources.FindObjectsOfTypeAll<HudManager>().Do(x =>
            {
                HudManagerCrisp crispHud = x.gameObject.AddComponent<HudManagerCrisp>();
                crispHud.reticle = (Image)_reticle.GetValue(x);
                crispHud.spriteCursorItem = Instantiate(crispHud.reticle);
                crispHud.spriteCursor = Instantiate(crispHud.reticle);
                crispHud.spriteCursorItem.transform.SetParent(crispHud.reticle.transform.parent);
                crispHud.spriteCursor.transform.SetParent(crispHud.reticle.transform.parent);
                crispHud.spriteCursor.rectTransform.anchoredPosition += Vector2.down * 15f;
                crispHud.spriteCursor.rectTransform.localScale = Vector3.one;
                crispHud.spriteCursorItem.rectTransform.localScale = Vector3.one / 2f;
                crispHud.spriteCursor.enabled = false;
                crispHud.spriteCursor.name = "SpriteCursor";
                crispHud.spriteCursor.sprite = (Sprite)_retOn.GetValue(x);
                crispHud.spriteCursorItem.enabled = false;
                crispHud.spriteCursorItem.name = "SpriteCursorItem";
            });
            yield break;
        }
    }
}
