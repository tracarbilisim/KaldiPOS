using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace KaldiPOS.Data;

public static class Database
{
    private static readonly string DatabaseFolder =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "KaldiPOS");

    private static readonly string DatabasePath =
        Path.Combine(DatabaseFolder, "KaldiPOS.db");

    private static readonly string ConnectionString =
        $"Data Source={DatabasePath}";
    private const string MenuVersionKey = "KaldiMenuVersion";

    public static void Initialize()
    {
        Directory.CreateDirectory(DatabaseFolder);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    Station TEXT NOT NULL DEFAULT 'Mutfak'
);

CREATE TABLE IF NOT EXISTS Products
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    ImagePath TEXT,
    Description TEXT,
    ExternalId TEXT,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE IF NOT EXISTS Tables
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Hall TEXT NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Orders
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TableId INTEGER NOT NULL,
    OpenedAt TEXT NOT NULL,
    BusinessDate TEXT NOT NULL,
    ClosedAt TEXT,
    Status INTEGER NOT NULL DEFAULT 0,
    PaymentType TEXT,
    TotalAmount REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS OrderItems
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    SentQuantity INTEGER NOT NULL DEFAULT 0,
    Note TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS OrderPayments
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    PaymentType TEXT NOT NULL,
    Amount REAL NOT NULL,
    Description TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE TABLE IF NOT EXISTS CancelledOrderItems
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    TableName TEXT NOT NULL,
    ProductId INTEGER NOT NULL,
    ProductName TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    TotalAmount REAL NOT NULL,
    CancelReason TEXT NOT NULL,
    CancelledBy TEXT NOT NULL,
    CancelledAt TEXT NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE INDEX IF NOT EXISTS IX_CancelledOrderItems_OrderId
ON CancelledOrderItems(OrderId);

CREATE TABLE IF NOT EXISTS DayEndClosures
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BusinessDate TEXT NOT NULL UNIQUE,
    ClosedAt TEXT NOT NULL,
    OrderCount INTEGER NOT NULL,
    TotalRevenue REAL NOT NULL,
    CashTotal REAL NOT NULL,
    CardTotal REAL NOT NULL,
    MixedTotal REAL NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DayEndClosures_BusinessDate
ON DayEndClosures(BusinessDate);

CREATE INDEX IF NOT EXISTS IX_OrderPayments_OrderId
ON OrderPayments(OrderId);


CREATE TABLE IF NOT EXISTS Users
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    PinHash TEXT NOT NULL UNIQUE,
    Role TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Users_IsActive
ON Users(IsActive);

CREATE TABLE IF NOT EXISTS Permissions
(
    PermissionKey TEXT PRIMARY KEY,
    PermissionName TEXT NOT NULL,
    Category TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS UserPermissions
(
    UserId INTEGER NOT NULL,
    PermissionKey TEXT NOT NULL,
    IsAllowed INTEGER NOT NULL DEFAULT 0,

    PRIMARY KEY(UserId, PermissionKey),

    FOREIGN KEY(UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE,

    FOREIGN KEY(PermissionKey)
        REFERENCES Permissions(PermissionKey)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_UserPermissions_UserId
ON UserPermissions(UserId);

CREATE INDEX IF NOT EXISTS IX_UserPermissions_PermissionKey
ON UserPermissions(PermissionKey);

CREATE TABLE IF NOT EXISTS AppMetadata
(
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);";
        command.ExecuteNonQuery();

        EnsureColumn(connection, "Categories", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Categories", "Station", "TEXT NOT NULL DEFAULT 'Mutfak'");
        EnsureColumn(connection, "Products", "ImagePath", "TEXT");
        EnsureColumn(connection, "Products", "Description", "TEXT");
        EnsureColumn(connection, "Products", "ExternalId", "TEXT");
        EnsureColumn(connection, "Products", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Orders", "BusinessDate", "TEXT");
        EnsureColumn(connection, "Orders", "PaymentType", "TEXT");
        EnsureColumn(connection, "Orders", "TotalAmount", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Orders", "IsCancelled", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Orders", "CancelledAt", "TEXT");
        EnsureColumn(connection, "Orders", "CancelledBy", "TEXT");
        EnsureColumn(connection, "Orders", "CancelReason", "TEXT");
        EnsureColumn(connection, "OrderItems", "SentQuantity", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "OrderItems", "Note", "TEXT NOT NULL DEFAULT ''");

        BackfillOrderBusinessDates(connection);
        EnsureActiveBusinessDate(connection);
        SeedDefaultAdmin(connection);
        SeedPermissions(connection);

        SeedTables(connection);
        ImportKaldiMenu(connection);
    }

    public static List<string> GetCategories()
    {
        var categories = new List<string>();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM Categories ORDER BY SortOrder, Id;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            categories.Add(reader.GetString(0));

        return categories;
    }

    public static string GetCategoryStation(string categoryName)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT Station
        FROM Categories
        WHERE Name = @Name
        LIMIT 1;";

        command.Parameters.AddWithValue("@Name", categoryName);

        object? result = command.ExecuteScalar();

        if (result is null || result == DBNull.Value)
            return "Mutfak";

        return result.ToString() ?? "Mutfak";
    }

    public static List<ProductRecord> GetProducts()
    {
        var products = new List<ProductRecord>();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id, p.Name, c.Name, p.Price, COALESCE(p.ImagePath, '')
FROM Products p
INNER JOIN Categories c ON c.Id = p.CategoryId
WHERE p.IsActive = 1
ORDER BY c.SortOrder, c.Id, p.SortOrder, p.Id;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(new ProductRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToDecimal(reader.GetDouble(3), CultureInfo.InvariantCulture),
                reader.GetString(4)));
        }

        return products;
    }


    public static void SetProductActive(
        int productId,
        bool isActive)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Products
SET IsActive = $isActive
WHERE Id = $productId;";

        command.Parameters.AddWithValue(
            "$isActive",
            isActive ? 1 : 0);
        command.Parameters.AddWithValue(
            "$productId",
            productId);

        int affectedRows = command.ExecuteNonQuery();

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Ürün bulunamadı veya durumu değiştirilemedi.");
        }
    }

    public static void UpdateProduct(
        int productId,
        string name,
        string categoryName,
        decimal price,
        string imagePath)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));

        name = name.Trim();
        categoryName = categoryName.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ürün adı boş bırakılamaz.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException(
                "Kategori seçilmelidir.",
                nameof(categoryName));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Ürün fiyatı negatif olamaz.");

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Products
SET Name = $name,
    CategoryId =
    (
        SELECT Id
        FROM Categories
        WHERE Name = $categoryName
        LIMIT 1
    ),
    Price = $price,
    ImagePath = $imagePath
