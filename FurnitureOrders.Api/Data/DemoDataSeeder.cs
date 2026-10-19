using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Orders;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Data;

/// Idempotentní: každý záznam se vloží, jen pokud jeho kód v databázi ještě není.
public static class DemoDataSeeder
{
    private const string Country = "Česká republika";

    private sealed record C(string No, CustomerType Type, string Name, string? Ico, string? Contact,
        string Email, string Phone, string Street, string City, string Zip);

    private sealed record M(string Code, string Name, MaterialType Type, string Unit, decimal Price,
        string Supplier, string? Color);

    private sealed record O(string Code, string Name, string Workplace, string Type, int Setup, decimal Run,
        decimal Rate, string Qualification);

    private sealed record P(string Code, string Name, string Group, string Type, decimal Price,
        decimal W, decimal H, decimal D, decimal Weight, int Days);

    private sealed record PM(string Product, string Material, decimal Qty, decimal Waste);
    private sealed record PO(string Product, string Operation, int Setup, decimal Run, string Instructions);

    private sealed record Ord(string No, string Customer, DateOnly Created, DateOnly Required, DateOnly? Completed,
        OrderStatus Status, string Address, string Shipping, string? Note, (string Product, decimal Qty, decimal Discount)[] Items);

    private static DateOnly D(int month, int day)
    {
        return new(2026, month, day);
    }

    // ------------------------------------------------------------------ data

    private static readonly C[] Customers =
    [
        new("Z90001", CustomerType.Company, "Nábytek Novák s.r.o.", "12345601", "Petr Novák", "petr.novak@example.com", "+420 600 100 001", "Dlouhá 15", "Brno", "602 00"),
        new("Z90002", CustomerType.Company, "Kancelářské systémy Dvořák a.s.", "12345602", "Jana Dvořáková", "jana.dvorakova@example.com", "+420 600 100 002", "Vinohradská 120", "Praha", "120 00"),
        new("Z90003", CustomerType.Company, "Hotel Panorama s.r.o.", "12345603", "Martin Svoboda", "martin.svoboda@example.com", "+420 600 100 003", "Hotelová 8", "Ostrava", "702 00"),
        new("Z90004", CustomerType.Company, "Interiéry Černý s.r.o.", "12345604", "Tomáš Černý", "tomas.cerny@example.com", "+420 600 100 004", "Masarykova 42", "Olomouc", "779 00"),
        new("Z90005", CustomerType.Individual, "Jana Procházková", null, null, "jana.prochazkova@example.com", "+420 600 100 005", "Zahradní 7", "Zlín", "760 01"),
        new("Z90006", CustomerType.Individual, "Karel Veselý", null, null, "karel.vesely@example.com", "+420 600 100 006", "Lipová 3", "Plzeň", "301 00"),
        new("Z90007", CustomerType.Company, "Restaurace U Lípy s.r.o.", "12345607", "Eva Horáková", "eva.horakova@example.com", "+420 600 100 007", "Náměstí 1", "České Budějovice", "370 01"),
        new("Z90008", CustomerType.Company, "Coworking Hub s.r.o.", "12345608", "Lukáš Marek", "lukas.marek@example.com", "+420 600 100 008", "Cejl 25", "Brno", "602 00"),
        new("Z90009", CustomerType.Individual, "Pavla Kučerová", null, null, "pavla.kucerova@example.com", "+420 600 100 009", "Horní 14", "Liberec", "460 01"),
        new("Z90010", CustomerType.Company, "Stavby Beneš s.r.o.", "12345610", "Ondřej Beneš", "ondrej.benes@example.com", "+420 600 100 010", "Průmyslová 90", "Ostrava", "703 00")
    ];

