
using System.Collections.Generic;
using UnityEngine; 

public class ViewerManager : MonoBehaviour
{
    private static ViewerManager instance = null;
    public static ViewerManager Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.FindObjectOfType(typeof(ViewerManager)) as ViewerManager;
            return instance;
        }
    }
     
    public List<GameObject> views;

    private void Start()
    {
        ResetViews();
    }

    void ResetViews()
    {
        foreach (GameObject go in views)
            go.SetActive(false);

        views[0].SetActive(true);
    }

    public void LoadNextView(int index)
    {
        for (int i = 0; i < views.Count; i++)
            views[i].SetActive(i == index);
    } 
}
