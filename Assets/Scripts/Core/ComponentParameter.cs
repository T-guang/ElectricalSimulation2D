using System;
using UnityEngine;

namespace ElectricalSim.Core
{
    [Serializable]
    public sealed class ComponentParameter
    {
        public string key;
        public string displayName;
        public float value;
        public string unit;
        public float min;
        public float max;
        public bool editable;

        public ComponentParameter Clone()
        {
            return new ComponentParameter
            {
                key = key,
                displayName = displayName,
                value = value,
                unit = unit,
                min = min,
                max = max,
                editable = editable
            };
        }

        public void ClampValue()
        {
            if (max > min)
            {
                value = Mathf.Clamp(value, min, max);
            }
        }
    }
}