    private static readonly M[] Materials =
    [
        new("MAT-001", "Lamino bílé 18 mm", MaterialType.Sheet, "m2", 245m, "Dřevoobchod Brno", "Bílá"),
        new("MAT-002", "Lamino dub sonoma 18 mm", MaterialType.Sheet, "m2", 289m, "Dřevoobchod Brno", "Dub sonoma"),
        new("MAT-003", "Lamino antracit 18 mm", MaterialType.Sheet, "m2", 279m, "Dřevoobchod Brno", "Antracit"),
        new("MAT-004", "MDF deska 16 mm", MaterialType.Sheet, "m2", 198m, "Desky CZ", null),
        new("MAT-005", "Překližka bříza 12 mm", MaterialType.Sheet, "m2", 410m, "Desky CZ", "Bříza"),
        new("MAT-006", "Záda HDF 3 mm", MaterialType.Sheet, "m2", 62m, "Desky CZ", "Bílá"),
        new("MAT-007", "ABS hrana bílá 22 mm", MaterialType.Other, "m", 6.5m, "Hrany a lišty", "Bílá"),
        new("MAT-008", "ABS hrana dub sonoma 22 mm", MaterialType.Other, "m", 7.2m, "Hrany a lišty", "Dub sonoma"),
        new("MAT-009", "Závěs s tlumením", MaterialType.Hardware, "ks", 38m, "Kování Servis", null),
        new("MAT-010", "Kuličkový výsuv 450 mm", MaterialType.Hardware, "pár", 96m, "Kování Servis", null),
        new("MAT-011", "Úchytka kovová 128 mm", MaterialType.Hardware, "ks", 24m, "Kování Servis", "Nerez"),
        new("MAT-012", "Konfirmát 7x50", MaterialType.Fastener, "ks", 0.9m, "Spojovací materiál s.r.o.", null),
        new("MAT-013", "Kolík dřevěný 8x30", MaterialType.Fastener, "ks", 0.3m, "Spojovací materiál s.r.o.", null),
        new("MAT-014", "Kartonová krabice", MaterialType.Packaging, "ks", 28m, "Obaly Morava", null),
        new("MAT-015", "Ochranná fólie", MaterialType.Packaging, "m2", 12m, "Obaly Morava", null)
    ];

    private static readonly O[] Operations =
    [
        new("OP-001", "Formátování dílců", "Formátovací pila", "Dělení", 10, 4m, 650m, "Operátor pily"),
        new("OP-002", "Olepování hran", "Olepovačka", "Hranění", 8, 3m, 600m, "Operátor olepovačky"),
        new("OP-003", "CNC frézování", "CNC centrum", "Obrábění", 15, 6m, 850m, "CNC programátor"),
        new("OP-004", "CNC vrtání", "CNC centrum", "Obrábění", 10, 3m, 850m, "CNC operátor"),
        new("OP-005", "Broušení", "Brusírna", "Povrchová úprava", 5, 5m, 500m, "Truhlář"),
        new("OP-006", "Lakování", "Lakovna", "Povrchová úprava", 20, 8m, 700m, "Lakýrník"),
        new("OP-007", "Předmontáž", "Montáž", "Montáž", 5, 10m, 480m, "Montážní pracovník"),
        new("OP-008", "Finální montáž", "Montáž", "Montáž", 10, 25m, 480m, "Montážní pracovník"),
        new("OP-009", "Kontrola kvality", "Kontrola", "Kontrola", 0, 3m, 520m, "Kontrolor"),
        new("OP-010", "Balení", "Balení", "Balení", 0, 4m, 420m, "Skladník")
    ];

    private static readonly P[] Products =
    [
        new("VYR-001", "Psací stůl Alfa", "Stoly", "Pracovní stůl", 4990m, 1400, 750, 700, 38m, 14),
        new("VYR-002", "Skříňka Beta", "Skříně", "Skříňka", 2490m, 800, 1200, 400, 24m, 10),
        new("VYR-003", "Židle Gama", "Židle", "Kancelářská židle", 1890m, 480, 900, 520, 7m, 7),
        new("VYR-004", "Police Delta", "Police", "Nástěnná police", 1290m, 900, 300, 250, 9m, 7),
        new("VYR-005", "Police Epsilon", "Police", "Nástěnná police", 1590m, 1200, 300, 250, 11m, 7),
        new("VYR-006", "Komoda Omega", "Skříně", "Komoda", 6990m, 1200, 850, 450, 52m, 21),
        new("VYR-007", "Regál Sigma", "Regály", "Knihovna", 3290m, 800, 1800, 350, 30m, 14),
        new("VYR-008", "Postel Lambda", "Ložnice", "Postel 160x200", 8990m, 1700, 900, 2100, 65m, 28),
        new("VYR-009", "Noční stolek Theta", "Ložnice", "Noční stolek", 1190m, 450, 450, 380, 10m, 10),
        new("VYR-010", "Jídelní stůl Kappa", "Stoly", "Jídelní stůl", 7490m, 1800, 760, 900, 55m, 21)
    ];

