using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using Dapper;
using Encrypt_Utils;
using Microsoft.Reporting.Map.WebForms.BingMaps;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportingServices.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;


namespace PrintingAppQR
{
    public partial class PrintBill : Form
    {

        public class Table_Watcher
        {

            ReceiptPrinter rp = new ReceiptPrinter();
            string sqlconn = ConfigurationManager.ConnectionStrings["Connection"].ToString();
            public string ConnectionString()
            {

                string conn = string.Empty;
                var decryptedString = AesOperation.DecryptString("ATSWMN@!", sqlconn);
                SqlConnection sqlcon = new SqlConnection();
                sqlcon.ConnectionString = decryptedString;

                conn = sqlcon.ConnectionString;
                return conn;
            }
            public string _connectionString;

            private SqlTableDependency<FlightDetails> _dependency;
            public void WatchTable()
            {
                ReceiptPrinter rp = new ReceiptPrinter();
                try
                {
                    rp.WriteLog("Printing App Started " + DateTime.Now);
                    _connectionString = ConnectionString();
                    var mapper = new ModelToTableMapper<FlightDetails>();
                    mapper.AddMapping(model => model.Id, "Id");
                    mapper.AddMapping(model => model.IsChange, "IsChange");

                    _dependency = new SqlTableDependency<FlightDetails>(_connectionString, "FlightDetails");
                    _dependency.OnChanged += _dependency_OnChanged;
                    _dependency.OnError += _dependency_OnError;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        rp.WriteLog("Connectivity Error occured: " + ex.InnerException.Message);
                    }
                    rp.WriteLog("Connectivity Error: " + ex.Message);
                    RestartDependency();
                }

            }

            private void RestartDependency()
            {
                rp.WriteLog("Printing App RestartDependency " + DateTime.Now);
                if (_dependency != null)
                {
                    _dependency.OnChanged -= _dependency_OnChanged;
                    _dependency.Dispose();
                    _dependency = null;
                }
                WatchTable();
                StartTableWatcher();
            }
            public void StartTableWatcher()
            {
                _dependency.Start();
            }
            public void StopTableWatcher()
            {
                //_dependency.Stop();
                rp.WriteLog("StopTableWatcher");
                RestartDependency();
            }
            void _dependency_OnError(object sender, ErrorEventArgs e)
            {
                rp.WriteLog("_dependency_OnError " + e.Message);
                RestartDependency();
            }

