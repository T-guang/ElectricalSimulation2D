using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class ExpandCanvasScript {
    public static void Run() {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo.unity");
        string logPath = "canvas_log.txt";
        using (StreamWriter sw = new StreamWriter(logPath)) {
            sw.WriteLine("--- START SCRIPT ---");
            var scrollRects = Object.FindObjectsOfType<ScrollRect>(true);
            foreach(var sr in scrollRects) {
                sw.WriteLine("Found ScrollRect: " + sr.gameObject.name);
                var content = sr.content;
                if (content != null) {
                    sw.WriteLine("Content old size: " + content.sizeDelta);
                    if (content.sizeDelta.x > 0) {
                        content.sizeDelta = new Vector2(content.sizeDelta.x * 1.5f, content.sizeDelta.y * 1.5f);
                        sw.WriteLine("Content new size: " + content.sizeDelta);
                    }
                }
            }
            
            string[] targetNames = new string[] { "Workspace", "ComponentLayer", "WireLayer", "SimulationPage", "Grid", "Background" };
            foreach(string name in targetNames) {
                GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach(GameObject go in objs) {
                    if (go.name == name && go.scene.isLoaded) {
                        RectTransform rt = go.GetComponent<RectTransform>();
                        if (rt != null) {
                            sw.WriteLine("Found Target: " + name + " | Anchor: " + rt.anchorMin + "-" + rt.anchorMax + " | SizeDelta: " + rt.sizeDelta);
                            if (rt.anchorMin == rt.anchorMax) {
                                rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.5f, rt.sizeDelta.y * 1.5f);
                                sw.WriteLine("Expanded Absolute Size to: " + rt.sizeDelta);
                                EditorUtility.SetDirty(rt.gameObject);
                            } else if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one) {
                                rt.offsetMin = new Vector2(rt.offsetMin.x - 500, rt.offsetMin.y - 500);
                                rt.offsetMax = new Vector2(rt.offsetMax.x + 500, rt.offsetMax.y + 500);
                                sw.WriteLine("Expanded Stretched Bounds to offsetMin=" + rt.offsetMin + " offsetMax=" + rt.offsetMax);
                                EditorUtility.SetDirty(rt.gameObject);
                            }
                        }
                    }
                }
            }
        }
        EditorSceneManager.SaveScene(scene);
    }
}
