using System;

namespace NIDGO_POS
{
    /// <summary>
    /// Represents the currently authenticated user session.
    /// </summary>
    public class UserSession
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents an active line item in the checkout shopping cart.
    /// </summary>
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => Price * Quantity;
    }

    /// <summary>
    /// Represents a catalog product record.
    /// </summary>
    public class Product
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int StockQty { get; set; }
        public int ReorderLevel { get; set; }
        public string ImagePath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a product category item.
    /// </summary>
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a registered store customer.
    /// </summary>
    public class Customer
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a completed or voided transaction header.
    /// </summary>
    public class Order
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int UserId { get; set; }
        public string CashierUsername { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeDue { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Represents individual purchased line items under an order.
    /// </summary>
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    /// <summary>
    /// Represents an inventory adjustment or transaction stock audit entry.
    /// </summary>
    public class InventoryLog
    {
        public int LogId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int ChangeQty { get; set; }
        public string Reason { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}