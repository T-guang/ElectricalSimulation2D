using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class BlueprintController : MonoBehaviour
    {
        private const float PageMargin = 28f;
        private const float CardWidth = 446f;
        private const float CardHeight = 336f;
        private const float CardGap = 27f;

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

        private void ApplyTheme()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = MainUiTheme.Hex("F8FBFF");

            ApplyFilterButtonMetrics();
            ApplyFilterAreaLayout();

            foreach (var card in blueprintCards)
            {
                if (card == null) continue;

                var rect = card.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(CardWidth, CardHeight);
                }
                
                var images = card.GetComponentsInChildren<Image>(true);
                var cardBg = card.GetComponent<Image>();
                if (cardBg == null && images.Length > 0) cardBg = images[0];

                if (cardBg != null)
                {
                    cardBg.sprite = UiThemeTokens.GetRoundedSprite(16);
                    cardBg.type = Image.Type.Sliced;
                    cardBg.color = MainUiTheme.Hex("FAFCFF");
                }

                var outline = card.GetComponent<Outline>() ?? card.AddComponent<Outline>();
                outline.effectColor = MainUiTheme.Hex("D2D2D2");
                outline.effectDistance = new Vector2(1f, -1f);
                
                if (card.GetComponent<UnityEngine.UI.Shadow>() == null)
                {
                    var shadow = card.AddComponent<UnityEngine.UI.Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, 0.04f);
                    shadow.effectDistance = new Vector2(0, -4);
                }

                var btn = card.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    var btnBg = btn.GetComponent<Image>();
                    if (btnBg != null && btnBg != cardBg)
                    {
                        btnBg.sprite = UiThemeTokens.GetRoundedSprite(8);
                        btnBg.type = Image.Type.Sliced;
                        btnBg.color = MainUiTheme.PrimaryBlue;
                        
                        var btnText = btn.GetComponentInChildren<Text>();
                        if (btnText != null)
                        {
                            btnText.font = MainUiTheme.BodyFont;
                            btnText.fontSize = 16;
                            btnText.fontStyle = FontStyle.Bold;
                            btnText.color = Color.white;
                            btnText.resizeTextForBestFit = false;
                        }
                    }
                }

                ApplyBlueprintCardTextStyle(card);
            }

            if (searchInput != null)
            {
                var searchRect = searchInput.GetComponent<RectTransform>();
                if (searchRect != null)
                {
                    searchRect.sizeDelta = new Vector2(236f, 36f);
                }

                var searchBg = searchInput.GetComponent<Image>();
                if (searchBg != null)
                {
                    searchBg.sprite = UiThemeTokens.GetRoundedSprite(16);
                    searchBg.type = Image.Type.Sliced;
                    searchBg.color = Color.white;
                }

                if (searchInput.textComponent != null)
                {
                    searchInput.textComponent.font = MainUiTheme.BodyFont;
                    searchInput.textComponent.fontSize = 16;
                    searchInput.textComponent.color = MainUiTheme.Hex("464646");
                }
            }

            StyleModal(previewModal);
            StyleModal(referencePanel);
            StyleCloseButton(previewCloseButton);
            StyleCloseButton(referenceCloseButton);

            if (configureButton != null)
            {
                var cfgBg = configureButton.GetComponent<Image>();
                if (cfgBg != null)
                {
                    cfgBg.sprite = UiThemeTokens.GetRoundedSprite(8);
                    cfgBg.type = Image.Type.Sliced;
                    cfgBg.color = MainUiTheme.PrimaryBlue;
                }
                var txt = configureButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.font = MainUiTheme.BodyFont;
                    txt.fontSize = 20;
                    txt.color = Color.white;
                    txt.resizeTextForBestFit = false;
                }
            }
        }

        private void ApplyFilterButtonMetrics()
        {
            for (var i = 0; i < categoryButtons.Count; i++)
            {
                var rect = categoryButtons[i] != null ? categoryButtons[i].GetComponent<RectTransform>() : null;
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(175f, 36f);
                }
            }

            for (var i = 0; i < difficultyButtons.Count; i++)
            {
                var rect = difficultyButtons[i] != null ? difficultyButtons[i].GetComponent<RectTransform>() : null;
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(138f, 36f);
                }
            }
        }

        private void ApplyFilterAreaLayout()
        {
            const float rowLabelX = 34f;
            const float buttonStartX = 114f;
            const float firstRowY = -24f;
            const float secondRowY = -74f;
            const float rowHeight = 36f;

            EnsureFilterRowLabel("BlueprintTypeRowLabel", "\u7535\u8def\u7c7b\u578b", rowLabelX, firstRowY, rowHeight);
            EnsureFilterRowLabel("BlueprintDifficultyRowLabel", "\u96be\u5ea6", rowLabelX, secondRowY, rowHeight);

            for (var i = 0; i < categoryButtons.Count; i++)
            {
                PositionFilterButton(categoryButtons[i], buttonStartX + i * 184f, firstRowY, 175f, rowHeight);
            }

            for (var i = 0; i < difficultyButtons.Count; i++)
            {
                PositionFilterButton(difficultyButtons[i], buttonStartX + i * 146f, secondRowY, 138f, rowHeight);
            }

            if (searchInput != null)
            {
                var searchRect = searchInput.GetComponent<RectTransform>();
                if (searchRect != null)
                {
                    searchRect.anchorMin = new Vector2(1f, 1f);
                    searchRect.anchorMax = new Vector2(1f, 1f);
                    searchRect.pivot = new Vector2(1f, 1f);
                    searchRect.anchoredPosition = new Vector2(-28f, secondRowY);
                    searchRect.sizeDelta = new Vector2(236f, 36f);
                }

                var placeholder = searchInput.placeholder as Text;
                if (placeholder != null)
                {
                    placeholder.color = MainUiTheme.Hex("94A3B8");
                    placeholder.font = MainUiTheme.BodyFont;
                    placeholder.fontSize = 15;
                }
            }
        }

        private void EnsureFilterRowLabel(string name, string text, float x, float y, float height)
        {
            var existing = transform.Find(name);
            Text label;
            RectTransform rect;
            if (existing == null)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(transform, false);
                rect = obj.GetComponent<RectTransform>();
                label = obj.GetComponent<Text>();
            }
            else
            {
                rect = existing as RectTransform;
                label = existing.GetComponent<Text>();
            }

            if (rect == null || label == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(76f, height);

            label.text = text;
            label.font = MainUiTheme.BodyFont;
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.color = MainUiTheme.Hex("64748B");
            label.alignment = TextAnchor.MiddleLeft;
            label.resizeTextForBestFit = false;
        }

        private static void PositionFilterButton(Button button, float x, float y, float width, float height)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void ApplyBlueprintCardTextStyle(GameObject card)
        {
            var texts = card.GetComponentsInChildren<Text>(true);
            string category = null;
            string difficulty = null;
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                if (IsGeneratedCardTagText(text))
                {
                    continue;
                }

                text.font = MainUiTheme.BodyFont;
                text.resizeTextForBestFit = false;
                if (IsActionLabel(text.text))
                {
                    text.fontSize = 16;
                    text.fontStyle = FontStyle.Bold;
                    text.color = Color.white;
                }
                else if (IsCategoryLabel(text.text))
                {
                    category = text.text;
                    text.gameObject.SetActive(false);
                }
                else if (IsDifficultyLabel(text.text))
                {
                    difficulty = text.text;
                    text.gameObject.SetActive(false);
                }
                else if (!string.IsNullOrWhiteSpace(text.text))
                {
                    text.fontSize = 20;
                    text.fontStyle = FontStyle.Bold;
                    text.color = MainUiTheme.Hex("1F2937");
                }
            }

            EnsureCardTag(card, "BlueprintTypeTag", string.IsNullOrWhiteSpace(category) ? "\u5de5\u4e1a\u7535\u8def" : category, new Vector2(16f, 18f), new Vector2(86f, 28f), MainUiTheme.Hex("DBEAFE"), MainUiTheme.PrimaryBlue);
            EnsureCardTag(card, "BlueprintDifficultyTag", string.IsNullOrWhiteSpace(difficulty) ? "\u521d\u7ea7" : difficulty, new Vector2(112f, 18f), new Vector2(68f, 28f), ResolveDifficultyTagFill(difficulty), Color.white);
            PositionCardActionButton(card);
        }

        private static bool IsGeneratedCardTagText(Text text)
        {
            var parent = text != null ? text.transform.parent : null;
            return parent != null && (parent.name == "BlueprintTypeTag" || parent.name == "BlueprintDifficultyTag");
        }

        private static void EnsureCardTag(GameObject card, string name, string text, Vector2 position, Vector2 size, Color fill, Color textColor)
        {
            var cardRect = card != null ? card.GetComponent<RectTransform>() : null;
            if (cardRect == null)
            {
                return;
            }

            var tag = card.transform.Find(name) as RectTransform;
            Text label;
            Image bg;
            if (tag == null)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(card.transform, false);
                tag = obj.GetComponent<RectTransform>();
                bg = obj.GetComponent<Image>();

                var labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(obj.transform, false);
                label = labelObj.GetComponent<Text>();
                var labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
            else
            {
                bg = tag.GetComponent<Image>() ?? tag.gameObject.AddComponent<Image>();
                label = tag.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    var labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    labelObj.transform.SetParent(tag, false);
                    label = labelObj.GetComponent<Text>();
                    var labelRect = labelObj.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }

            tag.anchorMin = new Vector2(0f, 0f);
            tag.anchorMax = new Vector2(0f, 0f);
            tag.pivot = new Vector2(0f, 0f);
            tag.anchoredPosition = position;
            tag.sizeDelta = size;
            tag.SetAsLastSibling();

            bg.sprite = UiThemeTokens.GetRoundedSprite(8);
            bg.type = Image.Type.Sliced;
            bg.color = fill;
            bg.raycastTarget = false;

            label.text = NormalizeTagText(text);
            label.font = MainUiTheme.BodyFont;
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.color = textColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = false;
            label.raycastTarget = false;
        }

        private static void PositionCardActionButton(GameObject card)
        {
            var button = card != null ? card.GetComponentInChildren<Button>(true) : null;
            var rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-16f, 16f);
            rect.sizeDelta = new Vector2(116f, 32f);
        }

        private static Color ResolveDifficultyTagFill(string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                return MainUiTheme.Hex("0BB148");
            }

            if (difficulty.Contains("\u9ad8") || difficulty.Contains("楂")) return MainUiTheme.Hex("EF4444");
            if (difficulty.Contains("\u4e2d") || difficulty.Contains("\u8fdb") || difficulty.Contains("涓")) return MainUiTheme.Hex("F59E0B");
            return MainUiTheme.Hex("0BB148");
        }

        private static string NormalizeTagText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace("\u56fe\u7eb8", string.Empty).Trim();
        }

        private void StyleModal(GameObject modal)
        {
            if (modal == null) return;
            var modalImages = modal.GetComponentsInChildren<Image>(true);
            foreach (var img in modalImages)
            {
                if (img.color.r > 0.9f && img.color.g > 0.9f && img.color.b > 0.9f && img.rectTransform.rect.width > 200)
                {
                    img.sprite = UiThemeTokens.GetRoundedSprite(16);
                    img.type = Image.Type.Sliced;
                    img.color = UiThemeTokens.CardBackground;
                    
                    if (img.gameObject.GetComponent<UnityEngine.UI.Shadow>() == null)
                    {
                        var shadow = img.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                        shadow.effectColor = new Color(0, 0, 0, 0.15f);
                        shadow.effectDistance = new Vector2(0, -8);
                    }
                }
            }
        }

        private void StyleCloseButton(Button closeBtn)
        {
            if (closeBtn == null) return;
            
            var outline = closeBtn.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) Destroy(outline);

            var closeBg = closeBtn.GetComponent<Image>();
            if (closeBg != null)
            {
                closeBg.sprite = UiThemeTokens.GetRoundedSprite(16);
                closeBg.type = Image.Type.Sliced;
                closeBg.color = new Color(0.94f, 0.95f, 0.97f);
            }
            var closeText = closeBtn.GetComponentInChildren<Text>();
            if (closeText != null)
            {
                closeText.color = UiThemeTokens.TextMuted;
                closeText.text = "✕";
                closeText.fontSize = 20;
            }
        }

        private void Awake()
        {

            // Scene-authored cards are legacy gallery placeholders. Keep one as
            // the clone template, but exclude every static card from filtering
            // and counts so the gallery is driven only by template_catalog.json.
            for (var i = 0; i < blueprintButtons.Count; i++)
            {
                dynamicTemplates.Add(null);
                if (i < blueprintCategories.Count)
                {
                    blueprintCategories[i] = -1;
                }

                if (i < blueprintDifficulties.Count)
                {
                    blueprintDifficulties[i] = -1;
                }

                if (i < blueprintCards.Count && blueprintCards[i] != null)
                {
                    blueprintCards[i].SetActive(false);
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
            ApplyTheme();
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
                    var availableWidth = cardContent != null && cardContent.rect.width > 1f
                        ? cardContent.rect.width
                        : Mathf.Max(960f, Screen.width - PageMargin * 2f);
                    var columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth - PageMargin + CardGap) / (CardWidth + CardGap)));
                    var row = visibleIndex / columns;
                    var col = visibleIndex % columns;
                    rect.sizeDelta = new Vector2(CardWidth, CardHeight);
                    rect.anchoredPosition = new Vector2(PageMargin + col * (CardWidth + CardGap), -PageMargin - row * (CardHeight + CardGap));
                }

                visibleIndex++;
            }

            if (cardContent != null)
            {
                var availableWidth = cardContent.rect.width > 1f ? cardContent.rect.width : Mathf.Max(960f, Screen.width - PageMargin * 2f);
                var columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth - PageMargin + CardGap) / (CardWidth + CardGap)));
                var rows = Mathf.CeilToInt(visibleIndex / (float)columns);
                cardContent.sizeDelta = new Vector2(0f, Mathf.Max(720f, PageMargin + rows * (CardHeight + CardGap)));
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
                image.sprite = UiThemeTokens.GetRoundedSprite(16);
                image.type = Image.Type.Sliced;
                image.color = ResolveFilterFill(button, active);
            }

            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = ResolveFilterBorder(button, active);
            outline.effectDistance = new Vector2(1f, -1f);

            var label = button.GetComponentInChildren<Text>();
            var countText = string.Empty;
            if (label != null)
            {
                countText = ExtractCountText(label.text, out var displayText);
                label.text = displayText;
                label.font = MainUiTheme.BodyFont;
                label.fontSize = 16;
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = false;
                label.color = ResolveFilterTextColor(button, active);
                label.alignment = TextAnchor.MiddleCenter;

                var labelRect = label.GetComponent<RectTransform>();
                if (labelRect != null)
                {
                    labelRect.offsetMin = new Vector2(10f, labelRect.offsetMin.y);
                    labelRect.offsetMax = new Vector2(string.IsNullOrEmpty(countText) ? -10f : -34f, labelRect.offsetMax.y);
                }
            }

            ApplyCountBadge(button, countText, active);

            if (label != null)
            {
                if (image != null)
                {
                    image.color = ResolveFilterFillByDisplayText(label.text, active);
                }

                outline.effectColor = ResolveFilterTextColorByDisplayText(label.text, active);
                label.color = ResolveFilterTextColorByDisplayText(label.text, active);
            }
        }

        private static string ExtractCountText(string raw, out string displayText)
        {
            displayText = raw;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var open = raw.LastIndexOf('(');
            var close = raw.LastIndexOf(')');
            if (open < 0 || close <= open)
            {
                open = raw.LastIndexOf('\uff08');
                close = raw.LastIndexOf('\uff09');
            }

            if (open < 0 || close <= open)
            {
                return string.Empty;
            }

            displayText = raw.Substring(0, open).Trim();
            return raw.Substring(open + 1, close - open - 1).Trim();
        }

        private static void ApplyCountBadge(Button button, string countText, bool active)
        {
            if (button == null)
            {
                return;
            }

            var badge = button.transform.Find("CountBadge") as RectTransform;
            if (string.IsNullOrWhiteSpace(countText))
            {
                if (badge != null)
                {
                    badge.gameObject.SetActive(false);
                }
                return;
            }

            Text badgeText;
            Image badgeBg;
            if (badge == null)
            {
                var obj = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(button.transform, false);
                badge = obj.GetComponent<RectTransform>();
                badgeBg = obj.GetComponent<Image>();

                var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(obj.transform, false);
                badgeText = textObj.GetComponent<Text>();
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            else
            {
                badgeBg = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
                badgeText = badge.GetComponentInChildren<Text>(true);
            }

            badge.gameObject.SetActive(true);
            badge.anchorMin = new Vector2(1f, 0.5f);
            badge.anchorMax = new Vector2(1f, 0.5f);
            badge.pivot = new Vector2(1f, 0.5f);
            badge.anchoredPosition = new Vector2(-8f, 0f);
            badge.sizeDelta = new Vector2(24f, 24f);
            badge.SetAsLastSibling();

            badgeBg.sprite = UiThemeTokens.GetRoundedSprite(12);
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = active ? new Color(1f, 1f, 1f, 0.65f) : MainUiTheme.Hex("BFDAFC");
            badgeBg.raycastTarget = false;

            if (badgeText != null)
            {
                badgeText.text = countText;
                badgeText.font = MainUiTheme.BodyFont;
                badgeText.fontSize = 12;
                badgeText.fontStyle = FontStyle.Bold;
                var buttonLabel = button.GetComponentInChildren<Text>() != null ? button.GetComponentInChildren<Text>().text : string.Empty;
                badgeText.color = active ? ResolveFilterTextColorByDisplayText(buttonLabel, true) : MainUiTheme.Hex("1F2937");
                badgeText.alignment = TextAnchor.MiddleCenter;
                badgeText.resizeTextForBestFit = false;
                badgeText.raycastTarget = false;
            }
        }

        private static Color ResolveFilterFillByDisplayText(string text, bool active)
        {
            if (!active)
            {
                return Color.white;
            }

            if (IsPrimaryDifficultyDisplayText(text)) return MainUiTheme.Hex("DCFCE7");
            if (IsMiddleDifficultyDisplayText(text)) return MainUiTheme.Hex("FEF3C7");
            if (IsAdvancedDifficultyDisplayText(text)) return MainUiTheme.Hex("FEE2E2");
            return MainUiTheme.Hex("DBEAFE");
        }

        private static Color ResolveFilterTextColorByDisplayText(string text, bool active)
        {
            if (!active)
            {
                return MainUiTheme.Hex("464646");
            }

            if (IsPrimaryDifficultyDisplayText(text)) return MainUiTheme.Hex("0BB148");
            if (IsMiddleDifficultyDisplayText(text)) return MainUiTheme.Hex("F59E0B");
            if (IsAdvancedDifficultyDisplayText(text)) return MainUiTheme.Hex("EF4444");
            return MainUiTheme.PrimaryBlue;
        }

        private static bool IsPrimaryDifficultyDisplayText(string text)
        {
            return !string.IsNullOrEmpty(text) && (text.Contains("\u521d\u7ea7") || text.Contains("\u5165\u95e8") || text.Contains("鍒濈骇"));
        }

        private static bool IsMiddleDifficultyDisplayText(string text)
        {
            return !string.IsNullOrEmpty(text) && (text.Contains("\u4e2d\u7ea7") || text.Contains("\u8fdb\u9636") || text.Contains("涓骇"));
        }

        private static bool IsAdvancedDifficultyDisplayText(string text)
        {
            return !string.IsNullOrEmpty(text) && (text.Contains("\u9ad8\u7ea7") || text.Contains("楂樼骇"));
        }

        private static Color ResolveFilterFill(Button button, bool active)
        {
            if (!active)
            {
                return Color.white;
            }

            var text = button != null && button.GetComponentInChildren<Text>() != null ? button.GetComponentInChildren<Text>().text : string.Empty;
            if (text.Contains("初级")) return MainUiTheme.Hex("DCFCE7");
            if (text.Contains("中级")) return MainUiTheme.Hex("FEF3C7");
            if (text.Contains("高级")) return MainUiTheme.Hex("FEE2E2");
            return MainUiTheme.Hex("DBEAFE");
        }

        private static Color ResolveFilterBorder(Button button, bool active)
        {
            if (!active)
            {
                return MainUiTheme.Hex("D2D2D2");
            }

            return ResolveFilterTextColor(button, true);
        }

        private static Color ResolveFilterTextColor(Button button, bool active)
        {
            if (!active)
            {
                return MainUiTheme.Hex("464646");
            }

            var text = button != null && button.GetComponentInChildren<Text>() != null ? button.GetComponentInChildren<Text>().text : string.Empty;
            if (text.Contains("初级")) return MainUiTheme.Hex("0BB148");
            if (text.Contains("中级")) return MainUiTheme.Hex("F59E0B");
            if (text.Contains("高级")) return MainUiTheme.Hex("EF4444");
            return MainUiTheme.PrimaryBlue;
        }
    }
}


