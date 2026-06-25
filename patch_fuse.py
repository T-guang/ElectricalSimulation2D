import re

with open('Assets/Scripts/Core/CircuitComponent.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Constants
const_target = '        private const string experimentalThreePhasePowerSpritePath = "Assets/Art/Components/AC_ThreePhase_Power_Visual.png";\n'
const_repl = '''        // Temporary fuse visual pilot. Set to false to restore the default appearance.
        private const bool useExperimentalFuseVisualPrefab = true;
        private const bool showExperimentalFuseTerminalDebugMarkers = false;
        private const string experimentalFuse1PDefinitionName = "Fuse_1P";
        private const string experimentalFuse3PDefinitionName = "Fuse_3P";
        private const string experimentalFuse1PVisualAssetPath = "Assets/Prefab/Fuse_1P_Visual.prefab";
        private const string experimentalFuse3PVisualAssetPath = "Assets/Prefab/Fuse_3P_Visual.prefab";
'''
content = content.replace(const_target, const_target + const_repl)

# Fields
field_target = '        private readonly Dictionary<string, RectTransform> experimentalThreePhasePowerTerminalAnchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);\n'
field_repl = '''        private RectTransform experimentalFuse1PVisualRoot;
        private Image experimentalFuse1PBodyImage;
        private readonly Dictionary<string, RectTransform> experimentalFuse1PTerminalAnchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);
        private RectTransform experimentalFuse3PVisualRoot;
        private Image experimentalFuse3PBodyImage;
        private readonly Dictionary<string, RectTransform> experimentalFuse3PTerminalAnchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);
'''
content = content.replace(field_target, field_target + field_repl)

# Initialize
init_target = '            TryApplyExperimentalThreePhasePowerVisualPrefab();\n'
init_repl = '''            TryApplyExperimentalFuse1PVisualPrefab();
            TryApplyExperimentalFuse3PVisualPrefab();\n'''
content = content.replace(init_target, init_target + init_repl)

# TryGetExperimentalVisualActive
active_target = '                   IsExperimentalThreePhasePowerVisualActive();\n'
active_repl = '''                   IsExperimentalThreePhasePowerVisualActive() ||
                   IsExperimentalFuse1PVisualActive() ||
                   IsExperimentalFuse3PVisualActive();\n'''
content = content.replace(active_target, active_target + active_repl)

# Active Definitions
def_target = '''        private bool IsExperimentalThreePhasePowerVisualActive()
        {
            return useExperimentalThreePhasePowerVisualPrefab &&
                   experimentalThreePhasePowerVisualRoot != null &&
                   Definition != null &&
                   string.Equals(Definition.name, experimentalThreePhasePowerDefinitionName, System.StringComparison.Ordinal);
        }\n'''
def_repl = '''
        private bool IsExperimentalFuse1PVisualActive()
        {
            return useExperimentalFuseVisualPrefab &&
                   experimentalFuse1PVisualRoot != null &&
                   Definition != null &&
                   string.Equals(Definition.name, experimentalFuse1PDefinitionName, System.StringComparison.Ordinal);
        }

        private bool IsExperimentalFuse3PVisualActive()
        {
            return useExperimentalFuseVisualPrefab &&
                   experimentalFuse3PVisualRoot != null &&
                   Definition != null &&
                   string.Equals(Definition.name, experimentalFuse3PDefinitionName, System.StringComparison.Ordinal);
        }\n'''
content = content.replace(def_target, def_target + def_repl)

# TryGetTerminalPosition
pos_target = '''            if (TryGetExperimentalThreePhasePowerTerminalPosition(terminalId, out localPosition))
            {
                showDebugMarker = showExperimentalThreePhasePowerTerminalDebugMarkers;
                return true;
            }\n'''
pos_repl = '''            if (TryGetExperimentalFuse1PTerminalPosition(terminalId, out localPosition))
            {
                showDebugMarker = showExperimentalFuseTerminalDebugMarkers;
                return true;
            }

            if (TryGetExperimentalFuse3PTerminalPosition(terminalId, out localPosition))
            {
                showDebugMarker = showExperimentalFuseTerminalDebugMarkers;
                return true;
            }\n'''
content = content.replace(pos_target, pos_target + pos_repl)

# The large block for 1P
fuse_blocks = '''
        private void TryApplyExperimentalFuse1PVisualPrefab()
        {
            experimentalFuse1PVisualRoot = null;
            experimentalFuse1PBodyImage = null;
            experimentalFuse1PTerminalAnchors.Clear();

            if (!useExperimentalFuseVisualPrefab ||
                Definition == null ||
                !string.Equals(Definition.name, experimentalFuse1PDefinitionName, System.StringComparison.Ordinal))
            {
                return;
            }

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(experimentalFuse1PVisualAssetPath);
            if (prefab == null) return;

            var visualObject = Instantiate(prefab, transform);
            visualObject.name = prefab.name + "_Pilot";
            visualObject.transform.SetAsFirstSibling();

            experimentalFuse1PVisualRoot = visualObject.GetComponent<RectTransform>();
            if (experimentalFuse1PVisualRoot != null)
            {
                experimentalFuse1PVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
                experimentalFuse1PVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
                experimentalFuse1PVisualRoot.pivot = new Vector2(0.5f, 0.5f);
                experimentalFuse1PVisualRoot.anchoredPosition = Vector2.zero;

                if (rectTransform != null &&
                    experimentalFuse1PVisualRoot.sizeDelta.x > 0f &&
                    experimentalFuse1PVisualRoot.sizeDelta.y > 0f)
                {
                    rectTransform.sizeDelta = experimentalFuse1PVisualRoot.sizeDelta;
                }
            }

            RegisterExperimentalFuse1PTerminalAnchors(visualObject.transform);

            var bodyTransform = visualObject.transform.Find("Body");
            if (bodyTransform != null)
            {
                experimentalFuse1PBodyImage = bodyTransform.GetComponent<Image>();
                if (experimentalFuse1PBodyImage != null)
                {
                    experimentalFuse1PBodyImage.raycastTarget = false;
                }
            }

            if (body != null)
            {
                body.enabled = true;
                body.raycastTarget = true;
                body.color = Color.clear;
            }

            if (title != null)
            {
                title.enabled = false;
            }
#endif
        }

        private void RegisterExperimentalFuse1PTerminalAnchors(Transform root)
        {
            if (root == null) return;
            var rects = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                var candidate = rects[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.name) || !candidate.name.StartsWith("Terminal_", System.StringComparison.Ordinal)) continue;
                var terminalId = candidate.name.Substring("Terminal_".Length);
                if (!experimentalFuse1PTerminalAnchors.ContainsKey(terminalId))
                {
                    experimentalFuse1PTerminalAnchors.Add(terminalId, candidate);
                }
            }
        }

        private bool TryGetExperimentalFuse1PTerminalPosition(string terminalId, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            if (!IsExperimentalFuse1PVisualActive() || rectTransform == null || string.IsNullOrWhiteSpace(terminalId)) return false;

            if (experimentalFuse1PTerminalAnchors.TryGetValue(terminalId, out var anchor) && anchor != null && TryGetAnchoredPositionRelativeToExperimentalRoot(anchor, experimentalFuse1PVisualRoot, out localPosition))
            {
                return true;
            }
            return false;
        }

        private void TryApplyExperimentalFuse3PVisualPrefab()
        {
            experimentalFuse3PVisualRoot = null;
            experimentalFuse3PBodyImage = null;
            experimentalFuse3PTerminalAnchors.Clear();

            if (!useExperimentalFuseVisualPrefab ||
                Definition == null ||
                !string.Equals(Definition.name, experimentalFuse3PDefinitionName, System.StringComparison.Ordinal))
            {
                return;
            }

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(experimentalFuse3PVisualAssetPath);
            if (prefab == null) return;

            var visualObject = Instantiate(prefab, transform);
            visualObject.name = prefab.name + "_Pilot";
            visualObject.transform.SetAsFirstSibling();

            experimentalFuse3PVisualRoot = visualObject.GetComponent<RectTransform>();
            if (experimentalFuse3PVisualRoot != null)
            {
                experimentalFuse3PVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
                experimentalFuse3PVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
                experimentalFuse3PVisualRoot.pivot = new Vector2(0.5f, 0.5f);
                experimentalFuse3PVisualRoot.anchoredPosition = Vector2.zero;

                if (rectTransform != null &&
                    experimentalFuse3PVisualRoot.sizeDelta.x > 0f &&
                    experimentalFuse3PVisualRoot.sizeDelta.y > 0f)
                {
                    rectTransform.sizeDelta = experimentalFuse3PVisualRoot.sizeDelta;
                }
            }

            RegisterExperimentalFuse3PTerminalAnchors(visualObject.transform);

            var bodyTransform = visualObject.transform.Find("Body");
            if (bodyTransform != null)
            {
                experimentalFuse3PBodyImage = bodyTransform.GetComponent<Image>();
                if (experimentalFuse3PBodyImage != null)
                {
                    experimentalFuse3PBodyImage.raycastTarget = false;
                }
            }

            if (body != null)
            {
                body.enabled = true;
                body.raycastTarget = true;
                body.color = Color.clear;
            }

            if (title != null)
            {
                title.enabled = false;
            }
#endif
        }

        private void RegisterExperimentalFuse3PTerminalAnchors(Transform root)
        {
            if (root == null) return;
            var rects = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                var candidate = rects[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.name) || !candidate.name.StartsWith("Terminal_", System.StringComparison.Ordinal)) continue;
                var terminalId = candidate.name.Substring("Terminal_".Length);
                if (!experimentalFuse3PTerminalAnchors.ContainsKey(terminalId))
                {
                    experimentalFuse3PTerminalAnchors.Add(terminalId, candidate);
                }
            }
        }

        private bool TryGetExperimentalFuse3PTerminalPosition(string terminalId, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            if (!IsExperimentalFuse3PVisualActive() || rectTransform == null || string.IsNullOrWhiteSpace(terminalId)) return false;

            if (experimentalFuse3PTerminalAnchors.TryGetValue(terminalId, out var anchor) && anchor != null && TryGetAnchoredPositionRelativeToExperimentalRoot(anchor, experimentalFuse3PVisualRoot, out localPosition))
            {
                return true;
            }
            return false;
        }
'''

content = content + "\n" + fuse_blocks
# Actually we should place fuse_blocks inside the class, before the last bracket
last_brace_idx = content.rfind('}')
last_brace_idx2 = content.rfind('}', 0, last_brace_idx)

# Place it before the last two braces (assuming namespace and class)
content = content[:last_brace_idx2] + fuse_blocks + content[last_brace_idx2:]

with open('Assets/Scripts/Core/CircuitComponent.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Patched successfully")
