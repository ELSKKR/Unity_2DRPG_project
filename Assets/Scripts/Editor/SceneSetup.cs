using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SceneSetup
{
    [MenuItem("JRPG/Setup Scene")]
    public static void SetupScene()
    {
        SetupCamera();
        var player = CreatePlayer();
        CreateTilemap();
        ConnectCameraToPlayer(player);
        EditorUtility.DisplayDialog("完成", "場景設定完成！\n按 Play 測試移動。", "OK");
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);

        if (cam.GetComponent<CameraFollow>() == null)
            cam.gameObject.AddComponent<CameraFollow>();
    }

    static void ConnectCameraToPlayer(GameObject player)
    {
        var follow = Camera.main?.GetComponent<CameraFollow>();
        if (follow == null) return;
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedProperties();
    }

    // ── Player ────────────────────────────────────────────────────────────────

    static GameObject CreatePlayer()
    {
        var existing = GameObject.FindWithTag("Player");
        if (existing != null)
        {
            Debug.Log("Player 已存在，跳過建立。");
            return existing;
        }

        var player = new GameObject("Player");
        player.tag = "Player";

        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreatePlayerSprite();
        sr.sortingOrder = 0;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.35f);
        col.offset = new Vector2(0f, -0.2f);

        player.AddComponent<PlayerController>();
        player.AddComponent<SpriteSortingByY>();

        var anim = player.AddComponent<Animator>();
        anim.runtimeAnimatorController = GetOrCreateAnimatorController();

        player.AddComponent<PlayerAnimator>();

        return player;
    }

    // ── Tilemap ───────────────────────────────────────────────────────────────

    static void CreateTilemap()
    {
        if (GameObject.Find("Grid") != null) return;

        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        AddTilemapLayer(gridGO, "Ground",   sortOrder: -10);
        AddTilemapLayer(gridGO, "Collision", sortOrder: -9);
    }

    static void AddTilemapLayer(GameObject parent, string name, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = sortOrder;
    }

    // ── Assets ────────────────────────────────────────────────────────────────

    static Sprite GetOrCreatePlayerSprite()
    {
        const string dir  = "Assets/Art/Sprites";
        const string path = dir + "/Player_Placeholder.png";

        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(dir);

        int w = 32, h = 48;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px  = new Color[w * h];
        var skin = new Color(0.40f, 0.80f, 1.00f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool head = x >= 10 && x < 22 && y >= 34 && y < 46;
                bool body = x >= 8  && x < 24 && y >= 16 && y < 34;
                bool legs = (x >= 8  && x < 14 && y >= 4 && y < 16)
                         || (x >= 18 && x < 24 && y >= 4 && y < 16);
                px[y * w + x] = (head || body || legs) ? skin : Color.clear;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType          = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit  = 32;
        imp.filterMode           = FilterMode.Point;
        imp.textureCompression   = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot     = new Vector2(0.5f, 0.15f);
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static RuntimeAnimatorController GetOrCreateAnimatorController()
    {
        const string dir  = "Assets/Animations";
        const string path = dir + "/PlayerAnimator.controller";

        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("MoveX",    AnimatorControllerParameterType.Float);
        ctrl.AddParameter("MoveY",    AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        var sm   = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle");
        var walk = sm.AddState("Walk");
        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration    = 0f;

        var toIdle = walk.AddTransition(idle);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration    = 0f;

        AssetDatabase.SaveAssets();
        return ctrl;
    }
}
