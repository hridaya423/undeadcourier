public static class QualityTier
{
    public enum Tier
    {
        Web,
        Desktop
    }

#if UNITY_WEBGL
    public static Tier Current => Tier.Web;
#else
    public static Tier Current => Tier.Desktop;
#endif

    public static int MaxActiveEnemies => Current == Tier.Web ? 15 : 30;
    public static int MaxCorpses => Current == Tier.Web ? 4 : 12;
    public static int MaxDecals => Current == Tier.Web ? 24 : 96;
}
