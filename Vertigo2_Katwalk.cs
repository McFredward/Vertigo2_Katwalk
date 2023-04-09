using System;
using System.IO;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using MelonLoader;
using static KATNativeSDK;
using Vertigo2.Player;
using Vertigo2;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using Vertigo2.Localization;
using System.Timers;

[assembly: MelonInfo(typeof(Vertigo2_Katwalk.Main), "Vertigo2_Katwalk", "1.0.0", "McFredward")]
[assembly: MelonGame("Zulubo Productions", "vertigo2")]
namespace Vertigo2_Katwalk
{
    public class Main : MelonMod
    {

        #region Private Parameters

        private static bool initilized = false;
        private static bool in_main_menu = true;
        private static bool custom_options_pending_changes = false;
        private static bool reveresed_options_pressed = false;
        private static bool invoke_auto_calibrate = false;
        private static System.Timers.Timer invoke_auto_calibrate_time = new System.Timers.Timer();
        private static GameObject OptionsTab_KatWalk;
        private static GameObject Tab_KatWalk;
        private static GameObject Toggle_Disable_Joystick;
        private static GameObject Slider_SpeedMul;
        private static GameObject Slider_SpeedMaxRange;
        private static GameObject Slider_SpeedExponent;
        private static VertigoSettingsBoolAdapted VertigoSettingsBool_Disable_Joystick;
        private static VertigoSettingsSliderAdapted VertigoSettingsSlider_SpeedMul;
        private static VertigoSettingsSliderAdapted VertigoSettingsSlider_SpeedMaxRange;
        private static VertigoSettingsSliderAdapted VertigoSettingsSlider_SpeedExponent;
        private static TextMeshProUGUI label_text_Toggle_Disable_Joystick;
        private static TextMeshProUGUI label_text_Slider_SpeedMul;
        private static TextMeshProUGUI label_text_Slider_SpeedMaxRange;
        private static TextMeshProUGUI label_text_Slider_SpeedExponent;
        private static TeleportSurface last_surface = null;

        private static float yawCorrection = 0.0f;
        private static float old_MovingPlatform_angle = float.NaN;

        private static float speedMul;
        private static float speedMaxRange;
        private static float speedExponent;
        private static bool joystick_disabled;

        private void AutoCalibrateTimeElapsed(object sender, ElapsedEventArgs e)
        {
            invoke_auto_calibrate = true;
        }

        #endregion

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod started");
            invoke_auto_calibrate_time.Interval = 2000; //2 seconds
            invoke_auto_calibrate_time.Elapsed += AutoCalibrateTimeElapsed;
            LoadOptions();
        }



        // --- Add Option for the mod to the Locomotion menu ---
        public static void setUIValues()
        {
            if (VertigoSettingsBool_Disable_Joystick.toggle != null)
                VertigoSettingsBool_Disable_Joystick.toggle.isOn = joystick_disabled;
            if(VertigoSettingsSlider_SpeedMul.slider != null)
                VertigoSettingsSlider_SpeedMul.slider.value = (float)Math.Ceiling(((speedMul - 0.1f) * 5.3f) / 0.05f);
                //VertigoSettingsSlider_SpeedMul.slider.value = speedMul != 1 ? speedMul * 100 : 99;
            if (VertigoSettingsSlider_SpeedMaxRange.slider != null)
                VertigoSettingsSlider_SpeedMaxRange.slider.value = (float)Math.Ceiling(((speedMaxRange - 0.5f) * 5.5f) / 0.25f);
            if (VertigoSettingsSlider_SpeedExponent.slider != null)
                VertigoSettingsSlider_SpeedExponent.slider.value = (float)Math.Ceiling(((speedExponent - 0.5f) * 3.3f) / 0.05f);
        }

