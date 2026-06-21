using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace WattsLeft
{
    [KSPModule("Watts Left RTG")]
    public class ModuleWattsLeftRTG : PartModule
    {
        // editor info fields
        [KSPField]
        public string b9ModuleID = "wattsLeftIsotope";

        [KSPField]
        public double infoOutputThreshold = 0.1;

        // status fields
        [KSPField(
            guiName = "Status",
            guiActive = true,
            guiActiveEditor = true,
            groupName = "WattsLeft",
            groupDisplayName = "RTG"
        )]
        public string statusDisplay = "Loaded";

        [KSPField(
            guiName = "Output",
            guiActive = true,
            guiActiveEditor = true,
            groupName = "WattsLeft",
            groupDisplayName = "RTG"
        )]
        public string currentOutputDisplay = "n/a";

        // invisible utility fields
        [KSPField(isPersistant = true)]
        public double timeOfStart = -1.0;

        [KSPField(isPersistant = true)]
        public bool generateElectricity = true;

        // variables
        private WattsLeftIsotope selectedIsotope;
        private PartResource selectedResource;
        private string selectedResourceName;
        private double selectedResourceDensity;
        private double selectedResourceMaxAmount = -1.0;
        private double peakOutput;
        private bool isotopeIsSelected = false;
        private const float DisplayUpdateInterval = 0.25f;
        private float nextDisplayUpdateTime = 0f;

        // helpers
        private double CalculatePeakOutput()
        {
            if (selectedIsotope == null || selectedResource == null)
            {
                return 0.0;
            }

            double fuelMass = selectedResource.maxAmount * selectedResourceDensity;
            return selectedIsotope.PowerDensity * fuelMass;
        }

        private double GetResourceDensity(string resourceName)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            if (definition == null)
            {
                Debug.LogError("[WattsLeft] No RESOURCE_DEFINITION found for " + resourceName);
                return 0.0;
            }

            return definition.density;
        }

        private double GetConfiguredIsotopeAmount()
        {
            double baseVolume;

            if (TryGetB9BaseVolume(out baseVolume))
            {
                return baseVolume;
            }

            if (selectedResource != null && selectedResource.maxAmount > 0.0)
            {
                return selectedResource.maxAmount;
            }

            Debug.LogWarning("[WattsLeft] Could not determine B9 baseVolume for part info. Falling back to 1.0.");
            return 1.0;
        }

        private bool TryGetB9BaseVolume(out double baseVolume)
        {
            baseVolume = 0.0;

            if (part == null || part.partInfo == null || part.partInfo.partConfig == null)
            {
                return false;
            }

            ConfigNode[] moduleNodes = part.partInfo.partConfig.GetNodes("MODULE");

            foreach (ConfigNode moduleNode in moduleNodes)
            {
                if (!moduleNode.HasValue("name") || moduleNode.GetValue("name") != "ModuleB9PartSwitch")
                {
                    continue;
                }

                string moduleID = moduleNode.HasValue("moduleID") ? moduleNode.GetValue("moduleID") : "";

                if (!string.IsNullOrEmpty(b9ModuleID) && moduleID != b9ModuleID)
                {
                    continue;
                }

                if (!moduleNode.HasValue("baseVolume"))
                {
                    continue;
                }

                if (double.TryParse(
                    moduleNode.GetValue("baseVolume"),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out baseVolume
                ) && baseVolume > 0.0)
                {
                    return true;
                }
            }

            return false;
        }

        private double GetUsableOutput(double remainingFraction)
        {
            if (selectedIsotope == null || selectedResource == null)
            {
                return 0.0;
            }

            if (remainingFraction <= selectedIsotope.CutoffFraction)
            {
                return 0.0;
            }

            return peakOutput * remainingFraction * WattsLeftConfig.ElectricityScalar;
        }

        private double GetRemainingFraction()
        {
            if (selectedIsotope == null)
            {
                return 0.0;
            }

            if (timeOfStart < 0.0)
            {
                return 1.0;
            }

            double now = Planetarium.GetUniversalTime();
            double elapsedSeconds = now - timeOfStart;
            double halfLifeSeconds = selectedIsotope.HalfLife * WattsLeftConfig.KerbinYearSeconds;

            if (halfLifeSeconds <= 0.0)
            {
                return 0.0;
            }

            double fraction = Math.Pow(2.0, -(elapsedSeconds / halfLifeSeconds));

            if (fraction < 0.0)
            {
                return 0.0;
            }

            if (fraction > 1.0)
            {
                return 1.0;
            }

            return fraction;
        }

        private void UpdateDisplays(double remainingFraction)
        {
            if (selectedIsotope == null || selectedResource == null)
            {
                statusDisplay = "No isotope";
                currentOutputDisplay = "n/a";
                return;
            }

            double usableOutput = GetUsableOutput(remainingFraction);

            currentOutputDisplay = FormatOutput(usableOutput) + " (" + (remainingFraction * 100.0).ToString("0.##") + "% of peak)";

            if (remainingFraction <= selectedIsotope.CutoffFraction)
            {
                statusDisplay = "No usable output";
            }
            else if (remainingFraction <= selectedIsotope.WarningFraction)
            {
                statusDisplay = "Nearing end of life";
            }
            else
            {
                statusDisplay = "Nominal";
            }
        }

        private void UpdateDisplaysThrottled(double remainingFraction, bool force = false)
        {
            if (!force && Time.realtimeSinceStartup < nextDisplayUpdateTime)
            {
                return;
            }

            UpdateDisplays(remainingFraction);
            nextDisplayUpdateTime = Time.realtimeSinceStartup + DisplayUpdateInterval;
        }

        private string FormatOutput(double electricChargePerSecond)
        {
            if (electricChargePerSecond <= 0.0)
            {
                return "0 EC/s";
            } 
            else if (electricChargePerSecond < 0.01)
            {
                return (electricChargePerSecond * 60.0).ToString("0.###") + " EC/min";
            } 
            else if (electricChargePerSecond < 1.0)
            {
                return electricChargePerSecond.ToString("0.###") + " EC/s";
            }

            return electricChargePerSecond.ToString("0.##") + " EC/s";
        }

        // editor helpers
        private const double KerbinDaySeconds = 21600.0;

        private double GetYearsUntilFraction(WattsLeftIsotope isotope, double fraction)
        {
            if (isotope == null || fraction <= 0.0 || fraction >= 1.0)
            {
                return 0.0;
            }

            return -isotope.HalfLife * Math.Log(fraction) / Math.Log(2.0);
        }

        private string FormatKerbinDuration(double years)
        {
            if (double.IsNaN(years) || double.IsInfinity(years) || years < 0.0)
            {
                return "n/a";
            }

            double seconds = years * WattsLeftConfig.KerbinYearSeconds;
            double days = seconds / KerbinDaySeconds;

            if (days < 1.0)
            {
                return (days * 6.0).ToString("0.#") + " hours";
            }

            if (years < 1.0)
            {
                return days.ToString("0.#") + " days";
            }

            if (years < 10.0)
            {
                return years.ToString("0.##") + " years";
            }

            return years.ToString("0.#") + " years";
        }

        private double CalculateInfoPeakOutput(WattsLeftIsotope isotope, double isotopeAmount)
        {
            if (isotope == null)
            {
                return 0.0;
            }

            double density = GetResourceDensity(isotope.Name);

            if (density <= 0.0)
            {
                return 0.0;
            }

            double fuelMass = isotopeAmount * density;
            return isotope.PowerDensity * fuelMass * WattsLeftConfig.ElectricityScalar;
        }

        private string FormatTimeUntilBelowOutput(WattsLeftIsotope isotope, double peakOutputValue, double targetOutput)
        {
            if (isotope == null || peakOutputValue <= 0.0 || targetOutput <= 0.0)
            {
                return "n/a";
            }

            if (peakOutputValue <= targetOutput)
            {
                return "below at launch";
            }

            double targetFraction = targetOutput / peakOutputValue;

            if (targetFraction < isotope.CutoffFraction)
            {
                targetFraction = isotope.CutoffFraction;
            }

            return FormatKerbinDuration(GetYearsUntilFraction(isotope, targetFraction));
        }

        private void ClearSelectedIsotope(string status)
        {
            selectedIsotope = null;
            selectedResource = null;
            selectedResourceName = null;
            selectedResourceDensity = 0.0;
            selectedResourceMaxAmount = -1.0;
            peakOutput = 0.0;
            isotopeIsSelected = false;

            statusDisplay = status;
            currentOutputDisplay = "n/a";
        }


        // b9 insanity
        public override string GetInfo()
        {
            WattsLeftConfig.Load();

            StringBuilder info = new StringBuilder();
            double isotopeAmount = GetConfiguredIsotopeAmount();

            info.AppendLine("Output decays over time.");
            info.AppendLine("Isotope amount: " + isotopeAmount.ToString("0.###") + " kg");
            info.AppendLine();

            info.AppendLine("<color=#99ff00>Available isotopes:</color>");

            foreach (WattsLeftIsotope isotope in WattsLeftConfig.Isotopes)
            {
                double peak = CalculateInfoPeakOutput(isotope, isotopeAmount);
                double warningYears = GetYearsUntilFraction(isotope, isotope.WarningFraction);
                double cutoffYears = GetYearsUntilFraction(isotope, isotope.CutoffFraction);

                info.AppendLine();
                info.AppendLine("<color=#ffcc66>" + isotope.Title + "</color>");
                info.AppendLine("  Peak output: " + FormatOutput(peak));
                info.AppendLine("  Half-life: " + FormatKerbinDuration(isotope.HalfLife));
                info.AppendLine("  Below " + FormatOutput(infoOutputThreshold) + ": " + FormatTimeUntilBelowOutput(isotope, peak, infoOutputThreshold));
                info.AppendLine("  No output after: " + FormatKerbinDuration(cutoffYears));
            }

            return info.ToString();
        }

        private void RefreshSelectedIsotope()
        {
            WattsLeftIsotope detectedIsotope = null;
            PartResource detectedResource = null;

            if (part == null)
            {
                ClearSelectedIsotope("No part");
                return;
            }

            foreach (PartResource resource in part.Resources)
            {
                WattsLeftIsotope isotope = WattsLeftConfig.FindIsotope(resource.resourceName);

                if (isotope != null)
                {
                    detectedIsotope = isotope;
                    detectedResource = resource;
                    break;
                }
            }

            if (detectedIsotope == null || detectedResource == null)
            {
                bool wasSelected = isotopeIsSelected;

                ClearSelectedIsotope("No isotope");

                if (wasSelected)
                {
                    Debug.LogWarning("[WattsLeft] Isotope resource was removed from part " + part.partInfo.title);
                }

                return;
            }

            // recalculate peak output in case the resource/density changed like with tweakscale or something
            bool resourceChanged = !isotopeIsSelected || selectedResource != detectedResource || selectedResourceName != detectedResource.resourceName;
            bool amountChanged = Math.Abs(selectedResourceMaxAmount - detectedResource.maxAmount) > 0.000001;

            if (!resourceChanged && !amountChanged)
            {
                return;
            }

            double density = GetResourceDensity(detectedResource.resourceName);

            if (density <= 0.0)
            {
                ClearSelectedIsotope("Invalid isotope resource");

                Debug.LogError(
                    "[WattsLeft] Isotope resource " +
                    detectedResource.resourceName +
                    " on part " +
                    part.partInfo.title +
                    " has invalid density."
                );

                return;
            }

            selectedIsotope = detectedIsotope;
            selectedResource = detectedResource;
            selectedResourceName = detectedResource.resourceName;
            selectedResourceDensity = density;
            selectedResourceMaxAmount = detectedResource.maxAmount;

            peakOutput = CalculatePeakOutput();

            isotopeIsSelected = true;

            UpdateDisplaysThrottled(GetRemainingFraction(), true);
        }

        public override void OnStart(StartState state)
        {
            WattsLeftConfig.Load();

            Debug.Log("[WattsLeft] ModuleWattsLeftRTG started in scene: " + HighLogic.LoadedScene);

            RefreshSelectedIsotope();

            // resets RTG time in editor so newly placed ones dont decay instantly
            if (HighLogic.LoadedSceneIsEditor)
            {
                timeOfStart = -1.0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();

                if (timeOfStart < 0.0)
                {
                    timeOfStart = Planetarium.GetUniversalTime();
                }
            }

            UpdateDisplaysThrottled(GetRemainingFraction(), true);
        }

        private void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                RefreshSelectedIsotope();
                UpdateDisplaysThrottled(GetRemainingFraction());
            }
        }

        // evil maths
        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            if (!isotopeIsSelected || selectedIsotope == null || selectedResource == null)
            {
                RefreshSelectedIsotope();

                if (!isotopeIsSelected)
                {
                    UpdateDisplaysThrottled(0.0);
                    return;
                }
            }

            if (timeOfStart < 0.0)
            {
                timeOfStart = Planetarium.GetUniversalTime();
                Debug.Log("[WattsLeft] timeOfStart initialized to " + timeOfStart);
            }

            double remainingFraction = GetRemainingFraction();

            // RTG fuel is not actually consumed but this visually shows the decayed amount of fuel
            selectedResource.amount = selectedResource.maxAmount * remainingFraction;

            double usableOutput = GetUsableOutput(remainingFraction);

            if (generateElectricity && usableOutput > 0.0)
            {
                double electricChargeToGenerate = usableOutput * TimeWarp.fixedDeltaTime;
                part.RequestResource("ElectricCharge", -electricChargeToGenerate, false);
            }

            UpdateDisplaysThrottled(remainingFraction);
        }
    }
}