    // technologický postup pro 5 výrobků
    private static readonly PM[] ProductMaterials =
    [
        new("VYR-001", "MAT-001", 2.4m, 8m), new("VYR-001", "MAT-007", 8m, 5m),
        new("VYR-001", "MAT-012", 24m, 0m), new("VYR-001", "MAT-014", 1m, 0m),
        new("VYR-002", "MAT-002", 3.2m, 8m), new("VYR-002", "MAT-008", 12m, 5m),
        new("VYR-002", "MAT-006", 0.8m, 5m), new("VYR-002", "MAT-009", 4m, 0m),
        new("VYR-002", "MAT-011", 2m, 0m), new("VYR-002", "MAT-012", 30m, 0m),
        new("VYR-003", "MAT-005", 0.9m, 10m), new("VYR-003", "MAT-013", 16m, 0m),
        new("VYR-003", "MAT-014", 1m, 0m),
        new("VYR-004", "MAT-004", 0.7m, 6m), new("VYR-004", "MAT-013", 6m, 0m),
        new("VYR-004", "MAT-014", 1m, 0m),
        new("VYR-005", "MAT-004", 0.95m, 6m), new("VYR-005", "MAT-013", 8m, 0m),
        new("VYR-005", "MAT-014", 1m, 0m)
    ];

    private static readonly PO[] ProductOperations =
    [
        new("VYR-001", "OP-001", 10, 6m, "Nařezat dílce dle výkresu."),
        new("VYR-001", "OP-002", 8, 5m, "Olepit viditelné hrany."),
        new("VYR-001", "OP-003", 15, 8m, "Frézovat průchodky a drážky."),
        new("VYR-001", "OP-008", 10, 25m, "Sestavit stůl a seřídit."),
        new("VYR-001", "OP-009", 0, 3m, "Vizuální a rozměrová kontrola."),
        new("VYR-001", "OP-010", 0, 4m, "Zabalit do fólie a krabice."),
        new("VYR-002", "OP-001", 10, 5m, "Nařezat dílce dle výkresu."),
        new("VYR-002", "OP-002", 8, 6m, "Olepit všechny hrany."),
        new("VYR-002", "OP-004", 10, 4m, "Vyvrtat otvory pro závěsy a konfirmáty."),
        new("VYR-002", "OP-008", 10, 20m, "Sestavit skříňku, osadit dvířka."),
        new("VYR-002", "OP-009", 0, 3m, "Kontrola funkce závěsů."),
        new("VYR-002", "OP-010", 0, 4m, "Zabalit."),
        new("VYR-003", "OP-003", 15, 4m, "Vyfrézovat tvarové díly."),
        new("VYR-003", "OP-005", 5, 6m, "Přebrousit hrany."),
        new("VYR-003", "OP-007", 5, 12m, "Předmontovat sedák a opěrák."),
        new("VYR-003", "OP-009", 0, 2m, "Kontrola stability."),
        new("VYR-003", "OP-010", 0, 3m, "Zabalit."),
        new("VYR-004", "OP-001", 10, 2m, "Nařezat dle výkresu."),
        new("VYR-004", "OP-002", 8, 2m, "Olepit viditelné hrany."),
        new("VYR-004", "OP-006", 20, 5m, "Nalakovat."),
        new("VYR-004", "OP-010", 0, 2m, "Zabalit."),
        new("VYR-005", "OP-001", 10, 3m, "Nařezat dle výkresu."),
        new("VYR-005", "OP-002", 8, 3m, "Olepit viditelné hrany."),
        new("VYR-005", "OP-006", 20, 6m, "Nalakovat."),
        new("VYR-005", "OP-010", 0, 2m, "Zabalit.")
    ];

