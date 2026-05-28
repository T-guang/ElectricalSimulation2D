using System.Collections.Generic;

namespace ElectricalSim.Core
{
    [System.Serializable]
    public sealed class ComponentParameterSet
    {
        public List<ComponentParameter> parameters = new List<ComponentParameter>();

        public void SetParameters(IEnumerable<ComponentParameter> source)
        {
            parameters.Clear();
            if (source == null)
            {
                return;
            }

            foreach (var parameter in source)
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.key))
                {
                    continue;
                }

                var clone = parameter.Clone();
                clone.ClampValue();
                parameters.Add(clone);
            }
        }

        public ComponentParameter GetParameter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return parameters.Find(item => item != null && item.key == key);
        }

        public bool SetParameterValue(string key, float value)
        {
            var parameter = GetParameter(key);
            if (parameter == null)
            {
                return false;
            }

            parameter.value = value;
            parameter.ClampValue();
            return true;
        }

        public List<ComponentParameter> CloneList()
        {
            var result = new List<ComponentParameter>();
            foreach (var parameter in parameters)
            {
                if (parameter != null)
                {
                    result.Add(parameter.Clone());
                }
            }

            return result;
        }
    }
}
