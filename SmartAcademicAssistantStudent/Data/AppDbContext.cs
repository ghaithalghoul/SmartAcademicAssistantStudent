using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Models;

namespace SmartAcademicAssistantStudent.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<CourseSection> CourseSections { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<CourseReview> CourseReviews { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ====== User ======
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(u => u.Email).IsUnique(); // البريد لا يتكرر
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            });

            // ====== Student ======
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.UniversityId).IsRequired().HasMaxLength(20);
                entity.HasIndex(s => s.UniversityId).IsUnique();
                entity.Property(s => s.Major).IsRequired().HasMaxLength(100);
                entity.Property(s => s.GPA).HasPrecision(3, 2); // مثل: 3.75

                // علاقة Student -> User (one-to-one)
                entity.HasOne(s => s.User)
                      .WithOne(u => u.Student)
                      .HasForeignKey<Student>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ====== Instructor ======
            modelBuilder.Entity<Instructor>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Name).IsRequired().HasMaxLength(100);
                entity.Property(i => i.Department).IsRequired().HasMaxLength(100);
            });

            // ====== Course ======
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Code).IsRequired().HasMaxLength(20);
                entity.HasIndex(c => c.Code).IsUnique(); // كود المادة لا يتكرر
                entity.Property(c => c.Department).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(500);
            });

            // ====== CourseSection ======
            modelBuilder.Entity<CourseSection>(entity =>
            {
                entity.HasKey(cs => cs.Id);
                entity.Property(cs => cs.SectionNumber).IsRequired().HasMaxLength(10);
                entity.Property(cs => cs.Time).HasMaxLength(50);
                entity.Property(cs => cs.Location).HasMaxLength(100);

                // علاقة CourseSection -> Course
                entity.HasOne(cs => cs.Course)
                      .WithMany(c => c.Sections)
                      .HasForeignKey(cs => cs.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                // علاقة CourseSection -> Instructor
                entity.HasOne(cs => cs.Instructor)
                      .WithMany(i => i.Sections)
                      .HasForeignKey(cs => cs.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict); // لا تحذف الأستاذ إذا عنده sections
            });

            // ====== Enrollment ======
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Semester).IsRequired().HasMaxLength(20);

                // منع تسجيل نفس الطالب في نفس الـ Section مرتين
                entity.HasIndex(e => new { e.StudentId, e.CourseSectionId }).IsUnique();

                // علاقة Enrollment -> Student
                entity.HasOne(e => e.Student)
                      .WithMany(s => s.Enrollments)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // علاقة Enrollment -> CourseSection
                entity.HasOne(e => e.CourseSection)
                      .WithMany(cs => cs.Enrollments)
                      .HasForeignKey(e => e.CourseSectionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ====== CourseReview ======
            modelBuilder.Entity<CourseReview>(entity =>
            {
                entity.HasKey(cr => cr.Id);
                entity.Property(cr => cr.Comment).HasMaxLength(1000);

                // طالب واحد = review واحد لكل مادة
                entity.HasIndex(cr => new { cr.StudentId, cr.CourseId }).IsUnique();

                // علاقة CourseReview -> Student
                entity.HasOne(cr => cr.Student)
                      .WithMany(s => s.Reviews)
                      .HasForeignKey(cr => cr.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // علاقة CourseReview -> Course
                entity.HasOne(cr => cr.Course)
                      .WithMany(c => c.Reviews)
                      .HasForeignKey(cr => cr.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ====== ChatMessage ======
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(cm => cm.Id);
                entity.Property(cm => cm.Message).IsRequired().HasMaxLength(2000);
                entity.Property(cm => cm.Response).HasMaxLength(5000);

                // علاقة ChatMessage -> User
                entity.HasOne(cm => cm.User)
                      .WithMany(u => u.ChatMessages)
                      .HasForeignKey(cm => cm.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ====== FAQ ======
            modelBuilder.Entity<FAQ>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Question).IsRequired().HasMaxLength(500);
                entity.Property(f => f.Answer).IsRequired().HasMaxLength(2000);
            });
            modelBuilder.Entity<CoursePrerequisite>(entity =>
            {
                entity.HasKey(cp => cp.Id);

                entity.HasOne(cp => cp.Course)
                      .WithMany(c => c.Prerequisites)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cp => cp.RequiredCourse)
                      .WithMany()
                      .HasForeignKey(cp => cp.RequiredCourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                // نفس المادة ما تتكرر كـ prerequisite
                entity.HasIndex(cp => new { cp.CourseId, cp.RequiredCourseId }).IsUnique();
            });
        }
    }
}