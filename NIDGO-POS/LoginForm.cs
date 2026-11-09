using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace NIDGO_POS
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            this.Text = "Enterprise POS - Login"; // Set form title bar text
            AcceptButton = btnLogin;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            lblStatus.Text = string.Empty;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            // Level 1 UI Validation
            if (string.IsNullOrEmpty(username))
            {
                lblStatus.Text = "Please enter your username.";
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Please enter your password.";
                txtPassword.Focus();
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["POS"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.usp_Login", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!Convert.ToBoolean(reader["IsActive"]))
                            {
                                lblStatus.Text = "Account is inactive. Please contact support.";
                                return;
                            }

                            string dbSalt = reader["Salt"].ToString();
                            string dbHash = reader["PasswordHash"].ToString();

                            if (string.Equals(dbHash, ComputeSha256Hex(dbSalt + password), StringComparison.OrdinalIgnoreCase))
                            {
                                var session = new UserSession
                                {
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    Username = reader["Username"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    RoleId = Convert.ToInt32(reader["RoleId"]),
                                    RoleName = reader["RoleName"].ToString(),
                                    IsActive = true
                                };

                                reader.Close();

                                SqlCommand touchCmd = new SqlCommand("dbo.usp_User_TouchLogin", conn);
                                touchCmd.CommandType = CommandType.StoredProcedure;
                                touchCmd.Parameters.AddWithValue("@UserId", session.UserId);
                                touchCmd.ExecuteNonQuery();

                                Hide();
                                using (var main = new DashboardForm(session))
                                {
                                    main.ShowDialog();
                                }
                                Show();
                                txtPassword.Clear();
                                lblStatus.Text = string.Empty;
                            }
                            else
                            {
                                lblStatus.Text = "Invalid username or password.";
                                txtPassword.Clear();
                                txtPassword.Focus();
                            }
                        }
                        else
                        {
                            lblStatus.Text = "Invalid username or password.";
                            txtPassword.Clear();
                            txtPassword.Focus();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log technical details to %LocalAppData%\NIDGO_POS\Logs
                    Logger.LogException(ex, "Login Form DB Exception");

                    // Show clean non-sensitive error message on the form
                    lblStatus.Text = "Unable to connect to database service. Please try again later.";
                }
            }
        }

        private static string ComputeSha256Hex(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("X2"));
                }
                return builder.ToString();
            }
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblPassword_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}