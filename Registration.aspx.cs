using System;
using System.Collections.Generic;
using System.Net.Mail;
namespace UnsafeComputerSecurityProject
{
    public partial class Registration : System.Web.UI.Page
    {
        //MySql.Data.MySqlClient.MySqlConnection conn;
        //MySql.Data.MySqlClient.MySqlCommand cmd;
        //string queryStr;
        private List<string> validationErrors = new List<string>();
        SecurePasswordHandler SecurePassword = new SecurePasswordHandler();

        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void RegisterEventMethod(object sender, EventArgs e)
        {


            /// change to  hash and salt in registration
            if (checkUserExists(emailTextBox.Text.ToString()) == false)
            {
                if (SecurePassword.ValidatePassword(passWordTextBox.Text, ref validationErrors, emailTextBox.Text)) // Validate password before registering
                {
                    if (EmailIsValid(emailTextBox.Text))
                    {
                        RegisterUser();
                        Session.Abandon();
                        Response.BufferOutput = true;
                        Response.Redirect("Default.aspx", false);
                    }
                    else
                    {
                        errorLabel.Text = "Email is not valid";
                        errorLabel.Visible = true;
                        errorLabel.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {

                    // Show error message to the user
                    errorLabel.Text = string.Join("<br/>", validationErrors);
                    errorLabel.Visible = true;
                    errorLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            else
            {
                errorLabel.Text = "Email is already exists";
                errorLabel.Visible = true;
                errorLabel.ForeColor = System.Drawing.Color.Red;
            }

        }


        /*private void RegisterUser()
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            
            conn = new MySql.Data.MySqlClient.MySqlConnection(connString);
            conn.Open();
            queryStr = "";
            queryStr = "INSERT INTO webapp.new_tableuserregistration(firstname,lastname,username,password,email)" +
                "VALUES('" + firstNameTextBox.Text + "','" + lastNameTextBox.Text + "','" + userNameTextBox.Text + "','" + passWordTextBox.Text + "','" + emailTextBox.Text + "')";
            cmd = new MySql.Data.MySqlClient.MySqlCommand(queryStr, conn);
            cmd.ExecuteReader();

            conn.Close();

        }*/

        public bool EmailIsValid(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        private bool RegisterUser()
        {

            /*
            */

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            SecurePasswordHandler SecurePassword = new SecurePasswordHandler();
            var (hashedSaltPassword, salt) = SecurePassword.CreatePasswordHash(passWordTextBox.Text);

            // Use a using block to ensure proper disposal of the connection
            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
            {
                conn.Open();

                string queryStr1 = "INSERT INTO webapp.new_tableuserregistration SET firstname='"+ firstNameTextBox.Text + "',lastname='"+ lastNameTextBox.Text + "',username='"+ userNameTextBox.Text + "',password='"+ passWordTextBox.Text + "',email='"+ emailTextBox.Text + "',password_hash='"+ hashedSaltPassword + "',salt='"+ salt + "'";

                string queryStr2 = "INSERT INTO webapp.new_user_hash_salt_data SET Email='" + emailTextBox.Text + "',password_hash='"+ hashedSaltPassword + "',salt='"+ salt + "'";

                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(queryStr1, conn))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();

                    }
                    catch (Exception ex)
                    {
                        errorLabel.Text = ex.ToString();
                        errorLabel.Visible = true;
                        return false;
                    }
                    
                    
                }
                // Execute the second INSERT statement
                using (var cmd2 = new MySql.Data.MySqlClient.MySqlCommand(queryStr2, conn))
                {              
                    cmd2.ExecuteNonQuery();
                }
            }
            return true;
        }

        private bool checkUserExists(string email)
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT userID FROM new_tableuserregistration WHERE email LIKE '"+ email + "'";
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result == null) { return false; }
                    else return true;

                }

            }
        }

        protected void BackToLogInEventMethod(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.BufferOutput = true;
            Response.Redirect("Default.aspx", false);
        }
    }
}
