using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WinClicker.Models
{
    public enum KeyCategory
    {
        Modifier,    // Win, Ctrl, Alt, Shift
        FunctionKey, // F1-F24
        Regular      // Letters, digits, special chars, arrows, etc.
    }

    [method: SetsRequiredMembers]
    public class ShortcutKeyItem(string displayName, int sortOrder, KeyCategory category = KeyCategory.Regular, bool isInvalid = false, string? glyph = null, Geometry? iconGeometry = null) : IComparable<ShortcutKeyItem>
    {
        public required string DisplayName { get; set; } = displayName;
        public int SortOrder { get; set; } = sortOrder;
        public KeyCategory Category { get; set; } = category;
        public bool IsInvalid { get; set; } = isInvalid;
        public string? Glyph { get; set; } = glyph;
        public Geometry? IconGeometry { get; set; } = iconGeometry;

        public Visibility GlyphVisibility => !string.IsNullOrEmpty(Glyph) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PathVisibility => IconGeometry != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextVisibility => string.IsNullOrEmpty(Glyph) && IconGeometry == null ? Visibility.Visible : Visibility.Collapsed;

        public int CompareTo(ShortcutKeyItem? other)
        {
            if (other == null)
            {
                return 1;
            }

            return SortOrder.CompareTo(other.SortOrder);
        }
    }
}
