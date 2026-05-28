using System;
using UnityEngine;

namespace ElectricalSim.Core
{
    public enum ComponentKind
    {
        PowerSource,
        Switch,
        TwoWaySwitch,
        PushButton,
        Fuse,
        Breaker,
        EnergyMeter,
        Lamp,
        Fan,
        Motor,
        ContactorCoil,
        Indicator,
        TerminalBlock,
        Instrument
    }

    public enum ComponentCategory
    {
        Household,
        Industrial,
        Measurement
    }

    public enum TerminalRole
    {
        Generic,
        Phase,
        Neutral,
        ProtectiveEarth,
        Input,
        Output,
        CoilA1,
        CoilA2
    }

    public enum WireStyle
    {
        Straight,
        Orthogonal
    }

    [Serializable]
    public sealed class TerminalDefinition
    {
        public string id = "T1";
        public string label = "T1";
        public TerminalRole role = TerminalRole.Generic;
        public Vector2 normalizedPosition = new Vector2(0.5f, 0f);
        public Color color = Color.yellow;
    }
}

