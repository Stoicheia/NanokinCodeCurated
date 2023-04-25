using UnityEditor;

namespace Util
{
	/// <summary>
	/// There is currently a major and enigtmatic bug
	/// where all lua assets will not be referenced correctly until after a domain reload.
	///
	/// When auto-refresh is enabled, it seems that Unity automatically makes a domain reload on startup,
	/// so the bug is only apparent when auto-refresh is disabled.
	///
	/// Therefore, we will force a domain reload on startup when auto-refresh is disabled.
	/// </summary>
	[InitializeOnLoad]
	public static class DomainReloadOnStartupWhenNoAutorefresh
	{
		static DomainReloadOnStartupWhenNoAutorefresh()
		{
			if (EditorPrefs.GetBool("kAutoRefresh"))
			{
				// Auto-refresh is enabled, so we don't need to do anything.
				return;
			}

			if (!SessionState.GetBool("DomainReloadOnStartupWhenNoAutorefresh", false))
			{
				// We haven't done a domain reload yet, so do it now.
				// We will also set a flag so that we don't do it again.
				SessionState.SetBool("DomainReloadOnStartupWhenNoAutorefresh", true);
				EditorApplication.delayCall += EditorUtility.RequestScriptReload;
			}
		}
	}
}