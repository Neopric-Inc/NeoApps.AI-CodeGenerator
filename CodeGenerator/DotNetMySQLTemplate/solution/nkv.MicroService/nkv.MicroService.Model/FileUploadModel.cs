using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace nkv.MicroService.Model
{
    public class FileUploadModel
    {
        public IFormFile File { get; set; }
        public string BucketId { get; set; }
        public string folderselected { get; set; }
    }

}