WHERE Id = $productId
  AND EXISTS
  (
      SELECT 1
      FROM Categories
      WHERE Name = $categoryName
  );";

        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue(
            "$categoryName",
            categoryName);
        command.Parameters.AddWithValue("$price", price);
        command.Parameters.AddWithValue("$imagePath", imagePath ?? string.Empty);
        command.Parameters.AddWithValue(
            "$productId",
            productId);

        int affectedRows = command.ExecuteNonQuery();

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Ürün güncellenemedi. Seçilen kategori bulunamadı.");
        }
    }

    public static int AddProduct(
        string name,
        string categoryName,
        decimal price,
        string imagePath)
    {
        name = name.Trim();
        categoryName = categoryName.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ürün adı boş bırakılamaz.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException(
                "Kategori seçilmelidir.",
                nameof(categoryName));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Ürün fiyatı negatif olamaz.");

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO Products
(
    CategoryId,
    Name,
    Price,
    ImagePath,
    SortOrder,
    IsActive
)
SELECT
    Id,
    $name,
    $price,
    $imagePath,
    COALESCE
    (
        (
            SELECT MAX(SortOrder) + 1
            FROM Products
            WHERE CategoryId = Categories.Id
        ),
        0
    ),
    1
FROM Categories
WHERE Name = $categoryName;";

        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue(
            "$categoryName",
            categoryName);
        command.Parameters.AddWithValue("$price", price);
        command.Parameters.AddWithValue("$imagePath", imagePath ?? string.Empty);

        int affectedRows = command.ExecuteNonQuery();

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Ürün eklenemedi. Seçilen kategori bulunamadı.");
        }

        using var idCommand = connection.CreateCommand();
        idCommand.Transaction = transaction;
        idCommand.CommandText =
            "SELECT last_insert_rowid();";

        int productId = Convert.ToInt32(
            (long)(idCommand.ExecuteScalar() ?? 0L));

        transaction.Commit();
        return productId;
    }

    public static List<TableRecord> GetTables(string hall)
    {
        var tables = new List<TableRecord>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, Name, Hall, Status
FROM Tables
WHERE Hall = $hall
ORDER BY Id;";
        command.Parameters.AddWithValue("$hall", hall);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(new TableRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return tables;
    }

    public static List<SavedOrderItem> LoadOpenOrder(string tableName)
    {
        var items = new List<SavedOrderItem>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT oi.ProductId, p.Name, oi.Quantity, oi.UnitPrice, oi.SentQuantity, oi.Note
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
INNER JOIN OrderItems oi ON oi.OrderId = o.Id
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE t.Name = $tableName
  AND o.Status = 0
  AND o.ClosedAt IS NULL
ORDER BY oi.Id;";

        command.Parameters.AddWithValue("$tableName", tableName);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            items.Add(new SavedOrderItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                Convert.ToDecimal(reader.GetDouble(3), CultureInfo.InvariantCulture),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5)));
        }

        return items;
    }

    public static void SaveOpenOrder(
        string tableName,
        IEnumerable<SavedOrderItem> items)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        long tableId;

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText =
                "SELECT Id FROM Tables WHERE Name = $tableName LIMIT 1;";
            tableCommand.Parameters.AddWithValue("$tableName", tableName);

            tableId = Convert.ToInt64(
                tableCommand.ExecuteScalar()
                ?? throw new InvalidOperationException("Masa bulunamadı."));
        }

        long orderId;

        using (var orderCommand = connection.CreateCommand())
        {
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = @"
SELECT Id
FROM Orders
WHERE TableId = $tableId
  AND Status = 0
  AND ClosedAt IS NULL
LIMIT 1;";

            orderCommand.Parameters.AddWithValue("$tableId", tableId);
            object? existingOrderId = orderCommand.ExecuteScalar();

            if (existingOrderId is not null)
            {
                orderId = Convert.ToInt64(existingOrderId);
            }
            else
            {
                orderCommand.CommandText = @"
INSERT INTO Orders
(
    TableId,
    OpenedAt,
    BusinessDate,
    ClosedAt,
    Status
)
VALUES
(
    $tableId,
    $openedAt,
    $businessDate,
    NULL,
    0
);
SELECT last_insert_rowid();";

                orderCommand.Parameters.Clear();
                orderCommand.Parameters.AddWithValue("$tableId", tableId);
                orderCommand.Parameters.AddWithValue(
                    "$openedAt",
                    DateTime.Now.ToString("O"));

                orderCommand.Parameters.AddWithValue(
    "$businessDate",
    GetActiveBusinessDate(connection).ToString("yyyy-MM-dd"));

                orderId = Convert.ToInt64(orderCommand.ExecuteScalar());
            }
        }

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText =
                "DELETE FROM OrderItems WHERE OrderId = $orderId;";
            deleteCommand.Parameters.AddWithValue("$orderId", orderId);
            deleteCommand.ExecuteNonQuery();
        }

        foreach (SavedOrderItem item in items)
        {
            using var itemCommand = connection.CreateCommand();
            itemCommand.Transaction = transaction;
            itemCommand.CommandText = @"
INSERT INTO OrderItems
(OrderId, ProductId, Quantity, UnitPrice, SentQuantity, Note)
VALUES
($orderId, $productId, $quantity, $unitPrice, $sentQuantity, $note);";

            itemCommand.Parameters.AddWithValue("$orderId", orderId);
            itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
            itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
            itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
            itemCommand.Parameters.AddWithValue(
                "$sentQuantity",
                Math.Min(item.SentQuantity, item.Quantity));
            itemCommand.Parameters.AddWithValue("$note", item.Note ?? string.Empty);
            itemCommand.ExecuteNonQuery();
        }

        using (var statusCommand = connection.CreateCommand())
        {
            statusCommand.Transaction = transaction;
            statusCommand.CommandText =
                "UPDATE Tables SET Status = 1 WHERE Id = $tableId;";
            statusCommand.Parameters.AddWithValue("$tableId", tableId);
            statusCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void TransferOpenOrder(
    string sourceTableName,
    string targetTableName)
    {
        if (string.IsNullOrWhiteSpace(sourceTableName) ||
            string.IsNullOrWhiteSpace(targetTableName))
        {
            throw new ArgumentException("Masa adı boş olamaz.");
        }

        if (string.Equals(
            sourceTableName,
            targetTableName,
            StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException(
                "Adisyon aynı masaya aktarılamaz.");
        }

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction =
            connection.BeginTransaction();

        long sourceTableId;
        long targetTableId;

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText = @"
SELECT Id
FROM Tables
WHERE Name = $tableName
LIMIT 1;";

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                sourceTableName);

            sourceTableId = Convert.ToInt64(
                tableCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Kaynak masa bulunamadı."));

            tableCommand.Parameters.Clear();
            tableCommand.Parameters.AddWithValue(
                "$tableName",
                targetTableName);

            targetTableId = Convert.ToInt64(
                tableCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Hedef masa bulunamadı."));
        }

        long orderId;

        using (var sourceOrderCommand = connection.CreateCommand())
        {
            sourceOrderCommand.Transaction = transaction;
            sourceOrderCommand.CommandText = @"
SELECT Id
FROM Orders
WHERE TableId = $tableId
  AND Status = 0
  AND ClosedAt IS NULL
LIMIT 1;";

            sourceOrderCommand.Parameters.AddWithValue(
                "$tableId",
                sourceTableId);

            orderId = Convert.ToInt64(
                sourceOrderCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Kaynak masada açık adisyon bulunamadı."));
        }

        using (var targetOrderCommand = connection.CreateCommand())
        {
            targetOrderCommand.Transaction = transaction;
            targetOrderCommand.CommandText = @"
SELECT COUNT(*)
FROM Orders
WHERE TableId = $tableId
  AND Status = 0
  AND ClosedAt IS NULL;";

            targetOrderCommand.Parameters.AddWithValue(
                "$tableId",
                targetTableId);

            long targetOrderCount = Convert.ToInt64(
                targetOrderCommand.ExecuteScalar() ?? 0L);

            if (targetOrderCount > 0)
            {
                throw new InvalidOperationException(
                    "Hedef masada açık bir adisyon bulunuyor.");
            }
        }

        using (var transferCommand = connection.CreateCommand())
        {
            transferCommand.Transaction = transaction;
            transferCommand.CommandText = @"
UPDATE Orders
SET TableId = $targetTableId
WHERE Id = $orderId;";

            transferCommand.Parameters.AddWithValue(
                "$targetTableId",
                targetTableId);

            transferCommand.Parameters.AddWithValue(
                "$orderId",
                orderId);

            transferCommand.ExecuteNonQuery();
        }

        using (var statusCommand = connection.CreateCommand())
        {
            statusCommand.Transaction = transaction;
            statusCommand.CommandText = @"
UPDATE Tables
SET Status =
    CASE
        WHEN Id = $sourceTableId THEN 0
        WHEN Id = $targetTableId THEN 1
        ELSE Status
    END
WHERE Id IN ($sourceTableId, $targetTableId);";

            statusCommand.Parameters.AddWithValue(
                "$sourceTableId",
                sourceTableId);

            statusCommand.Parameters.AddWithValue(
                "$targetTableId",
                targetTableId);

            statusCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void TransferProducts(
    string sourceTableName,
    string targetTableName,
    IEnumerable<SavedOrderItem> transferredItems)
    {
        if (string.IsNullOrWhiteSpace(sourceTableName) ||
            string.IsNullOrWhiteSpace(targetTableName))
        {
            throw new ArgumentException("Masa adı boş olamaz.");
        }

        if (string.Equals(
                sourceTableName,
                targetTableName,
                StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException(
                "Ürünler aynı masaya aktarılamaz.");
        }

        List<SavedOrderItem> items = transferredItems
            .Where(item => item.Quantity > 0)
            .ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "Aktarılacak ürün bulunamadı.");
        }

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction =
            connection.BeginTransaction();

        long sourceTableId;
        long targetTableId;

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText = @"
SELECT Id
FROM Tables
WHERE Name = $tableName
LIMIT 1;";

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                sourceTableName);

            sourceTableId = Convert.ToInt64(
                tableCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Kaynak masa bulunamadı."));

            tableCommand.Parameters.Clear();

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                targetTableName);

            targetTableId = Convert.ToInt64(
                tableCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Hedef masa bulunamadı."));
        }

        long sourceOrderId;

        using (var sourceOrderCommand =
               connection.CreateCommand())
        {
            sourceOrderCommand.Transaction = transaction;
            sourceOrderCommand.CommandText = @"
SELECT Id
FROM Orders
WHERE TableId = $tableId
  AND Status = 0
  AND ClosedAt IS NULL
LIMIT 1;";

            sourceOrderCommand.Parameters.AddWithValue(
                "$tableId",
                sourceTableId);

            sourceOrderId = Convert.ToInt64(
                sourceOrderCommand.ExecuteScalar()
                ?? throw new InvalidOperationException(
                    "Kaynak masada açık adisyon bulunamadı."));
        }

        long targetOrderId;

        using (var targetOrderCommand =
               connection.CreateCommand())
        {
            targetOrderCommand.Transaction = transaction;
            targetOrderCommand.CommandText = @"
SELECT Id
FROM Orders
WHERE TableId = $tableId
  AND Status = 0
  AND ClosedAt IS NULL
LIMIT 1;";

            targetOrderCommand.Parameters.AddWithValue(
                "$tableId",
                targetTableId);

            object? existingTargetOrderId =
                targetOrderCommand.ExecuteScalar();

            if (existingTargetOrderId is not null)
            {
                targetOrderId =
                    Convert.ToInt64(existingTargetOrderId);
            }
            else
            {
                targetOrderCommand.CommandText = @"
INSERT INTO Orders
(
    TableId,
    OpenedAt,
    BusinessDate,
    ClosedAt,
    Status
)
VALUES
(
    $tableId,
    $openedAt,
    $businessDate,
    NULL,
    0
);
SELECT last_insert_rowid();";

                targetOrderCommand.Parameters.Clear();

                targetOrderCommand.Parameters.AddWithValue(
                    "$tableId",
                    targetTableId);

                targetOrderCommand.Parameters.AddWithValue(
                    "$openedAt",
                    DateTime.Now.ToString("O"));

                targetOrderCommand.Parameters.AddWithValue(
                    "$businessDate",
                    GetActiveBusinessDate(connection)
                        .ToString("yyyy-MM-dd"));

                targetOrderId = Convert.ToInt64(
                    targetOrderCommand.ExecuteScalar());
            }
        }

        foreach (SavedOrderItem transferItem in items)
        {
            long sourceItemId;
            int sourceQuantity;
            int sourceSentQuantity;
            decimal sourceUnitPrice;
            string sourceNote;

            using (var sourceItemCommand =
                   connection.CreateCommand())
            {
                sourceItemCommand.Transaction = transaction;
                sourceItemCommand.CommandText = @"
SELECT
    Id,
    Quantity,
    SentQuantity,
    UnitPrice,
    Note
FROM OrderItems
WHERE OrderId = $orderId
  AND ProductId = $productId
LIMIT 1;";

                sourceItemCommand.Parameters.AddWithValue(
                    "$orderId",
                    sourceOrderId);

                sourceItemCommand.Parameters.AddWithValue(
                    "$productId",
                    transferItem.ProductId);

                using var reader =
                    sourceItemCommand.ExecuteReader();

                if (!reader.Read())
                {
                    throw new InvalidOperationException(
                        $"{transferItem.Name} kaynak adisyonda bulunamadı.");
                }

                sourceItemId = reader.GetInt64(0);
                sourceQuantity = reader.GetInt32(1);
                sourceSentQuantity = reader.GetInt32(2);

                sourceUnitPrice = Convert.ToDecimal(
                    reader.GetDouble(3),
                    CultureInfo.InvariantCulture);

                sourceNote = reader.IsDBNull(4)
                    ? string.Empty
                    : reader.GetString(4);
            }

            if (transferItem.Quantity > sourceQuantity)
            {
                throw new InvalidOperationException(
                    $"{transferItem.Name} için aktarılmak istenen " +
                    "miktar mevcut miktardan fazla.");
            }

            int transferredQuantity =
                transferItem.Quantity;

            int transferredSentQuantity =
                Math.Min(
                    sourceSentQuantity,
                    transferredQuantity);

            int remainingQuantity =
                sourceQuantity - transferredQuantity;

            int remainingSentQuantity =
                Math.Max(
                    0,
                    sourceSentQuantity -
                    transferredSentQuantity);

            if (remainingQuantity == 0)
            {
                using var deleteSourceItemCommand =
                    connection.CreateCommand();

                deleteSourceItemCommand.Transaction =
                    transaction;

                deleteSourceItemCommand.CommandText = @"
DELETE FROM OrderItems
WHERE Id = $itemId;";

                deleteSourceItemCommand.Parameters.AddWithValue(
                    "$itemId",
                    sourceItemId);

                deleteSourceItemCommand.ExecuteNonQuery();
            }
            else
            {
                using var updateSourceItemCommand =
                    connection.CreateCommand();

                updateSourceItemCommand.Transaction =
                    transaction;

                updateSourceItemCommand.CommandText = @"
UPDATE OrderItems
SET Quantity = $quantity,
    SentQuantity = $sentQuantity
WHERE Id = $itemId;";

                updateSourceItemCommand.Parameters.AddWithValue(
                    "$quantity",
                    remainingQuantity);

                updateSourceItemCommand.Parameters.AddWithValue(
                    "$sentQuantity",
                    Math.Min(
                        remainingSentQuantity,
                        remainingQuantity));

                updateSourceItemCommand.Parameters.AddWithValue(
                    "$itemId",
                    sourceItemId);

                updateSourceItemCommand.ExecuteNonQuery();
            }

            long? targetItemId = null;
            int targetQuantity = 0;
            int targetSentQuantity = 0;

            using (var targetItemCommand =
                   connection.CreateCommand())
            {
                targetItemCommand.Transaction = transaction;
                targetItemCommand.CommandText = @"
SELECT
    Id,
    Quantity,
    SentQuantity
FROM OrderItems
WHERE OrderId = $orderId
  AND ProductId = $productId
  AND UnitPrice = $unitPrice
  AND Note = $note
LIMIT 1;";

                targetItemCommand.Parameters.AddWithValue(
                    "$orderId",
                    targetOrderId);

                targetItemCommand.Parameters.AddWithValue(
                    "$productId",
                    transferItem.ProductId);

                targetItemCommand.Parameters.AddWithValue(
                    "$unitPrice",
                    sourceUnitPrice);

                targetItemCommand.Parameters.AddWithValue(
                    "$note",
                    sourceNote);

                using var reader =
                    targetItemCommand.ExecuteReader();

                if (reader.Read())
                {
                    targetItemId = reader.GetInt64(0);
                    targetQuantity = reader.GetInt32(1);
                    targetSentQuantity = reader.GetInt32(2);
                }
            }

            if (targetItemId.HasValue)
            {
                int newTargetQuantity =
                    targetQuantity +
                    transferredQuantity;

                int newTargetSentQuantity =
                    Math.Min(
                        newTargetQuantity,
                        targetSentQuantity +
                        transferredSentQuantity);

                using var updateTargetItemCommand =
                    connection.CreateCommand();

                updateTargetItemCommand.Transaction =
                    transaction;

                updateTargetItemCommand.CommandText = @"
UPDATE OrderItems
SET Quantity = $quantity,
    SentQuantity = $sentQuantity
WHERE Id = $itemId;";

                updateTargetItemCommand.Parameters.AddWithValue(
                    "$quantity",
                    newTargetQuantity);

                updateTargetItemCommand.Parameters.AddWithValue(
                    "$sentQuantity",
                    newTargetSentQuantity);

                updateTargetItemCommand.Parameters.AddWithValue(
                    "$itemId",
                    targetItemId.Value);

                updateTargetItemCommand.ExecuteNonQuery();
            }
            else
            {
                using var insertTargetItemCommand =
                    connection.CreateCommand();

                insertTargetItemCommand.Transaction =
                    transaction;

                insertTargetItemCommand.CommandText = @"
INSERT INTO OrderItems
(
    OrderId,
    ProductId,
    Quantity,
    UnitPrice,
    SentQuantity,
    Note
)
VALUES
(
    $orderId,
    $productId,
    $quantity,
    $unitPrice,
    $sentQuantity,
    $note
);";

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$orderId",
                    targetOrderId);

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$productId",
                    transferItem.ProductId);

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$quantity",
                    transferredQuantity);

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$unitPrice",
                    sourceUnitPrice);

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$sentQuantity",
                    transferredSentQuantity);

                insertTargetItemCommand.Parameters.AddWithValue(
                    "$note",
                    sourceNote);

                insertTargetItemCommand.ExecuteNonQuery();
            }
        }

        long remainingItemCount;

        using (var countCommand = connection.CreateCommand())
        {
            countCommand.Transaction = transaction;
            countCommand.CommandText = @"
SELECT COUNT(*)
FROM OrderItems
WHERE OrderId = $orderId;";

            countCommand.Parameters.AddWithValue(
                "$orderId",
                sourceOrderId);

            remainingItemCount = Convert.ToInt64(
                countCommand.ExecuteScalar() ?? 0L);
        }

        if (remainingItemCount == 0)
        {
            using var deleteSourceOrderCommand =
                connection.CreateCommand();

            deleteSourceOrderCommand.Transaction =
                transaction;

            deleteSourceOrderCommand.CommandText = @"
DELETE FROM Orders
WHERE Id = $orderId;";

            deleteSourceOrderCommand.Parameters.AddWithValue(
                "$orderId",
                sourceOrderId);

            deleteSourceOrderCommand.ExecuteNonQuery();
        }

        using (var statusCommand = connection.CreateCommand())
        {
            statusCommand.Transaction = transaction;
            statusCommand.CommandText = @"
UPDATE Tables
SET Status =
    CASE
        WHEN Id = $sourceTableId
            THEN $sourceStatus
        WHEN Id = $targetTableId
            THEN 1
        ELSE Status
    END
WHERE Id IN
(
    $sourceTableId,
    $targetTableId
);";

            statusCommand.Parameters.AddWithValue(
                "$sourceTableId",
                sourceTableId);

            statusCommand.Parameters.AddWithValue(
                "$targetTableId",
                targetTableId);

            statusCommand.Parameters.AddWithValue(
                "$sourceStatus",
                remainingItemCount > 0 ? 1 : 0);

            statusCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void RecordProductCancellation(
    string tableName,
    int productId,
    string productName,
    int quantity,
    decimal unitPrice,
    string cancelReason,
    string cancelledBy)
    {
        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
INSERT INTO CancelledOrderItems
(
    OrderId,
    TableName,
    ProductId,
    ProductName,
    Quantity,
    UnitPrice,
    TotalAmount,
    CancelReason,
    CancelledBy,
    CancelledAt
)
SELECT
    o.Id,
    $tableName,
    $productId,
    $productName,
    $quantity,
    $unitPrice,
    $totalAmount,
    $cancelReason,
    $cancelledBy,
    $cancelledAt
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE t.Name = $tableName
  AND o.Status = 0
  AND o.ClosedAt IS NULL
ORDER BY o.Id DESC
LIMIT 1;";

        command.Parameters.AddWithValue(
            "$tableName",
            tableName);

        command.Parameters.AddWithValue(
            "$productId",
            productId);

        command.Parameters.AddWithValue(
            "$productName",
            productName);

        command.Parameters.AddWithValue(
            "$quantity",
            quantity);

        command.Parameters.AddWithValue(
            "$unitPrice",
            unitPrice);

        command.Parameters.AddWithValue(
            "$totalAmount",
            unitPrice * quantity);

        command.Parameters.AddWithValue(
            "$cancelReason",
            cancelReason.Trim());

        command.Parameters.AddWithValue(
            "$cancelledBy",
            cancelledBy.Trim());

        command.Parameters.AddWithValue(
            "$cancelledAt",
            DateTime.Now.ToString("O"));

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException(
                "Açık adisyon bulunamadığı için ürün iptali kaydedilemedi.");
        }
    }

    public static void CancelOpenOrder(
    string tableName,
    string cancelReason,
    string cancelledBy)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
