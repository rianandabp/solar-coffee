using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarCoffee.Data;
using SolarCoffee.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarCoffee.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly SolarDbContext _db; 
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(SolarDbContext dbContext, ILogger<InventoryService>, ILogger<InventoryService> logger)
        {
            _db = dbContext;
            _logger = logger;
        }
        public InventoryService(SolarDbContext dbContext)
        {
            _db = dbContext;
        }

        private void CreateSnapshot(ProductInventory inventory)
        {
            var now = DateTime.UtcNow;

            var snapshot = new ProductInventorySnapshot
            {
                SnapshotTime = now,
                Product = inventory.Product, 
                QuantityOnHand = inventory.QuantityOnHand
            };

            _db.Add(snapshot);
            _db.SaveChanges();
        }

        public List<ProductInventorySnapshot> GetSnapshotsHistory()
        {
            var earliest = DateTime.UtcNow - TimeSpan.FromHours(6);
            return _db.ProductInventorySnapshots.Include(snap => snap.Product).Where(snap => snap.SnapshotTime > earliest && !snap.Product.IsArchived).ToList();
        }

        public List<ProductInventory> GetCurrentInventory()
        {
            return _db.ProductInventories.Include(pi => pi.Product).Where(pi => !pi.Product.IsArchived).ToList();
        }

        public ProductInventory GetProductById(int productId)
        {
            return _db.ProductInventories.Include(pi => pi.Product).FirstOrDefault(pi => pi.Product.Id == productId);
        }

        public List<ProductInventorySnapshot> GetSnapshotHistory()
        {
            throw new NotImplementedException();
        }

        public ServiceResponse<ProductInventory> UpdateUnitsAvailable(int id, int adjustment)
        {
            var now = DateTime.UtcNow;
            try
            {
                var inventory = _db.ProductInventories.Include(inv => inv.Product).First(inv => inv.Product.Id == id);

                inventory.QuantityOnHand += adjustment;

                try
                {
                    CreateSnapshot(inventory);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error creating inventory snapshot");
                    _logger.LogError(e.StackTrace);
                }
                _db.SaveChanges();

                return new ServiceResponse<ProductInventory> {
                    IsSuccess = true,
                    Data = inventory,
                    Message = $"Product {id} inventory adjusted",
                    Time = now
                };
            }
            catch (Exception e)
            {
                return new ServiceResponse<ProductInventory>
                {
                    IsSuccess = false,
                    Data = null,
                    Message = $"Error updating ProductInventory QuantityOnHand",
                    Time = now
                };
            }
        }
    }
}
