using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// One-click scaffolding for the museum tour placeholder content and popup UI.
// Run from the Unity Editor menu (Museum Tour/...) instead of hand-editing the scene file.
public static class MuseumTourSetup
{
    private const int RoomCount = 5;
    private const string RenderTexturePath = "Assets/Mena-xr.com/360 Tour/Materials/InfoPopupVideoRT.renderTexture";
    private const string PanoramaShaderName = "Museum/PanoramaUnlit";

    // Searches every currently loaded scene (not just the "active" one) so this
    // works regardless of which scene tab happens to be focused when the menu is used.
    private static GameObject FindInLoadedScenes(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject found = FindRecursive(root.transform, name);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    private static GameObject FindRecursive(Transform t, string name)
    {
        if (t.name == name)
            return t.gameObject;

        for (int i = 0; i < t.childCount; i++)
        {
            GameObject found = FindRecursive(t.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void LogLoadedScenesDiagnostic()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Museum Tour Setup diagnostic - loaded scenes: ");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            sb.Append("[" + scene.name + " loaded=" + scene.isLoaded + " path=" + scene.path + " roots=(" +
                string.Join(", ", scene.isLoaded ? scene.GetRootGameObjects().Select(g => g.name).ToArray() : new string[0]) + ")] ");
        }
        Debug.LogWarning(sb.ToString());
    }

    [MenuItem("Museum Tour/Apply Reference Photos To Room 1 And 2")]
    public static void ApplyReferencePhotosToFirstRooms()
    {
        GameObject view0 = FindInLoadedScenes("View (0)");
        GameObject view1 = FindInLoadedScenes("View (1)");
        if (view0 == null || view1 == null)
        {
            Debug.LogError("Museum Tour Setup: could not find 'View (0)' or 'View (1)' in the open scene.");
            LogLoadedScenesDiagnostic();
            return;
        }

        Material room1Material = CreatePanoramaMaterial(
            "Assets/Mena-xr.com/360 Tour/Materials/Room1.mat",
            "Assets/Images/Referens_1.png");
        Material room2Material = CreatePanoramaMaterial(
            "Assets/Mena-xr.com/360 Tour/Materials/Room2.mat",
            "Assets/Images/Referens_2.png");

        if (room1Material == null || room2Material == null)
            return;

        view0.GetComponent<MeshRenderer>().sharedMaterial = room1Material;
        view1.GetComponent<MeshRenderer>().sharedMaterial = room2Material;

        EditorUtility.SetDirty(view0);
        EditorUtility.SetDirty(view1);

        Debug.Log("Museum Tour Setup: applied the two reference photos to Room 1 and Room 2.");
    }

    private static Material CreatePanoramaMaterial(string materialPath, string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError("Museum Tour Setup: could not find texture at " + texturePath);
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find(PanoramaShaderName);
            if (shader == null)
            {
                Debug.LogError("Museum Tour Setup: could not find shader " + PanoramaShaderName);
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = texture;
        material.mainTextureScale = new Vector2(-1f, 1f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    [MenuItem("Museum Tour/Build Placeholder Rooms (1-5)")]
    public static void BuildPlaceholderRooms()
    {
        GameObject view0 = FindInLoadedScenes("View (0)");
        GameObject view1 = FindInLoadedScenes("View (1)");
        Navigater[] existingHotspots = Object.FindObjectsOfType<Navigater>();

        if (view0 == null || view1 == null || existingHotspots.Length == 0)
        {
            Debug.LogError("Museum Tour Setup: could not find 'View (0)', 'View (1)' or an existing teleport hotspot (Navigater) in the open scene. Open a scene duplicated from Demo.unity first.");
            LogLoadedScenesDiagnostic();
            return;
        }

        ViewerManager viewerManager = Object.FindObjectOfType<ViewerManager>();
        if (viewerManager != null && viewerManager.views != null && viewerManager.views.Count >= RoomCount)
        {
            Debug.LogWarning("Museum Tour Setup: ViewerManager already has " + viewerManager.views.Count + " rooms registered. Skipping to avoid duplicating rooms.");
            return;
        }

        GameObject hotspotTemplate = existingHotspots[0].gameObject;
        Material materialA = view0.GetComponent<MeshRenderer>().sharedMaterial;
        Material materialB = view1.GetComponent<MeshRenderer>().sharedMaterial;

        List<GameObject> rooms = new List<GameObject> { view0, view1 };
        for (int i = 2; i < RoomCount; i++)
        {
            GameObject room = Object.Instantiate(view0);
            room.name = "View (" + i + ")";
            room.transform.position = view0.transform.position;
            room.transform.rotation = view0.transform.rotation;
            room.transform.localScale = view0.transform.localScale;
            room.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? materialA : materialB;

            // A fresh clone of View (0) carries no hotspot children, so nothing to strip here.
            rooms.Add(room);
        }

        for (int i = 0; i < rooms.Count - 1; i++)
        {
            CreateTeleportHotspot(hotspotTemplate, rooms[i], i + 1, new Vector3(0f, -0.2f, 0.45f));
            CreateTeleportHotspot(hotspotTemplate, rooms[i + 1], i, new Vector3(0f, -0.2f, -0.45f));
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            CreateInfoPoint(hotspotTemplate, rooms[i], i, new Vector3(0.35f, 0.1f, 0.3f));
        }

        if (viewerManager != null)
        {
            viewerManager.views = rooms;
            EditorUtility.SetDirty(viewerManager);
        }
        else
        {
            Debug.LogWarning("Museum Tour Setup: no ViewerManager found in the scene, rooms were created but not registered.");
        }

        Debug.Log("Museum Tour Setup: built " + rooms.Count + " placeholder rooms with teleport and info hotspots. Positions are generic placeholders - reposition them in the Scene view as needed.");
    }

    private static void CreateTeleportHotspot(GameObject template, GameObject parentRoom, int targetIndex, Vector3 localPos)
    {
        GameObject hotspot = Object.Instantiate(template, parentRoom.transform);
        hotspot.name = "Teleport To (" + targetIndex + ")";
        hotspot.transform.localPosition = localPos;
        hotspot.transform.localRotation = Quaternion.LookRotation(localPos.normalized);

        InfoPoint staleInfo = hotspot.GetComponent<InfoPoint>();
        if (staleInfo != null)
            Object.DestroyImmediate(staleInfo);

        Navigater nav = hotspot.GetComponent<Navigater>();
        if (nav == null)
            nav = hotspot.AddComponent<Navigater>();
        nav.index = targetIndex;
    }

    private static void CreateInfoPoint(GameObject template, GameObject parentRoom, int roomIndex, Vector3 localPos)
    {
        GameObject hotspot = Object.Instantiate(template, parentRoom.transform);
        hotspot.name = "Info Point (Room " + roomIndex + ")";
        hotspot.transform.localPosition = localPos;
        hotspot.transform.localRotation = Quaternion.LookRotation(localPos.normalized);

        Navigater staleNav = hotspot.GetComponent<Navigater>();
        if (staleNav != null)
            Object.DestroyImmediate(staleNav);

        InfoPoint info = hotspot.GetComponent<InfoPoint>();
        if (info == null)
            info = hotspot.AddComponent<InfoPoint>();
        info.title = "Room " + roomIndex + " info";
        info.description = "Add real description text here once content is ready.";
    }

    [MenuItem("Museum Tour/Build Info Popup UI")]
    public static void BuildInfoPopupUI()
    {
        if (Object.FindObjectOfType<InfoPopupManager>() != null)
        {
            Debug.LogWarning("Museum Tour Setup: an InfoPopupManager already exists in this scene. Remove it first to rebuild the popup UI.");
            return;
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasGO = new GameObject("InfoPopupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject dimGO = CreateUIObject("Dim", canvasGO.transform);
        Image dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        StretchFull(dimGO.GetComponent<RectTransform>());

        GameObject panelGO = CreateUIObject("PopupPanel", canvasGO.transform);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(700f, 520f);
        panelRT.anchoredPosition = Vector2.zero;

        GameObject titleGO = CreateUIObject("Title", panelGO.transform);
        Text titleText = titleGO.AddComponent<Text>();
        SetupText(titleText, 28, FontStyle.Bold, TextAnchor.UpperLeft);
        AnchorTopStretch(titleGO.GetComponent<RectTransform>(), 20f, 50f);

        GameObject descGO = CreateUIObject("Description", panelGO.transform);
        Text descText = descGO.AddComponent<Text>();
        SetupText(descText, 20, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 1f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot = new Vector2(0.5f, 1f);
        descRT.anchoredPosition = new Vector2(0f, -80f);
        descRT.sizeDelta = new Vector2(-40f, 150f);

        GameObject imageGO = CreateUIObject("ContentImage", panelGO.transform);
        Image contentImage = imageGO.AddComponent<Image>();
        contentImage.preserveAspect = true;
        RectTransform imageRT = imageGO.GetComponent<RectTransform>();
        imageRT.anchorMin = new Vector2(0.5f, 0f);
        imageRT.anchorMax = new Vector2(0.5f, 0f);
        imageRT.pivot = new Vector2(0.5f, 0f);
        imageRT.sizeDelta = new Vector2(600f, 230f);
        imageRT.anchoredPosition = new Vector2(0f, 70f);
        imageGO.SetActive(false);

        RenderTexture videoRenderTexture = GetOrCreateRenderTexture();
        GameObject videoGO = CreateUIObject("ContentVideo", panelGO.transform);
        RawImage videoDisplay = videoGO.AddComponent<RawImage>();
        videoDisplay.texture = videoRenderTexture;
        VideoPlayer videoPlayer = videoGO.AddComponent<VideoPlayer>();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.playOnAwake = false;
        RectTransform videoRT = videoGO.GetComponent<RectTransform>();
        videoRT.anchorMin = new Vector2(0.5f, 0f);
        videoRT.anchorMax = new Vector2(0.5f, 0f);
        videoRT.pivot = new Vector2(0.5f, 0f);
        videoRT.sizeDelta = new Vector2(600f, 230f);
        videoRT.anchoredPosition = new Vector2(0f, 70f);
        videoGO.SetActive(false);

        GameObject closeButtonGO = CreateUIObject("CloseButton", panelGO.transform);
        Image closeButtonImage = closeButtonGO.AddComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        Button closeButton = closeButtonGO.AddComponent<Button>();
        RectTransform closeButtonRT = closeButtonGO.GetComponent<RectTransform>();
        closeButtonRT.anchorMin = new Vector2(1f, 1f);
        closeButtonRT.anchorMax = new Vector2(1f, 1f);
        closeButtonRT.pivot = new Vector2(1f, 1f);
        closeButtonRT.sizeDelta = new Vector2(40f, 40f);
        closeButtonRT.anchoredPosition = new Vector2(-10f, -10f);

        GameObject closeLabelGO = CreateUIObject("Label", closeButtonGO.transform);
        Text closeLabel = closeLabelGO.AddComponent<Text>();
        SetupText(closeLabel, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        closeLabel.text = "X";
        StretchFull(closeLabelGO.GetComponent<RectTransform>());

        InfoPopupManager manager = canvasGO.AddComponent<InfoPopupManager>();
        manager.panelRoot = panelGO;
        manager.titleText = titleText;
        manager.descriptionText = descText;
        manager.imageRoot = imageGO;
        manager.contentImage = contentImage;
        manager.videoRoot = videoGO;
        manager.videoPlayer = videoPlayer;
        manager.videoDisplay = videoDisplay;
        manager.videoRenderTexture = videoRenderTexture;

        UnityEventTools.AddPersistentListener(closeButton.onClick, manager.Hide);

        panelGO.SetActive(false);

        Selection.activeGameObject = canvasGO;
        EditorUtility.SetDirty(canvasGO);
        Debug.Log("Museum Tour Setup: built the Info Popup UI.");
    }

    private static RenderTexture GetOrCreateRenderTexture()
    {
        RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (existing != null)
            return existing;

        RenderTexture rt = new RenderTexture(1024, 576, 0);
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        return rt;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AnchorTopStretch(RectTransform rt, float topOffset, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -topOffset);
        rt.sizeDelta = new Vector2(-40f, height);
    }

    private static void SetupText(Text text, int size, FontStyle style, TextAnchor anchor)
    {
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
