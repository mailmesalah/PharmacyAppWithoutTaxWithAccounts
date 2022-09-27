﻿using ServerServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfServerApp.General;
using WpfServerApp.Services.Accounts;

namespace WpfServerApp.Services
{   
    public class SalesService : ISales
    {
    
        public bool CreateBill(CSales oSales, string billType)
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

                        //Creating accounts entries
                        CreateAccountTransaction(oSales, billType);

                        int cbillNo = bs.ReadNextSalesBillNo(oSales.FinancialCode,billType);
                        bs.UpdateSalesBillNo(oSales.FinancialCode,cbillNo+1,billType);
                        
                        for (int i = 0; i < oSales.Details.Count; i++)
                        {
                            product_transactions pt = new product_transactions();

                            pt.bill_no= cbillNo.ToString();
                            pt.bill_type = billType;
                            pt.bill_date_time = oSales.BillDateTime;
                            pt.customer_code = oSales.CustomerCode;
                            pt.customer = oSales.Customer;
                            pt.customer_address = oSales.CustomerAddress;
                            pt.narration = oSales.Narration;
                            pt.advance = oSales.Advance;
                            pt.extra_charges = oSales.Expense;
                            pt.discounts = oSales.Discount;
                            pt.financial_code = oSales.FinancialCode;

                            pt.serial_no = oSales.Details.ElementAt(i).SerialNo;
                            pt.product_code = oSales.Details.ElementAt(i).ProductCode;
                            pt.product = oSales.Details.ElementAt(i).Product;
                            pt.sales_unit = oSales.Details.ElementAt(i).SalesUnit;
                            pt.sales_unit_code = oSales.Details.ElementAt(i).SalesUnitCode;
                            pt.sales_unit_value = oSales.Details.ElementAt(i).SalesUnitValue;
                            pt.quantity = oSales.Details.ElementAt(i).Quantity*-1;
                            pt.product_discount = oSales.Details.ElementAt(i).ProductDiscount;
                            pt.sales_rate = oSales.Details.ElementAt(i).SalesRate;
                            pt.mrp = oSales.Details.ElementAt(i).Rate;
                            pt.expiry_date = oSales.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oSales.Details.ElementAt(i).Batch;                            
                            //get a barcode here
                            pt.barcode = oSales.Details.ElementAt(i).Barcode;
                            pt.unit_code= oSales.Details.ElementAt(i).SalesUnitCode;
                            pt.unit_value= oSales.Details.ElementAt(i).SalesUnitValue;

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
                        //Deletion of account entries
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

        public CSales ReadBill(string billNo, string billType, string financialCode)
        {
            CSales ccp = null;

            using (var dataB = new Database9007Entities())
            {
                var cps = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == billNo && x.financial_code == financialCode&&x.bill_type==billType).OrderBy(y=>y.serial_no);
                
                if (cps.Count() > 0)
                {
                    ccp = new CSales();

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

                    foreach (var item in cps)
                    {
                        decimal grossValue = (decimal)(item.quantity * item.sales_rate*-1);
                        ccp.Details.Add(new CSalesDetails() { SerialNo=(int)item.serial_no,ProductCode=item.product_code,Product=item.product, SalesUnit=item.sales_unit, SalesUnitCode=item.sales_unit_code, SalesUnitValue = (decimal)item.sales_unit_value, Quantity=(decimal)item.quantity*-1, SalesRate = (decimal)item.sales_rate, Rate = (decimal)item.mrp, Total= (grossValue - (decimal)item.product_discount), Barcode = item.barcode, ProductDiscount=(decimal)item.product_discount, GrossValue=grossValue, ExpiryDate = item.expiry_date, Batch = item.batch });
                    }
                }
                
            }

            return ccp;
        }

        public int ReadNextBillNo(string billType, string financialCode)
        {
            
            BillNoService bns = new BillNoService();
            return bns.ReadNextSalesBillNo(financialCode, billType);
            
        }
        
