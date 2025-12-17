namespace AssignmentC_.Models
{
    public class ActionLogViewModel
    {
        public List<ActionLog> Logs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
