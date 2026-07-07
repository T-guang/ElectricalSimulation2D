using UnityEngine;
using ElectricalSim.Core;

namespace ElectricalSim.UI.VisualPrefab
{
    public class MotorVisualController : MonoBehaviour
    {
        [SerializeField] private RectTransform fanPivot;
        [SerializeField] private float rotationSpeed = -720f;

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
                var dir = 1f;
                var param = component.GetParameter("rotationDirection");
                if (param != null)
                {
                    if (param.value < -0.5f) dir = -1f;
                    else if (param.value > -0.5f && param.value < 0.5f) dir = 0f;
                }

                if (dir != 0f)
                {
                    fanPivot.Rotate(0, 0, dir * rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
}
