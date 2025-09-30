using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Settlement.API.Models;

public class SettlementTransaction
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public string Reference { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BankTransferId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int RetryCount { get; set; } = 0;
    public Guid? BatchId { get; set; }
}

public class MerchantAccount
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    public string Currency { get; set; } = "NOK";
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastSettlementAt { get; set; }
}

public class SettlementBatch
{
    public Guid Id { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "NOK";
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, PartiallyCompleted, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SuccessfulTransactions { get; set; } = 0;
    public int FailedTransactions { get; set; } = 0;
}