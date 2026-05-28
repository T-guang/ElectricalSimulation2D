using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class EncyclopediaController : MonoBehaviour
    {
        [SerializeField] private List<Button> categoryButtons = new List<Button>();
        [SerializeField] private List<Text> categoryLabels = new List<Text>();
        [SerializeField] private List<RectTransform> categorySections = new List<RectTransform>();
        [SerializeField] private RectTransform content;

        private void Awake()
        {
            for (var i = 0; i < categoryButtons.Count; i++)
            {
                var index = i;
                categoryButtons[i].onClick.AddListener(() => SelectCategory(index));
            }

            SelectCategory(0);
        }

        private void SelectCategory(int index)
        {
            var y = -4f;
            for (var i = 0; i < categorySections.Count; i++)
            {
                var active = i == index;
                if (categorySections[i] != null)
                {
                    categorySections[i].gameObject.SetActive(active);
                    if (active)
                    {
                        categorySections[i].anchoredPosition = new Vector2(0f, y);
                        y -= categorySections[i].sizeDelta.y;
                    }
                }

                SetButtonState(i, active);
            }

            if (content != null)
            {
                content.sizeDelta = new Vector2(0f, Mathf.Max(760f, Mathf.Abs(y) + 40f));
                content.anchoredPosition = Vector2.zero;
            }
        }

        private void SetButtonState(int index, bool active)
        {
            if (index >= categoryButtons.Count || categoryButtons[index] == null)
            {
                return;
            }

            var image = categoryButtons[index].GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.89f, 0.94f, 1f) : new Color(0.96f, 0.98f, 1f);
            }

            if (index < categoryLabels.Count && categoryLabels[index] != null)
            {
                categoryLabels[index].color = active ? new Color(0.06f, 0.38f, 0.95f) : new Color(0.18f, 0.24f, 0.32f);
                categoryLabels[index].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
