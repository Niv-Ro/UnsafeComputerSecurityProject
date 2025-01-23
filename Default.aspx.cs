using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace UnsafeComputerSecurityProject
{
    public partial class Default : System.Web.UI.Page
    {


        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void LogInEventMethod(object sender, EventArgs e)
        {
            string uname;
            string uemail;
            MySqlConnection conn = null;
            string email = userEmailTextBox.Text;
            string password = passWordTextBox.Text; // Directly use raw input

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            using (conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    // Vulnerable SQL query with directly concatenated inputs
                    string sql = "SELECT firstname, lastname FROM new_tableuserregistration WHERE email = '" + email + "' AND password = '" + password + "'";
                    using (var command = new MySqlCommand(sql, conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Fetch user details
                                string firstName = !reader.IsDBNull(0) ? reader.GetString(0) : null;
                                string lastName = !reader.IsDBNull(1) ? reader.GetString(1) : null;
                                uname = firstName + " " + lastName;
                                uemail = email;

                                Session["uname"] = uname;
                                Session["uemail"] = uemail;

                                // Output unsanitized user data
                                Message.Text = $"Welcome, {uname}!";
                                userEmailTextBox.Text = "";
                                passWordTextBox.Text = "";
                                Response.BufferOutput = true;
                                Response.Redirect("Logged_in.aspx", false);
                            }
                            else
                            {
                                // This will execute if no rows are returned
                                Message.Text = "Invalid credentials.";
                                Message.ForeColor = System.Drawing.Color.Red;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Output error details (Stored XSS vulnerability)
                    Message.Text = "Error: " + ex.Message;
                    Message.ForeColor = System.Drawing.Color.Red;
                }
            }
        }






        protected void ForgotPasswordEventMethod(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.BufferOutput = true;
            Response.Redirect("Forgot_password.aspx", false);
        }

        protected void RegisterEventMethod(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.BufferOutput = true;
            Response.Redirect("Registration.aspx", false);
        }
        private int ExtractNumber(string rule)
        {
            var match = Regex.Match(rule, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }

}
