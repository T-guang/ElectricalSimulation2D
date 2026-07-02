import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Update Constants
content = re.sub(r'private const float CardWidth = 360f;', r'private const float CardWidth = 300f;', content)
content = re.sub(r'private const float CardHeight = 160f;', r'private const float CardHeight = 140f;', content)
content = re.sub(r'private const float CardGapX = 18f;', r'private const float CardGapX = 14f;', content)
content = re.sub(r'private const float CardGapY = 18f;', r'private const float CardGapY = 14f;', content)

# 2. Update CreateListView Constraint
content = re.sub(r'cardGridLayout\.constraintCount = 2;', r'cardGridLayout.constraintCount = 3;', content)

# 3. Update CreateCard
old_create_card = '''        private RectTransform CreateCard(ComponentEncyclopediaEntry entry)
        {
            var card = CreatePanel("ComponentCard_" + entry.DefinitionName, cardContent, Color.white);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            var button = card.gameObject.AddComponent<Button>();
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.91f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            button.onClick.AddListener(() => ShowDetail(entry));

            var image = CreatePanel("ImageArea", card, new Color(0.97f, 0.98f, 1f, 1f));
            SetRect(image, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(104f, 124f));
            var imageComponent = image.GetComponent<Image>();
            var sprite = ResolveIcon(entry.Definition);
            imageComponent.sprite = sprite != null ? sprite : GetFallbackSprite();
            imageComponent.color = sprite != null ? Color.white : new Color(0.86f, 0.90f, 0.96f, 1f);
            imageComponent.preserveAspect = true;
            imageComponent.raycastTarget = false;

            var name = CreateText("Name", card, entry.DisplayName, 18, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            name.alignment = TextAnchor.UpperLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(136f, -18f), new Vector2(-18f, 48f));

            var meta = CreateText("Meta", card, entry.Category + " / " + entry.CircuitType, 13, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f));
            meta.alignment = TextAnchor.UpperLeft;
            SetRect(meta.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(136f, -72f), new Vector2(-18f, 36f));

            var rating = CreateText("Rating", card, BuildBasicSummary(entry), 13, FontStyle.Normal, new Color(0.08f, 0.32f, 0.60f));
            rating.alignment = TextAnchor.UpperLeft;
            rating.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(rating.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(16f, 46f), new Vector2(-16f, 46f));

            var terminals = CreateText("Terminals", card, "端子：" + BuildTerminalSummary(entry.Definition, entry.Terminals), 13, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            terminals.alignment = TextAnchor.UpperLeft;
            terminals.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(terminals.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(16f, 14f), new Vector2(-16f, 30f));

            return card;
        }'''

new_create_card = '''        private RectTransform CreateCard(ComponentEncyclopediaEntry entry)
        {
            var card = CreatePanel("ComponentCard_" + entry.DefinitionName, cardContent, Color.white);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = CardWidth;
            cardLayout.preferredHeight = CardHeight;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.91f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            button.onClick.AddListener(() => ShowDetail(entry));

            var thumbnailArea = CreatePanel("ThumbnailArea", card, new Color(0.97f, 0.98f, 1f, 1f));
            SetRect(thumbnailArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(104f, -24f));
            var mask = thumbnailArea.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var image = CreateRect("ThumbnailImage", thumbnailArea);
            var imageComponent = image.gameObject.AddComponent<Image>();
            var sprite = ResolveIcon(entry.Definition);
            imageComponent.sprite = sprite != null ? sprite : GetFallbackSprite();
            imageComponent.color = sprite != null ? Color.white : new Color(0.86f, 0.90f, 0.96f, 1f);
            imageComponent.preserveAspect = true;
            imageComponent.raycastTarget = false;
            
            SetRect(image, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 108f));

            var infoArea = CreateRect("InfoArea", card);
            SetRect(infoArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            infoArea.offsetMin = new Vector2(124f, 12f);
            infoArea.offsetMax = new Vector2(-12f, -14f);

            var infoLayout = infoArea.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.UpperLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 5f;

            var name = CreateText("NameText", infoArea, entry.DisplayName, 16, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            name.alignment = TextAnchor.UpperLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var nameLayout = name.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 42f;

            var category = CreateText("CategoryText", infoArea, entry.Category, 12, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f));
            category.alignment = TextAnchor.MiddleLeft;
            category.horizontalOverflow = HorizontalWrapMode.Wrap;
            category.verticalOverflow = VerticalWrapMode.Truncate;
            var catLayout = category.gameObject.AddComponent<LayoutElement>();
            catLayout.preferredHeight = 16f;

            var param = CreateText("ParamText", infoArea, BuildBasicSummary(entry), 12, FontStyle.Normal, new Color(0.08f, 0.32f, 0.60f));
            param.alignment = TextAnchor.MiddleLeft;
            param.horizontalOverflow = HorizontalWrapMode.Wrap;
            param.verticalOverflow = VerticalWrapMode.Truncate;
            var paramLayout = param.gameObject.AddComponent<LayoutElement>();
            paramLayout.preferredHeight = 16f;

            var terminals = CreateText("TerminalText", infoArea, BuildTerminalSummary(entry.Definition, entry.Terminals), 12, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            terminals.alignment = TextAnchor.UpperLeft;
            terminals.horizontalOverflow = HorizontalWrapMode.Wrap;
            terminals.verticalOverflow = VerticalWrapMode.Truncate;
            var termLayout = terminals.gameObject.AddComponent<LayoutElement>();
            termLayout.preferredHeight = 32f;

            return card;
        }'''

if old_create_card in content:
    content = content.replace(old_create_card, new_create_card)
    print("CreateCard replaced")
else:
    print("CreateCard not found")

# 4. Update BuildTerminalSummary
old_build_terminal_summary = '''        private static string BuildTerminalSummary(ComponentDefinition definition, string[] fallbackTerminals)
        {
            var parts = new List<string>();
            if (definition != null && definition.terminals != null)
            {
                for (var i = 0; i < definition.terminals.Count; i++)
                {
                    var terminal = definition.terminals[i];
                    if (terminal != null)
                    {
                        parts.Add(string.IsNullOrWhiteSpace(terminal.label) ? terminal.id : terminal.label);
                    }
                }
            }

            if (parts.Count == 0 && fallbackTerminals != null)
            {
                parts.AddRange(fallbackTerminals);
            }

            return parts.Count > 0 ? string.Join("、", parts) : "暂无端子";
        }'''

new_build_terminal_summary = '''        private static string BuildTerminalSummary(ComponentDefinition definition, string[] fallbackTerminals)
        {
            var parts = new List<string>();
            if (definition != null && definition.terminals != null)
            {
                for (var i = 0; i < definition.terminals.Count; i++)
                {
                    var terminal = definition.terminals[i];
                    if (terminal != null)
                    {
                        parts.Add(string.IsNullOrWhiteSpace(terminal.label) ? terminal.id : terminal.label);
                    }
                }
            }

            if (parts.Count == 0 && fallbackTerminals != null)
            {
                parts.AddRange(fallbackTerminals);
            }

            if (parts.Count == 0)
                return "端子：暂无端子";

            if (parts.Count <= 6)
                return "端子：" + string.Join("、", parts);

            var firstSix = new List<string>();
            for (var i = 0; i < 6; i++) firstSix.Add(parts[i]);
            
            return "端子：共 " + parts.Count + " 个，" + string.Join("、", firstSix) + " 等";
        }'''

if old_build_terminal_summary in content:
    content = content.replace(old_build_terminal_summary, new_build_terminal_summary)
    print("BuildTerminalSummary replaced")
else:
    print("BuildTerminalSummary not found")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
