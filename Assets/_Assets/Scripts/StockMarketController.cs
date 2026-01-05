using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StockMarketController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI stockValueText;
    
    [Header("3D Plane Reference")]
    [SerializeField] private Transform planeTransform;
    [SerializeField] private float lineHeightOffset = 0.1f; // Height above the plane
    [SerializeField] private float graphWidth = 10f; // Width of the graph on the plane
    [SerializeField] private float graphHeight = 5f; // Height of the graph on the plane
    
    [Header("Stock Settings")]
    [SerializeField] private float initialMoney = 2000f;
    [SerializeField] private float baseStockValue = 100f;
    [SerializeField] private float volatility = 3f;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private int maxDataPoints = 50;
    [SerializeField] private Color upColor = Color.green;
    [SerializeField] private Color downColor = Color.red;
    [SerializeField] private float lineWidth = 0.02f;
    
    private float currentMoney;
    private float currentStockValue;
    private float previousStockValue;
    private List<float> stockHistory = new List<float>();
    private float lastUpdateTime = 0f;
    
    // Line Renderer for drawing the stock line
    private LineRenderer stockLineRenderer;
    private GameObject lineObject;
    
    void Start()
    {
        // Initialize values
        currentMoney = initialMoney;
        currentStockValue = baseStockValue;
        
        // Find buttons if not assigned
        if (buyButton == null)
        {
            buyButton = GameObject.Find("BuyButton")?.GetComponent<Button>();
        }
        
        if (sellButton == null)
        {
            sellButton = GameObject.Find("SellButton")?.GetComponent<Button>();
        }
        
        // Find Plane if not assigned
        if (planeTransform == null)
        {
            GameObject planeObj = GameObject.Find("Plane");
            if (planeObj != null)
            {
                planeTransform = planeObj.transform;
            }
        }
        
        // Setup button listeners
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellClicked);
        }
        
        // Initialize stock history
        for (int i = 0; i < maxDataPoints; i++)
        {
            stockHistory.Add(baseStockValue);
        }
        
        // Setup line renderer
        SetupLineRenderer();
        
        UpdateUI();
    }
    
    void Update()
    {
        // Update stock value at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStockValue();
            DrawStockLine();
            UpdateUI();
            lastUpdateTime = Time.time;
        }
    }
    
    private void SetupLineRenderer()
    {
        if (planeTransform == null) return;
        
        // Create GameObject for line renderer
        lineObject = new GameObject("StockLine");
        lineObject.transform.SetParent(planeTransform, false);
        lineObject.transform.localPosition = Vector3.zero;
        
        // Add LineRenderer component
        stockLineRenderer = lineObject.AddComponent<LineRenderer>();
        stockLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        stockLineRenderer.startColor = upColor;
        stockLineRenderer.endColor = upColor;
        stockLineRenderer.startWidth = lineWidth;
        stockLineRenderer.endWidth = lineWidth;
        stockLineRenderer.useWorldSpace = true; // Use world space for 3D
    }
    
    private void UpdateStockValue()
    {
        // Store previous value before updating
        previousStockValue = currentStockValue;
        
        // Random movement for stock price (random walk)
        float change = Random.Range(-volatility, volatility);
        currentStockValue += change;
        
        // Keep stock value reasonable (minimum $10)
        currentStockValue = Mathf.Max(10f, currentStockValue);
        
        // Add to history
        stockHistory.Add(currentStockValue);
        
        // Remove oldest if we exceed max
        if (stockHistory.Count > maxDataPoints)
        {
            stockHistory.RemoveAt(0);
        }
    }
    
    private void DrawStockLine()
    {
        if (stockLineRenderer == null || planeTransform == null || stockHistory.Count < 2)
            return;
        
        // Calculate min and max values for scaling
        float minValue = Mathf.Min(stockHistory.ToArray());
        float maxValue = Mathf.Max(stockHistory.ToArray());
        float range = maxValue - minValue;
        
        // Prevent division by zero
        if (range < 0.1f)
        {
            range = 0.1f;
            minValue = currentStockValue - 0.05f;
            maxValue = currentStockValue + 0.05f;
        }
        
        // Set number of points
        int pointCount = stockHistory.Count;
        stockLineRenderer.positionCount = pointCount;
        
        // Create points for the line in world space
        Vector3[] positions = new Vector3[pointCount];
        
        // Get plane's world position and rotation
        Vector3 planePosition = planeTransform.position;
        Quaternion planeRotation = planeTransform.rotation;
        
        for (int i = 0; i < pointCount; i++)
        {
            // Normalize value (0 to 1)
            float normalizedValue = (stockHistory[i] - minValue) / range;
            
            // Convert to local coordinates relative to plane
            // X: spread across graph width (left to right)
            // Y: map to graph height (bottom to top)
            // Z: height offset above the plane
            float localX = (i / (float)(pointCount - 1)) * graphWidth - graphWidth * 0.5f;
            float localY = normalizedValue * graphHeight;
            float localZ = lineHeightOffset;
            
            // Create local position
            Vector3 localPos = new Vector3(localX, localZ, localY);
            
            // Transform to world space (accounting for plane rotation)
            positions[i] = planePosition + planeRotation * localPos;
        }
        
        // Update line renderer
        stockLineRenderer.SetPositions(positions);
        
        // Create gradient with colors based on each segment's direction
        UpdateLineGradient();
    }
    
    private void UpdateLineGradient()
    {
        if (stockLineRenderer == null || stockHistory.Count < 2)
            return;
        
        // Create a gradient for the line
        Gradient gradient = new Gradient();
        List<GradientColorKey> colorKeys = new List<GradientColorKey>();
        List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();
        
        int pointCount = stockHistory.Count;
        
        // Add color keys at segment boundaries for better per-segment coloring
        for (int i = 0; i < pointCount - 1; i++)
        {
            // Determine direction of this segment (from point i to point i+1)
            bool goingUp = stockHistory[i + 1] > stockHistory[i];
            Color segmentColor = goingUp ? upColor : downColor;
            
            // Add color key at the start of the segment
            float startTime = i / (float)(pointCount - 1);
            colorKeys.Add(new GradientColorKey(segmentColor, startTime));
            alphaKeys.Add(new GradientAlphaKey(1f, startTime));
            
            // Add color key just before the end of the segment (to prevent blending)
            float endTime = (i + 0.99f) / (float)(pointCount - 1);
            colorKeys.Add(new GradientColorKey(segmentColor, endTime));
            alphaKeys.Add(new GradientAlphaKey(1f, endTime));
        }
        
        // Add final point
        if (pointCount > 1)
        {
            float finalTime = 1f;
            bool finalGoingUp = stockHistory[pointCount - 1] > stockHistory[pointCount - 2];
            Color finalColor = finalGoingUp ? upColor : downColor;
            colorKeys.Add(new GradientColorKey(finalColor, finalTime));
            alphaKeys.Add(new GradientAlphaKey(1f, finalTime));
        }
        
        // Set the gradient
        gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        stockLineRenderer.colorGradient = gradient;
    }
    
    private void OnBuyClicked()
    {
        // Buy 1 share at current price
        if (currentMoney >= currentStockValue)
        {
            currentMoney -= currentStockValue;
            UpdateUI();
            Debug.Log($"Bought 1 share at ${currentStockValue:F2}. Remaining money: ${currentMoney:F2}");
        }
        else
        {
            Debug.LogWarning($"Not enough money! Need ${currentStockValue:F2}, have ${currentMoney:F2}");
        }
    }
    
    private void OnSellClicked()
    {
        // Sell 1 share at current price
        currentMoney += currentStockValue;
        UpdateUI();
        Debug.Log($"Sold 1 share at ${currentStockValue:F2}. Total money: ${currentMoney:F2}");
    }
    
    private void UpdateUI()
    {
        // Update money display
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${currentMoney:F2}";
        }
        
        // Update stock value display
        if (stockValueText != null)
        {
            stockValueText.text = $"Price: ${currentStockValue:F2}";
        }
    }
}