UPDATE Orders
SET
    Status = 1,
    ClosedAt = $cancelledAt,
    IsCancelled = 1,
    CancelledAt = $cancelledAt,
    CancelledBy = $cancelledBy,
    CancelReason = $cancelReason,
PaymentType = NULL,
TotalAmount =
(
    SELECT COALESCE(SUM(oi.Quantity * oi.UnitPrice), 0)
    FROM OrderItems oi
    WHERE oi.OrderId = Orders.Id
)
WHERE Id =
(
    SELECT o.Id
    FROM Orders o
    INNER JOIN Tables t ON t.Id = o.TableId
    WHERE t.Name = $tableName
      AND o.Status = 0
      AND o.ClosedAt IS NULL
    ORDER BY o.Id DESC
    LIMIT 1
);";

            command.Parameters.AddWithValue(
                "$cancelledAt",
                DateTime.Now.ToString("O"));

            command.Parameters.AddWithValue(
                "$cancelledBy",
                cancelledBy.Trim());

            command.Parameters.AddWithValue(
                "$cancelReason",
                cancelReason.Trim());

            command.Parameters.AddWithValue(
                "$tableName",
                tableName);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException(
                    "İptal edilecek açık adisyon bulunamadı.");
            }
        }

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText = @"
UPDATE Tables
SET Status = 0
WHERE Name = $tableName;";

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                tableName);

            tableCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void DeleteOpenOrder(string tableName)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var deleteItemsCommand = connection.CreateCommand())
        {
            deleteItemsCommand.Transaction = transaction;
            deleteItemsCommand.CommandText = @"
DELETE FROM OrderItems
WHERE OrderId IN
(
    SELECT o.Id
    FROM Orders o
    INNER JOIN Tables t ON t.Id = o.TableId
    WHERE t.Name = $tableName
      AND o.Status = 0
      AND o.ClosedAt IS NULL
);";

            deleteItemsCommand.Parameters.AddWithValue("$tableName", tableName);
            deleteItemsCommand.ExecuteNonQuery();
        }

        using (var deleteOrderCommand = connection.CreateCommand())
        {
            deleteOrderCommand.Transaction = transaction;
            deleteOrderCommand.CommandText = @"
DELETE FROM Orders
WHERE Id IN
(
    SELECT o.Id
    FROM Orders o
    INNER JOIN Tables t ON t.Id = o.TableId
    WHERE t.Name = $tableName
      AND o.Status = 0
      AND o.ClosedAt IS NULL
);";

            deleteOrderCommand.Parameters.AddWithValue("$tableName", tableName);
            deleteOrderCommand.ExecuteNonQuery();
        }

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText =
                "UPDATE Tables SET Status = 0 WHERE Name = $tableName;";
            tableCommand.Parameters.AddWithValue("$tableName", tableName);
            tableCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static decimal GetOpenOrderPaidTotal(string tableName)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(SUM(op.Amount), 0)
