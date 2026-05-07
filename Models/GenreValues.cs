using System;
using System.Collections.Generic;
using System.Linq;

namespace CreativeWrites.Models
{
    /// <summary>
    /// Static helper that exposes a Genre[] so XAML ItemsSource can bind to it.
    /// Usage: ItemsSource="{x:Static models:GenreValues.All}"
    /// </summary>
    public static class GenreValues
    {
        public static IReadOnlyList<Genre> All { get; } = Enum.GetValues<Genre>().ToArray();
    }
}
