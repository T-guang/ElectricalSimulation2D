using UnityEngine;
using ElectricalSim.Core;

namespace ElectricalSim.UI.VisualPrefab
{
    public class MotorVisualController : MonoBehaviour
    {
        [SerializeField] private RectTransform fanPivot;
        [SerializeField] private float rotationSpeed = -540f;

        private CircuitComponent component;

        private void Awake()
        {
            component = GetComponentInParent<CircuitComponent>();
            if (fanPivot == null)
            {
                var pivotTransform = transform.Find("FanPivot");
                if (pivotTransform != null)
                {
                    fanPivot = pivotTransform as RectTransform;
                }
            }
        }

        private void Update()
        {
            if (component == null || fanPivot == null)
            {
                return;
            }

            if (component.IsEnergized)
            {
                fanPivot.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
