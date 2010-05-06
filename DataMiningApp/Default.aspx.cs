﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.DataVisualization.Charting;

namespace DataMiningApp
{
    public partial class _Default : System.Web.UI.Page
    {
        // Public variables necessary for data write on "Next" click
        string[, ,] controlarray;
        int max_layout_cols;
        int max_layout_rows;
        
        // Core variables
        int jobid = 1;
        int algorithmid = 1;
        int stepid = 1;

        // Define database connection objects
        SqlConnection connection;
        SqlCommand command;
        SqlDataReader reader;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Specify connection string to database

            // Microsoft Access
            //connection = new SqlConnection("Driver={Microsoft Access Driver (*.mdb)};DBQ=" + Server.MapPath("/App_Data/database.mdb") + ";UID=;PWD=;");

            // Microsoft SQL Server
            connection = new SqlConnection("Data Source=RANJAN-PC\\SQLEXPRESS;Initial Catalog=DMP;UId=webapp;Password=password;");

            // Create SQL query to find out table size
            string tablesize_query = "SELECT MAX(LAYOUT_X), MAX(LAYOUT_Y) FROM WEBAPP_LAYOUT";
            
            // Establish connection
            reader = openconnection(tablesize_query, connection);

            // Read table size
            reader.Read();
                max_layout_cols = (int)reader[0] + 1;
                max_layout_rows = (int)reader[1] + 1;
            closeconnection(reader, connection);

            // Create SQL query to pull table layout information for this job and step
            string layout_query = "SELECT LAYOUT_X, LAYOUT_Y, ROWSPAN, COLSPAN, CONTROL_TYPE, FILL_DATANAME, OUTPUT_DATANAME, CONST FROM WEBAPP_LAYOUT";
            command = new SqlCommand(layout_query, connection);

            // Open connection and execute query using SQL Reader
            connection.Open();
            reader = command.ExecuteReader();

            // Control array - last index is for control type (0), fill data name (1), and output data name (2)
            controlarray = new string[max_layout_cols, max_layout_rows, 3];
            
            // Span array control row and column spans
            int[, ,] spanarray = new int[max_layout_cols, max_layout_rows, 2];
            int layout_x, layout_y;

            // Read through layout table for this step and algorithm
            while (reader.Read())
            {
                // Populate control array
                layout_x = (int)reader[0];                                  // Table x index
                layout_y = (int)reader[1];                                  // Table y index
               
                controlarray[layout_x, layout_y, 0] = (string)reader[4];    // Control type
                controlarray[layout_x, layout_y, 1] = (string)reader[5];    // Fill data name
                controlarray[layout_x, layout_y, 2] = (string)reader[6];    // Output data name

                spanarray[layout_x, layout_y, 0] = (int)reader[2];          // Rowspan
                spanarray[layout_x, layout_y, 1] = (int)reader[3];          // Colspan
            }

            connection.Close();

            // Build interface

            // Evenly distribute width and height of cells to conform to panel
            // Panel is designed to show scroll bars in case cell contents force size larger than specified here
            string html_cellwidth = Convert.ToString((Convert.ToInt16(mainpanel.Width.ToString().Substring(0, mainpanel.Width.ToString().Length - 2)) - ((max_layout_cols)*layouttable.Border) - layouttable.CellPadding) / max_layout_cols) + "px";
            string html_cellheight = Convert.ToString((Convert.ToInt16(mainpanel.Height.ToString().Substring(0, mainpanel.Height.ToString().Length - 2)) - ((max_layout_rows)*layouttable.Border) - layouttable.CellPadding) / max_layout_rows) + "px";

            // Run through rows
            for (int row_traverse = 0; row_traverse < max_layout_rows; row_traverse++)
            {
                // Add row
                HtmlTableRow newrow = new HtmlTableRow();
                newrow.Height = html_cellheight;
                layouttable.Rows.Add(newrow);
                
                // Run through columns
                for (int col_traverse = 0; col_traverse < max_layout_cols; col_traverse++)
                {
                    // Check if this is a valid cell
                    if (spanarray[col_traverse, row_traverse, 0] > 0 && spanarray[col_traverse, row_traverse, 1] > 0)
                    {
                        // Create new cell object
                        HtmlTableCell newcell = new HtmlTableCell();

                        // Set column and row span properties (merge cells)
                        newcell.RowSpan = spanarray[col_traverse, row_traverse, 0];
                        newcell.ColSpan = spanarray[col_traverse, row_traverse, 1];

                        // Set cell width and height based on prior calculation
                        newcell.Width = html_cellwidth;
                        newcell.VAlign = "top";

                        // Add cell to table
                        layouttable.Rows[row_traverse].Cells.Add(newcell);
                    
                        // Add control, if applicable
                        Control newcontrol = addcontrol(controlarray, newcell, newrow, col_traverse, row_traverse);
                        
                        // Fill data into control
                        fillcontrol(newcontrol, controlarray, col_traverse, row_traverse, jobid, algorithmid, stepid, reader, connection);
                        
                    }
                }
            }
        }

