using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

/// <summary>
/// Helper component to automatically connect a VideoPlayer to a RawImage for UI video display.
/// Attach this to any GameObject that has both VideoPlayer and RawImage components,
/// or assign them manually in the Inspector.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerRawImageSetup : MonoBehaviour
{
    [Header("Video Player & RawImage Setup")]
    [Tooltip("VideoPlayer component. Will auto-find if not assigned.")]
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Tooltip("RawImage component to display the video. Will auto-find if not assigned.")]
    [SerializeField] private RawImage rawImage;
    
    [Header("Render Texture Settings")]
    [Tooltip("Width of the RenderTexture (default: 1920)")]
    [SerializeField] private int renderTextureWidth = 1920;
    
    [Tooltip("Height of the RenderTexture (default: 1080)")]
    [SerializeField] private int renderTextureHeight = 1080;
    
    [Tooltip("If true, will setup connection automatically on Start")]
    [SerializeField] private bool setupOnStart = true;
    
    private RenderTexture renderTexture;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupConnection();
        }
    }
    
    /// <summary>
    /// Sets up the connection between VideoPlayer and RawImage
    /// </summary>
    public void SetupConnection()
    {
        // Find VideoPlayer if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>();
            }
        }
        
        if (videoPlayer == null)
        {
            Debug.LogError($"[VideoPlayerRawImageSetup] VideoPlayer not found on {gameObject.name}!");
            return;
        }
        
        // Find RawImage if not assigned
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
            if (rawImage == null)
            {
                rawImage = GetComponentInChildren<RawImage>();
            }
        }
        
        if (rawImage == null)
        {
            Debug.LogWarning($"[VideoPlayerRawImageSetup] RawImage not found on {gameObject.name}! Video will not display in UI.");
            return;
        }
        
        // Set VideoPlayer render mode
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create or get RenderTexture
        renderTexture = videoPlayer.targetTexture;
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = $"{gameObject.name}_RenderTexture";
            videoPlayer.targetTexture = renderTexture;
        }
        
        // Assign RenderTexture to RawImage texture
        rawImage.texture = renderTexture;
        
        // Set material texture if using custom material
        if (rawImage.material != null && rawImage.material != rawImage.defaultMaterial)
        {
            rawImage.material.mainTexture = renderTexture;
        }
        
        Debug.Log($"[VideoPlayerRawImageSetup] Successfully connected VideoPlayer to RawImage on {gameObject.name}.");
    }
    
    /// <summary>
    /// Manually assign a VideoPlayer and RawImage, then setup the connection
    /// </summary>
    public void SetupConnection(VideoPlayer player, RawImage image)
    {
        if (player == null || image == null)
        {
            Debug.LogWarning("[VideoPlayerRawImageSetup] VideoPlayer or RawImage is null! Cannot setup connection.");
            return;
        }
        
        videoPlayer = player;
        rawImage = image;
        SetupConnection();
    }
    
    /// <summary>
    /// Clean up RenderTexture when component is destroyed
    /// </summary>
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
    
    // Public getters
    public VideoPlayer VideoPlayer => videoPlayer;
    public RawImage RawImage => rawImage;
    public RenderTexture RenderTexture => renderTexture;
}
