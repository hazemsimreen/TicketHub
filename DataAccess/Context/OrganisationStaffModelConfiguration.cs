using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public static class OrganisationStaffModelConfiguration
{
    public static void ApplyOrganisationStaffCore(
        this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(x => x.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(x => new
            {
                x.UserId,
                x.RoleId,
                x.DepartmentId
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        });


        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.Name)
                .IsUnique(false);

 








           entity.HasIndex(x => new
            {
                x.DepartmentId,
                x.Name
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        });









        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("Agents");

            entity.HasKey(x => x.Id);







            entity.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");





            entity.HasOne(x => x.User)
                .WithOne()
     


           .HasForeignKey<Agent>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Department)
 



               .WithMany(x => x.Agents)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

 



           entity.HasMany(x => x.Skills)
                .WithMany(x => x.Agents)
                .UsingEntity(j => j.ToTable("AgentSkills"));
        });





        modelBuilder.Entity<AgentProfile>(entity =>
        {
            entity.ToTable("AgentProfiles");





            entity.HasKey(x => x.Id);

       


     entity.HasIndex(x => x.AgentId)
                .IsUnique()
 

               .HasFilter("[IsDeleted] = 0");

            entity.HasOne(x => x.Agent)
 


               .WithOne(x => x.Profile)
                .HasForeignKey<AgentProfile>(x => x.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });



        modelBuilder.Entity<Skill>(entity =>
        {
 


           entity.ToTable("Skills");

 

           entity.HasKey(x => x.Id);

 

           entity.Property(x => x.Name)
                .HasMaxLength(100)
 

               .IsRequired();

            entity.HasIndex(x => x.Name)



                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
 

       });
    }
}