        // CONTROL ADDITION -------------------------------------------------------------------------------------------------------------

        Control addcontrol(string[, ,] controlarray, HtmlTableCell cell, HtmlTableRow row, int col_traverse, int row_traverse)
        {
            // Generic return object
            Control returncontrol = new Control();

            // Specific object generation methods
            switch(controlarray[col_traverse, row_traverse, 0])
            {
                case "LABEL":   // Label control
                    {
                        // Create new control
                        Label newlabel = new Label();

                        // Set control properties
                        newlabel.Font.Name = "Arial"; newlabel.Font.Size = 11;
                        newlabel.ID = "control_" + col_traverse + "_" + row_traverse;

                        // Add control
                        cell.Controls.Add(newlabel);
                        returncontrol = newlabel;

                        break;
                    }
                case "TEXTBOX":
                    {
                        // Create new control
                        TextBox newtextbox = new TextBox();
                        Label newlabel = new Label();

                        // Set textbox control properties
                        newtextbox.Font.Name = "Arial"; newtextbox.Font.Size = 11;
                        newtextbox.ID = "control_" + col_traverse + "_" + row_traverse;
                        newtextbox.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0,cell.Width.Length-2))*cell.ColSpan - 2*(layouttable.Border + layouttable.CellPadding));

                        // Set label control properties
                        newlabel.Font.Name = "Arial"; newlabel.Font.Size = 11;

                        // Add control
                        cell.Controls.Add(newlabel);
                        cell.Controls.Add(new LiteralControl("<br><br>"));
                        cell.Controls.Add(newtextbox);
                        
