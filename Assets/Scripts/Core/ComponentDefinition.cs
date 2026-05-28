using System.Collections.Generic;
using UnityEngine;

namespace ElectricalSim.Core
{
    [CreateAssetMenu(menuName = "Electrical Simulation/Component Definition")]
    public sealed class ComponentDefinition : ScriptableObject
    {
        [Header("Palette")]
        public string displayName = "Component";
        public ComponentCategory category = ComponentCategory.Household;
        public ComponentKind kind = ComponentKind.Switch;
        public Color bodyColor = new Color(0.86f, 0.88f, 0.9f);
        public Color accentColor = new Color(0.2f, 0.45f, 1f);
        public Sprite sprite;
        public Vector2 size = new Vector2(110f, 130f);

        [Header("Electrical")]
        public bool startsClosed;
        public bool togglable;
        public string controlledByTag;
        public string outputTag;

        [Header("Simple Teaching Parameters")]
        public float sourceVoltage;
        public float sourceLineVoltage;
        public int sourcePhaseCount;
        public float ratedVoltage;
        public float ratedPower;
        public float ratedCurrent;
        public float maxVoltage;
        public float maxCurrent;
        public bool canBurnOut;
        public bool canTrip;
        public string parameterNote;

        public List<TerminalDefinition> terminals = new List<TerminalDefinition>();
    }
}

