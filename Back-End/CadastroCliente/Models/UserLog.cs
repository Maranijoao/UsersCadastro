namespace CadastroCliente.Models
{
    public class UserLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
        public string Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
