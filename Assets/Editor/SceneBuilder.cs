#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SipLab;

public static class SceneBuilder
{
    const string MATERIALS_FOLDER = "Assets/Materials";
    static int InteractableLayer => LayerMask.NameToLayer("Interactable");

    [MenuItem("SIP Lab/Rebuild Scene %#b")]
    public static void RebuildSceneMenu()
    {
        var path = BuildAll();
        Debug.Log($"[SceneBuilder] Scene rebuilt: {path}");
    }

    public static string BuildAll()
    {
        EnsureFolder("Assets/Materials");
        var mats = CreateMaterials();
        var scene = SceneManager.GetActiveScene();
        ClearScene(scene);

        BuildRoom(mats);
        var lighting = BuildLighting(mats);
        var managers = BuildManagers();
        var (canvases, hudCtl, menuMgr) = BuildUI();
        BuildPlayer(mats, hudCtl);
        BuildPuzzle1(mats);
        BuildPuzzle2(mats);
        BuildPuzzle3(mats);
        WireUpManagers(managers, lighting, canvases, hudCtl, menuMgr);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings(scene.path);
        return "scene built and saved at " + scene.path;
    }

    static void ClearScene(Scene s)
    {
        foreach (var go in s.GetRootGameObjects())
            Object.DestroyImmediate(go);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    public class Mats
    {
        public Material WallGray, FloorGray, CeilingGray, CrateOrange, EnergyCoreBlue,
            GeneratorMetal, IndicatorRed, IndicatorGreen, IndicatorYellow, KeypadDark,
            KeypadButton, SafeMetal, SafeDoor, KeycardWhite, BlastDoor, DoorPanel, HintBoard,
            ServerRack, ServerDrawerMat, ServerLedOff, ServerLedOn, DeskWood, Paper;
    }

    static Material MakeMat(string name, Color color, float metallic, float smoothness, Color? emission = null)
    {
        string path = MATERIALS_FOLDER + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mats CreateMaterials()
    {
        var m = new Mats();
        m.WallGray = MakeMat("WallGray", new Color(0.32f, 0.34f, 0.38f), 0.4f, 0.25f);
        m.FloorGray = MakeMat("FloorGray", new Color(0.18f, 0.20f, 0.23f), 0.5f, 0.4f);
        m.CeilingGray = MakeMat("CeilingGray", new Color(0.14f, 0.15f, 0.17f), 0.2f, 0.1f);
        m.CrateOrange = MakeMat("CrateOrange", new Color(0.78f, 0.42f, 0.12f), 0.1f, 0.2f);
        m.EnergyCoreBlue = MakeMat("EnergyCoreBlue", new Color(0.2f, 0.7f, 1.0f), 0.0f, 0.6f, new Color(0.2f, 0.8f, 1.5f));
        m.GeneratorMetal = MakeMat("GeneratorMetal", new Color(0.30f, 0.32f, 0.36f), 0.8f, 0.55f);
        m.IndicatorRed = MakeMat("IndicatorRed", new Color(0.9f, 0.1f, 0.1f), 0.0f, 0.3f, new Color(0.9f, 0.1f, 0.1f) * 1.5f);
        m.IndicatorGreen = MakeMat("IndicatorGreen", new Color(0.1f, 0.9f, 0.2f), 0.0f, 0.3f, new Color(0.1f, 0.9f, 0.2f) * 1.5f);
        m.IndicatorYellow = MakeMat("IndicatorYellow", new Color(0.95f, 0.85f, 0.1f), 0.0f, 0.3f, new Color(0.95f, 0.85f, 0.1f) * 1.5f);
        m.KeypadDark = MakeMat("KeypadDark", new Color(0.10f, 0.11f, 0.13f), 0.6f, 0.5f);
        m.KeypadButton = MakeMat("KeypadButton", new Color(0.85f, 0.85f, 0.88f), 0.2f, 0.4f);
        m.SafeMetal = MakeMat("SafeMetal", new Color(0.25f, 0.27f, 0.30f), 0.8f, 0.6f);
        m.SafeDoor = MakeMat("SafeDoorMat", new Color(0.40f, 0.42f, 0.46f), 0.85f, 0.7f);
        m.KeycardWhite = MakeMat("KeycardWhite", new Color(0.95f, 0.95f, 1.0f), 0.0f, 0.4f, new Color(0.7f, 0.95f, 1.0f) * 0.7f);
        m.BlastDoor = MakeMat("BlastDoor", new Color(0.45f, 0.30f, 0.10f), 0.85f, 0.6f);
        m.DoorPanel = MakeMat("DoorPanel", new Color(0.20f, 0.22f, 0.25f), 0.6f, 0.5f);
        m.HintBoard = MakeMat("HintBoard", new Color(0.06f, 0.07f, 0.09f), 0.3f, 0.2f, new Color(0.05f, 0.4f, 0.5f) * 0.4f);
        m.ServerRack = MakeMat("ServerRack", new Color(0.08f, 0.09f, 0.11f), 0.75f, 0.35f);
        m.ServerDrawerMat = MakeMat("ServerDrawer", new Color(0.16f, 0.17f, 0.19f), 0.7f, 0.45f);
        m.ServerLedOff = MakeMat("ServerLedOff", new Color(0.04f, 0.06f, 0.05f), 0.1f, 0.2f);
        m.ServerLedOn = MakeMat("ServerLedOn", new Color(0.15f, 0.95f, 0.25f), 0.0f, 0.4f, new Color(0.2f, 1.0f, 0.3f) * 1.8f);
        m.DeskWood = MakeMat("DeskWood", new Color(0.35f, 0.24f, 0.14f), 0.1f, 0.3f);
        m.Paper = MakeMat("Paper", new Color(0.92f, 0.88f, 0.78f), 0.0f, 0.15f);
        AssetDatabase.SaveAssets();
        return m;
    }

    static GameObject MakePrim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject MakeEmpty(string name, Vector3 pos, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go;
    }

    public static void BuildRoom(Mats m)
    {
        var root = new GameObject("Room").transform;

        // Floor (10x10) at y=0
        var floor = MakePrim(PrimitiveType.Cube, "Floor", new Vector3(0, -0.05f, 0), new Vector3(10f, 0.1f, 10f), m.FloorGray, root);

        // Ceiling at y=4
        var ceiling = MakePrim(PrimitiveType.Cube, "Ceiling", new Vector3(0, 4.05f, 0), new Vector3(10f, 0.1f, 10f), m.CeilingGray, root);

        // Walls (10m wide, 4m high, 0.2m thick)
        // South wall
        MakePrim(PrimitiveType.Cube, "WallSouth", new Vector3(0, 2f, -5f - 0.1f), new Vector3(10f, 4f, 0.2f), m.WallGray, root);
        // East wall
        MakePrim(PrimitiveType.Cube, "WallEast", new Vector3(5f + 0.1f, 2f, 0f), new Vector3(0.2f, 4f, 10f), m.WallGray, root);
        // West wall
        MakePrim(PrimitiveType.Cube, "WallWest", new Vector3(-5f - 0.1f, 2f, 0f), new Vector3(0.2f, 4f, 10f), m.WallGray, root);

        // North wall in two segments leaving 2m wide gap (door span -1 to +1) and lintel above
        // Left segment from x=-5 to x=-1 (width 4)
        MakePrim(PrimitiveType.Cube, "WallNorthLeft", new Vector3(-3f, 2f, 5f + 0.1f), new Vector3(4f, 4f, 0.2f), m.WallGray, root);
        // Right segment from x=1 to x=5 (width 4)
        MakePrim(PrimitiveType.Cube, "WallNorthRight", new Vector3(3f, 2f, 5f + 0.1f), new Vector3(4f, 4f, 0.2f), m.WallGray, root);
        // Lintel above gap (gap height = 3m, so lintel from y=3 to y=4, height 1m)
        MakePrim(PrimitiveType.Cube, "WallNorthLintel", new Vector3(0f, 3.5f, 5f + 0.1f), new Vector3(2f, 1f, 0.2f), m.WallGray, root);

        // Door frame on north (decorative)
        MakePrim(PrimitiveType.Cube, "DoorFrameLeft", new Vector3(-1.05f, 1.5f, 5f), new Vector3(0.1f, 3f, 0.3f), m.DoorPanel, root);
        MakePrim(PrimitiveType.Cube, "DoorFrameRight", new Vector3(1.05f, 1.5f, 5f), new Vector3(0.1f, 3f, 0.3f), m.DoorPanel, root);
        MakePrim(PrimitiveType.Cube, "DoorFrameTop", new Vector3(0f, 3.05f, 5f), new Vector3(2.3f, 0.1f, 0.3f), m.DoorPanel, root);

        // Outer victory zone (small floor extension behind door so player can step out)
        MakePrim(PrimitiveType.Cube, "OuterFloor", new Vector3(0f, -0.05f, 6.5f), new Vector3(2.5f, 0.1f, 3f), m.FloorGray, root);
        MakePrim(PrimitiveType.Cube, "OuterWallBack", new Vector3(0f, 2f, 8f), new Vector3(2.5f, 4f, 0.2f), m.WallGray, root);
        MakePrim(PrimitiveType.Cube, "OuterWallLeft", new Vector3(-1.25f, 2f, 6.5f), new Vector3(0.2f, 4f, 3f), m.WallGray, root);
        MakePrim(PrimitiveType.Cube, "OuterWallRight", new Vector3(1.25f, 2f, 6.5f), new Vector3(0.2f, 4f, 3f), m.WallGray, root);
    }

    public class LightingRefs { public LightingController Controller; public List<Light> Reds = new List<Light>(); public List<Light> Whites = new List<Light>(); }

    public static LightingRefs BuildLighting(Mats m)
    {
        var refs = new LightingRefs();
        var root = new GameObject("Lighting").transform;

        // Configure render settings
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.0f, 0.0f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.06f, 0.02f, 0.02f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 4f;
        RenderSettings.fogEndDistance = 16f;
        RenderSettings.skybox = null;

        // Red point lights at four ceiling corners
        Vector3[] corners = new Vector3[] {
            new Vector3(-3.5f, 3.7f, -3.5f),
            new Vector3( 3.5f, 3.7f, -3.5f),
            new Vector3(-3.5f, 3.7f,  3.5f),
            new Vector3( 3.5f, 3.7f,  3.5f),
        };
        for (int i = 0; i < corners.Length; i++)
        {
            var go = new GameObject("RedLight_" + i);
            go.transform.SetParent(root, false);
            go.transform.localPosition = corners[i];
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.10f, 0.10f);
            l.intensity = 1.2f;
            l.range = 9f;
            l.shadows = LightShadows.Soft;
            refs.Reds.Add(l);

            // small bulb visual
            var bulb = MakePrim(PrimitiveType.Sphere, "Bulb", Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), null, go.transform);
            bulb.GetComponent<Renderer>().sharedMaterial = m.IndicatorRed;
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
        }

        // White directional light (powered state, initially off)
        var dirGO = new GameObject("WhiteDirectional");
        dirGO.transform.SetParent(root, false);
        dirGO.transform.localPosition = new Vector3(0f, 3.8f, 0f);
        dirGO.transform.localRotation = Quaternion.Euler(55f, 30f, 0f);
        var dl = dirGO.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.95f, 0.97f, 1.0f);
        dl.intensity = 0f;
        dl.shadows = LightShadows.Soft;
        refs.Whites.Add(dl);

