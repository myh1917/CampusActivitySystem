using CampusActivitySystem.Data;
using CampusActivitySystem.Models;

namespace CampusActivitySystem.Services
{
    public class NoticeService
    {
        private readonly AppDbContext _context;

        public NoticeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendAsync(long userId, string type, string title, string content)
        {
            var notice = new Notice
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notices.Add(notice);
            await _context.SaveChangesAsync();
        }
    }
}