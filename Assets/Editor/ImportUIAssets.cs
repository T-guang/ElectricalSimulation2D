using UnityEditor;
using UnityEngine;
using System.IO;

public class ImportUIAssets : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("UIAssets"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
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
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("Forced UIAssets import as Sprite.");
    }
}
