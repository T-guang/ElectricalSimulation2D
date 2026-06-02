using System.Collections.Generic;
using ElectricalSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class PaletteController : MonoBehaviour
    {
        [SerializeField] private InputField searchInput;
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private WorkspaceController workspace;
        [SerializeField] private RectTransform content;
        [SerializeField] private List<RectTransform> sectionTitles = new List<RectTransform>();
        [SerializeField] private List<RectTransform> itemRects = new List<RectTransform>();
        [SerializeField] private List<string> itemNames = new List<string>();
        [SerializeField] private List<int> itemCategories = new List<int>();

        private readonly ComponentCategory[] categoryOrder =
        {
            ComponentCategory.Household,
            ComponentCategory.Industrial,
            ComponentCategory.Measurement
        };

        private void Awake()
        {
            searchInput?.onValueChanged.AddListener(_ => ApplyFilter());
            AddMissingCatalogItems();
            ApplyFilter();
        }

        private void AddMissingCatalogItems()
        {
            if (content == null)
            {
                return;
            }

            if (saveLoadService == null)
            {
                saveLoadService = FindObjectOfType<SaveLoadService>();
            }

            if (workspace == null)
            {
                workspace = FindObjectOfType<WorkspaceController>();
            }

            var catalog = saveLoadService != null ? saveLoadService.Catalog : null;
            if (catalog == null || workspace == null)
            {
                return;
            }

            foreach (var definition in catalog)
            {
                if (definition == null || HasPaletteItem(definition))
                {
                    continue;
                }

                CreateRuntimePaletteItem(definition);
            }
        }

        private bool HasPaletteItem(ComponentDefinition definition)
        {
            for (var i = 0; i < itemRects.Count; i++)
            {
                var rect = itemRects[i];
                if (rect == null)
                {
                    continue;
                }

                var item = rect.GetComponent<PaletteItem>();
                if (item != null && item.Definition == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateRuntimePaletteItem(ComponentDefinition definition)
        {
            var itemObject = new GameObject("Palette_" + definition.name, typeof(RectTransform), typeof(Image), typeof(PaletteItem));
            itemObject.transform.SetParent(content, false);

            var rect = itemObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(122f, 92f);

            var background = itemObject.GetComponent<Image>();
            background.color = new Color(0.94f, 0.96f, 0.99f, 1f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(itemObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 6f);
            labelRect.offsetMax = new Vector2(-6f, -6f);

            var label = labelObject.GetComponent<Text>();
            label.text = definition.displayName;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.05f, 0.08f, 0.14f);
            label.raycastTarget = false;

            var item = itemObject.GetComponent<PaletteItem>();
            item.Initialize(definition, workspace);

            itemRects.Add(rect);
            itemNames.Add(definition.displayName);
            itemCategories.Add((int)definition.category);
        }

        private void ApplyFilter()
        {
            var query = searchInput != null ? searchInput.text.Trim() : string.Empty;
            var y = -10f;

            for (var sectionIndex = 0; sectionIndex < categoryOrder.Length; sectionIndex++)
            {
                var category = (int)categoryOrder[sectionIndex];
                var visibleCount = 0;
                for (var i = 0; i < itemRects.Count; i++)
                {
                    if (i >= itemCategories.Count || itemCategories[i] != category)
                    {
                        continue;
                    }

                    if (Matches(i, query))
                    {
                        visibleCount++;
                    }
                }

                var title = sectionIndex < sectionTitles.Count ? sectionTitles[sectionIndex] : null;
                if (title != null)
                {
                    title.gameObject.SetActive(visibleCount > 0);
                    title.anchoredPosition = new Vector2(20f, y);
                }

                if (visibleCount == 0)
                {
                    for (var i = 0; i < itemRects.Count; i++)
                    {
                        if (i < itemCategories.Count && itemCategories[i] == category)
                        {
                            itemRects[i].gameObject.SetActive(false);
                        }
                    }

                    continue;
                }

                y -= 58f;
                var visibleIndex = 0;
                for (var i = 0; i < itemRects.Count; i++)
                {
                    if (i >= itemCategories.Count || itemCategories[i] != category)
                    {
                        continue;
                    }

                    var visible = Matches(i, query);
                    itemRects[i].gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    var row = visibleIndex / 3;
                    var col = visibleIndex % 3;
                    itemRects[i].anchoredPosition = new Vector2(20f + col * 132f, y - row * 110f);
                    visibleIndex++;
                }

                var rows = Mathf.CeilToInt(visibleCount / 3f);
                y -= rows * 110f + 26f;
            }

            for (var i = 0; i < itemRects.Count; i++)
            {
                var categoryKnown = i < itemCategories.Count;
                if (!categoryKnown)
                {
                    itemRects[i].gameObject.SetActive(false);
                }
            }

            if (content != null)
            {
                content.sizeDelta = new Vector2(0f, Mathf.Max(900f, Mathf.Abs(y) + 24f));
                content.anchoredPosition = Vector2.zero;
            }
        }

        private bool Matches(int index, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var name = index >= 0 && index < itemNames.Count ? itemNames[index] : string.Empty;
            return name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
