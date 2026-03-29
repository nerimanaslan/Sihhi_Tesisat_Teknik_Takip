using System;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	internal static class Program
	{
		/// <summary>
		/// Uygulamanın ana girdi noktası.
		/// </summary>
		[STAThread]
		static void Main ()
		{
			AppDomain.CurrentDomain.SetData("DataDirectory" , AppDomain.CurrentDomain.BaseDirectory);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//Application.SetHighDpiMode(HighDpiMode.SystemAware);
			AppAuthentication.VarsayilanKullanicilariHazirla();

			using(LoginForm loginForm = new LoginForm())
			{
				if(loginForm.ShowDialog()!=DialogResult.OK||loginForm.AuthenticatedUser==null)
					return;

				Application.Run(new Form1(loginForm.AuthenticatedUser));
			}
		}
	}
}
