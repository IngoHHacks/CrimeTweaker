using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.UIElements;

namespace CrimeTweaker
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "IngoH.HardTime.CrimeTweaker";
        public const string PluginName = "CrimeTweaker";
        public const string PluginVer = "1.0.1";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;

        internal static ConfigEntry<float> OverallCrimeModifier;
        internal static ConfigEntry<float> StarCrimeModifier;
        internal static ConfigEntry<float> AICrimeModifier;

        internal static List<ConfigEntry<float>> PerCrimeModifiers = new();
        
        internal static List<string> Crimes =
        [
            "insubordination",
            "conspiracy",
            "possession",
            "vandalism",
            "escaping",
            "theft",
            "fornication",
            "arson",
            "assault",
            "assault with a weapon",
            "grievious bodily harm",
            "attempted murder",
            "murder",
            "serial murder",
            "terrorism",
            "trespassing",
            "trading",
            "freeloading (unused)",
            "urination/defecation",
            "tardiness (unused)",
            "truancy",
            "sleeping",
            "graffiti (partially unused)",
            "shoplifting (unused)",
            "snacking",
            "reckless driving",
            "discharging a firearm",
            "animal abuse",
            "impersonation"
        ];



        private void Awake()
        {
            Plugin.Log = base.Logger;

            PluginPath = Path.GetDirectoryName(Info.Location);

            OverallCrimeModifier = Config.Bind("General", "OverallCrimeModifier", 100f, new ConfigDescription("Changes the overall percent chance of crimes being witnessed.", new AcceptableValueRange<float>(0f, 1e9f)));
            StarCrimeModifier = Config.Bind("General", "StarCrimeModifier", 100f, new ConfigDescription("Changes the percent chance of the player being witnessed committing a crime.", new AcceptableValueRange<float>(0f, 1e9f)));
            AICrimeModifier = Config.Bind("General", "AICrimeModifier", 100f, new ConfigDescription("Changes the percent chance of AI being witnessed committing a crime.", new AcceptableValueRange<float>(0f, 1e9f)));
            foreach (var crime in Crimes)
            {
                PerCrimeModifiers.Add(Config.Bind("General", crime.ToPascalCase() + "CrimeModifier", 100f, new ConfigDescription($"Changes the percent chance of {crime} being witnessed.", new AcceptableValueRange<float>(0f, 1e9f))));
            }
        }

        private void OnEnable()
        {
            Harmony.PatchAll();
            Logger.LogInfo($"Loaded {PluginName}!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            Logger.LogInfo($"Unloaded {PluginName}!");
        }

        [HarmonyPatch(typeof(DFOGOCNBECG), nameof(DFOGOCNBECG.DOLDKPGLHBL))]
        [HarmonyPrefix]
        public static bool DFOGOCNBECG_DOLDKPGLHBL(DFOGOCNBECG __instance, int LMCLGBPJKFM, int NFCNFEIIFMA, int DEGGCDHLFBC, int MKEBAFANECN, ref int KPJANCMHICM)
        {
            var pct = (1.0 / (1 + KPJANCMHICM));
            pct = AdjustProb(__instance, LMCLGBPJKFM, NFCNFEIIFMA, DEGGCDHLFBC, MKEBAFANECN, pct);
            KPJANCMHICM = (int)(1.0 / pct) - 1;
            if (KPJANCMHICM < 1e-6)
            {
                return false;
            }
            return true;
        }

        private static double AdjustProb(DFOGOCNBECG culprit, int warrant, int victim, int witness, int variable, double prob)
        {
            if (warrant > 0 && warrant <= Crimes.Count) {
                Log.LogDebug($"{Characters.c[culprit.GOOKPABIPBC].name} was spotted doing {Crimes[warrant - 1]}! Normal prob: {prob*100}%");
            }
            prob *= OverallCrimeModifier.Value / 100;
            if (culprit.GOOKPABIPBC == Characters.star)
            {
                prob *= StarCrimeModifier.Value / 100;
            }
            else
            {
                prob *= AICrimeModifier.Value / 100;
            }
            if (warrant > 0 && warrant <= Crimes.Count)
            {
                prob *= PerCrimeModifiers[warrant - 1].Value / 100;
            }
            if (prob > 1)
            {
                prob = 1;
            }
            if (prob < 1e-9)
            {
                prob = 1e-9;
            }
            if (warrant > 0 && warrant <= Crimes.Count) {
                Log.LogDebug($"Adjusted prob: {prob*100}%");
            }
            return prob;
        }
    }
}