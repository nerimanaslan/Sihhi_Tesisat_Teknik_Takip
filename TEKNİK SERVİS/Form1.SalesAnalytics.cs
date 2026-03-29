using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	public partial class Form1
	{
		private const string YeniGunlukSatisTabloAdi = "SatisGunlukKayitlari";
		private const string EskiGunlukSatisTabloAdi = "GunlukSatislar";
		private const string GunlukSatisKayitTuruSatis = "SATIS";
		private const string GunlukSatisKayitTuruIade = "IADE";
		private string _gunlukSatisTabloAdi = EskiGunlukSatisTabloAdi;

		private sealed class SatisOzetSatiri
		{
			public int? GunlukSatisId;
			public DateTime Tarih;
			public int UrunId;
			public string UrunAdi;
			public string BirimAdi;
			public decimal Miktar;
			public decimal BirimFiyat;
			public decimal BirimMaliyet;
			public decimal Ciro;
			public decimal ToplamMaliyet;
			public string Kaynak;
			public string Aciklama;
			public string KayitTuru;

			public decimal KarTutari
			{
				get { return Ciro-ToplamMaliyet; }
			}

			public decimal KarOrani
			{
				get { return Ciro<=0m ? 0m : ( KarTutari*100m )/Ciro; }
			}
		}

		private sealed class SatisOzetToplami
		{
			public decimal Ciro;
			public decimal Kar;
			public decimal Miktar;
			public int UrunCesidi;

			public decimal KarOrani
			{
				get { return Ciro<=0m ? 0m : ( Kar*100m )/Ciro; }
			}
		}

		private sealed class GenelToplamSatiri
		{
			public string KayitTuru;
			public string AdSoyad;
			public decimal Ciro;
			public decimal ToplamMaliyet;
			public decimal KarTutari;
			public decimal ToplamAlim;
			public decimal ToplamOdeme;
			public decimal KalanBakiye;
			public string Aciklama;
		}

		private sealed class AylikFaturaOzetSatiri
		{
			public int FaturaId;
			public DateTime Tarih;
			public string CariAdi;
			public string CariTelefon;
			public int KalemSayisi;
			public decimal ToplamTutar;
		}

		private sealed class AylikFaturaOzetToplami
		{
			public decimal ToplamTutar;
			public int FaturaSayisi;
			public int KalemSayisi;

			public decimal OrtalamaTutar
			{
				get { return FaturaSayisi<=0 ? 0m : ToplamTutar/FaturaSayisi; }
			}
		}

		private bool _gunlukSatisSekmesiHazir;
		private bool _gunlukSatisAltyapiHazir;
		private bool _gunlukSatisAltyapiHatasiGosterildi;
		private bool _gunlukSatisUrunComboDolduruluyor;
		private bool _iadeUrunComboDolduruluyor;
		private bool _gunlukSatisFormHesaplaniyor;
		private bool _iadeFormHesaplaniyor;
		private bool _gunlukSatisTarihEsitleniyor;
		private int? _seciliGunlukSatisId;
		private int? _seciliIadeKaydiId;

		private static string GunlukSatisKayitTuruGetir ( bool iade )
		{
			return iade ? GunlukSatisKayitTuruIade : GunlukSatisKayitTuruSatis;
		}

		private static string GunlukSatisKayitTurunuCoz ( string kayitTuru , decimal miktar )
		{
			string normalKayitTuru = ( kayitTuru??string.Empty ).Trim().ToUpperInvariant();
			if(normalKayitTuru==GunlukSatisKayitTuruIade||normalKayitTuru==GunlukSatisKayitTuruSatis)
				return normalKayitTuru;

			return miktar<0m ? GunlukSatisKayitTuruIade : GunlukSatisKayitTuruSatis;
		}

		private static bool GunlukSatisKayitTuruEslesiyor ( string kayitTuru , string hedefKayitTuru , decimal miktar )
		{
			return string.Equals(
				GunlukSatisKayitTurunuCoz(kayitTuru , miktar) ,
				( hedefKayitTuru??string.Empty ).Trim().ToUpperInvariant() ,
				StringComparison.Ordinal);
		}

		private void EnsureGunlukSatisAltyapi ()
		{
			GunlukSatisAltyapisiniHazirla(false);
		}

		private void GunlukSatisTasarimYuzeyiniHazirla ()
		{
			if(!TasarimModundaCalisiyorMu()||tabControl1==null||_gunlukSatisSekmesiHazir)
				return;

			try
			{
				int hedefIndex = tabPage9!=null ? tabControl1.TabPages.IndexOf(tabPage9)+1 : tabControl1.TabPages.Count;
				_satisRaporTabPage=TabPageGetirVeyaOlustur(tabControl1 , "Satış" , hedefIndex);
				_satisRaporTabPage.BackColor=Color.FromArgb(241 , 245 , 249);
				_satisRaporTabPage.Padding=Padding.Empty;

				_satisRaporAltTabControl=_satisRaporTabPage.Controls.OfType<TabControl>().FirstOrDefault();
				if(_satisRaporAltTabControl==null)
				{
					_satisRaporAltTabControl=new TabControl
					{
						Dock=DockStyle.Fill,
						Name="tabControlGunlukSatisRaporlari"
					};
					_satisRaporTabPage.Controls.Add(_satisRaporAltTabControl);
				}

				_satisRaporAltTabControl.SuspendLayout();
				try
				{
					_satisRaporAltTabControl.TabPages.Clear();

					_gunlukSatisTabPage=SatisRaporAltTabOlustur("Günlük Satış");
					_iadeTabPage=SatisRaporAltTabOlustur("İade");
					_gunlukSatisToplamTabPage=SatisRaporAltTabOlustur("Günlük Özet");
					_aylikSatisTabPage=SatisRaporAltTabOlustur("Aylık Satış");
					_aylikFabrikaFaturaTabPage=SatisRaporAltTabOlustur("Aylık Fabrika");
					_aylikMusteriFaturaTabPage=SatisRaporAltTabOlustur("Aylık Müşteri");
					_toplamSatisTabPage=SatisRaporAltTabOlustur("Genel Satış");
					_genelToplamTabPage=SatisRaporAltTabOlustur("Finans Özeti");

					_satisRaporAltTabControl.TabPages.Add(_gunlukSatisTabPage);
					_satisRaporAltTabControl.TabPages.Add(_iadeTabPage);
					_satisRaporAltTabControl.TabPages.Add(_gunlukSatisToplamTabPage);
					_satisRaporAltTabControl.TabPages.Add(_aylikSatisTabPage);
					_satisRaporAltTabControl.TabPages.Add(_aylikFabrikaFaturaTabPage);
					_satisRaporAltTabControl.TabPages.Add(_aylikMusteriFaturaTabPage);
					_satisRaporAltTabControl.TabPages.Add(_toplamSatisTabPage);
					_satisRaporAltTabControl.TabPages.Add(_genelToplamTabPage);
				}
				finally
				{
					_satisRaporAltTabControl.ResumeLayout();
				}

				GunlukSatisSayfasiniOlustur();
				IadeSayfasiniOlustur();
				GunlukSatisToplamSayfasiniOlustur();
				AylikSatisSayfasiniOlustur();
				AylikFabrikaFaturaSayfasiniOlustur();
				AylikMusteriFaturaSayfasiniOlustur();
				ToplamSatisSayfasiniOlustur();
				GenelToplamSayfasiniOlustur();
			}
			catch
			{
				// Tasarım yüzeyi açılırken satış sekmesi kurulum hatası tasarım ekranını engellemesin.
			}
		}

		private static bool TasarimciIslemindeCalisiyorMu ()
		{
			try
			{
				string islemAdi = Process.GetCurrentProcess().ProcessName;
				if(string.IsNullOrWhiteSpace(islemAdi))
					return false;

				return islemAdi.IndexOf("devenv" , StringComparison.OrdinalIgnoreCase)>=0
					|| islemAdi.IndexOf("xdesproc" , StringComparison.OrdinalIgnoreCase)>=0
					|| islemAdi.IndexOf("designtoolsserver" , StringComparison.OrdinalIgnoreCase)>=0;
			}
			catch
			{
				return false;
			}
		}

		private bool TasarimModundaCalisiyorMu ()
		{
			return LicenseManager.UsageMode==LicenseUsageMode.Designtime
				|| ( Site?.DesignMode??false )
				|| TasarimciIslemindeCalisiyorMu();
		}

		private bool GunlukSatisAltyapisiniHazirla ( bool hataGoster )
		{
			if(_gunlukSatisAltyapiHazir)
				return true;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string tabloAdi = GunlukSatisTabloAdiniBelirle(conn);
					if(string.IsNullOrWhiteSpace(tabloAdi))
					{
						tabloAdi=GunlukSatisTablosunuOlustur(conn);
						_gunlukSatisTabloAdi=tabloAdi;
						GunlukSatisEskiTablodanVeriTasi(conn , tabloAdi);
					}

					HashSet<string> mevcutKolonlar = GunlukSatisKolonAdlariniGetir(conn);
					GunlukSatisEksikKolonlariTamamla(conn , mevcutKolonlar);
				}

				_gunlukSatisAltyapiHazir=true;
				_gunlukSatisAltyapiHatasiGosterildi=false;
				return true;
			}
			catch(Exception ex)
			{
				_gunlukSatisAltyapiHazir=false;
				if(hataGoster&&!_gunlukSatisAltyapiHatasiGosterildi)
				{
					_gunlukSatisAltyapiHatasiGosterildi=true;
					MessageBox.Show("Günlük satış altyapısı oluşturulamadı: "+ex.Message);
				}

				return false;
			}
		}

		private string GunlukSatisTabloAdiniBelirle ( OleDbConnection conn )
		{
			if(conn==null)
				return null;

			if(TabloVarMi(conn , EskiGunlukSatisTabloAdi))
			{
				_gunlukSatisTabloAdi=EskiGunlukSatisTabloAdi;
				return _gunlukSatisTabloAdi;
			}

			if(TabloVarMi(conn , YeniGunlukSatisTabloAdi))
			{
				_gunlukSatisTabloAdi=YeniGunlukSatisTabloAdi;
				return _gunlukSatisTabloAdi;
			}

			return null;
		}

		private string GunlukSatisTabloAdiniGetir ()
		{
			return string.IsNullOrWhiteSpace(_gunlukSatisTabloAdi) ? EskiGunlukSatisTabloAdi : _gunlukSatisTabloAdi;
		}

		private string GunlukSatisTablosunuOlustur ( OleDbConnection conn )
		{
			if(conn==null)
				return null;

			string[] adayTablolar = { YeniGunlukSatisTabloAdi , EskiGunlukSatisTabloAdi };
			foreach(string tabloAdi in adayTablolar)
			{
				try
				{
					using(OleDbCommand cmd = new OleDbCommand(
						"CREATE TABLE ["+tabloAdi+"] ([GunlukSatisID] AUTOINCREMENT, [SatisTarihi] DATETIME, [UrunID] LONG, [Miktar] DOUBLE, [BirimSatisFiyati] CURRENCY, [BirimMaliyet] CURRENCY, [ToplamTutar] CURRENCY, [ToplamMaliyet] CURRENCY, [Aciklama] LONGTEXT, [KayitTuru] TEXT(20), [SonGuncellemeTarihi] DATETIME)" ,
						conn))
					{
						cmd.ExecuteNonQuery();
					}

					return tabloAdi;
				}
				catch
				{
					if(TabloVarMi(conn , tabloAdi))
						return tabloAdi;
				}
			}

			throw new InvalidOperationException("Günlük satış tablosu oluşturulamadı.");
		}

		private void GunlukSatisEskiTablodanVeriTasi ( OleDbConnection conn , string hedefTabloAdi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(hedefTabloAdi)||string.Equals(hedefTabloAdi , EskiGunlukSatisTabloAdi , StringComparison.OrdinalIgnoreCase)||!TabloVarMi(conn , EskiGunlukSatisTabloAdi)||!TabloVarMi(conn , hedefTabloAdi))
				return;

			try
			{
				string sorgu = "INSERT INTO ["+hedefTabloAdi+"] (SatisTarihi, UrunID, Miktar, BirimSatisFiyati, BirimMaliyet, ToplamTutar, ToplamMaliyet, Aciklama, SonGuncellemeTarihi) " +
					"SELECT SatisTarihi, UrunID, Miktar, BirimSatisFiyati, BirimMaliyet, ToplamTutar, ToplamMaliyet, Aciklama, SonGuncellemeTarihi FROM ["+EskiGunlukSatisTabloAdi+"]";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
					cmd.ExecuteNonQuery();
			}
			catch
			{
				// Eski tablo erişilemiyorsa yeni yapıyla devam et.
			}
		}

		private void GunlukSatisKolonunuEkle ( OleDbConnection conn , string kolonAdi , string veriTipi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(kolonAdi)||string.IsNullOrWhiteSpace(veriTipi))
				return;

			if(GunlukSatisKolonuVarMi(conn , kolonAdi))
				return;

			using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE ["+GunlukSatisTabloAdiniGetir()+"] ADD COLUMN ["+kolonAdi+"] "+veriTipi , conn))
				cmd.ExecuteNonQuery();
		}

		private void GunlukSatisEksikKolonlariTamamla ( OleDbConnection conn , HashSet<string> mevcutKolonlar )
		{
			if(conn==null)
				return;

			HashSet<string> kolonlar = mevcutKolonlar??new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Tuple<string, string>[] beklenenKolonlar = new[]
			{
				Tuple.Create("SatisTarihi" , "DATETIME"),
				Tuple.Create("UrunID" , "LONG"),
				Tuple.Create("Miktar" , "DOUBLE"),
				Tuple.Create("BirimSatisFiyati" , "CURRENCY"),
				Tuple.Create("BirimMaliyet" , "CURRENCY"),
				Tuple.Create("ToplamTutar" , "CURRENCY"),
				Tuple.Create("ToplamMaliyet" , "CURRENCY"),
				Tuple.Create("Aciklama" , "LONGTEXT"),
				Tuple.Create("KayitTuru" , "TEXT(20)"),
				Tuple.Create("SonGuncellemeTarihi" , "DATETIME")
			};

			foreach(Tuple<string, string> kolon in beklenenKolonlar)
			{
				if(kolonlar.Contains(kolon.Item1))
					continue;

				GunlukSatisKolonunuEkle(conn , kolon.Item1 , kolon.Item2);
				kolonlar.Add(kolon.Item1);
			}

			if(kolonlar.Contains("KayitTuru"))
			{
				using(OleDbCommand cmd = new OleDbCommand(
					"UPDATE ["+GunlukSatisTabloAdiniGetir()+"] " +
					"SET [KayitTuru]=IIF(IIF([Miktar] IS NULL, 0, [Miktar])<0, '"+GunlukSatisKayitTuruIade+"', '"+GunlukSatisKayitTuruSatis+"') " +
					"WHERE [KayitTuru] IS NULL OR Trim([KayitTuru])=''" ,
					conn))
				{
					cmd.ExecuteNonQuery();
				}
			}
		}

		private bool GunlukSatisKolonuVarMi ( OleDbConnection conn , string kolonAdi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(kolonAdi))
				return false;

			return GunlukSatisKolonAdlariniGetir(conn).Contains(kolonAdi);
		}

		private HashSet<string> GunlukSatisKolonAdlariniGetir ( OleDbConnection conn )
		{
			HashSet<string> kolonlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string tabloAdi = GunlukSatisTabloAdiniBelirle(conn);
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi)||!TabloVarMi(conn , tabloAdi))
				return kolonlar;

			using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM ["+tabloAdi+"] WHERE 1=0" , conn))
			using(OleDbDataReader rd = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
			{
				DataTable schema = rd?.GetSchemaTable();
				if(schema==null)
					return kolonlar;

				foreach(DataRow satir in schema.Rows
					.Cast<DataRow>()
					.Where(x => x!=null))
				{
					string kolonAdi = Convert.ToString(satir["ColumnName"]);
					if(!string.IsNullOrWhiteSpace(kolonAdi))
						kolonlar.Add(kolonAdi);
				}
			}

			return kolonlar;
		}

		#if false
		private void KurGunlukSatisSekmesi ()
		{
			if(_gunlukSatisSekmesiHazir||tabControl1==null)
				return;

			int hedefIndex = tabPage4!=null ? tabControl1.TabPages.IndexOf(tabPage4)+1 : tabControl1.TabPages.Count;
			_satisRaporTabPage=TabPageGetirVeyaOlustur(tabControl1 , "Satış" , hedefIndex);
			_satisRaporTabPage.BackColor=Color.FromArgb(241 , 245 , 249);
			_satisRaporTabPage.Padding=Padding.Empty;

			_satisRaporAltTabControl=_satisRaporTabPage.Controls.OfType<TabControl>().FirstOrDefault();
			if(_satisRaporAltTabControl==null)
			{
				_satisRaporAltTabControl=new TabControl
				{
					Dock=DockStyle.Fill,
					Name="tabControlGunlukSatisRaporlari"
				};
				_satisRaporTabPage.Controls.Add(_satisRaporAltTabControl);
			}

			_satisRaporAltTabControl.SuspendLayout();
			try
			{
				_satisRaporAltTabControl.TabPages.Clear();

				_gunlukSatisTabPage=SatisRaporAltTabOlustur("GÃ¼nlÃ¼k SatÄ±ÅŸ");
				_gunlukSatisToplamTabPage=SatisRaporAltTabOlustur("GÃ¼nlÃ¼k SatÄ±ÅŸ Toplam");
				_aylikSatisTabPage=SatisRaporAltTabOlustur("AylÄ±k");
				_toplamSatisTabPage=SatisRaporAltTabOlustur("Toplam SatÄ±ÅŸ");

				_satisRaporAltTabControl.TabPages.Add(_gunlukSatisTabPage);
				_satisRaporAltTabControl.TabPages.Add(_gunlukSatisToplamTabPage);
				_satisRaporAltTabControl.TabPages.Add(_aylikSatisTabPage);
				_satisRaporAltTabControl.TabPages.Add(_toplamSatisTabPage);
			}
			finally
			{
				_satisRaporAltTabControl.ResumeLayout();
			}

			_satisRaporAltTabControl.SelectedIndexChanged-=SatisRaporAltTabControl_SelectedIndexChanged;
			_satisRaporAltTabControl.SelectedIndexChanged+=SatisRaporAltTabControl_SelectedIndexChanged;

			GunlukSatisSayfasiniOlustur();
			GunlukSatisToplamSayfasiniOlustur();
			AylikSatisSayfasiniOlustur();
			ToplamSatisSayfasiniOlustur();

			_gunlukSatisSekmesiHazir=true;
			GunlukSatisUrunListesiniYenile();
		}

		private TabPage TabPageGetirVeyaOlustur ( TabControl tabControl , string baslik , int hedefIndex )
		{
			if(tabControl==null)
				return null;

			string karsilastirma = KarsilastirmaMetniHazirla(baslik);
			TabPage tabPage = tabControl.TabPages.Cast<TabPage>()
				.FirstOrDefault(x => string.Equals(KarsilastirmaMetniHazirla(x.Text) , karsilastirma , StringComparison.Ordinal));

			if(tabPage==null)
			{
				tabPage=new TabPage(baslik);
				tabControl.TabPages.Add(tabPage);
			}

			tabPage.Text=baslik;
			if(hedefIndex>=0&&hedefIndex<tabControl.TabPages.Count)
				tabControl.Controls.SetChildIndex(tabPage , hedefIndex);

			return tabPage;
		}

		private TabPage SatisRaporAltTabOlustur ( string baslik )
		{
			return new TabPage
			{
				Text=baslik,
				BackColor=Color.FromArgb(241 , 245 , 249),
				Padding=new Padding(14)
			};
		}

		private void GunlukSatisSayfasiniOlustur ()
		{
			if(_gunlukSatisTabPage==null)
				return;

			_gunlukSatisTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=1,
				BackColor=_gunlukSatisTabPage.BackColor
			};
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 74f));
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 26f));

			TableLayoutPanel solLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_gunlukSatisTabPage.BackColor
			};
			solLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			solLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"BUGÃœNKÃœ CÄ°RO" ,
				"BUGÃœNKÃœ KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _gunlukSatisCiroLabel ,
				out _gunlukSatisKarLabel ,
				out _gunlukSatisKarOraniLabel ,
				out _gunlukSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("GÃ¼nlÃ¼k SatÄ±ÅŸ Listesi");
			GroupBox girisKutusu = SatisRaporGroupBoxOlustur("SatÄ±ÅŸ GiriÅŸi");

			Size gunlukSatisFiltreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(gunlukSatisFiltreBoyutu , aramaKutusuBoyutu);
			Label tarihLabel = SatisRaporFiltreEtiketiOlustur("Tarih");
			_gunlukSatisTarihPicker=SatisRaporTarihSeciciOlustur(false , gunlukSatisFiltreBoyutu);
			_gunlukSatisTarihPicker.ValueChanged-=GunlukSatisTarihPicker_ValueChanged;
			_gunlukSatisTarihPicker.ValueChanged+=GunlukSatisTarihPicker_ValueChanged;
			Button yenileButonu = SatisRaporButonuOlustur(
				"Yenile" ,
				gunlukSatisFiltreBoyutu ,
				new Padding(10 , 2 , 10 , 2));
			yenileButonu.Click+=(sender , e) => GunlukSatisVerileriniYenile();
			_gunlukSatisAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_gunlukSatisGrid=SatisRaporGridiOlustur();
			_gunlukSatisGrid.CellClick-=GunlukSatisGrid_CellClick;
			_gunlukSatisGrid.CellClick+=GunlukSatisGrid_CellClick;

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				tarihLabel ,
				_gunlukSatisTarihPicker ,
				yenileButonu ,
				_gunlukSatisAramaKutusu ,
				gunlukSatisFiltreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_gunlukSatisGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			TableLayoutPanel girisLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=8,
				Padding=new Padding(10 , 12 , 10 , 6),
				BackColor=_gunlukSatisTabPage.BackColor
			};
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 118f));
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100f));
			for(int i = 0 ; i<6 ; i++)
				girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 96f));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			_gunlukSatisUrunComboBox=SatisRaporComboBoxOlustur();
			_gunlukSatisUrunComboBox.SelectedIndexChanged-=GunlukSatisUrunComboBox_SelectedIndexChanged;
			_gunlukSatisUrunComboBox.SelectedIndexChanged+=GunlukSatisUrunComboBox_SelectedIndexChanged;
			_gunlukSatisUrunComboBox.TextChanged-=GunlukSatisUrunComboBox_TextChanged;
			_gunlukSatisUrunComboBox.TextChanged+=GunlukSatisUrunComboBox_TextChanged;
			_gunlukSatisUrunComboBox.Leave-=GunlukSatisUrunComboBox_Leave;
			_gunlukSatisUrunComboBox.Leave+=GunlukSatisUrunComboBox_Leave;

			_gunlukSatisBirimTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_gunlukSatisMiktarTextBox=SatisRaporMetinKutusuOlustur(false , false);
			_gunlukSatisBirimFiyatTextBox=SatisRaporMetinKutusuOlustur(false , false);
			_gunlukSatisBirimMaliyetTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_gunlukSatisToplamTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_gunlukSatisNotTextBox=SatisRaporMetinKutusuOlustur(false , true);

			_gunlukSatisMiktarTextBox.Text="1";
			_gunlukSatisBirimFiyatTextBox.Text="0,00";
			_gunlukSatisBirimMaliyetTextBox.Text="0,00";
			_gunlukSatisToplamTextBox.Text="0,00";

			_gunlukSatisMiktarTextBox.KeyPress-=SepetSayisal_KeyPress;
			_gunlukSatisMiktarTextBox.KeyPress+=SepetSayisal_KeyPress;
			_gunlukSatisBirimFiyatTextBox.KeyPress-=SepetSayisal_KeyPress;
			_gunlukSatisBirimFiyatTextBox.KeyPress+=SepetSayisal_KeyPress;
			_gunlukSatisMiktarTextBox.TextChanged-=GunlukSatisHesapAlanlari_TextChanged;
			_gunlukSatisMiktarTextBox.TextChanged+=GunlukSatisHesapAlanlari_TextChanged;
			_gunlukSatisBirimFiyatTextBox.TextChanged-=GunlukSatisHesapAlanlari_TextChanged;
			_gunlukSatisBirimFiyatTextBox.TextChanged+=GunlukSatisHesapAlanlari_TextChanged;

			SatisRaporFormSatiriEkle(girisLayout , 0 , "ÃœrÃ¼n" , _gunlukSatisUrunComboBox);
			SatisRaporFormSatiriEkle(girisLayout , 1 , "Birim" , _gunlukSatisBirimTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 2 , "Miktar" , _gunlukSatisMiktarTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 3 , "SatÄ±ÅŸ FiyatÄ±" , _gunlukSatisBirimFiyatTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 4 , "Birim Maliyet" , _gunlukSatisBirimMaliyetTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 5 , "Toplam" , _gunlukSatisToplamTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 6 , "Not" , _gunlukSatisNotTextBox);

			TableLayoutPanel butonPaneli = new TableLayoutPanel
			{
				Dock=DockStyle.Top,
				ColumnCount=2,
				RowCount=2,
				Height=92,
				Margin=Padding.Empty,
				Padding=new Padding(0 , 8 , 0 , 0),
				BackColor=_gunlukSatisTabPage.BackColor
			};
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50f));
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50f));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));

			Button kaydetButonu = SatisRaporButonuOlustur("Kaydet");
			kaydetButonu.Click-=GunlukSatisKaydetButonu_Click;
			kaydetButonu.Click+=GunlukSatisKaydetButonu_Click;
			SatisRaporButonTemasiniUygula(kaydetButonu , Color.FromArgb(13 , 148 , 136) , Color.White , Color.FromArgb(13 , 148 , 136));
			Button silButonu = SatisRaporButonuOlustur("SeÃ§iliyi Sil");
			silButonu.Click-=GunlukSatisSilButonu_Click;
			silButonu.Click+=GunlukSatisSilButonu_Click;
			SatisRaporButonTemasiniUygula(silButonu , Color.FromArgb(241 , 245 , 249) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			Button temizleButonu = SatisRaporButonuOlustur("Temizle");
			temizleButonu.Click+=(sender , e) => GunlukSatisFormunuTemizle();
			SatisRaporButonTemasiniUygula(temizleButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));

			butonPaneli.Controls.Add(kaydetButonu , 0 , 0);
			butonPaneli.SetColumnSpan(kaydetButonu , 2);
			butonPaneli.Controls.Add(temizleButonu , 0 , 1);
			butonPaneli.Controls.Add(silButonu , 1 , 1);
			girisLayout.Controls.Add(butonPaneli , 1 , 7);

			girisKutusu.Controls.Add(girisLayout);

			solLayout.Controls.Add(kartLayout , 0 , 0);
			solLayout.Controls.Add(listeKutusu , 0 , 1);
			anaLayout.Controls.Add(solLayout , 0 , 0);
			anaLayout.Controls.Add(girisKutusu , 1 , 0);
			_gunlukSatisTabPage.Controls.Add(anaLayout);

			AramaKutusuHazirla(_gunlukSatisAramaKutusu , _gunlukSatisGrid);
		}

		private void GunlukSatisToplamSayfasiniOlustur ()
		{
			if(_gunlukSatisToplamTabPage==null)
				return;

			_gunlukSatisToplamTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_gunlukSatisToplamTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"GÃœNLÃœK CÄ°RO" ,
				"GÃœNLÃœK KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _gunlukSatisToplamCiroLabel ,
				out _gunlukSatisToplamKarLabel ,
				out _gunlukSatisToplamKarOraniLabel ,
				out _gunlukSatisToplamMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("GÃ¼nlÃ¼k Toplam SatÄ±ÅŸ");
			Size filtreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu , aramaKutusuBoyutu);
			Label tarihLabel = SatisRaporFiltreEtiketiOlustur("Tarih");
			_gunlukSatisToplamTarihPicker=SatisRaporTarihSeciciOlustur(false , filtreBoyutu);
			_gunlukSatisToplamTarihPicker.ValueChanged-=GunlukSatisToplamTarihPicker_ValueChanged;
			_gunlukSatisToplamTarihPicker.ValueChanged+=GunlukSatisToplamTarihPicker_ValueChanged;
			_gunlukSatisToplamAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_gunlukSatisToplamGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				tarihLabel ,
				_gunlukSatisToplamTarihPicker ,
				null ,
				_gunlukSatisToplamAramaKutusu ,
				filtreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_gunlukSatisToplamGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_gunlukSatisToplamTabPage.Controls.Add(anaLayout);

			AramaKutusuHazirla(_gunlukSatisToplamAramaKutusu , _gunlukSatisToplamGrid);
		}

		private void AylikSatisSayfasiniOlustur ()
		{
			if(_aylikSatisTabPage==null)
				return;

			_aylikSatisTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_aylikSatisTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"AYLIK CÄ°RO" ,
				"AYLIK KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _aylikSatisCiroLabel ,
				out _aylikSatisKarLabel ,
				out _aylikSatisKarOraniLabel ,
				out _aylikSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("AylÄ±k SatÄ±ÅŸ Ã–zeti");
			Size filtreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu , aramaKutusuBoyutu);
			Label ayLabel = SatisRaporFiltreEtiketiOlustur("Ay");
			_aylikSatisAyPicker=SatisRaporTarihSeciciOlustur(true , filtreBoyutu);
			_aylikSatisAyPicker.ValueChanged-=AylikSatisAyPicker_ValueChanged;
			_aylikSatisAyPicker.ValueChanged+=AylikSatisAyPicker_ValueChanged;
			_aylikSatisAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_aylikSatisGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				ayLabel ,
				_aylikSatisAyPicker ,
				null ,
				_aylikSatisAramaKutusu ,
				filtreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_aylikSatisGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_aylikSatisTabPage.Controls.Add(anaLayout);

			AramaKutusuHazirla(_aylikSatisAramaKutusu , _aylikSatisGrid);
		}

		private void ToplamSatisSayfasiniOlustur ()
		{
			if(_toplamSatisTabPage==null)
				return;

			_toplamSatisTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_toplamSatisTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"GENEL CÄ°RO" ,
				"GENEL KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _toplamSatisCiroLabel ,
				out _toplamSatisKarLabel ,
				out _toplamSatisKarOraniLabel ,
				out _toplamSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Toplam SatÄ±ÅŸ");
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur();
			_toplamSatisAramaKutusu=SatisRaporAramaKutusuOlustur();
			_toplamSatisGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				null ,
				null ,
				null ,
				_toplamSatisAramaKutusu);

			listeKutusu.Controls.Add(_toplamSatisGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_toplamSatisTabPage.Controls.Add(anaLayout);

			AramaKutusuHazirla(_toplamSatisAramaKutusu , _toplamSatisGrid);
		}

		private GroupBox SatisRaporGroupBoxOlustur ( string baslik )
		{
			return new GroupBox
			{
				Text=baslik,
				Dock=DockStyle.Fill,
				BackColor=Color.FromArgb(241 , 245 , 249),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(10)
			};
		}

		private TableLayoutPanel SatisKartLayoutiniOlustur (
			string kart1Baslik ,
			string kart2Baslik ,
			string kart3Baslik ,
			string kart4Baslik ,
			out Label kart1DegerLabel ,
			out Label kart2DegerLabel ,
			out Label kart3DegerLabel ,
			out Label kart4DegerLabel )
		{
			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=4,
				RowCount=1,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=Color.FromArgb(241 , 245 , 249)
			};

			for(int i = 0 ; i<4 ; i++)
				layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25f));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			Panel kart1 = SatisOzetKartiOlustur(kart1Baslik , Color.FromArgb(243 , 146 , 98) , out kart1DegerLabel);
			Panel kart2 = SatisOzetKartiOlustur(kart2Baslik , Color.FromArgb(236 , 126 , 104) , out kart2DegerLabel);
			Panel kart3 = SatisOzetKartiOlustur(kart3Baslik , Color.FromArgb(0 , 179 , 179) , out kart3DegerLabel);
			Panel kart4 = SatisOzetKartiOlustur(kart4Baslik , Color.FromArgb(246 , 208 , 120) , out kart4DegerLabel);

			kart1.Margin=new Padding(0 , 0 , 12 , 0);
			kart2.Margin=new Padding(0 , 0 , 12 , 0);
			kart3.Margin=new Padding(0 , 0 , 12 , 0);
			kart4.Margin=Padding.Empty;

			layout.Controls.Add(kart1 , 0 , 0);
			layout.Controls.Add(kart2 , 1 , 0);
			layout.Controls.Add(kart3 , 2 , 0);
			layout.Controls.Add(kart4 , 3 , 0);
			return layout;
		}

		private Panel SatisOzetKartiOlustur ( string baslik , Color arkaRenk , out Label degerLabel )
		{
			Panel kart = new Panel
			{
				Dock=DockStyle.Fill,
				Margin=Padding.Empty,
				Padding=new Padding(22 , 18 , 22 , 18),
				BackColor=arkaRenk
			};

			Label baslikLabel = new Label
			{
				Dock=DockStyle.Top,
				AutoSize=false,
				Height=28,
				Text=baslik,
				ForeColor=Color.White,
				Font=new Font("Segoe UI" , 9.5f , FontStyle.Bold),
				TextAlign=ContentAlignment.MiddleLeft
			};

			degerLabel=new Label
			{
				Dock=DockStyle.Fill,
				Text="0,00",
				ForeColor=Color.White,
				Font=new Font("Segoe UI" , 18f , FontStyle.Bold),
				TextAlign=ContentAlignment.MiddleLeft
			};

			kart.Controls.Add(degerLabel);
			kart.Controls.Add(baslikLabel);
			return kart;
		}

		private Size SatisRaporFiltreKontrolBoyutunuGetir ()
		{
			return new Size(258 , 28);
		}

		private Size SatisRaporKompaktFiltreKontrolBoyutunuGetir ()
		{
			return new Size(200 , 22);
		}

		private Size SatisRaporAramaKutusuBoyutunuGetir ()
		{
			return new Size(258 , 28);
		}

		private Panel SatisRaporFiltrePaneliniOlustur ( Size? filtreKontrolBoyutu = null , Size? aramaKutusuBoyutu = null )
		{
			Size kontrolBoyutu = filtreKontrolBoyutu??SatisRaporFiltreKontrolBoyutunuGetir();
			Size aramaBoyutu = aramaKutusuBoyutu??kontrolBoyutu;
			return new Panel
			{
				Dock=DockStyle.Top,
				Height=Math.Max(kontrolBoyutu.Height , aramaBoyutu.Height)+14,
				BackColor=Color.FromArgb(241 , 245 , 249),
				Padding=new Padding(0 , 4 , 0 , 10)
			};
		}

		private void SatisRaporFiltrePaneliniYerlesitir (
			Panel panel ,
			Label etiket ,
			Control secimKontrolu ,
			Button yenileButonu ,
			TextBox aramaKutusu ,
			Size? filtreKontrolBoyutu = null ,
			int? aramaKutusuGenisligi = null ,
			Size? aramaKutusuBoyutu = null )
		{
			if(panel==null)
				return;

			panel.Controls.Clear();
			Size kontrolBoyutu = filtreKontrolBoyutu??SatisRaporFiltreKontrolBoyutunuGetir();
			Size aramaBoyutu = aramaKutusuBoyutu??kontrolBoyutu;
			int secimKontroluGenisligi = secimKontrolu!=null
				? Math.Max(kontrolBoyutu.Width , secimKontrolu.Width)
				: 0;
			int yenileButonuGenisligi = yenileButonu!=null
				? Math.Max(kontrolBoyutu.Width , yenileButonu.Width)
				: 0;
			int aramaAlaniGenisligi = aramaKutusu!=null
				? Math.Max(aramaKutusuGenisligi??aramaBoyutu.Width , aramaKutusu.Width)
				: 0;

			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=5,
				RowCount=1,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=panel.BackColor
			};

			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , etiket!=null ? 56f : 0f));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , secimKontroluGenisligi));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , yenileButonuGenisligi));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100f));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , aramaAlaniGenisligi));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , Math.Max(kontrolBoyutu.Height , aramaKutusu!=null ? aramaBoyutu.Height : 0)));

			if(etiket!=null)
			{
				etiket.Dock=DockStyle.Fill;
				etiket.Margin=new Padding(0 , 0 , 10 , 0);
				layout.Controls.Add(etiket , 0 , 0);
			}

			if(secimKontrolu!=null)
			{
				secimKontrolu.Dock=DockStyle.Fill;
				secimKontrolu.Margin=new Padding(0 , 0 , 12 , 0);
				layout.Controls.Add(secimKontrolu , 1 , 0);
			}

			if(yenileButonu!=null)
			{
				yenileButonu.Dock=DockStyle.Fill;
				yenileButonu.Margin=new Padding(0 , 0 , 12 , 0);
				SatisRaporButonTemasiniUygula(yenileButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
				layout.Controls.Add(yenileButonu , 2 , 0);
			}

			if(aramaKutusu!=null)
			{
				aramaKutusu.MinimumSize=aramaBoyutu;
				aramaKutusu.Dock=DockStyle.Fill;
				aramaKutusu.Margin=Padding.Empty;
				layout.Controls.Add(aramaKutusu , 4 , 0);
			}

			panel.Controls.Add(layout);
		}

		private Label SatisRaporFiltreEtiketiOlustur ( string metin )
		{
			return new Label
			{
				AutoSize=false,
				Width=48,
				Dock=DockStyle.Left,
				Text=metin,
				TextAlign=ContentAlignment.MiddleLeft,
				Font=new Font("Segoe UI" , 9f , FontStyle.Bold),
				ForeColor=Color.FromArgb(51 , 65 , 85)
			};
		}

		private DateTimePicker SatisRaporTarihSeciciOlustur ( bool aySecimi , Size? boyut = null )
		{
			Size filtreKontrolBoyutu = boyut??SatisRaporFiltreKontrolBoyutunuGetir();
			DateTimePicker picker = new DateTimePicker
			{
				Dock=DockStyle.Left,
				Size=filtreKontrolBoyutu,
				Font=new Font("Segoe UI" , 9.5f , FontStyle.Regular),
				CalendarMonthBackground=Color.White
			};

			if(aySecimi)
			{
				picker.Format=DateTimePickerFormat.Custom;
				picker.CustomFormat="MMMM yyyy";
				picker.ShowUpDown=true;
			}
			else
			{
				picker.Format=DateTimePickerFormat.Custom;
				picker.CustomFormat="dd.MM.yyyy";
			}

			return picker;
		}

		private ComboBox SatisRaporComboBoxOlustur ()
		{
			return new ComboBox
			{
				DropDownStyle=ComboBoxStyle.DropDown,
				Font=SatisRaporKontrolFontunuGetir(),
				Size=SatisRaporStandartKontrolBoyutunuGetir(),
				IntegralHeight=false,
				MaxDropDownItems=12
			};
		}

		private TextBox SatisRaporMetinKutusuOlustur ( bool saltOkunur , bool cokSatirli )
		{
			TextBox textBox = new TextBox
			{
				Font=SatisRaporKontrolFontunuGetir(),
				BorderStyle=BorderStyle.FixedSingle,
				BackColor=saltOkunur ? SystemColors.ControlLight : Color.White,
				ReadOnly=saltOkunur,
				Multiline=cokSatirli,
				ScrollBars=cokSatirli ? ScrollBars.Vertical : ScrollBars.None,
				Size=SatisRaporStandartKontrolBoyutunuGetir(),
				WordWrap=cokSatirli
			};

			if(cokSatirli)
				textBox.Height=82;

			return textBox;
		}

		private Button SatisRaporButonuOlustur ( string metin , Size? boyut = null , Padding? icBosluk = null )
		{
			Size butonBoyutu = boyut??new Size(108 , 38);
			Padding butonIciBosluk = icBosluk??new Padding(12 , 6 , 12 , 6);
			Button buton = new Button
			{
				Text=metin,
				AutoSize=false,
				Size=butonBoyutu,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				FlatStyle=FlatStyle.Flat,
				Font=new Font("Segoe UI" , 9F , FontStyle.Bold),
				Margin=Padding.Empty,
				Padding=butonIciBosluk,
				Cursor=Cursors.Hand,
				UseVisualStyleBackColor=false
			};

			buton.FlatAppearance.BorderSize=1;
			buton.FlatAppearance.BorderColor=Color.FromArgb(148 , 163 , 184);
			buton.FlatAppearance.MouseOverBackColor=Color.FromArgb(248 , 250 , 252);
			buton.FlatAppearance.MouseDownBackColor=Color.FromArgb(226 , 232 , 240);
			return buton;
		}

		private void SatisRaporButonTemasiniUygula ( Button buton , Color arkaRenk , Color yaziRenk , Color kenarRengi )
		{
			if(buton==null)
				return;

			buton.BackColor=arkaRenk;
			buton.ForeColor=yaziRenk;
			buton.FlatAppearance.BorderColor=kenarRengi;
		}

		private TextBox SatisRaporAramaKutusuOlustur ( Size? boyut = null )
		{
			Size aramaKutusuBoyutu = boyut??SatisRaporFiltreKontrolBoyutunuGetir();
			TextBox aramaKutusu = new TextBox
			{
				Dock=DockStyle.Fill,
				Size=aramaKutusuBoyutu
			};
			AramaKutusuGorunumunuUygula(aramaKutusu);
			aramaKutusu.Size=aramaKutusuBoyutu;
			return aramaKutusu;
		}

		private DataGridView SatisRaporGridiOlustur ()
		{
			DataGridView grid = new DataGridView
			{
				Dock=DockStyle.Fill,
				MultiSelect=false,
				AllowUserToAddRows=false,
				AllowUserToDeleteRows=false,
				AllowUserToResizeRows=false
			};

			DatagridviewSetting(grid);
			return grid;
		}

		private Font SatisRaporKontrolFontunuGetir ()
		{
			return comboBox8?.Font??new Font("Microsoft Sans Serif" , 10.2f , FontStyle.Regular , GraphicsUnit.Point , 162);
		}

		private Size SatisRaporStandartKontrolBoyutunuGetir ()
		{
			TextBox ornek = AramaKutusuOrnekTextBoxGetir();
			if(ornek!=null)
				return ornek.Size;

			return new Size(258 , 28);
		}

		private void SatisRaporFormSatiriEkle ( TableLayoutPanel layout , int satirIndex , string etiketMetni , Control kontrol )
		{
			if(layout==null||kontrol==null)
				return;

			Label etiket = new Label
			{
				Text=etiketMetni,
				AutoSize=false,
				Dock=DockStyle.Fill,
				TextAlign=ContentAlignment.MiddleRight,
				Font=new Font("Segoe UI" , 9f , FontStyle.Bold),
				ForeColor=Color.FromArgb(51 , 65 , 85),
				Padding=new Padding(0 , 0 , 8 , 0)
			};

			kontrol.Dock=DockStyle.Fill;
			layout.Controls.Add(etiket , 0 , satirIndex);
			layout.Controls.Add(kontrol , 1 , satirIndex);
		}

		#endif
		private void SatisRaporAltTabControl_SelectedIndexChanged ( object sender , EventArgs e )
		{
			GunlukSatisVerileriniYenile();
		}

		private void GunlukSatisTarihPicker_ValueChanged ( object sender , EventArgs e )
		{
			if(!_gunlukSatisTarihEsitleniyor&&_gunlukSatisToplamTarihPicker!=null)
			{
				_gunlukSatisTarihEsitleniyor=true;
				try
				{
					_gunlukSatisToplamTarihPicker.Value=_gunlukSatisTarihPicker.Value.Date;
				}
				finally
				{
					_gunlukSatisTarihEsitleniyor=false;
				}
			}

			GunlukSatisVerileriniYenile();
		}

		private void GunlukSatisToplamTarihPicker_ValueChanged ( object sender , EventArgs e )
		{
			if(!_gunlukSatisTarihEsitleniyor&&_gunlukSatisTarihPicker!=null)
			{
				_gunlukSatisTarihEsitleniyor=true;
				try
				{
					_gunlukSatisTarihPicker.Value=_gunlukSatisToplamTarihPicker.Value.Date;
				}
				finally
				{
					_gunlukSatisTarihEsitleniyor=false;
				}
			}

			GunlukSatisVerileriniYenile();
		}

		private void AylikSatisAyPicker_ValueChanged ( object sender , EventArgs e )
		{
			GunlukSatisVerileriniYenile();
		}

		private void AylikFabrikaFaturaAyPicker_ValueChanged ( object sender , EventArgs e )
		{
			GunlukSatisVerileriniYenile();
		}

		private void AylikMusteriFaturaAyPicker_ValueChanged ( object sender , EventArgs e )
		{
			GunlukSatisVerileriniYenile();
		}

		private void GunlukSatisGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||_gunlukSatisGrid==null||e.RowIndex>=_gunlukSatisGrid.Rows.Count)
				return;

			DataGridViewRow satir = _gunlukSatisGrid.Rows[e.RowIndex];
			int gunlukSatisId;
			_seciliGunlukSatisId=int.TryParse(Convert.ToString(satir.Cells["GunlukSatisID"].Value) , out gunlukSatisId)
				? gunlukSatisId
				: ( int? )null;
		}

		private void GunlukSatisUrunComboBox_TextChanged ( object sender , EventArgs e )
		{
			if(_gunlukSatisUrunComboDolduruluyor||_gunlukSatisUrunComboBox==null)
				return;

			GunlukSatisUrunListesiniYenile();
			ComboBoxEslesmeleriniGoster(_gunlukSatisUrunComboBox , _gunlukSatisUrunComboBox.Text);
		}

		private void GunlukSatisUrunComboBox_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_gunlukSatisUrunComboDolduruluyor)
				return;

			UrunAramaKaydi urunKaydi = _gunlukSatisUrunComboBox?.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null)
				return;

			GunlukSatisUrunBilgileriniYukle(urunKaydi);
		}

		private void GunlukSatisUrunComboBox_Leave ( object sender , EventArgs e )
		{
			if(_gunlukSatisUrunComboBox==null||string.IsNullOrWhiteSpace(_gunlukSatisUrunComboBox.Text))
				return;

			UrunAramaKaydi urunKaydi = _gunlukSatisUrunComboBox.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null)
				urunKaydi=GunlukSatisUrunKaydiniBul(_gunlukSatisUrunComboBox.Text);

			if(urunKaydi!=null)
				GunlukSatisUrunBilgileriniYukle(urunKaydi);
		}

		private void GunlukSatisHesapAlanlari_TextChanged ( object sender , EventArgs e )
		{
			GunlukSatisToplamHesapla();
		}

		private void GunlukSatisKaydetButonu_Click ( object sender , EventArgs e )
		{
			GunlukSatisKaydiniKaydet(false);
		}

		private void GunlukSatisSilButonu_Click ( object sender , EventArgs e )
		{
			GunlukSatisKaydiniSil(false);
		}

		private void IadeKaydetButonu_Click ( object sender , EventArgs e )
		{
			GunlukSatisKaydiniKaydet(true);
		}

		private void IadeSilButonu_Click ( object sender , EventArgs e )
		{
			GunlukSatisKaydiniSil(true);
		}

		private void GunlukSatisKaydiniKaydet ( bool iadeMi )
		{
			if(!GunlukSatisAltyapisiniHazirla(true))
				return;

			ComboBox urunComboBox = iadeMi ? _iadeUrunComboBox : _gunlukSatisUrunComboBox;
			TextBox miktarTextBox = iadeMi ? _iadeMiktarTextBox : _gunlukSatisMiktarTextBox;
			TextBox birimFiyatTextBox = iadeMi ? _iadeBirimFiyatTextBox : _gunlukSatisBirimFiyatTextBox;
			TextBox birimMaliyetTextBox = iadeMi ? _iadeBirimMaliyetTextBox : _gunlukSatisBirimMaliyetTextBox;
			TextBox notTextBox = iadeMi ? _iadeNotTextBox : _gunlukSatisNotTextBox;
			DateTimePicker tarihPicker = iadeMi ? _iadeTarihPicker : _gunlukSatisTarihPicker;
			string kayitTuru = GunlukSatisKayitTuruGetir(iadeMi);
			string islemAdi = iadeMi ? "İade" : "Günlük satış";

			UrunAramaKaydi urunKaydi = iadeMi
				? IadeUrunKaydiniBul(urunComboBox?.Text)
				: GunlukSatisUrunKaydiniBul(urunComboBox?.Text);
			if(urunKaydi==null)
			{
				MessageBox.Show("Lütfen geçerli bir ürün seçin.");
				return;
			}

			decimal miktar = SepetDecimalParse(miktarTextBox?.Text);
			decimal birimFiyat = SepetDecimalParse(birimFiyatTextBox?.Text);
			decimal birimMaliyet = SepetDecimalParse(birimMaliyetTextBox?.Text);
			if(miktar<=0m)
			{
				MessageBox.Show("Miktar 0'dan büyük olmalıdır.");
				return;
			}

			DateTime islemTarihi = tarihPicker!=null ? tarihPicker.Value.Date : DateTime.Today;
			string aciklama = notTextBox?.Text?.Trim()??string.Empty;
			decimal kayitMiktari = iadeMi ? -miktar : miktar;
			decimal kayitToplami = kayitMiktari*birimFiyat;
			decimal kayitMaliyeti = kayitMiktari*birimMaliyet;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							string tabloAdi = GunlukSatisTabloAdiniGetir();
							DateTime ertesiGun = islemTarihi.AddDays(1);
							int? mevcutKayitId = null;
							decimal mevcutMiktar = 0m;
							decimal mevcutToplam = 0m;
							decimal mevcutMaliyet = 0m;
							string mevcutAciklama = string.Empty;

							using(OleDbCommand cmd = new OleDbCommand(
								"SELECT GunlukSatisID, Miktar, ToplamTutar, ToplamMaliyet, Aciklama, IIF(KayitTuru IS NULL, '', KayitTuru) AS KayitTuru " +
								"FROM ["+tabloAdi+"] WHERE UrunID=? AND SatisTarihi>=? AND SatisTarihi<? ORDER BY GunlukSatisID DESC" ,
								conn ,
								tx))
							{
								cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunKaydi.UrunId;
								cmd.Parameters.Add("?" , OleDbType.Date).Value=islemTarihi;
								cmd.Parameters.Add("?" , OleDbType.Date).Value=ertesiGun;
								using(OleDbDataReader rd = cmd.ExecuteReader())
								{
									while(rd!=null&&rd.Read())
									{
										decimal mevcutSatirMiktari = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
										string mevcutKayitTuru = Convert.ToString(rd["KayitTuru"])??string.Empty;
										if(!GunlukSatisKayitTuruEslesiyor(mevcutKayitTuru , kayitTuru , mevcutSatirMiktari))
											continue;

										mevcutKayitId=Convert.ToInt32(rd["GunlukSatisID"]);
										mevcutMiktar=mevcutSatirMiktari;
										mevcutToplam=rd["ToplamTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTutar"]);
										mevcutMaliyet=rd["ToplamMaliyet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamMaliyet"]);
										mevcutAciklama=rd["Aciklama"]?.ToString()??string.Empty;
										break;
									}
								}
							}

							UrunStokDegisimiUygula(conn , tx , urunKaydi.UrunId , -kayitMiktari , urunKaydi.UrunAdi);

							if(mevcutKayitId.HasValue)
							{
								decimal yeniMiktar = mevcutMiktar+kayitMiktari;
								decimal yeniToplam = mevcutToplam+kayitToplami;
								decimal yeniMaliyet = mevcutMaliyet+kayitMaliyeti;
								decimal yeniBirimFiyat = yeniMiktar==0m ? 0m : yeniToplam/yeniMiktar;
								decimal yeniBirimMaliyet = yeniMiktar==0m ? 0m : yeniMaliyet/yeniMiktar;
								string yeniAciklama = string.IsNullOrWhiteSpace(aciklama) ? mevcutAciklama : aciklama;

								using(OleDbCommand guncelle = new OleDbCommand(
									"UPDATE ["+tabloAdi+"] SET Miktar=?, BirimSatisFiyati=?, BirimMaliyet=?, ToplamTutar=?, ToplamMaliyet=?, Aciklama=?, KayitTuru=?, SonGuncellemeTarihi=? WHERE GunlukSatisID=?" ,
									conn ,
									tx))
								{
									guncelle.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(yeniMiktar);
									guncelle.Parameters.Add("?" , OleDbType.Currency).Value=yeniBirimFiyat;
									guncelle.Parameters.Add("?" , OleDbType.Currency).Value=yeniBirimMaliyet;
									guncelle.Parameters.Add("?" , OleDbType.Currency).Value=yeniToplam;
									guncelle.Parameters.Add("?" , OleDbType.Currency).Value=yeniMaliyet;
									guncelle.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(yeniAciklama) ? (object)DBNull.Value : yeniAciklama;
									guncelle.Parameters.Add("?" , OleDbType.VarWChar).Value=kayitTuru;
									guncelle.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now;
									guncelle.Parameters.Add("?" , OleDbType.Integer).Value=mevcutKayitId.Value;
									guncelle.ExecuteNonQuery();
								}

								if(iadeMi)
									_seciliIadeKaydiId=mevcutKayitId;
								else
									_seciliGunlukSatisId=mevcutKayitId;
							}
							else
							{
								using(OleDbCommand ekle = new OleDbCommand(
									"INSERT INTO ["+tabloAdi+"] (SatisTarihi, UrunID, Miktar, BirimSatisFiyati, BirimMaliyet, ToplamTutar, ToplamMaliyet, Aciklama, KayitTuru, SonGuncellemeTarihi) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)" ,
									conn ,
									tx))
								{
									ekle.Parameters.Add("?" , OleDbType.Date).Value=islemTarihi.Add(DateTime.Now.TimeOfDay);
									ekle.Parameters.Add("?" , OleDbType.Integer).Value=urunKaydi.UrunId;
									ekle.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(kayitMiktari);
									ekle.Parameters.Add("?" , OleDbType.Currency).Value=birimFiyat;
									ekle.Parameters.Add("?" , OleDbType.Currency).Value=birimMaliyet;
									ekle.Parameters.Add("?" , OleDbType.Currency).Value=kayitToplami;
									ekle.Parameters.Add("?" , OleDbType.Currency).Value=kayitMaliyeti;
									ekle.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
									ekle.Parameters.Add("?" , OleDbType.VarWChar).Value=kayitTuru;
									ekle.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now;
									ekle.ExecuteNonQuery();
								}

								using(OleDbCommand kimlik = new OleDbCommand("SELECT @@IDENTITY" , conn , tx))
								{
									int yeniId = Convert.ToInt32(kimlik.ExecuteScalar());
									if(iadeMi)
										_seciliIadeKaydiId=yeniId;
									else
										_seciliGunlukSatisId=yeniId;
								}
							}

							tx.Commit();
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				if(iadeMi)
					IadeFormunuTemizle();
				else
					GunlukSatisFormunuTemizle();

				GunlukSatisVerileriniYenile();
				StokDegisimindenSonraEkranlariYenile();
				MessageBox.Show(islemAdi+" kaydedildi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show(islemAdi+" kaydedilemedi: "+ex.Message);
			}
		}

		private void GunlukSatisKaydiniSil ( bool iadeMi )
		{
			if(!GunlukSatisAltyapisiniHazirla(true))
				return;

			DataGridView grid = iadeMi ? _iadeGrid : _gunlukSatisGrid;
			int? seciliId = iadeMi ? _seciliIadeKaydiId : _seciliGunlukSatisId;
			string kayitAdi = iadeMi ? "iade" : "günlük satış";

			if(!seciliId.HasValue&&grid!=null&&grid.CurrentRow!=null&&!grid.CurrentRow.IsNewRow)
			{
				int gunlukSatisId;
				if(int.TryParse(Convert.ToString(grid.CurrentRow.Cells["GunlukSatisID"].Value) , out gunlukSatisId))
					seciliId=gunlukSatisId;
			}

			if(!seciliId.HasValue)
			{
				MessageBox.Show("Silmek için "+kayitAdi+" listesinden bir satır seçin.");
				return;
			}

			if(MessageBox.Show("Seçili "+kayitAdi+" kaydı silinsin mi?" , "Sil" , MessageBoxButtons.YesNo , MessageBoxIcon.Question)!=DialogResult.Yes)
				return;

			try
			{
				string tabloAdi = GunlukSatisTabloAdiniGetir();
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							int urunId = 0;
							decimal kayitMiktari = 0m;
							string urunAdi = string.Empty;

							using(OleDbCommand oku = new OleDbCommand(
								"SELECT GS.UrunID, IIF(GS.Miktar IS NULL, 0, GS.Miktar) AS Miktar, IIF(U.UrunAdi IS NULL, '', U.UrunAdi) AS UrunAdi " +
								"FROM ["+tabloAdi+"] AS GS " +
								"LEFT JOIN Urunler AS U ON CLng(IIF(GS.UrunID IS NULL, 0, GS.UrunID)) = U.UrunID " +
								"WHERE GS.GunlukSatisID=?" ,
								conn ,
								tx))
							{
								oku.Parameters.Add("?" , OleDbType.Integer).Value=seciliId.Value;
								using(OleDbDataReader rd = oku.ExecuteReader())
								{
									if(rd!=null&&rd.Read()&&rd["UrunID"]!=DBNull.Value)
									{
										urunId=Convert.ToInt32(rd["UrunID"]);
										kayitMiktari=rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
										urunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty;
									}
								}
							}

							if(urunId>0&&kayitMiktari!=0m)
								UrunStokDegisimiUygula(conn , tx , urunId , kayitMiktari , urunAdi);

							using(OleDbCommand cmd = new OleDbCommand("DELETE FROM ["+tabloAdi+"] WHERE GunlukSatisID=?" , conn , tx))
							{
								cmd.Parameters.Add("?" , OleDbType.Integer).Value=seciliId.Value;
								cmd.ExecuteNonQuery();
							}

							tx.Commit();
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				if(iadeMi)
				{
					_seciliIadeKaydiId=null;
					IadeFormunuTemizle();
				}
				else
				{
					_seciliGunlukSatisId=null;
					GunlukSatisFormunuTemizle();
				}

				GunlukSatisVerileriniYenile();
				StokDegisimindenSonraEkranlariYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show(char.ToUpper(kayitAdi[0] , _yazdirmaKulturu)+kayitAdi.Substring(1)+" kaydı silinemedi: "+ex.Message);
			}
		}

		private void GunlukSatisFormunuTemizle ()
		{
			_seciliGunlukSatisId=null;
			if(_gunlukSatisUrunComboBox!=null)
				_gunlukSatisUrunComboBox.Text=string.Empty;
			if(_gunlukSatisBirimTextBox!=null)
				_gunlukSatisBirimTextBox.Clear();
			if(_gunlukSatisMiktarTextBox!=null)
				_gunlukSatisMiktarTextBox.Text="1";
			if(_gunlukSatisBirimFiyatTextBox!=null)
				_gunlukSatisBirimFiyatTextBox.Text="0,00";
			if(_gunlukSatisBirimMaliyetTextBox!=null)
				_gunlukSatisBirimMaliyetTextBox.Text="0,00";
			if(_gunlukSatisToplamTextBox!=null)
				_gunlukSatisToplamTextBox.Text="0,00";
			if(_gunlukSatisNotTextBox!=null)
				_gunlukSatisNotTextBox.Clear();
		}

		private void GunlukSatisToplamHesapla ()
		{
			if(_gunlukSatisFormHesaplaniyor||_gunlukSatisToplamTextBox==null)
				return;

			_gunlukSatisFormHesaplaniyor=true;
			try
			{
				decimal miktar = SepetDecimalParse(_gunlukSatisMiktarTextBox?.Text);
				decimal fiyat = SepetDecimalParse(_gunlukSatisBirimFiyatTextBox?.Text);
				_gunlukSatisToplamTextBox.Text=( miktar*fiyat ).ToString("N2" , _yazdirmaKulturu);
			}
			finally
			{
				_gunlukSatisFormHesaplaniyor=false;
			}
		}

		private void GunlukSatisUrunListesiniYenile ()
		{
			if(_gunlukSatisUrunComboBox==null||_gunlukSatisUrunComboBox.IsDisposed)
				return;

			string mevcutMetin = _gunlukSatisUrunComboBox.Text;
			_gunlukSatisUrunComboDolduruluyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					_gunlukSatisUrunComboBox ,
					ComboBoxIcinKayitlariFiltrele(
						UrunSecimKayitlariniGetir() ,
						mevcutMetin ,
						x => x.UrunGosterimAdi) ,
					nameof(UrunAramaKaydi.UrunGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_gunlukSatisUrunComboDolduruluyor=false;
			}
		}

		private UrunAramaKaydi GunlukSatisUrunKaydiniBul ( string aramaMetni )
		{
			string arama = ( aramaMetni??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(arama))
				return null;

			UrunAramaKaydi seciliKayit = _gunlukSatisUrunComboBox?.SelectedItem as UrunAramaKaydi;
			if(seciliKayit!=null&&string.Equals(seciliKayit.UrunGosterimAdi , arama , StringComparison.CurrentCultureIgnoreCase))
				return seciliKayit;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				return EnUygunUrunKaydiniBul(conn , null , arama);
			}
		}

		private void GunlukSatisUrunBilgileriniYukle ( UrunAramaKaydi urunKaydi )
		{
			if(urunKaydi==null)
				return;

			if(_gunlukSatisBirimTextBox!=null)
				_gunlukSatisBirimTextBox.Text=urunKaydi.BirimAdi??string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					decimal satisFiyati = UrunSatisFiyatiGetir(conn , urunKaydi.UrunId);
					decimal maliyet = UrunNetAlisFiyatiGetir(conn , urunKaydi.UrunId , DateTime.Now);

					if(_gunlukSatisBirimFiyatTextBox!=null)
						_gunlukSatisBirimFiyatTextBox.Text=satisFiyati.ToString("N2" , _yazdirmaKulturu);
					if(_gunlukSatisBirimMaliyetTextBox!=null)
						_gunlukSatisBirimMaliyetTextBox.Text=maliyet.ToString("N2" , _yazdirmaKulturu);
				}
			}
			catch
			{
				if(_gunlukSatisBirimMaliyetTextBox!=null)
					_gunlukSatisBirimMaliyetTextBox.Text="0,00";
			}

			GunlukSatisToplamHesapla();
		}

		private void IadeTarihPicker_ValueChanged ( object sender , EventArgs e )
		{
			GunlukSatisVerileriniYenile();
		}

		private void IadeGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||_iadeGrid==null||e.RowIndex>=_iadeGrid.Rows.Count)
				return;

			DataGridViewRow satir = _iadeGrid.Rows[e.RowIndex];
			int iadeKaydiId;
			_seciliIadeKaydiId=int.TryParse(Convert.ToString(satir.Cells["GunlukSatisID"].Value) , out iadeKaydiId)
				? iadeKaydiId
				: ( int? )null;
		}

		private void IadeUrunComboBox_TextChanged ( object sender , EventArgs e )
		{
			if(_iadeUrunComboDolduruluyor||_iadeUrunComboBox==null)
				return;

			IadeUrunListesiniYenile();
			ComboBoxEslesmeleriniGoster(_iadeUrunComboBox , _iadeUrunComboBox.Text);
		}

		private void IadeUrunComboBox_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_iadeUrunComboDolduruluyor)
				return;

			UrunAramaKaydi urunKaydi = _iadeUrunComboBox?.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null)
				return;

			IadeUrunBilgileriniYukle(urunKaydi);
		}

		private void IadeUrunComboBox_Leave ( object sender , EventArgs e )
		{
			if(_iadeUrunComboBox==null||string.IsNullOrWhiteSpace(_iadeUrunComboBox.Text))
				return;

			UrunAramaKaydi urunKaydi = _iadeUrunComboBox.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null)
				urunKaydi=IadeUrunKaydiniBul(_iadeUrunComboBox.Text);

			if(urunKaydi!=null)
				IadeUrunBilgileriniYukle(urunKaydi);
		}

		private void IadeHesapAlanlari_TextChanged ( object sender , EventArgs e )
		{
			IadeToplamHesapla();
		}

		private void IadeFormunuTemizle ()
		{
			_seciliIadeKaydiId=null;
			if(_iadeUrunComboBox!=null)
				_iadeUrunComboBox.Text=string.Empty;
			if(_iadeBirimTextBox!=null)
				_iadeBirimTextBox.Clear();
			if(_iadeMiktarTextBox!=null)
				_iadeMiktarTextBox.Text="1";
			if(_iadeBirimFiyatTextBox!=null)
				_iadeBirimFiyatTextBox.Text="0,00";
			if(_iadeBirimMaliyetTextBox!=null)
				_iadeBirimMaliyetTextBox.Text="0,00";
			if(_iadeToplamTextBox!=null)
				_iadeToplamTextBox.Text="0,00";
			if(_iadeNotTextBox!=null)
				_iadeNotTextBox.Clear();
		}

		private void IadeToplamHesapla ()
		{
			if(_iadeFormHesaplaniyor||_iadeToplamTextBox==null)
				return;

			_iadeFormHesaplaniyor=true;
			try
			{
				decimal miktar = SepetDecimalParse(_iadeMiktarTextBox?.Text);
				decimal fiyat = SepetDecimalParse(_iadeBirimFiyatTextBox?.Text);
				_iadeToplamTextBox.Text=( miktar*fiyat ).ToString("N2" , _yazdirmaKulturu);
			}
			finally
			{
				_iadeFormHesaplaniyor=false;
			}
		}

		private void IadeUrunListesiniYenile ()
		{
			if(_iadeUrunComboBox==null||_iadeUrunComboBox.IsDisposed)
				return;

			string mevcutMetin = _iadeUrunComboBox.Text;
			_iadeUrunComboDolduruluyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					_iadeUrunComboBox ,
					ComboBoxIcinKayitlariFiltrele(
						UrunSecimKayitlariniGetir() ,
						mevcutMetin ,
						x => x.UrunGosterimAdi) ,
					nameof(UrunAramaKaydi.UrunGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_iadeUrunComboDolduruluyor=false;
			}
		}

		private UrunAramaKaydi IadeUrunKaydiniBul ( string aramaMetni )
		{
			string arama = ( aramaMetni??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(arama))
				return null;

			UrunAramaKaydi seciliKayit = _iadeUrunComboBox?.SelectedItem as UrunAramaKaydi;
			if(seciliKayit!=null&&string.Equals(seciliKayit.UrunGosterimAdi , arama , StringComparison.CurrentCultureIgnoreCase))
				return seciliKayit;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				return EnUygunUrunKaydiniBul(conn , null , arama);
			}
		}

		private void IadeUrunBilgileriniYukle ( UrunAramaKaydi urunKaydi )
		{
			if(urunKaydi==null)
				return;

			if(_iadeBirimTextBox!=null)
				_iadeBirimTextBox.Text=urunKaydi.BirimAdi??string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					decimal satisFiyati = UrunSatisFiyatiGetir(conn , urunKaydi.UrunId);
					decimal maliyet = UrunNetAlisFiyatiGetir(conn , urunKaydi.UrunId , DateTime.Now);

					if(_iadeBirimFiyatTextBox!=null)
						_iadeBirimFiyatTextBox.Text=satisFiyati.ToString("N2" , _yazdirmaKulturu);
					if(_iadeBirimMaliyetTextBox!=null)
						_iadeBirimMaliyetTextBox.Text=maliyet.ToString("N2" , _yazdirmaKulturu);
				}
			}
			catch
			{
				if(_iadeBirimMaliyetTextBox!=null)
					_iadeBirimMaliyetTextBox.Text="0,00";
			}

			IadeToplamHesapla();
		}

		private decimal UrunSatisFiyatiGetir ( OleDbConnection conn , int urunId )
		{
			using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? ORDER BY UrunSatisFiyatID DESC" , conn))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
			}
		}

		private decimal UrunNetAlisFiyatiGetir ( OleDbConnection conn , int urunId , DateTime referansTarih )
		{
			using(OleDbCommand cmd = new OleDbCommand(
				"SELECT TOP 1 NetAlisFiyati FROM UrunAlis WHERE UrunID=? AND (Tarih IS NULL OR Tarih<=?) ORDER BY IIF(Tarih IS NULL, #01/01/1900#, Tarih) DESC, UrunAlisID DESC" ,
				conn))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				cmd.Parameters.Add("?" , OleDbType.Date).Value=referansTarih;
				object sonuc = cmd.ExecuteScalar();
				if(sonuc!=null&&sonuc!=DBNull.Value)
					return Convert.ToDecimal(sonuc);
			}

			using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 NetAlisFiyati FROM UrunAlis WHERE UrunID=? ORDER BY UrunAlisID DESC" , conn))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
			}
		}

		private List<SatisOzetSatiri> ManuelSatisKayitlariniGetir ( DateTime baslangic , DateTime bitis , string kayitTuruFiltresi = null )
		{
			List<SatisOzetSatiri> sonuc = new List<SatisOzetSatiri>();
			if(!GunlukSatisAltyapisiniHazirla(false))
				return sonuc;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string tabloAdi = GunlukSatisTabloAdiniGetir();
				string sorgu = @"SELECT
								GS.GunlukSatisID,
								GS.SatisTarihi,
								GS.UrunID,
								IIF(U.UrunAdi IS NULL, '', U.UrunAdi) AS UrunAdi,
								IIF(B.BirimAdi IS NULL, '', B.BirimAdi) AS BirimAdi,
								IIF(GS.Miktar IS NULL, 0, GS.Miktar) AS Miktar,
								IIF(GS.BirimSatisFiyati IS NULL, 0, GS.BirimSatisFiyati) AS BirimSatisFiyati,
								IIF(GS.BirimMaliyet IS NULL, 0, GS.BirimMaliyet) AS BirimMaliyet,
								IIF(GS.ToplamTutar IS NULL, 0, GS.ToplamTutar) AS ToplamTutar,
								IIF(GS.ToplamMaliyet IS NULL, 0, GS.ToplamMaliyet) AS ToplamMaliyet,
								IIF(GS.Aciklama IS NULL, '', GS.Aciklama) AS Aciklama,
								IIF(GS.KayitTuru IS NULL, '', GS.KayitTuru) AS KayitTuru
							FROM ([" + tabloAdi + @"] AS GS
							LEFT JOIN Urunler AS U ON CLng(IIF(GS.UrunID IS NULL, 0, GS.UrunID)) = U.UrunID)
							LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
							WHERE GS.SatisTarihi>=? AND GS.SatisTarihi<?
							ORDER BY GS.SatisTarihi DESC, IIF(U.UrunAdi IS NULL, '', U.UrunAdi) ASC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.Date).Value=baslangic;
					cmd.Parameters.Add("?" , OleDbType.Date).Value=bitis;
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							decimal miktar = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
							string kayitTuru = GunlukSatisKayitTurunuCoz(Convert.ToString(rd["KayitTuru"]) , miktar);
							if(!string.IsNullOrWhiteSpace(kayitTuruFiltresi)&&!string.Equals(kayitTuru , kayitTuruFiltresi , StringComparison.OrdinalIgnoreCase))
								continue;

							sonuc.Add(new SatisOzetSatiri
							{
								GunlukSatisId=rd["GunlukSatisID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["GunlukSatisID"]),
								Tarih=rd["SatisTarihi"]==DBNull.Value ? baslangic : Convert.ToDateTime(rd["SatisTarihi"]),
								UrunId=rd["UrunID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["UrunID"]),
								UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
								BirimAdi=Convert.ToString(rd["BirimAdi"])??string.Empty,
								Miktar=miktar,
								BirimFiyat=rd["BirimSatisFiyati"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["BirimSatisFiyati"]),
								BirimMaliyet=rd["BirimMaliyet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["BirimMaliyet"]),
								Ciro=rd["ToplamTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTutar"]),
								ToplamMaliyet=rd["ToplamMaliyet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamMaliyet"]),
								Kaynak=string.Equals(kayitTuru , GunlukSatisKayitTuruIade , StringComparison.Ordinal) ? "MANUEL IADE" : "MANUEL",
								Aciklama=Convert.ToString(rd["Aciklama"])??string.Empty,
								KayitTuru=kayitTuru
							});
						}
					}
				}
			}

			return sonuc;
		}

		private List<SatisOzetSatiri> FaturaSatisKayitlariniGetir ( DateTime baslangic , DateTime bitis )
		{
			List<SatisOzetSatiri> sonuc = new List<SatisOzetSatiri>();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				Dictionary<string, decimal> maliyetCache = new Dictionary<string, decimal>();
				string sorgu = @"SELECT
								FD.UrunID,
								FD.YapilanIsID,
								F.FaturaTarihi,
								IIF(FD.KalemTuru IS NULL, '', FD.KalemTuru) AS KalemTuru,
								IIF(FD.IsBilgisi IS NULL, '', FD.IsBilgisi) AS IsBilgisi,
								IIF(FD.KalemAdi IS NULL OR FD.KalemAdi='', IIF(U.UrunAdi IS NULL, IIF(FD.IsBilgisi IS NULL, '', FD.IsBilgisi), U.UrunAdi), FD.KalemAdi) AS UrunAdi,
								IIF(FD.Birim IS NULL OR FD.Birim='', IIF(B.BirimAdi IS NULL, '', B.BirimAdi), FD.Birim) AS BirimAdi,
								IIF(FD.Miktar IS NULL, 0, FD.Miktar) AS Miktar,
								IIF(FD.SatisFiyati IS NULL, 0, FD.SatisFiyati) AS SatisFiyati
							FROM ((FaturaDetay AS FD
							INNER JOIN Faturalar AS F ON FD.FaturaID = F.FaturaID)
							LEFT JOIN Urunler AS U ON CLng(IIF(FD.UrunID IS NULL, 0, FD.UrunID)) = U.UrunID)
							LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
							WHERE F.FaturaTarihi>=? AND F.FaturaTarihi<?
							ORDER BY F.FaturaTarihi DESC, IIF(FD.KalemAdi IS NULL OR FD.KalemAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), FD.KalemAdi) ASC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.Date).Value=baslangic;
					cmd.Parameters.Add("?" , OleDbType.Date).Value=bitis;
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							int urunId = rd["UrunID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["UrunID"]);
							DateTime tarih = rd["FaturaTarihi"]==DBNull.Value ? baslangic : Convert.ToDateTime(rd["FaturaTarihi"]);
							decimal miktar = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
							decimal satisFiyati = rd["SatisFiyati"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["SatisFiyati"]);
							string kalemTuru = Convert.ToString(rd["KalemTuru"])??string.Empty;
							string isBilgisi = Convert.ToString(rd["IsBilgisi"])??string.Empty;
							string cacheAnahtari = urunId.ToString(CultureInfo.InvariantCulture)+"_"+tarih.ToString("yyyyMMdd" , CultureInfo.InvariantCulture);
							decimal birimMaliyet;
							if(!maliyetCache.TryGetValue(cacheAnahtari , out birimMaliyet))
							{
								birimMaliyet=urunId>0 ? UrunNetAlisFiyatiGetir(conn , urunId , tarih) : 0m;
								maliyetCache[cacheAnahtari]=birimMaliyet;
							}

							sonuc.Add(new SatisOzetSatiri
							{
								Tarih=tarih,
								UrunId=urunId,
								UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
								BirimAdi=Convert.ToString(rd["BirimAdi"])??string.Empty,
								Miktar=miktar,
								BirimFiyat=satisFiyati,
								BirimMaliyet=birimMaliyet,
								Ciro=miktar*satisFiyati,
								ToplamMaliyet=miktar*birimMaliyet,
								Kaynak=string.IsNullOrWhiteSpace(kalemTuru) ? "FATURA" : "FATURA / "+kalemTuru,
								Aciklama=isBilgisi,
								KayitTuru=GunlukSatisKayitTuruSatis
							});
						}
					}
				}
			}

			return sonuc;
		}

		private List<AylikFaturaOzetSatiri> AylikFaturaKayitlariniGetir ( DateTime baslangic , DateTime bitis , BelgeKayitTuru belgeTuru )
		{
			List<AylikFaturaOzetSatiri> sonuc = new List<AylikFaturaOzetSatiri>();
			if(_belgePanelleri.Count==0&&!TasarimModundaCalisiyorMu())
				BelgePanelleriniHazirla();

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT
								F.FaturaID,
								F.FaturaTarihi,
								IIF(C.adsoyad IS NULL, '', C.adsoyad) AS CariAdi,
								IIF(C.telefon IS NULL, '', C.telefon) AS CariTelefon,
								C.CariTipID,
								IIF(T.TipAdi IS NULL, '', T.TipAdi) AS CariTipi,
								IIF(F.ToplamTutar IS NULL, 0, F.ToplamTutar) AS ToplamTutar,
								(SELECT COUNT(*) FROM FaturaDetay AS FD WHERE FD.FaturaID = F.FaturaID) AS KalemSayisi
							FROM (Faturalar AS F
							LEFT JOIN Cariler AS C ON CLng(IIF(F.CariID IS NULL, 0, F.CariID)) = C.CariID)
							LEFT JOIN CariTipi AS T ON CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = T.CariTipID
							WHERE F.FaturaTarihi>=? AND F.FaturaTarihi<?
							ORDER BY F.FaturaTarihi DESC, F.FaturaID DESC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.Date).Value=baslangic;
					cmd.Parameters.Add("?" , OleDbType.Date).Value=bitis;
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							int? cariTipId = rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"]);
							string tipAdi = Convert.ToString(rd["CariTipi"])??string.Empty;
							if(CariTiptenBelgeTuruGetir(cariTipId , tipAdi)!=belgeTuru)
								continue;

							sonuc.Add(new AylikFaturaOzetSatiri
							{
								FaturaId=rd["FaturaID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["FaturaID"]),
								Tarih=rd["FaturaTarihi"]==DBNull.Value ? baslangic : Convert.ToDateTime(rd["FaturaTarihi"]),
								CariAdi=Convert.ToString(rd["CariAdi"])??string.Empty,
								CariTelefon=Convert.ToString(rd["CariTelefon"])??string.Empty,
								KalemSayisi=rd["KalemSayisi"]==DBNull.Value ? 0 : Convert.ToInt32(rd["KalemSayisi"]),
								ToplamTutar=rd["ToplamTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTutar"])
							});
						}
					}
				}
			}

			return sonuc;
		}

		private DataTable AylikFaturaTablosunuOlustur ( IEnumerable<AylikFaturaOzetSatiri> kayitlar )
		{
			DataTable tablo = new DataTable();
			tablo.Columns.Add("FaturaID" , typeof(int));
			tablo.Columns.Add("Tarih" , typeof(DateTime));
			tablo.Columns.Add("CariAdi" , typeof(string));
			tablo.Columns.Add("CariTelefon" , typeof(string));
			tablo.Columns.Add("KalemSayisi" , typeof(int));
			tablo.Columns.Add("ToplamTutar" , typeof(decimal));

			foreach(AylikFaturaOzetSatiri kayit in kayitlar??Enumerable.Empty<AylikFaturaOzetSatiri>())
			{
				DataRow satir = tablo.NewRow();
				satir["FaturaID"]=kayit.FaturaId;
				satir["Tarih"]=kayit.Tarih;
				satir["CariAdi"]=BosIseYerineGetir(kayit.CariAdi);
				satir["CariTelefon"]=BosIseYerineGetir(kayit.CariTelefon);
				satir["KalemSayisi"]=kayit.KalemSayisi;
				satir["ToplamTutar"]=kayit.ToplamTutar;
				tablo.Rows.Add(satir);
			}

			return tablo;
		}

		private AylikFaturaOzetToplami AylikFaturaToplaminiHesapla ( IEnumerable<AylikFaturaOzetSatiri> kayitlar )
		{
			List<AylikFaturaOzetSatiri> satirlar = ( kayitlar??Enumerable.Empty<AylikFaturaOzetSatiri>() )
				.Where(x => x!=null)
				.ToList();

			return new AylikFaturaOzetToplami
			{
				ToplamTutar=satirlar.Sum(x => x.ToplamTutar),
				FaturaSayisi=satirlar.Count,
				KalemSayisi=satirlar.Sum(x => x.KalemSayisi)
			};
		}

		private List<SatisOzetSatiri> SatisOzetleriniGrupla ( IEnumerable<SatisOzetSatiri> kaynak , bool miktaraGoreSirala = false , bool kayitTuruneGoreAyir = false )
		{
			List<SatisOzetSatiri> gruplanmis = ( kaynak??Enumerable.Empty<SatisOzetSatiri>() )
				.Where(x => x!=null)
				.GroupBy(x => new
				{
					x.UrunId,
					UrunAdi=x.UrunAdi??string.Empty,
					BirimAdi=x.BirimAdi??string.Empty,
					KayitTuru=kayitTuruneGoreAyir
						? GunlukSatisKayitTurunuCoz(x.KayitTuru , x.Miktar)
						: string.Empty
				})
				.Select(grup =>
				{
					decimal toplamMiktar = grup.Sum(x => x.Miktar);
					decimal toplamCiro = grup.Sum(x => x.Ciro);
					decimal toplamMaliyet = grup.Sum(x => x.ToplamMaliyet);
					return new SatisOzetSatiri
					{
						Tarih=grup.Max(x => x.Tarih),
						UrunId=grup.Key.UrunId,
						UrunAdi=grup.Key.UrunAdi,
						BirimAdi=grup.Key.BirimAdi,
						Miktar=toplamMiktar,
						BirimFiyat=toplamMiktar<=0m ? 0m : toplamCiro/toplamMiktar,
						BirimMaliyet=toplamMiktar<=0m ? 0m : toplamMaliyet/toplamMiktar,
						Ciro=toplamCiro,
						ToplamMaliyet=toplamMaliyet,
						KayitTuru=string.IsNullOrWhiteSpace(grup.Key.KayitTuru)
							? GunlukSatisKayitTurunuCoz(grup.Select(x => x.KayitTuru).FirstOrDefault() , toplamMiktar)
							: grup.Key.KayitTuru,
						Kaynak=string.Join(" + " , grup.Select(x => x.Kaynak).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()),
						Aciklama=string.Join(" | " , grup.Select(x => x.Aciklama).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
					};
				})
				.ToList();

			if(miktaraGoreSirala)
				return gruplanmis.OrderByDescending(x => x.Miktar).ThenBy(x => x.UrunAdi , StringComparer.CurrentCultureIgnoreCase).ToList();

			return gruplanmis.OrderByDescending(x => x.Ciro).ThenBy(x => x.UrunAdi , StringComparer.CurrentCultureIgnoreCase).ToList();
		}

		private DataTable SatisOzetTablosunuOlustur ( IEnumerable<SatisOzetSatiri> kayitlar , bool mutlakDegerlerleGoster = false )
		{
			DataTable tablo = new DataTable();
			tablo.Columns.Add("GunlukSatisID" , typeof(int));
			tablo.Columns.Add("Tarih" , typeof(DateTime));
			tablo.Columns.Add("UrunID" , typeof(int));
			tablo.Columns.Add("UrunAdi" , typeof(string));
			tablo.Columns.Add("BirimAdi" , typeof(string));
			tablo.Columns.Add("Miktar" , typeof(decimal));
			tablo.Columns.Add("BirimFiyat" , typeof(decimal));
			tablo.Columns.Add("BirimMaliyet" , typeof(decimal));
			tablo.Columns.Add("Ciro" , typeof(decimal));
			tablo.Columns.Add("ToplamMaliyet" , typeof(decimal));
			tablo.Columns.Add("KarTutari" , typeof(decimal));
			tablo.Columns.Add("KarOrani" , typeof(decimal));
			tablo.Columns.Add("Kaynak" , typeof(string));
			tablo.Columns.Add("KayitTuru" , typeof(string));
			tablo.Columns.Add("Aciklama" , typeof(string));

			foreach(SatisOzetSatiri kayit in kayitlar??Enumerable.Empty<SatisOzetSatiri>())
			{
				decimal miktar = mutlakDegerlerleGoster ? Math.Abs(kayit.Miktar) : kayit.Miktar;
				decimal birimFiyat = mutlakDegerlerleGoster ? Math.Abs(kayit.BirimFiyat) : kayit.BirimFiyat;
				decimal birimMaliyet = mutlakDegerlerleGoster ? Math.Abs(kayit.BirimMaliyet) : kayit.BirimMaliyet;
				decimal ciro = mutlakDegerlerleGoster ? Math.Abs(kayit.Ciro) : kayit.Ciro;
				decimal toplamMaliyet = mutlakDegerlerleGoster ? Math.Abs(kayit.ToplamMaliyet) : kayit.ToplamMaliyet;
				DataRow satir = tablo.NewRow();
				satir["GunlukSatisID"]=kayit.GunlukSatisId.HasValue ? (object)kayit.GunlukSatisId.Value : DBNull.Value;
				satir["Tarih"]=kayit.Tarih;
				satir["UrunID"]=kayit.UrunId;
				satir["UrunAdi"]=BosIseYerineGetir(kayit.UrunAdi);
				satir["BirimAdi"]=BosIseYerineGetir(kayit.BirimAdi);
				satir["Miktar"]=miktar;
				satir["BirimFiyat"]=birimFiyat;
				satir["BirimMaliyet"]=birimMaliyet;
				satir["Ciro"]=ciro;
				satir["ToplamMaliyet"]=toplamMaliyet;
				satir["KarTutari"]=kayit.KarTutari;
				satir["KarOrani"]=kayit.KarOrani;
				satir["Kaynak"]=kayit.Kaynak??string.Empty;
				satir["KayitTuru"]=kayit.KayitTuru??string.Empty;
				satir["Aciklama"]=kayit.Aciklama??string.Empty;
				tablo.Rows.Add(satir);
			}

			return tablo;
		}

		private SatisOzetToplami SatisOzetToplaminiHesapla ( IEnumerable<SatisOzetSatiri> kayitlar )
		{
			List<SatisOzetSatiri> liste = kayitlar?.ToList()??new List<SatisOzetSatiri>();
			return new SatisOzetToplami
			{
				Ciro=liste.Sum(x => x.Ciro),
				Kar=liste.Sum(x => x.KarTutari),
				Miktar=liste.Sum(x => x.Miktar),
				UrunCesidi=liste.Count
			};
		}

		private void GunlukSatisVerileriniYenile ()
		{
			if(!_gunlukSatisSekmesiHazir)
				return;

			GunlukSatisSayfasiVerisiniYenile();
			IadeSayfasiVerisiniYenile();
			GunlukSatisToplamSayfasiVerisiniYenile();
			AylikSatisSayfasiVerisiniYenile();
			AylikFabrikaFaturaSayfasiVerisiniYenile();
			AylikMusteriFaturaSayfasiVerisiniYenile();
			ToplamSatisSayfasiVerisiniYenile();
			GenelToplamSayfasiVerisiniYenile();
		}

		private void GunlukSatisSayfasiVerisiniYenile ()
		{
			if(_gunlukSatisGrid==null)
				return;

			DateTime seciliGun = _gunlukSatisTarihPicker!=null ? _gunlukSatisTarihPicker.Value.Date : DateTime.Today;
			List<SatisOzetSatiri> kayitlar = ManuelSatisKayitlariniGetir(seciliGun , seciliGun.AddDays(1) , GunlukSatisKayitTuruSatis);
			_gunlukSatisGrid.DataSource=SatisOzetTablosunuOlustur(kayitlar);
			SatisOzetGridiniBicimlendir(_gunlukSatisGrid , true , false , true);
			SatisKartDegerleriniGuncelle(SatisOzetToplaminiHesapla(kayitlar) , _gunlukSatisCiroLabel , _gunlukSatisKarLabel , _gunlukSatisKarOraniLabel , _gunlukSatisMiktarLabel);
			GridAramaFiltresiniUygula(_gunlukSatisAramaKutusu , _gunlukSatisGrid);
		}

		private void IadeSayfasiVerisiniYenile ()
		{
			if(_iadeGrid==null)
				return;

			DateTime seciliGun = _iadeTarihPicker!=null ? _iadeTarihPicker.Value.Date : DateTime.Today;
			List<SatisOzetSatiri> kayitlar = ManuelSatisKayitlariniGetir(seciliGun , seciliGun.AddDays(1) , GunlukSatisKayitTuruIade);
			_iadeGrid.DataSource=SatisOzetTablosunuOlustur(kayitlar , true);
			SatisOzetGridiniBicimlendir(_iadeGrid , true , false , true);

			if(_iadeGrid.Columns.Contains("KarTutari"))
				_iadeGrid.Columns["KarTutari"].Visible=false;
			if(_iadeGrid.Columns.Contains("KarOrani"))
				_iadeGrid.Columns["KarOrani"].Visible=false;
			if(_iadeGrid.Columns.Contains("Ciro"))
				_iadeGrid.Columns["Ciro"].HeaderText="TOPLAM İADE";

			IadeKartDegerleriniGuncelle(kayitlar);
			GridAramaFiltresiniUygula(_iadeAramaKutusu , _iadeGrid);
		}

		private void GunlukSatisToplamSayfasiVerisiniYenile ()
		{
			if(_gunlukSatisToplamGrid==null)
				return;

			DateTime seciliGun = _gunlukSatisToplamTarihPicker!=null ? _gunlukSatisToplamTarihPicker.Value.Date : DateTime.Today;
			List<SatisOzetSatiri> kayitlar = SatisOzetleriniGrupla(
				ManuelSatisKayitlariniGetir(seciliGun , seciliGun.AddDays(1))
				.Concat(FaturaSatisKayitlariniGetir(seciliGun , seciliGun.AddDays(1))) ,
				false ,
				true);

			_gunlukSatisToplamGrid.DataSource=SatisOzetTablosunuOlustur(kayitlar);
			SatisOzetGridiniBicimlendir(_gunlukSatisToplamGrid , false , true , false);
			GunlukSatisToplamKartDegerleriniGuncelle(kayitlar);
			GridAramaFiltresiniUygula(_gunlukSatisToplamAramaKutusu , _gunlukSatisToplamGrid);
		}

		private void AylikSatisSayfasiVerisiniYenile ()
		{
			if(_aylikSatisGrid==null)
				return;

			DateTime seciliAy = _aylikSatisAyPicker!=null ? _aylikSatisAyPicker.Value : DateTime.Today;
			DateTime ayBaslangici = new DateTime(seciliAy.Year , seciliAy.Month , 1);
			DateTime sonrakiAy = ayBaslangici.AddMonths(1);
			List<SatisOzetSatiri> kayitlar = SatisOzetleriniGrupla(
				ManuelSatisKayitlariniGetir(ayBaslangici , sonrakiAy)
				.Concat(FaturaSatisKayitlariniGetir(ayBaslangici , sonrakiAy)));

			_aylikSatisGrid.DataSource=SatisOzetTablosunuOlustur(kayitlar);
			SatisOzetGridiniBicimlendir(_aylikSatisGrid , false , true , false);
			SatisKartDegerleriniGuncelle(SatisOzetToplaminiHesapla(kayitlar) , _aylikSatisCiroLabel , _aylikSatisKarLabel , _aylikSatisKarOraniLabel , _aylikSatisMiktarLabel);
			GridAramaFiltresiniUygula(_aylikSatisAramaKutusu , _aylikSatisGrid);
		}

		private void AylikFabrikaFaturaSayfasiVerisiniYenile ()
		{
			AylikFaturaSayfasiniYenile(
				_aylikFabrikaFaturaAyPicker ,
				_aylikFabrikaFaturaAramaKutusu ,
				_aylikFabrikaFaturaGrid ,
				BelgeKayitTuru.FabrikaFaturasi ,
				_aylikFabrikaFaturaToplamLabel ,
				_aylikFabrikaFaturaSayisiLabel ,
				_aylikFabrikaFaturaKalemLabel ,
				_aylikFabrikaFaturaOrtalamaLabel);
		}

		private void AylikMusteriFaturaSayfasiVerisiniYenile ()
		{
			AylikFaturaSayfasiniYenile(
				_aylikMusteriFaturaAyPicker ,
				_aylikMusteriFaturaAramaKutusu ,
				_aylikMusteriFaturaGrid ,
				BelgeKayitTuru.MusteriFaturasi ,
				_aylikMusteriFaturaToplamLabel ,
				_aylikMusteriFaturaSayisiLabel ,
				_aylikMusteriFaturaKalemLabel ,
				_aylikMusteriFaturaOrtalamaLabel);
		}

		private void AylikFaturaSayfasiniYenile (
			DateTimePicker ayPicker ,
			TextBox aramaKutusu ,
			DataGridView grid ,
			BelgeKayitTuru belgeTuru ,
			Label toplamLabel ,
			Label faturaSayisiLabel ,
			Label kalemSayisiLabel ,
			Label ortalamaLabel )
		{
			if(grid==null)
				return;

			DateTime seciliAy = ayPicker!=null ? ayPicker.Value : DateTime.Today;
			DateTime ayBaslangici = new DateTime(seciliAy.Year , seciliAy.Month , 1);
			DateTime sonrakiAy = ayBaslangici.AddMonths(1);
			List<AylikFaturaOzetSatiri> kayitlar = AylikFaturaKayitlariniGetir(ayBaslangici , sonrakiAy , belgeTuru);

			grid.DataSource=AylikFaturaTablosunuOlustur(kayitlar);
			AylikFaturaGridiniBicimlendir(grid);
			AylikFaturaKartDegerleriniGuncelle(
				AylikFaturaToplaminiHesapla(kayitlar) ,
				toplamLabel ,
				faturaSayisiLabel ,
				kalemSayisiLabel ,
				ortalamaLabel);
			GridAramaFiltresiniUygula(aramaKutusu , grid);
		}

		private void ToplamSatisSayfasiVerisiniYenile ()
		{
			if(_toplamSatisGrid==null)
				return;

			DateTime baslangic = new DateTime(2000 , 1 , 1);
			DateTime bitis = DateTime.Today.AddDays(1);
			List<SatisOzetSatiri> kayitlar = SatisOzetleriniGrupla(
				ManuelSatisKayitlariniGetir(baslangic , bitis)
				.Concat(FaturaSatisKayitlariniGetir(baslangic , bitis)) ,
				true);

			_toplamSatisGrid.DataSource=SatisOzetTablosunuOlustur(kayitlar);
			SatisOzetGridiniBicimlendir(_toplamSatisGrid , false , true , false);
			SatisKartDegerleriniGuncelle(SatisOzetToplaminiHesapla(kayitlar) , _toplamSatisCiroLabel , _toplamSatisKarLabel , _toplamSatisKarOraniLabel , _toplamSatisMiktarLabel);
			GridAramaFiltresiniUygula(_toplamSatisAramaKutusu , _toplamSatisGrid);
		}

		private void GenelToplamSayfasiVerisiniYenile ()
		{
			if(_genelToplamGrid==null)
				return;

			DateTime baslangic = new DateTime(2000 , 1 , 1);
			DateTime bitis = DateTime.Today.AddDays(1);
			List<SatisOzetSatiri> satisKayitlari = SatisOzetleriniGrupla(
				ManuelSatisKayitlariniGetir(baslangic , bitis)
				.Concat(FaturaSatisKayitlariniGetir(baslangic , bitis)) ,
				true);
			SatisOzetToplami satisToplami = SatisOzetToplaminiHesapla(satisKayitlari);
			decimal toplamSatisMaliyeti = satisKayitlari.Sum(x => x.ToplamMaliyet);

			decimal toplamToptanciAlimi;
			decimal toplamToptanciOdemesi;
			decimal kalanToptanciBorcu;
			List<GenelToplamSatiri> kayitlar = GenelToplamKayitlariniGetir(
				satisToplami ,
				toplamSatisMaliyeti ,
				out toplamToptanciAlimi ,
				out toplamToptanciOdemesi ,
				out kalanToptanciBorcu);

			_genelToplamGrid.DataSource=GenelToplamTablosunuOlustur(kayitlar);
			GenelToplamGridiniBicimlendir(_genelToplamGrid);
			GenelToplamKartDegerleriniGuncelle(satisToplami.Ciro , satisToplami.Kar , toplamToptanciOdemesi , kalanToptanciBorcu);
			GridAramaFiltresiniUygula(_genelToplamAramaKutusu , _genelToplamGrid);
		}

		private List<GenelToplamSatiri> GenelToplamKayitlariniGetir (
			SatisOzetToplami satisToplami ,
			decimal toplamSatisMaliyeti ,
			out decimal toplamToptanciAlimi ,
			out decimal toplamToptanciOdemesi ,
			out decimal kalanToptanciBorcu )
		{
			List<GenelToplamSatiri> sonuc = new List<GenelToplamSatiri>();
			toplamToptanciAlimi=0m;
			toplamToptanciOdemesi=0m;
			kalanToptanciBorcu=0m;

			sonuc.Add(new GenelToplamSatiri
			{
				KayitTuru="SATIŞ",
				AdSoyad="Tüm satışlar",
				Ciro=satisToplami?.Ciro??0m,
				ToplamMaliyet=toplamSatisMaliyeti,
				KarTutari=satisToplami?.Kar??0m,
				Aciklama=string.Format(
					_yazdirmaKulturu ,
					"{0:N0} ürün, {1:N2} adet satış",
					satisToplami?.UrunCesidi??0 ,
					satisToplami?.Miktar??0m)
			});

			EnsureToptanciAltyapi();
			List<GenelToplamSatiri> toptanciDetaylari = new List<GenelToplamSatiri>();

			if(_toptanciTablosuVar)
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string adIfadesi = ToptanciAdSqlIfadesi("T");
					string sorgu = @"SELECT T.[ToptanciID],
										" + adIfadesi + @" AS AdSoyad,
										(SELECT SUM(IIF(A.[Tutar] IS NULL, 0, A.[Tutar])) FROM [ToptanciAlimlari] AS A WHERE A.[ToptanciID]=T.[ToptanciID]) AS ToplamAlim,
										(SELECT SUM(IIF(O.[OdenenTutar] IS NULL, 0, O.[OdenenTutar])) FROM [ToptanciOdemeleri] AS O WHERE O.[ToptanciID]=T.[ToptanciID]) AS ToplamOdeme
									FROM [Toptancilar] AS T
									ORDER BY " + adIfadesi;

					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
					{
						DataTable dt = new DataTable();
						da.Fill(dt);

						foreach(DataRow satir in dt.Rows)
						{
							decimal toplamAlim = satir["ToplamAlim"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamAlim"]);
							decimal toplamOdeme = satir["ToplamOdeme"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamOdeme"]);
							decimal kalanBakiye = toplamAlim-toplamOdeme;

							if(toplamAlim==0m&&toplamOdeme==0m&&kalanBakiye==0m)
								continue;

							toplamToptanciAlimi+=toplamAlim;
							toplamToptanciOdemesi+=toplamOdeme;
							kalanToptanciBorcu+=kalanBakiye;

							toptanciDetaylari.Add(new GenelToplamSatiri
							{
								KayitTuru="TOPTANCI",
								AdSoyad=BosIseYerineGetir(Convert.ToString(satir["AdSoyad"])),
								ToplamAlim=toplamAlim,
								ToplamOdeme=toplamOdeme,
								KalanBakiye=kalanBakiye,
								Aciklama=kalanBakiye>0m ? "Açık bakiye var." : "Borç kapatılmış."
							});
						}
					}
				}
			}

			sonuc.Add(new GenelToplamSatiri
			{
				KayitTuru="TOPTANCI",
				AdSoyad="Toptancı genel toplamı",
				ToplamAlim=toplamToptanciAlimi,
				ToplamOdeme=toplamToptanciOdemesi,
				KalanBakiye=kalanToptanciBorcu,
				Aciklama=toptanciDetaylari.Count>0
					? string.Format(_yazdirmaKulturu , "{0:N0} toptancı hareketi hesaplandı.", toptanciDetaylari.Count)
					: "Henüz toptancı hareketi yok."
			});

			sonuc.AddRange(
				toptanciDetaylari
					.OrderByDescending(x => x.KalanBakiye)
					.ThenBy(x => x.AdSoyad , StringComparer.CurrentCultureIgnoreCase));

			return sonuc;
		}

		private DataTable GenelToplamTablosunuOlustur ( IEnumerable<GenelToplamSatiri> kayitlar )
		{
			DataTable tablo = new DataTable();
			tablo.Columns.Add("KayitTuru" , typeof(string));
			tablo.Columns.Add("AdSoyad" , typeof(string));
			tablo.Columns.Add("Ciro" , typeof(decimal));
			tablo.Columns.Add("ToplamMaliyet" , typeof(decimal));
			tablo.Columns.Add("KarTutari" , typeof(decimal));
			tablo.Columns.Add("ToplamAlim" , typeof(decimal));
			tablo.Columns.Add("ToplamOdeme" , typeof(decimal));
			tablo.Columns.Add("KalanBakiye" , typeof(decimal));
			tablo.Columns.Add("Aciklama" , typeof(string));

			foreach(GenelToplamSatiri kayit in kayitlar??Enumerable.Empty<GenelToplamSatiri>())
			{
				DataRow satir = tablo.NewRow();
				satir["KayitTuru"]=kayit.KayitTuru??string.Empty;
				satir["AdSoyad"]=kayit.AdSoyad??string.Empty;
				satir["Ciro"]=kayit.Ciro;
				satir["ToplamMaliyet"]=kayit.ToplamMaliyet;
				satir["KarTutari"]=kayit.KarTutari;
				satir["ToplamAlim"]=kayit.ToplamAlim;
				satir["ToplamOdeme"]=kayit.ToplamOdeme;
				satir["KalanBakiye"]=kayit.KalanBakiye;
				satir["Aciklama"]=kayit.Aciklama??string.Empty;
				tablo.Rows.Add(satir);
			}

			return tablo;
		}

		private void GenelToplamKartDegerleriniGuncelle ( decimal ciro , decimal kar , decimal toptanciOdemesi , decimal kalanToptanciBorcu )
		{
			if(_genelToplamCiroLabel!=null)
				_genelToplamCiroLabel.Text=SatisRaporParaMetniGetir(ciro);
			if(_genelToplamKarLabel!=null)
				_genelToplamKarLabel.Text=SatisRaporParaMetniGetir(kar);
			if(_genelToplamToptanciOdemeLabel!=null)
				_genelToplamToptanciOdemeLabel.Text=SatisRaporParaMetniGetir(toptanciOdemesi);
			if(_genelToplamKalanBorcLabel!=null)
				_genelToplamKalanBorcLabel.Text=SatisRaporParaMetniGetir(kalanToptanciBorcu);
		}

		private void SatisKartDegerleriniGuncelle ( SatisOzetToplami toplam , Label ciroLabel , Label karLabel , Label karOraniLabel , Label miktarLabel )
		{
			if(ciroLabel!=null)
				ciroLabel.Text=SatisRaporParaMetniGetir(toplam?.Ciro??0m);
			if(karLabel!=null)
				karLabel.Text=SatisRaporParaMetniGetir(toplam?.Kar??0m);
			if(karOraniLabel!=null)
				karOraniLabel.Text=( toplam?.KarOrani??0m ).ToString("N2" , _yazdirmaKulturu)+" %";
			if(miktarLabel!=null)
				miktarLabel.Text=( toplam?.Miktar??0m ).ToString("N2" , _yazdirmaKulturu);
		}

		private void IadeKartDegerleriniGuncelle ( IEnumerable<SatisOzetSatiri> kayitlar )
		{
			List<SatisOzetSatiri> liste = kayitlar?.ToList()??new List<SatisOzetSatiri>();
			decimal toplamIade = liste.Sum(x => Math.Abs(x.Ciro));
			decimal toplamMaliyet = liste.Sum(x => Math.Abs(x.ToplamMaliyet));
			decimal toplamAdet = liste.Sum(x => Math.Abs(x.Miktar));
			decimal netEtki = -( toplamIade-toplamMaliyet );

			if(_iadeToplamLabel!=null)
				_iadeToplamLabel.Text=SatisRaporParaMetniGetir(toplamIade);
			if(_iadeMaliyetLabel!=null)
				_iadeMaliyetLabel.Text=SatisRaporParaMetniGetir(toplamMaliyet);
			if(_iadeNetEtkiLabel!=null)
				_iadeNetEtkiLabel.Text=SatisRaporParaMetniGetir(netEtki);
			if(_iadeMiktarLabel!=null)
				_iadeMiktarLabel.Text=toplamAdet.ToString("N2" , _yazdirmaKulturu);
		}

		private void GunlukSatisToplamKartDegerleriniGuncelle ( IEnumerable<SatisOzetSatiri> kayitlar )
		{
			List<SatisOzetSatiri> liste = kayitlar?.ToList()??new List<SatisOzetSatiri>();
			SatisOzetToplami toplam = SatisOzetToplaminiHesapla(liste);
			decimal toplamIade = liste
				.Where(x => GunlukSatisKayitTuruEslesiyor(x.KayitTuru , GunlukSatisKayitTuruIade , x.Miktar))
				.Sum(x => Math.Abs(x.Ciro));

			if(_gunlukSatisToplamCiroLabel!=null)
				_gunlukSatisToplamCiroLabel.Text=SatisRaporParaMetniGetir(toplam.Ciro);
			if(_gunlukSatisToplamKarLabel!=null)
				_gunlukSatisToplamKarLabel.Text=SatisRaporParaMetniGetir(toplam.Kar);
			if(_gunlukSatisToplamKarOraniLabel!=null)
				_gunlukSatisToplamKarOraniLabel.Text=toplam.KarOrani.ToString("N2" , _yazdirmaKulturu)+" %";
			if(_gunlukSatisToplamMiktarLabel!=null)
				_gunlukSatisToplamMiktarLabel.Text=SatisRaporParaMetniGetir(toplamIade);
		}

		private void AylikFaturaKartDegerleriniGuncelle (
			AylikFaturaOzetToplami toplam ,
			Label toplamLabel ,
			Label faturaSayisiLabel ,
			Label kalemSayisiLabel ,
			Label ortalamaLabel )
		{
			if(toplamLabel!=null)
				toplamLabel.Text=SatisRaporParaMetniGetir(toplam?.ToplamTutar??0m);
			if(faturaSayisiLabel!=null)
				faturaSayisiLabel.Text=( toplam?.FaturaSayisi??0 ).ToString("N0" , _yazdirmaKulturu);
			if(kalemSayisiLabel!=null)
				kalemSayisiLabel.Text=( toplam?.KalemSayisi??0 ).ToString("N0" , _yazdirmaKulturu);
			if(ortalamaLabel!=null)
				ortalamaLabel.Text=SatisRaporParaMetniGetir(toplam?.OrtalamaTutar??0m);
		}

		private string SatisRaporParaMetniGetir ( decimal tutar )
		{
			return tutar.ToString("N2" , _yazdirmaKulturu)+" TL";
		}

		private void SatisOzetGridiniBicimlendir ( DataGridView grid , bool aciklamaGoster , bool kaynakGoster , bool tarihGoster )
		{
			if(grid==null)
				return;

			if(grid.Columns.Contains("GunlukSatisID"))
				grid.Columns["GunlukSatisID"].Visible=false;
			if(grid.Columns.Contains("UrunID"))
				grid.Columns["UrunID"].Visible=false;
			if(grid.Columns.Contains("KayitTuru"))
				grid.Columns["KayitTuru"].Visible=false;
			if(grid.Columns.Contains("Tarih"))
			{
				grid.Columns["Tarih"].Visible=tarihGoster;
				grid.Columns["Tarih"].HeaderText="TARİH";
				grid.Columns["Tarih"].DefaultCellStyle.Format="dd.MM.yyyy";
			}
			if(grid.Columns.Contains("UrunAdi"))
				grid.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
			if(grid.Columns.Contains("BirimAdi"))
				grid.Columns["BirimAdi"].HeaderText="BİRİM";
			if(grid.Columns.Contains("Miktar"))
			{
				grid.Columns["Miktar"].HeaderText="MİKTAR";
				grid.Columns["Miktar"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("BirimFiyat"))
			{
				grid.Columns["BirimFiyat"].HeaderText="BİRİM SATIŞ";
				grid.Columns["BirimFiyat"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("BirimMaliyet"))
			{
				grid.Columns["BirimMaliyet"].HeaderText="BİRİM MALİYET";
				grid.Columns["BirimMaliyet"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("Ciro"))
			{
				grid.Columns["Ciro"].HeaderText="TOPLAM SATIŞ";
				grid.Columns["Ciro"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("ToplamMaliyet"))
			{
				grid.Columns["ToplamMaliyet"].HeaderText="TOPLAM MALİYET";
				grid.Columns["ToplamMaliyet"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("KarTutari"))
			{
				grid.Columns["KarTutari"].HeaderText="KAR";
				grid.Columns["KarTutari"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("KarOrani"))
			{
				grid.Columns["KarOrani"].HeaderText="KAR ORANI (%)";
				grid.Columns["KarOrani"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("Kaynak"))
			{
				grid.Columns["Kaynak"].Visible=kaynakGoster;
				grid.Columns["Kaynak"].HeaderText="KAYNAK";
			}
			if(grid.Columns.Contains("Aciklama"))
			{
				grid.Columns["Aciklama"].Visible=aciklamaGoster;
				grid.Columns["Aciklama"].HeaderText="NOT";
			}
		}

		private void AylikFaturaGridiniBicimlendir ( DataGridView grid )
		{
			if(grid==null)
				return;

			if(grid.Columns.Contains("FaturaID"))
				grid.Columns["FaturaID"].HeaderText="FATURA NO";
			if(grid.Columns.Contains("Tarih"))
			{
				grid.Columns["Tarih"].HeaderText="TARİH";
				grid.Columns["Tarih"].DefaultCellStyle.Format="dd.MM.yyyy";
			}
			if(grid.Columns.Contains("CariAdi"))
				grid.Columns["CariAdi"].HeaderText="CARİ";
			if(grid.Columns.Contains("CariTelefon"))
				grid.Columns["CariTelefon"].HeaderText="TELEFON";
			if(grid.Columns.Contains("KalemSayisi"))
				grid.Columns["KalemSayisi"].HeaderText="KALEM SAYISI";
			if(grid.Columns.Contains("ToplamTutar"))
			{
				grid.Columns["ToplamTutar"].HeaderText="TOPLAM";
				grid.Columns["ToplamTutar"].DefaultCellStyle.Format="N2";
			}
		}

		private void GenelToplamGridiniBicimlendir ( DataGridView grid )
		{
			if(grid==null)
				return;

			if(grid.Columns.Contains("KayitTuru"))
				grid.Columns["KayitTuru"].HeaderText="KAYIT TÜRÜ";
			if(grid.Columns.Contains("AdSoyad"))
				grid.Columns["AdSoyad"].HeaderText="DETAY";
			if(grid.Columns.Contains("Ciro"))
			{
				grid.Columns["Ciro"].HeaderText="TOPLAM SATIŞ";
				grid.Columns["Ciro"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("ToplamMaliyet"))
			{
				grid.Columns["ToplamMaliyet"].HeaderText="SATIŞ MALİYETİ";
				grid.Columns["ToplamMaliyet"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("KarTutari"))
			{
				grid.Columns["KarTutari"].HeaderText="SATIŞ KARI";
				grid.Columns["KarTutari"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("ToplamAlim"))
			{
				grid.Columns["ToplamAlim"].HeaderText="TOPTANCI ALIŞI";
				grid.Columns["ToplamAlim"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("ToplamOdeme"))
			{
				grid.Columns["ToplamOdeme"].HeaderText="TOPTANCI ÖDEMESİ";
				grid.Columns["ToplamOdeme"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("KalanBakiye"))
			{
				grid.Columns["KalanBakiye"].HeaderText="KALAN TOPTANCI BORCU";
				grid.Columns["KalanBakiye"].DefaultCellStyle.Format="N2";
			}
			if(grid.Columns.Contains("Aciklama"))
				grid.Columns["Aciklama"].HeaderText="NOT";
		}
	}
}

