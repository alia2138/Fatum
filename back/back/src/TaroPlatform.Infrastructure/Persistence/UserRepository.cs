using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using TaroPlatform.Application.Catalog;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Infrastructure.Persistence;

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(MongoContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Collection.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(u => u.ExternalLogins.Any(e => e.Provider == provider && e.ProviderUserId == providerUserId))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Пошук у каталозі: лише таро-консультанти, які приймають клієнтів.
    /// Фільтри, сортування і сторінки виконуються в MongoDB, а не в пам'яті.
    /// </summary>
    public async Task<(IReadOnlyList<Consultant> Items, long Total)> SearchConsultantsAsync(ConsultantSearch search, CancellationToken cancellationToken = default)
    {
        var f = Builders<User>.Filter;
        var filters = new List<FilterDefinition<User>>
        {
            f.OfType<Consultant>(),
            f.Eq("IsAcceptingClients", true)
        };

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var pattern = Regex.Escape(search.Text.Trim());
            filters.Add(f.Regex("DisplayName", new BsonRegularExpression(pattern, "i")));
        }

        if (search.Topic is { } topic)
            filters.Add(f.AnyEq("Specializations", topic));

        if (!string.IsNullOrWhiteSpace(search.Language))
        {
            var pattern = "^" + Regex.Escape(search.Language.Trim()) + "$";
            filters.Add(f.Regex("Languages", new BsonRegularExpression(pattern, "i")));
        }

        if (search.MinPrice is { } minPrice)
            filters.Add(f.Gte("PriceUah", minPrice));
        if (search.MaxPrice is { } maxPrice)
            filters.Add(f.Lte("PriceUah", maxPrice));
        if (search.MinExperience is { } minExperience)
            filters.Add(f.Gte("ExperienceYears", minExperience));

        var filter = f.And(filters);

        var sortField = search.SortBy switch
        {
            "experience" => "ExperienceYears",
            "name" => "DisplayName",
            "rating" => "RatingAverage",
            _ => "PriceUah"
        };
        var sort = search.Descending
            ? Builders<User>.Sort.Descending(sortField)
            : Builders<User>.Sort.Ascending(sortField);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var found = await Collection
            .Find(filter)
            .Sort(sort)
            .Skip((search.Page - 1) * search.PageSize)
            .Limit(search.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyList<Consultant> consultants = found.OfType<Consultant>().ToList();
        return (consultants, total);
    }
}
