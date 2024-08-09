using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using nkv.MicroService.Manager.Interface;

namespace nkv.MicroService.API.Controllers
{
    [Authorize]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IS3bucketManager _bucketManager;
        public FileUploadController(IConfiguration configuration, IS3bucketManager s3BucketManager)
        {
            _configuration = configuration;
            _bucketManager = s3BucketManager;
        }

        [HttpPost]
        [Route(APIEndpoint.DefaultRoute)]
        public async Task<IActionResult> UploadFile([FromForm] Model.FileUploadModel model)
        {
            try
            {
                Console.WriteLine(model);
                if (model.File == null || model.File.Length == 0)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "No file received"));
                }

                if (!int.TryParse(model.BucketId, out int bucketId) || bucketId <= 0)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Invalid integer value"));
                }

                APIResponse bucket = _bucketManager.GetS3bucketByID(bucketId);
                S3bucketModel bux = (S3bucketModel)bucket.Document;

                AmazonS3Config config = new AmazonS3Config
                {
                    ServiceURL = _configuration["AWS:EndPoint"],
                    ForcePathStyle = true
                };

                var s3Client = new AmazonS3Client(bux.bucket_url, _configuration["AWS:SecretKey"], config);

                var fileTransferUtility = new TransferUtility(s3Client);
                string key = $@"{_configuration["AWS:SubBucketName"]}/{model.folderselected}/{model.File.FileName}";
                // replace 'your-bucket-name' with your actual bucket name
                await fileTransferUtility.UploadAsync(model.File.OpenReadStream(), bux.bucket_name, key);

                // Generate pre-signed URL for the uploaded object
                /*var request = new Amazon.S3.Model.GetPreSignedUrlRequest
                {
                    BucketName = bux.bucket_name,
                    Key = model.File.FileName,
                    Expires = DateTime.UtcNow.AddHours(1)  // Expires in 1 hour
                };

                var url = s3Client.GetPreSignedURL(request);*/

                return Ok(new APIResponse(ResponseCode.SUCCESS, "File uploaded successfully", model.File.FileName));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
        [HttpPost]
        [Route(APIEndpoint.DefaultRoute + "/geturl")]
        public async Task<IActionResult> GetUrl([FromForm] UrlModel model)
        {

            try
            {
                if (!int.TryParse(model.BucketId, out int bucketId) || bucketId <= 0)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Invalid integer value"));
                }

                APIResponse bucket = _bucketManager.GetS3bucketByID(bucketId);
                S3bucketModel bux = (S3bucketModel)bucket.Document;

                AmazonS3Config config = new AmazonS3Config
                {
                    ServiceURL = _configuration["AWS:EndPoint"],
                    ForcePathStyle = true
                };

                var s3Client = new AmazonS3Client(bux.bucket_url, _configuration["AWS:SecretKey"], config);

                // Generate pre-signed URL for the uploaded object
                var request = new Amazon.S3.Model.GetPreSignedUrlRequest
                {
                    BucketName = bux.bucket_name,
                    Key = model.Key,
                    Expires = DateTime.UtcNow.AddHours(1)  // Expires in 1 hour
                };

                var url = s3Client.GetPreSignedURL(request);

                return Ok(new APIResponse(ResponseCode.SUCCESS, "url Generated successfully", url));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
    }
}
