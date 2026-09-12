namespace WinClicker.Models
{
    public class ClickSettings
    {
        public int IntervalMs { get; set; } = 100;

        public bool UseRandomOffset { get; set; }
        public double RandomOffsetValue { get; set; } = 40;

        public bool UseCurrentPosition { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public string MouseButton { get; set; } = "Left";
        public string ClickType { get; set; } = "Single";

        public bool IsCustomRepeat { get; set; }
        public string RepeatCount { get; set; } = "10";
    }
}
