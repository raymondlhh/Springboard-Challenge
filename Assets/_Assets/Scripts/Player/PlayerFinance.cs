using UnityEngine;
using System.Collections.Generic;

public class PlayerFinance : MonoBehaviour
{
    [Header("Financial Data")]
    [SerializeField] private float totalIncome = 0f;
    [SerializeField] private float totalExpenses = 0f;
    
    [Header("Purchased Items")]
    [SerializeField] private List<string> realEstateItems = new List<string>();
    [SerializeField] private List<string> businessItems = new List<string>();
    [SerializeField] private List<string> stockItems = new List<string>();
    [SerializeField] private List<string> unitTrustItems = new List<string>();
    [SerializeField] private List<string> insuranceItems = new List<string>();
    
    // Public properties
    public float TotalIncome 
    { 
        get { return totalIncome; } 
        private set { totalIncome = value; }
    }
    
    public float TotalExpenses 
    { 
        get { return totalExpenses; } 
        private set { totalExpenses = value; }
    }
    
    public float CurrentPayday => TotalIncome - TotalExpenses;
    
    // Read-only access to item lists
    public IReadOnlyList<string> RealEstateItems => realEstateItems;
    public IReadOnlyList<string> BusinessItems => businessItems;
    public IReadOnlyList<string> StockItems => stockItems;
    public IReadOnlyList<string> UnitTrustItems => unitTrustItems;
    public IReadOnlyList<string> InsuranceItems => insuranceItems;
    
    void Start()
    {
        // Initialize with default values if needed
        if (realEstateItems == null) realEstateItems = new List<string>();
        if (businessItems == null) businessItems = new List<string>();
        if (stockItems == null) stockItems = new List<string>();
        if (unitTrustItems == null) unitTrustItems = new List<string>();
        if (insuranceItems == null) insuranceItems = new List<string>();
    }
    
    /// <summary>
    /// Adds income to the player's total income
    /// </summary>
    public void AddIncome(float amount)
    {
        if (amount > 0)
        {
            TotalIncome += amount;
            Debug.Log($"Income added: {amount}. Total Income: {TotalIncome}");
        }
    }
    
    /// <summary>
    /// Adds expense to the player's total expenses
    /// </summary>
    public void AddExpense(float amount)
    {
        if (amount > 0)
        {
            TotalExpenses += amount;
            Debug.Log($"Expense added: {amount}. Total Expenses: {TotalExpenses}");
        }
    }
    
    /// <summary>
    /// Adds a Real Estate item to the player's inventory
    /// </summary>
    public void AddRealEstate(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !realEstateItems.Contains(itemName))
        {
            realEstateItems.Add(itemName);
            Debug.Log($"Real Estate added: {itemName}");
        }
    }
    
    /// <summary>
    /// Adds a Business item to the player's inventory
    /// </summary>
    public void AddBusiness(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !businessItems.Contains(itemName))
        {
            businessItems.Add(itemName);
            Debug.Log($"Business added: {itemName}");
        }
    }
    
    /// <summary>
    /// Adds a Stock item to the player's inventory
    /// </summary>
    public void AddStock(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !stockItems.Contains(itemName))
        {
            stockItems.Add(itemName);
            Debug.Log($"Stock added: {itemName}");
        }
    }
    
    /// <summary>
    /// Adds a Unit Trust item to the player's inventory
    /// </summary>
    public void AddUnitTrust(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !unitTrustItems.Contains(itemName))
        {
            unitTrustItems.Add(itemName);
            Debug.Log($"Unit Trust added: {itemName}");
        }
    }
    
    /// <summary>
    /// Adds an Insurance item to the player's inventory
    /// </summary>
    public void AddInsurance(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !insuranceItems.Contains(itemName))
        {
            insuranceItems.Add(itemName);
            Debug.Log($"Insurance added: {itemName}");
        }
    }
    
    /// <summary>
    /// Removes a Real Estate item from the player's inventory
    /// </summary>
    public bool RemoveRealEstate(string itemName)
    {
        if (realEstateItems.Remove(itemName))
        {
            Debug.Log($"Real Estate removed: {itemName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes a Business item from the player's inventory
    /// </summary>
    public bool RemoveBusiness(string itemName)
    {
        if (businessItems.Remove(itemName))
        {
            Debug.Log($"Business removed: {itemName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes a Stock item from the player's inventory
    /// </summary>
    public bool RemoveStock(string itemName)
    {
        if (stockItems.Remove(itemName))
        {
            Debug.Log($"Stock removed: {itemName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes a Unit Trust item from the player's inventory
    /// </summary>
    public bool RemoveUnitTrust(string itemName)
    {
        if (unitTrustItems.Remove(itemName))
        {
            Debug.Log($"Unit Trust removed: {itemName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes an Insurance item from the player's inventory
    /// </summary>
    public bool RemoveInsurance(string itemName)
    {
        if (insuranceItems.Remove(itemName))
        {
            Debug.Log($"Insurance removed: {itemName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Resets all financial data (useful for new game or testing)
    /// </summary>
    public void ResetFinance()
    {
        TotalIncome = 0f;
        TotalExpenses = 0f;
        realEstateItems.Clear();
        businessItems.Clear();
        stockItems.Clear();
        unitTrustItems.Clear();
        insuranceItems.Clear();
        Debug.Log("Player finance data reset.");
    }
    
    /// <summary>
    /// Gets a summary of the player's financial status
    /// </summary>
    public string GetFinanceSummary()
    {
        return $"Total Income: {TotalIncome:F2}\n" +
               $"Total Expenses: {TotalExpenses:F2}\n" +
               $"Current Payday: {CurrentPayday:F2}\n" +
               $"Real Estate Items: {realEstateItems.Count}\n" +
               $"Business Items: {businessItems.Count}\n" +
               $"Stock Items: {stockItems.Count}\n" +
               $"Unit Trust Items: {unitTrustItems.Count}\n" +
               $"Insurance Items: {insuranceItems.Count}";
    }
}