FROM OrderPayments op
INNER JOIN Orders o ON o.Id = op.OrderId
INNER JOIN Tables t ON t.Id = o.TableId
WHERE t.Name = $tableName
  AND o.Status = 0
  AND o.ClosedAt IS NULL;";

        command.Parameters.AddWithValue("$tableName", tableName);
        object? result = command.ExecuteScalar();

        return result is null || result == DBNull.Value
            ? 0
            : Convert.ToDecimal(result, CultureInfo.InvariantCulture);
    }

    public static void AddOpenOrderPayment(
    string tableName,
    string paymentType,
    decimal amount,
    string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Ödeme tutarı sıfırdan büyük olmalıdır.");

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction =
            connection.BeginTransaction();

        long orderId;

        using (var orderCommand = connection.CreateCommand())
        {
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = @"
SELECT o.Id
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE t.Name = $tableName
  AND o.Status = 0
  AND o.ClosedAt IS NULL
LIMIT 1;";

            orderCommand.Parameters.AddWithValue(
                "$tableName",
                tableName);

            object? result = orderCommand.ExecuteScalar();

            if (result is null)
            {
                throw new InvalidOperationException(
                    "Ödeme kaydedilecek açık adisyon bulunamadı.");
            }

            orderId = Convert.ToInt64(result);
        }

        using (var paymentCommand = connection.CreateCommand())
        {
            paymentCommand.Transaction = transaction;
            paymentCommand.CommandText = @"
INSERT INTO OrderPayments
(
    OrderId,
    PaymentType,
    Amount,
    Description,
    CreatedAt
)
VALUES
(
    $orderId,
    $paymentType,
    $amount,
    $description,
    $createdAt
);";

            paymentCommand.Parameters.AddWithValue(
                "$orderId",
                orderId);

            paymentCommand.Parameters.AddWithValue(
                "$paymentType",
                paymentType);

            paymentCommand.Parameters.AddWithValue(
                "$amount",
                amount);

            paymentCommand.Parameters.AddWithValue(
                "$description",
                description ?? string.Empty);

            paymentCommand.Parameters.AddWithValue(
                "$createdAt",
                DateTime.Now.ToString("O"));

            paymentCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static bool ProcessProductPayment(
        string tableName,
        IEnumerable<SavedOrderItem> selectedItems,
        string paymentType,
        decimal amount,
        string? description = null)
    {
        var selections = selectedItems
            .Where(item => item.Quantity > 0)
            .GroupBy(item => item.ProductId)
            .Select(group => new SavedOrderItem(
                group.Key,
                group.First().Name,
                group.Sum(item => item.Quantity),
                group.First().UnitPrice,
                group.Sum(item => item.SentQuantity),
                group.Select(item => item.Note).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)) ?? string.Empty))
            .ToList();

        if (selections.Count == 0)
            throw new InvalidOperationException(
                "Ödeme için seçilmiş ürün bulunamadı.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Ödeme tutarı sıfırdan büyük olmalıdır.");

        decimal selectionTotal = selections.Sum(
            item => item.UnitPrice * item.Quantity);

        if (Math.Abs(selectionTotal - amount) > 0.01m)
            throw new InvalidOperationException(
                "Seçilen ürünlerin toplamı ile ödeme tutarı uyuşmuyor.");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        long orderId;
        long tableId;

        using (var orderCommand = connection.CreateCommand())
        {
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = @"
SELECT o.Id, o.TableId
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE t.Name = $tableName
  AND o.Status = 0
  AND o.ClosedAt IS NULL
LIMIT 1;";

            orderCommand.Parameters.AddWithValue("$tableName", tableName);

            using var reader = orderCommand.ExecuteReader();

            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    "Ödeme kaydedilecek açık adisyon bulunamadı.");
            }

            orderId = reader.GetInt64(0);
            tableId = reader.GetInt64(1);
        }

        foreach (SavedOrderItem selection in selections)
        {
            long orderItemId;
            int currentQuantity;
            decimal currentUnitPrice;
            int currentSentQuantity;

            using (var itemCommand = connection.CreateCommand())
            {
                itemCommand.Transaction = transaction;
                itemCommand.CommandText = @"
SELECT Id, Quantity, UnitPrice, SentQuantity
FROM OrderItems
WHERE OrderId = $orderId
  AND ProductId = $productId
LIMIT 1;";

                itemCommand.Parameters.AddWithValue("$orderId", orderId);
                itemCommand.Parameters.AddWithValue(
                    "$productId",
                    selection.ProductId);

                using var reader = itemCommand.ExecuteReader();

                if (!reader.Read())
                {
                    throw new InvalidOperationException(
                        $"{selection.Name} açık adisyonda bulunamadı.");
                }

                orderItemId = reader.GetInt64(0);
                currentQuantity = reader.GetInt32(1);
                currentUnitPrice = Convert.ToDecimal(
                    reader.GetDouble(2),
                    CultureInfo.InvariantCulture);
                currentSentQuantity = reader.GetInt32(3);
            }

            if (selection.Quantity > currentQuantity)
            {
                throw new InvalidOperationException(
                    $"{selection.Name} için seçilen adet adisyondaki adedi aşıyor.");
            }

            if (Math.Abs(currentUnitPrice - selection.UnitPrice) > 0.01m)
            {
                throw new InvalidOperationException(
                    $"{selection.Name} ürününün fiyatı değişmiş. Adisyonu yenileyin.");
            }

            if (selection.Quantity == currentQuantity)
            {
                using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText =
                    "DELETE FROM OrderItems WHERE Id = $orderItemId;";
                deleteCommand.Parameters.AddWithValue(
                    "$orderItemId",
                    orderItemId);
                deleteCommand.ExecuteNonQuery();
            }
            else
            {
                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
UPDATE OrderItems
SET Quantity = Quantity - $quantity,
    SentQuantity = MAX(0, SentQuantity - $quantity)
WHERE Id = $orderItemId;";

                updateCommand.Parameters.AddWithValue(
                    "$quantity",
                    selection.Quantity);
                updateCommand.Parameters.AddWithValue(
                    "$orderItemId",
                    orderItemId);
                updateCommand.ExecuteNonQuery();
            }
        }

        using (var paymentCommand = connection.CreateCommand())
        {
            paymentCommand.Transaction = transaction;
            paymentCommand.CommandText = @"
INSERT INTO OrderPayments
(
    OrderId,
    PaymentType,
    Amount,
    Description,
    CreatedAt
)
VALUES
(
    $orderId,
    $paymentType,
    $amount,
    $description,
    $createdAt
);";

            paymentCommand.Parameters.AddWithValue("$orderId", orderId);
            paymentCommand.Parameters.AddWithValue(
                "$paymentType",
                paymentType);
            paymentCommand.Parameters.AddWithValue("$amount", amount);
            paymentCommand.Parameters.AddWithValue(
                "$description",
                description ?? string.Empty);
            paymentCommand.Parameters.AddWithValue(
                "$createdAt",
                DateTime.Now.ToString("O"));
            paymentCommand.ExecuteNonQuery();
        }

        bool hasRemainingItems;

        using (var countCommand = connection.CreateCommand())
        {
            countCommand.Transaction = transaction;
            countCommand.CommandText = @"
SELECT EXISTS
(
    SELECT 1
    FROM OrderItems
    WHERE OrderId = $orderId
      AND Quantity > 0
);";

            countCommand.Parameters.AddWithValue("$orderId", orderId);
            hasRemainingItems =
                Convert.ToInt32(countCommand.ExecuteScalar()) == 1;
        }

        if (hasRemainingItems)
        {
            using var tableCommand = connection.CreateCommand();
            tableCommand.Transaction = transaction;
            tableCommand.CommandText =
                "UPDATE Tables SET Status = 1 WHERE Id = $tableId;";
            tableCommand.Parameters.AddWithValue("$tableId", tableId);
            tableCommand.ExecuteNonQuery();

            transaction.Commit();
            return false;
        }

        decimal paidTotal;
        int paymentTypeCount;
        string finalPaymentType;

        using (var totalCommand = connection.CreateCommand())
        {
            totalCommand.Transaction = transaction;
            totalCommand.CommandText = @"
SELECT COALESCE(SUM(Amount), 0),
       COUNT(DISTINCT PaymentType),
       COALESCE(MIN(PaymentType), '')
FROM OrderPayments
WHERE OrderId = $orderId;";

            totalCommand.Parameters.AddWithValue("$orderId", orderId);

            using var reader = totalCommand.ExecuteReader();
            reader.Read();

            paidTotal = Convert.ToDecimal(
                reader.GetDouble(0),
                CultureInfo.InvariantCulture);
            paymentTypeCount = reader.GetInt32(1);
            finalPaymentType = reader.GetString(2);
        }

        if (paymentTypeCount > 1)
            finalPaymentType = "Çoklu Ödeme";

        using (var closeCommand = connection.CreateCommand())
        {
            closeCommand.Transaction = transaction;
            closeCommand.CommandText = @"
UPDATE Orders
SET ClosedAt = $closedAt,
    Status = 1,
    PaymentType = $paymentType,
    TotalAmount = $totalAmount
WHERE Id = $orderId;";

            closeCommand.Parameters.AddWithValue(
                "$closedAt",
                DateTime.Now.ToString("O"));
            closeCommand.Parameters.AddWithValue(
                "$paymentType",
                finalPaymentType);
            closeCommand.Parameters.AddWithValue(
                "$totalAmount",
                paidTotal);
            closeCommand.Parameters.AddWithValue("$orderId", orderId);
            closeCommand.ExecuteNonQuery();
        }

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText =
                "UPDATE Tables SET Status = 0 WHERE Id = $tableId;";
            tableCommand.Parameters.AddWithValue("$tableId", tableId);
            tableCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        return true;
    }

    public static void CloseOpenOrder(
    string tableName,
    string paymentType,
    decimal totalAmount)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        using (var orderCommand = connection.CreateCommand())
        {
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = @"
UPDATE Orders
SET ClosedAt = $closedAt,
    Status = 1,
    PaymentType = $paymentType,
    TotalAmount = $totalAmount
WHERE Id =
(
    SELECT o.Id
    FROM Orders o
    INNER JOIN Tables t ON t.Id = o.TableId
    WHERE t.Name = $tableName
      AND o.Status = 0
      AND o.ClosedAt IS NULL
    LIMIT 1
);";

            orderCommand.Parameters.AddWithValue(
                "$closedAt",
                DateTime.Now.ToString("O"));

            orderCommand.Parameters.AddWithValue(
                "$paymentType",
                paymentType);

            orderCommand.Parameters.AddWithValue(
                "$totalAmount",
                totalAmount);

            orderCommand.Parameters.AddWithValue(
                "$tableName",
                tableName);

            int affectedRows = orderCommand.ExecuteNonQuery();

            if (affectedRows == 0)
                throw new InvalidOperationException(
                    "Kapatılacak açık adisyon bulunamadı.");
        }

        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.Transaction = transaction;
            tableCommand.CommandText = @"
