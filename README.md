Museum 360° Tour — Interaction Layer (Placeholder Content, Mouse-First)
Context

The project already has an imported asset (Assets/Mena-xr.com/360 Tour/) providing a minimal desktop 360° viewer: ViewerManager (switches between panorama sphere "views"), Navigater (mouse-click hotspot that switches view), CursorManager (cursor icon swap), SimpleRotateSphere (drag-to-look). Confirmed via codebase search: no XR/VR packages, no OpenXR/Oculus SDK, no popup/video UI exist yet — this is greenfield for those pieces.

Goal (per user): get the interaction UI working now — teleport points between rooms, an info popup that appears when a point on the panorama is clicked (title + description + optional image + optional video) — using placeholder panoramas, with real photos and the Oculus Quest 2 build to follow later. User explicitly asked to build this mouse-first (they don't have the headset in hand yet) and convert to VR input afterward. So the interaction code must be written so the "click" entry point is a plain public method, not buried in OnMouseUp, so swapping the input source later (XR Interaction Toolkit) doesn't require touching the popup/logic code.

Approach

Reuse the existing asset's patterns (singleton managers via static Instance, OnMouseEnter/Exit/Up for hover+click, CursorManager for hover feedback) rather than introducing a new framework. Add two new scripts and one Editor automation script; avoid hand-editing the fragile .unity YAML scene file directly — instead write an Editor menu command that builds the UI/scene objects via UnityEditor API, so it's re-runnable and inspectable.

1. New runtime scripts (Assets/Mena-xr.com/360 Tour/Scripts/)

InfoPoint.cs — hotspot for showing info, separate from Navigater (which stays as-is for room-to-room teleport):

Serialized fields: title (string), description (string, multiline), image (Sprite, optional), video (VideoClip, optional).
OnMouseEnter/Exit → reuse CursorManager.Instance.SetCursor(...) exactly like Navigater.
OnMouseUp() calls a public Interact() method, which calls InfoPopupManager.Instance.Show(this). Interact() is the seam for a future XR XRSimpleInteractable.selectEntered listener to call instead of OnMouseUp — no popup/logic code changes needed later.

InfoPopupManager.cs — singleton (same Instance pattern as ViewerManager/CursorManager):

Show(InfoPoint data): activates popup panel, sets title/description text, shows/hides the image Image and video RawImage sections depending on whether image/video are assigned, starts VideoPlayer if a clip is present.
Hide(): deactivates panel, stops video.
Holds serialized refs to the popup GameObject, Text fields, Image, RawImage + VideoPlayer, Close Button (wired by the Editor setup script below).
2. Editor automation (Assets/Mena-xr.com/360 Tour/Editor/MuseumTourSetup.cs)

A [MenuItem]-based one-click builder (run from the Unity Editor menu, not by hand-editing scene files):

"Museum Tour/Build Info Popup UI" — creates a Screen Space Overlay Canvas with a popup panel (Title Text, Description Text, content Image, content RawImage + VideoPlayer component targeting a RenderTexture, Close Button), adds InfoPopupManager and wires all serialized references, starts hidden.
"Museum Tour/Build Placeholder Rooms (1-5)" — duplicates the existing panorama sphere ("View") for 5 rooms, cycling the two existing textures (SF Bay copy.png / SF Bay Night copy.png) as stand-ins, places a Navigater teleport hotspot linking each room to the next/previous (reusing the existing "Door Icon" hover art), adds one sample InfoPoint per room with placeholder text (no image/video — user fills those in per-point once real content exists), and registers all rooms in ViewerManager.views.

This keeps room/point authoring data-driven and re-runnable instead of a one-off hand-built scene, and mirrors how the original asset is structured (so it stays easy to follow).

3. Scene

Duplicate Assets/Mena-xr.com/360 Tour/Scenes/Demo.unity → Assets/Scenes/MuseumTour.unity (keep the original Demo scene untouched as reference) and run the two menu commands inside it.

Out of scope for this pass (explicitly deferred per user)
Real panorama photos (placeholders only, easy to swap: just replace the material's texture per room).
Oculus Quest 2 / XR Interaction Toolkit / OpenXR packages and build settings — will be a separate follow-up once mouse-based flow is validated. Interact() seam above is the intended attach point.
Actual video/image content — fields exist and work if assigned, but no real assets are added now.
Verification
Compile check: run Unity 6000.4.3f1 in batch mode against this project (-batchmode -quit -projectPath "C:\Unity\BuxDu Museum" -logFile ...) to confirm no compile errors after adding the scripts.
Manual test (user, in-editor): open MuseumTour.unity, run the two "Museum Tour" menu commands, press Play — drag to look around, click a door-icon hotspot to move between the 5 placeholder rooms, click an info hotspot to see the popup (title/description show; image/video sections stay hidden since none assigned yet), click Close to dismiss.
