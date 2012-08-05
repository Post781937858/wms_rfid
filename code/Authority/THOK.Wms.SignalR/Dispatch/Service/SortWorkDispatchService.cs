﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using THOK.Wms.SignalR.Connection;
using THOK.Wms.SignalR.Dispatch.Interfaces;
using Microsoft.Practices.Unity;
using THOK.Wms.Dal.Interfaces;
using THOK.Wms.DbModel;

namespace THOK.Wms.SignalR.Dispatch.Service
{
    public class SortWorkDispatchService : Notifier<DispatchSortWorkConnection>, ISortOrderWorkDispatchService
    {
        [Dependency]
        public ISortOrderDispatchRepository SortOrderDispatchRepository { get; set; }
        [Dependency]
        public IEmployeeRepository EmployeeRepository { get; set; }
        [Dependency]
        public IMoveBillMasterRepository MoveBillMasterRepository { get; set; }
        [Dependency]
        public IMoveBillDetailRepository MoveBillDetailRepository { get; set; }
        [Dependency]
        public IOutBillMasterRepository OutBillMasterRepository { get; set; }
        [Dependency]
        public IOutBillDetailRepository OutBillDetailRepository { get; set; }
        [Dependency]
        public ISortOrderRepository SortOrderRepository { get; set; }
        [Dependency]
        public ISortOrderDetailRepository SortOrderDetailRepository { get; set; }
        [Dependency]
        public ISortingLowerlimitRepository SortingLowerlimitRepository { get; set; }
        [Dependency]
        public ISortingLineRepository SortingLineRepository { get; set; }
        [Dependency]
        public ISortWorkDispatchRepository SortWorkDispatchRepository { get; set; }

        [Dependency]
        public IStorageRepository StorageRepository { get; set; }
        [Dependency]
        public IProductRepository ProductRepository { get; set; }

        [Dependency]
        public IUnitRepository UnitRepository { get; set; }
        #region ISortWorkDispatchService 成员

