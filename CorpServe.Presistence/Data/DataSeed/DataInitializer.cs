using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Presistence.Data.DbContext;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorpServe.Presistence.Data.DataSeed
{
    public class DataInitializer : IDataInitializer
    {
        private readonly CorpServeDbContext _dbContext;

        public DataInitializer(CorpServeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var HasCategories = await _dbContext.Categories.AnyAsync();
                if (HasCategories) return;
                if (!HasCategories)
                {
                    await SeedDataFromJsonAsync<Category, string>("Categories.json", _dbContext.Categories);
                }
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error While Seeding Data : {ex}");
            }
        }

        private async Task SeedDataFromJsonAsync<T, TKey>(string fileName, DbSet<T> dbset) where T : BaseEntity<TKey>
        {

            var FilePath = @"..\CorpServe.Presistence\Data\DataSeed\JSONFiles\" + fileName;
            if (!File.Exists(FilePath)) throw new FileNotFoundException($"File {fileName} is not exists");
            try
            {
                using var DataStream = File.OpenRead(FilePath);
                var Data = await JsonSerializer.DeserializeAsync<List<T>>(DataStream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                if (Data is not null)
                {
                    await dbset.AddRangeAsync(Data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error While Reading Json File : {ex}");
                return;
            }
        }
    }
}
