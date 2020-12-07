#l "templates-variables.cake"

#addin "nuget:?package=Cake.FileHelpers&version=3.0.0"

using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

//-------------------------------------------------------------

public class TemplatesProcessor : ProcessorBase
{
    public TemplatesProcessor(BuildContext buildContext)
        : base(buildContext)
    {
    }

    public override bool HasItems()
    {
        return BuildContext.Templates.Items.Count > 0;
    }

    public override async Task PrepareAsync()
    {
        if (!HasItems())
        {
            return;
        }    
    }

    public override async Task UpdateInfoAsync()
    {
        if (!HasItems())
        {
            return;
        }

        var variableRegex = new Regex(@"\$\{([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        
        foreach (var template in BuildContext.Templates.Items)
        {
            CakeContext.Information("Updating file '{0}'", template);
            
            var templateFile  = $"deployment/templates/{template}";
            var content = CakeContext.FileReadText(templateFile);
            var variableNames = variableRegex.Matches(content).OfType<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            
            foreach (var variableName in variableNames)
            {
                if(BuildContext.Variables.TryGetValue(variableName, out var replacement))
                {
                    content = content.Replace($"${{{variableName}}}", replacement);
                }
            }

            CakeContext.FileWriteText($"src/{template}", content);
        }        
    }

    public override async Task BuildAsync()
    {
        if (!HasItems())
        {
            return;
        }
    }

    public override async Task PackageAsync()
    {
        if (!HasItems())
        {
            return;
        }
    }

    public override async Task DeployAsync()
    {
        if (!HasItems())
        {
            return;
        }
    }

    public override async Task FinalizeAsync()
    {

    }
}
