using System;

namespace DurableFunctions.HumanInteraction
{
    public class VacationRequest
    {
        public string employeeID { get; set; }

        public string employeeFirstName { get; set; }
        public string employeeLastName { get; set; }
        public string employeeEmail { get; set; }
        public string managerEmail { get; set; }

        public DateTime dateFrom { get; set; }
        public DateTime dateTo { get; set; }

        public string notes { get; set; }

        public override string ToString()
        {
            return $"employeeID={employeeID}, employeeFirstName={employeeFirstName}, employeeLastName={employeeLastName}, employeeEmail={employeeEmail}, managerEmail={managerEmail}, dateFrom={dateFrom}, dateTo={dateTo}, notes={notes}";
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(employeeID))
                return false;
            if (string.IsNullOrWhiteSpace(managerEmail))
                return false;
            if (dateFrom > dateTo)
                return false;

            return true;
        }
    }
}