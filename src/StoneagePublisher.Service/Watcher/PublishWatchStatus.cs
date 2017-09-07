using System;

namespace StoneagePublisher.Service.Watcher
{
    public class PublishWatchStatus
    {
        public DateTime? LastUpdate { get; set; }
        public DateTime? LastProcessed { get; set; }

        public bool ProcessedAfterPublish => LastProcessed.HasValue && LastProcessed > LastUpdate;
    }
}
