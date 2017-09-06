using System.ComponentModel.DataAnnotations;

namespace StoneagePublisher.Web.Models.Publish
{
    public class PublishRequestModel
    {
        [Required]
        public byte[] Bytes { get; set; }

        [Required]
        public string WebRootPath { get; set; }
    }
}