UPDATE Tables
SET Status = 0
WHERE Name = $tableName;";

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                tableName);

            tableCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void ImportKaldiMenu(SqliteConnection connection)
    {
        string menuPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Data",
            "kaldi-menu.json");

        if (!File.Exists(menuPath))
            throw new FileNotFoundException("Kaldi menü veri dosyası bulunamadı.", menuPath);

        KaldiMenuData? menu = JsonSerializer.Deserialize<KaldiMenuData>(
            File.ReadAllText(menuPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (menu is null || string.IsNullOrWhiteSpace(menu.MenuVersion))
            throw new InvalidDataException("Kaldi menü veri dosyası okunamadı.");

        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT Value FROM AppMetadata WHERE Key = $key;";
        versionCommand.Parameters.AddWithValue("$key", MenuVersionKey);
        string? currentVersion = versionCommand.ExecuteScalar()?.ToString();

        if (currentVersion == menu.MenuVersion)
            return;

        using var transaction = connection.BeginTransaction();

        using (var clearProducts = connection.CreateCommand())
        {
            clearProducts.Transaction = transaction;
            clearProducts.CommandText = "DELETE FROM Products;";
            clearProducts.ExecuteNonQuery();
        }

        using (var clearCategories = connection.CreateCommand())
        {
            clearCategories.Transaction = transaction;
            clearCategories.CommandText = "DELETE FROM Categories;";
            clearCategories.ExecuteNonQuery();
        }

        foreach (KaldiCategoryData category in menu.Categories)
        {
            using var categoryCommand = connection.CreateCommand();
            categoryCommand.Transaction = transaction;
            categoryCommand.CommandText = @"
INSERT INTO Categories (Name, SortOrder)
VALUES ($name, $sortOrder);
SELECT last_insert_rowid();";
            categoryCommand.Parameters.AddWithValue("$name", category.Name);
            categoryCommand.Parameters.AddWithValue("$sortOrder", category.SortOrder);
            long categoryId = (long)(categoryCommand.ExecuteScalar() ?? 0L);

            foreach (KaldiProductData product in category.Products)
            {
                using var productCommand = connection.CreateCommand();
                productCommand.Transaction = transaction;
                productCommand.CommandText = @"
INSERT INTO Products
(CategoryId, Name, Price, ImagePath, Description, ExternalId, SortOrder, IsActive)
VALUES
($categoryId, $name, $price, $imagePath, $description, $externalId, $sortOrder, 1);";
                productCommand.Parameters.AddWithValue("$categoryId", categoryId);
                productCommand.Parameters.AddWithValue("$name", product.Name);
                productCommand.Parameters.AddWithValue("$price", product.Price);
                productCommand.Parameters.AddWithValue("$imagePath", product.ImagePath);
                productCommand.Parameters.AddWithValue("$description", product.Description ?? string.Empty);
                productCommand.Parameters.AddWithValue("$externalId", product.ExternalId ?? string.Empty);
                productCommand.Parameters.AddWithValue("$sortOrder", product.SortOrder);
                productCommand.ExecuteNonQuery();
            }
        }

        using (var metadataCommand = connection.CreateCommand())
        {
            metadataCommand.Transaction = transaction;
            metadataCommand.CommandText = @"
INSERT INTO AppMetadata (Key, Value)
VALUES ($key, $value)
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            metadataCommand.Parameters.AddWithValue("$key", MenuVersionKey);
            metadataCommand.Parameters.AddWithValue("$value", menu.MenuVersion);
            metadataCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void SeedTables(SqliteConnection connection)
    {
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Tables;";
        if (Convert.ToInt32(countCommand.ExecuteScalar()) > 0)
            return;

        using var transaction = connection.BeginTransaction();
        for (int i = 1; i <= 60; i++)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Tables (Name, Hall, Status)
VALUES ($name, 'Salon', 0);";
            command.Parameters.AddWithValue("$name", $"Masa {i}");
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public static DateTime GetActiveBusinessDate()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        return GetActiveBusinessDate(connection);
    }

    private static DateTime GetActiveBusinessDate(
        SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Value
FROM AppMetadata
WHERE Key = 'ActiveBusinessDate'
LIMIT 1;";

        string? value = command.ExecuteScalar()?.ToString();

        if (DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime businessDate))
        {
            return businessDate.Date;
        }

        DateTime initialDate = DateTime.Today;
        SetActiveBusinessDate(connection, initialDate);

        return initialDate;
    }

    private static void SetActiveBusinessDate(
        SqliteConnection connection,
        DateTime businessDate)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AppMetadata (Key, Value)
VALUES ('ActiveBusinessDate', $value)
ON CONFLICT(Key) DO UPDATE SET
    Value = excluded.Value;";

        command.Parameters.AddWithValue(
            "$value",
            businessDate.Date.ToString("yyyy-MM-dd"));

        command.ExecuteNonQuery();
    }

    private static void EnsureActiveBusinessDate(
        SqliteConnection connection)
    {
        _ = GetActiveBusinessDate(connection);
    }

    private static void BackfillOrderBusinessDates(
        SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Orders
SET BusinessDate = substr(OpenedAt, 1, 10)
WHERE BusinessDate IS NULL
   OR trim(BusinessDate) = '';";

        command.ExecuteNonQuery();
    }

    public static OrderDetailData? GetOrderDetail(long orderId)
    {
        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var orderCommand =
            connection.CreateCommand();

        orderCommand.CommandText = @"
SELECT
    o.Id,
    t.Name,
    o.OpenedAt,
    o.ClosedAt,
    COALESCE(o.PaymentType, 'Belirtilmedi'),
    o.TotalAmount
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE o.Id = $orderId
LIMIT 1;";

        orderCommand.Parameters.AddWithValue(
            "$orderId",
            orderId);

        using var reader =
            orderCommand.ExecuteReader();

        if (!reader.Read())
            return null;

        long id = reader.GetInt64(0);
        string tableName = reader.GetString(1);

        DateTime openedAt = DateTime.Parse(
            reader.GetString(2),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        DateTime? closedAt = reader.IsDBNull(3)
            ? null
            : DateTime.Parse(
                reader.GetString(3),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

        string paymentType =
            reader.GetString(4);

        decimal totalAmount = Convert.ToDecimal(
            reader.GetDouble(5),
            CultureInfo.InvariantCulture);

        reader.Close();

        var items =
            new List<OrderDetailItem>();

        using var itemCommand =
            connection.CreateCommand();

        itemCommand.CommandText = @"
SELECT
    p.Name,
    oi.Quantity,
    oi.UnitPrice,
    COALESCE(oi.Note, '')
FROM OrderItems oi
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = $orderId
ORDER BY oi.Id;";

        itemCommand.Parameters.AddWithValue(
            "$orderId",
            orderId);

        using var itemReader =
            itemCommand.ExecuteReader();

        while (itemReader.Read())
        {
            items.Add(new OrderDetailItem(
                itemReader.GetString(0),
                itemReader.GetInt32(1),
                Convert.ToDecimal(
                    itemReader.GetDouble(2),
                    CultureInfo.InvariantCulture),
                itemReader.GetString(3)));
        }

        return new OrderDetailData(
            id,
            tableName,
            openedAt,
            closedAt,
            paymentType,
            totalAmount,
            items);
    }

    public sealed record OrderDetailItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Note)
    {
        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");

        public decimal LineTotal =>
            Quantity * UnitPrice;

        public string QuantityText =>
            Quantity + " x";

        public string UnitPriceText =>
            UnitPrice.ToString("N2", TurkishCulture) + " ₺";

        public string LineTotalText =>
            LineTotal.ToString("N2", TurkishCulture) + " ₺";
    }

    public sealed record OrderDetailData(
        long OrderId,
        string TableName,
        DateTime OpenedAt,
        DateTime? ClosedAt,
        string PaymentType,
        decimal TotalAmount,
        List<OrderDetailItem> Items)
    {
        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");

        public string TotalAmountText =>
            TotalAmount.ToString("N2", TurkishCulture) + " ₺";
    }

    public static List<CancelledOrderReportItem> GetCancelledOrders(
    DateTime date)
    {
        var results = new List<CancelledOrderReportItem>();

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
SELECT
    o.Id,
    t.Name,
    COALESCE(o.CancelledAt, o.ClosedAt),
    COALESCE(o.CancelledBy, 'Bilinmiyor'),
    COALESCE(o.CancelReason, 'Sebep belirtilmedi'),
    COALESCE(o.TotalAmount, 0)
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE COALESCE(o.IsCancelled, 0) = 1
  AND o.BusinessDate = $businessDate
ORDER BY COALESCE(o.CancelledAt, o.ClosedAt) DESC;";

        command.Parameters.AddWithValue(
            "$businessDate",
            date.Date.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            string cancelledAtText =
                reader.IsDBNull(2)
                    ? DateTime.Now.ToString("O")
                    : reader.GetString(2);

            DateTime cancelledAt = DateTime.Parse(
                cancelledAtText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            decimal totalAmount = Convert.ToDecimal(
                reader.GetDouble(5),
                CultureInfo.InvariantCulture);

            results.Add(
                new CancelledOrderReportItem(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    cancelledAt,
                    reader.GetString(3),
                    reader.GetString(4),
                    totalAmount));
        }

        return results;
    }

    public static List<SalesReportItem> GetClosedOrders(DateTime date)
    {
        var results = new List<SalesReportItem>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    o.Id,
    t.Name,
    o.ClosedAt,
    COALESCE(o.PaymentType, 'Belirtilmedi'),
    o.TotalAmount
FROM Orders o
INNER JOIN Tables t ON t.Id = o.TableId
WHERE o.Status = 1
  AND o.ClosedAt IS NOT NULL
  AND COALESCE(o.IsCancelled, 0) = 0
  AND o.BusinessDate = $businessDate
ORDER BY o.ClosedAt DESC;";

        command.Parameters.AddWithValue(
            "$businessDate",
            date.Date.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            DateTime closedAt = DateTime.Parse(
                reader.GetString(2),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            results.Add(new SalesReportItem(
                reader.GetInt64(0),
                reader.GetString(1),
                closedAt,
                reader.GetString(3),
                Convert.ToDecimal(
                    reader.GetDouble(4),
                    CultureInfo.InvariantCulture)));
        }

        return results;
    }

    public static SalesReportSummary GetSalesReportSummary(DateTime date)
    {
        List<SalesReportItem> orders = GetClosedOrders(date);

        decimal cashTotal = orders
            .Where(order => order.PaymentType == "Nakit")
            .Sum(order => order.TotalAmount);

        decimal cardTotal = orders
            .Where(order => order.PaymentType == "Kart")
            .Sum(order => order.TotalAmount);

        decimal mixedTotal = orders
            .Where(order =>
                order.PaymentType != "Nakit" &&
                order.PaymentType != "Kart")
            .Sum(order => order.TotalAmount);

        return new SalesReportSummary(
            orders.Count,
            orders.Sum(order => order.TotalAmount),
            cashTotal,
            cardTotal,
            mixedTotal);
    }

    public static int GetOpenTableCount()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM Orders
WHERE Status = 0
  AND ClosedAt IS NULL;";

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static bool IsDayEndClosed(DateTime date)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT EXISTS
(
    SELECT 1
    FROM DayEndClosures
    WHERE BusinessDate = $businessDate
);";

        command.Parameters.AddWithValue(
            "$businessDate",
            date.Date.ToString("yyyy-MM-dd"));

        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    public static bool CreateAutomaticDayEnd()
    {
        DateTime activeBusinessDate = GetActiveBusinessDate();
        DateTime nextBusinessDate = activeBusinessDate.AddDays(1);

        SalesReportSummary summary =
            GetSalesReportSummary(activeBusinessDate);

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction = connection.BeginTransaction();

        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
SELECT EXISTS
(
    SELECT 1
    FROM DayEndClosures
    WHERE BusinessDate = $businessDate
);";

            checkCommand.Parameters.AddWithValue(
                "$businessDate",
                activeBusinessDate.ToString("yyyy-MM-dd"));

            bool alreadyClosed =
                Convert.ToInt32(checkCommand.ExecuteScalar()) == 1;

            if (alreadyClosed)
            {
                transaction.Rollback();
                return false;
            }
        }

        using (var dayEndCommand = connection.CreateCommand())
        {
            dayEndCommand.Transaction = transaction;
            dayEndCommand.CommandText = @"
INSERT INTO DayEndClosures
(
    BusinessDate,
    ClosedAt,
    OrderCount,
    TotalRevenue,
    CashTotal,
    CardTotal,
    MixedTotal
)
VALUES
(
    $businessDate,
    $closedAt,
    $orderCount,
    $totalRevenue,
    $cashTotal,
    $cardTotal,
    $mixedTotal
);";

            dayEndCommand.Parameters.AddWithValue(
                "$businessDate",
                activeBusinessDate.ToString("yyyy-MM-dd"));

            dayEndCommand.Parameters.AddWithValue(
                "$closedAt",
                DateTime.Now.ToString("O"));

            dayEndCommand.Parameters.AddWithValue(
                "$orderCount",
                summary.OrderCount);

            dayEndCommand.Parameters.AddWithValue(
                "$totalRevenue",
                summary.TotalRevenue);

            dayEndCommand.Parameters.AddWithValue(
                "$cashTotal",
                summary.CashTotal);

            dayEndCommand.Parameters.AddWithValue(
                "$cardTotal",
                summary.CardTotal);

            dayEndCommand.Parameters.AddWithValue(
                "$mixedTotal",
                summary.MixedTotal);

            dayEndCommand.ExecuteNonQuery();
        }

        // Açık adisyonları kapatmadan yeni iş gününe devret.
        using (var carryCommand = connection.CreateCommand())
        {
            carryCommand.Transaction = transaction;
            carryCommand.CommandText = @"
UPDATE Orders
SET BusinessDate = $nextBusinessDate
WHERE Status = 0
  AND ClosedAt IS NULL;";

            carryCommand.Parameters.AddWithValue(
                "$nextBusinessDate",
                nextBusinessDate.ToString("yyyy-MM-dd"));

            carryCommand.ExecuteNonQuery();
        }

        using (var metadataCommand = connection.CreateCommand())
        {
            metadataCommand.Transaction = transaction;
            metadataCommand.CommandText = @"
INSERT INTO AppMetadata (Key, Value)
VALUES ('ActiveBusinessDate', $nextBusinessDate)
ON CONFLICT(Key) DO UPDATE SET
    Value = excluded.Value;";

            metadataCommand.Parameters.AddWithValue(
                "$nextBusinessDate",
                nextBusinessDate.ToString("yyyy-MM-dd"));

            metadataCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        return true;
    }

    public static void CreateDayEnd(DateTime date)
    {
        DateTime activeBusinessDate = GetActiveBusinessDate();
        DateTime requestedDate = date.Date;

        if (requestedDate != activeBusinessDate)
        {
            throw new InvalidOperationException(
                $"Aktif iş günü {activeBusinessDate:dd.MM.yyyy}. " +
                "Yalnızca aktif iş günü kapatılabilir.");
        }

        if (GetOpenTableCount() > 0)
        {
            throw new InvalidOperationException(
                "Açık masalar kapatılmadan gün sonu alınamaz.");
        }

        SalesReportSummary summary =
            GetSalesReportSummary(activeBusinessDate);

        if (summary.OrderCount == 0)
        {
            throw new InvalidOperationException(
                "Aktif iş gününde kapatılmış adisyon bulunmuyor.");
        }

        using var connection =
            new SqliteConnection(ConnectionString);

        connection.Open();

        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO DayEndClosures
(
    BusinessDate,
    ClosedAt,
    OrderCount,
    TotalRevenue,
    CashTotal,
    CardTotal,
    MixedTotal
)
VALUES
(
    $businessDate,
    $closedAt,
    $orderCount,
    $totalRevenue,
    $cashTotal,
    $cardTotal,
    $mixedTotal
)
ON CONFLICT(BusinessDate) DO UPDATE SET
    ClosedAt = excluded.ClosedAt,
    OrderCount = excluded.OrderCount,
    TotalRevenue = excluded.TotalRevenue,
    CashTotal = excluded.CashTotal,
    CardTotal = excluded.CardTotal,
    MixedTotal = excluded.MixedTotal;";

            command.Parameters.AddWithValue(
                "$businessDate",
                activeBusinessDate.ToString("yyyy-MM-dd"));

            command.Parameters.AddWithValue(
                "$closedAt",
                DateTime.Now.ToString("O"));

            command.Parameters.AddWithValue(
                "$orderCount",
                summary.OrderCount);

            command.Parameters.AddWithValue(
                "$totalRevenue",
                summary.TotalRevenue);

            command.Parameters.AddWithValue(
                "$cashTotal",
                summary.CashTotal);

            command.Parameters.AddWithValue(
                "$cardTotal",
                summary.CardTotal);

            command.Parameters.AddWithValue(
                "$mixedTotal",
                summary.MixedTotal);

            command.ExecuteNonQuery();
        }

        SetActiveBusinessDate(
            connection,
            activeBusinessDate.AddDays(1));

        transaction.Commit();
    }

    public static List<DayEndRecord> GetDayEndClosures()
    {
        var results = new List<DayEndRecord>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    Id,
    BusinessDate,
    ClosedAt,
    OrderCount,
    TotalRevenue,
    CashTotal,
    CardTotal,
    MixedTotal
FROM DayEndClosures
ORDER BY BusinessDate DESC, Id DESC;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new DayEndRecord(
                reader.GetInt64(0),
                DateTime.ParseExact(
                    reader.GetString(1),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),
                DateTime.Parse(
                    reader.GetString(2),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                reader.GetInt32(3),
                Convert.ToDecimal(
                    reader.GetDouble(4),
                    CultureInfo.InvariantCulture),
                Convert.ToDecimal(
                    reader.GetDouble(5),
                    CultureInfo.InvariantCulture),
                Convert.ToDecimal(
                    reader.GetDouble(6),
                    CultureInfo.InvariantCulture),
                Convert.ToDecimal(
                    reader.GetDouble(7),
                    CultureInfo.InvariantCulture)));
        }

        return results;
    }


    public static List<UserRecord> GetUsers()
    {
        var users = new List<UserRecord>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FullName, Role, IsActive, CreatedAt
FROM Users
ORDER BY IsActive DESC, FullName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new UserRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                DateTime.Parse(
                    reader.GetString(4),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind)));
        }

        return users;
    }

    public static List<PermissionRecord> GetPermissions()
    {
        var permissions = new List<PermissionRecord>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT PermissionKey, PermissionName, Category
FROM Permissions
ORDER BY Category, PermissionName COLLATE NOCASE;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            permissions.Add(new PermissionRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                false));
        }

        return permissions;
    }

    public static List<PermissionRecord> GetUserPermissions(int userId)
    {
        var permissions = new List<PermissionRecord>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    p.PermissionKey,
    p.PermissionName,
    p.Category,
    COALESCE(up.IsAllowed, 0)
FROM Permissions p
LEFT JOIN UserPermissions up
    ON up.PermissionKey = p.PermissionKey
   AND up.UserId = @UserId
ORDER BY p.Category, p.PermissionName COLLATE NOCASE;";

        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            permissions.Add(new PermissionRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1));
        }

        return permissions;
    }

    public static void SaveUserPermissions(
        int userId,
        IEnumerable<PermissionRecord> permissions)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = @"
