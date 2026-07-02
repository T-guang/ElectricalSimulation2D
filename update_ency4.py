import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Update margin from sidebar (250f to 290f)
content = re.sub(r'StretchTo\(scrollRoot, 250f, 104f, 32f, 24f\);', r'StretchTo(scrollRoot, 290f, 104f, 32f, 24f);', content)
content = re.sub(r'StretchTo\(emptyText\.rectTransform, 250f, 104f, 32f, 24f\);', r'StretchTo(emptyText.rectTransform, 290f, 104f, 32f, 24f);', content)

# 2. Update CardGridContent Anchors and Fitter for centering
old_content_setup = '''            cardContent = CreateRect("CardGridContent", viewport);
            cardContent.anchorMin = new Vector2(0f, 1f);
            cardContent.anchorMax = new Vector2(1f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = Vector2.zero;

            var fitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;'''

new_content_setup = '''            cardContent = CreateRect("CardGridContent", viewport);
            cardContent.anchorMin = new Vector2(0.5f, 1f);
            cardContent.anchorMax = new Vector2(0.5f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = Vector2.zero;

            var fitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;'''

if old_content_setup in content:
    content = content.replace(old_content_setup, new_content_setup)
    print("CardGridContent anchors updated.")
else:
    print("CardGridContent anchors NOT FOUND.")

# 3. Update Columns Constraint Logic (Remove the 3-column hard limit)
old_columns_logic = '''            var columns = Mathf.Max(1, Mathf.FloorToInt((viewportWidth + CardGapX) / (CardWidth + CardGapX)));
            cardGridLayout.constraintCount = Mathf.Clamp(columns, 1, 3);'''

new_columns_logic = '''            var columns = Mathf.Max(1, Mathf.FloorToInt((viewportWidth + CardGapX) / (CardWidth + CardGapX)));
            cardGridLayout.constraintCount = columns;'''

if old_columns_logic in content:
    content = content.replace(old_columns_logic, new_columns_logic)
    print("Columns constraint updated.")
else:
    print("Columns constraint NOT FOUND.")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
