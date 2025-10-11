using System.Reflection;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace NeoLuke.Tests;

public class DirectoryImplementationTests
{
    [Fact]
    public void LoadDirectoryImplementations_ShouldFindSimpleFSDirectory()
    {
        var implementations = GetDirectoryImplementations();

        Assert.Contains(implementations, impl => impl.Name == "SimpleFSDirectory");
    }

    [Fact]
    public void LoadDirectoryImplementations_ShouldFindMMapDirectory()
    {
        var implementations = GetDirectoryImplementations();

        Assert.Contains(implementations, impl => impl.Name == "MMapDirectory");
    }

    [Fact]
    public void LoadDirectoryImplementations_ShouldFindNIOFSDirectory()
    {
        var implementations = GetDirectoryImplementations();

        Assert.Contains(implementations, impl => impl.Name == "NIOFSDirectory");
    }

    [Fact]
    public void LoadDirectoryImplementations_ShouldOnlyReturnConcreteClasses()
    {
        var implementations = GetDirectoryImplementations();

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        Assert.All(implementations, impl =>
        {
            Assert.True(impl.IsClass);
            Assert.False(impl.IsAbstract);
        });
    }

    private static Type[] GetDirectoryImplementations()
    {
        var directoryType = typeof(LuceneDirectory);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return [];
                }
            })
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                directoryType.IsAssignableFrom(type))
            .ToArray();
    }
}
