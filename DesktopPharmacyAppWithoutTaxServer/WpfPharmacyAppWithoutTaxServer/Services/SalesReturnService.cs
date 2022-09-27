﻿using ServerServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfServerApp.General;
using WpfServerApp.Services.Accounts;
namespace WpfServerApp.Services
{   
    public class SalesReturnService : ISalesReturn
    {
    
        public bool CreateBill(CSalesReturn oSalesReturn, string billType)
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

                        //Create accounts entry
                        CreateAccountTransaction(oSalesReturn, billType);

                        int cbillNo = bs.ReadNextSalesReturnBillNo(oSalesReturn.FinancialCode, billType);
                        bs.UpdateSalesReturnBillNo(oSalesReturn.FinancialCode,cbillNo+1, billType);

                        List<string> barcodes = new BarcodeService().ReadBarcodes(oSalesReturn.Details.Count);
                        
                        for (int i = 0; i < oSalesReturn.Details.Count; i++)
                        {
                            product_transactions pt = new product_transactions();

                            pt.bill_no= cbillNo.ToString();
                            pt.bill_type = billType;                            
                            pt.bill_date_time = oSalesReturn.BillDateTime;
                            pt.ref_bill_no = oSalesReturn.RefBillNo;
                            pt.ref_bill_date_time = oSalesReturn.RefBillDateTime;
                            pt.customer_code = oSalesReturn.CustomerCode;
                            pt.customer = oSalesReturn.Customer;
                            pt.customer_address = oSalesReturn.CustomerAddress;
                            pt.narration = oSalesReturn.Narration;
                            pt.advance = oSalesReturn.Advance;
                            pt.extra_charges = oSalesReturn.Expense;
                            pt.discounts = oSalesReturn.Discount;
                            pt.financial_code = oSalesReturn.FinancialCode;

                            pt.serial_no = oSalesReturn.Details.ElementAt(i).SerialNo;
                            pt.product_code = oSalesReturn.Details.ElementAt(i).ProductCode;
                            pt.product = oSalesReturn.Details.ElementAt(i).Product;
                            pt.sales_unit = oSalesReturn.Details.ElementAt(i).SalesReturnUnit;
                            pt.sales_unit_code = oSalesReturn.Details.ElementAt(i).SalesReturnUnitCode;
                            pt.sales_unit_value = oSalesReturn.Details.ElementAt(i).SalesReturnUnitValue;
                            pt.quantity = oSalesReturn.Details.ElementAt(i).Quantity;
                            pt.product_discount = oSalesReturn.Details.ElementAt(i).ProductDiscount;
                            pt.sales_rate = oSalesReturn.Details.ElementAt(i).SalesReturnRate;
                            pt.mrp = oSalesReturn.Details.ElementAt(i).Rate;
                            pt.expiry_date = oSalesReturn.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oSalesReturn.Details.ElementAt(i).Batch;                            
                            //get a barcode here
                            pt.barcode = barcodes.ElementAt(i);
                            pt.unit_code = oSalesReturn.Details.ElementAt(i).SalesReturnUnitCode;
                            pt.unit_value = oSalesReturn.Details.ElementAt(i).SalesReturnUnitValue;

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
                        //Delete accounts entries
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

        public CSalesReturn ReadBill(string billNo, string billType, string financialCode)
        {
            CSalesReturn ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode&&x.bill_type==billType).OrderBy(y=>y.serial_no);
                
                if (cps.Count() > 0)
                {
                    ccp = new CSalesReturn();

                    var cp = cps.FirstOrDefault();
                    ccp.Id = cp.id;
                    ccp.BillNo = cp.bill_no;
                    ccp.BillDateTime = cp.bill_date_time;
                    ccp.RefBillNo = cp.ref_bill_no;
                    ccp.RefBillDateTime = (DateTime)cp.ref_bill_date_time;
                    ccp.CustomerCode = cp.customer_code;
                    ccp.Customer = cp.customer;
                    ccp.CustomerAddress = cp.customer_address;
                    ccp.Narration = cp.narration;
                    ccp.Advance = (decimal)cp.advance;
                    ccp.Expense = (decimal)cp.extra_charges;
                    ccp.Discount = (decimal)cp.discounts;
                    ccp.FinancialCode = cp.financial_code;
                    
                    foreach (var item in cps)
                    {
                        decimal grossValue = (decimal)(item.quantity * item.sales_rate);
                        ccp.Details.Add(new CSalesReturnDetails() { SerialNo = (int)item.serial_no, ProductCode = item.product_code, Product = item.product, SalesReturnUnit = item.sales_unit, SalesReturnUnitCode = item.sales_unit_code, SalesReturnUnitValue = (decimal)item.sales_unit_value, Quantity = (decimal)item.quantity, SalesReturnRate = (decimal)item.sales_rate, Rate = (decimal)item.mrp, Total = (grossValue - (decimal)item.product_discount), Barcode = item.barcode, OldQuantity= (decimal)item.quantity, ProductDiscount=(decimal)item.product_discount, GrossValue=grossValue, ExpiryDate = item.expiry_date,  Batch = item.batch });
                    }
                }
                
            }

