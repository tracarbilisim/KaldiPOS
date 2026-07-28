using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace KaldiPOS.Data;

public static class Database
{
    private const string ConnectionString = "Data Source=KaldiPOS.db";
    private const string MenuVersionKey = "KaldiMenuVersion";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    SortOrder INTEGER NOT NULL DEFAULT 0
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
    UnitPrice REAL NOT NULL
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

CREATE INDEX IF NOT EXISTS IX_OrderPayments_OrderId
ON OrderPayments(OrderId);

CREATE TABLE IF NOT EXISTS AppMetadata
(
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);";
        command.ExecuteNonQuery();

        EnsureColumn(connection, "Categories", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Products", "ImagePath", "TEXT");
        EnsureColumn(connection, "Products", "Description", "TEXT");
        EnsureColumn(connection, "Products", "ExternalId", "TEXT");
        EnsureColumn(connection, "Products", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Orders", "PaymentType", "TEXT");
        EnsureColumn(connection, "Orders", "TotalAmount", "REAL NOT NULL DEFAULT 0");

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
SELECT oi.ProductId, p.Name, oi.Quantity, oi.UnitPrice
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
                Convert.ToDecimal(reader.GetDouble(3), CultureInfo.InvariantCulture)));
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
INSERT INTO Orders (TableId, OpenedAt, ClosedAt, Status)
VALUES ($tableId, $openedAt, NULL, 0);
SELECT last_insert_rowid();";

                orderCommand.Parameters.Clear();
                orderCommand.Parameters.AddWithValue("$tableId", tableId);
                orderCommand.Parameters.AddWithValue(
                    "$openedAt",
                    DateTime.Now.ToString("O"));

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
(OrderId, ProductId, Quantity, UnitPrice)
VALUES
($orderId, $productId, $quantity, $unitPrice);";

            itemCommand.Parameters.AddWithValue("$orderId", orderId);
            itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
            itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
            itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
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
                group.First().UnitPrice))
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

            using (var itemCommand = connection.CreateCommand())
            {
                itemCommand.Transaction = transaction;
                itemCommand.CommandText = @"
SELECT Id, Quantity, UnitPrice
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
SET Quantity = Quantity - $quantity
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
    decimal UnitPrice);