        public void Dispatch(string workDispatchId)
        {
            IQueryable<SortOrderDispatch> sortOrderDispatchQuery = SortOrderDispatchRepository.GetQueryable();
            IQueryable<SortOrder> sortOrderQuery = SortOrderRepository.GetQueryable();
            IQueryable<SortOrderDetail> sortOrderDetailQuery = SortOrderDetailRepository.GetQueryable();
            IQueryable<OutBillMaster> outBillMasterQuery = OutBillMasterRepository.GetQueryable();
            IQueryable<OutBillDetail> outBillDetailQuery = OutBillDetailRepository.GetQueryable();
            IQueryable<MoveBillMaster> moveBillMasterQuery = MoveBillMasterRepository.GetQueryable();
            IQueryable<MoveBillDetail> moveBillDetailQuery = MoveBillDetailRepository.GetQueryable();
            IQueryable<SortingLowerlimit> SortingLowerlimitQuery = SortingLowerlimitRepository.GetQueryable();
            IQueryable<SortingLine> sortingLineQuery = SortingLineRepository.GetQueryable();
            IQueryable<SortWorkDispatch> sortWorkDispatchQuery = SortWorkDispatchRepository.GetQueryable();

            workDispatchId = workDispatchId.Substring(0, workDispatchId.Length - 1);
            int[] work = workDispatchId.Split(',').Select(s => Convert.ToInt32(s)).ToArray();

            //调度表未作业的数据
            var temp = sortOrderDispatchQuery.Where(s => work.Any(w => w == s.ID) && s.WorkStatus == "1")
                                           .Join(sortOrderQuery,
                                                dp => new { dp.OrderDate, dp.DeliverLineCode },
                                                om => new { om.OrderDate, om.DeliverLineCode },
                                                (dp, om) => new { dp.OrderDate, dp.SortingLineCode, dp.DeliverLineCode, om.OrderID }
                                           ).Join(sortOrderDetailQuery,
                                                dm => new { dm.OrderID },
                                                od => new { od.OrderID },
                                                (dm, od) => new { dm.OrderDate, dm.SortingLineCode, od.ProductCode, od.UnitCode, od.Price, od.RealQuantity }
                                           ).GroupBy(r => new { r.OrderDate, r.SortingLineCode, r.ProductCode, r.UnitCode, r.Price })
                                            .Select(r => new { r.Key.OrderDate, r.Key.SortingLineCode, r.Key.ProductCode, r.Key.UnitCode, r.Key.Price, SumQuantity = r.Sum(p => p.RealQuantity) })
                                            .GroupBy(r => new { r.OrderDate, r.SortingLineCode })
                                            .Select(r => new { r.Key.OrderDate, r.Key.SortingLineCode, Products = r });


            foreach (var item in temp.ToArray())
            {
                string outBill = GenOutBillNo("long").ToString();
                string moveBill = GenMoveBillNo("long").ToString();
                if (item.Products != null)
                {
                    //添加出库、移库主单和作业调度表
                    AddBillMaster(outBill, moveBill, item.SortingLineCode, item.OrderDate);
                    //添加出库单细单
                    foreach (var product in item.Products.ToArray())
                    {
                        AddOutBillDetail(outBill, product.ProductCode, product.SumQuantity, product.Price,product.UnitCode);
                    }
                    OutBillDetailRepository.SaveChanges();
                }

                //var outBillDetails = outBillDetailQuery.Where(o => o.BillNo == outBill);

                //foreach (var outBillDetail in outBillDetails)
                //{
                //    //获取下限数据
                //    var sortingLowerlimit = SortingLowerlimitQuery.FirstOrDefault(s => s.ProductCode == outBillDetail.ProductCode && s.SortingLineCode == sortline.SortingLineCode);
                //    //获取昨日分拣数据
                //    var sortingLine = SortingLineQuery.FirstOrDefault(s => s.SortingLineCode == sortline.SortingLineCode);
                //    var storage = StorageRepository.GetQueryable().FirstOrDefault(s => s.ProductCode == outBillDetail.ProductCode && s.CellCode == sortingLine.CellCode && s.IsLock == "0" && s.LockTag == "");

                //    string product = outBillDetail.ProductCode;
                //    decimal quantity = (outBillDetail.BillQuantity * outBillDetail.Unit.Count) + (sortingLowerlimit.Quantity * sortingLowerlimit.Unit.Count) - storage.Quantity;

                //    MoveBillDetail moveBillDetail = Allot(product, quantity, outBill);
                //    MoveBillDetailRepository.Add(moveBillDetail);
                //    MoveBillDetailRepository.SaveChanges();
                //}

                //修改线路调度作业状态和作业ID
                var sortDispTemp = sortOrderDispatchQuery.Where(s => work.Any(w => w == s.ID) && s.OrderDate == item.OrderDate && s.SortingLineCode == item.SortingLineCode);
                var sortWorkDisp = sortWorkDispatchQuery.FirstOrDefault(s => s.MoveBillNo == moveBill && s.OutBillNo == outBill);
                foreach (var sortDisp in sortDispTemp.ToArray())
                {
                    sortDisp.SortWorkDispatchID = sortWorkDisp.ID;
                    sortDisp.WorkStatus = "3";
                    SortOrderDispatchRepository.SaveChanges();
                }
            }
        }

        public object GenOutBillNo(string userName)
        {
            string billno = "";
            IQueryable<OutBillMaster> outBillMasterQuery = OutBillMasterRepository.GetQueryable();
            string sysTime = System.DateTime.Now.ToString("yyMMdd");
            var outBillMaster = outBillMasterQuery.Where(i => i.BillNo.Contains(sysTime)).AsEnumerable().OrderBy(i => i.BillNo).Select(i => new { i.BillNo }.BillNo);
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(i => i.UserName == userName);
            if (outBillMaster.Count() == 0)
            {
                billno = System.DateTime.Now.ToString("yyMMdd") + "0001" + "CK";
            }
            else
            {
                string billNoStr = outBillMaster.Last(b => b.Contains(sysTime));
                int i = Convert.ToInt32(billNoStr.ToString().Substring(6, 4));
                i++;
                string newcode = i.ToString();
                for (int j = 0; j < 4 - i.ToString().Length; j++)
                {
                    newcode = "0" + newcode;
                }
                billno = System.DateTime.Now.ToString("yyMMdd") + newcode + "CK";
            }

            return billno;
        }

