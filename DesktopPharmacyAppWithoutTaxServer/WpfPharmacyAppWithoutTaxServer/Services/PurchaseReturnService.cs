﻿using ServerServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfServerApp.General;
using WpfServerApp.Services.Accounts;

namespace WpfServerApp.Services
{   
    public class PurchaseReturnService : IPurchaseReturn
    {        

        public bool CreateBill(CPurchaseReturn oPurchaseReturn, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {

                        ProductService ls = new ProductService();
                        BillNoService bs = new BillNoService();

                        // Account Transaction Entry
                        CreateAccountTransaction(oPurchaseReturn, billType);

                        int cbillNo = bs.ReadNextPurchaseReturnBillNo(oPurchaseReturn.FinancialCode, billType);
                        bs.UpdatePurchaseReturnBillNo(oPurchaseReturn.FinancialCode,cbillNo+1, billType);

                        List<string> barcodes = new BarcodeService().ReadBarcodes(oPurchaseReturn.Details.Count);
                        
                        for (int i = 0; i < oPurchaseReturn.Details.Count; i++)
                        {
                            product_transactions pt = new product_transactions();

                            pt.bill_no= cbillNo.ToString();
                            pt.bill_type = billType;                            
                            pt.bill_date_time = oPurchaseReturn.BillDateTime;
                            pt.ref_bill_no = oPurchaseReturn.RefBillNo;
                            pt.ref_bill_date_time = oPurchaseReturn.RefBillDateTime;
                            pt.supplier_code = oPurchaseReturn.SupplierCode;
                            pt.supplier = oPurchaseReturn.Supplier;
                            pt.supplier_address = oPurchaseReturn.SupplierAddress;
                            pt.narration = oPurchaseReturn.Narration;
                            pt.advance = oPurchaseReturn.Advance;
                            pt.extra_charges = oPurchaseReturn.Expense;
                            pt.discounts = oPurchaseReturn.Discount;
                            pt.financial_code = oPurchaseReturn.FinancialCode;

                            pt.serial_no = oPurchaseReturn.Details.ElementAt(i).SerialNo;
                            pt.product_code = oPurchaseReturn.Details.ElementAt(i).ProductCode;
                            pt.product = oPurchaseReturn.Details.ElementAt(i).Product;
                            pt.purchase_unit = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnit;
                            pt.purchase_unit_code = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitCode;
                            pt.purchase_unit_value = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitValue;
                            pt.quantity = oPurchaseReturn.Details.ElementAt(i).Quantity*-1;
                            pt.product_discount = oPurchaseReturn.Details.ElementAt(i).ProductDiscount;
                            pt.purchase_rate = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnRate;
                            pt.interstate_rate = oPurchaseReturn.Details.ElementAt(i).InterstateRate;
                            pt.wholesale_rate = oPurchaseReturn.Details.ElementAt(i).WholesaleRate;
                            pt.mrp = oPurchaseReturn.Details.ElementAt(i).MRP;
                            pt.expiry_date = oPurchaseReturn.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oPurchaseReturn.Details.ElementAt(i).Batch;                            
                            //get a barcode here
                            pt.barcode = barcodes.ElementAt(i);
                            pt.unit_code = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitCode;
                            pt.unit_value = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitValue;

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
                        LedgerService ls = new LedgerService();
                        ls.DeleteLedgerTransaction(billNo, billType, financialCode);

                        //Delete the transaction
                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode && x.bill_type == billType);
                        dataB.product_transactions.RemoveRange(cpp);
                        
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

        public CPurchaseReturn ReadBill(string billNo, string billType, string financialCode)
        {
            CPurchaseReturn ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode&&x.bill_type==billType).OrderBy(y=>y.serial_no);
                
                if (cps.Count() > 0)
                {
                    ccp = new CPurchaseReturn();

                    var cp = cps.FirstOrDefault();
                    ccp.Id = cp.id;
                    ccp.BillNo = cp.bill_no;
                    ccp.BillDateTime = cp.bill_date_time;
                    ccp.RefBillNo = cp.ref_bill_no;
                    ccp.RefBillDateTime = (DateTime)cp.ref_bill_date_time;
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
                        decimal grossValue = (decimal)(item.quantity * item.purchase_rate)*-1;
                        ccp.Details.Add(new CPurchaseReturnDetails() { SerialNo = (int)item.serial_no, ProductCode = item.product_code, Product = item.product, PurchaseReturnUnit = item.purchase_unit, PurchaseReturnUnitCode = item.purchase_unit_code, PurchaseReturnUnitValue = (decimal)item.purchase_unit_value, Quantity = (decimal)item.quantity, PurchaseReturnRate = (decimal)item.purchase_rate, MRP = (decimal)item.mrp, Total = (grossValue - (decimal)item.product_discount), Barcode = item.barcode, ProductDiscount = (decimal)item.product_discount, InterstateRate = (decimal)item.interstate_rate, WholesaleRate = (decimal)item.wholesale_rate, GrossValue = grossValue, OldQuantity = (decimal)item.quantity*-1, ExpiryDate=item.expiry_date, Batch=item.batch});
                    }
                }
                
            }

