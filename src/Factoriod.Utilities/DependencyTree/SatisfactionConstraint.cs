namespace Factoriod.Utilities.DependencyTree;

/// <summary>
/// Satisfaction constraints determine whether a dependency must, cannot, or may be satisfied.
/// </summary>
public enum SatisfactionConstraint
{
    /// <summary>
    /// A required dependency must be satisfied.
    /// </summary>
    Required,

    /// <summary>
    /// An incompatible dependency cannot be satisfied.
    /// </summary>
    Incompatible,

    /// <summary>
    /// An optional dependency may be satisfied.
    /// </summary>
    Optional,
}

/// <summary>
/// A version operator compares two versions for compatibility.
/// </summary>
public enum VersionOperator
{
    /// <summary>
    /// The dependency's version must be less than the indicated version.
    /// </summary>
    LessThan,

    /// <summary>
    /// The dependency's version must be less than or equal to the indicated version.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// The dependency's version must be equal to the indicated version.
    /// </summary>
    Equal,

    /// <summary>
    /// The dependency's version must be greater than or equal to the indicated version.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// The dependency's version must be greater than the indicated version.
    /// </summary>
    GreaterThan,
}

/// <summary>
/// A version constraint for a dependency compares a candidate version with <paramref name="Version"/> using <paramref name="Operator"/> to determine whether the candidate version is compatible.
/// </summary>
/// <param name="Operator">The operator to apply to the <paramref name="Version"/>.</param>
/// <param name="Version">The version to constrain against.</param>
/// <exception cref="ArgumentOutOfRangeException">If <paramref name="Operator"/> is not a defined value of <see cref="VersionOperator"/>.</exception>
public record VersionConstraint(VersionOperator Operator, Version Version)
{
    /// <summary>
    /// The operator to apply to the <see cref="Version"/>.
    /// </summary>
    public VersionOperator Operator { get; } = Enum.IsDefined(Operator) 
        ? Operator 
        : throw new ArgumentOutOfRangeException(nameof(Operator), $"Unknown operator: {Operator}");

    /// <summary>
    /// Compares a candidate version for compatibility with <see cref="Version"/>.
    /// </summary>
    /// <param name="candidateVersion">The candidate version to compare <see cref="Version"/> against using <see cref="Operator"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="candidateVersion"/> is compatible, <see langword="false"/> otherwise.</returns>
    public bool IsCompatible(Version candidateVersion) => Operator switch
    {
        VersionOperator.LessThan => candidateVersion < Version,
        VersionOperator.LessThanOrEqual => candidateVersion <= Version,
        VersionOperator.Equal => candidateVersion == Version,
        VersionOperator.GreaterThanOrEqual => candidateVersion >= Version,
        VersionOperator.GreaterThan => candidateVersion > Version,
        _ => throw new NotImplementedException($"Unknown operator: {Operator}"),
    };
}

/// <summary>
/// A dependency with optional satisfaction and version constraints.
/// </summary>
/// <param name="Name">The name of the dependency.</param>
/// <param name="SatisfactionConstraint">The satisfaction constraint. Optional: defaults to required.</param>
/// <param name="VersionConstraint">The version constraint. Optional: defaults to any version.</param>
public record Dependency(
    string Name,
    SatisfactionConstraint SatisfactionConstraint = SatisfactionConstraint.Required,
    VersionConstraint? VersionConstraint = null);

/// <summary>
/// A resolved dependency satisfies all constraints and provides the maximum compatible version.
/// </summary>
/// <param name="Name">The name of the dependency.</param>
/// <param name="Version">The maximum compatible version.</param>
public record ResolvedDependency(string Name, Version Version);

/// <summary>
/// A version of a package with optional dependencies.
/// </summary>
/// <param name="Version">The package's version.</param>
/// <param name="Dependencies">The dependencies for this package's version. Optional: defaults to no dependencies.</param>
public record PackageVersion(Version Version, IReadOnlySet<Dependency>? Dependencies = null)
{
    public IReadOnlySet<Dependency> Dependencies { get; } = Dependencies?.ToHashSet() ?? new HashSet<Dependency>();
}

/// <summary>
/// A package with available versions.
/// </summary>
/// <param name="Name">The name of the package.</param>
/// <param name="Versions">The available versions for this package.</param>
public record Package(string Name, IReadOnlyCollection<PackageVersion> Versions)
{
    /// <summary>
    /// The available versions for this package, in descending order by <see cref="PackageVersion.Version"/>.
    /// </summary>
    public IReadOnlyCollection<PackageVersion> Versions { get; } = Versions.OrderByDescending(v => v.Version).ToList();
}

public interface IPackageFetcher
{
    Task<IEnumerable<Package>> GetPackageVersions(string Name);
}

/// <summary>
/// A tree of dependencies with constraints.
/// </summary>
public class DependencyTree
{
    /// <summary>
    /// The requested packages. May not include dependencies.
    /// </summary>
    private readonly IReadOnlyCollection<Package> requestedPackages;

    /// <summary>
    /// An object to fetch additional package information.
    /// </summary>
    private readonly IPackageFetcher packageFetcher;

    /// <summary>
    /// Create a dependency tree from a minimal set of requested packages.
    /// </summary>
    /// <param name="requestedPackages">The minimal set of packages to resolve.</param>
    /// <param name="packageFetcher">An object to fetch additional package information.</param>
    public DependencyTree(IReadOnlyCollection<Package> requestedPackages, IPackageFetcher packageFetcher)
    {
        this.requestedPackages = requestedPackages;
        this.packageFetcher = packageFetcher;
    }

    /// <summary>
    /// Resolves a dependency tree. The returned enumerable contains dependencies and their maximum compatible version.
    /// The tree may possibly be unresolvable if there are conflicting constraints.
    /// </summary>
    /// <returns>An enumerable of dependencies with their maximum compatible version, or <see langword="null"/> if the tree is not resolvable.</returns>
    public IEnumerable<ResolvedDependency>? Resolve()
    {
        var map = new Dictionary<string, TreeNode>();
        var root = new TreeNode(null, new List<TreeNode>());

        // TODO: resolve all dependencies for requestedPackages into resolvedPackages
        var toAdd = new Queue<string>();
        foreach (var package in requestedPackages)
        {
            TreeNode node;
            if (map.ContainsKey(package.Name))
            {
                node = map[package.Name];
            }
            else
            {
                node = new TreeNode(package, new List<TreeNode>());
                root.Children.Add(node);
                map.Add(package.Name, node);
            }
        }

        // TODO: identify incompatiblities and return null
        // TODO: identify conflicting version constraints nd return null
        // TODO: identify maximum version for all resolvedPackages
        return null;
    }


    private record TreeNode(Package? Package, List<TreeNode> Children);
}