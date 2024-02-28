using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace studiaT_G_test.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<DbInfoModel> DbIModels { get; set; } 
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            :base(options)
        {
            Database.EnsureCreated();
        }
    }
}
