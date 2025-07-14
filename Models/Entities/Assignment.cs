using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace POS_Shoes.Models.Entities
{
    public class Assignment
    {
        [Key]
        public Guid AssignmentID { get; set; }
        public string Description { get; set; }
        public string Shift { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }


        [ForeignKey("UserID")]
        public Guid UserID { get; set; }
        public User? User { get; set; }

    }
}