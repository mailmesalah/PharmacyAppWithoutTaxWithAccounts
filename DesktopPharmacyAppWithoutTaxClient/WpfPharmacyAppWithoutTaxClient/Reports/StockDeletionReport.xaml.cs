﻿using ServerServiceInterface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfClientApp.General;
using WpfClientApp.Transactions;

namespace WpfClientApp.Reports
{
    /// <summary>
    /// </summary>
    public partial class StockDeletionReport : Window
    {

        //For managing summary and detailed reports
        private const int SUMMARY = 0;
        private const int DETAILED = 1;
        private static int Report = SUMMARY;

        private const string mBillType = "SD";

        public StockDeletionReport()
        {
            InitializeComponent();

            //Methods
            loadFinancialCodes();
            loadProducts();
        }

        //Member methods
    
        private void loadProducts()
        {
            try
            {
                using (ChannelFactory<IProduct> productProxy = new ChannelFactory<ServerServiceInterface.IProduct>("ProductEndpoint"))
                {
                    productProxy.Open();
                    IProduct productService = productProxy.CreateChannel();
                    List<CProduct> products = productService.ReadAllProducts();
                    mComboProduct.ItemsSource = products;
                    mComboProduct.DisplayMemberPath = "Product";
                    mComboProduct.SelectedValuePath = "ProductCode";
                }
            }
            catch
            {
             
            }
        }
        
        private void loadFinancialCodes()
        {
            try
            {
                using (ChannelFactory<IBillNo> billNoProxy = new ChannelFactory<ServerServiceInterface.IBillNo>("BillNoEndpoint"))
                {
                    billNoProxy.Open();
                    IBillNo billNoService = billNoProxy.CreateChannel();

                    List<String> fcodes = billNoService.ReadAllFinancialCodes();
                    mComboFinancialYear.ItemsSource = fcodes;                    
                }
               
            }
            catch
            {                
            }
        }
        
