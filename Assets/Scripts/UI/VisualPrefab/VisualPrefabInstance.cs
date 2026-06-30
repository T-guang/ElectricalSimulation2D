using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.Core
{
    public sealed class VisualPrefabInstance
    {
        private readonly VisualPrefabConfig config;
        private readonly RectTransform root;
        private readonly Image bodyImage;
        private readonly Sprite defaultSprite;
        private readonly Sprite activeSprite;
        private readonly Dictionary<string, RectTransform> terminalAnchors;

        private VisualPrefabInstance(
            VisualPrefabConfig config,
            RectTransform root,
            Image bodyImage,
            Sprite defaultSprite,
            Sprite activeSprite,
            Dictionary<string, RectTransform> terminalAnchors)
        {
            this.config = config;
            this.root = root;
            this.bodyImage = bodyImage;
            this.defaultSprite = defaultSprite;
            this.activeSprite = activeSprite;
            this.terminalAnchors = terminalAnchors;
        }

        public VisualPrefabConfig Config => config;
        public RectTransform Root => root;
        public bool IsActive => root != null;
        public bool ShowTerminalDebugMarkers => config != null && config.ShowTerminalDebugMarkers;

        public static bool TryCreate(
            VisualPrefabConfig config,
            Transform parent,
            RectTransform ownerRect,
            Image legacyBody,
            Text legacyTitle,
            out VisualPrefabInstance instance)
        {
            instance = null;
            if (config == null || parent == null || string.IsNullOrWhiteSpace(config.PrefabPath))
            {
                return false;
            }

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[VisualPrefab] Missing prefab: " + config.DefinitionName + " " + config.PrefabPath);
                return false;
            }

            var visualObject = Object.Instantiate(prefab, parent);
            visualObject.name = prefab.name + "_Visual";
            visualObject.transform.SetAsFirstSibling();

            var visualRoot = visualObject.GetComponent<RectTransform>();
            if (visualRoot != null)
            {
                visualRoot.anchorMin = new Vector2(0.5f, 0.5f);
                visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
                visualRoot.pivot = new Vector2(0.5f, 0.5f);
                visualRoot.anchoredPosition = Vector2.zero;

                if (ownerRect != null && visualRoot.sizeDelta.x > 0f && visualRoot.sizeDelta.y > 0f)
                {
                    ownerRect.sizeDelta = visualRoot.sizeDelta;
                }
            }

            var bodyTransform = visualObject.transform.Find("Body");
            var visualBodyImage = bodyTransform != null ? bodyTransform.GetComponent<Image>() : null;
            if (visualBodyImage != null)
            {
                visualBodyImage.raycastTarget = false;
            }

            var defaultSprite = string.IsNullOrWhiteSpace(config.DefaultSpritePath)
                ? null
                : UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(config.DefaultSpritePath);
            var activeSprite = string.IsNullOrWhiteSpace(config.ActiveSpritePath)
                ? null
                : UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(config.ActiveSpritePath);

            if (legacyBody != null)
            {
                legacyBody.enabled = true;
                legacyBody.raycastTarget = true;
                legacyBody.color = Color.clear;
            }

            if (legacyTitle != null && config.HideLegacyTerminalLabel)
            {
                legacyTitle.enabled = false;
            }

            instance = new VisualPrefabInstance(
                config,
                visualRoot,
                visualBodyImage,
                defaultSprite,
                activeSprite,
                CollectTerminalAnchors(visualObject.transform));
            instance.UpdateBodySprite(false);
            return true;
#else
            return false;
#endif
        }

        public bool TryGetTerminalPosition(string terminalId, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            if (!IsActive || string.IsNullOrWhiteSpace(terminalId))
            {
                return false;
            }

            if (terminalAnchors.TryGetValue(terminalId, out var anchor) &&
                anchor != null &&
                TryGetAnchoredPositionRelativeToRoot(anchor, root, out localPosition))
            {
                return true;
            }

            Debug.LogWarning("[VisualPrefab] Missing TerminalAnchor: " + config.DefinitionName + " Terminal_" + terminalId);
            return false;
        }

        public void UpdateBodySprite(bool active)
        {
            if (bodyImage == null)
            {
                return;
            }

            var targetSprite = active && activeSprite != null ? activeSprite : defaultSprite;
            if (targetSprite != null && bodyImage.sprite != targetSprite)
            {
                bodyImage.sprite = targetSprite;
            }
        }

        private static Dictionary<string, RectTransform> CollectTerminalAnchors(Transform visualRoot)
        {
            var anchors = new Dictionary<string, RectTransform>(System.StringComparer.OrdinalIgnoreCase);
            if (visualRoot == null)
            {
                return anchors;
            }

            var rects = visualRoot.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                var candidate = rects[i];
                if (candidate == null ||
                    string.IsNullOrEmpty(candidate.name) ||
                    !candidate.name.StartsWith("Terminal_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var terminalId = candidate.name.Substring("Terminal_".Length);
                if (!anchors.ContainsKey(terminalId))
                {
                    anchors.Add(terminalId, candidate);
                }
            }

            return anchors;
        }

        private static bool TryGetAnchoredPositionRelativeToRoot(
            RectTransform anchor,
            RectTransform visualRoot,
            out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            var current = anchor;
            while (current != null && current != visualRoot)
            {
                localPosition += current.anchoredPosition;
                current = current.parent as RectTransform;
            }

            return current == visualRoot;
        }
    }
}