    private static readonly Ord[] Orders =
    [
        new("ZAK-90001", "Z90001", D(8, 12), D(9, 10), D(9, 8), OrderStatus.Completed, "Dlouhá 15, Brno", "Vlastní doprava",
            "Před doručením prosím zavolat.", [("VYR-001", 3, 0), ("VYR-002", 2, 0)]),
        new("ZAK-90002", "Z90002", D(8, 25), D(9, 30), D(9, 28), OrderStatus.Completed, "Vinohradská 120, Praha", "Spedice",
            null, [("VYR-001", 10, 5), ("VYR-003", 20, 5), ("VYR-004", 4, 0)]),
        new("ZAK-90003", "Z90003", D(9, 3), D(10, 20), null, OrderStatus.InProduction, "Hotelová 8, Ostrava", "Spedice",
            "Montáž na místě.", [("VYR-008", 12, 10), ("VYR-009", 24, 10), ("VYR-006", 2, 0)]),
        new("ZAK-90004", "Z90004", D(9, 10), D(10, 15), null, OrderStatus.Confirmed, "Masarykova 42, Olomouc", "Spedice",
            null, [("VYR-006", 2, 0), ("VYR-007", 3, 0)]),
        new("ZAK-90005", "Z90005", D(9, 18), D(10, 30), null, OrderStatus.Confirmed, "Zahradní 7, Zlín", "Vlastní doprava",
            null, [("VYR-010", 1, 0), ("VYR-003", 6, 0)]),
        new("ZAK-90006", "Z90006", D(9, 22), D(11, 5), null, OrderStatus.New, "Lipová 3, Plzeň", "Spedice",
            null, [("VYR-005", 2, 0), ("VYR-004", 2, 0)]),
        new("ZAK-90007", "Z90007", D(9, 29), D(11, 15), null, OrderStatus.New, "Náměstí 1, České Budějovice", "Spedice",
            "Doručit před otevírací dobou.", [("VYR-010", 6, 8), ("VYR-003", 24, 8), ("VYR-002", 1, 0)]),
        new("ZAK-90008", "Z90008", D(10, 2), D(11, 20), null, OrderStatus.Confirmed, "Cejl 25, Brno", "Vlastní doprava",
            null, [("VYR-001", 8, 0), ("VYR-003", 8, 0), ("VYR-005", 4, 0)]),
        new("ZAK-90009", "Z90009", D(10, 5), D(10, 25), null, OrderStatus.Cancelled, "Horní 14, Liberec", "Spedice",
            "Zrušeno zákazníkem.", [("VYR-008", 1, 0), ("VYR-009", 2, 0)]),
        new("ZAK-90010", "Z90010", D(10, 8), D(12, 1), null, OrderStatus.New, "Průmyslová 90, Ostrava", "Spedice",
            null, [("VYR-007", 5, 3), ("VYR-006", 1, 0)])
    ];

    // ------------------------------------------------------------------ seed

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var added = 0;
        added += await SeedCustomersAsync(db, ct);
        added += await SeedMaterialsAsync(db, ct);
        added += await SeedOperationsAsync(db, ct);
        added += await SeedProductsAsync(db, ct);
        added += await SeedRoutingAsync(db, ct);
        added += await SeedOrdersAsync(db, ct);

