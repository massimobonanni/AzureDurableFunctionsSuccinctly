using System;

namespace DurableFunctions.HumanInteraction
{
    public class VacationResponseRow
    {
        public VacationResponseRow()
        {
        }

        public VacationResponseRow(VacationResponse response)
        {
            this.employeeID = response.request.employeeID;
            this.dateFrom = response.request.dateFrom;
            this.dateTo = response.request.dateTo;
            this.employeeEmail = response.request.employeeEmail;
            this.employeeFirstName = response.request.employeeFirstName;
            this.employeeID = response.request.employeeID;
            this.employeeLastName = response.request.employeeLastName;
            this.instanceId = response.instanceId;
            this.isApproved = response.isApproved;
            this.managerEmail = response.request.managerEmail;
            this.notes = response.request.notes;
        }

        public string PartitionKey
        {
            get
            {
                return employeeID;
            }
        }

        public string RowKey
        {
            get
            {
                return instanceId;
            }
        }

        public string employeeID { get; set; }

        public string employeeFirstName { get; set; }
        public string employeeLastName { get; set; }
        public string employeeEmail { get; set; }
        public string managerEmail { get; set; }

        public DateTime dateFrom { get; set; }
        public DateTime dateTo { get; set; }

        public string notes { get; set; }

        public bool? isApproved { get; set; }
        public string instanceId { get; internal set; }
    }
}