namespace QuestradeAPI
{
    public class OrderNotification
    {
        public Order[] orders { get; set; }
        public int accountNumber { get; set; }
    }

    public class ExecutionNotification
    {
        public Order[] orders { get; set; }
        public Execution[] executions { get; set; }
        public int accountNumber { get; set; }
    }
}