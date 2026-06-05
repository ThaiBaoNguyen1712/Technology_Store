using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tech_Store.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string Gateway { get; set; } = null!;

    public string? AccountNumber { get; set; }

    public string? SubAccount { get; set; }

    [Column(TypeName = "decimal(20,2)")]
    public decimal AmountIn { get; set; }

    [Column(TypeName = "decimal(20,2)")]
    public decimal AmountOut { get; set; }

    [Column(TypeName = "decimal(20,2)")]
    public decimal Accumulated { get; set; }

    public string? Code { get; set; }

    public string? TransactionContent { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Body { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }
}
