using osmintegrator.Database;
using osmintegrator.Database.DataInitialization;
using osmintegrator.Models;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("***** Fun with ADO.NET EF Core 2 *****\n");

            using (var context = new AppDbContext())
            {
                DataInitializer.RecreateDatabase(context);
                DataInitializer.InitializeData(context);
                foreach (Stop c in context.Stops)
                {
                    Console.WriteLine(c);
                }
                Console.WriteLine("***** Using a Repository *****\n");
                //using (var repo = new InventoryRepo(context))
                //{
                //    foreach (Inventory c in repo.GetAll())
                //    {
                //        Console.WriteLine(c);
                //    }
                //}
            }
            //TestConcurrency();
            Console.ReadLine();
        }
    }
}
