#l "buildserver.cake"

//-------------------------------------------------------------

public class DependenciesContext : BuildContextWithItemsBase
{
    public DependenciesContext(IBuildContext parentBuildContext, Dictionary<string, List<string>> dependencies)
        : base(parentBuildContext)
    {
        Dependencies = dependencies ?? new Dictionary<string, List<string>>();
        Items = Dependencies.Keys.ToList();
    }

    public Dictionary<string, List<string>> Dependencies { get; private set; }

    protected override void ValidateContext()
    {

    }
    
    protected override void LogStateInfoForContext()
    {
        CakeContext.Information($"Found '{Items.Count}' dependency projects");
    }
}

//-------------------------------------------------------------

private DependenciesContext InitializeDependenciesContext(BuildContext buildContext, IBuildContext parentBuildContext)
{
    var data = new DependenciesContext(parentBuildContext, Dependencies);

    return data;
}

//-------------------------------------------------------------

Dictionary<string, List<string>> _dependencies;

public Dictionary<string, List<string>> Dependencies
{
    get 
    {
        if (_dependencies is null)
        {
            _dependencies = new Dictionary<string, List<string>>();
        }

        return _dependencies;
    }
}