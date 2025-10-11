using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;

namespace NeoLuke.Tests;

/// <summary>
/// Utility class for generating test Lucene indexes for unit tests and demos
/// </summary>
public static class TestIndexGenerator
{
    /// <summary>
    /// Creates a small in-memory test index with sample documents
    /// </summary>
    public static RAMDirectory CreateSampleIndex()
    {
        var directory = new RAMDirectory();
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

        using var writer = new IndexWriter(directory, config);

        // Document 1: Technology article about AI
        writer.AddDocument(new Document
        {
            new StringField("id", "1", Field.Store.YES),
            new TextField("title", "Introduction to Artificial Intelligence", Field.Store.YES),
            new TextField("content", "Artificial intelligence is transforming how we interact with technology. Machine learning algorithms enable computers to learn from data and improve their performance over time. Deep learning, a subset of machine learning, uses neural networks to process complex patterns.", Field.Store.YES),
            new StringField("category", "technology", Field.Store.YES),
            new StringField("author", "John Smith", Field.Store.YES)
        });

        // Document 2: Technology article about Machine Learning (similar to doc 1)
        writer.AddDocument(new Document
        {
            new StringField("id", "2", Field.Store.YES),
            new TextField("title", "Machine Learning Fundamentals", Field.Store.YES),
            new TextField("content", "Machine learning is a branch of artificial intelligence that focuses on building systems that learn from data. Neural networks and deep learning have revolutionized the field, enabling computers to recognize patterns and make predictions with remarkable accuracy.", Field.Store.YES),
            new StringField("category", "technology", Field.Store.YES),
            new StringField("author", "Jane Doe", Field.Store.YES)
        });

        // Document 3: Sports article about Basketball
        writer.AddDocument(new Document
        {
            new StringField("id", "3", Field.Store.YES),
            new TextField("title", "NBA Season Highlights", Field.Store.YES),
            new TextField("content", "The basketball season has been exciting with amazing performances from star players. Teams are competing fiercely for playoff positions. The championship race is heating up as we approach the finals.", Field.Store.YES),
            new StringField("category", "sports", Field.Store.YES),
            new StringField("author", "Mike Johnson", Field.Store.YES)
        });

        // Document 4: Sports article about Football (different from basketball)
        writer.AddDocument(new Document
        {
            new StringField("id", "4", Field.Store.YES),
            new TextField("title", "World Cup Preview", Field.Store.YES),
            new TextField("content", "The World Cup is the biggest event in football. National teams from around the globe compete for the trophy. Fans eagerly anticipate the matches and support their countries with passion.", Field.Store.YES),
            new StringField("category", "sports", Field.Store.YES),
            new StringField("author", "Sarah Williams", Field.Store.YES)
        });

        // Document 5: Technology article about Cloud Computing
        writer.AddDocument(new Document
        {
            new StringField("id", "5", Field.Store.YES),
            new TextField("title", "Cloud Computing Trends", Field.Store.YES),
            new TextField("content", "Cloud computing has revolutionized how businesses store and process data. Artificial intelligence and machine learning services in the cloud make advanced technology accessible to companies of all sizes. The future of computing is in the cloud.", Field.Store.YES),
            new StringField("category", "technology", Field.Store.YES),
            new StringField("author", "John Smith", Field.Store.YES)
        });

        writer.Commit();

        return directory;
    }

    /// <summary>
    /// Creates a demo index on disk at the specified path
    /// </summary>
    public static void CreateDemoIndexOnDisk(string path)
    {
        if (System.IO.Directory.Exists(path))
        {
            System.IO.Directory.Delete(path, true);
        }

        var directory = FSDirectory.Open(path);
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

        using (var writer = new IndexWriter(directory, config))
        {
            // Add the same sample documents
            var ramDir = CreateSampleIndex();
            using var reader = DirectoryReader.Open(ramDir);

            for (int i = 0; i < reader.MaxDoc; i++)
            {
                var doc = reader.Document(i);
                writer.AddDocument(doc);
            }

            writer.Commit();
        }

        directory.Dispose();
    }

    /// <summary>
    /// Creates a test index with the specified number of similar document pairs
    /// Useful for testing More Like This functionality
    /// </summary>
    public static RAMDirectory CreateSimilarDocumentsIndex(int pairCount = 3)
    {
        var directory = new RAMDirectory();
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

        using var writer = new IndexWriter(directory, config);

        for (int i = 0; i < pairCount; i++)
        {
            // Add a pair of similar documents
            var sharedTerms = $"topic{i} concept{i} subject{i} matter{i}";

            writer.AddDocument(new Document
            {
                new StringField("id", $"{i*2}", Field.Store.YES),
                new TextField("content", $"{sharedTerms} first document variation unique terms", Field.Store.YES),
            });

            writer.AddDocument(new Document
            {
                new StringField("id", $"{i*2+1}", Field.Store.YES),
                new TextField("content", $"{sharedTerms} second document variation different terms", Field.Store.YES),
            });
        }

        writer.Commit();

        return directory;
    }
}
