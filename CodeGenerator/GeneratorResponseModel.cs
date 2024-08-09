using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator
{
    public class GeneratorResponseModel
    {
        [Range(int.MinValue, int.MaxValue)]
        public int project_id { get; set; }
        [Required]
        public string classifier { get; set; }

        [Required]
        public string link { get; set; }

        public List<string> error_log { get; set; }
        [Required]
        public int is_successful { get; set; }

        

    }
}
