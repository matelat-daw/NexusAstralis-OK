using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Models.User;

namespace NexusAstralis.Data
{
    public class UserContext(DbContextOptions<UserContext> options) : IdentityDbContext<NexusUser>(options)
    {
        public virtual DbSet<Favorites> Favorites { get; set; } = default!;
        public virtual DbSet<Comments> Comments { get; set; } = default!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // USERS SEEDING
            List<NexusUser> users =
            [
                new()
                {
                    Nick = "Mae",
                    Name = "Leslie Ann",
                    Surname1 = "Ledesma",
                    Surname2 = "Lara",
                    UserName = "ledesma.leslie@gmail.com",
                    NormalizedUserName = "LEDESMA.LESLIE@GMAIL.COM",
                    Email = "ledesma.leslie@gmail.com",
                    NormalizedEmail = "LEDESMA.LESLIE@GMAIL.COM",
                    EmailConfirmed = true,
                    PhoneNumber = "644388160",
                    Bday = DateOnly.Parse("1995-07-15"),
                    ProfileImage = "/imgs/profile/Mae/Leslie.jpg",
                    About = null,
                    UserLocation = null,
                    PublicProfile = false
                },
                new()
                {
                    Nick = "PatrokWanzana",
                    Name = "Patrick Edward",
                    Surname1 = "Murphy",
                    Surname2 = "González",
                    UserName = "patrickmurphygon@gmail.com",
                    NormalizedUserName = "PATRICKMURPHYGON@GMAIL.COM",
                    Email = "patrickmurphygon@gmail.com",
                    NormalizedEmail = "PATRICKMURPHYGON@GMAIL.COM",
                    EmailConfirmed = true,
                    PhoneNumber = "634547833",
                    Bday = DateOnly.Parse("2001-01-03"),
                    ProfileImage = "/imgs/profile/Patrokwanzana/Patrick.jpg",
                    About = null,
                    UserLocation = null,
                    PublicProfile = false
                },
                new()
                {
                    Nick = "Orions@68",
                    Name = "César Osvaldo",
                    Surname1 = "Matelat",
                    Surname2 = "Borneo",
                    UserName = "cesarmatelat@gmail.com",
                    NormalizedUserName = "CESARMATELAT@GMAIL.COM",
                    Email = "cesarmatelat@gmail.com",
                    NormalizedEmail = "CESARMATELAT@GMAIL.COM",
                    EmailConfirmed = true,
                    PhoneNumber = "664774821",
                    Bday = DateOnly.Parse("1968-04-05"),
                    ProfileImage = "/imgs/profile/Orions@68/profile.jpg",
                    About = null,
                    UserLocation = null,
                    PublicProfile = false
                }
            ];
            builder.Entity<NexusUser>().HasData(users);
            // PASSWORD HASH
            var passwordHasher = new PasswordHasher<NexusUser>();
            users[0].PasswordHash = passwordHasher.HashPassword(users[0], "Leslie@Leader");
            users[1].PasswordHash = passwordHasher.HashPassword(users[1], "Patrick@Coleader");
            users[2].PasswordHash = passwordHasher.HashPassword(users[2], "Cesar@Peon");

            // ROLES SEEDING
            List<IdentityRole> roles =
            [
                new()
                {
                    Name = "Basic",
                    NormalizedName = "BASIC"
                },
                new()
                {
                    Name = "Premium",
                    NormalizedName = "PREMIUM"
                },
                new()
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                }
            ];
            builder.Entity<IdentityRole>().HasData(roles);

            // USERS-ROLES SEEDING
            List<IdentityUserRole<string>> usersRoles =
            [
                new()
                {
                    UserId = users[0].Id,
                    RoleId = roles[2].Id
                },
                new()
                {
                    UserId = users[1].Id,
                    RoleId = roles[2].Id
                },
                new()
                {
                    UserId = users[2].Id,
                    RoleId = roles[2].Id
                }
            ];
            builder.Entity<IdentityUserRole<string>>().HasData(usersRoles);
        }
    }
}