using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// Maps the C# models onto the EXISTING RaceDayDB tables and snake_case columns.
/// No migrations are used: the database is changed only by docs/part3_upgrade.sql.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrollment> EventEnrollments => Set<EventEnrollment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    // The existing columns are varchar, not nvarchar.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().AreUnicode(false);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("user_id");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date");
            e.Property(x => x.EmergencyContact).HasColumnName("emergency_contact").HasMaxLength(150);
            e.Property(x => x.ClubName).HasColumnName("club_name").HasMaxLength(100);
            e.Property(x => x.ProfilePicturePath).HasColumnName("profile_picture_path").HasMaxLength(300);
        });

        b.Entity<RaceEvent>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("event_id");
            e.Property(x => x.OrganiserId).HasColumnName("organiser_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.EventDate).HasColumnName("event_date").HasColumnType("date");
            e.Property(x => x.Location).HasColumnName("location").HasMaxLength(150);
            e.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DistanceKm).HasColumnName("distance_km").HasPrecision(6, 2);
            e.Property(x => x.BannerImagePath).HasColumnName("banner_image_path").HasMaxLength(300);
            e.HasOne(x => x.Organiser).WithMany().HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("category_id");
            e.Property(x => x.EventId).HasColumnName("event_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50);
            e.Property(x => x.DistanceKm).HasColumnName("distance_km").HasPrecision(6, 2);
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
            e.Property(x => x.MaxParticipants).HasColumnName("max_participants");
            e.Property(x => x.MinAge).HasColumnName("min_age");
            e.Property(x => x.MaxAge).HasColumnName("max_age");
            e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EventEnrollment>(e =>
        {
            e.ToTable("Entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("entry_id");
            e.Property(x => x.UserId).HasColumnName("participant_id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.EnrolledOn).HasColumnName("entry_date");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RaceResult>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("result_id");
            e.Property(x => x.EnrollmentId).HasColumnName("entry_id");
            e.Property(x => x.FinishTime).HasColumnName("finish_time");
            e.Property(x => x.Position).HasColumnName("position_overall");
            e.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            e.Property(x => x.IsPublished).HasColumnName("is_published");
            e.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ---------- deletes that don't rely on database cascade rules ----------
    private async Task RemoveCategoryChildrenAsync(List<int> categoryIds)
    {
        var entries = await EventEnrollments.Where(e => categoryIds.Contains(e.CategoryId)).ToListAsync();
        var entryIds = entries.Select(e => e.Id).ToList();
        Results.RemoveRange(await Results.Where(r => entryIds.Contains(r.EnrollmentId)).ToListAsync());
        EventEnrollments.RemoveRange(entries);
    }

    public async Task DeleteCategoryCascadeAsync(Category category)
    {
        await RemoveCategoryChildrenAsync(new List<int> { category.Id });
        Categories.Remove(category);
        await SaveChangesAsync();
    }

    public async Task DeleteEventCascadeAsync(RaceEvent ev)
    {
        var categories = await Categories.Where(c => c.EventId == ev.Id).ToListAsync();
        await RemoveCategoryChildrenAsync(categories.Select(c => c.Id).ToList());
        Categories.RemoveRange(categories);
        Events.Remove(ev);

        // Part 1 table that points at events; clear this event's rows so the delete is not blocked.
        if (Database.IsRelational())
        {
            try { await Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Weather_Forecasts WHERE event_id = {ev.Id}"); }
            catch (DbException) { /* table not present: nothing to clear */ }
        }
        await SaveChangesAsync();
    }
}
