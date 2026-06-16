using System;
using System.Collections.Generic;
using UnityEngine;

namespace WattsLeft
{
    public static class WattsLeftConfig
    {
        private static bool isLoaded = false;
        private static readonly List<WattsLeftIsotope> isotopes = new List<WattsLeftIsotope>();

        // Settings with stock defaults as fallback
        public static double KerbinYearSeconds { get; private set; } = 9203545.0;
        public static double ElectricityScalar { get; private set; } = 1000.0;

        public static IReadOnlyList<WattsLeftIsotope> Isotopes
        {
            get
            {
                Load();
                return isotopes;
            }
        }

        public static void Load()
        {
            if (isLoaded)
            {
                return;
            }

            LoadSettings();
            LoadIsotopes();

            isLoaded = true;
        }

        private static void LoadSettings()
        {
            ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes("WATTSLEFT_SETTINGS");

            if (settingsNodes.Length == 0)
            {
                Debug.LogWarning("[WattsLeft] No WATTSLEFT_SETTINGS node found, using defaults.");
                return;
            }

            if (settingsNodes.Length > 1)
            {
                Debug.LogWarning("[WattsLeft] Multiple WATTSLEFT_SETTINGS nodes found, using the first.");
            }

            ConfigNode node = settingsNodes[0];

            string rawYear = node.GetValue("kerbinYearSeconds");
            if (rawYear != null && double.TryParse(rawYear, out double parsedYear) && parsedYear > 0.0)
            {
                KerbinYearSeconds = parsedYear;
            }
            else
            {
                Debug.LogWarning("[WattsLeft] Invalid or missing kerbinYearSeconds, using default: " + KerbinYearSeconds);
            }

            string rawScalar = node.GetValue("electricityScalar");
            if (rawScalar != null && double.TryParse(rawScalar, out double parsedScalar) && parsedScalar > 0.0)
            {
                ElectricityScalar = parsedScalar;
            }
            else
            {
                Debug.LogWarning("[WattsLeft] Invalid or missing electricityScalar, using default: " + ElectricityScalar);
            }

            Debug.Log("[WattsLeft] Settings loaded — kerbinYearSeconds: " + KerbinYearSeconds + ", electricityScalar: " + ElectricityScalar);
        }

        private static void LoadIsotopes()
        {
            isotopes.Clear();

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("WATTSLEFT_ISOTOPE");

            foreach (ConfigNode node in nodes)
            {
                try
                {
                    WattsLeftIsotope isotope = new WattsLeftIsotope(node);
                    isotopes.Add(isotope);
                    Debug.Log("[WattsLeft] Loaded isotope: " + isotope.Name);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[WattsLeft] Failed to load isotope:\n" + ex);
                }
            }

            Debug.Log("[WattsLeft] Loaded " + isotopes.Count + " isotope configs.");
        }

        public static WattsLeftIsotope FindIsotope(string resourceName)
        {
            Load();

            foreach (WattsLeftIsotope isotope in isotopes)
            {
                if (isotope.Name == resourceName)
                {
                    return isotope;
                }
            }
            return null;
        }
    }
}