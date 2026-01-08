using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class FinancialItem
{
    public string details;
    public float amount;
    
    public FinancialItem(string details, float amount)
    {
        this.details = details;
        this.amount = amount;
    }
}

public class PlayerFinance : MonoBehaviour
{
    [Header("Income Items")]
    [SerializeField] private List<FinancialItem> incomeItems = new List<FinancialItem>();
    
    [Header("Expense Items")]
    [SerializeField] private List<FinancialItem> expenseItems = new List<FinancialItem>();
    
    [Header("Cash Settings")]
    [Tooltip("Initial cash amount when game starts")]
    [SerializeField] private float initialCash = 0f;
    
    private float currentCash = 0f;
    
    // Public properties - Auto-calculated from items
    public float TotalIncome 
    { 
        get { return incomeItems != null ? incomeItems.Sum(item => item.amount) : 0f; }
    }
    
    public float TotalExpenses 
    { 
        get { return expenseItems != null ? expenseItems.Sum(item => item.amount) : 0f; }
    }
    
    public float CurrentPayday => TotalIncome - TotalExpenses;
    
    public float CurrentCash => currentCash;
    
    // Events that fire when income, expenses, or cash change
    public System.Action<float> OnPaydayChanged;
    public System.Action<float> OnCashChanged;
    
    // Read-only access to income and expense lists
    public IReadOnlyList<FinancialItem> IncomeItems => incomeItems;
    public IReadOnlyList<FinancialItem> ExpenseItems => expenseItems;
    
    void Start()
    {
        // Initialize with default values if needed
        if (incomeItems == null) incomeItems = new List<FinancialItem>();
        if (expenseItems == null) expenseItems = new List<FinancialItem>();
        
        // Initialize cash with initial value
        currentCash = initialCash;
        OnCashChanged?.Invoke(currentCash);
    }
    
    /// <summary>
    /// Adds an income item with details and amount
    /// </summary>
    public void AddIncomeItem(string details, float amount)
    {
        if (!string.IsNullOrEmpty(details) && amount > 0)
        {
            incomeItems.Add(new FinancialItem(details, amount));
            Debug.Log($"Income item added: {details} - {amount}. Total Income: {TotalIncome}");
            OnPaydayChanged?.Invoke(CurrentPayday);
        }
    }
    
    /// <summary>
    /// Adds an expense item with details and amount
    /// </summary>
    public void AddExpenseItem(string details, float amount)
    {
        if (!string.IsNullOrEmpty(details) && amount > 0)
        {
            expenseItems.Add(new FinancialItem(details, amount));
            Debug.Log($"Expense item added: {details} - {amount}. Total Expenses: {TotalExpenses}");
            OnPaydayChanged?.Invoke(CurrentPayday);
        }
    }
    
    /// <summary>
    /// Removes an income item by details (removes first matching item)
    /// </summary>
    public bool RemoveIncomeItem(string details)
    {
        var item = incomeItems.FirstOrDefault(i => i.details == details);
        if (item != null)
        {
            incomeItems.Remove(item);
            Debug.Log($"Income item removed: {details}. Total Income: {TotalIncome}");
            OnPaydayChanged?.Invoke(CurrentPayday);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes an expense item by details (removes first matching item)
    /// </summary>
    public bool RemoveExpenseItem(string details)
    {
        var item = expenseItems.FirstOrDefault(i => i.details == details);
        if (item != null)
        {
            expenseItems.Remove(item);
            Debug.Log($"Expense item removed: {details}. Total Expenses: {TotalExpenses}");
            OnPaydayChanged?.Invoke(CurrentPayday);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Updates an income item's amount by details (updates first matching item)
    /// </summary>
    public bool UpdateIncomeItem(string details, float newAmount)
    {
        var item = incomeItems.FirstOrDefault(i => i.details == details);
        if (item != null && newAmount > 0)
        {
            item.amount = newAmount;
            Debug.Log($"Income item updated: {details} - {newAmount}. Total Income: {TotalIncome}");
            OnPaydayChanged?.Invoke(CurrentPayday);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Updates an expense item's amount by details (updates first matching item)
    /// </summary>
    public bool UpdateExpenseItem(string details, float newAmount)
    {
        var item = expenseItems.FirstOrDefault(i => i.details == details);
        if (item != null && newAmount > 0)
        {
            item.amount = newAmount;
            Debug.Log($"Expense item updated: {details} - {newAmount}. Total Expenses: {TotalExpenses}");
            OnPaydayChanged?.Invoke(CurrentPayday);
            return true;
        }
        return false;
    }
    
    // Legacy methods for backward compatibility
    /// <summary>
    /// Adds income to the player's total income (legacy method - creates item with "Income" as details)
    /// </summary>
    [System.Obsolete("Use AddIncomeItem(string details, float amount) instead")]
    public void AddIncome(float amount)
    {
        AddIncomeItem("Income", amount);
    }
    
    /// <summary>
    /// Adds expense to the player's total expenses (legacy method - creates item with "Expense" as details)
    /// </summary>
    [System.Obsolete("Use AddExpenseItem(string details, float amount) instead")]
    public void AddExpense(float amount)
    {
        AddExpenseItem("Expense", amount);
    }
    
    /// <summary>
    /// Adds cash to the player's current cash
    /// </summary>
    public void AddCash(float amount)
    {
        if (amount > 0)
        {
            currentCash += amount;
            Debug.Log($"Cash added: {amount}. Current Cash: {currentCash}");
            OnCashChanged?.Invoke(currentCash);
        }
    }
    
    /// <summary>
    /// Subtracts cash from the player's current cash
    /// </summary>
    public bool SubtractCash(float amount)
    {
        if (amount > 0)
        {
            if (currentCash >= amount)
            {
                currentCash -= amount;
                Debug.Log($"Cash subtracted: {amount}. Current Cash: {currentCash}");
                OnCashChanged?.Invoke(currentCash);
                return true;
            }
            else
            {
                Debug.LogWarning($"Not enough cash! Need {amount}, have {currentCash}");
                return false;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Adds CurrentPayday to cash (called when player passes Path01_Start)
    /// </summary>
    public void AddPaydayToCash()
    {
        float paydayAmount = CurrentPayday;
        if (paydayAmount > 0)
        {
            AddCash(paydayAmount);
            Debug.Log($"Added CurrentPayday ({paydayAmount}) to cash. New Cash: {currentCash}");
        }
    }
    
    /// <summary>
    /// Resets all financial data (useful for new game or testing)
    /// </summary>
    public void ResetFinance()
    {
        incomeItems.Clear();
        expenseItems.Clear();
        currentCash = initialCash;
        OnPaydayChanged?.Invoke(CurrentPayday);
        OnCashChanged?.Invoke(currentCash);
        Debug.Log("Player finance data reset.");
    }
    
    /// <summary>
    /// Gets a summary of the player's financial status
    /// </summary>
    public string GetFinanceSummary()
    {
        string summary = $"Total Income: {TotalIncome:F2}\n" +
                         $"Total Expenses: {TotalExpenses:F2}\n" +
                         $"Current Payday: {CurrentPayday:F2}\n\n";
        
        summary += "Income Items:\n";
        foreach (var item in incomeItems)
        {
            summary += $"  - {item.details}: {item.amount:F2}\n";
        }
        
        summary += "\nExpense Items:\n";
        foreach (var item in expenseItems)
        {
            summary += $"  - {item.details}: {item.amount:F2}\n";
        }
        
        return summary;
    }
}
