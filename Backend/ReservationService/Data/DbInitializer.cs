using ReservationService.Data;

namespace ReservationService.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ReservationDbContext context)
        {
            // Migrations should handle creation, but EnsureCreated is good for quick start if no migrations
            // However, since we are using migrations, we should rely on them. 
            // But for seeding, we just check if data exists.
            
            if (context.Tables.Any())
            {
                return;   // DB has been seeded
            }

            var tables = new List<Table>();

            // Floor 1
            for (int i = 1; i <= 10; i++)
            {
                tables.Add(new Table { TableNumber = $"Masa 1-{i}", FloorId = 1 });
            }

            // Floor 2
            for (int i = 1; i <= 10; i++)
            {
                tables.Add(new Table { TableNumber = $"Masa 2-{i}", FloorId = 2 });
            }

            // Floor 3
            for (int i = 1; i <= 10; i++)
            {
                tables.Add(new Table { TableNumber = $"Masa 3-{i}", FloorId = 3 });
            }

            context.Tables.AddRange(tables);
            context.SaveChanges();
        }
    }
}