        public static void setUIText()
        {
            //Change text
            //float value_SpeedMul = VertigoSettingsSlider_SpeedMul.slider.value != 0 ? (float)(Math.Ceiling(VertigoSettingsSlider_SpeedMul.slider.value / 10) / 10) : 0.1f;
            if(VertigoSettingsSlider_SpeedMul.slider != null)
            {
                float value_SpeedMul = 0.1f + (float)(Math.Floor(VertigoSettingsSlider_SpeedMul.slider.value / 5.3f) * 0.05);
                label_text_Slider_SpeedMul.text = "Speed Multiplicator: " + value_SpeedMul.ToString();
            }
            if(VertigoSettingsSlider_SpeedMaxRange.slider != null)
            {
                float value_SpeedMaxRange = 0.5f + (float)Math.Floor(VertigoSettingsSlider_SpeedMaxRange.slider.value / 5.5f) * 0.25f;
                label_text_Slider_SpeedMaxRange.text = "Speed max. Range: " + value_SpeedMaxRange.ToString();
            }
            if(VertigoSettingsSlider_SpeedExponent.slider != null)
            {
                float value_SpeedExponent = 0.5f + (float)Math.Floor(VertigoSettingsSlider_SpeedExponent.slider.value / 3.3f) * 0.05f;
                label_text_Slider_SpeedExponent.text = "Speed curve exponent: " + value_SpeedExponent.ToString();
            }
        }

        public static void updateValues()
        {
            if(VertigoSettingsBool_Disable_Joystick.toggle != null)
                joystick_disabled = VertigoSettingsBool_Disable_Joystick.toggle.isOn;
            if (VertigoSettingsSlider_SpeedMul.slider != null)
                speedMul = 0.1f + (float)(Math.Floor(VertigoSettingsSlider_SpeedMul.slider.value / 5.3f) * 0.05);
            if (VertigoSettingsSlider_SpeedMaxRange.slider != null)
                speedMaxRange = 0.5f + (float)Math.Floor(VertigoSettingsSlider_SpeedMaxRange.slider.value / 5.5f) * 0.25f;
            if (VertigoSettingsSlider_SpeedExponent.slider != null)
                speedExponent = 0.5f + (float)Math.Floor(VertigoSettingsSlider_SpeedExponent.slider.value / 3.3f) * 0.05f;
        }

        static void SaveOptions()
        {
            using (StreamWriter writer = new StreamWriter("Mods\\Vertigo2_Katwalk_options.txt", false))
            {
                MelonLogger.Msg("Saving values: " + speedMul.ToString() + " " + speedMaxRange.ToString() + " " + speedExponent.ToString() + " " + joystick_disabled.ToString());
                writer.WriteLine($"speedMul: {speedMul}");
                writer.WriteLine($"speedMaxRange: {speedMaxRange}");
                writer.WriteLine($"speedExponent: {speedExponent}");
                writer.WriteLine($"joystick_disabled: {joystick_disabled}");
            }
        }

        static void LoadOptions()
        {
            if (File.Exists("Mods\\Vertigo2_Katwalk_options.txt"))
            {
                using (StreamReader reader = new StreamReader("Mods\\Vertigo2_Katwalk_options.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            string varName = parts[0].Trim();
                            string varValue = parts[1].Trim();
                            switch (varName)
                            {
                                case "speedMul":
                                    float.TryParse(varValue, out speedMul);
                                    break;
                                case "speedMaxRange":
                                    float.TryParse(varValue, out speedMaxRange);
                                    break;
                                case "speedExponent":
                                    float.TryParse(varValue, out speedExponent);
                                    break;
                                case "joystick_disabled":
                                    bool.TryParse(varValue, out joystick_disabled);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            else //Default values
            {
                speedMul = 0.35f;
                speedMaxRange = 2.5f;
                speedExponent = 1f;
                joystick_disabled = false;
            }
        }

        public class VertigoSettingsBoolAdapted : VertigoSettingsOption
        {
            [HideInInspector]
            public Toggle toggle;

            public override void Init()
            {
                this.toggle = this.GetComponent<Toggle>();
                this.toggle.onValueChanged.AddListener((UnityAction<bool>)(_param1 => this.OnValueChange()));
                this.menu = GameObject.Find("Options").gameObject.GetComponent<VertigoSettingsMenu>();
            }

            public override void OnValueChange()
            {
                if (!reveresed_options_pressed)
                    custom_options_pending_changes = true;
                this.menu.pendingChanges = false; //Workaround for pending changes get true, idk why..

            }

            public override void UpdateDisplay()
            {
            }
        }


        public class VertigoSettingsSliderAdapted : VertigoSettingsOption
        {
            [HideInInspector]
            public Slider slider;

            public override void Init()
            {
                this.slider = this.GetComponent<Slider>();
                this.slider.onValueChanged.AddListener((UnityAction<float>)(_param1 => this.OnValueChange()));
                this.menu = GameObject.Find("Options").gameObject.GetComponent<VertigoSettingsMenu>();
            }

            public override void OnValueChange()
            {
                setUIText();
                if(!reveresed_options_pressed)
                    custom_options_pending_changes = true;
                this.menu.pendingChanges = false; //Workaround for pending changes get true, idk why..
            }

            public override void UpdateDisplay()
            {
            }
        }

        [HarmonyPatch(typeof(VertigoSettingsMenu), "ApplyChanges", new Type[] { })]
        public static class Settings_Menu_Apply_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoSettingsMenu __instance)
            {
                MelonLogger.Msg("Apply pressed");
                custom_options_pending_changes = false;
                __instance.pendingChanges = false;
                updateValues();
                SaveOptions();
            }
        }

        [HarmonyPatch(typeof(VertigoSettingsMenu), "RevertChanges", new Type[] { })]
        public static class Settings_Menu_Revert_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoSettingsMenu __instance)
            {
                MelonLogger.Msg("Revert pressed");
                custom_options_pending_changes = false;
                __instance.pendingChanges = false;
                reveresed_options_pressed = true;
                setUIValues();
            }
        }

