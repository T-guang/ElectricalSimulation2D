import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix 1: Sidebar buttons layout
old_sidebar_btn = '''                var buttonRect = CreatePanel("Category_" + category, sidebar, new Color(0.96f, 0.98f, 1f, 1f));
                SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, -72f - i * 48f), new Vector2(-28f, 38f));
                var button = buttonRect.gameObject.AddComponent<Button>();
                var label = CreateText("Text", buttonRect, category, 16, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
                label.alignment = TextAnchor.MiddleLeft;
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(-20f, 0f));'''

new_sidebar_btn = '''                var buttonRect = CreatePanel("Category_" + category, sidebar, new Color(0.96f, 0.98f, 1f, 1f));
                SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f - i * 48f), new Vector2(-32f, 38f));
                var button = buttonRect.gameObject.AddComponent<Button>();
                var label = CreateText("Text", buttonRect, category, 16, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
                label.alignment = TextAnchor.MiddleLeft;
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-16f, 0f));'''

if old_sidebar_btn in content:
    content = content.replace(old_sidebar_btn, new_sidebar_btn)
    print("Sidebar buttons layout updated.")
else:
    print("Sidebar buttons layout NOT FOUND.")

# Fix 2: Remove LayoutElement from Text to prevent massive gaps and overlap
old_info_layout = '''            var infoLayout = infoArea.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.UpperLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 6f;

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
            termLayout.preferredHeight = 32f;'''

new_info_layout = '''            var infoLayout = infoArea.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 6f;

            var name = CreateText("NameText", infoArea, entry.DisplayName, 16, FontStyle.Bold, new Color(0.07f, 0.11f, 0.18f));
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Overflow;

            var category = CreateText("CategoryText", infoArea, entry.Category, 12, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f));
            category.alignment = TextAnchor.MiddleLeft;
            category.horizontalOverflow = HorizontalWrapMode.Wrap;
            category.verticalOverflow = VerticalWrapMode.Overflow;

            var param = CreateText("ParamText", infoArea, BuildBasicSummary(entry), 12, FontStyle.Normal, new Color(0.08f, 0.32f, 0.60f));
            param.alignment = TextAnchor.MiddleLeft;
            param.horizontalOverflow = HorizontalWrapMode.Wrap;
            param.verticalOverflow = VerticalWrapMode.Overflow;

            var terminals = CreateText("TerminalText", infoArea, BuildTerminalSummary(entry.Definition, entry.Terminals), 12, FontStyle.Normal, new Color(0.18f, 0.24f, 0.32f));
            terminals.alignment = TextAnchor.MiddleLeft;
            terminals.horizontalOverflow = HorizontalWrapMode.Wrap;
            terminals.verticalOverflow = VerticalWrapMode.Overflow;'''

if old_info_layout in content:
    content = content.replace(old_info_layout, new_info_layout)
    print("Info Layout replaced.")
else:
    print("Info Layout NOT FOUND.")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
