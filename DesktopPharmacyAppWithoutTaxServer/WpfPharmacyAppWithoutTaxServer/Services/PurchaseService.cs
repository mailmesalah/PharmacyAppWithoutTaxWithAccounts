﻿using ServerServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfServerApp.General;
using WpfServerApp.Services.Accounts;

namespace WpfServerApp.Services
{   
    public class PurchaseService : IPurchase
    {        

        public bool CreateBill(CPurchase oPurchase, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {

                        BillNoService bs = new BillNoService();

                        //Add accounts entry
                        CreateAccountTransaction(oPurchase, billType);

                        int cbillNo = bs.ReadNextPurchaseBillNo(oPurchase.FinancialCode, billType);
                        bs.UpdatePurchaseBillNo(oPurchase.FinancialCode,cbillNo+1, billType);

                        List<string> barcodes = new BarcodeService().ReadBarcodes(oPurchase.Details.Count);
                        
                        for (int i = 0; i < oPurchase.Details.Count; i++)
                        {
                            product_transactions pt = new product_transactions();

                            pt.bill_no= cbillNo.ToString();
                            pt.bill_type = billType;
                            pt.bill_date_time = oPurchase.BillDateTime;
                            pt.supplier_code = oPurchase.SupplierCode;
                            pt.supplier = oPurchase.Supplier;
                            pt.supplier_address = oPurchase.SupplierAddress;
                            pt.narration = oPurchase.Narration;
                            pt.advance = oPurchase.Advance;
                            pt.extra_charges = oPurchase.Expense;
                            pt.discounts = oPurchase.Discount;
                            pt.financial_code = oPurchase.FinancialCode;

                            pt.serial_no = oPurchase.Details.ElementAt(i).SerialNo;
                            pt.product_code = oPurchase.Details.ElementAt(i).ProductCode;
                            pt.product = oPurchase.Details.ElementAt(i).Product;
                            pt.purchase_unit = oPurchase.Details.ElementAt(i).PurchaseUnit;
                            pt.purchase_unit_code = oPurchase.Details.ElementAt(i).PurchaseUnitCode;
                            pt.purchase_unit_value = oPurchase.Details.ElementAt(i).PurchaseUnitValue;
                            pt.quantity = oPurchase.Details.ElementAt(i).Quantity;
                            pt.product_discount= oPurchase.Details.ElementAt(i).ProductDiscount;
                            pt.purchase_rate = oPurchase.Details.ElementAt(i).PurchaseRate;
                            pt.interstate_rate= oPurchase.Details.ElementAt(i).InterstateRate;
                            pt.wholesale_rate = oPurchase.Details.ElementAt(i).WholesaleRate;
                            pt.mrp = oPurchase.Details.ElementAt(i).MRP;
                            pt.expiry_date = oPurchase.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oPurchase.Details.ElementAt(i).Batch;
                            //get a barcode here
                            pt.barcode = barcodes.ElementAt(i);                            
                            pt.unit_code= oPurchase.Details.ElementAt(i).PurchaseUnitCode;
                            pt.unit_value= oPurchase.Details.ElementAt(i).PurchaseUnitValue;

                            dataB.product_transactions.Add(pt);                            
                        }

                        dataB.SaveChanges();
                        //Success
                        returnValue = true;

                        dataBTransaction.Commit();
                    }
                    catch(Exception e)
                    {                        
                        dataBTransaction.Rollback();
                    }
                    finally
                    {

                    }
                }                
            }

