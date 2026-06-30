using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.Core
{
    public sealed class KTTimerVisualController : MonoBehaviour
    {
        private const string DelayParameterKey = "delaySeconds";
        private const float MinDelaySeconds = 0f;
        private const float MaxDelaySeconds = 10f;

        [SerializeField] private Text displayText;
        [SerializeField] private RectTransform hitAreaDisplay;
        [SerializeField] private RectTransform hitAreaSet;
        [SerializeField] private RectTransform hitAreaPlus;
        [SerializeField] private RectTransform hitAreaMinus;

        private CircuitComponent component;
        private WorkspaceController workspace;
        private float lastDisplayedSeconds = -1f;
        private TimerRuntimePhase lastPhase = (TimerRuntimePhase)(-1);
        private bool lastCoilEnergized;

        public void Initialize(CircuitComponent owner, WorkspaceController ownerWorkspace)
        {
            component = owner;
            workspace = ownerWorkspace;
            ResolveReferences();
            ConfigureDisplayText();
            ConfigureHitArea(hitAreaDisplay, OpenDelayDialog);
            ConfigureHitArea(hitAreaSet, OpenDelayDialog);
            ConfigureHitArea(hitAreaPlus, IncreaseDelay);
            ConfigureHitArea(hitAreaMinus, DecreaseDelay);
            RefreshNow();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureDisplayText();
        }

        private void Update()
        {
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (displayText == null || component == null)
            {
                return;
            }

            var displaySeconds = ResolveDisplaySeconds(out var phase, out var coilEnergized);
            if (Mathf.Approximately(displaySeconds, lastDisplayedSeconds) &&
                phase == lastPhase &&
                coilEnergized == lastCoilEnergized)
            {
                return;
            }

            lastDisplayedSeconds = displaySeconds;
            lastPhase = phase;
            lastCoilEnergized = coilEnergized;
            displayText.text = FormatSeconds(Mathf.CeilToInt(displaySeconds));
        }

        private void ResolveReferences()
        {
            if (displayText == null)
            {
                var display = transform.Find("DisplayText");
                displayText = display != null ? display.GetComponent<Text>() : null;
            }

            var areas = transform.Find("InteractionAreas");
            if (areas == null)
            {
                return;
            }

            hitAreaDisplay = hitAreaDisplay != null ? hitAreaDisplay : FindRect(areas, "HitArea_Display");
            hitAreaSet = hitAreaSet != null ? hitAreaSet : FindRect(areas, "HitArea_Set");
            hitAreaPlus = hitAreaPlus != null ? hitAreaPlus : FindRect(areas, "HitArea_Plus");
            hitAreaMinus = hitAreaMinus != null ? hitAreaMinus : FindRect(areas, "HitArea_Minus");
        }

        private static RectTransform FindRect(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            return child != null ? child as RectTransform : null;
        }

        private void ConfigureDisplayText()
        {
            if (displayText == null)
            {
                return;
            }

            displayText.raycastTarget = false;
            displayText.color = new Color(1f, 0.08f, 0.05f, 1f);
            displayText.alignment = TextAnchor.MiddleCenter;
            displayText.fontStyle = FontStyle.Bold;
            displayText.horizontalOverflow = HorizontalWrapMode.Overflow;
            displayText.verticalOverflow = VerticalWrapMode.Overflow;
            displayText.resizeTextForBestFit = true;
            displayText.resizeTextMinSize = 16;
            displayText.resizeTextMaxSize = 32;
        }

        private static void ConfigureHitArea(RectTransform area, System.Action action)
        {
            if (area == null)
            {
                return;
            }

            var image = area.GetComponent<Image>();
            if (image == null)
            {
                image = area.gameObject.AddComponent<Image>();
            }

            image.color = Color.clear;
            image.raycastTarget = true;

            var hitArea = area.GetComponent<KTTimerHitArea>();
            if (hitArea == null)
            {
                hitArea = area.gameObject.AddComponent<KTTimerHitArea>();
            }

            hitArea.Configure(action);
        }

        private void IncreaseDelay()
        {
            SetDelaySeconds(ResolveDelaySeconds() + 1f);
        }

        private void DecreaseDelay()
        {
            SetDelaySeconds(ResolveDelaySeconds() - 1f);
        }

        private void OpenDelayDialog()
        {
            KTDelaySettingDialog.Show(component, workspace, SetDelaySeconds);
        }

        private void SetDelaySeconds(float value)
        {
            if (component == null)
            {
                return;
            }

            var clamped = Mathf.Clamp(value, MinDelaySeconds, MaxDelaySeconds);
            if (!component.SetParameterValue(DelayParameterKey, clamped))
            {
                return;
            }

            RefreshNow();
            workspace?.RefreshParameterPanelFor(component);
            workspace?.MarkSimulationDirty("时间继电器延时时间已修改，点击开始仿真刷新结果。");
        }

        private float ResolveDisplaySeconds(out TimerRuntimePhase phase, out bool coilEnergized)
        {
            var delaySeconds = ResolveDelaySeconds();
            phase = TimerRuntimePhase.Reset;
            coilEnergized = false;

            if (component != null &&
                RuntimeStateManager.Shared.TryGetTimerState(component.InstanceId, out var timerState) &&
                timerState != null)
            {
                phase = timerState.Phase;
                coilEnergized = timerState.IsCoilEnergized;
                if (timerState.IsCoilEnergized)
                {
                    if (timerState.Phase == TimerRuntimePhase.Elapsed)
                    {
                        return 0f;
                    }

                    if (timerState.Phase == TimerRuntimePhase.Timing)
                    {
                        var runtimeDelay = Mathf.Clamp(timerState.DelaySeconds > 0f ? timerState.DelaySeconds : delaySeconds, MinDelaySeconds, MaxDelaySeconds);
                        return Mathf.Max(0f, runtimeDelay - Mathf.Max(0f, timerState.ElapsedSeconds));
                    }
                }
            }

            return delaySeconds;
        }

        private float ResolveDelaySeconds()
        {
            var parameter = component != null ? component.GetParameter(DelayParameterKey) : null;
            return Mathf.Clamp(parameter != null ? parameter.value : 3f, MinDelaySeconds, MaxDelaySeconds);
        }

        internal static string FormatSeconds(int seconds)
        {
            seconds = Mathf.Clamp(seconds, 0, Mathf.RoundToInt(MaxDelaySeconds));
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        internal static bool TryParseDelaySeconds(string text, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var normalized = text.Trim().Replace("秒", string.Empty);
            var parts = normalized.Split(':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var minutes) &&
                int.TryParse(parts[1], out var sec))
            {
                seconds = Mathf.Clamp(minutes * 60 + sec, MinDelaySeconds, MaxDelaySeconds);
                return true;
            }

            if (float.TryParse(normalized, out var value))
            {
                seconds = Mathf.Clamp(value, MinDelaySeconds, MaxDelaySeconds);
                return true;
            }

            return false;
        }
    }
}
