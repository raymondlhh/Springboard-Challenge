using UnityEngine;

[CreateAssetMenu(fileName = "BusinessData", menuName = "Game Data/Business Data")]
public class BusinessData : ScriptableObject
{
    [System.Serializable]
    public class BusinessProperty
    {
        [Header("Business Info")]
        public string businessName; // e.g., "Business01", "Business02"
        public string displayName; // e.g., "Online Business"
        
        [Header("Financial Details")]
        public float capital; // Capital required to start the business
        public float cashFlow; // Monthly cash flow from business
        public float incomePerVisit; // Additional income per visit (e.g., +150)
    }
    
    [Header("Business Properties")]
    public BusinessProperty[] properties = new BusinessProperty[12];
    
    /// <summary>
    /// Gets business data by business name (e.g., "Business01", "Business02")
    /// </summary>
    public BusinessProperty GetBusinessByName(string businessName)
    {
        if (properties == null) return null;
        
        foreach (var business in properties)
        {
            if (business != null && business.businessName == businessName)
            {
                return business;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets business data by index (0-11)
    /// </summary>
    public BusinessProperty GetBusinessByIndex(int index)
    {
        if (properties == null || index < 0 || index >= properties.Length)
        {
            return null;
        }
        
        return properties[index];
    }
}
