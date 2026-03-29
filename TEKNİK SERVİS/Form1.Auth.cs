using System;

namespace TEKNİK_SERVİS
{
	public partial class Form1
	{
		private readonly AppUserSession _aktifKullanici;

		internal Form1 ( AppUserSession aktifKullanici )
			: this()
		{
			_aktifKullanici=aktifKullanici;
		}

		private void KullaniciOturumunuUygula ()
		{
			AppUserSession aktifKullanici = _aktifKullanici;
			if(aktifKullanici==null)
				return;

			Text="AST SIHHI TESISAT | "+aktifKullanici.GorunenAd;
		}
	}
}
