using System;

namespace CustomClaimAPI
{
    internal class Employee
    {
        public string EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string MiddleInitial { get; set; }
        public string LastName { get; set; }
        public int JobID { get; set; }
        public string JobTitle { get; set; }
        public int JobLevel { get; set; }
        public string PublisherID { get; set; }
        public string PublisherName { get; set; }
        public DateTime HireDate { get; set; }
        public string UserRole { get; set; }
    }
}