DELETE FROM UserPermissions
WHERE UserId = @UserId;";

                deleteCommand.Parameters.AddWithValue("@UserId", userId);
                deleteCommand.ExecuteNonQuery();
            }

            foreach (var permission in permissions)
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
INSERT INTO UserPermissions
(
    UserId,
    PermissionKey,
    IsAllowed
)
VALUES
(
    @UserId,
    @PermissionKey,
    @IsAllowed
);";

                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue(
                    "@PermissionKey",
                    permission.PermissionKey);
                insertCommand.Parameters.AddWithValue(
                    "@IsAllowed",
                    permission.IsAllowed ? 1 : 0);

                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static UserRecord? VerifyUserPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return null;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FullName, Role, IsActive, CreatedAt
FROM Users
WHERE PinHash = @PinHash
  AND IsActive = 1
LIMIT 1;";

        command.Parameters.AddWithValue("@PinHash", HashPin(pin));

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new UserRecord(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            true,
            DateTime.Parse(
                reader.GetString(4),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind));
    }

    public static int AddUser(
        string fullName,
        string pin,
        string role)
    {
        ValidateUserInput(fullName, pin, role);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Users
(
    FullName,
    PinHash,
    Role,
    IsActive,
    CreatedAt
)
VALUES
(
    @FullName,
    @PinHash,
    @Role,
    1,
    @CreatedAt
);

SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@FullName", fullName.Trim());
        command.Parameters.AddWithValue("@PinHash", HashPin(pin));
        command.Parameters.AddWithValue("@Role", role.Trim());
        command.Parameters.AddWithValue(
            "@CreatedAt",
            DateTime.Now.ToString("O", CultureInfo.InvariantCulture));

        try
        {
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(
                "Bu PIN başka bir kullanıcı tarafından kullanılıyor.");
        }
    }

    public static void UpdateUser(
        int userId,
        string fullName,
        string role,
        string? newPin = null)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Kullanıcı adı boş bırakılamaz.");

        ValidateRole(role);

        if (!string.IsNullOrWhiteSpace(newPin) &&
            (newPin.Length < 4 ||
             newPin.Length > 32 ||
             !newPin.All(char.IsDigit)))
        {
            throw new InvalidOperationException("PIN yalnızca rakamlardan oluşmalı ve 4-32 hane arasında olmalıdır.");
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(newPin))
        {
            command.CommandText = @"
UPDATE Users
SET FullName = @FullName,
    Role = @Role
WHERE Id = @Id;";
        }
        else
        {
            command.CommandText = @"
UPDATE Users
SET FullName = @FullName,
    Role = @Role,
    PinHash = @PinHash
WHERE Id = @Id;";

            command.Parameters.AddWithValue("@PinHash", HashPin(newPin));
        }

        command.Parameters.AddWithValue("@Id", userId);
        command.Parameters.AddWithValue("@FullName", fullName.Trim());
        command.Parameters.AddWithValue("@Role", role.Trim());

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(
                "Bu PIN başka bir kullanıcı tarafından kullanılıyor.");
        }
    }

    public static void SetUserActive(
        int userId,
        bool isActive)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        if (!isActive)
        {
            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"
SELECT Role, IsActive
FROM Users
WHERE Id = @Id
LIMIT 1;";

            checkCommand.Parameters.AddWithValue("@Id", userId);

            using var reader = checkCommand.ExecuteReader();

            if (!reader.Read())
                throw new InvalidOperationException("Kullanıcı bulunamadı.");

            string role = reader.GetString(0);
            bool currentlyActive = reader.GetInt32(1) == 1;
            reader.Close();

            if (currentlyActive &&
                string.Equals(role, "Yönetici", StringComparison.OrdinalIgnoreCase))
            {
                using var adminCountCommand = connection.CreateCommand();
                adminCountCommand.CommandText = @"
SELECT COUNT(*)
FROM Users
WHERE Role = 'Yönetici'
  AND IsActive = 1;";

                long activeAdminCount =
                    Convert.ToInt64(adminCountCommand.ExecuteScalar());

                if (activeAdminCount <= 1)
                {
                    throw new InvalidOperationException(
                        "Sistemde en az bir aktif yönetici bulunmalıdır.");
                }
            }
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Users
SET IsActive = @IsActive
WHERE Id = @Id;";

        command.Parameters.AddWithValue("@Id", userId);
        command.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
        command.ExecuteNonQuery();
    }

    private static void SeedDefaultAdmin(SqliteConnection connection)
    {
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Users;";

        long userCount = Convert.ToInt64(countCommand.ExecuteScalar());

        if (userCount > 0)
            return;

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
INSERT INTO Users
(
    FullName,
    PinHash,
    Role,
    IsActive,
    CreatedAt
)
VALUES
(
    'Yönetici',
    @PinHash,
    'Yönetici',
    1,
    @CreatedAt
);";

        insertCommand.Parameters.AddWithValue("@PinHash", HashPin("1234"));
        insertCommand.Parameters.AddWithValue(
            "@CreatedAt",
            DateTime.Now.ToString("O", CultureInfo.InvariantCulture));

        insertCommand.ExecuteNonQuery();
    }

    private static void SeedPermissions(SqliteConnection connection)
    {
        (string Key, string Name, string Category)[] permissions =
        {
        ("Order.AddItem","Ürün Ekle","Sipariş"),
        ("Order.RemoveItem","Ürün Sil","Sipariş"),
        ("Order.IncreaseQuantity","Adet Artır","Sipariş"),
        ("Order.DecreaseQuantity","Adet Azalt","Sipariş"),
        ("Order.Note","Sipariş Notu","Sipariş"),
        ("Order.Treat","İkram","Sipariş"),
        ("Order.Discount","İndirim","Sipariş"),
        ("Order.Transfer","Adisyon Taşı","Sipariş"),

        ("Payment.Cash","Nakit Ödeme","Ödeme"),
        ("Payment.Card","Kart Ödeme","Ödeme"),
        ("Payment.Mixed","Karma Ödeme","Ödeme"),
        ("Payment.Refund","İade","Ödeme"),
        ("Payment.Close","Adisyon Kapat","Ödeme"),

        ("Table.Open","Masa Aç","Masalar"),
        ("Table.Merge","Masa Birleştir","Masalar"),
        ("Table.Split","Masa Ayır","Masalar"),

        ("Menu.Products","Ürünler","Menü"),
        ("Menu.Reports","Raporlar","Menü"),
        ("Menu.DayEnd","Gün Sonu","Menü"),
        ("Menu.Settings","Ayarlar","Menü"),
        ("Menu.Audit","Denetim Kayıtları","Menü"),

        ("Manage.Users","Kullanıcı Yönetimi","Yönetim"),
        ("Manage.Products","Ürün Yönetimi","Yönetim"),
        ("Manage.Categories","Kategori Yönetimi","Yönetim"),
        ("Manage.Printers","Yazıcı Ayarları","Yönetim"),
        ("Manage.Backup","Yedekleme","Yönetim")
    };

        foreach (var permission in permissions)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText =
            """
        INSERT OR IGNORE INTO Permissions
        (
            PermissionKey,
            PermissionName,
            Category
        )
        VALUES
        (
            @Key,
            @Name,
            @Category
        );
        """;

            command.Parameters.AddWithValue("@Key", permission.Key);
            command.Parameters.AddWithValue("@Name", permission.Name);
            command.Parameters.AddWithValue("@Category", permission.Category);

            command.ExecuteNonQuery();
        }
    }

    private static void ValidateUserInput(
        string fullName,
        string pin,
        string role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Kullanıcı adı boş bırakılamaz.");

        if (pin.Length < 4 || pin.Length > 32 || !pin.All(char.IsDigit))
            throw new InvalidOperationException("PIN yalnızca rakamlardan oluşmalı ve 4-32 hane arasında olmalıdır.");

        ValidateRole(role);
    }

    private static void ValidateRole(string role)
    {
        string[] validRoles =
        {
            "Yönetici",
            "Kasiyer",
            "Garson"
        };

        if (!validRoles.Contains(role))
            throw new InvalidOperationException("Geçerli bir kullanıcı rolü seçin.");
    }

    private static string HashPin(string pin)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
        return Convert.ToHexString(hash);
    }

    private static void EnsureColumn(

        SqliteConnection connection,
        string tableName,
        string columnName,
        string definition)
    {

        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = checkCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return;
        }
        reader.Close();

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alterCommand.ExecuteNonQuery();
    }

}


