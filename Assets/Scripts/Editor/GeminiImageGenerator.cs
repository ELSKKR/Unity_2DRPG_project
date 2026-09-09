using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class GeminiImageGenerator : EditorWindow
{
    private string apiKey;
    private string prompt = "";
    private string savePath = "Assets/Textures/Generated";
    private string fileName = "generated_image";
    private string modelName = "imagen-3.0-generate-002";
    private bool isGenerating;
    private string status = "";
    private bool statusIsError;

    private static readonly HttpClient client = new HttpClient();

    [MenuItem("Tools/Gemini Image Generator")]
    static void Open() => GetWindow<GeminiImageGenerator>("Gemini Image Generator");

    void OnEnable()
    {
        apiKey   = EditorPrefs.GetString("GeminiImageGen_APIKey",    "");
        savePath = EditorPrefs.GetString("GeminiImageGen_SavePath",  "Assets/Textures/Generated");
        modelName = EditorPrefs.GetString("GeminiImageGen_Model",    "imagen-3.0-generate-002");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Gemini Image Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("API Key");
        string newKey = EditorGUILayout.PasswordField(apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            apiKey = newKey;
            EditorPrefs.SetString("GeminiImageGen_APIKey", apiKey);
        }

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Model");
        string newModel = EditorGUILayout.TextField(modelName);
        if (EditorGUI.EndChangeCheck())
        {
            modelName = newModel;
            EditorPrefs.SetString("GeminiImageGen_Model", modelName);
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Prompt");
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(80));

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Save Folder");
        savePath = EditorGUILayout.TextField(savePath);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString("GeminiImageGen_SavePath", savePath);

        EditorGUILayout.LabelField("File Name (no extension)");
        fileName = EditorGUILayout.TextField(fileName);

        EditorGUILayout.Space(8);

        GUI.enabled = !isGenerating && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(prompt);
        if (GUILayout.Button(isGenerating ? "Generating..." : "Generate & Import", GUILayout.Height(32)))
            _ = GenerateAsync();
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(status, statusIsError ? MessageType.Error : MessageType.Info);
        }
    }

    async Task GenerateAsync()
    {
        isGenerating = true;
        status = "Calling Gemini API...";
        statusIsError = false;
        Repaint();

        try
        {
            string url  = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:predict?key={apiKey}";
            string body = $"{{\"instances\":[{{\"prompt\":\"{EscapeJson(prompt)}\"}}],\"parameters\":{{\"sampleCount\":1}}}}";

            var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
            var response    = await client.PostAsync(url, httpContent);
            string json     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                status = $"API Error {(int)response.StatusCode}: {json}";
                statusIsError = true;
                return;
            }

            string base64 = ExtractBase64(json);
            if (base64 == null)
            {
                status = "Could not find image data in response:\n" + json;
                statusIsError = true;
                return;
            }

            byte[] imageBytes = Convert.FromBase64String(base64);

            string relDir  = savePath.StartsWith("Assets/") ? savePath.Substring("Assets/".Length) : savePath;
            string fullDir = Path.Combine(Application.dataPath, relDir);
            Directory.CreateDirectory(fullDir);

            string fullFilePath = Path.Combine(fullDir, fileName + ".png");
            File.WriteAllBytes(fullFilePath, imageBytes);

            string assetPath = savePath.TrimEnd('/') + "/" + fileName + ".png";
            AssetDatabase.ImportAsset(assetPath);

            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (imported != null)
                Selection.activeObject = imported;

            status = $"Saved to {assetPath}";
        }
        catch (Exception e)
        {
            status = $"Error: {e.Message}";
            statusIsError = true;
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }

    // Handles Imagen response: "bytesBase64Encoded":"..."
    // Also handles Gemini inline response: "data":"..."
    static string ExtractBase64(string json)
    {
        foreach (string marker in new[] { "\"bytesBase64Encoded\":\"", "\"data\":\"" })
        {
            int start = json.IndexOf(marker);
            if (start < 0) continue;
            start += marker.Length;
            int end = json.IndexOf('"', start);
            if (end < 0) continue;
            return json.Substring(start, end - start);
        }
        return null;
    }

    static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "\\r")
         .Replace("\t", "\\t");
}