                        // Return label for text fill
                        //returncontrol = newtextbox;
                        returncontrol = newlabel;
                        break;
                    }
                case "IMAGE":
                    {
                        // Create new control
                        Image newimage = new Image();

                        // Set control properties
                        newimage.ID = "control_" + col_traverse + "_" + row_traverse;
                        newimage.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        newimage.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));

                        // Add control
                        cell.Controls.Add(newimage);
                        returncontrol = newimage;

                        break;
                    }
                case "TABLE":
                    {
                        // Enclose table in panel
                        Panel tablepanel = new Panel();
                        tablepanel.ScrollBars = ScrollBars.Both;
                        tablepanel.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - (layouttable.Border + layouttable.CellPadding));
                        tablepanel.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - (layouttable.Border + layouttable.CellPadding));
                        
                        // Create new control
                        GridView newtable = new GridView();

                        // Set control properties
                        newtable.ID = "control_" + col_traverse + "_" + row_traverse;
                        newtable.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17));
                        newtable.Height = Unit.Pixel((int)(tablepanel.Height.Value - 17));
                        newtable.Font.Name = "Arial"; newtable.Font.Size = 11;
                        newtable.HeaderStyle.BackColor = System.Drawing.Color.Silver;
                        newtable.RowStyle.BackColor = System.Drawing.Color.White;
                        newtable.RowStyle.HorizontalAlign = HorizontalAlign.Center;

                        // Add control
                        tablepanel.Controls.Add(newtable);
                        cell.Controls.Add(tablepanel);
                        returncontrol = tablepanel;

                        break;
                    }
                case "SCATTERPLOT":
                    {
                        // Create new control
                        Chart chartcontrol = new Chart();

                        // Set chart width and height
                        chartcontrol.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        chartcontrol.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));

                        // Needed so server knows where to store temporary image
                        chartcontrol.ImageStorageMode = ImageStorageMode.UseImageLocation;

                        ChartArea mychartarea = new ChartArea();
                        chartcontrol.ChartAreas.Add(mychartarea);

                        Series myseries = new Series();
                        myseries.Name = "Series";
                        chartcontrol.Series.Add(myseries);

                        chartcontrol.Series["Series"].ChartType = SeriesChartType.Point;

                        // Add control
                        cell.Controls.Add(chartcontrol);
                        returncontrol = chartcontrol;
                        
                        break;
                    }
                case "UPLOAD":
                    {
                        // Create new controls
                        Label uploadlabel = new Label();
                        FileUpload uploadcontrol = new FileUpload();
                        HiddenField savedfile = new HiddenField(); HiddenField savedpath = new HiddenField();
                        Button uploadbutton = new Button();
                        GridView uploadtable = new GridView();

                        // Create panel to enclose table to it can scroll without having to scroll entire window
                        Panel tablepanel = new Panel();
                        tablepanel.ScrollBars = ScrollBars.Both;
                        tablepanel.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - (layouttable.Border + layouttable.CellPadding));
                        tablepanel.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - (layouttable.Border + layouttable.CellPadding));

                        // Set IDs for all controls (necessary to get information after postback on upload)
                        uploadlabel.ID = "control_" + col_traverse + "_" + row_traverse + "_label";
                        savedfile.ID = "control_" + col_traverse + "_" + row_traverse + "_savedfile";
                        savedpath.ID = "control_" + col_traverse + "_" + row_traverse + "_savedpath";
                        uploadcontrol.ID = "control_" + col_traverse + "_" + row_traverse;
                        uploadtable.ID = "control_" + col_traverse + "_" + row_traverse + "_table";
                        uploadbutton.ID = "control_" + col_traverse + "_" + row_traverse + "_button";

                        // Set control properties
                        uploadbutton.Text = "Load File";
                        uploadbutton.Font.Name = "Arial"; uploadbutton.Font.Size = 10;
                        uploadbutton.Width = 100;
                        uploadbutton.Click += new System.EventHandler(uploadbutton_Click);
                        uploadlabel.Font.Name = "Arial"; uploadlabel.Font.Size = 11;
                        uploadcontrol.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17) - (int)uploadbutton.Width.Value);
                        uploadtable.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17));
                        uploadtable.Height = Unit.Pixel((int)(tablepanel.Height.Value - 17));
                        uploadtable.Font.Name = "Arial"; uploadtable.Font.Size = 11;
                        uploadtable.HeaderStyle.BackColor = System.Drawing.Color.Silver;
                        uploadtable.RowStyle.BackColor = System.Drawing.Color.White;
                        uploadtable.RowStyle.HorizontalAlign = HorizontalAlign.Center;

                        // Add controls to form and format
                        tablepanel.Controls.Add(uploadlabel);
                        tablepanel.Controls.Add(new LiteralControl("<br><br>"));
                        tablepanel.Controls.Add(uploadcontrol);
                        tablepanel.Controls.Add(uploadbutton);
                        tablepanel.Controls.Add(new LiteralControl("<br><br>"));
                        tablepanel.Controls.Add(uploadtable);

                        // Add controls to scrollable panel
                        cell.Controls.Add(tablepanel);
                        
                        // Return uploadcontrol, even though this control itself does not need to be filled (need control type)
                        returncontrol = uploadcontrol;

                        break;
                    }

            }
            return returncontrol;

        }

        // CONTROL DATA FILL ------------------------------------------------------------------------------------------------------------

        void fillcontrol(Control fillcontrol, string[,,] controlarray, int col_traverse, int row_traverse, int jobid, int algorithmid, int stepid, SqlDataReader reader, SqlConnection connection)
        {
            // Fill data

            // Get fill query from controlarray
            string control_query = controlarray[col_traverse, row_traverse, 1];

            // Check if fill query is specified
            if (control_query != "NONE" && control_query != "")
            {
                // Add Job ID
                if (control_query != "CONST")
                {
                    control_query = control_query + " " + jobid;
                }
                else
                {
                    control_query = "WEBAPP_SELECTCONST " + algorithmid + "," + stepid + "," + col_traverse + "," + row_traverse;
                }

                // Initialize reader and get data
                reader = openconnection(control_query, connection);

                // Fill details are specific to control type
                switch(fillcontrol.GetType().ToString())
                {
                    // Label control type
                    case "System.Web.UI.WebControls.Label":
                        {
                            // Load label text into string and set control value
                            reader.Read();
                            string datavalue = (string)reader[0];
                            
                            // Create label control that points to fillcontrol object
                            Label labelcontrol = (Label)fillcontrol;

                            // Add label text
                            labelcontrol.Text = datavalue;
                            
                            break;
                        }
                    case "System.Web.UI.WebControls.Image":
                        {
                            // Load image into string and set control value
                            reader.Read();
                            string imagepath = (string)reader[0];

                            // Create image control that points to fillcontrol object
                            Image imagecontrol = (Image)fillcontrol;

                            // Add image path
                            imagecontrol.ImageUrl = imagepath;

                            break;
                        }
                    case "System.Web.UI.WebControls.Panel":
                        {
                            // Convert reader data to dataset
                            DataTable retrieveddataset;
                            retrieveddataset = db_dataretrieve(reader);

                            // Create GridView control that points to fillcontrol object
                            Panel tablecontainer = (Panel)fillcontrol;
                            GridView gridviewcontrol = (GridView)tablecontainer.Controls[0];

                            gridviewcontrol.DataSource = retrieveddataset;
                            gridviewcontrol.DataBind();

                            break;      
                        }
                    case "System.Web.UI.DataVisualization.Charting.Chart":
                        {
                            // Convert reader data to dataset
                            DataTable retrieveddataset;
                            retrieveddataset = db_dataretrieve(reader);

                            Chart chartcontrol = (Chart)fillcontrol;

                            // Set data plotted by returned column names
                            chartcontrol.Series["Series"].XValueMember = retrieveddataset.Columns[0].ColumnName;
                            chartcontrol.Series["Series"].YValueMembers = retrieveddataset.Columns[1].ColumnName;

                            chartcontrol.ChartAreas[0].AxisX.Title = chartcontrol.Series["Series"].XValueMember;
                            chartcontrol.ChartAreas[0].AxisY.Title = chartcontrol.Series["Series"].YValueMembers;

                            chartcontrol.DataSource = retrieveddataset;
                            chartcontrol.DataBind();
                            
                            break;
                        }
                    case "System.Web.UI.WebControls.FileUpload":
                        {
                            FileUpload uploadcontrol = (FileUpload)fillcontrol;
                            string id = uploadcontrol.ID;
                            
                            // Fill label
                            reader.Read();
                            string datavalue = (string)reader[0];
                            
                            Label uploadlabel = (Label)Form.FindControl(id + "_label");
                            uploadlabel.Text = datavalue;

                            break;
                        }
                }

                // Close reader and connection
                closeconnection(reader, connection);
            }
        }

        // DATABASE SUPPORT -------------------------------------------------------------------------------------------------------------
        
        // Reusable function to open data connection and execute reader given query string and SqlConnection object
        
        SqlDataReader openconnection(string query, SqlConnection connection)
        {
            SqlDataReader reader;
            SqlCommand command = new SqlCommand(query, connection);

            connection.Open();
            reader = command.ExecuteReader();

            return reader;
        }

        // Reusable function to close data connection and reader
        void closeconnection(SqlDataReader reader, SqlConnection connection)
        {
            reader.Close();
            connection.Close();
        }

        // DB DATA RETRIEVE AND CONVERT ------------------------------------------------------------------------------------------------

        DataTable db_dataretrieve(SqlDataReader reader)
        {
            // Create temporary data table to store data for return
            DataTable returndata = new DataTable();

            // Initialize row object, and add first row to data table
            int rowid = 0;
            DataRow currentrow = returndata.NewRow();
            //returndata.Rows.Add(currentrow);
            
            // Initialize column object
            DataColumn currentcol;
            
            // Loop through row, col, value records
            while (reader.Read())
            {  
                // If new row in source data, add row to data table
                if (rowid != (int)reader[0])
                {
                    // Add row
                    currentrow = returndata.NewRow();
                    returndata.Rows.Add(currentrow);
                    
                    // Set row counter to new row
                    rowid = (int)reader[0];
                }

                // If still the first row, any new record will be an additional column
                if ((int)reader[0] == 0)
                {
                    // Create new column
                    currentcol = new DataColumn();
                    currentcol.ColumnName = (string)reader[2];
                    returndata.Columns.Add(currentcol);
                }
                else
                {
                    // In any case, add value to current rol, col
                    currentrow[(int)reader[1]] = reader[2];
                }
            }

            // After loop through records, return temporary datatable
            return returndata;
        }

        // CONTROL DATA RETRIEVE -------------------------------------------------------------------------------------------------------

        string[,,] control_dataretrieve(Control outputcontrol)
        {          
            // Data to write - row, col, value
            string[, ,] datatowrite;

            // Dimensions of data - will come from specific control write implementations
            int max_rows;
            int max_cols;
            
            switch(outputcontrol.GetType().ToString())
            {
                case "System.Web.UI.WebControls.TextBox":
                {
                    max_rows = 1; max_cols = 1;
                    datatowrite = new string[max_rows, max_cols, 1];

                    // Create temporary text box object to retrieve value from generic control
                    TextBox datapull = new TextBox();
                    datapull = (TextBox)outputcontrol;

                    // Get value from text box
                    datatowrite[0,0,0] = datapull.Text;

                    break;
                }
                case "System.Web.UI.WebControls.FileUpload":
                {
                    FileUpload uploadcontrol = (FileUpload)outputcontrol;
                    string id = outputcontrol.ID;

                    GridView datatable = (GridView)Form.FindControl(id + "_table");

                    max_rows = datatable.Rows.Count; 
                    max_cols = datatable.Rows[0].Cells.Count;

                    datatowrite = new string[max_rows + 1, max_cols, 1];

                    for (int i = 1; i <= max_cols; i++)
                    {
                        datatowrite[0, i - 1, 0] = datatable.HeaderRow.Cells[i - 1].Text;
                    }

                    for(int j = 0; j < max_rows; j++)
                    {
                        for (int k = 0; k < max_cols; k++)
                        {
                            datatowrite[j+1,k,0] = datatable.Rows[j].Cells[k].Text;
                        }
                    }

                    break;
                }
                default:
                {
                    datatowrite = new string[1, 1, 1];
                    datatowrite[0, 0, 0] = null;
                    break;
                }

            }

            return datatowrite;
        }

        // INSERT DATA IN DATABASE -----------------------------------------------------------------------------------------------------

        void datawrite(string[, ,] datatowrite, string control_query, string control_id)
        {
            int row_counter; int col_counter;
            string execute_query;
            int total_rows = datatowrite.GetLength(0);
            int total_cols = datatowrite.GetLength(1);

            // Check if fill query is specified
            if (control_query != "NONE" && control_query != "")
            {
                // Add critical keys for data write to ALGORITHM_DATASTORE
                // JobID, StepID, Data_Name, Row_ID, Column_ID, Value

                for (row_counter = 0; row_counter < total_rows; row_counter++)
                {
                    for (col_counter = 0; col_counter < total_cols; col_counter++)
                    {
                        // Construct query
                        execute_query = control_query + " " + jobid + ", " + stepid + ",'" + control_id + "'," + row_counter + "," + col_counter + ",'" + datatowrite[row_counter, col_counter, 0] + "'";
                        
                        // Initialize reader and get data
                        reader = openconnection(execute_query, connection);
                        reader.Read();
                        closeconnection(reader, connection);
                    }
                }    
            }
        }

        // UPLOAD BUTTON HANDLER -------------------------------------------------------------------------------------------------------

        protected void uploadbutton_Click(object sender, EventArgs e)
        {
            // Get button ID
            Button getbuttonID = (Button)sender;
            string id = getbuttonID.ID.Replace("_button","");

            // Use button ID to find similarly named upload control ID
            FileUpload uploadcontrol = (FileUpload)Form.FindControl(id);

            // Only upload if control has file selected
            if (uploadcontrol.HasFile)
            {
                // Add upload path
                String savePath = @"c:\temp\";

                // Retrieve filename from upload control
                String fileName = uploadcontrol.FileName;

                // Save data to web server
                uploadcontrol.SaveAs(savePath + fileName);

                // Fill GridView

                // Establish text driver connection
                System.Data.Odbc.OdbcConnection csv_connection;
                System.Data.Odbc.OdbcDataAdapter csv_adapter;

                // Create temporary data table to store CSV data
                DataTable csv_data = new DataTable();

                // Create connection string and execute connection to CSV
                string csv_connectionString = @"Driver={Microsoft Text Driver (*.txt; *.csv)};Dbq=" + savePath + ";";
                csv_connection = new System.Data.Odbc.OdbcConnection(csv_connectionString);

                // Fill adapter with SELECT * query from CSV
                csv_adapter = new System.Data.Odbc.OdbcDataAdapter("select * from [" + fileName + "]", csv_connection);
                csv_adapter.Fill(csv_data);

                // Close CSV connection
                csv_connection.Close();

                // Find GridView and fill
                GridView filedata = (GridView)Form.FindControl(id + "_table");
                filedata.DataSource = csv_data;
                filedata.DataBind();
            }
        }
        
        // NEXT BUTTON HANDLER ---------------------------------------------------------------------------------------------------------

        protected void next_button_Click(object sender, EventArgs e)
        {
            // Create template control to operate on
            Control testcontrol;

            // Data storage set
            string[, ,] datatowrite;

            // Loop through cells in layout table looking for controls
            for (int row_traverse = 0; row_traverse < max_layout_rows; row_traverse++)
            {
                for (int col_traverse = 0; col_traverse < max_layout_cols; col_traverse++)
                {
                    // Check if cell has a control
                    testcontrol = (Control)Form.FindControl("control_" + col_traverse + "_" + row_traverse);

                    // If so, call data write function
                    if (testcontrol != null)
                    {
                        datatowrite = control_dataretrieve(testcontrol);
                        if (datatowrite != null)
                        {
                            datawrite(datatowrite, controlarray[col_traverse, row_traverse, 2], testcontrol.ID);
                        }                 
                    }
                }
            }

            // Move to next step
        }

    }
}
