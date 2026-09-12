using System;

namespace CROMS.Analytics
{
    /// <summary>
    /// One chart = one UserControl implementing this. The contract exists so the SAME
    /// control can be hosted by an Analytics tab and by the Dashboard without the chart
    /// code being copied: both hosts only ever call <see cref="Load"/> and read
    /// <see cref="HasSufficientData"/>.
    ///
    /// <see cref="HasSufficientData"/> is false whenever the widget fell back to an empty
    /// state (the source column is unfilled, or the range holds nothing), which is how the
    /// Dashboard decides to skip a widget rather than show the office a blank panel.
    /// </summary>
    public interface IAnalyticsWidget
    {
        /// <summary>Human title shown in the widget header (and used in tooltips).</summary>
        string Title { get; }

        /// <summary>Re-queries and re-renders for the given inclusive date range.</summary>
        void Load(DateTime from, DateTime to);

        /// <summary>False when the widget is showing an empty/insufficient-data state.</summary>
        bool HasSufficientData { get; }
    }
}
