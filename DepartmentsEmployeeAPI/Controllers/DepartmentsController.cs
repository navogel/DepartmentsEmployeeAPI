﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DepartmentsEmployeeAPI.Models;
using Microsoft.AspNetCore.Http;
namespace DepartmentEmployeesExample.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IConfiguration _config;
        public DepartmentController(IConfiguration config)
        {
            _config = config;
        }
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }


        /// <summary>
        /// Gets a list of all departments from database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT d.Id, d.DeptName, e.FirstName, e.LastName, e.DepartmentId, e.Id as EmployeeId
                                        FROM Department d
                                        LEFT JOIN Employee e ON d.Id = e.DepartmentId";
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Department> departments = new List<Department>();
                    while (reader.Read())
                    {
                        var departmentId = reader.GetInt32(reader.GetOrdinal("Id"));
                        var departmentAlreadyAdded = departments.FirstOrDefault(d => d.Id == departmentId);

                        if (departmentAlreadyAdded == null)
                        {
                            Department department = new Department
                            {
                                Id = departmentId,
                                DeptName = reader.GetString(reader.GetOrdinal("DeptName")),
                                Employees = new List<Employee>()
                            };
                            departments.Add(department);

                            var hasEmployee = !reader.IsDBNull(reader.GetOrdinal("EmployeeId"));

                            if (hasEmployee)
                            {
                                department.Employees.Add(new Employee()
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Id = reader.GetInt32(reader.GetOrdinal("EmployeeId"))
                                });
                            }
                        }
                        else
                        {
                            var hasEmployee = !reader.IsDBNull(reader.GetOrdinal("EmployeeId"));

                            if (hasEmployee)
                            {
                                departmentAlreadyAdded.Employees.Add(new Employee()
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Id = reader.GetInt32(reader.GetOrdinal("EmployeeId"))
                                });
                            }
                        }
                    }
                    reader.Close();
                    return Ok(departments);
                }
            }
        }
        [HttpGet("{id}", Name = "GetDepartment")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT e.FirstName, e.LastName, e.Id as EmployeeId, d.DeptName, d.Id
                                        FROM Department d
                                        LEFT JOIN Employee e ON d.Id = e.DepartmentId
                                        WHERE d.Id = @id;";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    Department department = null;
                    while (reader.Read())
                    {
                        if (department == null)
                        {
                            department = new Department
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                DeptName = reader.GetString(reader.GetOrdinal("DeptName")),
                            };
                        }

                        department.Employees.Add(new Employee()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName"))
                        });
                    }
                    reader.Close();
                    return Ok(department);
                }
            }
        }

        //custom route

        [HttpGet]
        [Route("employeeCount")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT d.DeptName, d.Id, COUNT(e.Id) as EmployeeCount
                                        FROM Department d
                                        LEFT JOIN Employee e ON d.Id = e.DepartmentId
                                        GROUP BY d.DeptName, d.Id";
                    
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    var deptEmpCount = new List<DepartmentEmployeeCount>();
                    while (reader.Read())
                    {
                        deptEmpCount.Add(new DepartmentEmployeeCount
                        {
                            DepartmentId = reader.GetInt32(reader.GetOrdinal("Id")),
                            DepartmentName = reader.GetString(reader.GetOrdinal("DeptName")),
                            EmployeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                        });

                        
                    }
                    reader.Close();
                    return Ok(deptEmpCount);
                }
            }
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Department department)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Department (DeptName)
                                        OUTPUT INSERTED.Id
                                        VALUES (@DeptName)";
                    cmd.Parameters.Add(new SqlParameter("@DeptName", department.DeptName));
                    int newId = (int) await cmd.ExecuteScalarAsync();
                    department.Id = newId;
                    return CreatedAtRoute("GetDepartment", new { id = newId }, department);
                }
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Department WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                bool exists = await DepartmentExists(id);
                if (!exists)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        private async Task<bool> DepartmentExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id FROM Department WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    return reader.Read();
                }
            }
        }
    }
}