public sealed record UserRecord(
    int Id,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt)
{
    public string StatusText =>
        IsActive ? "Aktif" : "Pasif";
}

public sealed record PermissionRecord(
    string PermissionKey,
    string PermissionName,
    string Category,
    bool IsAllowed);

public sealed record ProductRecord(
    int Id,
    string Name,
    string Category,
    decimal Price,
    string ImagePath);

public sealed record TableRecord(
    int Id,
    string Name,
    string Hall,
    int Status);

public sealed class KaldiMenuData
{
    public string MenuVersion { get; set; } = string.Empty;
    public List<KaldiCategoryData> Categories { get; set; } = new();
}

public sealed class KaldiCategoryData
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<KaldiProductData> Products { get; set; } = new();
}

public sealed class KaldiProductData
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed record SavedOrderItem(
    int ProductId,
    string Name,
    int Quantity,
    decimal UnitPrice,
    int SentQuantity = 0,
    string Note = "");

public sealed record OrderDetailItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Note)
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public decimal LineTotal =>
        Quantity * UnitPrice;

    public string QuantityText =>
        Quantity + " x";

    public string UnitPriceText =>
        UnitPrice.ToString("N2", TurkishCulture) + " ₺";

    public string LineTotalText =>
        LineTotal.ToString("N2", TurkishCulture) + " ₺";
}

