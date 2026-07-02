using System;
using UnityEngine;

namespace ElectricalSim.UI.CommonTools
{
    [Serializable]
    public sealed class ResistorColorEntry
    {
        public string Name;
        public int? Digit;
        public double? Multiplier;
        public string Tolerance;
        public Color Color;

        public bool HasDigit
        {
            get { return Digit.HasValue; }
        }

        public bool HasMultiplier
        {
            get { return Multiplier.HasValue; }
        }

        public bool HasTolerance
        {
            get { return !string.IsNullOrWhiteSpace(Tolerance); }
        }
    }
}
