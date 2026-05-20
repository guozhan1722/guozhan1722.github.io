namespace MyGithubPage.Models
{
    public class MediaItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public string Type  { get; set; } //video, image, audio
    }
}
