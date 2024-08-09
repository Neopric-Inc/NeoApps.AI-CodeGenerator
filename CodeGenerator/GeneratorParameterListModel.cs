using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NoCodeAppGenerator
{
    public class GeneratorParameterListModel
    {
        [Range(int.MinValue, int.MaxValue)]
        public int project_id { get; set; }
        public string server { get; set; }

        public string uid { get; set; }

        
        public string username { get; set; }

        
        public string password { get; set; }

        
        public string databaseName { get; set; }

        
        public string script { get; set; }

        
        public string statusOfGeneration { get; set; }

        
        public string projectName { get; set; }

        
        public string DBexists { get; set; }

        
        public string port { get; set; }

        
        public string rabbitMQConn { get; set; }

        
        public string Technology_Frontend { get; set; }

        
        public string Backend_technology { get; set; }

        
        public string buttonClicked { get; set; }

        
        public string projectType { get; set; }

        
        public string swgurl { get; set; }

        
        public string noderedurl { get; set; }
    }
}
