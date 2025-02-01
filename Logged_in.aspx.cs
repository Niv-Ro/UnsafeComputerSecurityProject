﻿using System;
using System.Web.UI;
using MySql.Data.MySqlClient;
using System.Data;

namespace UnsafeComputerSecurityProject
{
    public partial class Logged_in : System.Web.UI.Page
    {
        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WebAppConnString"].ToString();

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Headers.Add("X-XSS-Protection", "0");
            string name;
            string uemail;
            string passemail;
            name = (string)(Session["uname"]);
            uemail = (string)(Session["uemail"]);
            passemail = uemail;
            Session["passemail"] = uemail;
            if (!IsPostBack)
            {
                PopulateCustomerTable();
            }
            if (name == null)
            {
                Response.BufferOutput = true;
                Response.Redirect("Default.aspx", false);
            }
            else
            {
                userlabel.Text = name;
            }

        }

        private void PopulateCustomerTable()
        {
            string query = "SELECT customer_ID, name, email, phone, address, package_type, package_price FROM webapp.customers";
            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                    {
                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            CustomerGridView.DataSource = dt;
                            CustomerGridView.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or display the error
                ScriptManager.RegisterStartupScript(this, this.GetType(), "error", $"alert('Error: {ex.Message}');", true);
            }
        }

        protected void LogOutEventMethod(object sender, EventArgs e)
        {
            Session["uname"] = null;
            Session.Abandon();
            Response.BufferOutput = true;
            Response.Redirect("Default.aspx", false);
        }

        protected void ChangePasswordEventMethod(object sender, EventArgs e)
        {
            Response.BufferOutput = true;
            Response.Redirect("Change_Password.aspx", false);
        }

        protected void SaveCustomerEventMethod(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAddCustomerID.Text) ||
                string.IsNullOrWhiteSpace(txtAddCustomerName.Text) ||
                string.IsNullOrWhiteSpace(txtAddCustomerEmail.Text) ||
                string.IsNullOrWhiteSpace(txtAddCustomerPhone.Text) ||
                string.IsNullOrWhiteSpace(txtAddCustomerAddress.Text) ||
                string.IsNullOrWhiteSpace(txtAddPackageType.Text) ||
                string.IsNullOrWhiteSpace(txtAddPackagePrice.Text))
            {
                addMessage.Text = "All fields are required";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#addCustomerModal').modal('show');", true);
                return;
            }

            string saveQuery = "INSERT INTO webapp.customers SET customer_ID ='"+ txtAddCustomerID.Text + "',name ='"+ txtAddCustomerName.Text + "',email ='"+ txtAddCustomerEmail.Text + "',phone ='"+ txtAddCustomerPhone.Text + "',address ='"+ txtAddCustomerAddress.Text + "',package_type ='"+ txtAddPackageType.Text + "',package_price ='"+ txtAddPackagePrice.Text + "'";

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(saveQuery, conn))
                    {                     
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            PopulateCustomerTable();
                            addMessage.Text = "";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "alert('Customer added successfully!'); $('#addCustomerModal').modal('hide');", true);
                        }
                        else
                        {
                            addMessage.Text = "Failed to add customer";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                                "$('#addCustomerModal').modal('show');", true);
                        }
                    }
                }
            }
            catch
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#addCustomerModal').modal('show');", true);
            }
        }
        protected void UpdateCustomerEventMethod(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUpdateCustomerID.Text) ||
                string.IsNullOrWhiteSpace(txtUpdateCustomerName.Text) ||
                string.IsNullOrWhiteSpace(txtUpdateCustomerEmail.Text) ||
                string.IsNullOrWhiteSpace(txtUpdateCustomerPhone.Text) ||
                string.IsNullOrWhiteSpace(txtUpdateCustomerAddress.Text) ||
                string.IsNullOrWhiteSpace(txtUpdatePackageType.Text) ||
                string.IsNullOrWhiteSpace(txtUpdatePackagePrice.Text))
            {
                updateMessage.Text = "All fields are required";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#updateCustomerModal').modal('show');", true);
                return;
            }

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(updateQuery, conn))
                    {
                      
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            PopulateCustomerTable();
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "alert('Customer updated successfully!'); $('#updateCustomerModal').modal('hide');", true);
                        }
                        else
                        {
                            updateMessage.Text = "No customer found with this ID";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                                "$('#updateCustomerModal').modal('show');", true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateMessage.Text = "Error updating customer: " + ex.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#updateCustomerModal').modal('show');", true);
            }
        }
        protected void DeleteCustomerEventMethod(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDeleteCustomerID.Text))
            {
                deleteMessage.Text = "Customer ID is required";
                // Prevent the modal from closing
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#deleteCustomerModal').modal('show');", true);
                return;
            }

            string delquery = "DELETE FROM webapp.customers WHERE customer_ID ='"+ txtDeleteCustomerID.Text + "'";
            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(delquery, conn))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            PopulateCustomerTable();
                            deleteMessage.Text = "";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "alert",
                                "alert('Customer deleted successfully!'); $('#deleteCustomerModal').modal('hide');", true);
                        }
                        else
                        {
                            deleteMessage.Text = "No customer found with this ID";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                                "$('#deleteCustomerModal').modal('show');", true);
                        }
                    }
                }
            }
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "keepModalOpen",
                    "$('#deleteCustomerModal').modal('show');", true);
            }
        }



    }
}