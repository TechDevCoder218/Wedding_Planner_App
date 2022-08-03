#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SampleWebApp.Models;
public class Wedding
{
    [Key]
    [Required]
    public int WeddingId { get; set; }
    [Required]
    public string WedderOne {get;set;}
    [Required]
    public string WedderTwo {get;set;}
    [Required]
    [DataType(DataType.Date)]
    public DateTime WedDate {get;set;}
    [Required]
    public string WedAddress {get;set;}
    public int UserId {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.Now;
    public DateTime UpdatedAt {get;set;} = DateTime.Now;
    public int Creator {get;set;}
    public List<GuestList> GuestsInWedding {get;set;} = new List<GuestList>();
}