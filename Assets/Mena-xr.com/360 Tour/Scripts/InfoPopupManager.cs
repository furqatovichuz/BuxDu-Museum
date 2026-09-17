using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class InfoPopupManager : MonoBehaviour
{
    private static InfoPopupManager instance = null;
    public static InfoPopupManager Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.FindObjectOfType(typeof(InfoPopupManager)) as InfoPopupManager;
            return instance;
        }
    }

    [Header("Panel")]
    public GameObject panelRoot;
    public Text titleText;
    public Text descriptionText;

    [Header("Image content")]
    public GameObject imageRoot;
    public Image contentImage;

    [Header("Video content")]
    public GameObject videoRoot;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public RenderTexture videoRenderTexture;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show(InfoPoint data)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = data.title;
        if (descriptionText != null)
            descriptionText.text = data.description;

        bool hasImage = data.image != null;
        if (imageRoot != null)
            imageRoot.SetActive(hasImage);
        if (hasImage && contentImage != null)
            contentImage.sprite = data.image;

        bool hasVideo = data.video != null;
        if (videoRoot != null)
            videoRoot.SetActive(hasVideo);
        if (hasVideo && videoPlayer != null)
        {
            videoPlayer.clip = data.video;
            videoPlayer.Play();
        }
    }

    public void Hide()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
