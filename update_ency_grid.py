import re

file_path = 'Assets/Scripts/UI/EncyclopediaController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

old_block = '''            cardContent = CreateRect("CardGridContent", viewport);
            cardContent.anchorMin = new Vector2(0f, 1f);
            cardContent.anchorMax = new Vector2(1f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = Vector2.zero;

            cardGridLayout = cardContent.gameObject.AddComponent<GridLayoutGroup>();
            cardGridLayout.cellSize = new Vector2(CardWidth, CardHeight);
            cardGridLayout.spacing = new Vector2(CardGapX, CardGapY);
            cardGridLayout.padding = new RectOffset(0, 0, 0, 24);
            cardGridLayout.childAlignment = TextAnchor.UpperLeft;
            cardGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            cardGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            cardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cardGridLayout.constraintCount = 3;

            var fitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;'''

new_block = '''            cardContent = CreateRect("CardGridContent", viewport);
            cardContent.anchorMin = new Vector2(0.5f, 1f);
            cardContent.anchorMax = new Vector2(0.5f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = Vector2.zero;

            cardGridLayout = cardContent.gameObject.AddComponent<GridLayoutGroup>();
            cardGridLayout.cellSize = new Vector2(CardWidth, CardHeight);
            cardGridLayout.spacing = new Vector2(CardGapX, CardGapY);
            cardGridLayout.padding = new RectOffset(0, 0, 0, 24);
            cardGridLayout.childAlignment = TextAnchor.UpperLeft;
            cardGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            cardGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            cardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cardGridLayout.constraintCount = 3;

            var fitter = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;'''

if old_block in content:
    content = content.replace(old_block, new_block)
    print("SUCCESS: CardGridContent anchors and Fitter updated!")
else:
    print("ERROR: old_block NOT FOUND!")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
