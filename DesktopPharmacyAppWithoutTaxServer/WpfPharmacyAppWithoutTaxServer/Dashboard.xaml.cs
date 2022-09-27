﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfServerApp.Services;
using WpfServerApp.Services.Accounts;

namespace WpfServerApp
{
    
    public partial class MainWindow : Window
    {
        ServiceHost hostCashReceiptService;
        ServiceHost hostCashPaymentService;
        ServiceHost hostBankDepositService;
        ServiceHost hostbankWithdrawalService;
        ServiceHost hostJournalVoucherService;
        ServiceHost hostOpeningBalanceService;

        ServiceHost hostLedgerService;
        ServiceHost hostBillNoService;
        ServiceHost hostUnitService;
        ServiceHost hostProductService;
        ServiceHost hostPurchaseService;
        ServiceHost hostPurchaseReturnService;
        ServiceHost hostSalesService;
        ServiceHost hostSalesReturnService;
        ServiceHost hostStockAdditionService;
        ServiceHost hostStockDeletionService;        

        public MainWindow()
        {
            InitializeComponent();

            //Initialising host object
            hostCashReceiptService = new ServiceHost(typeof(Services.Accounts.CashReceiptService));
            hostCashPaymentService = new ServiceHost(typeof(Services.Accounts.CashPaymentService));
            hostBankDepositService = new ServiceHost(typeof(Services.Accounts.BankDepositService));
            hostbankWithdrawalService = new ServiceHost(typeof(Services.Accounts.BankWithdrawalService));
            hostJournalVoucherService = new ServiceHost(typeof(Services.Accounts.JournalVoucherService));
            hostOpeningBalanceService = new ServiceHost(typeof(Services.Accounts.OpeningBalanceService));

            hostLedgerService = new ServiceHost(typeof(Services.Accounts.LedgerService));
            hostBillNoService = new ServiceHost(typeof(Services.BillNoService));            
            hostUnitService = new ServiceHost(typeof(Services.UnitService));
            hostProductService = new ServiceHost(typeof(Services.ProductService));
            hostPurchaseService = new ServiceHost(typeof(Services.PurchaseService));
            hostPurchaseReturnService = new ServiceHost(typeof(Services.PurchaseReturnService));
            hostSalesService = new ServiceHost(typeof(Services.SalesService));
            hostSalesReturnService = new ServiceHost(typeof(Services.SalesReturnService));
            hostStockAdditionService = new ServiceHost(typeof(Services.StockAdditionService));
            hostStockDeletionService = new ServiceHost(typeof(Services.StockDeletionService));

            hostCashReceiptService.Open();
            hostCashPaymentService.Open();
            hostBankDepositService.Open();
            hostbankWithdrawalService.Open();
            hostJournalVoucherService.Open();
            hostOpeningBalanceService.Open();

            hostBillNoService.Open();
            hostLedgerService.Open();
            hostUnitService.Open();
            hostProductService.Open();
            hostPurchaseService.Open();
            hostPurchaseReturnService.Open();
            hostSalesService.Open();
            hostSalesReturnService.Open();
            hostStockAdditionService.Open();
            hostStockDeletionService.Open();            

            //Loading the Unique Ledgers
            LedgerService ls = new LedgerService();
            ls.LoadAllUniqueLedgers();
            //Loading barcode characters
            BarcodeService bs = new BarcodeService();
            bs.initialiseBarcodeService();

            Console.WriteLine("Services are started and running");
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            //Closing the service hoster
            hostCashReceiptService.Close();
            hostCashPaymentService.Close();
            hostBankDepositService.Close();
            hostbankWithdrawalService.Close();
            hostJournalVoucherService.Close();
            hostOpeningBalanceService.Close();

            hostLedgerService.Close();
            hostBillNoService.Close();
            hostUnitService.Close();
            hostProductService.Close();
            hostPurchaseService.Close();
            hostPurchaseReturnService.Close();
            hostSalesService.Close();
            hostSalesReturnService.Close();
            hostStockAdditionService.Close();
            hostStockDeletionService.Close();            

            Console.WriteLine("Services are stopped");
        }
    }
}