            return ccp;
        }

        public CPurchaseReturn ReadPurchaseBill(string billNo, string billType, string financialCode)
        {
            CPurchaseReturn ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode && x.bill_type == billType).OrderBy(y => y.serial_no);

                if (cps.Count() > 0)
                {
                    ccp = new CPurchaseReturn();

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

                    int serialNo = 1;
                    string retType = billType=="PI"?"PRI":"PRW";                   
                    foreach (var item in cps)
                    {
                        decimal quantity = ReadAvailableQuantityOfBarcode(item.barcode, item.purchase_unit_code, (decimal)item.purchase_unit_value,billNo,retType,financialCode);
                        if (quantity > 0)
                        {
                            decimal grossValue = (decimal)(item.quantity * item.purchase_rate);
                            ccp.Details.Add(new CPurchaseReturnDetails() { SerialNo = (int)serialNo++, ProductCode = item.product_code, Product = item.product, PurchaseReturnUnit = item.purchase_unit, PurchaseReturnUnitCode = item.purchase_unit_code, PurchaseReturnUnitValue = (decimal)item.purchase_unit_value, Quantity = (decimal)item.quantity, PurchaseReturnRate = (decimal)item.purchase_rate, MRP = (decimal)item.mrp, Total = (grossValue - (decimal)item.product_discount), Barcode = item.barcode, ProductDiscount = (decimal)item.product_discount, InterstateRate = (decimal)item.interstate_rate, WholesaleRate = (decimal)item.wholesale_rate, GrossValue = grossValue, OldQuantity=(decimal)item.quantity, ExpiryDate = item.expiry_date, Batch = item.batch });
                        }
                    }
                }

            }

            return ccp;
        }

        public int ReadNextBillNo(string billType, string financialCode)
        {
            
            BillNoService bns = new BillNoService();
            return bns.ReadNextPurchaseReturnBillNo(financialCode, billType);
            
        }

        public bool UpdateBill(CPurchaseReturn oPurchaseReturn, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {
                        //Update Account transaction
                        UpdateAccountTransaction(oPurchaseReturn, billType);

                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == oPurchaseReturn.BillNo&& x.financial_code==oPurchaseReturn.FinancialCode&&x.bill_type==billType);
                        dataB.product_transactions.RemoveRange(cpp);
                        
                        int serialNo = 1;
                        for (int i = 0; i < oPurchaseReturn.Details.Count; i++)
                        {

                            product_transactions pt = new product_transactions();

                            pt.bill_no = oPurchaseReturn.BillNo;
                            pt.bill_type = billType;
                            pt.bill_date_time = oPurchaseReturn.BillDateTime;
                            pt.ref_bill_no = oPurchaseReturn.RefBillNo;
                            pt.ref_bill_date_time = oPurchaseReturn.RefBillDateTime;
                            pt.supplier_code = oPurchaseReturn.SupplierCode;
                            pt.supplier = oPurchaseReturn.Supplier;
                            pt.supplier_address = oPurchaseReturn.SupplierAddress;
                            pt.narration = oPurchaseReturn.Narration;
                            pt.advance = oPurchaseReturn.Advance;
                            pt.extra_charges = oPurchaseReturn.Expense;
                            pt.discounts = oPurchaseReturn.Discount;
                            pt.financial_code = oPurchaseReturn.FinancialCode;

                            pt.serial_no = serialNo++;
                            pt.product_code = oPurchaseReturn.Details.ElementAt(i).ProductCode;
                            pt.product = oPurchaseReturn.Details.ElementAt(i).Product;
                            pt.purchase_unit = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnit;
                            pt.purchase_unit_code = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitCode;
                            pt.purchase_unit_value = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitValue;
                            pt.quantity = oPurchaseReturn.Details.ElementAt(i).Quantity * -1;
                            pt.product_discount = oPurchaseReturn.Details.ElementAt(i).ProductDiscount;
                            pt.purchase_rate = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnRate;
                            pt.interstate_rate = oPurchaseReturn.Details.ElementAt(i).InterstateRate;
                            pt.wholesale_rate = oPurchaseReturn.Details.ElementAt(i).WholesaleRate;
                            pt.mrp = oPurchaseReturn.Details.ElementAt(i).MRP;
                            pt.expiry_date = oPurchaseReturn.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oPurchaseReturn.Details.ElementAt(i).Batch;
                            pt.barcode = oPurchaseReturn.Details.ElementAt(i).Barcode;
                            pt.unit_code = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitCode;
                            pt.unit_value = oPurchaseReturn.Details.ElementAt(i).PurchaseReturnUnitValue;

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

        private decimal ReadAvailableQuantityOfBarcode(string barcode,string unitCode,decimal unitValue,string billNo, string billType, string fCode)
        {
            decimal quanity = 0;
            try {

                UnitService us = new UnitService();
                decimal lowestUnitVal = us.ReadLowestUnitValue(unitCode);

                using (var dataB = new Database9007Entities())
                {
                    var cps = dataB.product_transactions.Where(c=>c.barcode==barcode&&!(c.bill_no==billNo&&c.bill_type==billType&&c.financial_code==fCode)).GroupBy(x=>new {x.unit_code},x=>new { x.unit_code, x.unit_value, x.quantity })
                        .Select(y=>new { UnitCode=y.Key.unit_code,Quantity=y.Sum(x=>x.quantity),UnitValue=y.FirstOrDefault().unit_value});
                    foreach (var item in cps)
                    {                        
                        quanity += (lowestUnitVal / item.UnitValue) * (decimal)item.Quantity;
                    }
                    
                }

                quanity /= (lowestUnitVal/unitValue);

            }
            catch
            {                
            }

            return quanity;
        }

        private void CreateAccountTransaction(CPurchaseReturn oPurchaseReturn, string billType)
        {
            LedgerService ls = new LedgerService();

            //Finding ledger accounts for the type of transaction
            string purAccount = "";
            string purAccountCode = "";
            string purDisAccount = "";
            string purDisAccountCode = "";
            if (billType == "PIR")
            {
                purAccount = "Interstate Purchase Account";
                purDisAccount = "Interstate Purchase Discounts";
                purAccountCode = UniqueLedgers.LedgerCode["Interstate Purchase Account"];
                purDisAccountCode = UniqueLedgers.LedgerCode["Interstate Purchase Discounts"];
            }
            else if (billType == "PWR")
            {
                purAccount = "Wholesale Purchase Account";
                purDisAccount = "Wholesale Purchase Discounts";
                purAccountCode = UniqueLedgers.LedgerCode["Wholesale Purchase Account"];
                purDisAccountCode = UniqueLedgers.LedgerCode["Wholesale Purchase Discounts"];
            }

            //Finding bill amount
            decimal billAmount = oPurchaseReturn.Details.Sum(e => (e.Quantity * e.PurchaseReturnRate) - e.ProductDiscount) + oPurchaseReturn.Expense;
            CLedgerTransaction cltBillAmount = new CLedgerTransaction()
            {
                BillDate = oPurchaseReturn.BillDateTime,
                RefBillNo = oPurchaseReturn.BillNo,
                RefBillType = billType,
                FinancialCode = oPurchaseReturn.FinancialCode,
                Amount = billAmount,
                DebitLedger = oPurchaseReturn.Supplier,
                DebitLedgerCode = oPurchaseReturn.SupplierCode,
                CreditLedger = purAccount,
                CreditLedgerCode = purAccountCode

            };

            ls.CreateLedgerTransaction(cltBillAmount);

            if (oPurchaseReturn.Discount > 0)
            {
                CLedgerTransaction cltDiscount = new CLedgerTransaction()
                {
                    BillDate = oPurchaseReturn.BillDateTime,
                    RefBillNo = oPurchaseReturn.BillNo,
                    RefBillType = billType,
                    FinancialCode = oPurchaseReturn.FinancialCode,
                    Amount = oPurchaseReturn.Discount,
                    DebitLedger = purDisAccount,
                    DebitLedgerCode = purDisAccountCode,
                    CreditLedger = oPurchaseReturn.Supplier,
                    CreditLedgerCode = oPurchaseReturn.SupplierCode

                };
                ls.CreateLedgerTransaction(cltDiscount);
            }

            if (oPurchaseReturn.Advance > 0)
            {
                CLedgerTransaction cltCash = new CLedgerTransaction()
                {
                    BillDate = oPurchaseReturn.BillDateTime,
                    RefBillNo = oPurchaseReturn.BillNo,
                    RefBillType = billType,
                    FinancialCode = oPurchaseReturn.FinancialCode,
                    Amount = oPurchaseReturn.Advance,
                    DebitLedger = "Cash",
                    DebitLedgerCode = UniqueLedgers.LedgerCode["Cash"],
                    CreditLedger = oPurchaseReturn.Supplier,
                    CreditLedgerCode = oPurchaseReturn.SupplierCode

                };
                ls.CreateLedgerTransaction(cltCash);
            }
        }

        private void UpdateAccountTransaction(CPurchaseReturn oPurchaseReturn, string billType)
        {
            LedgerService ls = new LedgerService();
            //Deleting previous data
            ls.DeleteLedgerTransaction(oPurchaseReturn.BillNo, billType, oPurchaseReturn.FinancialCode);
            //Adding new data
            CreateAccountTransaction(oPurchaseReturn, billType);
        }

        //Reports
        public List<CPurchaseReturnReportDetailed> FindPurchaseReturnDetailed(DateTime startDate, DateTime endDate, string billType, string billNo, string supplierCode, string supplier, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CPurchaseReturnReportDetailed> report = new List<CPurchaseReturnReportDetailed>();

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

                var resData = dataB.Database.SqlQuery<CPurchaseReturnReportDetailed>("Select  ((bd.quantity * bd.purchase_rate)-bd.product_discount)*-1 As Total, (bd.quantity * bd.purchase_rate)*-1 As GrossValue, bd.quantity*-1 As Quantity, bd.expiry_date As ExpiryDate, bd.batch As Batch, bd.mrp As MRP, bd.wholesale_rate As WholesaleRate, bd.interstate_rate As InterstateRate, bd.purchase_rate As PurchaseRate, bd.product_discount As ProductDiscount, bd.purchase_unit As PurchaseUnit, bd.product As Product, bd.serial_no As SerialNo, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.supplier As Supplier, bd.supplier_address As SupplierAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + " Order By bd.bill_date_time,bd.bill_no, bd.serial_no");

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
                report.Add(new CPurchaseReturnReportDetailed() { BillDateTime = null, SerialNo = null, Product = "" });
                report.Add(new CPurchaseReturnReportDetailed() { BillDateTime = null, SerialNo = null, Product = "Total", Quantity = quantity, GrossValue = grossValue, ProductDiscount = proDiscount, Total = total });

            }


            return report;
        }

        public List<CPurchaseReturnReportSummary> FindPurchaseReturnSummary(DateTime startDate, DateTime endDate, string billType, string billNo, string supplierCode, string supplier, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CPurchaseReturnReportSummary> report = new List<CPurchaseReturnReportSummary>();

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

                var resData = dataB.Database.SqlQuery<CPurchaseReturnReportSummary>("Select Sum((((bd.quantity * bd.purchase_rate)-bd.product_discount)*bd.tax/100)+ ((bd.quantity * bd.purchase_rate) - bd.product_discount))*-1 As BillAmount, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.supplier As Supplier, bd.supplier_address As SupplierAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + "Group By bd.extra_charges, bd.discounts, bd.advance, bd.bill_date_time, bd.bill_no, bd.narration, bd.financial_code, bd.supplier, bd.supplier_address Order By bd.bill_date_time,bd.bill_no");

                decimal? billAmount = 0;
                foreach (var item in resData)
                {

                    billAmount = billAmount + item.BillAmount;
                    report.Add(item);
                }
                //Total
                report.Add(new CPurchaseReturnReportSummary() { BillDateTime = null, BillNo = "" });
                report.Add(new CPurchaseReturnReportSummary() { BillDateTime = null, Supplier = "Total", BillAmount = billAmount });

            }


            return report;
        }
    }
}
