using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NIDGO_POS
{
    public partial class ProductEditorForm : Form
    {
        private readonly UserSession session;
        private readonly int? editProductId;

        private TextBox txtSku;
        private TextBox txtBarcode;
        private TextBox txtName;
        private ComboBox cmbCategory;
        private TextBox txtSellingPrice;
        private TextBox txtCost;
        private TextBox txtStockQty;
        private TextBox txtReorderLevel;
        private CheckBox chkActive;
        private PictureBox picPreview;
        private string selectedImagePath = null;
        private string relativeImagePathToSave = null;

        public ProductEditorForm(UserSession user, int? productId = null)
        {
            session = user;
            editProductId = productId;
            SetupCustomUI();
            LoadCategories();

            if (editProductId.HasValue)
            {
                LoadProductData(editProductId.Value);
            }
        }

        private void SetupCustomUI()
        {
            Text = editProductId.HasValue ? "NIDGO POS - Edit Product" : "NIDGO POS - New Product Registration";
            Size = new Size(720, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);

            // 1. TOP HEADER BANNER
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(17, 25, 39),
                Padding = new Padding(25, 12, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = editProductId.HasValue ? "UPDATE PRODUCT DETAILS" : "REGISTER NEW PRODUCT",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Fill in the product specifications and inventory configuration below.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(156, 163, 175),
                Dock = DockStyle.Top,
                AutoSize = true
            };

            headerPanel.Controls.Add(lblSub);
            headerPanel.Controls.Add(lblTitle);
            Controls.Add(headerPanel);

            // 2. MAIN CARD CANVAS
            Panel mainCard = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(665, 510),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20)
            };
            Controls.Add(mainCard);

            // Section 1: Product Information
            mainCard.Controls.Add(CreateSectionHeader("1. Product Information", 15, 10));

            mainCard.Controls.Add(CreateFieldLabel("SKU / Item Code *", 15, 40));
            txtSku = CreateInputField("SKU-1001", 15, 62, 200);
            mainCard.Controls.Add(txtSku);

            mainCard.Controls.Add(CreateFieldLabel("Barcode (UPC/EAN)", 230, 40));
            txtBarcode = CreateInputField("", 230, 62, 200);
            mainCard.Controls.Add(txtBarcode);

            mainCard.Controls.Add(CreateFieldLabel("Category *", 445, 40));
            cmbCategory = new ComboBox
            {
                Location = new Point(445, 62),
                Width = 200,
                Font = new Font("Segoe UI", 9.5f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            mainCard.Controls.Add(cmbCategory);

            mainCard.Controls.Add(CreateFieldLabel("Product Display Name *", 15, 100));
            txtName = CreateInputField("", 15, 122, 630);
            mainCard.Controls.Add(txtName);

            // Section 2: Pricing & Stock Control
            mainCard.Controls.Add(CreateSectionHeader("2. Pricing & Stock Control", 15, 165));

            mainCard.Controls.Add(CreateFieldLabel("Selling Price (₱) *", 15, 195));
            txtSellingPrice = CreateInputField("0.00", 15, 217, 140);
            mainCard.Controls.Add(txtSellingPrice);

            mainCard.Controls.Add(CreateFieldLabel("Unit Cost (₱) *", 170, 195));
            txtCost = CreateInputField("0.00", 170, 217, 140);
            mainCard.Controls.Add(txtCost);

            mainCard.Controls.Add(CreateFieldLabel("Initial Stock Qty *", 325, 195));
            txtStockQty = CreateInputField("0", 325, 217, 140);
            mainCard.Controls.Add(txtStockQty);

            mainCard.Controls.Add(CreateFieldLabel("Reorder Warning Level *", 480, 195));
            txtReorderLevel = CreateInputField("5", 480, 217, 165);
            mainCard.Controls.Add(txtReorderLevel);

            // Section 3: Asset & Status
            mainCard.Controls.Add(CreateSectionHeader("3. Product Asset & Visibility", 15, 260));

            picPreview = new PictureBox
            {
                Location = new Point(15, 290),
                Size = new Size(160, 130),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(245, 247, 250)
            };
            mainCard.Controls.Add(picPreview);

            Button btnUpload = new Button
            {
                Text = "Upload Image...",
                Location = new Point(190, 290),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(240, 243, 248),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpload.FlatAppearance.BorderSize = 1;
            btnUpload.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnUpload.Click += BtnBrowse_Click;
            mainCard.Controls.Add(btnUpload);

            Button btnClearImg = new Button
            {
                Text = "Remove",
                Location = new Point(190, 330),
                Size = new Size(130, 34),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand
            };
            btnClearImg.Click += (s, e) => { picPreview.Image = null; selectedImagePath = null; relativeImagePathToSave = null; };
            mainCard.Controls.Add(btnClearImg);

            chkActive = new CheckBox
            {
                Text = "Active & Available for POS Sale",
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(350, 290),
                AutoSize = true
            };
            mainCard.Controls.Add(chkActive);

            Label lblHelp = new Label
            {
                Text = "Note: Inactive items will be hidden from cashier terminal checkout.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(350, 320),
                Size = new Size(295, 40)
            };
            mainCard.Controls.Add(lblHelp);

            // 3. ACTION FOOTER PANEL
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.FromArgb(240, 243, 248),
                Padding = new Padding(20, 12, 25, 12)
            };

            Button btnSave = new Button
            {
                Text = editProductId.HasValue ? "Save Changes" : "Create Product",
                Dock = DockStyle.Right,
                Width = 140,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => Close();

            footerPanel.Controls.Add(btnSave);
            footerPanel.Controls.Add(btnCancel);
            Controls.Add(footerPanel);
        }

        private Label CreateSectionHeader(string title, int x, int y)
        {
            return new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private TextBox CreateInputField(string text, int x, int y, int width)
        {
            return new TextBox
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private void LoadCategories()
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT CategoryId, Name FROM dbo.Categories ORDER BY Name ASC", conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cmbCategory.DataSource = dt;
                    cmbCategory.DisplayMember = "Name";
                    cmbCategory.ValueMember = "CategoryId";
                }
                catch { }
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Product Image",
                Filter = "Allowed Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FileInfo info = new FileInfo(ofd.FileName);

                    if (info.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Image file size exceeds the 5 MB limit.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (Image img = Image.FromFile(ofd.FileName))
                        {
                            if (img.Width < 50 || img.Height < 50)
                            {
                                MessageBox.Show("Image dimensions are too small (Minimum 50x50 pixels required).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            picPreview.Image = new Bitmap(img);
                            selectedImagePath = ofd.FileName;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Invalid image file format.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string ProcessAndSaveImage(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return relativeImagePathToSave;

            string mediaFolder = Path.Combine(Application.StartupPath, "Media", "Products");
            if (!Directory.Exists(mediaFolder))
            {
                Directory.CreateDirectory(mediaFolder);
            }

            string ext = Path.GetExtension(sourcePath).ToLower();
            string safeFilename = $"{Guid.NewGuid()}{ext}";
            string destinationPath = Path.Combine(mediaFolder, safeFilename);

            File.Copy(sourcePath, destinationPath, true);
            return Path.Combine("Media", "Products", safeFilename);
        }

        private void LoadProductData(int id)
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM dbo.Products WHERE ProductId = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtSku.Text = r["Sku"].ToString();
                            txtBarcode.Text = r["Barcode"]?.ToString();
                            txtName.Text = r["Name"].ToString();
                            cmbCategory.SelectedValue = Convert.ToInt32(r["CategoryId"]);
                            txtSellingPrice.Text = Convert.ToDecimal(r["Price"]).ToString("F2");
                            txtCost.Text = Convert.ToDecimal(r["Cost"]).ToString("F2");
                            txtStockQty.Text = r["StockQty"].ToString();
                            txtReorderLevel.Text = r["ReorderLevel"].ToString();
                            chkActive.Checked = Convert.ToBoolean(r["IsActive"]);

                            if (r["ImagePath"] != DBNull.Value)
                            {
                                relativeImagePathToSave = r["ImagePath"].ToString();
                                string fullPath = Path.Combine(Application.StartupPath, relativeImagePathToSave);
                                if (File.Exists(fullPath))
                                {
                                    picPreview.Image = Image.FromFile(fullPath);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading product details:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSku.Text))
            {
                MessageBox.Show("SKU / Item Code is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSku.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Product Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (cmbCategory.SelectedValue == null)
            {
                MessageBox.Show("Please select a valid product Category.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return;
            }

            if (!decimal.TryParse(txtSellingPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Selling Price must be a valid non-negative decimal value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSellingPrice.Focus();
                return;
            }

            if (!decimal.TryParse(txtCost.Text, out decimal cost) || cost < 0)
            {
                MessageBox.Show("Unit Cost must be a valid non-negative decimal value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCost.Focus();
                return;
            }

            if (!int.TryParse(txtStockQty.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Initial Stock Quantity must be a valid non-negative integer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtStockQty.Focus();
                return;
            }

            if (!int.TryParse(txtReorderLevel.Text, out int reorder) || reorder < 0)
            {
                MessageBox.Show("Reorder Level must be a valid non-negative integer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReorderLevel.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(selectedImagePath))
            {
                relativeImagePathToSave = ProcessAndSaveImage(selectedImagePath);
            }

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        editProductId.HasValue ? "dbo.usp_Product_Update" : "dbo.usp_Product_Insert", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (editProductId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ProductId", editProductId.Value);
                    }

                    cmd.Parameters.AddWithValue("@Sku", txtSku.Text.Trim());
                    cmd.Parameters.AddWithValue("@Barcode", string.IsNullOrWhiteSpace(txtBarcode.Text) ? (object)DBNull.Value : txtBarcode.Text.Trim());
                    cmd.Parameters.AddWithValue("@Name", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@CategoryId", cmbCategory.SelectedValue);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Cost", cost);
                    cmd.Parameters.AddWithValue("@StockQty", qty);
                    cmd.Parameters.AddWithValue("@ReorderLevel", reorder);
                    cmd.Parameters.AddWithValue("@IsActive", chkActive.Checked);
                    cmd.Parameters.AddWithValue("@ImagePath", (object)relativeImagePathToSave ?? DBNull.Value);

                    // Added required @UserId parameter
                    cmd.Parameters.AddWithValue("@UserId", session != null ? session.UserId : 1);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show(editProductId.HasValue ? "Product record updated!" : "New product registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    {
                        if (sqlex.Message.IndexOf("Sku", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            MessageBox.Show($"The SKU '{txtSku.Text.Trim()}' already exists in the database. Please enter a unique SKU.", "Duplicate Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (sqlex.Message.IndexOf("Barcode", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            MessageBox.Show($"The Barcode '{txtBarcode.Text.Trim()}' is already assigned to another product.", "Duplicate Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("A product with this unique SKU or Barcode already exists.", "Duplicate Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Database error saving product:\n" + sqlex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unexpected error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}