public sealed record OrderDetailData(
    long OrderId,
    string TableName,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string PaymentType,
    decimal TotalAmount,
    List<OrderDetailItem> Items)
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public string TotalAmountText =>
        TotalAmount.ToString("N2", TurkishCulture) + " ₺";
}

public sealed record CancelledOrderReportItem(
    long OrderId,
    string TableName,
    DateTime CancelledAt,
    string CancelledBy,
    string CancelReason,
    decimal TotalAmount)
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public string DateText =>
        CancelledAt.ToString("dd.MM.yyyy");

    public string TimeText =>
        CancelledAt.ToString("HH:mm");

    public string DateTimeText =>
        CancelledAt.ToString("dd.MM.yyyy HH:mm");

    public string AmountText =>
        TotalAmount.ToString("N2", TurkishCulture) + " ₺";
}

public sealed record SalesReportItem(
    long OrderId,
    string TableName,
    DateTime ClosedAt,
    string PaymentType,
    decimal TotalAmount)
{
    public string TimeText => ClosedAt.ToString("HH:mm");

    public string AmountText =>
        TotalAmount.ToString(
            "N2",
            CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
}

public sealed record SalesReportSummary(
    int OrderCount,
    decimal TotalRevenue,
    decimal CashTotal,
    decimal CardTotal,
    decimal MixedTotal);

public sealed record DayEndRecord(
    long Id,
    DateTime BusinessDate,
    DateTime ClosedAt,
    int OrderCount,
    decimal TotalRevenue,
    decimal CashTotal,
    decimal CardTotal,
    decimal MixedTotal)
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public string DateText =>
        BusinessDate.ToString("dd.MM.yyyy");

    public string ClosedAtText =>
        ClosedAt.ToString("dd.MM.yyyy HH:mm");

    public string OrderCountText =>
        $"{OrderCount} adisyon";

    public string TotalRevenueText =>
        TotalRevenue.ToString("N2", TurkishCulture) + " ₺";

    public string PaymentSummaryText =>
        $"Nakit: {CashTotal.ToString("N2", TurkishCulture)} ₺   •   " +
        $"Kart: {CardTotal.ToString("N2", TurkishCulture)} ₺   •   " +
        $"Karma: {MixedTotal.ToString("N2", TurkishCulture)} ₺";
}
