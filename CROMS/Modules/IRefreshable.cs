namespace CROMS.Modules
{
    /// <summary>
    /// Implemented by module forms that show data which can change while another
    /// module is in use (e.g. Release &amp; Claim's pending list changes when a
    /// Certificate Request is created). The shell calls <see cref="RefreshData"/>
    /// every time the module is navigated to, so a cached form never shows stale data.
    /// </summary>
    public interface IRefreshable
    {
        void RefreshData();
    }
}
