namespace GooberFactory; 

/// <summary>
/// A mod dependency entry
/// </summary>
public class Dependency {
    /// <summary>
    /// A mod dependency type
    /// </summary>
    public enum TypeEnum {
        Incompatibility,
        IgnoreLoadOrder,
        HiddenOptional,
        Optional, Hard
    }
    
    public TypeEnum Type { get; }
    public string ModName { get; }
    public Version? ComparedTo { get;  }
    public string? Operator { get;  }

    /// <summary>
    /// Does a version of this mod match the operator
    /// </summary>
    /// <param name="version">Version</param>
    /// <returns>True or false</returns>
    public bool Matches(string version)
        => Operator switch {
            "<" => new Version(version) < ComparedTo,
            "<=" => new Version(version) <= ComparedTo,
            "=" => new Version(version) == ComparedTo,
            ">=" => new Version(version) >= ComparedTo,
            ">" => new Version(version) > ComparedTo,
            _ => throw new Exception()
        };

    /// <summary>
    /// Creates a Dependency from a string entry
    /// </summary>
    /// <param name="entry">String entry</param>
    public Dependency(string entry) {
        Type = TypeEnum.Hard;
        var comparedTo = false;
        foreach (var i in entry.Split(" "))
            switch (i) {
                case "!":
                    Type = TypeEnum.Incompatibility;
                    break;
                case "?":
                    Type = TypeEnum.Optional;
                    break;
                case "(?)":
                    Type = TypeEnum.HiddenOptional;
                    break;
                case "~":
                    Type = TypeEnum.IgnoreLoadOrder;
                    break;
                case "<":
                case "<=":
                case "=":
                case ">=":
                case ">":
                    comparedTo = true;
                    Operator = i;
                    break;
                default:
                    if (comparedTo)
                        ComparedTo = new Version(i);
                    else ModName += i + " ";
                    break;
            }

        ModName = ModName![..^1];
    }
}