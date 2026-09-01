#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // netstandard2.0 n'expose pas ce type marqueur, requis par le compilateur
    // pour les accesseurs `init` et les records (utilises dans Threshold, Appearance, SkillIcon).
    internal static class IsExternalInit
    {
    }
}
#endif