        [HarmonyPatch(typeof(LoadingScreen), "LoadLevel", new Type[] { })]
        public static class LoadingScreen_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(LoadingScreen __instance)
            {
                initilized = false;
                if(__instance.levelName == "MainMenu")
                {
                    in_main_menu = true;
                }
                else
                {
                    in_main_menu = false;
                }
                invoke_auto_calibrate_time.Start();
            }
        }

        /*
        [HarmonyPatch(typeof(VertigoSettingsTabs), "Update", new Type[] { })]
        public static class Settings_Tab_Update_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(VertigoSettingsTabs __instance, ref float ___scrollPos)
            {
                if (__instance.tabs != null && __instance.tabs.Length != 0)
                {
                    __instance.selectedTab = Mathf.Clamp(__instance.selectedTab, 0, __instance.tabs.Length);
                    if ((UnityEngine.Object)__instance.tabs[__instance.selectedTab].toggle != (UnityEngine.Object)null && !__instance.tabs[__instance.selectedTab].toggle.isOn)
                    {
                        for (int index = 0; index < __instance.tabs.Length; ++index)
                        {
                            if ((UnityEngine.Object)__instance.tabs[index].toggle != (UnityEngine.Object)null && __instance.tabs[index].toggle.isOn)
                                __instance.selectedTab = index;
                        }
                    }
                    if (__instance.transitionType == VertigoSettingsTabs.TransitionTypes.Scroll)
                    {
                        MelonLogger.Msg("t1: " + __instance.selectedTab.ToString() + " " + __instance.tabs.Length.ToString());
                        ___scrollPos = Mathf.Lerp(___scrollPos, (float)__instance.selectedTab / ((float)__instance.tabs.Length - 1.23f), Time.unscaledDeltaTime * __instance.scrollSpeed);
                        //MelonLogger.Msg("scroll pos: " + ___scrollPos.ToString());
                    }
                }
                if (__instance.transitionType == VertigoSettingsTabs.TransitionTypes.Scroll && (UnityEngine.Object)__instance.content != (UnityEngine.Object)null)
                    __instance.content.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(-__instance.content.rect.width + __instance.windowWidth, 0.0f), ___scrollPos);
                MelonLogger.Msg("t2: " + __instance.content.rect.width.ToString() + " " + __instance.windowWidth.ToString() + __instance.content.anchoredPosition.ToString());
                if (__instance.transitionType != VertigoSettingsTabs.TransitionTypes.None)
                    return false;
                for (int index = 0; index < __instance.tabs.Length; ++index)
                    __instance.tabs[index].gameObject.SetActive(__instance.selectedTab == index);
                return false;
            }
        }
        */


        // --- Create a new Tab in the Option menu ---
        [HarmonyPatch(typeof(VertigoSettingsTabs), "OnEnable", new Type[] { })]
        public static class Settings_Tab_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(VertigoSettingsTabs __instance)
            {
                if (!initilized)
                {
                    // OptionsTab_KatWalk - for creating a new Tab within the Options Menu
                    GameObject TabGroup = GameObject.Find("TabGroup");
                    GameObject OptionsTab_Comfort = GameObject.Find("OptionsTab_Comfort");
                    OptionsTab_KatWalk = GameObject.Instantiate(OptionsTab_Comfort); //Make copy
                    OptionsTab_KatWalk.name = "OptionsTab_KatWalk";
                    TextMeshProUGUI tab_text = OptionsTab_KatWalk.GetComponentInChildren<TextMeshProUGUI>();
                    LocalizeTextAuto tab_localize = OptionsTab_KatWalk.GetComponentInChildren<LocalizeTextAuto>();
                    tab_text.SetText("KatWalk");
                    tab_localize.enabled = false;

                    OptionsTab_KatWalk.transform.SetParent(TabGroup.transform, false);

                    // Tab_KatWalk - contains all GUI elements
                    GameObject content = GameObject.Find("content"); //Luckely the first appearance of "content" is the searched Object
                    GameObject Tab_Locomotion = GameObject.Find("Tab_Locomotion");
                    Tab_KatWalk = GameObject.Instantiate(Tab_Locomotion);
                    Tab_KatWalk.name = "Tab_KatWalk";

                    
                    foreach (Transform child in Tab_KatWalk.transform)
                    {
                        GameObject.DestroyImmediate(child.gameObject,true);
                    }
                    //Somehow some objects are not dounf in the loop: Delete by hand
                    GameObject.DestroyImmediate(Tab_KatWalk.transform.Find("Orientation").gameObject, true);
                    GameObject.DestroyImmediate(Tab_KatWalk.transform.Find("Turning Mode").gameObject, true);
                    GameObject.DestroyImmediate(Tab_KatWalk.transform.Find("Turning Increment").gameObject, true);
                    GameObject.DestroyImmediate(Tab_KatWalk.transform.Find("Toggle Seated Mode").gameObject, true);

                    PrintComponentsAndChildren(Tab_KatWalk);

                    Tab_KatWalk.transform.SetParent(content.transform, false);


                    VertigoSettingsTabs.SettingsTab katWalkSettingsTabObj = new VertigoSettingsTabs.SettingsTab();
                    katWalkSettingsTabObj.name = "KatWalk";
                    katWalkSettingsTabObj.gameObject = Tab_KatWalk;
                    katWalkSettingsTabObj.toggle = OptionsTab_KatWalk.GetComponent<UnityEngine.UI.Toggle>();
                    katWalkSettingsTabObj.toggle.isOn = false;

                    VertigoSettingsTabs.SettingsTab[] tabs_new = new VertigoSettingsTabs.SettingsTab[7];
                    // __instance.content.rect.width = 1820
                    Array.Copy(__instance.tabs, tabs_new, __instance.tabs.Length);
                    tabs_new[6] = katWalkSettingsTabObj;
                    __instance.tabs = tabs_new;
                    //__instance.content = content.GetComponent<RectTransform>();
                    __instance.content.sizeDelta = new Vector2(__instance.content.sizeDelta.x + 500f, __instance.content.sizeDelta.y);
                    if(in_main_menu)
                        __instance.windowWidth = -1790f;
                    else
                        __instance.windowWidth = 1024f;
                    //PrintComponentsAndChildren(content);

                    //-------------------Add components to page--------------------

                    //GameObject Tab_Locomotion = GameObject.Find("Tab_Locomotion");
                    GameObject Toggle_Smooth_Locomotion = Tab_Locomotion.transform.Find("Toggle Smooth Locomotion").gameObject;
                    Toggle_Disable_Joystick = GameObject.Instantiate(Toggle_Smooth_Locomotion); //Make copy
                    Toggle_Disable_Joystick.name = "Toggle Disable Joystick";

                    GameObject Tab_Comfort = GameObject.Find("Tab_Comfort");
                    GameObject Slider_Screen_Shake = Tab_Comfort.transform.Find("Slider Screen Shake").gameObject;
                    Slider_SpeedMul = GameObject.Instantiate(Slider_Screen_Shake);
                    Slider_SpeedMul.name = "Slider SpeedMul";

                    Slider_SpeedMaxRange = GameObject.Instantiate(Slider_Screen_Shake);
                    Slider_SpeedMaxRange.name = "Slider SpeedMaxRange";

                    Slider_SpeedExponent = GameObject.Instantiate(Slider_Screen_Shake);
                    Slider_SpeedMaxRange.name = "Slider SpeedExponent";

                    // --- Replace old VertigoSettings components with the adapted one ---
                    VertigoSettingsBool Old_VertigoSettingsBool_Disable_Joystick = Toggle_Disable_Joystick.GetComponent<VertigoSettingsBool>();
                    int existingIndex = Toggle_Disable_Joystick.GetComponents<Component>().ToList().IndexOf(Old_VertigoSettingsBool_Disable_Joystick);
                    GameObject.DestroyImmediate(Old_VertigoSettingsBool_Disable_Joystick, true);
                    VertigoSettingsBool_Disable_Joystick = Toggle_Disable_Joystick.AddComponent<VertigoSettingsBoolAdapted>();
                    Toggle_Disable_Joystick.GetComponents<Component>()[existingIndex] = VertigoSettingsBool_Disable_Joystick;
                    VertigoSettingsBool_Disable_Joystick.optionName = "katwalk_disableJoystick";

                    VertigoSettingsSlider Old_VertigoSettingsSlider_SpeedMul = Slider_SpeedMul.GetComponent<VertigoSettingsSlider>();
                    existingIndex = Slider_SpeedMul.GetComponents<Component>().ToList().IndexOf(Old_VertigoSettingsSlider_SpeedMul);
                    GameObject.DestroyImmediate(Old_VertigoSettingsBool_Disable_Joystick, true);
                    VertigoSettingsSlider_SpeedMul = Slider_SpeedMul.AddComponent<VertigoSettingsSliderAdapted>();
                    Slider_SpeedMul.GetComponents<Component>()[existingIndex] = VertigoSettingsSlider_SpeedMul;
                    VertigoSettingsSlider_SpeedMul.optionName = "katwalk_speedMul";

                    VertigoSettingsSlider Old_VertigoSettingsSlider_SpeedMaxRange = Slider_SpeedMaxRange.GetComponent<VertigoSettingsSlider>();
                    existingIndex = Slider_SpeedMaxRange.GetComponents<Component>().ToList().IndexOf(Old_VertigoSettingsSlider_SpeedMaxRange);
                    GameObject.DestroyImmediate(Old_VertigoSettingsSlider_SpeedMaxRange, true);
                    VertigoSettingsSlider_SpeedMaxRange = Slider_SpeedMaxRange.AddComponent<VertigoSettingsSliderAdapted>();
                    Slider_SpeedMaxRange.GetComponents<Component>()[existingIndex] = VertigoSettingsSlider_SpeedMaxRange;
                    VertigoSettingsSlider_SpeedMaxRange.optionName = "katwalk_speedMaxRange";

                    VertigoSettingsSlider Old_VertigoSettingsSlider_SpeedExponent = Slider_SpeedExponent.GetComponent<VertigoSettingsSlider>();
                    existingIndex = Slider_SpeedExponent.GetComponents<Component>().ToList().IndexOf(Old_VertigoSettingsSlider_SpeedExponent);
                    GameObject.DestroyImmediate(Old_VertigoSettingsSlider_SpeedExponent, true);
                    VertigoSettingsSlider_SpeedExponent = Slider_SpeedExponent.AddComponent<VertigoSettingsSliderAdapted>();
                    Slider_SpeedExponent.GetComponents<Component>()[existingIndex] = VertigoSettingsSlider_SpeedExponent;
                    VertigoSettingsSlider_SpeedExponent.optionName = "katwalk_speedExponent";

                    // --- Change text of the option ---
                    GameObject label_Toggle_Disable_Joystick = Toggle_Disable_Joystick.transform.Find("label").gameObject;
                    label_text_Toggle_Disable_Joystick = label_Toggle_Disable_Joystick.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto label_localize_Toggle_Disable_Joystick = label_Toggle_Disable_Joystick.GetComponent<LocalizeTextAuto>();
                    label_text_Toggle_Disable_Joystick.text = "Disable Joystick for Movement";
                    label_localize_Toggle_Disable_Joystick.enabled = false;
                    //GameObject.Destroy(Toggle_Disable_Joystick.GetComponent<VRTooltip>());
                    //GameObject tooltip_Toggle_Disable_Joystick = Toggle_Disable_Joystick.transform.Find("tooltip").gameObject;
                    /* //Tooltip of first option is hidden by the border - Simple solution is to change order and remove the redundant tooltip on the toggle
                    GameObject tooltip_textobject_Toggle_Disable_Joystick = tooltip_Toggle_Disable_Joystick.transform.Find("Text (TMP)").gameObject;
                    TextMeshProUGUI tooltip_text_Toggle_Disable_Joystick = tooltip_textobject_Toggle_Disable_Joystick.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto tooltip_localize_Toggle_Disable_Joystick = tooltip_textobject_Toggle_Disable_Joystick.GetComponent <LocalizeTextAuto>();
                    tooltip_text_Toggle_Disable_Joystick.text = "Disabling the Movement with the left Joystick. KatWalk only.";
                    tooltip_localize_Toggle_Disable_Joystick.enabled = false;
                    */

                    GameObject Toggle_Seated_Mode = Tab_Locomotion.transform.Find("Toggle Seated Mode").gameObject;
                    GameObject tooltip_Toggle_Seated_Mode = Toggle_Seated_Mode.transform.Find("tooltip").gameObject;

                    GameObject label_Slider_SpeedMul = Slider_SpeedMul.transform.Find("label").gameObject;
                    label_text_Slider_SpeedMul = label_Slider_SpeedMul.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto label_localize_Slider_SpeedMul = label_Slider_SpeedMul.GetComponent<LocalizeTextAuto>();
                    //label_text_Slider_SpeedMul.text = "Speed Multiplicator: " + speedMul.ToString();
                    label_localize_Slider_SpeedMul.enabled = false;
                    //There is no Slider with a tooltip to copy and modifiy. Add the components by hand
                    VRTooltip tooltip_component_Slider_SpeedMul = Slider_SpeedMul.AddComponent<VRTooltip>();
                    GameObject tooltip_Slider_SpeedMul = GameObject.Instantiate(tooltip_Toggle_Seated_Mode);
                    GameObject tooltip_textobject_Slider_SpeedMul = tooltip_Slider_SpeedMul.transform.Find("Text (TMP)").gameObject;
                    TextMeshProUGUI tooltip_text_Slider_SpeedMul = tooltip_textobject_Slider_SpeedMul.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto tooltip_localize_Slider_SpeedMul = tooltip_textobject_Slider_SpeedMul.GetComponent<LocalizeTextAuto>();
                    tooltip_text_Slider_SpeedMul.text = "Modifies how the speed of the KatWalk is translated to the in-game speed.";
                    tooltip_localize_Slider_SpeedMul.enabled = false;
                    tooltip_Slider_SpeedMul.transform.SetParent(Slider_SpeedMul.transform, false);
                    tooltip_component_Slider_SpeedMul.tooltip = tooltip_Slider_SpeedMul;

                    GameObject label_Slider_SpeedMaxRange = Slider_SpeedMaxRange.transform.Find("label").gameObject;
                    label_text_Slider_SpeedMaxRange = label_Slider_SpeedMaxRange.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto label_localize_Slider_SpeedMaxRange = label_Slider_SpeedMaxRange.GetComponent<LocalizeTextAuto>();
                    //label_text_Slider_SpeedMaxRange.text = "Speed max. Range: " + speedMaxRange.ToString();
                    label_localize_Slider_SpeedMaxRange.enabled = false;
                    VRTooltip tooltip_component_Slider_SpeedMaxRange = Slider_SpeedMaxRange.AddComponent<VRTooltip>();
                    GameObject tooltip_Slider_SpeedMaxRange = GameObject.Instantiate(tooltip_Toggle_Seated_Mode);
                    GameObject tooltip_textobject_Slider_SpeedMaxRange = tooltip_Slider_SpeedMaxRange.transform.Find("Text (TMP)").gameObject;
                    TextMeshProUGUI tooltip_text_Slider_SpeedMaxRange = tooltip_textobject_Slider_SpeedMaxRange.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto tooltip_localize_Slider_SpeedMaxRange = tooltip_textobject_Slider_SpeedMaxRange.GetComponent<LocalizeTextAuto>();
                    tooltip_text_Slider_SpeedMaxRange.text = "Modifies what maximal ingame speed value corresponds to the maximal KatWalk speed.";
                    tooltip_localize_Slider_SpeedMaxRange.enabled = false;
                    tooltip_Slider_SpeedMaxRange.transform.SetParent(Slider_SpeedMaxRange.transform, false);
                    tooltip_component_Slider_SpeedMaxRange.tooltip = tooltip_Slider_SpeedMaxRange;

                    GameObject label_Slider_SpeedExponent = Slider_SpeedExponent.transform.Find("label").gameObject;
                    label_text_Slider_SpeedExponent = label_Slider_SpeedExponent.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto label_localize_Slider_SpeedExponent = label_Slider_SpeedExponent.GetComponent<LocalizeTextAuto>();
                    //label_text_Slider_SpeedExponent.text = "Speed curve exponent: " + speedExponent.ToString();
                    label_localize_Slider_SpeedExponent.enabled = false;
                    VRTooltip tooltip_component_Slider_SpeedExponent = Slider_SpeedExponent.AddComponent<VRTooltip>();
                    GameObject tooltip_Slider_SpeedExponent = GameObject.Instantiate(tooltip_Toggle_Seated_Mode);
                    GameObject tooltip_textobject_Slider_SpeedExponent = tooltip_Slider_SpeedExponent.transform.Find("Text (TMP)").gameObject;
                    TextMeshProUGUI tooltip_text_Slider_SpeedExponent = tooltip_textobject_Slider_SpeedExponent.GetComponent<TextMeshProUGUI>();
                    LocalizeTextAuto tooltip_localize_Slider_SpeedExponent = tooltip_textobject_Slider_SpeedExponent.GetComponent<LocalizeTextAuto>();
                    tooltip_text_Slider_SpeedExponent.text = "Modifies the speed curve which determines how fast the maximal speed is reached.";
                    tooltip_localize_Slider_SpeedExponent.enabled = false;
                    tooltip_Slider_SpeedExponent.transform.SetParent(Slider_SpeedExponent.transform, false);
                    tooltip_component_Slider_SpeedExponent.tooltip = tooltip_Slider_SpeedExponent;

                    //Fix layout
                    //FlowLayoutGroup group = Tab_Locomotion.GetComponent<FlowLayoutGroup>();
                    //group.SpacingX = 100;
                    //group.preferredHeight = 100;
                    Transform test = Tab_KatWalk.transform; //<<<<< ES LIEGT HIERAN AUssERHALB DES MAIN MENUS, WARUM?!

                    // Insert the Objects in the hierachy
                    Toggle_Disable_Joystick.transform.SetParent(Tab_KatWalk.transform, false);
                    Slider_SpeedMul.transform.SetParent(Tab_KatWalk.transform, false);
                    Slider_SpeedMaxRange.transform.SetParent(Tab_KatWalk.transform, false);
                    Slider_SpeedExponent.transform.SetParent(Tab_KatWalk.transform, false);

                    setUIValues();
                    initilized = true;
                }
            }
        }
        
        // Somehow changing pending_changes for the custom options did not work -> Just implement it by myself
        [HarmonyPatch(typeof(VertigoSettingsMenu), "Update", new Type[] { })]
        public static class Settings_Menu_Patch_test
        {
            [HarmonyPrefix]
            public static bool Prefix(VertigoSettingsMenu __instance)
            {
                __instance.applyButtonsRoot.SetActive(__instance.pendingChanges || custom_options_pending_changes);
                if (reveresed_options_pressed)
                    reveresed_options_pressed = false;
                return false;
            }
        }
       

        public static void PrintComponentsAndChildren(GameObject gameObject, int depth = 0)
        {
            string prefix = new string('-', depth * 2);
            MelonLogger.Msg(prefix + "GameObject: " + gameObject.name);

            // Print all Components of the GameObject
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                MelonLogger.Msg(prefix + "  Component: " + components[i].GetType().Name);
            }

            // Print all children GameObjects and their Components
            foreach (Transform child in gameObject.transform)
            {
                PrintComponentsAndChildren(child.gameObject, depth + 1);
            }
        }

        public static void PrintComponents(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                MelonLogger.Msg(component.GetType().ToString());
            }
        }

        private static void PrintHierarchy()
        {
            var rootObjects = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);

            foreach (var rootObject in rootObjects)
            {
                PrintGameObject(rootObject, 0);
            }
        }

        private static void PrintGameObject(GameObject gameObject, int depth)
        {
            var indent = new string('-', depth);
            MelonLogger.Msg(indent + gameObject.name);

            foreach (Transform child in gameObject.transform)
            {
                PrintGameObject(child.gameObject, depth + 1);
            }
        }

        [HarmonyPatch(typeof(VertigoCharacterController), "FixedUpdate", new Type[] { })]
        public static class VertigoCharacterController_FixedUpdate_Patch
        {

            [HarmonyPrefix]
            public static void Prefix(VertigoCharacterController __instance)
            {
                var ws = KATNativeSDK.GetWalkStatus();
                var lastCalibrationTime = KATNativeSDK.GetLastCalibratedTimeEscaped();

                if (ws.deviceDatas[0].btnPressed || lastCalibrationTime < 0.08 || invoke_auto_calibrate)
                {
                    invoke_auto_calibrate_time.Stop();
                    MelonLogger.Msg("Recalibrate");
                    //Do recalibration
                    var hmdYaw = __instance.head.transform.rotation.eulerAngles.y;
                    var bodyYaw = ws.bodyRotationRaw.eulerAngles.y;
                    yawCorrection = bodyYaw - hmdYaw;

                    var pos = __instance.transform.position;
                    var eyePos = __instance.head.transform.position;
                    eyePos.x = pos.x;
                    eyePos.z = pos.z;
                    __instance.head.transform.position = eyePos;

                    invoke_auto_calibrate = false;
                }

                //Reset old_MovingPlatform_angle if not on a moving platform anymore
                if(last_surface != __instance.currentSurface)
                {
                    //MelonLogger.Msg("Surface changed!");
                    last_surface = __instance.currentSurface;
                    old_MovingPlatform_angle = float.NaN;
                }
            }
        }


        [HarmonyPatch(typeof(VertigoCharacterController), "Move", new Type[] {typeof(Vector3)})]
        public static class VertigoCharacterController_Move_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref Vector3 ___input)
            {
                if (!in_main_menu)
                {
                    var ws = KATNativeSDK.GetWalkStatus();

                    Quaternion direction = ws.bodyRotationRaw * Quaternion.Inverse(Quaternion.Euler(new UnityEngine.Vector3(0, yawCorrection, 0)));
                    //(0,0,Speed)
                    UnityEngine.Vector3 speed = ws.moveSpeed * speedMul;
                    Vector3 velocity = direction * speed;
                    //Debug.Log("velocity: " + velocity.ToString());
                    float ratio1 = CalculateRatioModified(Mathf.Abs(velocity.x), 0.0f, 5f, 0.0f, speedMaxRange);
                    float ratio2 = CalculateRatioModified(Mathf.Abs(velocity.z), 0.0f, 5f, 0.0f, speedMaxRange);
                    UnityEngine.Vector3 kat_result = new UnityEngine.Vector3((double)velocity.x > 0.0 ? ratio1 : -ratio1, 0.0f, (double)velocity.z > 0.0 ? ratio2 : -ratio2);


                    if (joystick_disabled)
                    {
                        ___input = kat_result;
                        return false; //Bypass original function
                    }
                    else
                    {
                        if (kat_result == Vector3.zero)
                        {
                            return true;
                        }
                        else
                        {
                            ___input = kat_result;
                            return false; //Bypass original function
                        }
                    }
                }
                else
                    return true;
            }
        }

        public static float CalculateRatioModified(
          float input,
          float inputMin,
          float inputMax,
          float outputMin,
          float outputMax)
        {
            if ((double)input > (double)inputMax)
                input = inputMax;
            if ((double)input < (double)inputMin)
                input = inputMin;

            double normalized_input = ((double)input - (double)inputMin) / ((double)inputMax - (double)inputMin);

            return (float)(Math.Pow(normalized_input, (Double)speedExponent) * ((double)outputMax - (double)outputMin) + outputMin);
        }


        //Fix yawCorrection if player is on a moving platform which turns the player
        [HarmonyPatch(typeof(MovingPlatform), "Update", new Type[] { })]
        public static class MovingPlatform_Patch
        {

            [HarmonyPostfix]
            public static void Postfix(MovingPlatform __instance)
            {
                VertigoCharacterController controller_instance = VertigoCharacterController.instance;
                //MelonLogger.Msg("type: " + controller_instance.currentSurface.GetType().Name);
                if (((UnityEngine.Object)controller_instance.currentSurface == (UnityEngine.Object)__instance)) //Player is on the moving platform
                {
                    if(!float.IsNaN(old_MovingPlatform_angle))
                    {
                        float difference = old_MovingPlatform_angle - __instance.transform.rotation.eulerAngles.y;
                        yawCorrection += difference;
                    }
                    old_MovingPlatform_angle = __instance.transform.rotation.eulerAngles.y;
                }
            }
        }

    }
}