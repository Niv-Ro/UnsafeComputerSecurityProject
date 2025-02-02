﻿using System;
using System.Collections.Generic;
namespace UnsafeComputerSecurityProject
{
    public partial class Reset_password : System.Web.UI.Page
    {
        private List<string> validationErrors = new List<string>();
        SecurePasswordHandler SecurePassword = new SecurePasswordHandler();
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BackToLoginEventMethod(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.BufferOutput = true;
            Response.Redirect("Default.aspx", false);
        }


        protected void Submit_New_Password_EventMethod(object sender, EventArgs e)
        {
            string userEmail = Session["userEmail"]?.ToString();
            if ((newPassword.Text == newPasswordAgain.Text) && SecurePassword.ValidatePassword(newPassword.Text, ref validationErrors, userEmail))
            {
                validationErrors.Clear();
                resetPasswordPlaceHolder.Controls.Clear();
                newPassword.Visible = false;
                newPasswordAgain.Visible = false;
                submitPassword.Visible = false;

                Message.Text = "Password reset complete!";
                Message.ForeColor = System.Drawing.Color.Green;
                Message.Visible = true;

                resetPasswordPlaceHolder.Controls.Add(Message);
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
                SecurePasswordHandler SecurePassword = new SecurePasswordHandler();
                var (hashedSaltPassword, salt) = SecurePassword.CreatePasswordHash(newPassword.Text);

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
                {

                    conn.Open();

                    // Single update query that handles everything
                    string updateQuery = @"UPDATE webapp.new_tableuserregistration SET password_hash = '" + hashedSaltPassword + "',salt = '" + salt + "' WHERE email = '" + userEmail + "'";

                    string queryStr2 = "INSERT INTO webapp.new_user_hash_salt_data SET email = '" + userEmail + "',password_hash = '" + hashedSaltPassword + "',salt = '" + salt + "'";


                    try
                    {
                        using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(updateQuery, conn))
                        {                         
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd2 = new MySql.Data.MySqlClient.MySqlCommand(queryStr2, conn))
                        {
                            cmd2.ExecuteNonQuery();
                        }


                        string script = "setTimeout(function() { window.location.href = 'Default.aspx'; }, 2000);";
                        ClientScript.RegisterStartupScript(this.GetType(), "RedirectAfterDelay", script, true);

                    }
                    catch
                    {
                        Message.Text = "An error occurred while resetting your password. Please try again.";
                        Message.ForeColor = System.Drawing.Color.Red;
                        Message.Visible = true;
                    }
                }
            }
            else
            {
                errorLabel.Text = string.Join("<br/>", validationErrors);
                errorLabel.Visible = true;
                errorLabel.ForeColor = System.Drawing.Color.Red;
            }

        }

        private bool Check_email_password(string email, string password)
        {
            MySql.Data.MySqlClient.MySqlConnection conn;
            MySql.Data.MySqlClient.MySqlCommand cmd;
            MySql.Data.MySqlClient.MySqlDataReader reader;

            // Retrieve connection string
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            conn = new MySql.Data.MySqlClient.MySqlConnection(connString);

            try
            {
                conn.Open();

                // SQL query to check email and password
                string queryStr = "SELECT * FROM webapp.new_tableuserregistration WHERE email = '"+ email + "' AND password = '"+ password + "'";
                cmd = new MySql.Data.MySqlClient.MySqlCommand(queryStr, conn);
                reader = cmd.ExecuteReader();

                // Return true if a match is found
                return reader.HasRows;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it
                Message.Text = "An error occurred: " + ex.Message;
                Message.ForeColor = System.Drawing.Color.Red;
                return false;
            }
            finally
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }


    }
}