            return ccp;
        }

        public CSalesReturn ReadSalesBill(string billNo, string billType, string financialCode)
        {
            CSalesReturn ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode && x.bill_type == billType).OrderBy(y => y.serial_no);

                if (cps.Count() > 0)
                {
                    ccp = new CSalesReturn();

                    var cp = cps.FirstOrDefault();
                    ccp.Id = cp.id;
                    ccp.BillNo = cp.bill_no;
                    ccp.BillDateTime = cp.bill_date_time;
                    ccp.CustomerCode = cp.customer_code;
                    ccp.Customer = cp.customer;
                    ccp.CustomerAddress = cp.customer_address;
                    ccp.Narration = cp.narration;
                    ccp.Advance = (decimal)cp.advance;
                    ccp.Expense = (decimal)cp.extra_charges;
                    ccp.Discount = (decimal)cp.discounts;
                    ccp.FinancialCode = cp.financial_code;

                    int serialNo = 1;
                    foreach (var item in cps)
                    {
                        decimal grossValue = (decimal)(item.quantity * item.sales_rate * -1);
                        ccp.Details.Add(new CSalesReturnDetails() { SerialNo = serialNo++, ProductCode = item.product_code, Product = item.product, SalesReturnUnit = item.sales_unit, SalesReturnUnitCode = item.sales_unit_code, SalesReturnUnitValue = (decimal)item.sales_unit_value, Quantity = (decimal)item.quantity*-1, SalesReturnRate = (decimal)item.sales_rate, Rate = (decimal)item.mrp, Total = (grossValue - (decimal)item.product_discount), Barcode = item.barcode, OldQuantity = (decimal)item.quantity*-1, ProductDiscount=(decimal)item.product_discount, GrossValue=grossValue, ExpiryDate = item.expiry_date, Batch = item.batch });
                    }
                }

            }

            return ccp;
        }

        public int ReadNextBillNo(string billType, string financialCode)
        {
            
            BillNoService bns = new BillNoService();
            return bns.ReadNextSalesReturnBillNo(financialCode, billType);
            
        }

        public bool UpdateBill(CSalesReturn oSalesReturn, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {
                        //Update Accounts entries
                        UpdateAccountTransaction(oSalesReturn, billType);

                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == oSalesReturn.BillNo&& x.financial_code==oSalesReturn.FinancialCode&&x.bill_type==billType);
                        dataB.product_transactions.RemoveRange(cpp);
                        
                        int serialNo = 1;
                        for (int i = 0; i < oSalesReturn.Details.Count; i++)
                        {

                            product_transactions pt = new product_transactions();

                            pt.bill_no = oSalesReturn.BillNo;
                            pt.bill_type = billType;
                            pt.bill_date_time = oSalesReturn.BillDateTime;
                            pt.ref_bill_no = oSalesReturn.RefBillNo;
                            pt.ref_bill_date_time = oSalesReturn.RefBillDateTime;
                            pt.customer_code = oSalesReturn.CustomerCode;
                            pt.customer = oSalesReturn.Customer;
                            pt.customer_address = oSalesReturn.CustomerAddress;
                            pt.narration = oSalesReturn.Narration;
                            pt.advance = oSalesReturn.Advance;
                            pt.extra_charges = oSalesReturn.Expense;
                            pt.discounts = oSalesReturn.Discount;
                            pt.financial_code = oSalesReturn.FinancialCode;

                            pt.serial_no = serialNo++;
                            pt.product_code = oSalesReturn.Details.ElementAt(i).ProductCode;
                            pt.product = oSalesReturn.Details.ElementAt(i).Product;
                            pt.sales_unit = oSalesReturn.Details.ElementAt(i).SalesReturnUnit;
                            pt.sales_unit_code = oSalesReturn.Details.ElementAt(i).SalesReturnUnitCode;
                            pt.sales_unit_value = oSalesReturn.Details.ElementAt(i).SalesReturnUnitValue;
                            pt.quantity = oSalesReturn.Details.ElementAt(i).Quantity;
                            pt.product_discount = oSalesReturn.Details.ElementAt(i).ProductDiscount;
                            pt.sales_rate = oSalesReturn.Details.ElementAt(i).SalesReturnRate;
                            pt.mrp = oSalesReturn.Details.ElementAt(i).Rate;
                            pt.expiry_date = oSalesReturn.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oSalesReturn.Details.ElementAt(i).Batch;                            
                            pt.barcode = oSalesReturn.Details.ElementAt(i).Barcode;
                            pt.unit_code = oSalesReturn.Details.ElementAt(i).SalesReturnUnitCode;
                            pt.unit_value = oSalesReturn.Details.ElementAt(i).SalesReturnUnitValue;

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

        private void CreateAccountTransaction(CSalesReturn oSalesReturn, string billType)
        {
            LedgerService ls = new LedgerService();

            //Finding ledger accounts for the type of transaction
            string salAccount = "";
            string salAccountCode = "";
            string salDisAccount = "";
            string salDisAccountCode = "";
            if (billType == "SIR")
            {
                salAccount = "Interstate Sales Account";
                salDisAccount = "Interstate Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Interstate Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Interstate Sales Discounts"];
            }
            else if (billType == "SWR")
            {
                salAccount = "Wholesale Sales Account";
                salDisAccount = "Wholesale Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Wholesale Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Wholesale Sales Discounts"];
            }
            else if (billType == "SLR")
            {
                salAccount = "Retail Sales Account";
                salDisAccount = "Retail Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Retail Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Retail Sales Discounts"];
            }

            //Finding bill amount
            decimal billAmount = oSalesReturn.Details.Sum(e => (e.Quantity * e.SalesReturnRate) - e.ProductDiscount) + oSalesReturn.Expense;
            CLedgerTransaction cltBillAmount = new CLedgerTransaction()
            {
                BillDate = oSalesReturn.BillDateTime,
                RefBillNo = oSalesReturn.BillNo,
                RefBillType = billType,
                FinancialCode = oSalesReturn.FinancialCode,
                Amount = billAmount,
                DebitLedger = salAccount,
                DebitLedgerCode = salAccountCode,
                CreditLedger = oSalesReturn.Customer,
                CreditLedgerCode = oSalesReturn.CustomerCode

            };

            ls.CreateLedgerTransaction(cltBillAmount);

            if (oSalesReturn.Discount > 0)
            {
                CLedgerTransaction cltDiscount = new CLedgerTransaction()
                {
                    BillDate = oSalesReturn.BillDateTime,
                    RefBillNo = oSalesReturn.BillNo,
                    RefBillType = billType,
                    FinancialCode = oSalesReturn.FinancialCode,
                    Amount = oSalesReturn.Discount,
                    DebitLedger = oSalesReturn.Customer,
                    DebitLedgerCode = oSalesReturn.CustomerCode,
                    CreditLedger = salDisAccount,
                    CreditLedgerCode = salDisAccountCode

                };
                ls.CreateLedgerTransaction(cltDiscount);
            }

            if (oSalesReturn.Advance > 0)
            {
                CLedgerTransaction cltCash = new CLedgerTransaction()
                {
                    BillDate = oSalesReturn.BillDateTime,
                    RefBillNo = oSalesReturn.BillNo,
                    RefBillType = billType,
                    FinancialCode = oSalesReturn.FinancialCode,
                    Amount = oSalesReturn.Advance,
                    DebitLedger = oSalesReturn.Customer,
                    DebitLedgerCode = oSalesReturn.CustomerCode,
                    CreditLedger = "Cash",
                    CreditLedgerCode = UniqueLedgers.LedgerCode["Cash"]

                };
                ls.CreateLedgerTransaction(cltCash);
            }
        }

        private void UpdateAccountTransaction(CSalesReturn oSalesReturn, string billType)
        {
            LedgerService ls = new LedgerService();
            //Deleting previous data
            ls.DeleteLedgerTransaction(oSalesReturn.BillNo, billType, oSalesReturn.FinancialCode);
            //Adding new data
            CreateAccountTransaction(oSalesReturn, billType);

        }


        //Reports
        public List<CSalesReturnReportDetailed> FindSalesReturnDetailed(DateTime startDate, DateTime endDate, string billType, string billNo, string customerCode, string customer, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CSalesReturnReportDetailed> report = new List<CSalesReturnReportDetailed>();

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
                string customerCodeQuery = customerCode.Trim().Equals("") ? "" : " && (bd.customer_code='" + customerCode.Trim() + "') ";
                string customerQuery = customer.Trim().Equals("") ? "" : " && (bd.customer Like '%" + customer.Trim() + "%') ";
                string narrationQuery = narration.Trim().Equals("") ? "" : " && (bd.narration Like '%" + narration.Trim() + "%') ";
                string financialCodeQuery = financialCode.Trim().Equals("") ? "" : " && (bd.financial_code='" + financialCode.Trim() + "') ";
                

                string subQ = billNoQuery + productCodeQuery + productQuery + narrationQuery + financialCodeQuery + billTypeQuery + expiryDateQuery + batchQuery + customerCodeQuery + customerQuery;

                var resData = dataB.Database.SqlQuery<CSalesReturnReportDetailed>("Select  ((bd.quantity * bd.sales_rate)-bd.product_discount) As Total, (bd.quantity * bd.sales_rate) As GrossValue, bd.quantity As Quantity, bd.expiry_date As ExpiryDate, bd.batch As Batch, bd.mrp As MRP, bd.wholesale_rate As WholesaleRate, bd.interstate_rate As InterstateRate, bd.sales_rate As SalesRate, bd.product_discount As ProductDiscount, bd.sales_unit As SalesUnit, bd.product As Product, bd.serial_no As SerialNo, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.customer As Customer, bd.customer_address As CustomerAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + " Order By bd.bill_date_time,bd.bill_no, bd.serial_no");

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
                report.Add(new CSalesReturnReportDetailed() { BillDateTime = null, SerialNo = null, Product = "" });
                report.Add(new CSalesReturnReportDetailed() { BillDateTime = null, SerialNo = null, Product = "Total", Quantity = quantity, GrossValue = grossValue, ProductDiscount = proDiscount, Total = total });

            }


            return report;
        }

        public List<CSalesReturnReportSummary> FindSalesReturnSummary(DateTime startDate, DateTime endDate, string billType, string billNo, string customerCode, string customer, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CSalesReturnReportSummary> report = new List<CSalesReturnReportSummary>();

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
                string customerCodeQuery = customerCode.Trim().Equals("") ? "" : " && (bd.customer_code='" + customerCode.Trim() + "') ";
                string customerQuery = customer.Trim().Equals("") ? "" : " && (bd.customer Like '%" + customer.Trim() + "%') ";
                string narrationQuery = narration.Trim().Equals("") ? "" : " && (bd.narration Like '%" + narration.Trim() + "%') ";
                string financialCodeQuery = financialCode.Trim().Equals("") ? "" : " && (bd.financial_code='" + financialCode.Trim() + "') ";
                

                string subQ = billNoQuery + productCodeQuery + productQuery + narrationQuery + financialCodeQuery + billTypeQuery + expiryDateQuery + batchQuery + customerCodeQuery + customerQuery;

                var resData = dataB.Database.SqlQuery<CSalesReturnReportSummary>("Select Sum(((bd.quantity * bd.sales_rate) - bd.product_discount)) As BillAmount, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.customer As Customer, bd.customer_address As CustomerAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + "Group By bd.extra_charges, bd.discounts, bd.advance, bd.bill_date_time, bd.bill_no, bd.narration, bd.financial_code, bd.customer, bd.customer_address Order By bd.bill_date_time,bd.bill_no");

                decimal? billAmount = 0;
                foreach (var item in resData)
                {

                    billAmount = billAmount + item.BillAmount;
                    report.Add(item);
                }
                //Total
                report.Add(new CSalesReturnReportSummary() { BillDateTime = null, BillNo = "" });
                report.Add(new CSalesReturnReportSummary() { BillDateTime = null, Customer = "Total", BillAmount = billAmount });

            }


            return report;
        }
    }
}
