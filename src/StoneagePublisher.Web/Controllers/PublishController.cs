using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Web.Http;
using log4net;
using StoneagePublisher.ClassLibrary.Services;
using StoneagePublisher.Web.Core;
using StoneagePublisher.Web.Models.Publish;

namespace StoneagePublisher.Web.Controllers
{
    public class PublishController : ApiController
    {
        private readonly CompressionService compressionService;

        private ILog logger;
        private ILog Logger => logger ?? (logger = LogManager.GetLogger(GetType()));

        public PublishController()
        {
            compressionService = new CompressionService();
        }

        [HttpGet]
        public IHttpActionResult Health()
        {
            Logger.Info("Health check: OK");
            return Ok(new {Health = "Ok"});
        }

        [HttpPost]
        public IHttpActionResult HandlePublish(PublishRequestModel model)
        {
            if (model == null)
            {
                return BadRequest("Nothing was posted");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);    
            }

            Logger.Info(FormattableString.Invariant($"Received publish request for {model.WebRootPath}"));
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //var bytes = Convert.FromBase64String(model.Bytes);
            var stream = new MemoryStream(model.Bytes);
            var rootPath = ConfigurationManager.AppSettings[SettingKeys.ProjectsRootFolder];
            var outputPath = Path.Combine(rootPath, model.WebRootPath);
            compressionService.ExtractStream(stream, outputPath);

            stopwatch.Stop();
            Logger.Info(FormattableString.Invariant($"Everything was published to {model.WebRootPath} in {stopwatch.Elapsed}"));
            return Ok();
        }
    }
}