        public bool UpdateBill(CSales oSales, string billType)
        {
            bool returnValue = false;

            lock (Synchronizer.@lock)
            {
                using (var dataB = new Database9007Entities())
                {
                    var dataBTransaction = dataB.Database.BeginTransaction();
                    try
                    {
                        //Updating accounts entries
                        UpdateAccountTransaction(oSales, billType);

                        var cpp = dataB.product_transactions.Select(c => c).Where(x => x.bill_no == oSales.BillNo&& x.financial_code==oSales.FinancialCode&&x.bill_type==billType);
                        dataB.product_transactions.RemoveRange(cpp);

                        for (int i = 0; i < oSales.Details.Count; i++)
                        {

                            product_transactions pt = new product_transactions();

                            pt.bill_no = oSales.BillNo;
                            pt.bill_type = billType;
                            pt.bill_date_time = oSales.BillDateTime;
                            pt.customer_code = oSales.CustomerCode;
                            pt.customer = oSales.Customer;
                            pt.customer_address = oSales.CustomerAddress;
                            pt.narration = oSales.Narration;
                            pt.advance = oSales.Advance;
                            pt.extra_charges = oSales.Expense;
                            pt.discounts = oSales.Discount;
                            pt.financial_code = oSales.FinancialCode;

                            pt.serial_no = oSales.Details.ElementAt(i).SerialNo;
                            pt.product_code = oSales.Details.ElementAt(i).ProductCode;
                            pt.product = oSales.Details.ElementAt(i).Product;
                            pt.sales_unit = oSales.Details.ElementAt(i).SalesUnit;
                            pt.sales_unit_code = oSales.Details.ElementAt(i).SalesUnitCode;
                            pt.sales_unit_value = oSales.Details.ElementAt(i).SalesUnitValue;
                            pt.quantity = oSales.Details.ElementAt(i).Quantity*-1;
                            pt.product_discount = oSales.Details.ElementAt(i).ProductDiscount;
                            pt.sales_rate = oSales.Details.ElementAt(i).SalesRate;
                            pt.mrp = oSales.Details.ElementAt(i).Rate;
                            pt.expiry_date = oSales.Details.ElementAt(i).ExpiryDate;
                            pt.batch = oSales.Details.ElementAt(i).Batch;                            
                            //get a barcode here
                            pt.barcode = oSales.Details.ElementAt(i).Barcode;
                            pt.unit_code = oSales.Details.ElementAt(i).SalesUnitCode;
                            pt.unit_value = oSales.Details.ElementAt(i).SalesUnitValue;

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

        public List<CStock> ReadStockOfProduct(string productCode,string unitCode,string unit, decimal unitValue,string billNo, string billType, string fCode)
        {
            List<CStock> stocks = new List<CStock>();
            
            try
            {

                UnitService us = new UnitService();
                decimal lowestUnitVal = us.ReadLowestUnitValue(unitCode);

                using (var dataB = new Database9007Entities())
                {
                    using (var dataBSub = new Database9007Entities())
                    {

                        var cps = dataB.product_transactions.Where(c => c.product_code == productCode && !(c.bill_no == billNo && c.bill_type == billType && c.financial_code == fCode)).GroupBy(x => new { x.barcode }, x => new { x.unit_value, x.quantity, x.barcode, x.mrp })
                        .Select(y => new { Quantity = y.Sum(x => x.quantity * (unitValue / x.unit_value)), Barcode = y.FirstOrDefault().barcode });
                        foreach (var item in cps)
                        {
                            var rateNUnitValue = dataBSub.product_transactions.Where(c => c.barcode == item.Barcode && (c.bill_type == "SA" || c.bill_type == "PI" || c.bill_type == "PW")).Select(c => new { c.interstate_rate, c.mrp, c.wholesale_rate, c.unit_value, c.expiry_date, c.batch  }).FirstOrDefault();
                            CStock cs = new CStock() { Barcode = item.Barcode, Quantity = (decimal)item.Quantity, Unit = unit, MRP = (decimal)rateNUnitValue.mrp/unitValue/rateNUnitValue.unit_value, InterstateRate=(decimal)rateNUnitValue.interstate_rate / unitValue / rateNUnitValue.unit_value, WholesaleRate=(decimal)rateNUnitValue.wholesale_rate / unitValue / rateNUnitValue.unit_value, ExpiryDate=rateNUnitValue.expiry_date, Batch= rateNUnitValue.batch};
                            stocks.Add(cs);
                        }
                    }

                }                

            }
            catch (Exception e)
            {

            }
            
            return stocks;
        }

        private void CreateAccountTransaction(CSales oSales, string billType)
        {
            LedgerService ls = new LedgerService();

            //Finding ledger accounts for the type of transaction
            string salAccount = "";
            string salAccountCode = "";
            string salDisAccount = "";
            string salDisAccountCode = "";
            if (billType == "SI")
            {
                salAccount = "Interstate Sales Account";
                salDisAccount = "Interstate Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Interstate Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Interstate Sales Discounts"];
            }
            else if (billType == "SW")
            {
                salAccount = "Wholesale Sales Account";
                salDisAccount = "Wholesale Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Wholesale Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Wholesale Sales Discounts"];
            }
            else if (billType == "SL")
            {
                salAccount = "Retail Sales Account";
                salDisAccount = "Retail Sales Discounts";
                salAccountCode = UniqueLedgers.LedgerCode["Retail Sales Account"];
                salDisAccountCode = UniqueLedgers.LedgerCode["Retail Sales Discounts"];
            }

            //Finding bill amount
            decimal billAmount = oSales.Details.Sum(e => (e.Quantity * e.SalesRate) - e.ProductDiscount) + oSales.Expense;
            CLedgerTransaction cltBillAmount = new CLedgerTransaction()
            {
                BillDate = oSales.BillDateTime,
                RefBillNo = oSales.BillNo,
                RefBillType = billType,
                FinancialCode = oSales.FinancialCode,
                Amount = billAmount,
                DebitLedger = oSales.Customer,
                DebitLedgerCode = oSales.CustomerCode,
                CreditLedger = salAccount,
                CreditLedgerCode = salAccountCode

            };

            ls.CreateLedgerTransaction(cltBillAmount);

            if (oSales.Discount > 0)
            {
                CLedgerTransaction cltDiscount = new CLedgerTransaction()
                {
                    BillDate = oSales.BillDateTime,
                    RefBillNo = oSales.BillNo,
                    RefBillType = billType,
                    FinancialCode = oSales.FinancialCode,
                    Amount = oSales.Discount,
                    DebitLedger = salDisAccount,
                    DebitLedgerCode = salDisAccountCode,
                    CreditLedger = oSales.Customer,
                    CreditLedgerCode = oSales.CustomerCode

                };
                ls.CreateLedgerTransaction(cltDiscount);
            }

            if (oSales.Advance > 0)
            {
                CLedgerTransaction cltCash = new CLedgerTransaction()
                {
                    BillDate = oSales.BillDateTime,
                    RefBillNo = oSales.BillNo,
                    RefBillType = billType,
                    FinancialCode = oSales.FinancialCode,
                    Amount = oSales.Advance,
                    DebitLedger = "Cash",
                    DebitLedgerCode = UniqueLedgers.LedgerCode["Cash"],
                    CreditLedger = oSales.Customer,
                    CreditLedgerCode = oSales.CustomerCode

                };
                ls.CreateLedgerTransaction(cltCash);
            }
        }

        private void UpdateAccountTransaction(CSales oSales, string billType)
        {
            LedgerService ls = new LedgerService();
            //Deleting previous data
            ls.DeleteLedgerTransaction(oSales.BillNo, billType, oSales.FinancialCode);
            //Adding new data
            CreateAccountTransaction(oSales, billType);

        }

        //Reports
        public List<CSalesReportDetailed> FindSalesDetailed(DateTime startDate, DateTime endDate, string billType, string billNo, string customerCode, string customer, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CSalesReportDetailed> report = new List<CSalesReportDetailed>();

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

                var resData = dataB.Database.SqlQuery<CSalesReportDetailed>("Select  ((bd.quantity * bd.sales_rate)-bd.product_discount)*-1 As Total, (bd.quantity * bd.sales_rate)*-1 As GrossValue, bd.quantity*-1 As Quantity, bd.expiry_date As ExpiryDate, bd.batch As Batch, bd.mrp As MRP, bd.wholesale_rate As WholesaleRate, bd.interstate_rate As InterstateRate, bd.sales_rate As SalesRate, bd.product_discount As ProductDiscount, bd.sales_unit As SalesUnit, bd.product As Product, bd.serial_no As SerialNo, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.customer As Customer, bd.customer_address As CustomerAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + " Order By bd.bill_date_time,bd.bill_no, bd.serial_no");

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
                report.Add(new CSalesReportDetailed() { BillDateTime = null, SerialNo = null, Product = "" });
                report.Add(new CSalesReportDetailed() { BillDateTime = null, SerialNo = null, Product = "Total", Quantity = quantity, GrossValue = grossValue, ProductDiscount = proDiscount, Total = total });

            }


            return report;
        }

        public List<CSalesReportSummary> FindSalesSummary(DateTime startDate, DateTime endDate, string billType, string billNo, string customerCode, string customer, string productCode, string product, string batch, DateTime expiryDate, string narration, string financialCode)
        {
            List<CSalesReportSummary> report = new List<CSalesReportSummary>();

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

                var resData = dataB.Database.SqlQuery<CSalesReportSummary>("Select Sum(((bd.quantity * bd.sales_rate) - bd.product_discount))*-1 As BillAmount, bd.extra_charges As Expense, bd.discounts As Discount, bd.advance As Advance, bd.bill_date_time As BillDateTime,bd.bill_no As BillNo, bd.narration As Narration, bd.financial_code As FinancialCode, bd.customer As Customer, bd.customer_address As CustomerAddress From product_transactions bd Where(bd.bill_date_time >= '" + startD + "' && bd.bill_date_time <='" + endD + "') " + subQ + "Group By bd.extra_charges, bd.discounts, bd.advance, bd.bill_date_time, bd.bill_no, bd.narration, bd.financial_code, bd.customer, bd.customer_address Order By bd.bill_date_time,bd.bill_no");

                decimal? billAmount = 0;
                foreach (var item in resData)
                {

                    billAmount = billAmount + item.BillAmount;
                    report.Add(item);
                }
                //Total
                report.Add(new CSalesReportSummary() { BillDateTime = null, BillNo = "" });
                report.Add(new CSalesReportSummary() { BillDateTime = null, Customer = "Total", BillAmount = billAmount });

            }


            return report;
        }
    }
}
