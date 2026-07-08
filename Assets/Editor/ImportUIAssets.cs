using UnityEditor;
using UnityEngine;
using System.IO;

public class ImportUIAssets : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("UIAssets"))
        {
            ApplyUiSpriteSettings((TextureImporter)assetImporter);
        }
    }

    [MenuItem("Tools/Force Import UI Assets As Sprite")]
    public static void ForceImport()
    {
        AssetDatabase.Refresh();
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/UIAssets" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                ApplyUiSpriteSettings(importer);
                importer.SaveAndReimport();
            }
        }
        Debug.Log("Forced UIAssets import as Sprite.");
    }

    private static void ApplyUiSpriteSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
