using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;

namespace TEKNİK_SERVİS
{
	internal enum KullaniciPanelTuru
	{
		Yonetici,
		Kullanici
	}

	internal sealed class AppUserSession
	{
		public AppUserSession ( string kullaniciAdi , string sifre , string gorunenAd , KullaniciPanelTuru panelTuru )
		{
			KullaniciAdi=kullaniciAdi??string.Empty;
			Sifre=sifre??string.Empty;
			GorunenAd=gorunenAd??KullaniciAdi;
			PanelTuru=panelTuru;
		}

		public string KullaniciAdi { get; }
		public string Sifre { get; }
		public string GorunenAd { get; }
		public KullaniciPanelTuru PanelTuru { get; }

		public bool YoneticiMi
		{
			get { return PanelTuru==KullaniciPanelTuru.Yonetici; }
		}

		public string YetkiMetni
		{
			get { return YoneticiMi ? "YONETICI" : "KULLANICI"; }
		}
	}

	internal static class AppAuthentication
	{
		private static readonly CultureInfo TrKulturu = new CultureInfo("tr-TR");
		private static readonly AppUserSession[] VarsayilanHesapListesi =
		{
			new AppUserSession("SAVAS" , "0689" , "SAVAS" , KullaniciPanelTuru.Kullanici),
			new AppUserSession("ADMIN" , "admin" , "ADMIN" , KullaniciPanelTuru.Yonetici),
			new AppUserSession("NERIMAN" , "1217" , "NERIMAN" , KullaniciPanelTuru.Kullanici)
		};

		internal static IReadOnlyList<AppUserSession> VarsayilanHesaplar
		{
			get { return VarsayilanHesapListesi; }
		}

		internal static AppUserSession VarsayilanYonetici
		{
			get { return VarsayilanHesapListesi.First(x => x.YoneticiMi); }
		}

		internal static AppUserSession KullaniciDogrula ( string kullaniciAdi , string sifre )
		{
			string normalizeKullanici = KullaniciAdiniNormalizeEt(kullaniciAdi);
			string temizSifre = ( sifre??string.Empty ).Trim();

			return VarsayilanHesapListesi.FirstOrDefault(x =>
				string.Equals(KullaniciAdiniNormalizeEt(x.KullaniciAdi) , normalizeKullanici , StringComparison.Ordinal)&&
				string.Equals(x.Sifre , temizSifre , StringComparison.Ordinal));
		}

		internal static void VarsayilanKullanicilariHazirla ()
		{
			string connStr = Properties.Settings.Default.TeknikServisSConnectionString;
			if(string.IsNullOrWhiteSpace(connStr))
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					foreach(AppUserSession hesap in VarsayilanHesapListesi)
					{
						using(OleDbCommand updateCmd = new OleDbCommand(
							"UPDATE [Kullanicilar] SET [Sifre]=?, [AktifMi]=?, [Yetki]=? WHERE UCASE(IIF([KullaniciAdi] IS NULL, '', [KullaniciAdi]))=?" ,
							conn))
						{
							updateCmd.Parameters.AddWithValue("?" , hesap.Sifre);
							updateCmd.Parameters.AddWithValue("?" , true);
							updateCmd.Parameters.AddWithValue("?" , hesap.YetkiMetni);
							updateCmd.Parameters.AddWithValue("?" , KullaniciAdiniNormalizeEt(hesap.KullaniciAdi));

							int etkilenen = updateCmd.ExecuteNonQuery();
							if(etkilenen>0)
								continue;
						}

						using(OleDbCommand insertCmd = new OleDbCommand(
							"INSERT INTO [Kullanicilar] ([KullaniciAdi], [Sifre], [PersonelID], [AktifMi], [Yetki]) VALUES (?, ?, ?, ?, ?)" ,
							conn))
						{
							insertCmd.Parameters.AddWithValue("?" , hesap.KullaniciAdi);
							insertCmd.Parameters.AddWithValue("?" , hesap.Sifre);
							insertCmd.Parameters.AddWithValue("?" , DBNull.Value);
							insertCmd.Parameters.AddWithValue("?" , true);
							insertCmd.Parameters.AddWithValue("?" , hesap.YetkiMetni);
							insertCmd.ExecuteNonQuery();
						}
					}
				}
			}
			catch
			{
			}
		}

		private static string KullaniciAdiniNormalizeEt ( string kullaniciAdi )
		{
			return ( kullaniciAdi??string.Empty ).Trim().ToUpper(TrKulturu);
		}
	}
}
