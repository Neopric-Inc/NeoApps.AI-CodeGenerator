using System.Collections.Generic;

namespace NoCodeAppGenerator
{
    /// <summary>Result of a generator run. Printed as a single "NEOAPPS_RESULT {json}" line on stdout.</summary>
    public class GeneratorResponseModel
    {
        public int project_id { get; set; }
        public string classifier { get; set; }
        /// <summary>S3 pre-signed URL when storage is configured, otherwise the local zip path.</summary>
        public string link { get; set; }
        /// <summary>Folder holding the generated project (DotNet_Output / ReactTs_Output*).</summary>
        public string output_path { get; set; }
        public List<string> error_log { get; set; }
        public int is_successful { get; set; }
        /// <summary>Login seeded into the generated app's Users table (plain text, local development only).</summary>
        public string admin_username { get; set; }
        public string admin_password { get; set; }
    }
}