        private void showDataFromDatabase()
        {
            try
            {
                string billNo=mTextBoxBillNo.Text.Trim();
                string productCode="";
                if (mComboProduct.SelectedItem != null && (mComboProduct.SelectedItem as CProduct).Product.Equals(mComboProduct.Text))
                {
                    productCode = mComboProduct.SelectedValue.ToString();
                }
                string product=mComboProduct.Text.Trim();
                string narration = mTextBoxNarration.Text.Trim(); string batch = mTextBoxBatch.Text.Trim();
              
                string financialCode="";
                if (mComboFinancialYear.SelectedItem != null)
                {
                    financialCode = mComboFinancialYear.Text.Trim();
                }

                using (ChannelFactory<IStockDeletion> stockDeletionProxy = new ChannelFactory<ServerServiceInterface.IStockDeletion>("StockDeletionEndpoint"))
                {
                    stockDeletionProxy.Open();
                    IStockDeletion stockDeletionService = stockDeletionProxy.CreateChannel();
                    if (Report == SUMMARY)
                    {
                        //Restructuring Colums                        
                        mDataGrid.Columns.Clear();

                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Date", Binding = new System.Windows.Data.Binding("BillDateTime"), Width = 120, IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Bill No", Binding = new System.Windows.Data.Binding("BillNo"), Width = 100, IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Narration", Binding = new System.Windows.Data.Binding("Narration"), Width = new DataGridLength(100, DataGridLengthUnitType.Star), IsReadOnly = true });
                        System.Windows.Data.Binding b1 = new System.Windows.Data.Binding("BillAmount"); b1.StringFormat = "N2";
                        System.Windows.Data.Binding b2 = new System.Windows.Data.Binding("Expense"); b2.StringFormat = "N2";
                        System.Windows.Data.Binding b3 = new System.Windows.Data.Binding("Discount"); b3.StringFormat = "N2";
                        System.Windows.Data.Binding b4 = new System.Windows.Data.Binding("Advance"); b4.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Bill Amount", Binding = b1, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });                        
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Expense", Binding = b2, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Discount", Binding = b3, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Advance", Binding = b4, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Financial Year", Binding = new System.Windows.Data.Binding("FinancialCode"), Width = 120, IsReadOnly = true });

                        mDataGrid.ItemsSource = stockDeletionService.FindStockDeletionSummary(mDTPStartDate.SelectedDate.Value, mDTPEndDate.SelectedDate.Value, mBillType, billNo, productCode, product, batch, mDTPExpiryDate.SelectedDate.Value, narration, financialCode);
                    }
                    else
                    {
                        //Restructuring Colums                        
                        mDataGrid.Columns.Clear();

                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Date", Binding = new System.Windows.Data.Binding("BillDateTime"), Width = 120, IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Bill No", Binding = new System.Windows.Data.Binding("BillNo"), Width = 100, IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Narration", Binding = new System.Windows.Data.Binding("Narration"), Width = new DataGridLength(100, DataGridLengthUnitType.Auto), IsReadOnly = true });

                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Sl No", Binding = new System.Windows.Data.Binding("SerialNo"), Width = 80, IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Product", Binding = new System.Windows.Data.Binding("Product"), Width = new DataGridLength(250, DataGridLengthUnitType.Auto), IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Batch", Binding = new System.Windows.Data.Binding("Batch"), Width = new DataGridLength(80, DataGridLengthUnitType.Auto), IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Expiry", Binding = new System.Windows.Data.Binding("ExpiryDate"), Width = new DataGridLength(80, DataGridLengthUnitType.Auto), IsReadOnly = true });
                        System.Windows.Data.Binding b5 = new System.Windows.Data.Binding("Quantity"); b5.StringFormat = "N3";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Quantity", Binding = b5, Width = 100, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Unit", Binding = new System.Windows.Data.Binding("StockDeletionUnit"), Width = new DataGridLength(80, DataGridLengthUnitType.Auto), IsReadOnly = true });
                        System.Windows.Data.Binding b6 = new System.Windows.Data.Binding("Tax"); b6.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Tax", Binding = b6, Width = 100, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        System.Windows.Data.Binding b7 = new System.Windows.Data.Binding("SalesRate"); b7.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Rate", Binding = b7, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        System.Windows.Data.Binding b8 = new System.Windows.Data.Binding("GrossValue"); b8.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Gross Value", Binding = b8, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                   
                        System.Windows.Data.Binding b11 = new System.Windows.Data.Binding("InterstateRate"); b11.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Interstate Rate", Binding = b11, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        System.Windows.Data.Binding b12 = new System.Windows.Data.Binding("WholesaleRate"); b12.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Wholesale Rate", Binding = b12, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        System.Windows.Data.Binding b13 = new System.Windows.Data.Binding("MRP"); b13.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "MRP", Binding = b13, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });

                        System.Windows.Data.Binding b1 = new System.Windows.Data.Binding("Total"); b1.StringFormat = "N2";
                        System.Windows.Data.Binding b2 = new System.Windows.Data.Binding("Expense"); b2.StringFormat = "N2";
                        System.Windows.Data.Binding b3 = new System.Windows.Data.Binding("Discount"); b3.StringFormat = "N2";
                        System.Windows.Data.Binding b4 = new System.Windows.Data.Binding("Advance"); b4.StringFormat = "N2";
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Total", Binding = b1, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Expense", Binding = b2, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Discount", Binding = b3, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Advance", Binding = b4, Width = 120, CellStyle = (Style)mDataGrid.Resources["ColRightAlign"], IsReadOnly = true });
                        mDataGrid.Columns.Add(new DataGridTextColumn() { Header = "Financial Year", Binding = new System.Windows.Data.Binding("FinancialCode"), Width = 120, IsReadOnly = true });

                        mDataGrid.ItemsSource = stockDeletionService.FindStockDeletionDetailed(mDTPStartDate.SelectedDate.Value, mDTPEndDate.SelectedDate.Value, mBillType, billNo, productCode, product, batch, mDTPExpiryDate.SelectedDate.Value, narration, financialCode);

                    }
                    
                }
            }
            catch(Exception e)
            {                
            }
        }
     
        

        //Events
        private void mButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }        
                
        private void mTextBoxBillNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            showDataFromDatabase();
        }
        
        private void mTextBoxNarration_TextChanged(object sender, TextChangedEventArgs e)
        {
            showDataFromDatabase();            
        }

        private void mDTPStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            showDataFromDatabase();            
        }

        private void mDTPEndDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            showDataFromDatabase();
        }        

        private void mComboProduct_SelectionChanged(object sender, RoutedEventArgs e)
        {
            showDataFromDatabase();
        }

        private void mDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (mDataGrid.SelectedItem != null)
            {
                if (Report == SUMMARY)
                {
                    CStockDeletionReportSummary cbd = mDataGrid.SelectedItem as CStockDeletionReportSummary;
                    if (cbd.BillNo != "" && cbd.BillDateTime != null)
                    {
                        StockDeletion bd = new StockDeletion(cbd.BillNo, (DateTime)cbd.BillDateTime);
                        bd.Show();
                    }
                }
                else
                {
                    CStockDeletionReportDetailed cbd = mDataGrid.SelectedItem as CStockDeletionReportDetailed;
                    if (cbd.BillNo != "" && cbd.BillDateTime != null)
                    {
                        StockDeletion bd = new StockDeletion(cbd.BillNo, (DateTime)cbd.BillDateTime);
                        bd.Show();
                    }
                }
            }
        }

        private void mButtonShowSummary_Click(object sender, RoutedEventArgs e)
        {
            Report = SUMMARY;
            showDataFromDatabase();
        }

        private void mButtonShowDetailed_Click(object sender, RoutedEventArgs e)
        {
            Report = DETAILED;
            showDataFromDatabase();
        }        
            
        private void mTextBoxTax_TextChanged(object sender, TextChangedEventArgs e)
        {
            showDataFromDatabase();
        }

        private void mComboCustomer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            showDataFromDatabase();
        }

        private void mComboFinancialYear_SelectionChanged(object sender, RoutedEventArgs e)
        {
            showDataFromDatabase();
        }

        private void mDTPExpiryDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            showDataFromDatabase();
        }

        private void mTextBoxBatch_TextChanged(object sender, TextChangedEventArgs e)
        {
            showDataFromDatabase();
        }
    }
}
