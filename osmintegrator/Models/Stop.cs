using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace osmintegrator.Models
{
    public class Stop
    {
        [Key]
        [Required]
        public int StopId { get; set; }

        [Required]
        public int TypeId { get; set; }

        [Required]
        public string StopName { get; set; }

        [Required]
        public float Lat { get; set; }
       
        [Required]
        public float Lon { get; set; }
             
    }
}
