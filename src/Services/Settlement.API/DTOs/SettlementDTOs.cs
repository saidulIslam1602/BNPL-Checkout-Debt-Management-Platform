using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Settlement.API.DTOs;

public class CreateSettlementRequest
{
    [Required]
    public Guid MerchantId { get; set; }

    [Required]
    [Range(100, 10000000)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "NOK";

    public string? Description { get; set; }

    public DateTime? ExecutionDate { get; set; }

    public bool IsUrgent { get; set; } = false;
}

public class SettlementResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BankTransferId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MerchantAccountRequest
{
    [Required]
    public Guid MerchantId { get; set; }

    [Required]
    [StringLength(200)]
    public string MerchantName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Norwegian bank account numbers must be 11 digits")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? BankName { get; set; }

    public string Currency { get; set; } = "NOK";
}

public class MerchantAccountResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string Currency { get; set; } = "NOK";
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSettlementAt { get; set; }
}

public class SettlementBatchResponse
{
    public Guid Id { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "NOK";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
}

public class SettlementSummaryResponse
{
    public Guid MerchantId { get; set; }
    public int TotalSettlements { get; set; }
    public decimal TotalAmount { get; set; }
    public int PendingSettlements { get; set; }
    public decimal PendingAmount { get; set; }
    public int CompletedSettlements { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedSettlements { get; set; }
    public DateTime? LastSettlementAt { get; set; }
}