using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NIDGO_POS
{
    public sealed class CheckoutForm : Form
    {
        private readonly UserSession session;
        private readonly List<CartItem> cart = new List<CartItem>();

        // Controls
        private readonly TextBox txtBarcode = new TextBox { Width = 180 };
        private readonly NumericUpDown numQty = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1 };
        private readonly ComboBox cmbCustomer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        private readonly DataGridView grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, AllowUserToAddRows = false };

        private readonly Label lblSubtotal = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184) };
        private readonly TextBox txtDiscount = new TextBox { Width = 80, Text = "0.00" };
        private readonly TextBox txtTaxRate = new TextBox { Width = 60, Text = "12.00" }; // Default VAT %
        private readonly Label lblTotal = new Label { AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(52, 211, 153) };
        private readonly Label lblChange = new Label { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(251, 191, 36) };
        private readonly TextBox txtPaid = new TextBox { Width = 100, Text = "0.00" };
        private readonly ComboBox cbPayment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };

        public CheckoutForm(UserSession user)
        {
            session = user;
            Text = "Point of Sale - Shopping Cart & Terminal Checkout";
            Width = 1100; Height = 680;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(15, 23, 42); // Dark Charcoal Canvas

            KeyPreview = true;
            KeyDown += CheckoutForm_KeyDown;

            cbPayment.Items.AddRange(new object[] { "Cash", "Card", "Mobile" });
            cbPayment.SelectedIndex = 0;

            LoadCustomers();

            // ==========================================
            // 1. TOP TOOLBAR (BARCODE, QTY, CUSTOMER)
            // ==========================================
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15, 12, 15, 10), BackColor = Color.FromArgb(30, 41, 59) };

            Label lblCode = new Label { Text = "BARCODE / SKU: ", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(96, 165, 250), Padding = new Padding(0, 5, 0, 0) };
            txtBarcode.Font = new Font("Segoe UI", 10f);
            txtBarcode.BackColor = Color.FromArgb(15, 23, 42);
            txtBarcode.ForeColor = Color.White;
            txtBarcode.BorderStyle = BorderStyle.FixedSingle;
            txtBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddToCart(null, null);
                }
            };

            Label lblQty = new Label { Text = "  QTY: ", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 5, 0, 0) };
            numQty.Font = new Font("Segoe UI", 9.5f);
            numQty.Width = 55;

            Button btnAdd = new Button
            {
                Text = "+ Add Item",
                Width = 90,
                Height = 28,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += AddToCart;

            Button btnRemoveLine = new Button
            {
                Text = "Remove Line",
                Width = 95,
                Height = 28,
                BackColor = Color.FromArgb(225, 29, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnRemoveLine.FlatAppearance.BorderSize = 0;
            btnRemoveLine.Click += BtnRemoveLine_Click;

            Label lblCust = new Label { Text = "   CUSTOMER: ", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(226, 232, 240), Padding = new Padding(0, 5, 0, 0) };
            cmbCustomer.Font = new Font("Segoe UI", 9.5f);

            FlowLayoutPanel topControls = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
            topControls.Controls.Add(lblCode);
            topControls.Controls.Add(txtBarcode);
            topControls.Controls.Add(lblQty);
            topControls.Controls.Add(numQty);
            topControls.Controls.Add(btnAdd);
            topControls.Controls.Add(btnRemoveLine);
            topControls.Controls.Add(lblCust);
            topControls.Controls.Add(cmbCustomer);

            topPanel.Controls.Add(topControls);

            // ==========================================
            // 2. BOTTOM SUMMARY & CHECKOUT BAR
            // ==========================================
            Panel footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(15, 10, 20, 10), BackColor = Color.FromArgb(30, 41, 59) };

            Button btnCheckout = new Button
            {
                Text = "CHECKOUT & PRINT (F12)",
                Height = 45,
                Width = 210,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += PerformCheckout;

            txtDiscount.Font = new Font("Segoe UI", 9.5f);
            txtDiscount.BackColor = Color.FromArgb(15, 23, 42);
            txtDiscount.ForeColor = Color.White;
            txtDiscount.BorderStyle = BorderStyle.FixedSingle;
            txtDiscount.TextChanged += (s, e) => CalculateTotals();

            txtTaxRate.Font = new Font("Segoe UI", 9.5f);
            txtTaxRate.BackColor = Color.FromArgb(15, 23, 42);
            txtTaxRate.ForeColor = Color.White;
            txtTaxRate.BorderStyle = BorderStyle.FixedSingle;
            txtTaxRate.TextChanged += (s, e) => CalculateTotals();

            txtPaid.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            txtPaid.BackColor = Color.FromArgb(15, 23, 42);
            txtPaid.ForeColor = Color.White;
            txtPaid.BorderStyle = BorderStyle.FixedSingle;
            txtPaid.TextChanged += (s, e) => CalculateTotals();

            cbPayment.Font = new Font("Segoe UI", 9.5f);

            // Left Calculation Panel
            TableLayoutPanel calcGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 830,
                ColumnCount = 4,
                RowCount = 2
            };
            calcGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            calcGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            calcGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            calcGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            calcGrid.Controls.Add(lblSubtotal, 0, 0);

            FlowLayoutPanel discBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            discBox.Controls.Add(new Label { Text = "Disc (₱): ", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 3, 0, 0) });
            discBox.Controls.Add(txtDiscount);
            calcGrid.Controls.Add(discBox, 1, 0);

            FlowLayoutPanel taxBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            taxBox.Controls.Add(new Label { Text = "Tax (%): ", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 3, 0, 0) });
            taxBox.Controls.Add(txtTaxRate);
            calcGrid.Controls.Add(taxBox, 2, 0);

            calcGrid.Controls.Add(lblTotal, 0, 1);

            FlowLayoutPanel payBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            payBox.Controls.Add(new Label { Text = "Method: ", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 3, 0, 0) });
            payBox.Controls.Add(cbPayment);
            calcGrid.Controls.Add(payBox, 1, 1);

            FlowLayoutPanel paidBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            paidBox.Controls.Add(new Label { Text = "Paid (₱): ", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 3, 0, 0) });
            paidBox.Controls.Add(txtPaid);
            calcGrid.Controls.Add(paidBox, 2, 1);

            calcGrid.Controls.Add(lblChange, 3, 1);

            footerPanel.Controls.Add(btnCheckout);
            footerPanel.Controls.Add(calcGrid);

            // ==========================================
            // 3. CART GRID VIEW
            // ==========================================
            grid.BackgroundColor = Color.FromArgb(15, 23, 42);
            grid.GridColor = Color.FromArgb(51, 65, 85);
            grid.BorderStyle = BorderStyle.None;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(226, 232, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 35;

            grid.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 64, 175);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.RowTemplate.Height = 30;

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            gridContainer.Controls.Add(grid);

            Controls.Add(gridContainer);
            Controls.Add(footerPanel);
            Controls.Add(topPanel);

            Shown += (s, e) => FocusBarcodeField();
            RefreshGrid();
        }

        private void LoadCustomers()
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT CustomerId, FullName FROM dbo.Customers ORDER BY FullName ASC", conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataRow walkIn = dt.NewRow();
                    walkIn["CustomerId"] = DBNull.Value;
                    walkIn["FullName"] = "Walk-in Customer";
                    dt.Rows.InsertAt(walkIn, 0);

                    cmbCustomer.DataSource = dt;
                    cmbCustomer.DisplayMember = "FullName";
                    cmbCustomer.ValueMember = "CustomerId";
                }
                catch { }
            }
        }

        private void CheckoutForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                PerformCheckout(null, null);
            }
            else if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F2)
            {
                FocusBarcodeField();
            }
        }

        private void AddToCart(object sender, EventArgs e)
        {
            string code = txtBarcode.Text.Trim();

            // If barcode field is empty, check if a row is currently selected in the grid to update its quantity
            if (string.IsNullOrEmpty(code))
            {
                if (grid.SelectedRows.Count > 0)
                {
                    int selectedProductId = Convert.ToInt32(grid.SelectedRows[0].Cells["ProductId"].Value);
                    CartItem itemToUpdate = cart.FirstOrDefault(x => x.ProductId == selectedProductId);

                    if (itemToUpdate != null)
                    {
                        int requestQty = (int)numQty.Value;

                        // Verify available stock from DB
                        string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
                        using (SqlConnection conn = new SqlConnection(connStr))
                        {
                            try
                            {
                                conn.Open();
                                SqlCommand cmd = new SqlCommand("SELECT StockQty FROM dbo.Products WHERE ProductId = @id", conn);
                                cmd.Parameters.AddWithValue("@id", selectedProductId);
                                int availableStock = Convert.ToInt32(cmd.ExecuteScalar());

                                if (requestQty > availableStock)
                                {
                                    MessageBox.Show($"Stock limit reached for '{itemToUpdate.Name}'.\nAvailable: {availableStock}", "Stock Insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    FocusBarcodeField();
                                    return;
                                }

                                // Set exact quantity or add to existing
                                itemToUpdate.Quantity = requestQty;
                                RefreshGrid();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Database error during stock check:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                FocusBarcodeField();
                            }
                        }
                        return;
                    }
                }

                MessageBox.Show("Please scan/type a Barcode/SKU or select an item in the cart to update.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusBarcodeField();
                return;
            }

            int requestQuantity = (int)numQty.Value;
            string connectionString = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(@"
                SELECT ProductId, Sku, Name, Price, StockQty 
                FROM dbo.Products 
                WHERE (Barcode = @Code OR Sku = @Code) AND IsActive = 1", conn);
                    cmd.Parameters.AddWithValue("@Code", code);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            int productId = Convert.ToInt32(r["ProductId"]);
                            string sku = r["Sku"].ToString();
                            string name = r["Name"].ToString();
                            decimal price = Convert.ToDecimal(r["Price"]);
                            int availableStock = Convert.ToInt32(r["StockQty"]);

                            CartItem existingItem = cart.FirstOrDefault(x => x.ProductId == productId);
                            int currentCartQty = existingItem != null ? existingItem.Quantity : 0;

                            if (currentCartQty + requestQuantity > availableStock)
                            {
                                MessageBox.Show($"Stock limit reached for '{name}'.\nAvailable: {availableStock} | Cart: {currentCartQty}", "Stock Insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                FocusBarcodeField();
                                return;
                            }

                            if (existingItem != null)
                            {
                                existingItem.Quantity += requestQuantity;
                            }
                            else
                            {
                                cart.Add(new CartItem
                                {
                                    ProductId = productId,
                                    Sku = sku,
                                    Name = name,
                                    Price = price,
                                    Quantity = requestQuantity
                                });
                            }

                            RefreshGrid();
                        }
                        else
                        {
                            MessageBox.Show($"No active product found matching Barcode/SKU: '{code}'", "Lookup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database error during item lookup:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    FocusBarcodeField();
                }
            }
        }

        private void BtnRemoveLine_Click(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an item from the cart to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int productId = Convert.ToInt32(grid.SelectedRows[0].Cells["ProductId"].Value);
            cart.RemoveAll(x => x.ProductId == productId);
            RefreshGrid();
            FocusBarcodeField();
        }

        private void RefreshGrid()
        {
            grid.DataSource = null;
            grid.DataSource = cart.ToList();

            if (grid.Columns["ProductId"] != null) grid.Columns["ProductId"].Visible = false;
            if (grid.Columns["Price"] != null) grid.Columns["Price"].DefaultCellStyle.Format = "c2";
            if (grid.Columns["LineTotal"] != null) grid.Columns["LineTotal"].DefaultCellStyle.Format = "c2";

            CalculateTotals();
        }

        private void CalculateTotals()
        {
            decimal subtotal = cart.Sum(x => x.LineTotal);
            lblSubtotal.Text = $"Subtotal: ₱{subtotal:N2}";

            decimal.TryParse(txtDiscount.Text, out decimal discount);
            decimal.TryParse(txtTaxRate.Text, out decimal taxRate);

            if (discount < 0) discount = 0;
            if (taxRate < 0) taxRate = 0;

            decimal taxableBase = subtotal - discount;
            if (taxableBase < 0) taxableBase = 0;

            decimal taxAmount = Math.Round(taxableBase * (taxRate / 100m), 2);
            decimal total = taxableBase + taxAmount;

            lblTotal.Text = $"TOTAL: ₱{total:N2}";

            if (decimal.TryParse(txtPaid.Text, out decimal paid))
            {
                decimal change = paid - total;
                lblChange.Text = change >= 0 ? $"Change: ₱{change:N2}" : "Change: ₱0.00";
            }
            else
            {
                lblChange.Text = "Change: ₱0.00";
            }
        }

        private void FocusBarcodeField()
        {
            txtBarcode.Clear();
            numQty.Value = 1;
            txtBarcode.Focus();
        }

        private void PerformCheckout(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("The shopping cart is empty.", "Checkout Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusBarcodeField();
                return;
            }

            decimal subtotal = cart.Sum(x => x.LineTotal);
            decimal.TryParse(txtDiscount.Text, out decimal discount);
            decimal.TryParse(txtTaxRate.Text, out decimal taxRate);
            decimal taxableBase = Math.Max(0, subtotal - discount);
            decimal taxAmount = Math.Round(taxableBase * (taxRate / 100m), 2);
            decimal total = taxableBase + taxAmount;

            if (!decimal.TryParse(txtPaid.Text, out decimal paid) || paid < total)
            {
                MessageBox.Show("Amount paid must be equal to or greater than the Total Due.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPaid.Focus();
                return;
            }

            XElement xmlItems = new XElement("Items",
                cart.Select(item => new XElement("Item",
                    new XAttribute("ProductId", item.ProductId),
                    new XAttribute("Quantity", item.Quantity)
                ))
            );

            object customerId = cmbCustomer.SelectedValue ?? DBNull.Value;

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_Order_Create", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", session.UserId);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@PaymentMethod", cbPayment.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@DiscountAmount", discount);
                    cmd.Parameters.AddWithValue("@TaxRate", taxRate);
                    cmd.Parameters.AddWithValue("@AmountPaid", paid);
                    cmd.Parameters.AddWithValue("@ItemsXml", xmlItems.ToString());

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        int orderId = 0;
                        if (r.Read())
                        {
                            orderId = Convert.ToInt32(r["Id"]);
                        }

                        r.Close();

                        SqlCommand cmdGetNo = new SqlCommand("SELECT OrderNumber FROM dbo.Orders WHERE OrderId = @id", conn);
                        cmdGetNo.Parameters.AddWithValue("@id", orderId);
                        string orderNumber = cmdGetNo.ExecuteScalar()?.ToString() ?? $"ORD-{orderId}";

                        decimal change = paid - total;

                        // Create a snapshot copy of cart items for the receipt preview BEFORE clearing cart
                        List<CartItem> cartSnapshot = new List<CartItem>(cart);

                        // Show receipt window
                        ShowReceiptPreview(orderNumber, cmbCustomer.Text, cartSnapshot, subtotal, discount, taxAmount, total, paid, change);

                        // Clear cart after database commit succeeds
                        cart.Clear();
                        txtPaid.Text = "0.00";
                        txtDiscount.Text = "0.00";
                        RefreshGrid();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Checkout Failed:\n" + ex.Message, "Transaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    FocusBarcodeField();
                }
            }
        }

        private void ShowReceiptPreview(string orderNo, string customerName, List<CartItem> items, decimal subtotal, decimal discount, decimal tax, decimal total, decimal paid, decimal change)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("             ENTERPRISE POS               ");
            sb.AppendLine("    123 Innovation Way, Tech District     ");
            sb.AppendLine("  Tel: (032) 412-8899 | TIN: 009-876-543 ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Order No : {orderNo}");
            sb.AppendLine($"Date     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cashier  : {session.FullName}");
            sb.AppendLine($"Customer : {customerName}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine(string.Format("{0,-20} {1,5} {2,10}", "ITEM", "QTY", "TOTAL"));
            sb.AppendLine("------------------------------------------");

            foreach (var item in items)
            {
                string name = item.Name.Length > 18 ? item.Name.Substring(0, 18) : item.Name;
                sb.AppendLine(string.Format("{0,-20} {1,5} ₱{2,9:N2}", name, item.Quantity, item.LineTotal));
            }

            sb.AppendLine("------------------------------------------");
            sb.AppendLine(string.Format("{0,-26} ₱{1,10:N2}", "Subtotal:", subtotal));
            if (discount > 0)
                sb.AppendLine(string.Format("{0,-26}-₱{1,10:N2}", "Discount:", discount));
            sb.AppendLine(string.Format("{0,-26} ₱{1,10:N2}", "VAT / Tax:", tax));
            sb.AppendLine("==========================================");
            sb.AppendLine(string.Format("{0,-26} ₱{1,10:N2}", "TOTAL DUE:", total));
            sb.AppendLine(string.Format("{0,-26} ₱{1,10:N2}", "Amount Paid (" + cbPayment.SelectedItem + "):", paid));
            sb.AppendLine(string.Format("{0,-26} ₱{1,10:N2}", "Change Due:", change));
            sb.AppendLine("==========================================");
            sb.AppendLine("             RETURN POLICY                ");
            sb.AppendLine("  Items may be exchanged within 7 days    ");
            sb.AppendLine("  with original receipt & tags intact.    ");
            sb.AppendLine("==========================================");
            sb.AppendLine("        Thank you for shopping!           ");
            sb.AppendLine("==========================================");

            Form receiptForm = new Form
            {
                Text = $"Receipt Preview - {orderNo}",
                Size = new Size(420, 620),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            TextBox txtReceipt = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5f),
                Text = sb.ToString(),
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            Button btnClose = new Button
            {
                Text = "Close & New Transaction",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnClose.Click += (s, e) => receiptForm.Close();

            receiptForm.Controls.Add(txtReceipt);
            receiptForm.Controls.Add(btnClose);
            receiptForm.ShowDialog(this);
        }
    }
}