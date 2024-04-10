namespace NPMRegistry;

public static class Utils
{
    public static IEnumerable<TSource> OrderBySemanticVersion<TSource>(this IEnumerable<TSource> source, Func<TSource, string> versionSelect) =>
        from item in source
        let version = versionSelect(item)
        let versionSplit = version.Split(".")
        let semanticVersion = new
        {
            Major = int.Parse(versionSplit[0]),
            Minor = int.Parse(versionSplit[1]),
            Patch = int.Parse(versionSplit[2]),
        }
        orderby semanticVersion.Major descending, semanticVersion.Minor descending, semanticVersion.Patch
            descending
        select item;
}