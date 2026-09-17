using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigater : MonoBehaviour
{
    public int index = 0;

    void OnMouseUp()
    {
        ViewerManager.Instance.LoadNextView(index);
    }

    void OnMouseEnter()
    {
        CursorManager.Instance.SetCursor(CursorType.Hover);
    }

    void OnMouseExit()
    {
        CursorManager.Instance.SetCursor(CursorType.Normal);
    }
}
