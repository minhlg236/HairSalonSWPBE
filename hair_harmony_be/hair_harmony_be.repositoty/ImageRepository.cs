using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.EntityFrameworkCore;

namespace hair_harmony_be.hair_harmony_be.Repositories
{
    public class ImageRepository
    {
        private readonly AppDbContext _context;

        public ImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Image>> GetAllImagesAsync()
        {
            return await _context.Images.ToListAsync();
        }

        public async Task<Image> GetImageByIdAsync(int id)
        {
            return await _context.Images.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task AddImageAsync(Image image)
        {
            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateImageAsync(Image image)
        {
            _context.Images.Update(image);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageAsync(int id)
        {
            var image = await GetImageByIdAsync(id);
            if (image != null)
            {
                _context.Images.Remove(image);
                await _context.SaveChangesAsync();
            }
        }
    }
}
