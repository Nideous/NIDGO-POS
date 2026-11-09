using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NIDGO_POS
{
    public partial class DashboardForm : Form
    {
        private readonly UserSession session;

        // Dashboard Layout Panels
        private Panel mainPanel;

        // Dashboard View Controls
        private Label lblSalesVal;
        private Label lblOrdersVal;
        private Label lblLowStockVal;
        private Label lblActiveProdVal;
        private DataGridView gridRecentOrders;
        private DataGridView gridLowStock;

        // Sales Analytics Controls
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private TabControl tabReports;
        private DataGridView gridDailySales;
        private DataGridView gridTopProducts;
        private DataGridView gridCashiers;
        private DataGridView gridCategories;
        private DataGridView gridInventory;
        private Label lblTotalNetSales;
        private Label lblTotalOrders;
        private Label lblTotalDiscounts;
        private Label lblTotalTax;

        // User & Role Admin Controls
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtFullName;
        private ComboBox cmbRole;
        private CheckBox chkIsActive;
        private Label lblLastLogin;
        private Button btnSaveUser;
        private Button btnResetPassword;
        private Button btnNewUser;
        private DataGridView gridUsers;
        private DataGridView gridAuditHistory;
        private int? selectedUserId = null;

        // Nav Buttons Array for state tracking
        private Button[] navButtons;

        public DashboardForm(UserSession user)
        {
            session = user;
            SetupDarkThemeUI();
            LoadDashboardData();
        }

        private bool HasPermission(string permissionKey)
        {
            if (session == null) return false;
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(1) 
                        FROM dbo.RolePermissions rp
                        JOIN dbo.Permissions p ON p.PermissionId = rp.PermissionId
                        WHERE rp.RoleId = @RoleId AND p.PermissionKey = @PermissionKey", conn);
                    cmd.Parameters.AddWithValue("@RoleId", session.RoleId);
                    cmd.Parameters.AddWithValue("@PermissionKey", permissionKey);

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void SetupDarkThemeUI()
        {
            Text = "Enterprise POS - Main Dashboard";
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(15, 23, 42);

            bool canManageUsers = HasPermission("Users.Manage");

            // 1. LEFT SIDEBAR NAVIGATION
            Panel sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(10, 15, 30)
            };
            Controls.Add(sidebar);

            Panel brandPanel = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(15, 12, 10, 5) };
            Label lblBrand = new Label
            {
                Text = "ENTERPRISE POS",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Dock = DockStyle.Top,
                AutoSize = true
            };
            Label lblSubBrand = new Label
            {
                Text = "POINT OF SALE SYSTEM\nTerminal: " + Environment.MachineName,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Top,
                AutoSize = true
            };
            brandPanel.Controls.Add(lblSubBrand);
            brandPanel.Controls.Add(lblBrand);
            sidebar.Controls.Add(brandPanel);

            Button btnLogout = CreateNavButton("Log Out", false, Color.FromArgb(248, 113, 113));
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.Height = 36;
            btnLogout.Click += (s, e) => Close();

            Button btnVoid = CreateNavButton("Void Selected Order", false, Color.White);
            btnVoid.BackColor = Color.FromArgb(225, 29, 72);
            btnVoid.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnVoid.Dock = DockStyle.Bottom;
            btnVoid.Height = 36;
            btnVoid.Click += BtnVoid_Click;

            sidebar.Controls.Add(btnVoid);
            sidebar.Controls.Add(btnLogout);

            FlowLayoutPanel navMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 5, 0, 0)
            };

            Button btnDash = CreateMenuBtn("Dashboard", true);
            Button btnInventory = CreateMenuBtn("Inventory", false);
            Button btnNewOrder = CreateMenuBtn("New Order / Sales", false);
            Button btnReports = CreateMenuBtn("Reports _Analytics", false);
            Button btnAdmin = CreateMenuBtn("Administration", false);

            List<Button> activeNavList = new List<Button> { btnDash, btnInventory, btnNewOrder, btnReports };
            if (canManageUsers)
            {
                activeNavList.Add(btnAdmin);
            }
            navButtons = activeNavList.ToArray();

            // Wire Nav Handlers
            btnDash.Click += (s, e) => { HighlightNavButton(btnDash); ShowDashboardHomeView(); };

            btnInventory.Click += (s, e) =>
            {
                HighlightNavButton(btnInventory);
                new ProductForm(session).ShowDialog(this);
                HighlightNavButton(btnDash);
                ShowDashboardHomeView();
            };

            btnNewOrder.Click += (s, e) =>
            {
                HighlightNavButton(btnNewOrder);
                new CheckoutForm(session).ShowDialog(this);
                HighlightNavButton(btnDash);
                ShowDashboardHomeView();
            };

            btnReports.Click += (s, e) => { HighlightNavButton(btnReports); ShowAnalyticsView(); };

            btnAdmin.Click += (s, e) =>
            {
                if (!HasPermission("Users.Manage"))
                {
                    MessageBox.Show("Access Denied: You do not possess the 'Users.Manage' administrative permission.", "Security Exception", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                HighlightNavButton(btnAdmin);
                ShowUserAdminView();
            };

            navMenu.Controls.Add(btnDash);
            navMenu.Controls.Add(btnInventory);
            navMenu.Controls.Add(btnNewOrder);
            navMenu.Controls.Add(btnReports);
            if (canManageUsers)
            {
                navMenu.Controls.Add(btnAdmin);
            }

            sidebar.Controls.Add(navMenu);
            navMenu.BringToFront();

            // 2. MAIN CONTAINER
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                AutoScroll = true
            };
            Controls.Add(mainPanel);
            mainPanel.BringToFront();

            ShowDashboardHomeView();
        }

        // ==========================================
        // VIEW 1: HOME DASHBOARD
        // ==========================================
        private void ShowDashboardHomeView()
        {
            mainPanel.Controls.Clear();

            Panel headerBar = new Panel { Dock = DockStyle.Top, Height = 40 };
            Label lblTitle = new Label { Text = "Dashboard", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(248, 250, 252), Dock = DockStyle.Left, AutoSize = true };
            Label lblUserBadge = new Label { Text = $"{session.FullName}  |  {session.RoleName}  |  {Environment.MachineName}", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184), Dock = DockStyle.Right, AutoSize = true };
            headerBar.Controls.Add(lblTitle); headerBar.Controls.Add(lblUserBadge);

            TableLayoutPanel cardGrid = new TableLayoutPanel { Dock = DockStyle.Top, Height = 85, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 5, 0, 15) };
            for (int i = 0; i < 4; i++) cardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            cardGrid.Controls.Add(CreateDarkCard("TODAY'S SALES", "₱0.00", Color.FromArgb(96, 165, 250), out lblSalesVal), 0, 0);
            cardGrid.Controls.Add(CreateDarkCard("ORDERS TODAY", "0", Color.FromArgb(52, 211, 153), out lblOrdersVal), 1, 0);
            cardGrid.Controls.Add(CreateDarkCard("LOW STOCK ITEMS", "0", Color.FromArgb(251, 191, 36), out lblLowStockVal), 2, 0);
            cardGrid.Controls.Add(CreateDarkCard("ACTIVE PRODUCTS", "0", Color.FromArgb(192, 132, 252), out lblActiveProdVal), 3, 0);

            Panel recentSection = new Panel { Dock = DockStyle.Top, Height = 210, Padding = new Padding(0, 10, 0, 10) };
            Panel recentHeader = new Panel { Dock = DockStyle.Top, Height = 30 };
            Label lblRecentTitle = new Label { Text = "Recent Transactions", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(241, 245, 249), Dock = DockStyle.Left, AutoSize = true };
            recentHeader.Controls.Add(lblRecentTitle);
            gridRecentOrders = CreateDarkStyledGrid();
            recentSection.Controls.Add(gridRecentOrders); recentSection.Controls.Add(recentHeader);

            Panel stockSection = new Panel { Dock = DockStyle.Top, Height = 220, Padding = new Padding(0, 10, 0, 10) };
            Label lblStockTitle = new Label { Text = "Low Stock Products", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(241, 245, 249), Dock = DockStyle.Top, Height = 30 };
            gridLowStock = CreateDarkStyledGrid();
            stockSection.Controls.Add(gridLowStock); stockSection.Controls.Add(lblStockTitle);

            mainPanel.Controls.Add(stockSection);
            mainPanel.Controls.Add(recentSection);
            mainPanel.Controls.Add(cardGrid);
            mainPanel.Controls.Add(headerBar);

            LoadDashboardData();
        }

        // ==========================================
        // VIEW 2: SALES ANALYTICS
        // ==========================================
        private void ShowAnalyticsView()
        {
            mainPanel.Controls.Clear();

            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(15, 10, 15, 10), BackColor = Color.FromArgb(30, 41, 59) };
            Label lblStart = new Label { Text = "Start Date: ", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 4, 0, 0) };
            dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today.AddDays(-30) };
            Label lblEnd = new Label { Text = "  End Date: ", AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.White, Padding = new Padding(0, 4, 0, 0) };
            dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };

            Button btnFilter = new Button { Text = "Apply Filter", Width = 95, Height = 28, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadAllReports();

            Button btnExportCsv = new Button { Text = "Export to CSV", Width = 105, Height = 28, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnExportCsv.FlatAppearance.BorderSize = 0;
            btnExportCsv.Click += BtnExportCsv_Click;

            FlowLayoutPanel filterControls = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
            filterControls.Controls.Add(lblStart); filterControls.Controls.Add(dtpStart);
            filterControls.Controls.Add(lblEnd); filterControls.Controls.Add(dtpEnd);
            filterControls.Controls.Add(btnFilter); filterControls.Controls.Add(btnExportCsv);
            topPanel.Controls.Add(filterControls);

            Panel kpiPanel = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(0, 10, 0, 10), BackColor = Color.FromArgb(15, 23, 42) };
            TableLayoutPanel kpiGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            for (int i = 0; i < 4; i++) kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblTotalNetSales = CreateKpiCard("TOTAL NET SALES", "₱0.00", Color.FromArgb(52, 211, 153), kpiGrid, 0);
            lblTotalOrders = CreateKpiCard("TOTAL ORDERS", "0", Color.FromArgb(96, 165, 250), kpiGrid, 1);
            lblTotalDiscounts = CreateKpiCard("TOTAL DISCOUNTS", "₱0.00", Color.FromArgb(251, 113, 133), kpiGrid, 2);
            lblTotalTax = CreateKpiCard("TAX COLLECTED", "₱0.00", Color.FromArgb(251, 191, 36), kpiGrid, 3);
            kpiPanel.Controls.Add(kpiGrid);

            tabReports = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Padding = new Point(10, 5) };
            gridDailySales = CreateDarkStyledGrid();
            gridTopProducts = CreateDarkStyledGrid();
            gridCashiers = CreateDarkStyledGrid();
            gridCategories = CreateDarkStyledGrid();
            gridInventory = CreateDarkStyledGrid();

            TabPage tab1 = new TabPage("Daily Sales Summary"); tab1.Controls.Add(gridDailySales);
            TabPage tab2 = new TabPage("Top Selling Products"); tab2.Controls.Add(gridTopProducts);
            TabPage tab3 = new TabPage("Sales by Cashier"); tab3.Controls.Add(gridCashiers);
            TabPage tab4 = new TabPage("Sales by Category"); tab4.Controls.Add(gridCategories);
            TabPage tab5 = new TabPage("Inventory Valuation"); tab5.Controls.Add(gridInventory);

            tabReports.TabPages.Add(tab1); tabReports.TabPages.Add(tab2); tabReports.TabPages.Add(tab3); tabReports.TabPages.Add(tab4); tabReports.TabPages.Add(tab5);

            Panel tabWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            tabWrapper.Controls.Add(tabReports);

            mainPanel.Controls.Add(tabWrapper); mainPanel.Controls.Add(kpiPanel); mainPanel.Controls.Add(topPanel);

            LoadAllReports();
        }

        // ==========================================
        // VIEW 3: USER & ROLE ADMINISTRATION
        // ==========================================
        private void ShowUserAdminView()
        {
            mainPanel.Controls.Clear();

            // Left Input Form Panel
            Panel formPanel = new Panel { Dock = DockStyle.Left, Width = 330, Padding = new Padding(15), BackColor = Color.FromArgb(30, 41, 59) };
            Label lblTitle = new Label { Text = "USER ACCOUNT MANAGEMENT", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(96, 165, 250), Dock = DockStyle.Top, Height = 30 };

            Label lblUsername = new Label { Text = "Username *", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 45), AutoSize = true };
            txtUsername = new TextBox { Location = new Point(15, 65), Width = 290, Font = new Font("Segoe UI", 9f), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblPassword = new Label { Text = "Password *", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 100), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(15, 120), Width = 290, Font = new Font("Segoe UI", 9f), UseSystemPasswordChar = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblFullName = new Label { Text = "Full Name *", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 155), AutoSize = true };
            txtFullName = new TextBox { Location = new Point(15, 175), Width = 290, Font = new Font("Segoe UI", 9f), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblRole = new Label { Text = "Assigned Role *", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 210), AutoSize = true };
            cmbRole = new ComboBox { Location = new Point(15, 230), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) };

            chkIsActive = new CheckBox { Text = "Account Active", Checked = true, Location = new Point(15, 268), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(52, 211, 153) };
            lblLastLogin = new Label { Text = "Last Login: Never", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(15, 295), AutoSize = true };

            btnSaveUser = new Button { Text = "Save Account", Location = new Point(15, 330), Width = 290, Height = 36, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSaveUser.FlatAppearance.BorderSize = 0;
            btnSaveUser.Click += BtnSaveUser_Click;

            btnResetPassword = new Button { Text = "Reset Password", Location = new Point(15, 375), Width = 290, Height = 32, BackColor = Color.FromArgb(217, 119, 6), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand, Enabled = false };
            btnResetPassword.FlatAppearance.BorderSize = 0;
            btnResetPassword.Click += BtnResetPassword_Click;

            btnNewUser = new Button { Text = "Clear / New User", Location = new Point(15, 415), Width = 290, Height = 32, BackColor = Color.FromArgb(71, 85, 105), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnNewUser.FlatAppearance.BorderSize = 0;
            btnNewUser.Click += (s, e) => ClearUserForm();

            formPanel.Controls.Add(lblTitle);
            formPanel.Controls.Add(lblUsername); formPanel.Controls.Add(txtUsername);
            formPanel.Controls.Add(lblPassword); formPanel.Controls.Add(txtPassword);
            formPanel.Controls.Add(lblFullName); formPanel.Controls.Add(txtFullName);
            formPanel.Controls.Add(lblRole); formPanel.Controls.Add(cmbRole);
            formPanel.Controls.Add(chkIsActive); formPanel.Controls.Add(lblLastLogin);
            formPanel.Controls.Add(btnSaveUser); formPanel.Controls.Add(btnResetPassword); formPanel.Controls.Add(btnNewUser);

            // Right Multi-Tab Container
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 0, 0) };
            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

            gridUsers = CreateDarkStyledGrid();
            gridUsers.SelectionChanged += GridUsers_SelectionChanged;

            gridAuditHistory = CreateDarkStyledGrid();

            TabPage tabUsers = new TabPage("System User Accounts"); tabUsers.Controls.Add(gridUsers);
            TabPage tabAudit = new TabPage("User Audit Trail"); tabAudit.Controls.Add(gridAuditHistory);

            tabs.TabPages.Add(tabUsers); tabs.TabPages.Add(tabAudit);
            rightPanel.Controls.Add(tabs);

            mainPanel.Controls.Add(rightPanel);
            mainPanel.Controls.Add(formPanel);

            LoadRoles();
            LoadUsers();
        }

        // ==========================================
        // DATA LOADERS & EVENT HANDLERS
        // ==========================================
        private void LoadRoles()
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Roles ORDER BY RoleId ASC", conn);
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);

                    if (cmbRole != null)
                    {
                        cmbRole.DataSource = dt;
                        cmbRole.DisplayMember = "RoleName";
                        cmbRole.ValueMember = "RoleId";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading roles: " + ex.Message);
                }
            }
        }

        private void LoadUsers()
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_User_GetAll", conn) { CommandType = CommandType.StoredProcedure };
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);

                    if (gridUsers != null)
                    {
                        gridUsers.DataSource = dt;
                        if (gridUsers.Columns["RoleId"] != null) gridUsers.Columns["RoleId"].Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading users:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void GridUsers_SelectionChanged(object sender, EventArgs e)
        {
            if (gridUsers != null && gridUsers.SelectedRows.Count > 0)
            {
                DataGridViewRow row = gridUsers.SelectedRows[0];
                selectedUserId = Convert.ToInt32(row.Cells["UserId"].Value);
                txtUsername.Text = row.Cells["Username"].Value.ToString();
                txtUsername.Enabled = false;
                txtPassword.Text = "********";
                txtPassword.Enabled = false;
                txtFullName.Text = row.Cells["FullName"].Value.ToString();
                cmbRole.SelectedValue = Convert.ToInt32(row.Cells["RoleId"].Value);
                chkIsActive.Checked = Convert.ToBoolean(row.Cells["IsActive"].Value);

                object lastLogin = row.Cells["LastLoginAt"].Value;
                lblLastLogin.Text = "Last Login: " + (lastLogin != DBNull.Value ? Convert.ToDateTime(lastLogin).ToString("g") : "Never");

                btnSaveUser.Text = "Update Account";
                btnResetPassword.Enabled = true;

                LoadAuditHistory(selectedUserId.Value);
            }
        }

        private void LoadAuditHistory(int userId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_User_GetAuditHistory", conn) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);

                    if (gridAuditHistory != null) gridAuditHistory.DataSource = dt;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Audit Error: " + ex.Message);
                }
            }
        }

        private void ClearUserForm()
        {
            selectedUserId = null;
            if (txtUsername != null) { txtUsername.Clear(); txtUsername.Enabled = true; }
            if (txtPassword != null) { txtPassword.Clear(); txtPassword.Enabled = true; }
            if (txtFullName != null) txtFullName.Clear();
            if (cmbRole != null && cmbRole.Items.Count > 0) cmbRole.SelectedIndex = 0;
            if (chkIsActive != null) chkIsActive.Checked = true;
            if (lblLastLogin != null) lblLastLogin.Text = "Last Login: Never";

            if (btnSaveUser != null) btnSaveUser.Text = "Save Account";
            if (btnResetPassword != null) btnResetPassword.Enabled = false;
            if (txtUsername != null) txtUsername.Focus();
        }

        private void BtnSaveUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please complete all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    if (selectedUserId.HasValue) // Update Existing
                    {
                        SqlCommand cmd = new SqlCommand("dbo.usp_User_Update", conn) { CommandType = CommandType.StoredProcedure };
                        cmd.Parameters.AddWithValue("@UserId", selectedUserId.Value);
                        cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@RoleId", cmbRole.SelectedValue);
                        cmd.Parameters.AddWithValue("@IsActive", chkIsActive.Checked);
                        cmd.ExecuteNonQuery();

                        MessageBox.Show("User account updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else // Create New
                    {
                        if (string.IsNullOrWhiteSpace(txtPassword.Text))
                        {
                            MessageBox.Show("Password is required for new user accounts.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        SqlCommand cmd = new SqlCommand("dbo.usp_User_Insert", conn) { CommandType = CommandType.StoredProcedure };
                        cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim());
                        cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@RoleId", cmbRole.SelectedValue);
                        cmd.Parameters.AddWithValue("@IsActive", chkIsActive.Checked);
                        cmd.ExecuteNonQuery();

                        MessageBox.Show("New user account created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    ClearUserForm();
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Save failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnResetPassword_Click(object sender, EventArgs e)
        {
            if (!selectedUserId.HasValue) return;

            string newPassword = Microsoft.VisualBasic.Interaction.InputBox("Enter new password for account '" + txtUsername.Text + "':", "Reset Password", "");
            if (string.IsNullOrWhiteSpace(newPassword)) return;

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_User_ResetPassword", conn) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("@UserId", selectedUserId.Value);
                    cmd.Parameters.AddWithValue("@NewPassword", newPassword.Trim());
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Password reset successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Password reset failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // GENERAL UI BUILDERS
        // ==========================================
        private Label CreateKpiCard(string title, string initialVal, Color accentColor, TableLayoutPanel parent, int col)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 41, 59), Margin = new Padding(4, 0, 4, 0), Padding = new Padding(10, 6, 10, 6) };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(148, 163, 184), Dock = DockStyle.Top, AutoSize = true };
            Label lblVal = new Label { Text = initialVal, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = accentColor, Dock = DockStyle.Bottom, AutoSize = true };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblVal);
            parent.Controls.Add(card, col, 0);

            return lblVal;
        }

        private void HighlightNavButton(Button selectedBtn)
        {
            if (navButtons == null) return;
            foreach (Button btn in navButtons)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(148, 163, 184);
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            }
            selectedBtn.BackColor = Color.FromArgb(37, 99, 235);
            selectedBtn.ForeColor = Color.White;
            selectedBtn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private Button CreateMenuBtn(string text, bool isActive)
        {
            Button btn = new Button
            {
                Text = "    " + text,
                Width = 220,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isActive ? Color.White : Color.FromArgb(148, 163, 184),
                BackColor = isActive ? Color.FromArgb(37, 99, 235) : Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateNavButton(string text, bool isActive, Color? customColor = null)
        {
            Button btn = new Button
            {
                Text = "    " + text,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = customColor ?? (isActive ? Color.White : Color.FromArgb(148, 163, 184)),
                BackColor = isActive ? Color.FromArgb(37, 99, 235) : Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Panel CreateDarkCard(string title, string defaultVal, Color valColor, out Label valueLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(10)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                AutoSize = true
            };

            valueLabel = new Label
            {
                Text = defaultVal,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            return card;
        }

        private DataGridView CreateDarkStyledGrid()
        {
            DataGridView grid = new DataGridView
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

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(226, 232, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 30;

            grid.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 64, 175);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
            grid.RowTemplate.Height = 28;

            return grid;
        }

        private void BtnVoid_Click(object sender, EventArgs e)
        {
            if (!HasPermission("Orders.Void"))
            {
                MessageBox.Show("Access Denied: You do not possess the 'Orders.Void' permission.", "Unauthorized Action", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (gridRecentOrders == null || gridRecentOrders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an order from the Recent Transactions table to void.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string orderNo = gridRecentOrders.SelectedRows[0].Cells["OrderNumber"].Value?.ToString();
            DialogResult res = MessageBox.Show($"Are you sure you want to void Order #{orderNo}?", "Confirm Void", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                ExecuteVoidOrderInDatabase(orderNo, session.UserId);
                MessageBox.Show($"Order #{orderNo} has been marked as void.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDashboardData();
            }
        }

        private void ExecuteVoidOrderInDatabase(string orderNo, int userId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_Order_Void", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderNo", orderNo);
                    cmd.Parameters.AddWithValue("@VoidedByUserId", userId);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Void Order DB Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadDashboardData()
        {
            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    SqlCommand cmdMetrics = new SqlCommand(@"
                        SELECT 
                            ISNULL(SUM(Total), 0) AS TodaySales,
                            COUNT(OrderId) AS TodayOrders
                        FROM dbo.Orders 
                        WHERE CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE) AND Status = N'Completed';

                        SELECT COUNT(*) AS LowStockCount FROM dbo.Products WHERE StockQty <= ReorderLevel AND IsActive = 1;
                        SELECT COUNT(*) AS ActiveCount FROM dbo.Products WHERE IsActive = 1;
                    ", conn);

                    using (SqlDataReader r = cmdMetrics.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            decimal sales = Convert.ToDecimal(r["TodaySales"]);
                            int orders = Convert.ToInt32(r["TodayOrders"]);
                            if (lblSalesVal != null) lblSalesVal.Text = $"₱{sales:N2}";
                            if (lblOrdersVal != null) lblOrdersVal.Text = orders.ToString();
                        }

                        if (r.NextResult() && r.Read())
                        {
                            if (lblLowStockVal != null) lblLowStockVal.Text = r["LowStockCount"].ToString();
                        }

                        if (r.NextResult() && r.Read())
                        {
                            if (lblActiveProdVal != null) lblActiveProdVal.Text = r["ActiveCount"].ToString();
                        }
                    }

                    SqlDataAdapter daOrders = new SqlDataAdapter(@"
                        SELECT TOP 5 
                            o.OrderNumber, 
                            ISNULL(c.FullName, 'Walk-in Customer') AS CustomerName, 
                            u.Username AS Cashier, 
                            o.OrderDate AS Date, 
                            o.Subtotal, 
                            o.TaxAmount, 
                            o.DiscountAmount, 
                            o.Total, 
                            o.PaymentMethod AS Payment, 
                            o.Status
                        FROM dbo.Orders o
                        LEFT JOIN dbo.Customers c ON o.CustomerId = c.CustomerId
                        JOIN dbo.Users u ON o.UserId = u.UserId
                        ORDER BY o.OrderId DESC
                    ", conn);

                    DataTable dtOrders = new DataTable();
                    daOrders.Fill(dtOrders);
                    if (gridRecentOrders != null)
                    {
                        gridRecentOrders.DataSource = dtOrders;
                        if (gridRecentOrders.Columns["Subtotal"] != null) gridRecentOrders.Columns["Subtotal"].DefaultCellStyle.Format = "c2";
                        if (gridRecentOrders.Columns["Total"] != null) gridRecentOrders.Columns["Total"].DefaultCellStyle.Format = "c2";
                    }

                    SqlDataAdapter daStock = new SqlDataAdapter(@"
                        SELECT Sku, Name AS [Product Name], StockQty AS [Stock Quantity], ReorderLevel AS [Reorder Level]
                        FROM dbo.Products
                        WHERE StockQty <= ReorderLevel AND IsActive = 1
                        ORDER BY StockQty ASC
                    ", conn);

                    DataTable dtStock = new DataTable();
                    daStock.Fill(dtStock);
                    if (gridLowStock != null) gridLowStock.DataSource = dtStock;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Dashboard Load Error: " + ex.Message);
                }
            }
        }

        private void LoadAllReports()
        {
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date.AddDays(1).AddTicks(-1);

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    SqlCommand cmdSales = new SqlCommand("dbo.usp_Report_Sales", conn) { CommandType = CommandType.StoredProcedure };
                    cmdSales.Parameters.AddWithValue("@StartDate", startDate);
                    cmdSales.Parameters.AddWithValue("@EndDate", endDate);
                    DataTable dtSales = new DataTable();
                    new SqlDataAdapter(cmdSales).Fill(dtSales);
                    gridDailySales.DataSource = dtSales;
                    FormatGridColumns(gridDailySales);

                    if (dtSales.Rows.Count > 0)
                    {
                        decimal netSales = dtSales.AsEnumerable().Sum(r => r.Field<decimal?>("NetSales") ?? 0m);
                        int totalOrders = dtSales.AsEnumerable().Sum(r => r.Field<int?>("OrderCount") ?? 0);
                        decimal totalDisc = dtSales.AsEnumerable().Sum(r => r.Field<decimal?>("TotalDiscount") ?? 0m);
                        decimal totalTax = dtSales.AsEnumerable().Sum(r => r.Field<decimal?>("TotalTax") ?? 0m);

                        lblTotalNetSales.Text = $"₱{netSales:N2}";
                        lblTotalOrders.Text = totalOrders.ToString("N0");
                        lblTotalDiscounts.Text = $"₱{totalDisc:N2}";
                        lblTotalTax.Text = $"₱{totalTax:N2}";
                    }
                    else
                    {
                        lblTotalNetSales.Text = "₱0.00";
                        lblTotalOrders.Text = "0";
                        lblTotalDiscounts.Text = "₱0.00";
                        lblTotalTax.Text = "₱0.00";
                    }

                    SqlCommand cmdTop = new SqlCommand("dbo.usp_Report_TopProducts", conn) { CommandType = CommandType.StoredProcedure };
                    cmdTop.Parameters.AddWithValue("@StartDate", startDate);
                    cmdTop.Parameters.AddWithValue("@EndDate", endDate);
                    DataTable dtTop = new DataTable();
                    new SqlDataAdapter(cmdTop).Fill(dtTop);
                    gridTopProducts.DataSource = dtTop;
                    FormatGridColumns(gridTopProducts);

                    SqlCommand cmdCashier = new SqlCommand("dbo.usp_Report_SalesByCashier", conn) { CommandType = CommandType.StoredProcedure };
                    cmdCashier.Parameters.AddWithValue("@StartDate", startDate);
                    cmdCashier.Parameters.AddWithValue("@EndDate", endDate);
                    DataTable dtCashier = new DataTable();
                    new SqlDataAdapter(cmdCashier).Fill(dtCashier);
                    gridCashiers.DataSource = dtCashier;
                    FormatGridColumns(gridCashiers);

                    SqlCommand cmdCategory = new SqlCommand("dbo.usp_Report_SalesByCategory", conn) { CommandType = CommandType.StoredProcedure };
                    cmdCategory.Parameters.AddWithValue("@StartDate", startDate);
                    cmdCategory.Parameters.AddWithValue("@EndDate", endDate);
                    DataTable dtCategory = new DataTable();
                    new SqlDataAdapter(cmdCategory).Fill(dtCategory);
                    gridCategories.DataSource = dtCategory;
                    FormatGridColumns(gridCategories);

                    SqlCommand cmdInv = new SqlCommand("dbo.usp_Report_InventoryValuation", conn) { CommandType = CommandType.StoredProcedure };
                    DataTable dtInv = new DataTable();
                    new SqlDataAdapter(cmdInv).Fill(dtInv);
                    gridInventory.DataSource = dtInv;
                    FormatGridColumns(gridInventory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading sales analytics data:\n" + ex.Message, "Analytics Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void FormatGridColumns(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.ValueType == typeof(decimal))
                {
                    col.DefaultCellStyle.Format = "c2";
                }
                else if (col.ValueType == typeof(DateTime))
                {
                    col.DefaultCellStyle.Format = "yyyy-MM-dd";
                }
            }
        }

        private void BtnExportCsv_Click(object sender, EventArgs e)
        {
            TabPage activeTab = tabReports.SelectedTab;
            DataGridView currentGrid = activeTab.Controls.OfType<DataGridView>().FirstOrDefault();

            if (currentGrid == null || currentGrid.Rows.Count == 0)
            {
                MessageBox.Show("There is no data to export in the currently selected tab.", "Export Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = $"{activeTab.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();

                        string[] headers = currentGrid.Columns.Cast<DataGridViewColumn>().Select(c => $"\"{c.HeaderText}\"").ToArray();
                        sb.AppendLine(string.Join(",", headers));

                        foreach (DataGridViewRow row in currentGrid.Rows)
                        {
                            string[] cells = row.Cells.Cast<DataGridViewCell>().Select(c => $"\"{c.Value?.ToString().Replace("\"", "\"\"")}\"").ToArray();
                            sb.AppendLine(string.Join(",", cells));
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show($"Report exported successfully to:\n{sfd.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to export CSV file:\n" + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}