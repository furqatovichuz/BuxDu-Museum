using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CursorType { Normal, Drag, Hover }
public class CursorManager : MonoBehaviour
{
    private static CursorManager instance = null;
    public static CursorManager Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.FindObjectOfType(typeof(CursorManager)) as CursorManager;
            return instance;
        }
    }

    public Texture2D cursorNormal;
    public Texture2D cursorDrag;
    public Texture2D cursorHover;
    private CursorMode cursorMode = CursorMode.ForceSoftware;
    private Vector2 hotSpot = new Vector2(5,5);

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            SetCursor(CursorType.Drag);
        if (Input.GetMouseButtonUp(0))
            SetCursor(CursorType.Normal);
    }

    public void SetCursor(CursorType status)
    {
        switch (status)
        {
            case CursorType.Normal:
                Cursor.SetCursor(cursorNormal, hotSpot, cursorMode);
                break;
            case CursorType.Drag:
                Cursor.SetCursor(cursorDrag, hotSpot, cursorMode);
                break;
            case CursorType.Hover:
                Cursor.SetCursor(cursorHover, hotSpot, cursorMode);
                break;
        }
    }
}
