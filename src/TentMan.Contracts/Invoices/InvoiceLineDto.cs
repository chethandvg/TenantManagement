using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoice line items representing individual charges on an invoice.
/// Each line item corresponds to a specific charge type (rent, utilities, maintenance, etc.).
/// </summary>
public sealed class InvoiceLineDto
{
    /// <summary>
    /// Gets the unique identifier for the invoice line.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the invoice ID this line belongs to.
    /// </summary>
    public Guid InvoiceId { get; init; }
    
    /// <summary>
    /// Gets the charge type ID (RENT, MAINT, ELEC, etc.).
    /// </summary>
    public Guid ChargeTypeId { get; init; }
    
    /// <summary>
    /// Gets the display name of the charge type for UI presentation.
    /// </summary>
    public string ChargeTypeName { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the sequential line number within the invoice (1, 2, 3, etc.).
    /// </summary>
    public int LineNumber { get; init; }
    
    /// <summary>
    /// Gets the description of this charge.
    /// Example: "Monthly Rent - January 2026" or "Electricity (250 units @ ₹3.80/unit)".
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the quantity for this line item.
    /// Typically 1 for rent/maintenance, or units consumed for utilities.
    /// </summary>
    public decimal Quantity { get; init; }
    
    /// <summary>
    /// Gets the unit price for this charge.
    /// For rent: monthly rent amount. For utilities: calculated rate per unit.
    /// </summary>
    public decimal UnitPrice { get; init; }
    
    /// <summary>
    /// Gets the line amount before tax (Quantity × UnitPrice).
    /// This is the base charge amount.
    /// </summary>
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Gets the tax rate percentage applied to this line (e.g., 18 for 18% GST).
    /// Zero if the charge type is not taxable.
    /// </summary>
    public decimal TaxRate { get; init; }
    
    /// <summary>
    /// Gets the calculated tax amount (Amount × TaxRate / 100).
    /// </summary>
    public decimal TaxAmount { get; init; }
    
    /// <summary>
    /// Gets the total amount for this line including tax (Amount + TaxAmount).
    /// </summary>
    public decimal TotalAmount { get; init; }
    
    /// <summary>
    /// Gets optional notes about this line item.
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the source type that generated this line item.
    /// Values: "Rent", "RecurringCharge", "Utility", "Manual".
    /// Used for traceability and audit trail.
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Gets the source entity ID that this line originated from.
    /// For Rent: LeaseTermId
    /// For RecurringCharge: RecurringChargeId
    /// For Utility: UtilityStatementId
    /// Used for complete audit trail and reconciliation.
    /// </summary>
    public Guid? SourceRefId { get; init; }
}
