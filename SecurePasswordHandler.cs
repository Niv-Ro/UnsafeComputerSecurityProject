﻿using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;



public class SecurePasswordHandler
{
    // Constants for password hashing
    private const int SALT_SIZE = 4;
    private const int HASH_SIZE = 16;
    private const int ITERATIONS = 0;// more iteretions- more difficult to bridge

    // Database connection string - replace with your actual connection details
    private string CONNECTION_STRING = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();

    // Class to store password verification result
    public class VerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    // Generate a new random salt
    private byte[] GenerateSalt()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] salt = new byte[SALT_SIZE];
            rng.GetBytes(salt);
            return salt;
        }
    }

    // Hash password using HMAC-SHA256 with salt
    private byte[] HashPassword(string password, byte[] salt)
    {
        using (var hmac = new HMACSHA256(salt))
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = passwordBytes;

            // Perform multiple iterations of hashing
            for (int i = 0; i < ITERATIONS; i++)
            {
                hash = hmac.ComputeHash(hash);
            }

            return hash;
        }
    }

    // Create password hash with salt for new user registration
    public (string HashedPassword, string Salt) CreatePasswordHash(string password)
    {
        // Generate new salt
        byte[] salt = GenerateSalt();

        // Hash password with salt
        byte[] hashBytes = HashPassword(password, salt);

        // Convert to base64 for storage
        string hashedPassword = Convert.ToBase64String(hashBytes);
        string saltString = Convert.ToBase64String(salt);

        return (hashedPassword, saltString);
    }



    public bool VerifyHashPassword(string userEmail, string password)
    {
        // Retrieve connection string from web.config
        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();

        using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
        {
            try
            {
                conn.Open();

                // SQL to retrieve password hash and salt for the given username
                string sql = "SELECT password_hash, salt FROM new_tableuserregistration WHERE email = '"+ userEmail + "'";

                using (var command = new MySqlCommand(sql, conn))
                {
                    // Execute the query and check if user exists
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Retrieve the stored hash and salt
                            string storedHash = !reader.IsDBNull(0) ? reader.GetString(0) : null;
                            string storedSalt = !reader.IsDBNull(1) ? reader.GetString(1) : null;

                            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                            {
                                // Log the issue if stored hash or salt is missing
                                return false;
                            }

                            // Convert the stored salt from Base64 to byte array
                            byte[] saltBytes = Convert.FromBase64String(storedSalt);

                            // Hash the entered password with the retrieved salt
                            byte[] hashBytes = HashPassword(password, saltBytes);
                            string hashedInput = Convert.ToBase64String(hashBytes);

                            // Compare the entered password hash with the stored hash
                            if (storedHash.Equals(hashedInput))
                            {
                                // If the hashes match, the password is correct
                                return true;
                            }
                            else
                            {
                                // If the hashes don't match, the password is invalid
                                return false;
                            }
                        }
                        else
                        {
                            // No user found with the given username
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exceptions for debugging
                Console.WriteLine($"Error during password verification for user {userEmail}: {ex.Message}");
                return false;
            }
        }
    }

    public bool NaiveVerifyHashPassword(string userEmail, string password)
    {
        // Pretend you get this from your web.config
        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();

        using (var conn = new MySqlConnection(connString))
        {
            conn.Open();

            // 1) Get the user's salt via unparameterized query
            //
            //    Suppose attacker enters userEmail as:  "' OR '1'='1' #"
            //    That breaks out of the string and can match all rows or cause other havoc.
            //
            //    This alone is an SQL injection vulnerability.
            //
            string getSaltSql =
                "SELECT salt " +
                "FROM new_tableuserregistration " +
                "WHERE email = '" + userEmail + "'";

            string storedSalt = null;
            using (var cmdSalt = new MySqlCommand(getSaltSql, conn))
            {
                object result = cmdSalt.ExecuteScalar();
                if (result != null)
                {
                    storedSalt = result.ToString(); // Base64-encoded salt
                }
                else
                {
                    // No row found for this email
                    return false;
                }
            }

            // 2) Hash the input password using your HMAC with salt approach
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] inputHashBytes = HashPassword(password, saltBytes);
            string base64InputHash = Convert.ToBase64String(inputHashBytes);

            // 3) Naive comparison in SQL, again with string concatenation
            //
            //    If the attacker’s earlier input injected something like:
            //         "' OR '1'='1' #"
            //    The second clause (password_hash check) can be commented out or bypassed,
            //    making the WHERE condition trivially true for any row.
            //
            string verifySql =
                "SELECT COUNT(*) " +
                "FROM new_tableuserregistration " +
                "WHERE email = '" + userEmail + "'" +
                "  AND password_hash = '" + base64InputHash + "'";

            using (var cmdVerify = new MySqlCommand(verifySql, conn))
            {
                int count = Convert.ToInt32(cmdVerify.ExecuteScalar());
                // If COUNT(*) > 0 => "login success" in naive logic
                return (count > 0);
            }
        }
    }

    public bool IsPasswordInHistory(string email, string password, int history_num)
    {
        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();

        using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
        {
            try
            {
                conn.Open();

                // SQL to retrieve the current password hash and salts
                string sql = @"SELECT password_hash, salt FROM webapp.new_user_hash_salt_data WHERE Email = '" + email + "' ORDER BY id DESC LIMIT '" + history_num + "'"; ;

                using (var command = new MySql.Data.MySqlClient.MySqlCommand(sql, conn))
                {                
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Read the password hash and salt from each row
                            string storedHash = reader.GetString(0);
                            string storedSalt = reader.GetString(1);

                            // Check if the entered password matches the stored hash and salt
                            if (IsPasswordMatch(password, storedHash, storedSalt))
                            {
                                return true; // Password is in history
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        return false; // No match found 
    }

    private bool IsPasswordMatch(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }

        // Convert the stored salt from Base64 to byte array
        byte[] saltBytes = Convert.FromBase64String(storedSalt);

        // Hash the entered password with the stored salt
        byte[] hashBytes = HashPassword(password, saltBytes);
        string hashedInput = Convert.ToBase64String(hashBytes);

        // Compare the entered password hash with the stored hash
        return storedHash.Equals(hashedInput);
    }

    public bool ValidatePassword(string password, ref List<string> validationErrors, string userEmail)
    {
        // Reset the validation errors list
        validationErrors.Clear();

        // Load the password validation rules
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasswordValidationRules.txt");
        var rules = File.ReadAllLines(filePath);

        foreach (string rule in rules)
        {
            if (rule.Contains("Minimum Length"))
            {
                int minLength = ExtractNumber(rule);
                if (password.Length < minLength)
                    validationErrors.Add($"Password must be at least {minLength} characters long.");
            }
            else if (rule.Contains("Uppercase"))
            {
                if (!password.Any(char.IsUpper))
                    validationErrors.Add("Password must contain at least one uppercase letter.");
            }
            else if (rule.Contains("Lowercase"))
            {
                if (!password.Any(char.IsLower))
                    validationErrors.Add("Password must contain at least one lowercase letter.");
            }
            else if (rule.Contains("Digit"))
            {
                int mindig = ExtractNumber(rule);
                if (!password.Any(char.IsDigit))
                    validationErrors.Add($"Password must contain at least {mindig} digits.");
            }
            else if (rule.Contains("Special Character"))
            {
                int minspec = ExtractNumber(rule);
                char[] specialChars = "!@#$%^&*()_-+=[]{}|:;\"'<>,.?/`~".ToCharArray();
                if (!password.Any(c => specialChars.Contains(c)))
                    validationErrors.Add($"Password must contain at least {minspec} special characters.");
            }
            else if (rule.Contains("Unique Characters"))
            {
                int minUnique = ExtractNumber(rule);
                if (password.Distinct().Count() < minUnique)
                    validationErrors.Add($"Password must contain at least {minUnique} unique characters.");
            }
            else if (rule.Contains("No Common Words"))
            {
                var commonWords = new[] { "password", "123456", "qwerty" }; // Expandable list of common words
                foreach (var word in commonWords)
                {
                    if (password.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        validationErrors.Add("Password contains a common word (e.g., 'password', '123456'). Please choose a stronger password.");
                }
            }
            else if (rule.Contains("Password History") && userEmail != null)
            {
                int History_num = ExtractNumber(rule);

                //userEmail = (string)(Session["userEmail"]);
                // Check if the password matches any of the last 3 passwords
                SecurePasswordHandler SecurePassword = new SecurePasswordHandler();
                if (IsPasswordInHistory(userEmail, password, History_num))
                {
                    validationErrors.Add("Password cannot be one of your last 3 passwords.");
                }
            }
            else
            {
                // Handle unknown rules (log or skip)
                //LogUnrecognizedRule(rule);
            }
        }

        // If any errors were found, return false and display them
        return validationErrors.Count == 0;
    }

    private int ExtractNumber(string rule)
    {
        var match = Regex.Match(rule, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
    /*private void LogUnrecognizedRule(string rule)
    {
        string logPath = Server.MapPath("~/UnrecognizedRules.txt"); // Path to log unrecognized rules
        using (var writer = new StreamWriter(logPath, true))
        {
            writer.WriteLine($"Unrecognized Rule: {rule} - {DateTime.Now}");
        }
    }*/

}




