﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using THOK.Wms.DbModel;
using THOK.Wms.Bll.Interfaces;
using Microsoft.Practices.Unity;
using THOK.Wms.Dal.Interfaces;

namespace THOK.Wms.Bll.Service
{
    public class DepartmentService : ServiceBase<Department>, IDepartmentService
    {
        [Dependency]
        public IDepartmentRepository DepartmentRepository { get; set; }

        [Dependency]
        public IEmployeeRepository EmployeeRepository { get; set; }

        [Dependency]
        public ICompanyRepository CompanyRepository { get; set; }

        protected override Type LogPrefix
        {
            get { return this.GetType(); }
        }

        #region IDepartmentService 增，删，改，查等方法

        public object GetDetails(int page, int rows, string DepartmentCode, string DepartmentName, string DepartmentLeaderID, string CompanyID)
        {
            IQueryable<Department> departQuery = DepartmentRepository.GetQueryable();
            var department = departQuery.Where(d => d.DepartmentCode.Contains(DepartmentCode) && d.DepartmentName.Contains(DepartmentName))
                .OrderBy(d => d.DepartmentCode).AsEnumerable().Select(d => new { d.ID, d.DepartmentCode, d.DepartmentName, d.Description, d.DepartmentLeaderID, EmployeeName = d.DepartmentLeaderID == null ? string.Empty : d.DepartmentLeader.EmployeeName, companyID = d.Company.ID, d.Company.CompanyName, ParentDepartmentID = d.ParentDepartmentID, ParentDepartmentName = d.ParentDepartment.DepartmentName, IsActive = d.IsActive == "1" ? "可用" : "不可用", UpdateTime = d.UpdateTime.ToString("yyyy-MM-dd hh:mm:ss") });
            if (!CompanyID.Equals("") || !DepartmentLeaderID.Equals(""))
            {
                var compId = new Guid(CompanyID);
                var empId = new Guid(DepartmentLeaderID);
                department = departQuery.Where(d => d.DepartmentCode.Contains(DepartmentCode) && d.DepartmentName.Contains(DepartmentName) && d.Company.ID == compId && d.DepartmentLeader.ID == empId)
                .OrderBy(d => d.DepartmentCode).AsEnumerable().Select(d => new { d.ID, d.DepartmentCode, d.DepartmentName, d.Description, d.DepartmentLeaderID, EmployeeName = d.DepartmentLeaderID == null ? string.Empty : d.DepartmentLeader.EmployeeName, companyID = d.Company.ID, d.Company.CompanyName, ParentDepartmentID = d.ParentDepartmentID, ParentDepartmentName = d.ParentDepartment.DepartmentName, IsActive = d.IsActive == "1" ? "可用" : "不可用", UpdateTime = d.UpdateTime.ToString("yyyy-MM-dd hh:mm:ss") });
            }
            int total = department.Count();
            department = department.Skip((page - 1) * rows).Take(rows);
            return new { total, rows = department.ToArray() };
        }

        public bool Add(Department department)
        {
            var newDepartment = new Department();
            var depart = DepartmentRepository.GetQueryable().FirstOrDefault(d => d.ID == department.ParentDepartmentID);
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(e => e.ID == department.DepartmentLeaderID);
            var company = CompanyRepository.GetQueryable().FirstOrDefault(c => c.ID == department.CompanyID);
            newDepartment.ID = Guid.NewGuid();
            newDepartment.DepartmentCode = department.DepartmentCode;
            newDepartment.DepartmentName = department.DepartmentName;
            newDepartment.ParentDepartment = depart ?? newDepartment;
            newDepartment.DepartmentLeader = employee;
            newDepartment.Description = department.Description;
            newDepartment.Company = company;
            newDepartment.UniformCode = department.UniformCode;
            newDepartment.IsActive = department.IsActive;
            newDepartment.UpdateTime = DateTime.Now;

            DepartmentRepository.Add(newDepartment);
            DepartmentRepository.SaveChanges();
            return true;
        }

        public bool Delete(string departmentId)
        {
            Guid deparId = new Guid(departmentId);
            var departemnt = DepartmentRepository.GetQueryable()
                .FirstOrDefault(c => c.ID == deparId);
            if (departemnt != null)
            {
                Del(DepartmentRepository, departemnt.Departments);
                DepartmentRepository.Delete(departemnt);
                DepartmentRepository.SaveChanges();
            }
            else
                return false;
            return true;
        }

        public bool Save(Department department)
        {
            var depart = DepartmentRepository.GetQueryable().FirstOrDefault(d => d.ID == department.ID);
            var parent = DepartmentRepository.GetQueryable().FirstOrDefault(p => p.ID == department.ParentDepartmentID);
            var employee = EmployeeRepository.GetQueryable().FirstOrDefault(e => e.ID == department.DepartmentLeaderID);
            var company = CompanyRepository.GetQueryable().FirstOrDefault(c => c.ID == department.CompanyID);
            depart.DepartmentCode = department.DepartmentCode;
            depart.DepartmentName = department.DepartmentName;
            depart.ParentDepartment = depart ?? depart;
            depart.DepartmentLeader = employee;
            depart.Description = department.Description;
            depart.Company = company;
            depart.UniformCode = department.UniformCode;
            depart.IsActive = department.IsActive;
            depart.UpdateTime = DateTime.Now;
            DepartmentRepository.SaveChanges();
            return true;
        }

        #endregion
    }
}
