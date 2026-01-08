using UnityEngine;

[CreateAssetMenu(fileName = "RealEstateData", menuName = "Game Data/Real Estate Data")]
public class RealEstateData : ScriptableObject
{
    [System.Serializable]
    public class RealEstateProperty
    {
        [Header("Property Info")]
        public string propertyName; // e.g., "RealEstate01", "RealEstate02"
        public string displayName; // e.g., "Residential: Condominium"
        
        [Header("Financial Details")]
        public float value; // Total property value
        public float downpayment; // Downpayment required
        public float income; // Monthly income from property
        public float incomePerVisit; // Additional income per visit (e.g., +50)
    }
    
    [Header("Real Estate Properties")]
    public RealEstateProperty[] properties = new RealEstateProperty[12];
    
    /// <summary>
    /// Gets property data by property name (e.g., "RealEstate01", "RealEstate02")
    /// </summary>
    public RealEstateProperty GetPropertyByName(string propertyName)
    {
        if (properties == null) return null;
        
        foreach (var property in properties)
        {
            if (property != null && property.propertyName == propertyName)
            {
                return property;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets property data by index (0-11)
    /// </summary>
    public RealEstateProperty GetPropertyByIndex(int index)
    {
        if (properties == null || index < 0 || index >= properties.Length)
        {
            return null;
        }
        
        return properties[index];
    }
}
