SELECT e.FirstName, e.LastName, e.Id as EmployeeId, d.DeptName, d.Id
FROM Department d
LEFT JOIN Employee e ON d.Id = e.DepartmentId
WHERE d.Id = 1;

--group by and have count of employees
SELECT d.DeptName, d.Id, COUNT(e.Id) as EmployeeCount
FROM Department d
LEFT JOIN Employee e ON d.Id = e.DepartmentId
GROUP BY d.DeptName, d.Id;