        logger.LogInformation("Demo data: {Count} records written (0 means everything was already present).", added);
    }

    private static async Task<int> SeedCustomersAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = (await db.Customers.Select(c => c.CustomerNumber).ToListAsync(ct)).ToHashSet();

        foreach (var c in Customers.Where(c => !existing.Contains(c.No)))
        {
            _ = db.Customers.Add(new Customer
            {
                CustomerNumber = c.No,
                Type = c.Type,
                Name = c.Name,
                CompanyId = c.Ico,
                VatId = c.Ico is null ? null : "CZ" + c.Ico,
                ContactPerson = c.Contact,
                Email = c.Email,
                Phone = c.Phone,
                Street = c.Street,
                City = c.City,
                PostalCode = c.Zip,
                Country = Country,
                IsActive = true
            });
        }
        return await db.SaveChangesAsync(ct);
    }

    private static async Task<int> SeedMaterialsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = (await db.Materials.Select(m => m.Code).ToListAsync(ct)).ToHashSet();

        foreach (var m in Materials.Where(m => !existing.Contains(m.Code)))
        {
            _ = db.Materials.Add(new Material
            {
                Code = m.Code,
                Name = m.Name,
                Type = m.Type,
                Unit = m.Unit,
                PurchasePrice = m.Price,
                Supplier = m.Supplier,
                ColorOrDecor = m.Color,
                MinOrderQuantity = 1m,
                IsActive = true
            });
        }
        return await db.SaveChangesAsync(ct);
    }

    private static async Task<int> SeedOperationsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = (await db.Operations.Select(o => o.Code).ToListAsync(ct)).ToHashSet();

        foreach (var o in Operations.Where(o => !existing.Contains(o.Code)))
        {
            _ = db.Operations.Add(new Operation
            {
                Code = o.Code,
                Name = o.Name,
                Workplace = o.Workplace,
                OperationType = o.Type,
                DefaultSetupMinutes = o.Setup,
                DefaultRunMinutes = (int)Math.Ceiling(o.Run),
                HourlyRate = o.Rate,
                RequiredQualification = o.Qualification,
                IsActive = true
            });
        }
        return await db.SaveChangesAsync(ct);
    }

    private static async Task<int> SeedProductsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = (await db.Products.Select(p => p.Code).ToListAsync(ct)).ToHashSet();

        foreach (var p in Products.Where(p => !existing.Contains(p.Code)))
        {
            _ = db.Products.Add(new Product
            {
                Code = p.Code,
                Name = p.Name,
                ProductGroup = p.Group,
                ProductType = p.Type,
                Unit = "ks",
                SalesPrice = p.Price,
                Width = p.W,
                Height = p.H,
                Depth = p.D,
                Weight = p.Weight,
                DeliveryDays = p.Days,
                IsActive = true
            });
        }
        return await db.SaveChangesAsync(ct);
    }

    private static async Task<int> SeedRoutingAsync(AppDbContext db, CancellationToken ct)
    {
        var products = await db.Products.ToDictionaryAsync(p => p.Code, p => p.Id, ct);
        var materials = await db.Materials.ToDictionaryAsync(m => m.Code, ct);
        var operations = await db.Operations.ToDictionaryAsync(o => o.Code, ct);

        // postup se doplní jen výrobkům, které ještě žádný materiál / operaci nemají
        var withMaterials = (await db.ProductMaterials.Select(x => x.ProductId).Distinct().ToListAsync(ct)).ToHashSet();
        var withOperations = (await db.ProductOperations.Select(x => x.ProductId).Distinct().ToListAsync(ct)).ToHashSet();

        foreach (var group in ProductMaterials.GroupBy(x => x.Product))
        {
            if (!products.TryGetValue(group.Key, out var productId) || withMaterials.Contains(productId)) continue;

            var sequence = 1;
            foreach (var x in group.Where(x => materials.ContainsKey(x.Material)))
            {
                var material = materials[x.Material];
                _ = db.ProductMaterials.Add(new ProductMaterial
                {
                    ProductId = productId,
                    MaterialId = material.Id,
                    Sequence = sequence++,
                    Quantity = x.Qty,
                    Unit = material.Unit,
                    WastePercent = x.Waste
                });
            }
        }

        foreach (var group in ProductOperations.GroupBy(x => x.Product))
        {
            if (!products.TryGetValue(group.Key, out var productId) || withOperations.Contains(productId)) continue;

            var sequence = 1;
            foreach (var x in group.Where(x => operations.ContainsKey(x.Operation)))
            {
                var operation = operations[x.Operation];
                _ = db.ProductOperations.Add(new ProductOperation
                {
                    ProductId = productId,
                    OperationId = operation.Id,
                    Sequence = sequence++,
                    Workplace = operation.Workplace,
                    SetupMinutes = x.Setup,
                    RunMinutesPerUnit = x.Run,
                    WorkInstructions = x.Instructions
                });
            }
        }
        return await db.SaveChangesAsync(ct);
    }

    private static async Task<int> SeedOrdersAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = (await db.Orders.Select(o => o.OrderNumber).ToListAsync(ct)).ToHashSet();
        var customerIds = await db.Customers.ToDictionaryAsync(c => c.CustomerNumber, c => c.Id, ct);
        var products = await db.Products.ToDictionaryAsync(p => p.Code, ct);
        var now = DateTime.UtcNow;

        foreach (var o in Orders.Where(o => !existing.Contains(o.No)))
        {
            if (!customerIds.TryGetValue(o.Customer, out var customerId)) continue;
            if (o.Items.Any(i => !products.ContainsKey(i.Product))) continue;

            var customer = Customers.First(c => c.No == o.Customer);
            var order = new Order
            {
                OrderNumber = o.No,
                CustomerId = customerId,
                CreatedDate = o.Created,
                RequiredDeliveryDate = o.Required,
                ActualCompletionDate = o.Completed,
                Status = o.Status,
                ContactPerson = customer.Contact,
                Email = customer.Email,
                Phone = customer.Phone,
                DeliveryAddress = o.Address,
                ShippingMethod = o.Shipping,
                Note = o.Note,
                LastModifiedAt = now
            };

            var sequence = 1;
            foreach (var (code, qty, discount) in o.Items)
            {
                var product = products[code];
                order.Items.Add(new OrderItem
                {
                    Sequence = sequence++,
                    ProductId = product.Id,
                    Quantity = qty,
                    Unit = product.Unit,
                    UnitPrice = product.SalesPrice,
                    DiscountPercent = discount,
                    TotalPrice = OrderPricing.ItemTotal(qty, product.SalesPrice, discount)
                });
            }

            order.TotalPrice = OrderPricing.OrderTotal(order.Items.Select(i => i.TotalPrice));
            _ = db.Orders.Add(order);
        }
        return await db.SaveChangesAsync(ct);
    }
}