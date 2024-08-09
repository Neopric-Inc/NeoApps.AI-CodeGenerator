using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator
{
    public class MasterCredentialModel
    {
        public int project_id { get; set; }
        [Required]

        public string username { get; set; }
        public string password { get; set; }
    }
}
