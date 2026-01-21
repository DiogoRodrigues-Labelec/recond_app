namespace Recondicionamento_DTC_Routers.Workflow
{
    public enum StepStatus { Pending, Running, Ok, Fail, Skipped }

    public sealed class StepVm
    {
        public int Order { get; }
        public string Name { get; }
        public StepStatus Status { get; set; }
        public string Detail { get; set; }

        public StepVm(int order, string name)
        {
            Order = order;
            Name = name;
            Status = StepStatus.Pending;
            Detail = "";
        }
    }
}
