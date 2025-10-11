using Bogus;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Globalization;
using LuceneDirectory = Lucene.Net.Store.Directory;

const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

Console.WriteLine("NeoLuke Demo Index Generator");
Console.WriteLine("=============================");
Console.WriteLine();

// Determine the demo index path
var repoRoot = FindRepoRoot();
if (repoRoot == null)
{
    Console.WriteLine("Error: Could not find repository root. Please run from within the lukenet repository.");
    return 1;
}

var demoPath = Path.Combine(repoRoot, "demo");

// Create demo directory if it doesn't exist
System.IO.Directory.CreateDirectory(demoPath);

Console.WriteLine($"Generating demo index at: {demoPath}");
Console.WriteLine();

// Initialize Bogus faker
Randomizer.Seed = new Random(12345); // Fixed seed for reproducibility

var productFaker = new Faker<Product>()
    .RuleFor(p => p.Id, f => f.IndexFaker + 1)
    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
    .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
    .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
    .RuleFor(p => p.Price, f => f.Random.Double(1.99, 999.99))
    .RuleFor(p => p.InStock, f => f.Random.Int(0, 1000))
    .RuleFor(p => p.Weight, f => f.Random.Float(0.1f, 50.0f))
    .RuleFor(p => p.Sku, f => f.Commerce.Ean13())
    .RuleFor(p => p.Rating, f => f.Random.Double(1.0, 5.0))
    .RuleFor(p => p.ReviewCount, f => f.Random.Int(0, 5000))
    .RuleFor(p => p.Manufacturer, f => f.Company.CompanyName())
    .RuleFor(p => p.Tags, f => f.Commerce.ProductAdjective() + ", " + f.Commerce.ProductMaterial())
    .RuleFor(p => p.ReleaseDate, f => f.Date.Past(5).ToString("yyyy-MM-dd"))
    .RuleFor(p => p.IsDiscounted, f => f.Random.Bool())
    .RuleFor(p => p.DiscountPercent, (f, p) => p.IsDiscounted ? f.Random.Int(5, 50) : 0);

// Generate 100 products
var products = productFaker.Generate(100);

// Create the Lucene index
using (LuceneDirectory directory = FSDirectory.Open(demoPath))
using (var analyzer = new StandardAnalyzer(AppLuceneVersion))
{
    var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
    {
        OpenMode = OpenMode.CREATE // Overwrite if exists
    };

    using var writer = new IndexWriter(directory, indexConfig);

    Console.WriteLine($"Writing {products.Count} documents to index...");

    foreach (var product in products)
    {
        var doc = new Document();

        // ID - stored, not indexed (use StringField with Field.Store.YES)
        doc.Add(new StringField("id", product.Id.ToString(CultureInfo.InvariantCulture), Field.Store.YES));

        // Name - analyzed text field, stored
        doc.Add(new TextField("name", product.Name, Field.Store.YES));

        // Description - analyzed text field, stored
        doc.Add(new TextField("description", product.Description, Field.Store.YES));

        // Category - not analyzed, stored (exact match searching)
        doc.Add(new StringField("category", product.Category, Field.Store.YES));

        // Price - numeric field for range queries
        doc.Add(new DoubleField("price", product.Price, Field.Store.YES));

        // InStock - integer field
        doc.Add(new Int32Field("in_stock", product.InStock, Field.Store.YES));

        // Weight - float field
        doc.Add(new SingleField("weight", product.Weight, Field.Store.YES));

        // SKU - stored string field for exact lookup
        doc.Add(new StringField("sku", product.Sku, Field.Store.YES));

        // Rating - double field
        doc.Add(new DoubleField("rating", product.Rating, Field.Store.YES));

        // Review Count - integer field
        doc.Add(new Int32Field("review_count", product.ReviewCount, Field.Store.YES));

        // Manufacturer - analyzed text field, stored
        doc.Add(new TextField("manufacturer", product.Manufacturer, Field.Store.YES));

        // Tags - analyzed text field
        doc.Add(new TextField("tags", product.Tags, Field.Store.YES));

        // Release Date - stored as string for simplicity
        doc.Add(new StringField("release_date", product.ReleaseDate, Field.Store.YES));

        // Is Discounted - stored as string "true"/"false"
        doc.Add(new StringField("is_discounted", product.IsDiscounted.ToString().ToLower(), Field.Store.YES));

        // Discount Percent - integer field
        doc.Add(new Int32Field("discount_percent", product.DiscountPercent, Field.Store.YES));

        // Calculated field: Final Price (not stored, for demonstration of computed fields)
        var finalPrice = product.IsDiscounted
            ? product.Price * (1.0 - product.DiscountPercent / 100.0)
            : product.Price;
        doc.Add(new DoubleField("final_price", finalPrice, Field.Store.YES));

        writer.AddDocument(doc);
    }

    writer.Commit();

    Console.WriteLine($"Successfully indexed {writer.NumDocs} documents.");
}

Console.WriteLine();
Console.WriteLine("Demo index generation complete!");
Console.WriteLine();
Console.WriteLine("You can now open this index in NeoLuke by running:");
Console.WriteLine("  dotnet run");
Console.WriteLine($"  and selecting: {demoPath}");

return 0;

// Helper method to find the repository root
static string? FindRepoRoot()
{
    var currentDir = System.IO.Directory.GetCurrentDirectory();

    while (currentDir != null)
    {
        // Look for .git directory or NeoLuke.csproj as indicators
        if (System.IO.Directory.Exists(Path.Combine(currentDir, ".git")) ||
            File.Exists(Path.Combine(currentDir, "NeoLuke", "NeoLuke.csproj")))
        {
            return currentDir;
        }

        currentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
    }

    return null;
}

// Product model class
class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Price { get; set; }
    public int InStock { get; set; }
    public float Weight { get; set; }
    public string Sku { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public bool IsDiscounted { get; set; }
    public int DiscountPercent { get; set; }
}
