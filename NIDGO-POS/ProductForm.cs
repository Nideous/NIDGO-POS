using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace NIDGO_POS
{
    public partial class ProductForm : Form
    {
        private readonly UserSession session;
        private DataGridView gridProducts;
        private TextBox txtSearch;
        private Button btnAddProduct;
        private Button btnEditProduct;
        private Button btnRefresh;

        public ProductForm(UserSession user)
        {
            session = user;
            SetupUI();
            LoadProducts();
        }

        private void SetupUI()
        {
            Text = "Product & Inventory Management";
            Size = new Size(1050, 650);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(15, 23, 42); // Dark Charcoal Canvas

            // ==========================================
            // 1. TOP HEADER & SEARCH BAR
            // ==========================================
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(20, 15, 20, 10) };

            Label lblTitle = new Label
            {
                Text = "Product Catalog _Inventory",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Width = 260,
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadProducts(txtSearch.Text.Trim());

            Label lblSearch = new Label
            {
                Text = "Search SKU / Barcode / Name:  ",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Right,
                AutoSize = true
            };

            topPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(lblSearch);
            topPanel.Controls.Add(lblTitle);
            Controls.Add(topPanel);

            // ==========================================
            // 2. ACTION TOOLBAR
            // ==========================================
            Panel actionPanel = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(20, 0, 20, 10) };

            btnAddProduct = new Button
            {
                Text = "+ Add New Product",
                Width = 150,
                Height = 32,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnAddProduct.FlatAppearance.BorderSize = 0;
            btnAddProduct.Click += BtnAddProduct_Click;

            btnEditProduct = new Button
            {
                Text = "Edit Selected",
                Width = 120,
                Height = 32,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 10, 0)
            };
            btnEditProduct.FlatAppearance.BorderSize = 0;
            btnEditProduct.Click += BtnEditProduct_Click;

            btnRefresh = new Button
            {
                Text = "Refresh Grid",
                Width = 110,
                Height = 32,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(148, 163, 184),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadProducts(txtSearch.Text.Trim());

            actionPanel.Controls.Add(btnRefresh);
            actionPanel.Controls.Add(btnEditProduct);
            actionPanel.Controls.Add(btnAddProduct);
            Controls.Add(actionPanel);

            // ==========================================
            // 3. PRODUCT DATA GRID
            // ==========================================
            gridProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 41, 59),
                GridColor = Color.FromArgb(51, 65, 85),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            gridProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            gridProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(226, 232, 240);
            gridProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            gridProducts.ColumnHeadersHeight = 35;

            gridProducts.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            gridProducts.DefaultCellStyle.ForeColor = Color.FromArgb(241, 245, 249);
            gridProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 64, 175);
            gridProducts.DefaultCellStyle.SelectionForeColor = Color.White;
            gridProducts.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            gridProducts.RowTemplate.Height = 32;

            // Wire formatting for Low-Stock visual indicators
            gridProducts.CellFormatting += GridProducts_CellFormatting;

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
            gridContainer.Controls.Add(gridProducts);
            Controls.Add(gridContainer);
            gridContainer.BringToFront();
        }

        private void LoadProducts(string search = "")
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            ProductId, 
                            Sku, 
                            Barcode,
                            Name AS [Product Name], 
                            Price, 
                            StockQty AS [Stock Quantity], 
                            ReorderLevel AS [Reorder Level], 
                            IsActive AS [Active]
                        FROM dbo.Products
                        WHERE (Sku LIKE @Query OR Barcode LIKE @Query OR Name LIKE @Query)
                        ORDER BY Name ASC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Query", "%" + search + "%");

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridProducts.DataSource = dt;

                    if (gridProducts.Columns["Price"] != null)
                    {
                        gridProducts.Columns["Price"].DefaultCellStyle.Format = "c2";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading products:\n" + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Low-Stock Visual Indicator Row Highlighting
        private void GridProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= gridProducts.Rows.Count) return;

            DataGridViewRow row = gridProducts.Rows[e.RowIndex];
            if (row.Cells["Stock Quantity"].Value != DBNull.Value && row.Cells["Reorder Level"].Value != DBNull.Value)
            {
                int stock = Convert.ToInt32(row.Cells["Stock Quantity"].Value);
                int reorder = Convert.ToInt32(row.Cells["Reorder Level"].Value);

                if (stock == 0)
                {
                    // Out of stock -> Crimson Tint
                    row.DefaultCellStyle.BackColor = Color.FromArgb(127, 29, 29);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (stock <= reorder)
                {
                    // Low stock warning -> Dark Amber Tint
                    row.DefaultCellStyle.BackColor = Color.FromArgb(120, 53, 15);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(254, 243, 199);
                }
            }
        }

        private void BtnAddProduct_Click(object sender, EventArgs e)
        {
            using (ProductEditorForm editor = new ProductEditorForm(session))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    LoadProducts(txtSearch.Text.Trim());
                }
            }
        }

        private void BtnEditProduct_Click(object sender, EventArgs e)
        {
            if (gridProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product from the grid to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (int.TryParse(gridProducts.SelectedRows[0].Cells["ProductId"].Value?.ToString(), out int productId))
            {
                using (ProductEditorForm editor = new ProductEditorForm(session, productId))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadProducts(txtSearch.Text.Trim());
                    }
                }
            }
        }
    }
}