using System;
using UnityEngine;

namespace WattsLeft
{
    [KSPModule("Watts Left RTG")]
    public class ModuleWattsLeftRTG : PartModule
    {
        // status fields
        [KSPField(
            guiName = "Status",
            guiActive = true,
            guiActiveEditor = true,
            groupName = "WattsLeft",
            groupDisplayName = "WattsLeft"
        )]
        public string statusDisplay = "Loaded";

        [KSPField(
            guiName = "Output",
            guiActive = true,
            guiActiveEditor = true,
            groupName = "WattsLeft",
            groupDisplayName = "WattsLeft"
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

        private double CalculatePeakOutput()
        {
            if (selectedIsotope == null || selectedResource == null)
            {
                return 0.0;
            }

            double fuelMass = selectedResource.maxAmount * selectedResourceDensity;
            return selectedIsotope.PowerDensity * fuelMass;
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

            return peakOutput * remainingFraction * WattsLeftConfig.ElectricityScale;
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