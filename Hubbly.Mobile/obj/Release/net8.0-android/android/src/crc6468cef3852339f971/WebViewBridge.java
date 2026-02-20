package crc6468cef3852339f971;


public class WebViewBridge
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_PostMessage:(Ljava/lang/String;)V:__export__\n" +
			"";
		mono.android.Runtime.register ("Hubbly.Mobile.Platforms.Android.WebViewBridge, Hubbly.Mobile", WebViewBridge.class, __md_methods);
	}


	public WebViewBridge ()
	{
		super ();
		if (getClass () == WebViewBridge.class) {
			mono.android.TypeManager.Activate ("Hubbly.Mobile.Platforms.Android.WebViewBridge, Hubbly.Mobile", "", this, new java.lang.Object[] {  });
		}
	}

	@android.webkit.JavascriptInterface

	public void postMessage (java.lang.String p0)
	{
		n_PostMessage (p0);
	}

	private native void n_PostMessage (java.lang.String p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
