using System;

namespace DurableFunctions.HumanInteraction
{
    public class VacationResponse
    {
        public VacationRequest request { get; set; }

        public bool? isApproved { get; set; }
        public string instanceId { get;  set; }

        public override string ToString()
        {
            return $"request=[{request}], isApproved={isApproved}, instanceId={instanceId}";
        }

    }
}