        // Add a second fill area-style point in the room (treated as white)
        var fillGO = new GameObject("WhiteFill");
        fillGO.transform.SetParent(root, false);
        fillGO.transform.localPosition = new Vector3(0f, 3.7f, 0f);
        var fl = fillGO.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.95f, 0.97f, 1.0f);
        fl.intensity = 0f;
        fl.range = 14f;
        fl.shadows = LightShadows.Soft;
        refs.Whites.Add(fl);

        var ctlGO = new GameObject("LightingController");
        ctlGO.transform.SetParent(root, false);
        var ctl = ctlGO.AddComponent<LightingController>();
        ctl.RedLights = refs.Reds;
        ctl.WhiteLights = refs.Whites;
        refs.Controller = ctl;
        return refs;
    }

    public static GameStateManager BuildManagers()
    {
        var go = new GameObject("GameStateManager");
        var gsm = go.AddComponent<GameStateManager>();

        var audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();
        return gsm;
    }

    public static void BuildPlayer(Mats m, HUDController hud)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.1f, -4f);

        var col = player.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.4f;
        col.center = new Vector3(0f, 0f, 0f);

        var rb = player.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.drag = 4f;
        rb.angularDrag = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var inv = player.AddComponent<PlayerInventory>();

        var camGO = new GameObject("Camera");
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 50f;
        cam.fieldOfView = 70f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.01f, 0.01f);
        camGO.AddComponent<AudioListener>();

        var fpc = player.AddComponent<FirstPersonController>();
        fpc.CameraPivot = camGO.transform;

        player.AddComponent<FootstepEmitter>();

        var ray = camGO.AddComponent<InteractionRaycaster>();
        ray.Cam = cam;
        ray.Inventory = inv;
        ray.Range = 3.0f;
        ray.InteractableMask = 1 << InteractableLayer;
    }

    public class CanvasRefs { public Canvas Hud, Start, Pause, Victory, Settings; public Text VictoryTime; }

    public static (CanvasRefs, HUDController, MenuManager) BuildUI()
    {
        var refs = new CanvasRefs();
        var uiRoot = new GameObject("UI");
        var es = new GameObject("EventSystem");
        es.transform.SetParent(uiRoot.transform, false);
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // HUD Canvas
        var hudGO = NewCanvas("HUDCanvas", uiRoot.transform, out refs.Hud, sortOrder: 0);
        var hudCG = hudGO.AddComponent<CanvasGroup>();
        hudCG.alpha = 0f;

        // Crosshair (ring sprite)
        var cross = NewImage("Crosshair", hudGO.transform);
        var crossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/CrosshairRing.png");
        if (crossSprite != null) cross.sprite = crossSprite;
        cross.color = new Color(1f, 1f, 1f, 0.9f);
        var cRT = cross.rectTransform;
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(28f, 28f);

        // Prompt text (center bottom)
        var prompt = NewText("PromptText", hudGO.transform, "", 26, TextAnchor.MiddleCenter);
        var pRT = prompt.rectTransform;
        pRT.anchorMin = new Vector2(0.5f, 0f);
        pRT.anchorMax = new Vector2(0.5f, 0f);
        pRT.pivot = new Vector2(0.5f, 0f);
        pRT.anchoredPosition = new Vector2(0f, 90f);
        pRT.sizeDelta = new Vector2(900f, 60f);
        prompt.color = new Color(1f, 1f, 1f, 0.95f);

        // Inventory text (top right)
        var inv = NewText("InventoryText", hudGO.transform, "Holding: Nothing", 22, TextAnchor.UpperRight);
        var iRT = inv.rectTransform;
        iRT.anchorMin = new Vector2(1f, 1f);
        iRT.anchorMax = new Vector2(1f, 1f);
        iRT.pivot = new Vector2(1f, 1f);
        iRT.anchoredPosition = new Vector2(-24f, -16f);
        iRT.sizeDelta = new Vector2(360f, 36f);

        // Timer (top left)
        var timer = NewText("TimerText", hudGO.transform, "Time: 00:00.00", 22, TextAnchor.UpperLeft);
        var tRT = timer.rectTransform;
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(0f, 1f);
        tRT.pivot = new Vector2(0f, 1f);
        tRT.anchoredPosition = new Vector2(24f, -16f);
        tRT.sizeDelta = new Vector2(280f, 36f);

        var hudCtl = uiRoot.AddComponent<HUDController>();
        hudCtl.Crosshair = cross;
        hudCtl.PromptText = prompt;
        hudCtl.InventoryText = inv;
        hudCtl.TimerText = timer;
        hudCtl.HudCanvasGroup = hudCG;

        // Start canvas
        var startGO = NewCanvas("StartCanvas", uiRoot.transform, out refs.Start, sortOrder: 10);
        AddPanel(startGO.transform, new Color(0.02f, 0.02f, 0.04f, 0.92f));
        AddTitle(startGO.transform, "THE SIP LAB LOCKDOWN", new Vector2(0f, 180f), 56);
        AddTitle(startGO.transform, "An Escape Room", new Vector2(0f, 110f), 28);
        AddTitle(startGO.transform, "WASD = Move    Mouse = Look    E / LMB = Interact    Esc / Space = Pause", new Vector2(0f, -30f), 18);

        // Menu manager
        var menuMgr = uiRoot.AddComponent<MenuManager>();

        var startBtn = AddButton(startGO.transform, "Start", new Vector2(0f, -120f));
        var settingsBtn1 = AddButton(startGO.transform, "Settings", new Vector2(0f, -200f));
        var quitBtn = AddButton(startGO.transform, "Quit", new Vector2(0f, -280f));
        UnityEventTools.AddPersistentListener(startBtn.onClick, (UnityAction)menuMgr.OnStartClicked);
        UnityEventTools.AddPersistentListener(settingsBtn1.onClick, (UnityAction)menuMgr.OnSettingsClicked);
        UnityEventTools.AddPersistentListener(quitBtn.onClick, (UnityAction)menuMgr.OnQuitClicked);

        // Pause canvas
        var pauseGO = NewCanvas("PauseCanvas", uiRoot.transform, out refs.Pause, sortOrder: 10);
        AddPanel(pauseGO.transform, new Color(0.02f, 0.02f, 0.04f, 0.85f));
        AddTitle(pauseGO.transform, "PAUSED", new Vector2(0f, 140f), 56);
        var resumeBtn = AddButton(pauseGO.transform, "Resume", new Vector2(0f, 20f));
        var settingsBtn2 = AddButton(pauseGO.transform, "Settings", new Vector2(0f, -60f));
        var restartBtn = AddButton(pauseGO.transform, "Restart Level", new Vector2(0f, -140f));
        var quitBtn2 = AddButton(pauseGO.transform, "Quit", new Vector2(0f, -220f));
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, (UnityAction)menuMgr.OnResumeClicked);
        UnityEventTools.AddPersistentListener(settingsBtn2.onClick, (UnityAction)menuMgr.OnSettingsClicked);
        UnityEventTools.AddPersistentListener(restartBtn.onClick, (UnityAction)menuMgr.OnRestartClicked);
        UnityEventTools.AddPersistentListener(quitBtn2.onClick, (UnityAction)menuMgr.OnQuitClicked);

        // Settings canvas
        var settingsGO = NewCanvas("SettingsCanvas", uiRoot.transform, out refs.Settings, sortOrder: 11);
        AddPanel(settingsGO.transform, new Color(0.02f, 0.02f, 0.04f, 0.94f));
        AddTitle(settingsGO.transform, "SETTINGS", new Vector2(0f, 200f), 52);

        var iLabel = AddTitle(settingsGO.transform, "Interaction Volume: 80%", new Vector2(0f, 90f), 24);
        var iSlider = AddSlider(settingsGO.transform, new Vector2(0f, 50f), 0.8f);
        var aLabel = AddTitle(settingsGO.transform, "Action Volume: 70%", new Vector2(0f, -10f), 24);
        var aSlider = AddSlider(settingsGO.transform, new Vector2(0f, -50f), 0.7f);
        var backBtn = AddButton(settingsGO.transform, "Back", new Vector2(0f, -160f));
        UnityEventTools.AddPersistentListener(backBtn.onClick, (UnityAction)menuMgr.OnSettingsBackClicked);

        var settingsCtl = settingsGO.AddComponent<SettingsPanel>();
        settingsCtl.InteractionSlider = iSlider;
        settingsCtl.ActionSlider = aSlider;
        settingsCtl.InteractionLabel = iLabel;
        settingsCtl.ActionLabel = aLabel;

        // Victory canvas
        var victoryGO = NewCanvas("VictoryCanvas", uiRoot.transform, out refs.Victory, sortOrder: 10);
        AddPanel(victoryGO.transform, new Color(0.0f, 0.05f, 0.0f, 0.92f));
        AddTitle(victoryGO.transform, "ESCAPED!", new Vector2(0f, 150f), 72);
        var vTime = AddTitle(victoryGO.transform, "Time to Escape: 00:00", new Vector2(0f, 60f), 32);
        refs.VictoryTime = vTime;
        var vRestartBtn = AddButton(victoryGO.transform, "Play Again", new Vector2(0f, -60f));
        var vQuitBtn = AddButton(victoryGO.transform, "Quit", new Vector2(0f, -140f));
        UnityEventTools.AddPersistentListener(vRestartBtn.onClick, (UnityAction)menuMgr.OnRestartClicked);
        UnityEventTools.AddPersistentListener(vQuitBtn.onClick, (UnityAction)menuMgr.OnQuitClicked);

        menuMgr.StartCanvas = refs.Start;
        menuMgr.PauseCanvas = refs.Pause;
        menuMgr.VictoryCanvas = refs.Victory;
        menuMgr.SettingsCanvas = refs.Settings;
        menuMgr.VictoryTimeText = refs.VictoryTime;

        // Initially hide pause/victory/settings
        refs.Pause.gameObject.SetActive(false);
        refs.Victory.gameObject.SetActive(false);
        refs.Settings.gameObject.SetActive(false);

        return (refs, hudCtl, menuMgr);
    }

    static GameObject NewCanvas(string name, Transform parent, out Canvas canvas, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static Image NewImage(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        return img;
    }

    static Text NewText(string name, Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void AddPanel(Transform parent, Color c)
    {
        var img = NewImage("Background", parent);
        img.color = c;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Text AddTitle(Transform parent, string text, Vector2 pos, int size)
    {
        var t = NewText("Title", parent, text, size, TextAnchor.MiddleCenter);
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1200f, size + 24f);
        return t;
    }

    static Slider AddSlider(Transform parent, Vector2 pos, float initial)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(420f, 24f);

        // Background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(go.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.20f, 0.24f, 1f);

        // Fill area + fill
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.25f);
        faRT.anchorMax = new Vector2(1f, 0.75f);
        faRT.offsetMin = new Vector2(8f, 0f);
        faRT.offsetMax = new Vector2(-8f, 0f);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        var fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.30f, 0.65f, 0.90f, 1f);

        // Handle
        var handleArea = new GameObject("HandleSlideArea");
        handleArea.transform.SetParent(go.transform, false);
        var haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(8f, 0f);
        haRT.offsetMax = new Vector2(-8f, 0f);

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        var hRT = handleGO.AddComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(20f, 36f);
        var handle = handleGO.AddComponent<Image>();
        handle.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        var slider = go.AddComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fillRT;
        slider.handleRect = hRT;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(initial);

        return slider;
    }

    static Button AddButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280f, 60f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.20f, 0.24f, 0.95f);
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        cb.highlightedColor = new Color(0.30f, 0.34f, 0.42f, 1f);
        cb.pressedColor = new Color(0.50f, 0.55f, 0.65f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        var tgo = new GameObject("Text");
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;

        return btn;
    }

    public static void BuildPuzzle1(Mats m)
    {
        var root = new GameObject("Puzzle1_PowerRestoration").transform;

        // Energy Core (hidden in southwest corner under crates)
        var coreGO = MakePrim(PrimitiveType.Cylinder, "EnergyCore", new Vector3(-4.0f, 0.25f, -4.0f), new Vector3(0.4f, 0.25f, 0.4f), m.EnergyCoreBlue, root);
        coreGO.layer = InteractableLayer;
        coreGO.AddComponent<EnergyCore>();
        // glow light
        var coreLight = new GameObject("CoreGlow");
        coreLight.transform.SetParent(coreGO.transform, false);
        var cl = coreLight.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.3f, 0.7f, 1f);
        cl.intensity = 0.8f;
        cl.range = 2.5f;

        // 5 crates piled in SW corner around the core
        var cratesRoot = new GameObject("Crates");
        cratesRoot.transform.SetParent(root, false);
        Vector3[] crateOffsets = new Vector3[]
        {
            new Vector3(-3.5f, 0.5f, -4.2f),
            new Vector3(-4.4f, 0.5f, -3.7f),
            new Vector3(-3.7f, 0.5f, -3.5f),
            new Vector3(-4.0f, 1.5f, -4.0f),
            new Vector3(-3.8f, 1.5f, -3.4f),
        };
        for (int i = 0; i < 5; i++)
        {
            var crate = MakePrim(PrimitiveType.Cube, "Crate_" + i, crateOffsets[i], new Vector3(0.9f, 0.9f, 0.9f), m.CrateOrange, cratesRoot.transform);
            var rb = crate.AddComponent<Rigidbody>();
            rb.mass = 4f;
            rb.drag = 2.0f;
            rb.angularDrag = 5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            crate.GetComponent<BoxCollider>();
            crate.AddComponent<CrateCollisionSound>();
        }

        // Generator Terminal on east wall
        var gen = MakeEmpty("GeneratorTerminal", new Vector3(4.6f, 0f, -3f), root);
        var body = MakePrim(PrimitiveType.Cube, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 1.2f, 0.4f), m.GeneratorMetal, gen.transform);
        body.layer = InteractableLayer;
        var screen = MakePrim(PrimitiveType.Cube, "Screen", new Vector3(-0.21f, 0.85f, 0f), new Vector3(0.05f, 0.35f, 0.3f), m.KeypadDark, gen.transform);
        var indicator = MakePrim(PrimitiveType.Sphere, "Indicator", new Vector3(-0.22f, 1.15f, 0f), new Vector3(0.12f, 0.12f, 0.12f), m.IndicatorRed, gen.transform);
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        indicator.layer = InteractableLayer;
        var slot = MakePrim(PrimitiveType.Cube, "CoreSlot", new Vector3(-0.22f, 0.4f, 0f), new Vector3(0.05f, 0.2f, 0.2f), m.KeypadDark, gen.transform);

        // Rotate generator to face into the room
        gen.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        var gt = body.AddComponent<GeneratorTerminal>();
        gt.IndicatorRenderer = indicator.GetComponent<Renderer>();
    }

    public static void BuildPuzzle2(Mats m)
    {
        var root = new GameObject("Puzzle2_Keypad").transform;

        // =============================================================
        // Binary server rack on SOUTH wall (replaces the old hint board).
        // Encodes the 4-digit code 7294 across four rows of four drawers.
        // Drawers with Bit=1 extrude from the wall + turn a green LED on
        // once the player restores power. Drawers with Bit=0 stay dormant.
        // Facing +Z (into room); no rotation needed — local axes align with
        // world axes. Player standing inside the room looks toward -Z so
        // their LEFT is world +X → col=0 (MSB, weight 8) maps to local +X.
        // =============================================================
        var arrayRoot = MakeEmpty("BinaryServerArray", new Vector3(0f, 1.9f, -4.95f), root);
        // identity rotation: drawer front faces +Z = into the room

        // Rack backplate (dark recessed panel that drawers are set into)
        MakePrim(PrimitiveType.Cube, "RackBackplate", new Vector3(0f, 0f, -0.05f), new Vector3(2.3f, 2.3f, 0.06f), m.ServerRack, arrayRoot.transform);
        // Outer frame (subtle lighter bevel around the rack)
        MakePrim(PrimitiveType.Cube, "RackFrameTop",    new Vector3( 0f,  1.10f, -0.02f), new Vector3(2.4f, 0.10f, 0.10f), m.GeneratorMetal, arrayRoot.transform);
        MakePrim(PrimitiveType.Cube, "RackFrameBottom", new Vector3( 0f, -1.10f, -0.02f), new Vector3(2.4f, 0.10f, 0.10f), m.GeneratorMetal, arrayRoot.transform);
        MakePrim(PrimitiveType.Cube, "RackFrameLeft",   new Vector3(-1.15f, 0f, -0.02f), new Vector3(0.10f, 2.3f, 0.10f), m.GeneratorMetal, arrayRoot.transform);
        MakePrim(PrimitiveType.Cube, "RackFrameRight",  new Vector3( 1.15f, 0f, -0.02f), new Vector3(0.10f, 2.3f, 0.10f), m.GeneratorMetal, arrayRoot.transform);

        // Password 7294 -> binary (MSB left):
        //   row 0: 7 = 0 1 1 1
        //   row 1: 2 = 0 0 1 0
        //   row 2: 9 = 1 0 0 1
        //   row 3: 4 = 0 1 0 0
        int[,] bits = new int[4, 4] {
            { 0, 1, 1, 1 },
            { 0, 0, 1, 0 },
            { 1, 0, 0, 1 },
            { 0, 1, 0, 0 },
        };

        var array = arrayRoot.AddComponent<BinaryServerArray>();
        array.Drawers = new ServerDrawer[16];

        const float cellSpacing = 0.5f;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                // South-wall mount with identity rotation. Player faces -Z (toward
                // the wall); their LEFT is world +X. We want col=0 (MSB, weight 8)
                // on the player's LEFT so MSB-left numeral reading is preserved.
                float localX = (1.5f - col) * cellSpacing;  // col0:+0.75 (player's left)  col3:-0.75 (player's right)
                float localY = (1.5f - row) * cellSpacing;  // row0:+0.75 (top)            row3:-0.75 (bottom)

                var cellRoot = MakeEmpty("Cell_R" + row + "_C" + col, new Vector3(localX, localY, 0f), arrayRoot.transform);

                // Drawer body: 0.42 x 0.42 x 0.25, centered so front face is flush with wall (z=0) when retracted.
                var body = MakePrim(PrimitiveType.Cube, "Drawer", new Vector3(0f, 0f, -0.125f), new Vector3(0.42f, 0.42f, 0.25f), m.ServerDrawerMat, cellRoot.transform);
                // Thin LED strip on the front face (bottom edge)
                var led = MakePrim(PrimitiveType.Cube, "LED", new Vector3(0f, -0.16f, 0.52f), new Vector3(0.65f, 0.08f, 0.04f), m.ServerLedOff, body.transform);
                // (LED local scale is divided by body scale because it's a child; we used 0.65 x 0.08 x 0.04 = ~0.27 x 0.034 x 0.01 in world.)

                var drawer = body.AddComponent<ServerDrawer>();
                drawer.Bit = bits[row, col] == 1;
                drawer.DrawerBody = body.transform;
                drawer.LedRenderer = led.GetComponent<Renderer>();
                drawer.LedOffMaterial = m.ServerLedOff;
                drawer.LedOnMaterial = m.ServerLedOn;
                array.Drawers[row * 4 + col] = drawer;
            }
        }

        // =============================================================
        // Codebook desk — a short wooden desk with a printed paper note
        // showing the binary-to-decimal legend. Positioned near the
        // player's natural exploration path from spawn to the crates.
        // =============================================================
        BuildCodebookDesk(m, root);

        // Safe on east wall north of generator
        var safeRoot = MakeEmpty("Safe", new Vector3(4.5f, 1.2f, 2.5f), root);
        safeRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        var safeBody = MakePrim(PrimitiveType.Cube, "SafeBody", Vector3.zero, new Vector3(1.0f, 1.0f, 0.6f), m.SafeMetal, safeRoot.transform);
        // Door pivot at left edge of safe (x=-0.5 on body's local axis)
        var doorPivot = MakeEmpty("DoorPivot", new Vector3(-0.5f, 0f, -0.32f), safeRoot.transform);
        var doorPanel = MakePrim(PrimitiveType.Cube, "DoorPanel", new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.9f, 0.04f), m.SafeDoor, doorPivot.transform);
        // Keycard inside safe (initially disabled)
        var keycard = MakePrim(PrimitiveType.Cube, "Keycard", new Vector3(0f, 0f, 0.05f), new Vector3(0.4f, 0.25f, 0.02f), m.KeycardWhite, safeRoot.transform);
        keycard.layer = InteractableLayer;
        keycard.AddComponent<KeycardPickup>();
        keycard.SetActive(false);

        var safeDoor = safeBody.AddComponent<SafeDoor>();
        safeDoor.DoorPivot = doorPivot.transform;
        safeDoor.KeycardObject = keycard;

        // Keypad on west wall (next to hint, lower)
        var keypadRoot = MakeEmpty("Keypad", new Vector3(-4.85f, 1.3f, -2f), root);
        keypadRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        var kpPanel = MakePrim(PrimitiveType.Cube, "Panel", Vector3.zero, new Vector3(1.0f, 1.7f, 0.06f), m.KeypadDark, keypadRoot.transform);
        // Display screen (in front of panel = +Z = into room because keypad is rotated Y=90)
        var displayBg = MakePrim(PrimitiveType.Cube, "DisplayBG", new Vector3(0f, 0.42f, 0.04f), new Vector3(0.7f, 0.18f, 0.02f), m.KeypadDark, keypadRoot.transform);
        var displayTextGO = new GameObject("DisplayText");
        displayTextGO.transform.SetParent(keypadRoot.transform, false);
        displayTextGO.transform.localPosition = new Vector3(0f, 0.42f, 0.06f);
        displayTextGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var displayText = displayTextGO.AddComponent<TextMesh>();
        displayText.text = "____";
        displayText.fontSize = 100;
        displayText.characterSize = 0.04f;
        displayText.color = new Color(0.4f, 1f, 0.5f);
        displayText.anchor = TextAnchor.MiddleCenter;
        displayText.alignment = TextAlignment.Center;

        var puzzle = keypadRoot.AddComponent<KeypadPuzzle>();
        puzzle.TargetCode = "7294";
        puzzle.DisplayText = displayText;
        puzzle.Safe = safeDoor;

        // Build 12 buttons in 4 rows x 3 cols
        // Layout: 1 2 3 / 4 5 6 / 7 8 9 / C 0 E
        string[] labels = new string[] { "1","2","3","4","5","6","7","8","9","C","0","E" };
        float spacing = 0.24f;
        float startX = -spacing;
        float startY = 0.10f;
        for (int i = 0; i < 12; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = startX + col * spacing;
            float y = startY - row * spacing;
            // Buttons protrude into room (+Z in keypad local = +X world)
            var btnGO = MakePrim(PrimitiveType.Cube, "Btn_" + labels[i], new Vector3(x, y, 0.06f), new Vector3(0.18f, 0.18f, 0.05f), m.KeypadButton, keypadRoot.transform);
            btnGO.layer = InteractableLayer;
            var btn = btnGO.AddComponent<KeypadButton>();
            btn.Puzzle = puzzle;
            btn.Value = labels[i];
            btn.ButtonRenderer = btnGO.GetComponent<Renderer>();

            // Label parented to keypadRoot (NOT the button) to avoid non-uniform parent scale distortion
            var lblGO = new GameObject("Label_" + labels[i]);
            lblGO.transform.SetParent(keypadRoot.transform, false);
            lblGO.transform.localPosition = new Vector3(x, y, 0.10f);
            lblGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            lblGO.transform.localScale = Vector3.one;
            var t = lblGO.AddComponent<TextMesh>();
            t.text = labels[i];
            t.color = labels[i] == "E" ? new Color(0.2f, 1f, 0.4f) : (labels[i] == "C" ? new Color(1f, 0.4f, 0.2f) : Color.black);
            t.characterSize = 0.025f;
            t.fontSize = 100;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
        }
    }

    // =============================================================
    // Codebook desk — pushed against the east wall. The access legend
    // is printed on a flat paper sheet laid directly on the desk top,
    // so the player reads it by looking down rather than face-on. This
    // avoids any TextMesh orientation ambiguity and matches the look of
    // an in-universe developer handbook lying on a workbench.
    // =============================================================
    static void BuildCodebookDesk(Mats m, Transform root)
    {
        // East wall inside face is at x = 5.0. Desk depth (along X) = 0.8 m.
        // Centre at x = 4.55 so the desk back touches the wall (right edge at
        // x = 4.95, 5 cm margin). Desk length along Z is 1.2 m between the
        // generator (z = -3) and the safe (z = +2.5).
        var deskRoot = MakeEmpty("CodebookDesk", new Vector3(4.55f, 0f, 0f), root);

        // Desk top: 0.8 m deep × 0.06 m thick × 1.2 m long
        MakePrim(PrimitiveType.Cube, "Top", new Vector3(0f, 0.78f, 0f), new Vector3(0.8f, 0.06f, 1.2f), m.DeskWood, deskRoot.transform);

        // Four legs at the underside corners of the desk top.
        float lx = 0.36f, lz = 0.56f;
        MakePrim(PrimitiveType.Cube, "LegFL", new Vector3(-lx, 0.375f, -lz), new Vector3(0.06f, 0.75f, 0.06f), m.DeskWood, deskRoot.transform);
        MakePrim(PrimitiveType.Cube, "LegFR", new Vector3(-lx, 0.375f,  lz), new Vector3(0.06f, 0.75f, 0.06f), m.DeskWood, deskRoot.transform);
        MakePrim(PrimitiveType.Cube, "LegBL", new Vector3( lx, 0.375f, -lz), new Vector3(0.06f, 0.75f, 0.06f), m.DeskWood, deskRoot.transform);
        MakePrim(PrimitiveType.Cube, "LegBR", new Vector3( lx, 0.375f,  lz), new Vector3(0.06f, 0.75f, 0.06f), m.DeskWood, deskRoot.transform);

        // Paper sheet lying flat on the desk. 2 mm thick so the legend that
        // sits a fraction of a millimetre above it reads as printed, not
        // floating. Sized slightly larger than the visible text so there is
        // a clear paper border around every glyph.
        MakePrim(PrimitiveType.Cube, "Paper", new Vector3(-0.06f, 0.813f, 0f), new Vector3(0.45f, 0.002f, 0.35f), m.Paper, deskRoot.transform);

        // Legend text — just the four binary-weight rows, no commentary; the
        // player is expected to work out the meaning on their own.
        //
        // Orientation: TextMesh's readable face is local -Z; rotating X=+90°
        // maps local -Z to world +Y, so the text is readable when looking
        // down at the desk. Local +Y then maps to world +Z → text "top"
        // faces north (matching the approach direction from spawn).
        //
        // Sizing: TextMesh on a dynamic font produces very large glyphs at
        // fontSize=80 / characterSize=0.02 (≈ 0.06 m per character), which
        // overflowed the paper and the desk. The GameObject is uniformly
        // scaled down to 35 % so the widest row of 13 characters lands at
        // ≈ 0.28 m — comfortably inside the 0.45 m paper — with a ≈ 0.11 m
        // block of 4 lines inside the 0.35 m paper depth.
        //
        // Y position is 0.5 mm above the paper top (paper top = 0.814),
        // so the glyphs appear printed on the sheet without z-fighting.
        var textGO = new GameObject("LegendText");
        textGO.transform.SetParent(deskRoot.transform, false);
        textGO.transform.localPosition = new Vector3(-0.06f, 0.8145f, 0f);
        textGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        textGO.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        var tm = textGO.AddComponent<TextMesh>();
        tm.text =
            "[X - - -] = 8\n" +
            "[- X - -] = 4\n" +
            "[- - X -] = 2\n" +
            "[- - - X] = 1";
        tm.characterSize = 0.02f;
        tm.fontSize = 80;
        tm.color = new Color(0.08f, 0.06f, 0.04f);
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        // Warm desk lamp so the desk remains a visible focal point even under
        // red alarm lighting. Positioned at the back-right corner so it stays
        // out of the player's reading line of sight.
        var lampGO = new GameObject("DeskLamp");
        lampGO.transform.SetParent(deskRoot.transform, false);
        lampGO.transform.localPosition = new Vector3(0.28f, 1.35f, 0.48f);
        var lamp = lampGO.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1.0f, 0.85f, 0.55f);
        lamp.intensity = 1.0f;
        lamp.range = 3.0f;
        var bulb = MakePrim(PrimitiveType.Sphere, "Bulb", Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), m.IndicatorYellow, lampGO.transform);
        Object.DestroyImmediate(bulb.GetComponent<Collider>());

        // Thin vertical lamp stand from desk surface up to the bulb.
        MakePrim(PrimitiveType.Cube, "LampStand", new Vector3(0.28f, 1.05f, 0.48f), new Vector3(0.04f, 0.5f, 0.04f), m.GeneratorMetal, deskRoot.transform);
    }

    public static void BuildPuzzle3(Mats m)
    {
        var root = new GameObject("Puzzle3_BlastDoor").transform;

        // Two door panels in the gap (from x=-1 to x=1, full height 3m)
        var leftDoor = MakePrim(PrimitiveType.Cube, "LeftDoor", new Vector3(-0.5f, 1.5f, 5.05f), new Vector3(1.0f, 3.0f, 0.15f), m.BlastDoor, root);
        var rightDoor = MakePrim(PrimitiveType.Cube, "RightDoor", new Vector3(0.5f, 1.5f, 5.05f), new Vector3(1.0f, 3.0f, 0.15f), m.BlastDoor, root);

        // Door terminal (panel on south side of door wall, inside the room)
        var terminal = MakeEmpty("DoorTerminal", new Vector3(2.0f, 1.4f, 4.85f), root);
        terminal.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var tBody = MakePrim(PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.6f, 0.8f, 0.1f), m.GeneratorMetal, terminal.transform);
        tBody.layer = InteractableLayer;
        var tIndicator = MakePrim(PrimitiveType.Sphere, "Indicator", new Vector3(0f, 0.25f, -0.06f), new Vector3(0.12f, 0.12f, 0.12f), m.IndicatorRed, terminal.transform);
        Object.DestroyImmediate(tIndicator.GetComponent<Collider>());

        // Victory trigger behind doors
        var trigGO = new GameObject("VictoryTrigger");
        trigGO.transform.SetParent(root, false);
        trigGO.transform.localPosition = new Vector3(0f, 1.1f, 6.5f);
        var bc = trigGO.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(2.0f, 2.2f, 1.5f);
        trigGO.AddComponent<VictoryTrigger>();

        var ctl = tBody.AddComponent<BlastDoorController>();
        ctl.LeftDoor = leftDoor.transform;
        ctl.RightDoor = rightDoor.transform;
        ctl.SlideDistance = 1.05f;
        ctl.IndicatorRenderer = tIndicator.GetComponent<Renderer>();
        ctl.VictoryTriggerObject = trigGO;
    }

    static void WireUpManagers(GameStateManager gsm, LightingRefs lighting, CanvasRefs canvases, HUDController hud, MenuManager menu)
    {
        // already wired above, keep as hook
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
            if (current[i].path == scenePath) return;
        var list = new List<EditorBuildSettingsScene>(current);
        list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
