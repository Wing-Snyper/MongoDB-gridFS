using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDBExample.Models;

namespace MongoDBExample.Controllers
{
    public class HomeController : Controller
    {
        public readonly ApplicationDbContext ContextNew = new ApplicationDbContext();
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("UploadFiles")]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
            ObjectId uploadSuccess;
            foreach (var formFile in files)
            {
                if (formFile.Length <= 0)
                {
                    continue;
                }
                using (var stream = formFile.OpenReadStream())
                {
                    uploadSuccess = await MongoUpload(formFile.FileName, stream);
                    ViewBag.Message = uploadSuccess;
                }
            }

            return View("UploadSuccess");
        }
        private async Task<ObjectId> MongoUpload(string filename, Stream stream)
        {

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    {"ContentType","image/jpeg"}
                }
            };
            var id = await ContextNew.ImagesBucket.UploadFromStreamAsync(filename, stream, options);
            return id;
            //if (id == null)
            //    return false;
            //else
            //    return true;
        }


        public ActionResult MongoDownload(string id)
        {
            try
            {
                var stream = ContextNew.ImagesBucket.OpenDownloadStream(new ObjectId(id));
                var contentType = stream.FileInfo.Metadata["ContentType"].AsString;
                return File(stream, contentType);
            }
            catch (GridFSFileNotFoundException)
            {
                return HttpNotFound();
            }
        }

        private ActionResult HttpNotFound()
        {
            throw new NotImplementedException();
        }
    }
}
