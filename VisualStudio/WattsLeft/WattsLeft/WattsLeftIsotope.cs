using System;

namespace WattsLeft
{
    public class WattsLeftIsotope
    {
        public string Name { get; private set; }
        public string Title { get; private set; }
        public double HalfLife { get; private set; }
        public double PowerDensity { get; private set; }
        public double WarningFraction { get; private set; }
        public double CutoffFraction { get; private set; }

        public WattsLeftIsotope(ConfigNode node)
        {
            if (!node.HasValue("name"))
            {
                throw new FormatException("WATTSLEFT_ISOTOPE node is missing required name");
            }

            Name = node.GetValue("name");
            Title = node.HasValue("title") ? node.GetValue("title") : Name;

            HalfLife = ReadDouble(node, "halfLife", 100.0);
            PowerDensity = ReadDouble(node, "powerDensity", 0.0);
            WarningFraction = ReadDouble(node, "warningFraction", 0.05);
            CutoffFraction = ReadDouble(node, "cutoffFraction", 0.01);
        }

        private static double ReadDouble(ConfigNode node, string key, double fallback)
        {
            if (!node.HasValue(key))
            {
                return fallback;
            }

            return double.Parse(node.GetValue(key));
        }
    }
}