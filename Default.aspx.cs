using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;
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
            SecurePasswordHandler SecurePassword = new SecurePasswordHandler();
            MySqlConnection conn = null;
            string email = userEmailTextBox.Text;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasswordValidationRules.txt");
            int timeout = 5;
            int allowedAttempts = 3;
            var rules = File.ReadAllLines(filePath);
            foreach (string rule in rules)
            {
                if (rule.Contains("Allowed Failed Attempts"))
                    allowedAttempts = ExtractNumber(rule);
                if (rule.Contains("Lockout Time"))
                    timeout = ExtractNumber(rule);

            }

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();
            using (conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    // Step 1: Check if the user is currently locked out
                    string lockoutCheckSql = "SELECT failed_login_attempts, locked_out_time FROM new_tableuserregistration WHERE email = '" + email + "'";
                    using (var lockoutCommand = new MySqlCommand(lockoutCheckSql, conn))
                    {
                        using (var reader = lockoutCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int failedAttempts = reader.GetInt32(0);
                                DateTime? lockoutEndTime = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

                                // If the lockout time has passed, reset FailedLoginAttempts and LockoutEndTime
                                if (lockoutEndTime.HasValue && lockoutEndTime.Value <= DateTime.Now)
                                {
                                    reader.Close(); // Close reader before updating
                                    string resetLockoutSql = "UPDATE new_tableuserregistration SET failed_login_attempts = 0, locked_out_time = NULL WHERE email = '" + email + "'";
                                    using (var resetLockoutCommand = new MySqlCommand(resetLockoutSql, conn))
                                    {
                                        resetLockoutCommand.ExecuteNonQuery();
                                    }
                                }
                                else if (lockoutEndTime.HasValue && lockoutEndTime.Value > DateTime.Now)
                                {
                                    // User is still locked out
                                    Message.Text = $"Account locked. Try again after {lockoutEndTime.Value}.";
                                    Message.ForeColor = System.Drawing.Color.Red;
                                    return;
                                }
                            }
                        }
                    }

                    //string sql = "SELECT firstname, lastname FROM new_tableuserregistration WHERE email = '" + email + "' AND password = '" + password + "'";
                    // Step 2: Verify the password
                    {
                        // Reset failed login attempts on successful login
                        string resetAttemptsSql = "UPDATE new_tableuserregistration SET failed_login_attempts = 0, locked_out_time = NULL WHERE email ='" + email + "'";
                        using (var resetCommand = new MySqlCommand(resetAttemptsSql, conn))
                        {
                            resetCommand.ExecuteNonQuery();
                        }

                        // Fetch user details
                        string sql = "SELECT firstname, lastname FROM new_tableuserregistration WHERE email ='" + email + "'";
                        using (var command = new MySqlCommand(sql, conn))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string firstName = !reader.IsDBNull(0) ? reader.GetString(0) : null;
                                    string lastName = !reader.IsDBNull(1) ? reader.GetString(1) : null;
                                    uname = firstName + " " + lastName;
                                    uemail = userEmailTextBox.Text;
                                    Session["uname"] = uname;
                                    Session["uemail"] = uemail;
                                    userEmailTextBox.Text = "";
                                    passWordTextBox.Text = "";
                                    Response.BufferOutput = true;
                                    Response.Redirect("Logged_in.aspx", false);
                                }
                                else
                                {
                                    uname = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Increment failed login attempts
                        string incrementAttemptsSql = "UPDATE new_tableuserregistration SET failed_login_attempts = failed_login_attempts + 1 WHERE email = '" + email + "'";
                        using (var incrementCommand = new MySqlCommand(incrementAttemptsSql, conn))
                        {
                            incrementCommand.ExecuteNonQuery();
                        }

                        // Check if the user needs to be locked out
                        string checkAttemptsSql = "SELECT failed_login_attempts FROM new_tableuserregistration WHERE email = '" + email + "'";
                        using (var checkCommand = new MySqlCommand(checkAttemptsSql, conn))
                        {
                            using (var reader = checkCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int failedAttempts = reader.GetInt32(0);
                                    if (failedAttempts >= allowedAttempts)
                                    {
                                        reader.Close();
                                        // Lock the user
                                        string lockoutSql = "UPDATE new_tableuserregistration SET locked_out_time = '" + DateTime.Now.AddMinutes(timeout) + "' WHERE email = '" + email + "'";
                                        using (var lockoutCommand = new MySqlCommand(lockoutSql, conn))
                                        {

                                            lockoutCommand.ExecuteNonQuery();
                                        }

                                        Message.Text = "Too many wrong attempts. Account is locked.\nTry again later";
                                        Message.ForeColor = System.Drawing.Color.Red;
                                        return;
                                    }
                                }
                            }
                        }

                        Message.Text = "User doesn't exist / Wrong password.";
                        Message.ForeColor = System.Drawing.Color.Red;
                    }
                }
                catch (Exception ex)
                {
                    // Log exceptions for debugging
                    Console.WriteLine($"Error during login for user {email}: {ex.Message}");
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
    }

}
