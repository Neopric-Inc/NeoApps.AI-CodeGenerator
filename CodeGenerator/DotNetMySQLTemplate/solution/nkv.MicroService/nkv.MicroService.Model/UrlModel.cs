using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace nkv.MicroService.Model
{
   public class UrlModel
{
    public string Key { get; set; }
    public string BucketId { get; set; }
}

}