        public object GenMoveBillNo(string userName)
        {
            IQueryable<MoveBillMaster> moveBillMasterQuery = MoveBillMasterRepository.GetQueryable();
            string sysTime = System.DateTime.Now.ToString("yyMMdd");
            string billNo = "";
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(i => i.UserName == userName);
            var inBillMaster = moveBillMasterQuery.Where(i => i.BillNo.Contains(sysTime)).AsEnumerable().OrderBy(i => i.BillNo).Select(i => new { i.BillNo }.BillNo);
            if (inBillMaster.Count() == 0)
            {
                billNo = System.DateTime.Now.ToString("yyMMdd") + "0001" + "MO";
            }
            else
            {
                string billNoStr = inBillMaster.Last(b => b.Contains(sysTime));
                int i = Convert.ToInt32(billNoStr.ToString().Substring(6, 4));
                i++;
                string newcode = i.ToString();
                for (int j = 0; j < 4 - i.ToString().Length; j++)
                {
                    newcode = "0" + newcode;
                }
                billNo = System.DateTime.Now.ToString("yyMMdd") + newcode + "MO";
            }

            return billNo;
        }

        //添加出库单主单，移库单主单，作业调度
        public void AddBillMaster(string outBill, string moveBill, string sortLine, string orderDate)
        {
            Guid emplooyye = new Guid("2c0a649d-5f44-4a33-8e83-2b6f1b5a06d8");
            var sortingLine = SortingLineRepository.GetQueryable().FirstOrDefault(s => s.SortingLineCode == sortLine);
            //添加出库单主单
            var outbm = new OutBillMaster();
            outbm.BillNo = outBill;
            outbm.BillDate = DateTime.Now;
            outbm.BillTypeCode = "2001";
            outbm.WarehouseCode = sortingLine.Cell.WarehouseCode;
            outbm.OperatePersonID = emplooyye;
            outbm.Status = "1";
            outbm.Description = "分拣作业调度生成出库单";
            outbm.IsActive = "1";
            outbm.UpdateTime = DateTime.Now;

            OutBillMasterRepository.Add(outbm);
            OutBillMasterRepository.SaveChanges();

            //添加移库单主单
            var movebm = new MoveBillMaster();
            movebm.BillNo = moveBill;
            movebm.BillDate = DateTime.Now;
            movebm.BillTypeCode = "3001";
            movebm.WarehouseCode = sortingLine.Cell.WarehouseCode;
            movebm.OperatePersonID = emplooyye;
            movebm.Status = "1";
            movebm.Description = "分拣作业调度生成移库单";
            movebm.IsActive = "1";
            movebm.UpdateTime = DateTime.Now;

            MoveBillMasterRepository.Add(movebm);
            MoveBillMasterRepository.SaveChanges();

            //添加分拣作业调度表
            var sortbm = new SortWorkDispatch();
            var workDispatch = SortWorkDispatchRepository.GetQueryable().FirstOrDefault(w => w.OrderDate == orderDate &&
                                                                       w.SortingLineCode == sortLine);
            sortbm.ID = Guid.NewGuid();
            sortbm.OrderDate = orderDate;
            sortbm.SortingLineCode = sortLine;
            sortbm.DispatchBatch = workDispatch == null ? "1" : (Convert.ToInt32(workDispatch.DispatchBatch) + 1) + "";
            sortbm.OutBillNo = outBill;
            sortbm.MoveBillNo = moveBill;
            sortbm.DispatchStatus = "2";
            sortbm.IsActive = "1";
            sortbm.UpdateTime = DateTime.Now;

            SortWorkDispatchRepository.Add(sortbm);
            SortWorkDispatchRepository.SaveChanges();
        }

        //添加出库单细单
        public void AddOutBillDetail(string outBill, string productCode, decimal quantity, decimal price,string unitCode)
        {
            var outbd = new OutBillDetail();
            var product = ProductRepository.GetQueryable().FirstOrDefault(u => u.ProductCode == productCode);
            var unit = UnitRepository.GetQueryable().FirstOrDefault(u => u.UnitCode == unitCode);
            outbd.BillNo = outBill;
            outbd.ProductCode = productCode;
            outbd.UnitCode = product.UnitCode;
            outbd.Price = price;
            outbd.BillQuantity = quantity * unit.Count;
            outbd.AllotQuantity = 0;
            outbd.RealQuantity = 0;
            outbd.Description = "分拣产生出库细单";

            OutBillDetailRepository.Add(outbd);
        }

        //移库分配
        public MoveBillDetail Allot(string product, decimal quantity, string moveBill)
        {
            var moveDetail =new MoveBillDetail();

            return moveDetail;
        }
        #endregion
    }
}