            void _dependency_OnChanged(object sender, RecordChangedEventArgs<FlightDetails> e)
            {
                ReceiptPrinter rp = new ReceiptPrinter();
                rp.WriteLog("Printing Started " + DateTime.Now);

                try
                {
                    if (e.ChangeType != ChangeType.None)
                    {
                        rp.WriteLog("Order " + e.ChangeType);
                        if (e.ChangeType == ChangeType.Insert)
                        {
                            int id = e.Entity.Id;
                            using (SqlConnection sqlcon = new SqlConnection(_connectionString))
                            {
                                //string strsql = "Select 'Food' ITEM,ItemQty Qty, Rate,Amount from orderDetail Where OrderMasterId = @OrderMasterId";
                                string strsql = "SP_ReciptPrintQR";
                                string PrinterName = ConfigurationManager.AppSettings["PrinterName"];

                                if (PrinterName == "")
                                {
                                    PrinterSettings settings = new PrinterSettings();
                                    PrinterName = settings.PrinterName;
                                }
                                rp.WriteLog("PrinterName " + PrinterName);

                                try
                                {
                                    rp.WriteLog("Connecting DB");
                                    sqlcon.Open();

                                    SqlDataAdapter da = new SqlDataAdapter(strsql, sqlcon);
                                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                                    da.SelectCommand.Parameters.AddWithValue("@Id", id);
                                    //da.SelectCommand.Parameters.AddWithValue("@ResturantId", 1);

                                    Products dsProduct = new Products();

                                    da.Fill(dsProduct, "dtProduct");
                                    //return dsCustomers;
                                    rp.WriteLog("SP: SP_ReciptPrintQR     FlightId: " + id);
                                    rp.WriteLog("Item Count: " + dsProduct.dtProduct.Count);
                                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
                                    if (dsProduct.dtProduct.Count > 0)
                                    {
                                        try
                                        {
                                            rp.WriteLog("Printing...");

                                            //LocalReport localReport = new LocalReport();
                                            //localReport.DataSources.Add(datasource);
                                            //localReport.ReportPath = Application.StartupPath + "\\Report1.rdlc";

                                            //// Refresh the report viewer to populate the data
                                            //localReport.Refresh();
                                            //// Render the report as PDF
                                            //byte[] pdfBytes = localReport.Render("PDF");

                                            //// Save the PDF bytes to a temporary file
                                            //string pdfFilePath = System.IO.Path.GetTempFileName() + ".pdf";
                                            //System.IO.File.WriteAllBytes(pdfFilePath, pdfBytes);

                                            //localReport.PrintToPrinter();

                                            ReportDocument rptDoc = new ReportDocument();
                                            rptDoc.Load(Application.StartupPath + "\\Report1.rpt");
                                            rptDoc.Database.Tables[0].SetDataSource(dsProduct.Tables[0]);
                                            PageMargins objPageMargins;
                                            objPageMargins = rptDoc.PrintOptions.PageMargins;
                                            objPageMargins.bottomMargin = 100;
                                            objPageMargins.leftMargin = 100;
                                            objPageMargins.rightMargin = 100;
                                            objPageMargins.topMargin = 100;
                                            rptDoc.PrintOptions.ApplyPageMargins(objPageMargins);
                                            rptDoc.PrintOptions.PrinterName = PrinterName;
                                            rptDoc.PrintToPrinter(1, false, 0, 0);
                                            rptDoc.Close();
                                            rptDoc.Dispose();
                                            rptDoc = null;
                                            GC.Collect();

                                            rp.WriteLog("Printed");

                                            //Process.Start("C:\\Users\\DELL\\Desktop\\PrintingAppKOT\\PrintingApp\\bin\\Release\\PrintingAppKOT.exe");

                                            //ReceiptPrinter rp = new ReceiptPrinter();

                                            //sqlcon.Close();
                                            //rp.PrintReceipt(dsProduct, PrinterName);

                                            //sqlcon.Open();
                                            //DynamicParameters param = new DynamicParameters();
                                            //param.Add("@OrderId", id);
                                            //SqlMapper.QueryFirstOrDefault<string>(
                                            //                  sqlcon, "SP_ReciptPrintUpdate", param, commandType: CommandType.StoredProcedure);


                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.InnerException != null)
                                            {
                                                rp.WriteLog("Error occured: " + ex.InnerException.Message);
                                            }
                                            rp.WriteLog("Error: " + ex.Message);

                                            try
                                            {
                                                ReportDataSource datasource = new ReportDataSource("Products", dsProduct.Tables[0]);

                                                LocalReport localReport = new LocalReport();
                                                localReport.DataSources.Add(datasource);
                                                localReport.ReportPath = Application.StartupPath + "\\Report1.rdlc";
                                                localReport.PrintToPrinter();
                                            }
                                            catch (Exception exe)
                                            {
                                                if (exe.InnerException != null)
                                                {
                                                    rp.WriteLog("Error occured: " + exe.InnerException.Message);
                                                }

                                                rp.WriteLog("Error: " + exe.Message);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex.InnerException != null)
                                    {
                                        rp.WriteLog("Error occured: " + ex.InnerException.Message);
                                    }
                                    rp.WriteLog("Error: " + ex.Message);
                                }

                            }

                        }
                        if (e.ChangeType == ChangeType.Update)
                        {
                            int id = e.Entity.Id;
                            using (SqlConnection sqlcon = new SqlConnection(_connectionString))
                            {
                                //string strsql = "Select 'Food' ITEM,ItemQty Qty, Rate,Amount from orderDetail Where OrderMasterId = @OrderMasterId";
                                string strsql = "SP_ReciptPrintQR";
                                string PrinterName = ConfigurationManager.AppSettings["PrinterName"];

                                if (PrinterName == "")
                                {
                                    PrinterSettings settings = new PrinterSettings();
                                    PrinterName = settings.PrinterName;
                                }
                                rp.WriteLog("PrinterName " + PrinterName);

                                try
                                {
                                    rp.WriteLog("Connecting DB");
                                    sqlcon.Open();
                                    SqlDataAdapter da = new SqlDataAdapter(strsql, sqlcon);
                                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                                    da.SelectCommand.Parameters.AddWithValue("@Id", id);
                                    //da.SelectCommand.Parameters.AddWithValue("@ResturantId", 1);

                                    Products dsProduct = new Products();

                                    da.Fill(dsProduct, "dtProduct");
                                    //return dsCustomers;
                                    rp.WriteLog("SP: SP_ReciptPrintQR     FlightId: " + id);
                                    rp.WriteLog("Item Count: " + dsProduct.dtProduct.Count);
                                    if (dsProduct.dtProduct.Count > 0)
                                    {
                                        try
                                        {

                                            rp.WriteLog("Printing...");

                                            ReportDocument rptDoc = new ReportDocument();
                                            rptDoc.Load(Application.StartupPath + "\\Report1.rpt");
                                            rptDoc.Database.Tables[0].SetDataSource(dsProduct.Tables[0]);
                                            PageMargins objPageMargins;
                                            objPageMargins = rptDoc.PrintOptions.PageMargins;
                                            objPageMargins.bottomMargin = 100;
                                            objPageMargins.leftMargin = 100;
                                            objPageMargins.rightMargin = 100;
                                            objPageMargins.topMargin = 100;
                                            rptDoc.PrintOptions.ApplyPageMargins(objPageMargins);
                                            rptDoc.PrintOptions.PrinterName = PrinterName;
                                            rptDoc.PrintToPrinter(1, false, 0, 0);
                                            rptDoc.Close();
                                            rptDoc.Dispose();
                                            rptDoc = null;
                                            GC.Collect();

                                            rp.WriteLog("Printed");

                                            // Initialize the report viewer
                                            //ReportViewer reportViewer = new ReportViewer();
                                            //reportViewer.ProcessingMode = ProcessingMode.Local;

                                            //// Set the path to your RDLC file
                                            //reportViewer.LocalReport.ReportPath = Application.StartupPath + "\\Report1.rdlc";

                                            //// Set up your data source
                                            //reportViewer.LocalReport.DataSources.Add(datasource);

                                            //// Refresh the report viewer to populate the data
                                            //reportViewer.LocalReport.Refresh();

                                            //byte[] pdfBytes = reportViewer.LocalReport.Render("PDF");

                                            //// Save the PDF bytes to a temporary file
                                            //string pdfFilePath = System.IO.Path.GetTempFileName() + ".pdf";
                                            //System.IO.File.WriteAllBytes(pdfFilePath, pdfBytes);
                                            //// Set the report's printer settings
                                            //PrintDocument pd = new PrintDocument();
                                            //pd.PrinterSettings.PrintFileName = pdfFilePath;
                                            //pd.DefaultPageSettings.PaperSize = new PaperSize("pprnm", 285, 600);
                                            //pd.Print();

                                            //Process.Start("C:\\Users\\DELL\\Desktop\\PrintingAppKOT\\PrintingApp\\bin\\Release\\PrintingAppKOT.exe");

                                            //ReceiptPrinter rp = new ReceiptPrinter();
                                            //sqlcon.Close();
                                            //rp.PrintReceipt(dsProduct, PrinterName);

                                            //ReceiptPrinter rp = new ReceiptPrinter();
                                            //    sqlcon.Close();
                                            //    rp.PrintReceipt(dsProduct, PrinterName);

                                            //    sqlcon.Open();
                                            //    DynamicParameters param = new DynamicParameters();
                                            //    param.Add("@OrderId", id);
                                            //    SqlMapper.QueryFirstOrDefault<string>(
                                            //                      sqlcon, "SP_ReciptPrintUpdate", param, commandType: CommandType.StoredProcedure);


                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.InnerException != null)
                                            {
                                                rp.WriteLog("Error occured: " + ex.InnerException.Message);
                                            }

                                            rp.WriteLog("Error: " + ex.Message);

                                            try
                                            {
                                                ReportDataSource datasource = new ReportDataSource("Products", dsProduct.Tables[0]);

                                                LocalReport localReport = new LocalReport();
                                                localReport.DataSources.Add(datasource);
                                                localReport.ReportPath = Application.StartupPath + "\\Report1.rdlc";
                                                localReport.PrintToPrinter();
                                            }
                                            catch (Exception exe)
                                            {
                                                if (exe.InnerException != null)
                                                {
                                                    rp.WriteLog("Error occured: " + exe.InnerException.Message);
                                                }

                                                rp.WriteLog("Error: " + exe.Message);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex.InnerException != null)
                                    {
                                        rp.WriteLog("Error occured: " + ex.InnerException.Message);
                                    }

                                    rp.WriteLog("Error: " + ex.Message);
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        rp.WriteLog("Error occured: " + ex.InnerException.Message);
                    }

                    rp.WriteLog("Error: " + ex.Message);
                }

            }
        }

        NotifyIcon notifyIcon1 = new NotifyIcon();
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.ComponentModel.IContainer components1;
        public PrintBill()
        {
            InitializeComponent();
            components1 = new System.ComponentModel.Container();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
        }

        private void PrintBill_Load(object sender, EventArgs e)
        {

            Table_Watcher tw = new Table_Watcher();
            tw.WatchTable();
            tw.StartTableWatcher();

            this.ShowInTaskbar = true;

            this.Hide();

            this.contextMenu1.MenuItems.AddRange(
                   new System.Windows.Forms.MenuItem[] { this.menuItem1 });


            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += MenuItem1_Click;

            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components1);


            notifyIcon1.Icon = new System.Drawing.Icon(Application.StartupPath + @"\sms.ico");
            notifyIcon1.Visible = true;

            notifyIcon1.ContextMenu = this.contextMenu1;
        }
        private void MenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PrintBill_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

    }
}