            return returnValue;
        }

        public bool DeleteBill(string billNo, string billType, string financialCode)
        {
            bool returnValue = true;

            lock (Synchronizer.@lock)
            {
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {
                        //deleting accounts
                        LedgerService ls = new LedgerService();
                        ls.DeleteLedgerTransaction(billNo, billType, financialCode);

                        //Delete the transaction
                        //get barcodes already used in other transactions
                        BarcodeService bs = new BarcodeService();
                        List<string> usedBarcodes = ReadAlreadyUsedBarcodes(billNo,billType,financialCode);
                        //Remove editable entries of transaction
                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode && x.bill_type == billType && !usedBarcodes.Contains<string>(x.barcode));
                        dataB.product_transactions.RemoveRange(cpp);

                        //Updating serial numbers of entires that are already used
                        int serialNo = 1;
                        var tr = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode && x.bill_type == billType && usedBarcodes.Contains<string>(x.barcode));
                        foreach (var item in tr)
                        {
                            item.serial_no = serialNo++;
                        }

                        dataB.SaveChanges();                        
                        dataBTransaction.Commit();
                    }
                    catch
                    {
                        returnValue = false;
                        dataBTransaction.Rollback();
                    }
                    finally
                    {

                    }
                }
            }
            return returnValue;
        }

        public CPurchase ReadBill(string billNo, string billType, string financialCode)
        {
            CPurchase ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode&&x.bill_type==billType).OrderBy(y=>y.serial_no);
                
                if (cps.Count() > 0)
                {
                    ccp = new CPurchase();

                    var cp = cps.FirstOrDefault();
                    ccp.Id = cp.id;
                    ccp.BillNo = cp.bill_no;
                    ccp.BillDateTime = cp.bill_date_time;
                    ccp.SupplierCode = cp.supplier_code;
                    ccp.Supplier = cp.supplier;
                    ccp.SupplierAddress = cp.supplier_address;
                    ccp.Narration = cp.narration;
                    ccp.Advance = (decimal)cp.advance;
                    ccp.Expense = (decimal)cp.extra_charges;
                    ccp.Discount = (decimal)cp.discounts;
                    ccp.FinancialCode = cp.financial_code;

                    foreach (var item in cps)
                    {
                        decimal grossValue = (decimal)(item.quantity * item.purchase_rate);
                        ccp.Details.Add(new CPurchaseDetails() { SerialNo=(int)item.serial_no,ProductCode=item.product_code,Product=item.product, PurchaseUnit=item.purchase_unit, PurchaseUnitCode=item.purchase_unit_code, PurchaseUnitValue = (decimal)item.purchase_unit_value, Quantity=(decimal)item.quantity, PurchaseRate = (decimal)item.purchase_rate, MRP = (decimal)item.mrp, Total= (grossValue - (decimal)item.product_discount), Barcode = item.barcode, ProductDiscount=(decimal)item.product_discount, InterstateRate=(decimal)item.interstate_rate, WholesaleRate=(decimal)item.wholesale_rate, GrossValue=grossValue, ExpiryDate = item.expiry_date, Batch = item.batch });
                    }
                }
                
            }

            return ccp;
        }

        public int ReadNextBillNo(string billType, string financialCode)
        {
            
            BillNoService bns = new BillNoService();
            return bns.ReadNextPurchaseBillNo(financialCode, billType);
            
        }

        public bool UpdateBill(CPurchase oPurchase, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {
                        //Updating accounts
                        UpdateAccountTransaction(oPurchase, billType);

                        //get barcodes already used in other transactions
                        BarcodeService bs = new BarcodeService();                        
                        List<string> usedBarcodes =ReadAlreadyUsedBarcodes(oPurchase.BillNo,billType,oPurchase.FinancialCode);
                        //Remove editable entries of transaction
                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == oPurchase.BillNo&& x.financial_code==oPurchase.FinancialCode&&x.bill_type==billType && !usedBarcodes.Contains<string>(x.barcode));
                        dataB.product_transactions.RemoveRange(cpp);
                        
                        int serialNo = 1;
                        for (int i = 0; i < oPurchase.Details.Count; i++)
                        {
                            if (!usedBarcodes.Contains<string>(oPurchase.Details.ElementAt(i).Barcode))
                            {
                                product_transactions pt = new product_transactions();

                                pt.bill_no = oPurchase.BillNo;
                                pt.bill_type = billType;
                                pt.bill_date_time = oPurchase.BillDateTime;
                                pt.supplier_code = oPurchase.SupplierCode;
                                pt.supplier = oPurchase.Supplier;
                                pt.supplier_address = oPurchase.SupplierAddress;
                                pt.narration = oPurchase.Narration;
                                pt.advance = oPurchase.Advance;
                                pt.extra_charges = oPurchase.Expense;
                                pt.discounts = oPurchase.Discount;
                                pt.financial_code = oPurchase.FinancialCode;

                                pt.serial_no = serialNo++;
                                pt.product_code = oPurchase.Details.ElementAt(i).ProductCode;
                                pt.product = oPurchase.Details.ElementAt(i).Product;
                                pt.purchase_unit = oPurchase.Details.ElementAt(i).PurchaseUnit;
                                pt.purchase_unit_code = oPurchase.Details.ElementAt(i).PurchaseUnitCode;
                                pt.purchase_unit_value = oPurchase.Details.ElementAt(i).PurchaseUnitValue;
                                pt.quantity = oPurchase.Details.ElementAt(i).Quantity;
                                pt.product_discount = oPurchase.Details.ElementAt(i).ProductDiscount;
                                pt.purchase_rate = oPurchase.Details.ElementAt(i).PurchaseRate;
                                pt.interstate_rate = oPurchase.Details.ElementAt(i).InterstateRate;
                                pt.wholesale_rate = oPurchase.Details.ElementAt(i).WholesaleRate;
                                pt.mrp = oPurchase.Details.ElementAt(i).MRP;
                                pt.expiry_date = oPurchase.Details.ElementAt(i).ExpiryDate;
                                pt.batch = oPurchase.Details.ElementAt(i).Batch;
                                //get a barcode here                                
                                pt.barcode = oPurchase.Details.ElementAt(i).Barcode != "" ?  oPurchase.Details.ElementAt(i).Barcode: bs.ReadNextBarcode();
                                pt.unit_code = oPurchase.Details.ElementAt(i).PurchaseUnitCode;
                                pt.unit_value = oPurchase.Details.ElementAt(i).PurchaseUnitValue;

                                dataB.product_transactions.Add(pt);
                            }
                        }

                        //Updating serial numbers of entires that are already used
                        var tr = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == oPurchase.BillNo && x.financial_code == oPurchase.FinancialCode && x.bill_type == billType && usedBarcodes.Contains<string>(x.barcode));
                        foreach (var item in tr)
                        {
                            item.serial_no = serialNo++;
                        }

                        dataB.SaveChanges();

                        //Success
                        returnValue = true;

                        dataBTransaction.Commit();
                    }
                    catch(Exception e)
                    {                        
                        dataBTransaction.Rollback();
                    }
                    finally
                    {

                    }
                }
            }
            return returnValue;
        }

        private List<string> ReadAlreadyUsedBarcodes(string billNo, string billType, string fCode)
        {
            List<string> barcodes = new List<string>();
            List<string> usedBarcodes = new List<string>();
            using (var dataB = new Database9007Entities())
            {
                try
                {
                    var bar = dataB.product_transactions.Select(c => new { c.barcode, c.bill_type, c.bill_no, c.financial_code }).Where(x => x.bill_type == billType && x.bill_no == billNo && x.financial_code == fCode);
                    barcodes = bar.Select(e => e.barcode).ToList<string>();

                    var usedBar = dataB.product_transactions.Select(c => new { c.barcode, c.bill_type }).Where(x => x.bill_type != billType && barcodes.Contains<string>(x.barcode));
                    usedBarcodes = usedBar.Select(e => e.barcode).ToList<string>();
                }
                catch
                {

                }
            }
            return usedBarcodes;
        }

        private void CreateAccountTransaction(CPurchase oPurchase, string billType)
        {
            LedgerService ls = new LedgerService();

            //Finding ledger accounts for the type of transaction
            string purAccount = "";
            string purAccountCode = "";
            string purDisAccount = "";
            string purDisAccountCode = "";
            if (billType == "PI")
            {
                purAccount = "Interstate Purchase Account";
                purDisAccount = "Interstate Purchase Discounts";
                purAccountCode = UniqueLedgers.LedgerCode["Interstate Purchase Account"];
                purDisAccountCode = UniqueLedgers.LedgerCode["Interstate Purchase Discounts"];
            }
            else if (billType == "PW")
            {
                purAccount = "Wholesale Purchase Account";
                purDisAccount = "Wholesale Purchase Discounts";
                purAccountCode = UniqueLedgers.LedgerCode["Wholesale Purchase Account"];
                purDisAccountCode = UniqueLedgers.LedgerCode["Wholesale Purchase Discounts"];
            }

            //Finding bill amount
            decimal billAmount = oPurchase.Details.Sum(e => (e.Quantity * e.PurchaseRate) - e.ProductDiscount) + oPurchase.Expense;
            CLedgerTransaction cltBillAmount = new CLedgerTransaction()
            {
                BillDate = oPurchase.BillDateTime,
                RefBillNo = oPurchase.BillNo,
                RefBillType = billType,
                FinancialCode = oPurchase.FinancialCode,
                Amount = billAmount,
                DebitLedger = purAccount,
                DebitLedgerCode = purAccountCode,
                CreditLedger = oPurchase.Supplier,
                CreditLedgerCode = oPurchase.SupplierCode

            };

            ls.CreateLedgerTransaction(cltBillAmount);

            if (oPurchase.Discount > 0)
            {
                CLedgerTransaction cltDiscount = new CLedgerTransaction()
                {
                    BillDate = oPurchase.BillDateTime,
                    RefBillNo = oPurchase.BillNo,
                    RefBillType = billType,
                    FinancialCode = oPurchase.FinancialCode,
                    Amount = oPurchase.Discount,
                    DebitLedger = oPurchase.Supplier,
                    DebitLedgerCode = oPurchase.SupplierCode,
                    CreditLedger = purDisAccount,
                    CreditLedgerCode = purDisAccountCode

                };
                ls.CreateLedgerTransaction(cltDiscount);
            }

            if (oPurchase.Advance > 0)
            {
                CLedgerTransaction cltCash = new CLedgerTransaction()
                {
                    BillDate = oPurchase.BillDateTime,
                    RefBillNo = oPurchase.BillNo,
                    RefBillType = billType,
                    FinancialCode = oPurchase.FinancialCode,
                    Amount = oPurchase.Advance,
                    DebitLedger = oPurchase.Supplier,
                    DebitLedgerCode = oPurchase.SupplierCode,
                    CreditLedger = "Cash",
                    CreditLedgerCode = UniqueLedgers.LedgerCode["Cash"]

                };
                ls.CreateLedgerTransaction(cltCash);
            }
        }

        private void UpdateAccountTransaction(CPurchase oPurchase, string billType)
        {
            LedgerService ls = new LedgerService();
            //Deleting previous data
            ls.DeleteLedgerTransaction(oPurchase.BillNo, billType, oPurchase.FinancialCode);
            //Adding new data
            CreateAccountTransaction(oPurchase, billType);

        }

        //Reports
        public List<CPurchaseReportDetailed> FindPurchaseDetailed(DateTime startDate, DateTime endDate, string billType, string billNo, string supplierCode, string supplier, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CPurchaseReportDetailed> report = new List<CPurchaseReportDetailed>();

            using (var dataB = new Database9007Entities())
            {
                string startD = startDate.Year + "-" + startDate.Month + "-" + startDate.Day;
                string endD = endDate.Year + "-" + endDate.Month + "-" + endDate.Day;

                string billTypeQuery = billType.Trim().Equals("") ? "" : " && (bd.bill_type='" + billType.Trim() + "') ";
                string billNoQuery = billNo.Trim().Equals("") ? "" : " && (bd.bill_no='" + billNo.Trim() + "') ";
                string productCodeQuery = productCode.Trim().Equals("") ? "" : " && (bd.product_code='" + productCode.Trim() + "') ";
                string productQuery = product.Trim().Equals("") ? "" : " && (bd.product Like '%" + product.Trim() + "%') ";
                string expiryDateQuery = " && (bd.bill_date_time <= '" + expiryDate + "') ";
                string batchQuery = batch.Trim().Equals("") ? "" : " && (bd.batch Like '%" + batch.Trim() + "%') ";
                string supplierCodeQuery = supplierCode.Trim().Equals("") ? "" : " && (bd.supplier_code='" + supplierCode.Trim() + "') ";
                string supplierQuery = supplier.Trim().Equals("") ? "" : " && (bd.supplier Like '%" + supplier.Trim() + "%') ";
                string narrationQuery = narration.Trim().Equals("") ? "" : " && (bd.narration Like '%" + narration.Trim() + "%') ";
                string financialCodeQuery = financialCode.Trim().Equals("") ? "" : " && (bd.financial_code='" + financialCode.Trim() + "') ";
                

                string subQ = billNoQuery + productCodeQuery + productQuery + narrationQuery + financialCodeQuery + billTypeQuery + expiryDateQuery + batchQuery + supplierCodeQuery + supplierQuery;

                var resData = dataB.Database.SqlQuery<CPurchaseReportDetailed>("Select  ((bd.quantity * bd.purchase_rate)-bd.product_discount) As Total, (bd.quantity * bd.purchase_rate) As GrossValue, bd.quantity As Quantity, bd.expiry_date As ExpiryDate, bd.batch As Batch, bd.mrp As MRP, bd.wholesale_rate As WholesaleRate, bd.interstate_rate As InterstateRate, bd.purchase_rate As PurchaseRate, bd.product_discount As ProductDiscount, bd.purchase_unit As PurchaseUnit, bd.product As Product, bd.serial_no As SerialNo, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.supplier As Supplier, bd.supplier_address As SupplierAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + " Order By bd.bill_date_time,bd.bill_no, bd.serial_no");

                decimal? quantity = 0;
                decimal? grossValue = 0;
                decimal? proDiscount = 0;
                decimal? total = 0;
                foreach (var item in resData)
                {
                    quantity = quantity + item.Quantity;
                    grossValue = grossValue + item.GrossValue;
                    proDiscount = proDiscount + item.ProductDiscount;
                    total = total + item.Total;

                    report.Add(item);
                }
                //Total
                report.Add(new CPurchaseReportDetailed() { BillDateTime = null, SerialNo = null, Product = "" });
                report.Add(new CPurchaseReportDetailed() { BillDateTime = null, SerialNo = null, Product = "Total", Quantity = quantity, GrossValue = grossValue, ProductDiscount = proDiscount, Total = total });

            }


            return report;
        }

        public List<CPurchaseReportSummary> FindPurchaseSummary(DateTime startDate, DateTime endDate, string billType, string billNo, string supplierCode, string supplier, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CPurchaseReportSummary> report = new List<CPurchaseReportSummary>();

            using (var dataB = new Database9007Entities())
            {
                string startD = startDate.Year + "-" + startDate.Month + "-" + startDate.Day;
                string endD = endDate.Year + "-" + endDate.Month + "-" + endDate.Day;

                string billTypeQuery = billType.Trim().Equals("") ? "" : " && (bd.bill_type='" + billType.Trim() + "') ";
                string billNoQuery = billNo.Trim().Equals("") ? "" : " && (bd.bill_no='" + billNo.Trim() + "') ";
                string productCodeQuery = productCode.Trim().Equals("") ? "" : " && (bd.product_code='" + productCode.Trim() + "') ";
                string productQuery = product.Trim().Equals("") ? "" : " && (bd.product Like '%" + product.Trim() + "%') ";
                string expiryDateQuery = " && (bd.bill_date_time <= '" + expiryDate + "') ";
                string batchQuery = batch.Trim().Equals("") ? "" : " && (bd.batch Like '%" + batch.Trim() + "%') ";
                string supplierCodeQuery = supplierCode.Trim().Equals("") ? "" : " && (bd.supplier_code='" + supplierCode.Trim() + "') ";
                string supplierQuery = supplier.Trim().Equals("") ? "" : " && (bd.supplier Like '%" + supplier.Trim() + "%') ";
                string narrationQuery = narration.Trim().Equals("") ? "" : " && (bd.narration Like '%" + narration.Trim() + "%') ";
                string financialCodeQuery = financialCode.Trim().Equals("") ? "" : " && (bd.financial_code='" + financialCode.Trim() + "') ";
                

                string subQ = billNoQuery + productCodeQuery + productQuery + narrationQuery + financialCodeQuery + billTypeQuery + expiryDateQuery + batchQuery + supplierCodeQuery + supplierQuery;

                var resData = dataB.Database.SqlQuery<CPurchaseReportSummary>("Select Sum(((bd.quantity * bd.purchase_rate) - bd.product_discount)) As BillAmount, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.supplier As Supplier, bd.supplier_address As SupplierAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + "Group By bd.extra_charges, bd.discounts, bd.advance, bd.bill_date_time, bd.bill_no, bd.narration, bd.financial_code, bd.supplier, bd.supplier_address Order By bd.bill_date_time,bd.bill_no");

                decimal? billAmount = 0;
                foreach (var item in resData)
                {

                    billAmount = billAmount + item.BillAmount;
                    report.Add(item);
                }
                //Total
                report.Add(new CPurchaseReportSummary() { BillDateTime = null, BillNo = "" });
                report.Add(new CPurchaseReportSummary() { BillDateTime = null, Supplier = "Total", BillAmount = billAmount });

            }


            return report;
        }
    }
}
