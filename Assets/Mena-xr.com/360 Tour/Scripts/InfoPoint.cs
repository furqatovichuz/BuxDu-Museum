using UnityEngine;
using UnityEngine.Video;

public class InfoPoint : MonoBehaviour
{
    [Header("Content")]
    public string title;
    [TextArea(3, 8)]
    public string description;
    public Sprite image;
    public VideoClip video;

    void OnMouseUp()
    {
        Interact();
    }

    void OnMouseEnter()
    {
        CursorManager.Instance.SetCursor(CursorType.Hover);
    }

    void OnMouseExit()
    {
        CursorManager.Instance.SetCursor(CursorType.Normal);
    }

    // Entry point for triggering this info point. Kept separate from
    // OnMouseUp so a future VR interactor (e.g. XR Interaction Toolkit's
    // selectEntered) can call this directly without touching popup logic.
    public void Interact()
    {
        InfoPopupManager.Instance.Show(this);
    }
}
