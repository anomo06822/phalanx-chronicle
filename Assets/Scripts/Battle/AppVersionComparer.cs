using System;

namespace PhalanxChronicle.Battle
{
    public static class AppVersionComparer
    {
        public static int Compare(string left, string right)
        {
            if (!TryParse(left, out SemanticVersion leftVersion) ||
                !TryParse(right, out SemanticVersion rightVersion))
            {
                return 0;
            }

            return leftVersion.CompareTo(rightVersion);
        }

        public static string Normalize(string version)
        {
            return TryParse(version, out SemanticVersion normalizedVersion)
                ? normalizedVersion.ToString()
                : string.Empty;
        }

        public static bool ShouldPrompt(string currentVersion, string latestTag, string dismissedTag)
        {
            string normalizedLatest = Normalize(latestTag);
            if (string.IsNullOrWhiteSpace(normalizedLatest) ||
                Compare(normalizedLatest, currentVersion) <= 0)
            {
                return false;
            }

            string normalizedDismissed = Normalize(dismissedTag);
            return !string.Equals(normalizedLatest, normalizedDismissed, StringComparison.Ordinal);
        }

        public static bool TryParse(string version, out SemanticVersion semanticVersion)
        {
            semanticVersion = default;
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            string normalized = version.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(1);
            }

            int metadataIndex = normalized.IndexOfAny(new[] { '-', '+' });
            if (metadataIndex >= 0)
            {
                normalized = normalized.Substring(0, metadataIndex);
            }

            string[] rawParts = normalized.Split('.');
            if (rawParts.Length == 0 || rawParts.Length > 3)
            {
                return false;
            }

            int[] parts = new int[3];
            for (int index = 0; index < rawParts.Length; index++)
            {
                if (!int.TryParse(rawParts[index], out parts[index]) || parts[index] < 0)
                {
                    return false;
                }
            }

            semanticVersion = new SemanticVersion(parts[0], parts[1], parts[2]);
            return true;
        }

        public readonly struct SemanticVersion : IComparable<SemanticVersion>
        {
            public SemanticVersion(int major, int minor, int patch)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
            }

            public int Major { get; }

            public int Minor { get; }

            public int Patch { get; }

            public int CompareTo(SemanticVersion other)
            {
                if (Major != other.Major)
                {
                    return Major.CompareTo(other.Major);
                }

                if (Minor != other.Minor)
                {
                    return Minor.CompareTo(other.Minor);
                }

                return Patch.CompareTo(other.Patch);
            }

            public override string ToString()
            {
                return $"{Major}.{Minor}.{Patch}";
            }
        }
    }
}
