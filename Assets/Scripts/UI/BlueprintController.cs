using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class BlueprintController : MonoBehaviour
    {
        [SerializeField] private TopNavigationController navigation;
        [SerializeField] private List<Button> blueprintButtons = new List<Button>();
        [SerializeField] private List<GameObject> blueprintCards = new List<GameObject>();
        [SerializeField] private List<string> blueprintNames = new List<string>();
        [SerializeField] private List<Sprite> blueprintSprites = new List<Sprite>();
        [SerializeField] private List<string> blueprintRecommendations = new List<string>();
        [SerializeField] private List<int> blueprintCategories = new List<int>();
        [SerializeField] private List<int> blueprintDifficulties = new List<int>();
        [SerializeField] private List<Button> categoryButtons = new List<Button>();
        [SerializeField] private List<Button> difficultyButtons = new List<Button>();
        [SerializeField] private InputField searchInput;
        [SerializeField] private RectTransform cardContent;
        [SerializeField] private GameObject previewModal;
        [SerializeField] private Text previewTitle;
        [SerializeField] private Image previewImage;
        [SerializeField] private Button previewCloseButton;
        [SerializeField] private Button previewCancelButton;
        [SerializeField] private Button configureButton;
        [SerializeField] private GameObject referencePanel;
        [SerializeField] private Text referenceTitle;
        [SerializeField] private Image referenceImage;
        [SerializeField] private Text referenceRecommendations;
        [SerializeField] private Button referenceCloseButton;

        private readonly List<ElectricalSim.Templates.CircuitTemplateCatalogItemDto> dynamicTemplates = new List<ElectricalSim.Templates.CircuitTemplateCatalogItemDto>();
        private readonly List<ElectricalSim.Templates.CircuitTemplateCatalogItemDto> catalogTemplates = new List<ElectricalSim.Templates.CircuitTemplateCatalogItemDto>();

        private int selectedIndex;
        private int activeCategory;
        private int activeDifficulty = -1;

        private void Awake()
        {
            // Initialize dynamicTemplates with nulls for static items
            for (var i = 0; i < blueprintButtons.Count; i++)
            {
                dynamicTemplates.Add(null);
                if (i < blueprintCategories.Count && blueprintCategories[i] == 1)
                {
                    blueprintCategories[i] = -1; // Hide existing static family circuits
                    if (blueprintCards[i] != null) blueprintCards[i].SetActive(false);
                }
            }

            var catalogJson = Resources.Load<TextAsset>("Blueprints/Templates/template_catalog");
            if (catalogJson != null)
            {
                var catalog = JsonUtility.FromJson<ElectricalSim.Templates.CircuitTemplateCatalogDto>(catalogJson.text);
                if (catalog != null && catalog.templates != null)
                {
                    catalogTemplates.Clear();
                    catalogTemplates.AddRange(catalog.templates);
                }

                if (catalog != null && catalog.templates != null && blueprintCards.Count > 0)
                {
                    var templateCard = blueprintCards[0];
                    foreach (var item in catalog.templates)
                    {
                        if (item.category == "\u5bb6\u5ead\u7535\u8def" || item.category == "\u5de5\u4e1a\u7535\u8def")
                        {
                            var newCard = Instantiate(templateCard, cardContent);
                            var newIndex = blueprintButtons.Count;
                            var categoryLabel = item.category == "\u5de5\u4e1a\u7535\u8def" ? "\u5de5\u4e1a\u7535\u8def" : "\u5bb6\u5ead\u7535\u8def";
                            
                            ApplyDynamicCardTexts(newCard, item, categoryLabel);

                            Sprite sprite = null;
                            if (!string.IsNullOrEmpty(item.thumbnailPath))
                            {
                                sprite = Resources.Load<Sprite>(item.thumbnailPath);
                            }

                            if (sprite != null && blueprintSprites.Count > 0)
                            {
                                var images = newCard.GetComponentsInChildren<Image>(true);
                                foreach (var img in images)
                                {
                                    if (img.sprite == blueprintSprites[0])
                                    {
                                        img.sprite = sprite;
                                    }
                                }
                            }

                            var btn = newCard.GetComponentInChildren<Button>(true);
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(() => OpenPreview(newIndex));

                            blueprintButtons.Add(btn);
                            blueprintCards.Add(newCard);
                            blueprintNames.Add(item.templateName);
                            blueprintSprites.Add(sprite);
                            blueprintCategories.Add(item.category == "\u5de5\u4e1a\u7535\u8def" ? 0 : 1);

                            int diff = 0;
                            if (!string.IsNullOrEmpty(item.difficulty) && (item.difficulty.Contains("\u4e2d") || item.difficulty.Contains("\u8fdb\u9636"))) diff = 1;
                            if (!string.IsNullOrEmpty(item.difficulty) && item.difficulty.Contains("\u9ad8")) diff = 2;
                            blueprintDifficulties.Add(diff);
                            blueprintRecommendations.Add(item.description);
                            dynamicTemplates.Add(item);
                        }
                    }
                }
            }

            for (var i = 0; i < blueprintButtons.Count; i++)
            {
                var index = i;
                blueprintButtons[i].onClick.AddListener(() => OpenPreview(index));
            }

            previewCloseButton?.onClick.AddListener(ClosePreview);
            previewCancelButton?.onClick.AddListener(ClosePreview);
            configureButton?.onClick.AddListener(EnterConfiguration);
            referenceCloseButton?.onClick.AddListener(HideReference);
            for (var i = 0; i < categoryButtons.Count; i++)
            {
                var category = i;
                categoryButtons[i].onClick.AddListener(() => SetCategory(category));
            }

            for (var i = 0; i < difficultyButtons.Count; i++)
            {
                var difficulty = i - 1;
                difficultyButtons[i].onClick.AddListener(() => SetDifficulty(difficulty));
            }

            if (categoryButtons != null)
            {
                int cat0Count = 0;
                int cat1Count = 0;
                for (int i = 0; i < blueprintCategories.Count; i++)
                {
                    if (blueprintCategories[i] == 0) cat0Count++;
                    if (blueprintCategories[i] == 1) cat1Count++;
                }
                
                if (categoryButtons.Count > 0)
                {
                    SetButtonText(categoryButtons[0], $"\u5de5\u4e1a\u7535\u8def\u56fe\u7eb8({cat0Count})");
                }
                if (categoryButtons.Count > 1)
                {
                    SetButtonText(categoryButtons[1], $"\u5bb6\u5ead\u7535\u8def\u56fe\u7eb8({cat1Count})");
                }
            }

            searchInput?.onValueChanged.AddListener(_ => ApplyFilter());
            ClosePreview();
            HideReference();
            ApplyFilter();
        }

        private void OpenPreview(int index)
        {
            selectedIndex = index;
            if (previewModal != null)
            {
                previewModal.SetActive(true);
            }

            ApplyBlueprint(index, previewTitle, previewImage);
        }

        private void ClosePreview()
        {
            if (previewModal != null)
            {
                previewModal.SetActive(false);
            }
        }

        private void EnterConfiguration()
        {
            var templateItem = ResolveSelectedTemplateItem();
            if (templateItem != null)
            {
                var practiceController = ElectricalSim.Practice.PracticeSessionController.Instance;
                if (practiceController != null)
                {
                    practiceController.StartPractice(templateItem, ClosePreview);
                    return;
                }
            }

            EnterConfigurationInternal();
        }

        private ElectricalSim.Templates.CircuitTemplateCatalogItemDto ResolveSelectedTemplateItem()
        {
            if (selectedIndex >= 0 && selectedIndex < dynamicTemplates.Count && dynamicTemplates[selectedIndex] != null)
            {
                return dynamicTemplates[selectedIndex];
            }

            var candidates = new List<string>();
            AddTemplateNameCandidate(candidates, selectedIndex >= 0 && selectedIndex < blueprintNames.Count ? blueprintNames[selectedIndex] : string.Empty);

            if (selectedIndex >= 0 && selectedIndex < blueprintCards.Count && blueprintCards[selectedIndex] != null)
            {
                var labels = blueprintCards[selectedIndex].GetComponentsInChildren<Text>(true);
                for (var i = 0; i < labels.Length; i++)
                {
                    AddTemplateNameCandidate(candidates, labels[i] != null ? labels[i].text : string.Empty);
                }
            }

            for (var i = 0; i < catalogTemplates.Count; i++)
            {
                var item = catalogTemplates[i];
                if (item == null || string.IsNullOrWhiteSpace(item.templateName))
                {
                    continue;
                }

                for (var c = 0; c < candidates.Count; c++)
                {
                    var candidate = candidates[c];
                    if (string.Equals(item.templateName, candidate, System.StringComparison.OrdinalIgnoreCase)
                        || item.templateName.Contains(candidate)
                        || candidate.Contains(item.templateName))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private static void AddTemplateNameCandidate(List<string> candidates, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var candidate = value.Trim();
            if (candidate.Length < 3)
            {
                return;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], candidate, System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            candidates.Add(candidate);
        }

        private static void ApplyDynamicCardTexts(GameObject card, ElectricalSim.Templates.CircuitTemplateCatalogItemDto item, string categoryLabel)
        {
            if (card == null || item == null)
            {
                return;
            }

            var texts = card.GetComponentsInChildren<Text>(true);
            var titleAssigned = false;
            var categoryAssigned = false;
            var difficultyAssigned = false;

            foreach (var text in texts)
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                var original = text.text.Trim();
                if (IsActionLabel(original))
                {
                    text.text = "\u8fdb\u5165\u7ec3\u4e60";
                    continue;
                }

                if (!categoryAssigned && IsCategoryLabel(original))
                {
                    text.text = categoryLabel;
                    categoryAssigned = true;
                    continue;
                }

                if (!difficultyAssigned && IsDifficultyLabel(original))
                {
                    text.text = item.difficulty;
                    difficultyAssigned = true;
                    continue;
                }

                if (!titleAssigned)
                {
                    text.text = item.templateName;
                    titleAssigned = true;
                }
            }
        }

        private static bool IsActionLabel(string text)
        {
            return text.Contains("\u7ec3\u4e60") || text.Contains("\u8fdb\u5165");
        }

        private static bool IsCategoryLabel(string text)
        {
            return text == "\u5bb6\u5ead"
                   || text == "\u5de5\u4e1a"
                   || text == "\u5bb6\u5ead\u7535\u8def"
                   || text == "\u5de5\u4e1a\u7535\u8def"
                   || text == "\u5bb6\u5ead\u7535\u8def\u56fe\u7eb8"
                   || text == "\u5de5\u4e1a\u7535\u8def\u56fe\u7eb8";
        }

        private static bool IsDifficultyLabel(string text)
        {
            return text == "\u521d\u7ea7"
                   || text == "\u4e2d\u7ea7"
                   || text == "\u9ad8\u7ea7"
                   || text == "\u5165\u95e8"
                   || text == "\u8fdb\u9636"
                   || text == "\u521d\u7ea7\u56fe\u7eb8"
                   || text == "\u4e2d\u7ea7\u56fe\u7eb8"
                   || text == "\u9ad8\u7ea7\u56fe\u7eb8";
        }

        private void EnterConfigurationInternal()
        {
            ClosePreview();
            navigation?.SelectTab(0);

            if (referencePanel != null)
            {
                referencePanel.SetActive(true);
            }

            ApplyBlueprint(selectedIndex, referenceTitle, referenceImage);
            ApplyRecommendation(selectedIndex);
        }

        private void HideReference()
        {
            if (referencePanel != null)
            {
                referencePanel.SetActive(false);
            }
        }

        private void ApplyBlueprint(int index, Text title, Image image)
        {
            if (title != null)
            {
                title.text = index >= 0 && index < blueprintNames.Count ? blueprintNames[index] : string.Empty;
            }

            if (image != null)
            {
                image.sprite = index >= 0 && index < blueprintSprites.Count ? blueprintSprites[index] : null;
                image.color = image.sprite != null ? Color.white : new Color(0.96f, 0.98f, 1f);
                image.preserveAspect = true;
            }
        }

        private void ApplyRecommendation(int index)
        {
            if (referenceRecommendations == null)
            {
                return;
            }

            var recommendation = index >= 0 && index < blueprintRecommendations.Count ? blueprintRecommendations[index] : string.Empty;
            referenceRecommendations.text = string.IsNullOrWhiteSpace(recommendation)
                ? "\u63a8\u8350\u5143\u4ef6\uff1a\u6682\u65e0"
                : "\u63a8\u8350\u5143\u4ef6\uff1a" + recommendation.Replace("|", " / ");
        }

        private void SetCategory(int category)
        {
            activeCategory = category;
            activeDifficulty = -1;
            ApplyFilter();
        }

        private void SetDifficulty(int difficulty)
        {
            activeDifficulty = difficulty;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var visibleIndex = 0;
            for (var i = 0; i < blueprintCards.Count; i++)
            {
                var categoryMatches = i < blueprintCategories.Count && blueprintCategories[i] == activeCategory;
                var difficultyMatches = activeDifficulty < 0 || i < blueprintDifficulties.Count && blueprintDifficulties[i] == activeDifficulty;
                var searchMatches = MatchesSearch(i);
                var visible = categoryMatches && difficultyMatches && searchMatches;
                blueprintCards[i].SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var rect = blueprintCards[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    var row = visibleIndex / 4;
                    var col = visibleIndex % 4;
                    rect.anchoredPosition = new Vector2(32f + col * 470f, -26f - row * 334f);
                }

                visibleIndex++;
            }

            if (cardContent != null)
            {
                var rows = Mathf.CeilToInt(visibleIndex / 4f);
                cardContent.sizeDelta = new Vector2(0f, Mathf.Max(720f, 32f + rows * 334f));
            }

            RefreshButtonStates();
        }

        private bool MatchesSearch(int index)
        {
            if (searchInput == null || string.IsNullOrWhiteSpace(searchInput.text))
            {
                return true;
            }

            var name = index >= 0 && index < blueprintNames.Count ? blueprintNames[index] : string.Empty;
            return name.IndexOf(searchInput.text.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshButtonStates()
        {
            RefreshDifficultyLabels();

            for (var i = 0; i < categoryButtons.Count; i++)
            {
                SetButtonActive(categoryButtons[i], i == activeCategory);
            }

            for (var i = 0; i < difficultyButtons.Count; i++)
            {
                SetButtonActive(difficultyButtons[i], i - 1 == activeDifficulty);
            }
        }

        private void RefreshDifficultyLabels()
        {
            if (difficultyButtons.Count < 4)
            {
                return;
            }

            SetButtonText(difficultyButtons[0], $"\u5168\u90e8\u56fe\u7eb8({CountByDifficulty(-1)})");
            SetButtonText(difficultyButtons[1], $"\u521d\u7ea7\u56fe\u7eb8({CountByDifficulty(0)})");
            SetButtonText(difficultyButtons[2], $"\u4e2d\u7ea7\u56fe\u7eb8({CountByDifficulty(1)})");
            SetButtonText(difficultyButtons[3], $"\u9ad8\u7ea7\u56fe\u7eb8({CountByDifficulty(2)})");
        }

        private int CountByDifficulty(int difficulty)
        {
            var count = 0;
            for (var i = 0; i < blueprintCategories.Count; i++)
            {
                if (blueprintCategories[i] != activeCategory)
                {
                    continue;
                }

                if (difficulty < 0 || i < blueprintDifficulties.Count && blueprintDifficulties[i] == difficulty)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetButtonActive(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.12f, 0.45f, 1f) : new Color(0.94f, 0.96f, 0.98f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = active ? Color.white : new Color(0.26f, 0.34f, 0.45f);
            }
        }
    }
}


