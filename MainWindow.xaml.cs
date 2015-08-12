using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Oracle.ManagedDataAccess.Client;
using System.Data;
using Microsoft.Reporting.WinForms;
using System.Collections.Concurrent;

namespace SMReport
{

    public partial class MainWindow : Window
    {
        
        ConcurrentBag<ReportDataSM> list4R;


        private delegate void NoArgDelegate();
        delegate void MyGetDataDelegate(string s);

        public MainWindow()
        {
            InitializeComponent();
        }


        void getDataMethod(string docid)
        {

            DataSet dataset = new DataSet();

            string oradb = "Data Source=" + "DATABASENAME" + ";User Id=" + "typehereusername" + ";Password=" + "changetopassword" + ";";


            // you need to edit TNSNAMES.ORA file to have correct database information

            using (OracleConnection conn = new OracleConnection(oradb))
            {

                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();
                }
                catch (Exception exep)
                {
                    MessageBox.Show("connection opening error!" + exep.Message.ToString());
                    try
                    {
                        if (conn.State != ConnectionState.Closed) conn.Close();
                    }
                    catch
                    {
                        MessageBox.Show("connection closing error!");
                    }
                    return;
                }


                try
                {

                    string commtext = "select SMDocLog.EVENTTIME, SMDocLog.ID, SMDocLog.USERNAME, NVL(SSDocStates.DOCSTATENAME,'Was created') as NEWSTATE";
                    commtext = commtext + " from SMDocLog LEFT JOIN SSDocStates";
                    commtext = commtext + " ON SMDocLog.DOCTYPE=SSDocStates.DOCTYPE and SMDocLog.NEWSTATE=SSDocStates.DOCSTATE";
                    commtext = commtext + " WHERE ID='" + docid + "'";
                    commtext = commtext + " ORDER BY EVENTTIME DESC";



                    using (OracleCommand cmd = new OracleCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = commtext;
                        cmd.CommandType = CommandType.Text;

                        using (OracleDataAdapter adapterO = new OracleDataAdapter(cmd))
                        {
                            using (DataSet ds = new DataSet())
                            {

                                adapterO.Fill(ds);

                                foreach (DataRow dr in ds.Tables[0].Rows)
                                {
                                    ReportDataSM rd = new ReportDataSM();
                                    rd.EventTime = Convert.ToDateTime(dr["EVENTTIME"]);
                                    rd.UserName = dr["USERNAME"].ToString();
                                    rd.Doc = dr["ID"].ToString();
                                    rd.NewState = dr["NEWSTATE"].ToString();
                                    list4R.Add(rd);
                                }

                            } // end using DataSet
                        } // end using OracleDataAdapter
                    } // end using OracleCommand

                }

                catch (Exception er)
                {
                    MessageBox.Show(er.Message);
                }

            }

        }




        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            list4R = new ConcurrentBag<ReportDataSM>();

            menuStart.IsEnabled = false;
            txtInfo.Text = "Wait...";

            MyGetDataDelegate dlgt = new MyGetDataDelegate(this.getDataMethod);
            IAsyncResult ar = dlgt.BeginInvoke(txtDocN.Text, new AsyncCallback(CompletedCallback), null);

        }


         void CompletedCallback(IAsyncResult result)
        {
         
            Dispatcher.BeginInvoke(
   System.Windows.Threading.DispatcherPriority.Normal,
   new NoArgDelegate(UpdateUserInterface));

        }

         void UpdateUserInterface()
         {

             _reportViewer.Reset();

             ReportDataSource reportDataSource= new ReportDataSource("DataSet1", list4R);

             _reportViewer.LocalReport.ReportPath = "ReportSM.rdlc";
             _reportViewer.LocalReport.DataSources.Add(reportDataSource);


             _reportViewer.RefreshReport();

             menuStart.IsEnabled = true;
             txtInfo.Text = "";

         }

    }


    public class ReportDataSM
    {
        public DateTime EventTime { get; set; }
        public string Doc { get; set; }
        public string UserName { get; set; }
        public string NewState { get; set; }
    }


}
