namespace StoneagePublisher.ClassLibrary.Entities
{
    public class PublishRequest
    {
        public byte[] Bytes { get; set; }
        public string WebRootPath { get; set; }
        public string FileName { get; set; }
    }
}