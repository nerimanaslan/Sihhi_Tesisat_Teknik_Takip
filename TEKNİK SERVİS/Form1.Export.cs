using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	public partial class Form1
	{
		private sealed class CariHesapRaporSatiri
		{
			public string Kaynak;
			public string IslemTuru;
			public string BelgeNo;
			public DateTime? Tarih;
			public decimal BorcTutar;
			public decimal TahsilatTutar;
			public decimal KalanTutar;
			public string Not;
		}

		private sealed class CariHesapRaporVerisi
		{
			public int CariId;
			public string CariAdi;
			public string Telefon;
			public decimal ToplamFatura;
			public decimal ToplamTahsilat;
			public decimal KalanTutar;
			public DateTime? SonFaturaTarihi;
			public DateTime RaporTarihi;
			public List<CariHesapRaporSatiri> Hareketler = new List<CariHesapRaporSatiri>();
		}

		private sealed class ToptanciBakiyeRaporSatiri
		{
			public string IslemTuru;
			public DateTime? Tarih;
			public decimal BorcTutar;
			public decimal OdemeTutar;
			public decimal KalanBakiye;
			public string Not;
		}

		private sealed class ToptanciBakiyeRaporVerisi
		{
			public int ToptanciId;
			public string ToptanciAdi;
			public string Telefon;
			public decimal ToplamAlim;
			public decimal ToplamOdeme;
			public decimal KalanBakiye;
			public DateTime? SonHareketTarihi;
			public DateTime RaporTarihi;
			public List<ToptanciBakiyeRaporSatiri> Hareketler = new List<ToptanciBakiyeRaporSatiri>();
		}

		private void SepetPdfButonu_Click ( object sender , EventArgs e )
		{
			try
			{
				BelgeYazdirmaVerisi veri = SepetYazdirmaVerisiniHazirla();
				if(veri!=null)
					BelgePdfDisaAktar(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Sepet PDF oluÅŸturma hatasÄ±: "+ex.Message);
			}
		}

		private void SepetExcelButonu_Click ( object sender , EventArgs e )
		{
			try
			{
				BelgeYazdirmaVerisi veri = SepetYazdirmaVerisiniHazirla();
				if(veri!=null)
					BelgeExcelDisaAktar(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Sepet Excel oluÅŸturma hatasÄ±: "+ex.Message);
			}
		}

		private void BelgePdfButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null)
				return;

			try
			{
				BelgeYazdirmaVerisi veri = BelgeYazdirmaVerisiniHazirla(panel);
				if(veri!=null)
					BelgePdfDisaAktar(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Belge PDF oluÅŸturma hatasÄ±: "+ex.Message);
			}
		}

		private void BelgeExcelButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null)
				return;

			try
			{
				BelgeYazdirmaVerisi veri = BelgeYazdirmaVerisiniHazirla(panel);
				if(veri!=null)
					BelgeExcelDisaAktar(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Belge Excel oluÅŸturma hatasÄ±: "+ex.Message);
			}
		}

		private void BelgePdfDisaAktar ( BelgeYazdirmaVerisi veri )
		{
			string dosyaYolu = BelgeKaydetmeYoluSec(veri , "pdf" , "PDF DosyasÄ± (*.pdf)|*.pdf");
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			BelgePdfDosyasiOlustur(veri , dosyaYolu);
			MessageBox.Show("PDF dosyasÄ± oluÅŸturuldu.\n"+dosyaYolu , "PDF HazÄ±r" , MessageBoxButtons.OK , MessageBoxIcon.Information);
		}

		private void BelgeExcelDisaAktar ( BelgeYazdirmaVerisi veri )
		{
			string dosyaYolu = BelgeKaydetmeYoluSec(veri , "xlsx" , "Excel DosyasÄ± (*.xlsx)|*.xlsx");
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			BelgeExcelDosyasiOlustur(veri , dosyaYolu);
			MessageBox.Show("Excel dosyasÄ± oluÅŸturuldu.\n"+dosyaYolu , "Excel HazÄ±r" , MessageBoxButtons.OK , MessageBoxIcon.Information);
		}

		private void GenelToplamYazdirButonu_Click ( object sender , EventArgs e )
		{
			if(!GenelToplamRaporundaVeriVarMi())
			{
				MessageBox.Show("Genel toplam raporunda yazdirilacak kayit bulunamadi." , "Yazdirma" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			List<Bitmap> sayfalar = GenelToplamRaporSayfalariniOlustur();
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Genel toplam raporu icin sayfa olusturulamadi." , "Yazdirma" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniYazdirmaOnizlemeAc(sayfalar , "Genel Toplam Raporu");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Genel toplam yazdirma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void GenelToplamPdfButonu_Click ( object sender , EventArgs e )
		{
			if(!GenelToplamRaporundaVeriVarMi())
			{
				MessageBox.Show("Genel toplam raporunda PDF olusturulacak kayit bulunamadi." , "PDF" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			string dosyaYolu = RaporKaydetmeYoluSec("Genel Toplam PDF Kaydet" , "pdf" , "PDF Dosyasi (*.pdf)|*.pdf" , "Genel_Toplam_Raporu");
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			List<Bitmap> sayfalar = GenelToplamRaporSayfalariniOlustur();
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Genel toplam raporu icin sayfa olusturulamadi." , "PDF" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniPdfDosyasinaYaz(sayfalar , dosyaYolu , "PDF icin sayfa olusturulamadi.");
				MessageBox.Show("PDF dosyasi olusturuldu.\n"+dosyaYolu , "PDF Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Genel toplam PDF olusturma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void GenelToplamExcelButonu_Click ( object sender , EventArgs e )
		{
			if(!GenelToplamRaporundaVeriVarMi())
			{
				MessageBox.Show("Genel toplam raporunda Excel'e aktarilacak kayit bulunamadi." , "Excel" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			string dosyaYolu = RaporKaydetmeYoluSec("Genel Toplam Excel Kaydet" , "xlsx" , "Excel Dosyasi (*.xlsx)|*.xlsx" , "Genel_Toplam_Raporu");
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			try
			{
				GenelToplamExcelDosyasiOlustur(dosyaYolu);
				MessageBox.Show("Excel dosyasi olusturuldu.\n"+dosyaYolu , "Excel Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Genel toplam Excel aktarma hatasi: "+ex.Message);
			}
		}

		private void CariHesapYazdirButonu_Click ( object sender , EventArgs e )
		{
			CariHesapRaporVerisi veri = CariHesapRaporVerisiniHazirla();
			if(veri==null)
				return;

			List<Bitmap> sayfalar = CariHesapRaporSayfalariniOlustur(veri);
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Cari hesap raporu icin sayfa olusturulamadi." , "Yazdirma" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniYazdirmaOnizlemeAc(sayfalar , "Cari Hesap Raporu");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap yazdirma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void CariHesapPdfButonu_Click ( object sender , EventArgs e )
		{
			CariHesapRaporVerisi veri = CariHesapRaporVerisiniHazirla();
			if(veri==null)
				return;

			string onEk = "Cari_Hesap_"+DosyaAdiParcasiTemizle(veri.CariAdi);
			string dosyaYolu = RaporKaydetmeYoluSec("Cari Hesap PDF Kaydet" , "pdf" , "PDF Dosyasi (*.pdf)|*.pdf" , onEk);
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			List<Bitmap> sayfalar = CariHesapRaporSayfalariniOlustur(veri);
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Cari hesap raporu icin sayfa olusturulamadi." , "PDF" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniPdfDosyasinaYaz(sayfalar , dosyaYolu , "Cari hesap PDF'i icin sayfa olusturulamadi.");
				MessageBox.Show("PDF dosyasi olusturuldu.\n"+dosyaYolu , "PDF Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap PDF olusturma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void CariHesapExcelButonu_Click ( object sender , EventArgs e )
		{
			CariHesapRaporVerisi veri = CariHesapRaporVerisiniHazirla();
			if(veri==null)
				return;

			string onEk = "Cari_Hesap_"+DosyaAdiParcasiTemizle(veri.CariAdi);
			string dosyaYolu = RaporKaydetmeYoluSec("Cari Hesap Excel Kaydet" , "xlsx" , "Excel Dosyasi (*.xlsx)|*.xlsx" , onEk);
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			try
			{
				CariHesapExcelDosyasiOlustur(veri , dosyaYolu);
				MessageBox.Show("Excel dosyasi olusturuldu.\n"+dosyaYolu , "Excel Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap Excel aktarma hatasi: "+ex.Message);
			}
		}

		private void ToptanciBakiyeYazdirButonu_Click ( object sender , EventArgs e )
		{
			ToptanciBakiyeRaporVerisi veri = ToptanciBakiyeRaporVerisiniHazirla();
			if(veri==null)
				return;

			List<Bitmap> sayfalar = ToptanciBakiyeRaporSayfalariniOlustur(veri);
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Toptanci bakiye raporu icin sayfa olusturulamadi." , "Yazdirma" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniYazdirmaOnizlemeAc(sayfalar , "Toptanci Bakiye Raporu");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptanci bakiye yazdirma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void ToptanciBakiyePdfButonu_Click ( object sender , EventArgs e )
		{
			ToptanciBakiyeRaporVerisi veri = ToptanciBakiyeRaporVerisiniHazirla();
			if(veri==null)
				return;

			string onEk = "Toptanci_Bakiye_"+DosyaAdiParcasiTemizle(veri.ToptanciAdi);
			string dosyaYolu = RaporKaydetmeYoluSec("Toptanci Bakiye PDF Kaydet" , "pdf" , "PDF Dosyasi (*.pdf)|*.pdf" , onEk);
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			List<Bitmap> sayfalar = ToptanciBakiyeRaporSayfalariniOlustur(veri);
			if(sayfalar.Count==0)
			{
				MessageBox.Show("Toptanci bakiye raporu icin sayfa olusturulamadi." , "PDF" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				BitmapSayfalariniPdfDosyasinaYaz(sayfalar , dosyaYolu , "Toptanci bakiye PDF'i icin sayfa olusturulamadi.");
				MessageBox.Show("PDF dosyasi olusturuldu.\n"+dosyaYolu , "PDF Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptanci bakiye PDF olusturma hatasi: "+ex.Message);
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void ToptanciBakiyeExcelButonu_Click ( object sender , EventArgs e )
		{
			ToptanciBakiyeRaporVerisi veri = ToptanciBakiyeRaporVerisiniHazirla();
			if(veri==null)
				return;

			string onEk = "Toptanci_Bakiye_"+DosyaAdiParcasiTemizle(veri.ToptanciAdi);
			string dosyaYolu = RaporKaydetmeYoluSec("Toptanci Bakiye Excel Kaydet" , "xlsx" , "Excel Dosyasi (*.xlsx)|*.xlsx" , onEk);
			if(string.IsNullOrWhiteSpace(dosyaYolu))
				return;

			try
			{
				ToptanciBakiyeExcelDosyasiOlustur(veri , dosyaYolu);
				MessageBox.Show("Excel dosyasi olusturuldu.\n"+dosyaYolu , "Excel Hazir" , MessageBoxButtons.OK , MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptanci bakiye Excel aktarma hatasi: "+ex.Message);
			}
		}

		private ToptanciBakiyeRaporVerisi ToptanciBakiyeRaporVerisiniHazirla ()
		{
			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			if(!toptanciId.HasValue)
			{
				MessageBox.Show("Rapor almak icin once toptanci secin!" , "Toptanci Bakiye" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return null;
			}

			ToptanciBakiyeRaporVerisi veri = new ToptanciBakiyeRaporVerisi
			{
				ToptanciId=toptanciId.Value,
				RaporTarihi=DateTime.Now,
				ToplamAlim=ToptanciToplamAlimGetir(toptanciId.Value),
				ToplamOdeme=ToptanciToplamOdemeGetir(toptanciId.Value)
			};
			veri.KalanBakiye=veri.ToplamAlim-veri.ToplamOdeme;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string adIfadesi = ToptanciAdSqlIfadesi("T");
					using(OleDbCommand cmd = new OleDbCommand("SELECT " + adIfadesi + " AS AdSoyad, IIF([Telefon] IS NULL, '', [Telefon]) AS Telefon FROM [Toptancilar] AS T WHERE [ToptanciID]=?" , conn))
					{
						cmd.Parameters.AddWithValue("?" , toptanciId.Value);
						using(OleDbDataReader rd = cmd.ExecuteReader())
						{
							if(rd!=null&&rd.Read())
							{
								veri.ToptanciAdi=Convert.ToString(rd["AdSoyad"])??string.Empty;
								veri.Telefon=Convert.ToString(rd["Telefon"])??string.Empty;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptanci bakiye raporu hazirlanamadi: "+ex.Message , "Toptanci Bakiye" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return null;
			}

			if(string.IsNullOrWhiteSpace(veri.ToptanciAdi))
				veri.ToptanciAdi=Convert.ToString(_toptanciBakiyeSecimComboBox?.Text)??("Toptanci #"+toptanciId.Value.ToString(CultureInfo.InvariantCulture));

			if(dataGridView27!=null)
			{
				foreach(DataGridViewRow row in dataGridView27.Rows)
				{
					if(row==null||row.IsNewRow||!row.Visible)
						continue;

					ToptanciBakiyeRaporSatiri satir = new ToptanciBakiyeRaporSatiri
					{
						IslemTuru=CariHesapHucreMetniGetir(row , "IslemTuru"),
						Tarih=CariHesapHucreTarihGetir(row , "Tarih"),
						BorcTutar=CariHesapHucreDecimalGetir(row , "BorcTutar"),
						OdemeTutar=CariHesapHucreDecimalGetir(row , "OdemeTutar"),
						KalanBakiye=CariHesapHucreDecimalGetir(row , "KalanBakiye"),
						Not=CariHesapHucreMetniGetir(row , "Aciklama")
					};
					veri.Hareketler.Add(satir);
					if(satir.Tarih.HasValue&&(!veri.SonHareketTarihi.HasValue||satir.Tarih.Value>veri.SonHareketTarihi.Value))
						veri.SonHareketTarihi=satir.Tarih.Value;
				}
			}

			return veri;
		}

		private CariHesapRaporVerisi CariHesapRaporVerisiniHazirla ()
		{
			int? cariId = SeciliCariHesapIdGetir();
			if(!cariId.HasValue)
			{
				MessageBox.Show("Rapor almak icin once cari secin!" , "Cari Hesap" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return null;
			}

			CariHesapRaporVerisi veri = new CariHesapRaporVerisi
			{
				CariId=cariId.Value,
				RaporTarihi=DateTime.Now,
				ToplamFatura=CariHesapToplamFaturaGetir(cariId.Value),
				ToplamTahsilat=CariHesapToplamTahsilatGetir(cariId.Value)
			};
			veri.KalanTutar=veri.ToplamFatura-veri.ToplamTahsilat;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					using(OleDbCommand cmd = new OleDbCommand("SELECT IIF([adsoyad] IS NULL, '', [adsoyad]) AS AdSoyad, IIF([telefon] IS NULL, '', [telefon]) AS Telefon FROM [Cariler] WHERE [CariID]=?" , conn))
					{
						cmd.Parameters.AddWithValue("?" , cariId.Value);
						using(OleDbDataReader rd = cmd.ExecuteReader())
						{
							if(rd!=null&&rd.Read())
							{
								veri.CariAdi=Convert.ToString(rd["AdSoyad"])??string.Empty;
								veri.Telefon=Convert.ToString(rd["Telefon"])??string.Empty;
							}
						}
					}

					if(_cariHesapFaturaTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("SELECT MAX([FaturaTarihi]) FROM [Faturalar] WHERE CLng(IIF([CariID] IS NULL, 0, [CariID]))=?" , conn))
						{
							cmd.Parameters.AddWithValue("?" , cariId.Value);
							object sonuc = cmd.ExecuteScalar();
							if(sonuc!=null&&sonuc!=DBNull.Value)
								veri.SonFaturaTarihi=Convert.ToDateTime(sonuc);
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap raporu hazirlanamadi: "+ex.Message , "Cari Hesap" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return null;
			}

			if(string.IsNullOrWhiteSpace(veri.CariAdi))
				veri.CariAdi=Convert.ToString(_cariHesapCariComboBox?.Text)??("Cari #"+cariId.Value.ToString(CultureInfo.InvariantCulture));

			if(_cariHesapHareketGrid!=null)
			{
				foreach(DataGridViewRow row in _cariHesapHareketGrid.Rows)
				{
					if(row==null||row.IsNewRow||!row.Visible)
						continue;

					veri.Hareketler.Add(new CariHesapRaporSatiri
					{
						Kaynak=CariHesapHucreMetniGetir(row , "Kaynak"),
						IslemTuru=CariHesapHucreMetniGetir(row , "IslemTuru"),
						BelgeNo=CariHesapHucreMetniGetir(row , "BelgeNo"),
						Tarih=CariHesapHucreTarihGetir(row , "Tarih"),
						BorcTutar=CariHesapHucreDecimalGetir(row , "BorcTutar"),
						TahsilatTutar=CariHesapHucreDecimalGetir(row , "TahsilatTutar"),
						KalanTutar=CariHesapHucreDecimalGetir(row , "KalanTutar"),
						Not=CariHesapHucreMetniGetir(row , "Aciklama")
					});
				}
			}

			return veri;
		}

		private string CariHesapHucreMetniGetir ( DataGridViewRow row , string kolonAdi )
		{
			if(row?.DataGridView==null||!row.DataGridView.Columns.Contains(kolonAdi))
				return string.Empty;

			object deger = row.Cells[kolonAdi].Value;
			return deger==null||deger==DBNull.Value ? string.Empty : Convert.ToString(deger)??string.Empty;
		}

		private decimal CariHesapHucreDecimalGetir ( DataGridViewRow row , string kolonAdi )
		{
			if(row?.DataGridView==null||!row.DataGridView.Columns.Contains(kolonAdi))
				return 0m;

			return GenelToplamSayisalDegerGetir(row.Cells[kolonAdi].Value);
		}

		private DateTime? CariHesapHucreTarihGetir ( DataGridViewRow row , string kolonAdi )
		{
			if(row?.DataGridView==null||!row.DataGridView.Columns.Contains(kolonAdi))
				return null;

			object deger = row.Cells[kolonAdi].Value;
			if(deger==null||deger==DBNull.Value)
				return null;

			DateTime tarih;
			if(DateTime.TryParse(Convert.ToString(deger) , _yazdirmaKulturu , DateTimeStyles.None , out tarih))
				return tarih;

			try
			{
				tarih=Convert.ToDateTime(deger);
				return tarih==DateTime.MinValue ? (DateTime?)null : tarih;
			}
			catch
			{
				return null;
			}
		}

		private List<Bitmap> CariHesapRaporSayfalariniOlustur ( CariHesapRaporVerisi veri )
		{
			List<Bitmap> sayfalar = new List<Bitmap>();
			if(veri==null)
				return sayfalar;

			const int sayfaGenisligi = 1754;
			const int sayfaYuksekligi = 1240;
			const int yatayBosluk = 64;
			const int ustBosluk = 40;
			const int altBosluk = 48;
			const int satirYuksekligi = 42;
			const int tabloBaslikYuksekligi = 48;
			int icerikGenisligi = sayfaGenisligi-( yatayBosluk*2 );
			string[] basliklar = { "Kaynak" , "Islem Turu" , "Belge No" , "Tarih" , "Alinan Urun Tutari" , "Alinan Tahsilat" , "Kalan Tutar" , "Not" };
			int cizilecekSatirSayisi = Math.Max(veri.Hareketler.Count , 1);
			int satirIndex = 0;
			int sayfaNo = 0;

			while(satirIndex<cizilecekSatirSayisi)
			{
				Bitmap sayfa = new Bitmap(sayfaGenisligi , sayfaYuksekligi , PixelFormat.Format24bppRgb);
				sayfa.SetResolution(150f , 150f);
				sayfalar.Add(sayfa);

				using(Graphics g = Graphics.FromImage(sayfa))
				using(Font bannerFont = new Font("Segoe UI Semibold" , 23f , FontStyle.Bold))
				using(Font bannerAltFont = new Font("Segoe UI" , 10.2f , FontStyle.Regular))
				using(Font panelBaslikFont = new Font("Segoe UI Semibold" , 10.2f , FontStyle.Bold))
				using(Font panelDegerFont = new Font("Segoe UI Semibold" , 16.5f , FontStyle.Bold))
				using(Font buyukCariFont = new Font("Segoe UI Semibold" , 16.2f , FontStyle.Bold))
				using(Font textFont = new Font("Segoe UI" , 9.6f , FontStyle.Regular))
				using(Font textBoldFont = new Font("Segoe UI Semibold" , 9.4f , FontStyle.Bold))
				using(Font tableHeaderFont = new Font("Segoe UI Semibold" , 9.4f , FontStyle.Bold))
				using(Font footerFont = new Font("Segoe UI" , 8.6f , FontStyle.Regular))
				using(Pen kenarlikKalemi = new Pen(Color.FromArgb(226 , 232 , 240)))
				using(Pen ayiriciKalem = new Pen(Color.FromArgb(226 , 232 , 240)))
				{
					g.Clear(Color.FromArgb(244 , 247 , 251));
					g.SmoothingMode=SmoothingMode.AntiAlias;
					g.InterpolationMode=InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode=PixelOffsetMode.HighQuality;
					g.CompositingQuality=CompositingQuality.HighQuality;
					g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;

					int y = ustBosluk;

					Rectangle banner = new Rectangle(yatayBosluk , y , icerikGenisligi , 116);
					Rectangle bannerGolge = new Rectangle(banner.X , banner.Y+8 , banner.Width , banner.Height);
					using(GraphicsPath bannerGolgeYolu = YuvarlatilmisDikdortgenOlustur(bannerGolge , 28))
					using(SolidBrush bannerGolgeFirca = new SolidBrush(Color.FromArgb(18 , 15 , 23 , 42)))
						g.FillPath(bannerGolgeFirca , bannerGolgeYolu);
					using(GraphicsPath bannerYolu = YuvarlatilmisDikdortgenOlustur(banner , 28))
					using(LinearGradientBrush bannerFirca = new LinearGradientBrush(banner , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(30 , 64 , 175) , LinearGradientMode.Horizontal))
					using(Pen bannerKalemi = new Pen(Color.FromArgb(30 , 41 , 59)))
					{
						g.FillPath(bannerFirca , bannerYolu);
						g.DrawPath(bannerKalemi , bannerYolu);

						GraphicsState bannerDurumu = g.Save();
						g.SetClip(bannerYolu);
						using(SolidBrush vurguFirca = new SolidBrush(Color.FromArgb(56 , 189 , 248)))
							g.FillRectangle(vurguFirca , new Rectangle(banner.X , banner.Bottom-6 , banner.Width , 6));
						g.Restore(bannerDurumu);
					}

					Rectangle logoAlani = new Rectangle(banner.X+24 , banner.Y+22 , 82 , 62);
					YazdirmaLogoCiz(g , logoAlani);
					Rectangle sagBilgiKutusu = new Rectangle(banner.Right-366 , banner.Y+20 , 340 , 74);
					int bannerMetinX = logoAlani.Right+18;
					int bannerMetinGenisligi = Math.Max(320 , sagBilgiKutusu.X-bannerMetinX-28);

					RaporMetinCiz(
						g ,
						"CARI HESAP RAPORU" ,
						bannerFont ,
						new Rectangle(bannerMetinX , banner.Y+20 , bannerMetinGenisligi , 38) ,
						Color.White ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Faturalar, tahsilatlar ve kalan bakiyeler bu raporda tek gorunumde sunulur." ,
						bannerAltFont ,
						new Rectangle(bannerMetinX , banner.Y+58 , bannerMetinGenisligi , 24) ,
						Color.FromArgb(191 , 219 , 254) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					RaporYuvarlatilmisKutuCiz(
						g ,
						sagBilgiKutusu ,
						Color.FromArgb(34 , 255 , 255 , 255) ,
						Color.FromArgb(58 , 148 , 163 , 184) ,
						18 ,
						0 ,
						Color.Transparent);
					RaporMetinCiz(
						g ,
						YazdirmaSirketAdi ,
						panelBaslikFont ,
						new Rectangle(sagBilgiKutusu.X+18 , sagBilgiKutusu.Y+14 , sagBilgiKutusu.Width-36 , 20) ,
						Color.White ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Olusturulma: "+veri.RaporTarihi.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) ,
						textFont ,
						new Rectangle(sagBilgiKutusu.X+18 , sagBilgiKutusu.Y+36 , 224 , 20) ,
						Color.FromArgb(219 , 234 , 254) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Sayfa "+( sayfaNo+1 ).ToString(CultureInfo.InvariantCulture) ,
						textBoldFont ,
						new Rectangle(sagBilgiKutusu.Right-110 , sagBilgiKutusu.Y+36 , 92 , 20) ,
						Color.FromArgb(224 , 231 , 255) ,
						TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					y=banner.Bottom+22;

					Rectangle bilgiKutusu = new Rectangle(yatayBosluk , y , icerikGenisligi , 118);
					RaporYuvarlatilmisKutuCiz(
						g ,
						bilgiKutusu ,
						Color.White ,
						Color.FromArgb(226 , 232 , 240) ,
						22 ,
						5 ,
						Color.FromArgb(14 , 15 , 23 , 42));

					Rectangle cariOzetAlani = new Rectangle(bilgiKutusu.X+24 , bilgiKutusu.Y+20 , 520 , bilgiKutusu.Height-40);
					RaporMetinCiz(
						g ,
						"Cari Bilgileri" ,
						panelBaslikFont ,
						new Rectangle(cariOzetAlani.X , cariOzetAlani.Y , 160 , 20) ,
						Color.FromArgb(71 , 85 , 105) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						BosIseYerineGetir(veri.CariAdi) ,
						buyukCariFont ,
						new Rectangle(cariOzetAlani.X , cariOzetAlani.Y+24 , cariOzetAlani.Width , 30) ,
						Color.FromArgb(15 , 23 , 42) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Cari ID: "+veri.CariId.ToString("N0" , _yazdirmaKulturu) ,
						textFont ,
						new Rectangle(cariOzetAlani.X , cariOzetAlani.Y+58 , 180 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Secili cari icin hesap ekstresi ve hareket ozeti." ,
						textFont ,
						new Rectangle(cariOzetAlani.X+146 , cariOzetAlani.Y+58 , cariOzetAlani.Width-146 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					int miniKartX = cariOzetAlani.Right+18;
					int miniKartBosluk = 12;
					int miniKartGenisligi = ( bilgiKutusu.Right-miniKartX-24-( miniKartBosluk*2 ) )/3;
					Rectangle[] bilgiKartlari =
					{
						new Rectangle(miniKartX , bilgiKutusu.Y+20 , miniKartGenisligi , 78),
						new Rectangle(miniKartX+miniKartGenisligi+miniKartBosluk , bilgiKutusu.Y+20 , miniKartGenisligi , 78),
						new Rectangle(miniKartX+( miniKartGenisligi+miniKartBosluk )*2 , bilgiKutusu.Y+20 , miniKartGenisligi , 78)
					};
					string[] bilgiBasliklari =
					{
						"Telefon",
						"Son Fatura",
						"Hareket"
					};
					string[] bilgiDegerleri =
					{
						BosIseYerineGetir(veri.Telefon),
						veri.SonFaturaTarihi.HasValue ? veri.SonFaturaTarihi.Value.ToString("dd.MM.yyyy" , _yazdirmaKulturu) : "-",
						veri.Hareketler.Count.ToString("N0" , _yazdirmaKulturu)+" kayit"
					};
					Color[] bilgiKartArkaPlanlari =
					{
						Color.FromArgb(239 , 246 , 255),
						Color.FromArgb(240 , 253 , 250),
						Color.FromArgb(255 , 247 , 237)
					};
					Color[] bilgiKartVurgulari =
					{
						Color.FromArgb(37 , 99 , 235),
						Color.FromArgb(13 , 148 , 136),
						Color.FromArgb(234 , 88 , 12)
					};

					for(int i = 0 ; i<bilgiKartlari.Length ; i++)
					{
						RaporYuvarlatilmisKutuCiz(
							g ,
							bilgiKartlari[i] ,
							bilgiKartArkaPlanlari[i] ,
							Color.FromArgb(219 , 234 , 254) ,
							16 ,
							0 ,
							Color.Transparent);
						RaporUstSeritCiz(g , bilgiKartlari[i] , 16 , bilgiKartVurgulari[i] , 5);
						RaporMetinCiz(
							g ,
							bilgiBasliklari[i] ,
							textBoldFont ,
							new Rectangle(bilgiKartlari[i].X+16 , bilgiKartlari[i].Y+16 , bilgiKartlari[i].Width-32 , 18) ,
							Color.FromArgb(71 , 85 , 105) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						RaporMetinCiz(
							g ,
							bilgiDegerleri[i] ,
							textBoldFont ,
							new Rectangle(bilgiKartlari[i].X+16 , bilgiKartlari[i].Y+40 , bilgiKartlari[i].Width-32 , 20) ,
							Color.FromArgb(15 , 23 , 42) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					}

					y=bilgiKutusu.Bottom+18;

					string durumMetni = CariHesapDurumMetniGetir(veri.KalanTutar);
					Color kalanKartRenk = veri.KalanTutar<0m ? Color.FromArgb(22 , 163 , 74) : Color.FromArgb(234 , 88 , 12);
					Color kalanKartArkaPlan = veri.KalanTutar<0m ? Color.FromArgb(240 , 253 , 244) : Color.FromArgb(255 , 247 , 237);
					Color durumKartRenk = veri.KalanTutar==0m
						? Color.FromArgb(37 , 99 , 235)
						: ( veri.KalanTutar<0m ? Color.FromArgb(22 , 163 , 74) : Color.FromArgb(100 , 116 , 139) );
					Color durumKartArkaPlan = veri.KalanTutar==0m
						? Color.FromArgb(239 , 246 , 255)
						: ( veri.KalanTutar<0m ? Color.FromArgb(240 , 253 , 244) : Color.FromArgb(248 , 250 , 252) );

					int kartBosluk = 14;
					int kartGenisligi = ( icerikGenisligi-( kartBosluk*3 ) )/4;
					Rectangle[] kartlar =
					{
						new Rectangle(yatayBosluk , y , kartGenisligi , 92),
						new Rectangle(yatayBosluk+kartGenisligi+kartBosluk , y , kartGenisligi , 92),
						new Rectangle(yatayBosluk+( kartGenisligi+kartBosluk )*2 , y , kartGenisligi , 92),
						new Rectangle(yatayBosluk+( kartGenisligi+kartBosluk )*3 , y , kartGenisligi , 92)
					};
					Color[] kartArkaPlanlari =
					{
						Color.FromArgb(239 , 246 , 255),
						Color.FromArgb(240 , 253 , 250),
						kalanKartArkaPlan,
						durumKartArkaPlan
					};
					Color[] kartRenkleri =
					{
						Color.FromArgb(37 , 99 , 235),
						Color.FromArgb(13 , 148 , 136),
						kalanKartRenk,
						durumKartRenk
					};
					string[] kartBasliklari = { "Toplam Fatura" , "Toplam Tahsilat" , "Kalan Tutar" , "Durum" };
					string[] kartDegerleri =
					{
						SatisRaporParaMetniGetir(veri.ToplamFatura),
						SatisRaporParaMetniGetir(veri.ToplamTahsilat),
						SatisRaporParaMetniGetir(veri.KalanTutar),
						durumMetni
					};

					for(int i = 0 ; i<kartlar.Length ; i++)
					{
						RaporYuvarlatilmisKutuCiz(
							g ,
							kartlar[i] ,
							kartArkaPlanlari[i] ,
							Color.FromArgb(226 , 232 , 240) ,
							18 ,
							4 ,
							Color.FromArgb(10 , 15 , 23 , 42));
						RaporUstSeritCiz(g , kartlar[i] , 18 , kartRenkleri[i] , 6);
						RaporMetinCiz(
							g ,
							kartBasliklari[i].ToUpper(_yazdirmaKulturu) ,
							panelBaslikFont ,
							new Rectangle(kartlar[i].X+16 , kartlar[i].Y+16 , kartlar[i].Width-32 , 18) ,
							Color.FromArgb(71 , 85 , 105) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						RaporMetinCiz(
							g ,
							kartDegerleri[i] ,
							panelDegerFont ,
							new Rectangle(kartlar[i].X+16 , kartlar[i].Y+42 , kartlar[i].Width-32 , 28) ,
							i==3 ? kartRenkleri[i] : Color.FromArgb(15 , 23 , 42) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					}

					y=kartlar[0].Bottom+20;

					Rectangle tabloPaneli = new Rectangle(yatayBosluk , y , icerikGenisligi , sayfaYuksekligi-altBosluk-y-34);
					RaporYuvarlatilmisKutuCiz(
						g ,
						tabloPaneli ,
						Color.White ,
						Color.FromArgb(226 , 232 , 240) ,
						22 ,
						5 ,
						Color.FromArgb(12 , 15 , 23 , 42));

					Rectangle tabloAlan = RectangleIcBoslukGetir(tabloPaneli , 22 , 20);
					RaporMetinCiz(
						g ,
						"Hesap Hareketleri" ,
						buyukCariFont ,
						new Rectangle(tabloAlan.X , tabloAlan.Y , 280 , 26) ,
						Color.FromArgb(15 , 23 , 42) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Otomatik faturalar ve manuel tahsilatlar tek listede gosterilir." ,
						textFont ,
						new Rectangle(tabloAlan.X , tabloAlan.Y+28 , 520 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporRozetiCiz(
						g ,
						new Rectangle(tabloAlan.Right-164 , tabloAlan.Y+2 , 164 , 28) ,
						veri.Hareketler.Count.ToString("N0" , _yazdirmaKulturu)+" kayit" ,
						textBoldFont ,
						Color.FromArgb(239 , 246 , 255) ,
						Color.FromArgb(30 , 64 , 175));

					int tabloX = tabloAlan.X;
					int tabloY = tabloAlan.Y+60;
					int tabloGenisligi = tabloAlan.Width;
					int notSutunGenisligi = Math.Max(250 , tabloGenisligi-1186);
					int[] sutunGenislikleri = { 126 , 160 , 198 , 176 , 170 , 170 , 170 , notSutunGenisligi };
					int kullanilanGenislik = 0;
					for(int i = 0 ; i<sutunGenislikleri.Length ; i++)
						kullanilanGenislik+=sutunGenislikleri[i];
					sutunGenislikleri[sutunGenislikleri.Length-1]+=tabloGenisligi-kullanilanGenislik;

					int x = tabloX;
					for(int i = 0 ; i<basliklar.Length ; i++)
					{
						Rectangle hucre = new Rectangle(x , tabloY , sutunGenislikleri[i] , tabloBaslikYuksekligi);
						using(SolidBrush baslikFirca = new SolidBrush(Color.FromArgb(15 , 118 , 110)))
							g.FillRectangle(baslikFirca , hucre);
						if(i>0)
							g.DrawLine(kenarlikKalemi , x , hucre.Y+10 , x , hucre.Bottom-10);
						RaporMetinCiz(
							g ,
							basliklar[i] ,
							tableHeaderFont ,
							RectangleIcBoslukGetir(hucre , 12 , 0) ,
							Color.White ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						x+=sutunGenislikleri[i];
					}

					int tabloSatirY = tabloY+tabloBaslikYuksekligi;
					int tabloAltSinir = tabloPaneli.Bottom-26;

					if(veri.Hareketler.Count==0)
					{
						Rectangle bosAlan = new Rectangle(tabloX , tabloSatirY , tabloGenisligi , Math.Max(78 , tabloAltSinir-tabloSatirY));
						using(SolidBrush bosFirca = new SolidBrush(Color.FromArgb(248 , 250 , 252)))
							g.FillRectangle(bosFirca , bosAlan);
						g.DrawRectangle(ayiriciKalem , bosAlan);
						RaporMetinCiz(
							g ,
							"Secili cari icin gorunur hareket kaydi bulunamadi." ,
							textFont ,
							bosAlan ,
							Color.FromArgb(100 , 116 , 139) ,
							TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						satirIndex++;
					}
					else
					{
						while(satirIndex<veri.Hareketler.Count&&tabloSatirY+satirYuksekligi<=tabloAltSinir)
						{
							CariHesapRaporSatiri satir = veri.Hareketler[satirIndex];
							Color satirArkaPlan = satirIndex%2==0 ? Color.White : Color.FromArgb(248 , 250 , 252);
							Rectangle satirAlani = new Rectangle(tabloX , tabloSatirY , tabloGenisligi , satirYuksekligi);
							using(SolidBrush satirFirca = new SolidBrush(satirArkaPlan))
								g.FillRectangle(satirFirca , satirAlani);
							g.DrawLine(ayiriciKalem , tabloX , satirAlani.Bottom , tabloX+tabloGenisligi , satirAlani.Bottom);

							x=tabloX;
							for(int i = 0 ; i<basliklar.Length ; i++)
							{
								Rectangle hucre = new Rectangle(x , tabloSatirY , sutunGenislikleri[i] , satirYuksekligi);
								if(i>0)
									g.DrawLine(ayiriciKalem , x , hucre.Y+8 , x , hucre.Bottom-8);

								if(i==0)
								{
									Color kaynakArkaPlan;
									Color kaynakYaziRengi;
									CariHesapKaynakRozetRenkleriGetir(satir.Kaynak , out kaynakArkaPlan , out kaynakYaziRengi);
									RaporRozetiCiz(g , RectangleIcBoslukGetir(hucre , 10 , 0) , BosIseYerineGetir(satir.Kaynak) , textBoldFont , kaynakArkaPlan , kaynakYaziRengi);
								}
								else if(i==1)
								{
									Color islemArkaPlan;
									Color islemYaziRengi;
									CariHesapIslemRozetRenkleriGetir(satir.IslemTuru , out islemArkaPlan , out islemYaziRengi);
									RaporRozetiCiz(g , RectangleIcBoslukGetir(hucre , 10 , 0) , BosIseYerineGetir(satir.IslemTuru) , textBoldFont , islemArkaPlan , islemYaziRengi);
								}
								else
								{
									string hucreMetni;
									Color yaziRengi = Color.FromArgb(15 , 23 , 42);
									Font kullanilanFont = textFont;
									TextFormatFlags hizalama;

									switch(i)
									{
										case 2:
											hucreMetni=BosIseYerineGetir(satir.BelgeNo);
											kullanilanFont=textBoldFont;
											hizalama=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											break;
										case 3:
											hucreMetni=satir.Tarih.HasValue ? satir.Tarih.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) : "-";
											hizalama=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											yaziRengi=Color.FromArgb(51 , 65 , 85);
											break;
										case 4:
											hucreMetni=SatisRaporParaMetniGetir(satir.BorcTutar);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											break;
										case 5:
											hucreMetni=SatisRaporParaMetniGetir(satir.TahsilatTutar);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											yaziRengi=Color.FromArgb(6 , 95 , 70);
											break;
										case 6:
											hucreMetni=SatisRaporParaMetniGetir(satir.KalanTutar);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											yaziRengi=satir.KalanTutar<0m ? Color.FromArgb(22 , 163 , 74) : ( satir.KalanTutar>0m ? Color.FromArgb(234 , 88 , 12) : Color.FromArgb(37 , 99 , 235) );
											break;
										default:
											hucreMetni=BosIseYerineGetir(satir.Not);
											hizalama=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											yaziRengi=Color.FromArgb(71 , 85 , 105);
											break;
									}

									RaporMetinCiz(
										g ,
										hucreMetni ,
										kullanilanFont ,
										RectangleIcBoslukGetir(hucre , 12 , 0) ,
										yaziRengi ,
										hizalama);
								}

								x+=sutunGenislikleri[i];
							}

							tabloSatirY+=satirYuksekligi;
							satirIndex++;
						}
					}

					g.DrawLine(ayiriciKalem , yatayBosluk , sayfaYuksekligi-altBosluk-12 , sayfaGenisligi-yatayBosluk , sayfaYuksekligi-altBosluk-12);
					RaporMetinCiz(
						g ,
						YazdirmaSirketAdres+" | "+YazdirmaSirketTelefon ,
						footerFont ,
						new Rectangle(yatayBosluk , sayfaYuksekligi-altBosluk , 720 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Cari hesap ekstresi" ,
						footerFont ,
						new Rectangle(( sayfaGenisligi/2 )-100 , sayfaYuksekligi-altBosluk , 200 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Sf. "+( sayfaNo+1 ).ToString(CultureInfo.InvariantCulture) ,
						footerFont ,
						new Rectangle(sayfaGenisligi-yatayBosluk-120 , sayfaYuksekligi-altBosluk , 120 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
				}

				sayfaNo++;
			}

			return sayfalar;
		}

		private List<Bitmap> ToptanciBakiyeRaporSayfalariniOlustur ( ToptanciBakiyeRaporVerisi veri )
		{
			List<Bitmap> sayfalar = new List<Bitmap>();
			if(veri==null)
				return sayfalar;

			const int sayfaGenisligi = 1754;
			const int sayfaYuksekligi = 1240;
			const int yatayBosluk = 64;
			const int ustBosluk = 40;
			const int altBosluk = 48;
			const int satirYuksekligi = 42;
			const int tabloBaslikYuksekligi = 48;
			int icerikGenisligi = sayfaGenisligi-( yatayBosluk*2 );
			string[] basliklar = { "Islem Turu" , "Tarih" , "Alinan Urun Tutari" , "Verilen Odeme" , "Kalan Bakiye" , "Not" };
			int cizilecekSatirSayisi = Math.Max(veri.Hareketler.Count , 1);
			int satirIndex = 0;
			int sayfaNo = 0;

			while(satirIndex<cizilecekSatirSayisi)
			{
				Bitmap sayfa = new Bitmap(sayfaGenisligi , sayfaYuksekligi , PixelFormat.Format24bppRgb);
				sayfa.SetResolution(150f , 150f);
				sayfalar.Add(sayfa);

				using(Graphics g = Graphics.FromImage(sayfa))
				using(Font bannerFont = new Font("Segoe UI Semibold" , 23f , FontStyle.Bold))
				using(Font bannerAltFont = new Font("Segoe UI" , 10.2f , FontStyle.Regular))
				using(Font panelBaslikFont = new Font("Segoe UI Semibold" , 10.2f , FontStyle.Bold))
				using(Font panelDegerFont = new Font("Segoe UI Semibold" , 16.5f , FontStyle.Bold))
				using(Font buyukFont = new Font("Segoe UI Semibold" , 16.2f , FontStyle.Bold))
				using(Font textFont = new Font("Segoe UI" , 9.6f , FontStyle.Regular))
				using(Font textBoldFont = new Font("Segoe UI Semibold" , 9.4f , FontStyle.Bold))
				using(Font tableHeaderFont = new Font("Segoe UI Semibold" , 9.4f , FontStyle.Bold))
				using(Font footerFont = new Font("Segoe UI" , 8.6f , FontStyle.Regular))
				using(Pen kenarlikKalemi = new Pen(Color.FromArgb(226 , 232 , 240)))
				using(Pen ayiriciKalem = new Pen(Color.FromArgb(226 , 232 , 240)))
				{
					g.Clear(Color.FromArgb(244 , 247 , 251));
					g.SmoothingMode=SmoothingMode.AntiAlias;
					g.InterpolationMode=InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode=PixelOffsetMode.HighQuality;
					g.CompositingQuality=CompositingQuality.HighQuality;
					g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;

					int y = ustBosluk;

					Rectangle banner = new Rectangle(yatayBosluk , y , icerikGenisligi , 116);
					Rectangle bannerGolge = new Rectangle(banner.X , banner.Y+8 , banner.Width , banner.Height);
					using(GraphicsPath bannerGolgeYolu = YuvarlatilmisDikdortgenOlustur(bannerGolge , 28))
					using(SolidBrush bannerGolgeFirca = new SolidBrush(Color.FromArgb(18 , 15 , 23 , 42)))
						g.FillPath(bannerGolgeFirca , bannerGolgeYolu);
					using(GraphicsPath bannerYolu = YuvarlatilmisDikdortgenOlustur(banner , 28))
					using(LinearGradientBrush bannerFirca = new LinearGradientBrush(banner , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(8 , 145 , 178) , LinearGradientMode.Horizontal))
					using(Pen bannerKalemi = new Pen(Color.FromArgb(30 , 41 , 59)))
					{
						g.FillPath(bannerFirca , bannerYolu);
						g.DrawPath(bannerKalemi , bannerYolu);

						GraphicsState bannerDurumu = g.Save();
						g.SetClip(bannerYolu);
						using(SolidBrush vurguFirca = new SolidBrush(Color.FromArgb(45 , 212 , 191)))
							g.FillRectangle(vurguFirca , new Rectangle(banner.X , banner.Bottom-6 , banner.Width , 6));
						g.Restore(bannerDurumu);
					}

					Rectangle logoAlani = new Rectangle(banner.X+24 , banner.Y+22 , 82 , 62);
					YazdirmaLogoCiz(g , logoAlani);
					Rectangle sagBilgiKutusu = new Rectangle(banner.Right-366 , banner.Y+20 , 340 , 74);
					int bannerMetinX = logoAlani.Right+18;
					int bannerMetinGenisligi = Math.Max(340 , sagBilgiKutusu.X-bannerMetinX-28);

					RaporMetinCiz(
						g ,
						"TOPTANCI BAKIYE RAPORU" ,
						bannerFont ,
						new Rectangle(bannerMetinX , banner.Y+20 , bannerMetinGenisligi , 38) ,
						Color.White ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Alimlar, odemeler ve kalan bakiye hareketleri tek listede sunulur." ,
						bannerAltFont ,
						new Rectangle(bannerMetinX , banner.Y+58 , bannerMetinGenisligi , 24) ,
						Color.FromArgb(204 , 251 , 241) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					RaporYuvarlatilmisKutuCiz(
						g ,
						sagBilgiKutusu ,
						Color.FromArgb(34 , 255 , 255 , 255) ,
						Color.FromArgb(58 , 148 , 163 , 184) ,
						18 ,
						0 ,
						Color.Transparent);
					RaporMetinCiz(
						g ,
						YazdirmaSirketAdi ,
						panelBaslikFont ,
						new Rectangle(sagBilgiKutusu.X+18 , sagBilgiKutusu.Y+14 , sagBilgiKutusu.Width-36 , 20) ,
						Color.White ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Olusturulma: "+veri.RaporTarihi.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) ,
						textFont ,
						new Rectangle(sagBilgiKutusu.X+18 , sagBilgiKutusu.Y+36 , 224 , 20) ,
						Color.FromArgb(204 , 251 , 241) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Sayfa "+( sayfaNo+1 ).ToString(CultureInfo.InvariantCulture) ,
						textBoldFont ,
						new Rectangle(sagBilgiKutusu.Right-110 , sagBilgiKutusu.Y+36 , 92 , 20) ,
						Color.FromArgb(240 , 253 , 250) ,
						TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					y=banner.Bottom+22;

					Rectangle bilgiKutusu = new Rectangle(yatayBosluk , y , icerikGenisligi , 118);
					RaporYuvarlatilmisKutuCiz(
						g ,
						bilgiKutusu ,
						Color.White ,
						Color.FromArgb(226 , 232 , 240) ,
						22 ,
						5 ,
						Color.FromArgb(14 , 15 , 23 , 42));

					Rectangle ozetAlani = new Rectangle(bilgiKutusu.X+24 , bilgiKutusu.Y+20 , 520 , bilgiKutusu.Height-40);
					RaporMetinCiz(
						g ,
						"Toptanci Bilgileri" ,
						panelBaslikFont ,
						new Rectangle(ozetAlani.X , ozetAlani.Y , 180 , 20) ,
						Color.FromArgb(71 , 85 , 105) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						BosIseYerineGetir(veri.ToptanciAdi) ,
						buyukFont ,
						new Rectangle(ozetAlani.X , ozetAlani.Y+24 , ozetAlani.Width , 30) ,
						Color.FromArgb(15 , 23 , 42) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Toptanci ID: "+veri.ToptanciId.ToString("N0" , _yazdirmaKulturu) ,
						textFont ,
						new Rectangle(ozetAlani.X , ozetAlani.Y+58 , 180 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Secili toptanci icin alim ve odeme ekstresi." ,
						textFont ,
						new Rectangle(ozetAlani.X+146 , ozetAlani.Y+58 , ozetAlani.Width-146 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					int miniKartX = ozetAlani.Right+18;
					int miniKartBosluk = 12;
					int miniKartGenisligi = ( bilgiKutusu.Right-miniKartX-24-( miniKartBosluk*2 ) )/3;
					Rectangle[] bilgiKartlari =
					{
						new Rectangle(miniKartX , bilgiKutusu.Y+20 , miniKartGenisligi , 78),
						new Rectangle(miniKartX+miniKartGenisligi+miniKartBosluk , bilgiKutusu.Y+20 , miniKartGenisligi , 78),
						new Rectangle(miniKartX+( miniKartGenisligi+miniKartBosluk )*2 , bilgiKutusu.Y+20 , miniKartGenisligi , 78)
					};
					string[] bilgiBasliklari =
					{
						"Telefon",
						"Son Hareket",
						"Hareket"
					};
					string[] bilgiDegerleri =
					{
						BosIseYerineGetir(veri.Telefon),
						veri.SonHareketTarihi.HasValue ? veri.SonHareketTarihi.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) : "-",
						veri.Hareketler.Count.ToString("N0" , _yazdirmaKulturu)+" kayit"
					};
					Color[] bilgiKartArkaPlanlari =
					{
						Color.FromArgb(239 , 246 , 255),
						Color.FromArgb(240 , 253 , 250),
						Color.FromArgb(255 , 247 , 237)
					};
					Color[] bilgiKartVurgulari =
					{
						Color.FromArgb(37 , 99 , 235),
						Color.FromArgb(13 , 148 , 136),
						Color.FromArgb(234 , 88 , 12)
					};

					for(int i = 0 ; i<bilgiKartlari.Length ; i++)
					{
						RaporYuvarlatilmisKutuCiz(
							g ,
							bilgiKartlari[i] ,
							bilgiKartArkaPlanlari[i] ,
							Color.FromArgb(219 , 234 , 254) ,
							16 ,
							0 ,
							Color.Transparent);
						RaporUstSeritCiz(g , bilgiKartlari[i] , 16 , bilgiKartVurgulari[i] , 5);
						RaporMetinCiz(
							g ,
							bilgiBasliklari[i] ,
							textBoldFont ,
							new Rectangle(bilgiKartlari[i].X+16 , bilgiKartlari[i].Y+16 , bilgiKartlari[i].Width-32 , 18) ,
							Color.FromArgb(71 , 85 , 105) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						RaporMetinCiz(
							g ,
							bilgiDegerleri[i] ,
							textBoldFont ,
							new Rectangle(bilgiKartlari[i].X+16 , bilgiKartlari[i].Y+38 , bilgiKartlari[i].Width-32 , 22) ,
							Color.FromArgb(15 , 23 , 42) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					}

					y=bilgiKutusu.Bottom+20;

					Rectangle[] kartlar =
					{
						new Rectangle(yatayBosluk , y , 380 , 98),
						new Rectangle(yatayBosluk+398 , y , 380 , 98),
						new Rectangle(yatayBosluk+796 , y , 380 , 98),
						new Rectangle(yatayBosluk+1194 , y , 432 , 98)
					};
					string[] kartBasliklari =
					{
						"TOPLAM ALIM",
						"TOPLAM ODEME",
						"KALAN BAKIYE",
						"DURUM"
					};
					string[] kartDegerleri =
					{
						SatisRaporParaMetniGetir(veri.ToplamAlim),
						SatisRaporParaMetniGetir(veri.ToplamOdeme),
						SatisRaporParaMetniGetir(veri.KalanBakiye),
						ToptanciBakiyeDurumMetniGetir(veri.KalanBakiye)
					};
					Color[] kartArkaPlanlari =
					{
						Color.FromArgb(239 , 246 , 255),
						Color.FromArgb(236 , 253 , 245),
						Color.FromArgb(255 , 247 , 237),
						Color.FromArgb(248 , 250 , 252)
					};
					Color[] kartRenkleri =
					{
						Color.FromArgb(37 , 99 , 235),
						Color.FromArgb(5 , 150 , 105),
						Color.FromArgb(234 , 88 , 12),
						veri.KalanBakiye<0m ? Color.FromArgb(22 , 163 , 74) : ( veri.KalanBakiye>0m ? Color.FromArgb(220 , 38 , 38) : Color.FromArgb(37 , 99 , 235) )
					};

					for(int i = 0 ; i<kartlar.Length ; i++)
					{
						RaporYuvarlatilmisKutuCiz(
							g ,
							kartlar[i] ,
							kartArkaPlanlari[i] ,
							Color.FromArgb(219 , 234 , 254) ,
							18 ,
							0 ,
							Color.Transparent);
						RaporUstSeritCiz(g , kartlar[i] , 18 , kartRenkleri[i] , 6);
						RaporMetinCiz(
							g ,
							kartBasliklari[i] ,
							panelBaslikFont ,
							new Rectangle(kartlar[i].X+16 , kartlar[i].Y+18 , kartlar[i].Width-32 , 18) ,
							Color.FromArgb(71 , 85 , 105) ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						RaporMetinCiz(
							g ,
							kartDegerleri[i] ,
							panelDegerFont ,
							new Rectangle(kartlar[i].X+16 , kartlar[i].Y+44 , kartlar[i].Width-32 , 28) ,
							kartRenkleri[i] ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					}

					y=kartlar[0].Bottom+20;

					Rectangle tabloPaneli = new Rectangle(yatayBosluk , y , icerikGenisligi , sayfaYuksekligi-altBosluk-y-34);
					RaporYuvarlatilmisKutuCiz(
						g ,
						tabloPaneli ,
						Color.White ,
						Color.FromArgb(226 , 232 , 240) ,
						22 ,
						5 ,
						Color.FromArgb(12 , 15 , 23 , 42));

					Rectangle tabloAlan = RectangleIcBoslukGetir(tabloPaneli , 22 , 20);
					RaporMetinCiz(
						g ,
						"Hareketler" ,
						buyukFont ,
						new Rectangle(tabloAlan.X , tabloAlan.Y , 220 , 26) ,
						Color.FromArgb(15 , 23 , 42) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Secili toptanciya ait alim ve odeme hareketleri listelenir." ,
						textFont ,
						new Rectangle(tabloAlan.X , tabloAlan.Y+28 , 560 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporRozetiCiz(
						g ,
						new Rectangle(tabloAlan.Right-164 , tabloAlan.Y+2 , 164 , 28) ,
						veri.Hareketler.Count.ToString("N0" , _yazdirmaKulturu)+" kayit" ,
						textBoldFont ,
						Color.FromArgb(236 , 253 , 245) ,
						Color.FromArgb(15 , 118 , 110));

					int tabloX = tabloAlan.X;
					int tabloY = tabloAlan.Y+60;
					int tabloGenisligi = tabloAlan.Width;
					int notSutunGenisligi = Math.Max(300 , tabloGenisligi-1030);
					int[] sutunGenislikleri = { 210 , 186 , 180 , 174 , 184 , notSutunGenisligi };
					int kullanilanGenislik = 0;
					for(int i = 0 ; i<sutunGenislikleri.Length ; i++)
						kullanilanGenislik+=sutunGenislikleri[i];
					sutunGenislikleri[sutunGenislikleri.Length-1]+=tabloGenisligi-kullanilanGenislik;

					int x = tabloX;
					for(int i = 0 ; i<basliklar.Length ; i++)
					{
						Rectangle hucre = new Rectangle(x , tabloY , sutunGenislikleri[i] , tabloBaslikYuksekligi);
						using(SolidBrush baslikFirca = new SolidBrush(Color.FromArgb(15 , 118 , 110)))
							g.FillRectangle(baslikFirca , hucre);
						if(i>0)
							g.DrawLine(kenarlikKalemi , x , hucre.Y+10 , x , hucre.Bottom-10);
						RaporMetinCiz(
							g ,
							basliklar[i] ,
							tableHeaderFont ,
							RectangleIcBoslukGetir(hucre , 12 , 0) ,
							Color.White ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						x+=sutunGenislikleri[i];
					}

					int tabloSatirY = tabloY+tabloBaslikYuksekligi;
					int tabloAltSinir = tabloPaneli.Bottom-26;

					if(veri.Hareketler.Count==0)
					{
						Rectangle bosAlan = new Rectangle(tabloX , tabloSatirY , tabloGenisligi , Math.Max(78 , tabloAltSinir-tabloSatirY));
						using(SolidBrush bosFirca = new SolidBrush(Color.FromArgb(248 , 250 , 252)))
							g.FillRectangle(bosFirca , bosAlan);
						g.DrawRectangle(ayiriciKalem , bosAlan);
						RaporMetinCiz(
							g ,
							"Secili toptanci icin gorunur hareket kaydi bulunamadi." ,
							textFont ,
							bosAlan ,
							Color.FromArgb(100 , 116 , 139) ,
							TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						satirIndex++;
					}
					else
					{
						while(satirIndex<veri.Hareketler.Count&&tabloSatirY+satirYuksekligi<=tabloAltSinir)
						{
							ToptanciBakiyeRaporSatiri satir = veri.Hareketler[satirIndex];
							Color satirArkaPlan = satirIndex%2==0 ? Color.White : Color.FromArgb(248 , 250 , 252);
							Rectangle satirAlani = new Rectangle(tabloX , tabloSatirY , tabloGenisligi , satirYuksekligi);
							using(SolidBrush satirFirca = new SolidBrush(satirArkaPlan))
								g.FillRectangle(satirFirca , satirAlani);
							g.DrawLine(ayiriciKalem , tabloX , satirAlani.Bottom , tabloX+tabloGenisligi , satirAlani.Bottom);

							x=tabloX;
							for(int i = 0 ; i<basliklar.Length ; i++)
							{
								Rectangle hucre = new Rectangle(x , tabloSatirY , sutunGenislikleri[i] , satirYuksekligi);
								if(i>0)
									g.DrawLine(ayiriciKalem , x , hucre.Y+8 , x , hucre.Bottom-8);

								if(i==0)
								{
									Color islemArkaPlan;
									Color islemYaziRengi;
									ToptanciBakiyeIslemRozetRenkleriGetir(satir.IslemTuru , out islemArkaPlan , out islemYaziRengi);
									RaporRozetiCiz(g , RectangleIcBoslukGetir(hucre , 10 , 0) , BosIseYerineGetir(satir.IslemTuru) , textBoldFont , islemArkaPlan , islemYaziRengi);
								}
								else
								{
									string hucreMetni;
									Color yaziRengi = Color.FromArgb(15 , 23 , 42);
									Font kullanilanFont = textFont;
									TextFormatFlags hizalama;

									switch(i)
									{
										case 1:
											hucreMetni=satir.Tarih.HasValue ? satir.Tarih.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) : "-";
											hizalama=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											yaziRengi=Color.FromArgb(51 , 65 , 85);
											break;
										case 2:
											hucreMetni=SatisRaporParaMetniGetir(satir.BorcTutar);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											yaziRengi=Color.FromArgb(37 , 99 , 235);
											break;
										case 3:
											hucreMetni=SatisRaporParaMetniGetir(satir.OdemeTutar);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											yaziRengi=Color.FromArgb(5 , 150 , 105);
											break;
										case 4:
											hucreMetni=SatisRaporParaMetniGetir(satir.KalanBakiye);
											hizalama=TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											kullanilanFont=textBoldFont;
											yaziRengi=satir.KalanBakiye<0m ? Color.FromArgb(22 , 163 , 74) : ( satir.KalanBakiye>0m ? Color.FromArgb(234 , 88 , 12) : Color.FromArgb(37 , 99 , 235) );
											break;
										default:
											hucreMetni=BosIseYerineGetir(satir.Not);
											hizalama=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
											yaziRengi=Color.FromArgb(71 , 85 , 105);
											break;
									}

									RaporMetinCiz(
										g ,
										hucreMetni ,
										kullanilanFont ,
										RectangleIcBoslukGetir(hucre , 12 , 0) ,
										yaziRengi ,
										hizalama);
								}

								x+=sutunGenislikleri[i];
							}

							tabloSatirY+=satirYuksekligi;
							satirIndex++;
						}
					}

					g.DrawLine(ayiriciKalem , yatayBosluk , sayfaYuksekligi-altBosluk-12 , sayfaGenisligi-yatayBosluk , sayfaYuksekligi-altBosluk-12);
					RaporMetinCiz(
						g ,
						YazdirmaSirketAdres+" | "+YazdirmaSirketTelefon ,
						footerFont ,
						new Rectangle(yatayBosluk , sayfaYuksekligi-altBosluk , 720 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Toptanci bakiye ekstresi" ,
						footerFont ,
						new Rectangle(( sayfaGenisligi/2 )-110 , sayfaYuksekligi-altBosluk , 220 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Sf. "+( sayfaNo+1 ).ToString(CultureInfo.InvariantCulture) ,
						footerFont ,
						new Rectangle(sayfaGenisligi-yatayBosluk-120 , sayfaYuksekligi-altBosluk , 120 , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
				}

				sayfaNo++;
			}

			return sayfalar;
		}

		private void RaporYuvarlatilmisKutuCiz ( Graphics g , Rectangle alan , Color arkaPlanRengi , Color kenarlikRengi , int yaricap , int golgeOfseti , Color golgeRengi )
		{
			if(g==null||alan.Width<=0||alan.Height<=0)
				return;

			if(golgeOfseti>0&&golgeRengi.A>0)
			{
				Rectangle golgeAlani = new Rectangle(alan.X , alan.Y+golgeOfseti , alan.Width , alan.Height);
				using(GraphicsPath golgeYolu = YuvarlatilmisDikdortgenOlustur(golgeAlani , yaricap))
				using(SolidBrush golgeFirca = new SolidBrush(golgeRengi))
					g.FillPath(golgeFirca , golgeYolu);
			}

			using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(alan , yaricap))
			using(SolidBrush arkaPlanFirca = new SolidBrush(arkaPlanRengi))
			using(Pen kenarlikKalemi = new Pen(kenarlikRengi))
			{
				g.FillPath(arkaPlanFirca , yol);
				g.DrawPath(kenarlikKalemi , yol);
			}
		}

		private void RaporMetinCiz ( Graphics g , string metin , Font font , Rectangle alan , Color yaziRengi , TextFormatFlags flags )
		{
			if(g==null||font==null||alan.Width<=0||alan.Height<=0)
				return;

			using(StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
			using(SolidBrush firca = new SolidBrush(yaziRengi))
			{
				format.FormatFlags|=StringFormatFlags.NoWrap;
				format.Trimming=( flags&TextFormatFlags.EndEllipsis )==TextFormatFlags.EndEllipsis
					? StringTrimming.EllipsisCharacter
					: StringTrimming.None;

				if(( flags&TextFormatFlags.Right )==TextFormatFlags.Right)
					format.Alignment=StringAlignment.Far;
				else if(( flags&TextFormatFlags.HorizontalCenter )==TextFormatFlags.HorizontalCenter)
					format.Alignment=StringAlignment.Center;
				else
					format.Alignment=StringAlignment.Near;

				if(( flags&TextFormatFlags.Bottom )==TextFormatFlags.Bottom)
					format.LineAlignment=StringAlignment.Far;
				else if(( flags&TextFormatFlags.VerticalCenter )==TextFormatFlags.VerticalCenter)
					format.LineAlignment=StringAlignment.Center;
				else
					format.LineAlignment=StringAlignment.Near;

				g.DrawString(
					string.IsNullOrEmpty(metin) ? string.Empty : metin ,
					font ,
					firca ,
					new RectangleF(alan.X-1f , alan.Y-1f , alan.Width+3f , alan.Height+3f) ,
					format);
			}
		}

		private void RaporUstSeritCiz ( Graphics g , Rectangle alan , int yaricap , Color seritRengi , int yukseklik )
		{
			if(g==null||yukseklik<=0||alan.Width<=0||alan.Height<=0)
				return;

			using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(alan , yaricap))
			using(SolidBrush seritFirca = new SolidBrush(seritRengi))
			{
				GraphicsState durum = g.Save();
				g.SetClip(yol);
				g.FillRectangle(seritFirca , new Rectangle(alan.X , alan.Y , alan.Width , yukseklik));
				g.Restore(durum);
			}
		}

		private void RaporRozetiCiz ( Graphics g , Rectangle alan , string metin , Font font , Color arkaPlanRengi , Color yaziRengi )
		{
			if(g==null||alan.Width<=0||alan.Height<=0)
				return;

			string rozetMetni = BosIseYerineGetir(metin);
			Size olcu = TextRenderer.MeasureText(
				g ,
				rozetMetni ,
				font ,
				new Size(int.MaxValue , int.MaxValue) ,
				TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);

			int rozetGenisligi = Math.Min(alan.Width , Math.Max(76 , olcu.Width+28));
			int rozetYuksekligi = Math.Min(28 , Math.Max(22 , alan.Height-10));
			Rectangle rozet = new Rectangle(
				alan.X ,
				alan.Y+Math.Max(0 , ( alan.Height-rozetYuksekligi )/2) ,
				rozetGenisligi ,
				rozetYuksekligi);

			using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(rozet , rozetYuksekligi/2))
			using(SolidBrush arkaPlanFirca = new SolidBrush(arkaPlanRengi))
				g.FillPath(arkaPlanFirca , yol);

			RaporMetinCiz(
				g ,
				rozetMetni ,
				font ,
				rozet ,
				yaziRengi ,
				TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
		}

		private void CariHesapKaynakRozetRenkleriGetir ( string kaynak , out Color arkaPlanRengi , out Color yaziRengi )
		{
			string metin = KarsilastirmaMetniHazirla(kaynak);
			if(metin.Contains("OTOMATIK"))
			{
				arkaPlanRengi=Color.FromArgb(219 , 234 , 254);
				yaziRengi=Color.FromArgb(30 , 64 , 175);
				return;
			}

			if(metin.Contains("MANUEL"))
			{
				arkaPlanRengi=Color.FromArgb(254 , 226 , 226);
				yaziRengi=Color.FromArgb(153 , 27 , 27);
				return;
			}

			arkaPlanRengi=Color.FromArgb(226 , 232 , 240);
			yaziRengi=Color.FromArgb(51 , 65 , 85);
		}

		private void CariHesapIslemRozetRenkleriGetir ( string islemTuru , out Color arkaPlanRengi , out Color yaziRengi )
		{
			string metin = KarsilastirmaMetniHazirla(islemTuru);
			if(metin.Contains("TAHSILAT"))
			{
				arkaPlanRengi=Color.FromArgb(220 , 252 , 231);
				yaziRengi=Color.FromArgb(21 , 128 , 61);
				return;
			}

			if(metin.Contains("FATURA"))
			{
				arkaPlanRengi=Color.FromArgb(255 , 237 , 213);
				yaziRengi=Color.FromArgb(194 , 65 , 12);
				return;
			}

			arkaPlanRengi=Color.FromArgb(240 , 253 , 250);
			yaziRengi=Color.FromArgb(15 , 118 , 110);
		}

		private string CariHesapDurumMetniGetir ( decimal kalanTutar )
		{
			if(kalanTutar>0m)
				return "BORC DEVAM EDIYOR";
			if(kalanTutar<0m)
				return "ALACAKLI";

			return "HESAP KAPANDI";
		}

		private void ToptanciBakiyeIslemRozetRenkleriGetir ( string islemTuru , out Color arkaPlanRengi , out Color yaziRengi )
		{
			string metin = KarsilastirmaMetniHazirla(islemTuru);
			if(metin.Contains("ALINAN"))
			{
				arkaPlanRengi=Color.FromArgb(219 , 234 , 254);
				yaziRengi=Color.FromArgb(30 , 64 , 175);
				return;
			}

			if(metin.Contains("ODEME"))
			{
				arkaPlanRengi=Color.FromArgb(220 , 252 , 231);
				yaziRengi=Color.FromArgb(21 , 128 , 61);
				return;
			}

			arkaPlanRengi=Color.FromArgb(226 , 232 , 240);
			yaziRengi=Color.FromArgb(51 , 65 , 85);
		}

		private string ToptanciBakiyeDurumMetniGetir ( decimal kalanBakiye )
		{
			if(kalanBakiye>0m)
				return "BORC VAR";
			if(kalanBakiye<0m)
				return "ALACAKLI";

			return "DENGEDE";
		}

		private string BelgeKaydetmeYoluSec ( BelgeYazdirmaVerisi veri , string uzanti , string filtre )
		{
			using(SaveFileDialog kaydet = new SaveFileDialog())
			{
				kaydet.Title=BelgeKaydetmeBasligiGetir(veri , uzanti);
				kaydet.Filter=filtre;
				kaydet.DefaultExt=uzanti;
				kaydet.AddExtension=true;
				kaydet.OverwritePrompt=true;
				kaydet.RestoreDirectory=true;
				kaydet.InitialDirectory=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				kaydet.FileName=BelgeVarsayilanDosyaAdiGetir(veri , uzanti);
				return kaydet.ShowDialog(this)==DialogResult.OK ? kaydet.FileName : null;
			}
		}

		private string BelgeVarsayilanDosyaAdiGetir ( BelgeYazdirmaVerisi veri , string uzanti )
		{
			List<string> parcaliAd = new List<string>();
			string belgeKimlik = DosyaAdiParcasiTemizle(BelgeKaydetmeKimligiGetir(veri));
			string cariAdi = DosyaAdiParcasiTemizle(veri?.CariAdi);
			string tarihMetni = DateTime.Now.ToString("yyyyMMdd_HHmmss" , CultureInfo.InvariantCulture);

			if(!string.IsNullOrWhiteSpace(belgeKimlik))
				parcaliAd.Add(belgeKimlik);
			parcaliAd.Add(tarihMetni);
			if(!string.IsNullOrWhiteSpace(cariAdi))
				parcaliAd.Add(cariAdi);

			string dosyaAdi = string.Join("_" , parcaliAd.Where(x => !string.IsNullOrWhiteSpace(x)));
			if(string.IsNullOrWhiteSpace(dosyaAdi))
				dosyaAdi="Belge_"+tarihMetni;

			return dosyaAdi+"."+uzanti.TrimStart('.');
		}

		private string BelgeKaydetmeBasligiGetir ( BelgeYazdirmaVerisi veri , string uzanti )
		{
			string belgeKimlik = BelgeKaydetmeKimligiGetir(veri);
			string belgeTuru = string.Equals(uzanti , "pdf" , StringComparison.OrdinalIgnoreCase)
				? "PDF"
				: "Excel";
			return belgeKimlik+" "+belgeTuru+" Kaydet";
		}

		private string BelgeKaydetmeKimligiGetir ( BelgeYazdirmaVerisi veri )
		{
			string belgeTuru = BelgeKaydetmeTurEtiketiGetir(veri?.BelgeBasligi);
			if(veri==null)
				return belgeTuru;

			if(veri.BelgeSiraNo.HasValue)
				return belgeTuru+" "+veri.BelgeSiraNo.Value.ToString("N0" , _yazdirmaKulturu);

			if(!string.IsNullOrWhiteSpace(veri.BelgeNo)&&!string.Equals(veri.BelgeNo , "SEPET" , StringComparison.OrdinalIgnoreCase))
				return belgeTuru+" "+veri.BelgeNo.Trim();

			return belgeTuru;
		}

		private string BelgeKaydetmeTurEtiketiGetir ( string belgeBasligi )
		{
			string karsilastirmaMetni = KarsilastirmaMetniHazirla(belgeBasligi);
			if(karsilastirmaMetni.Contains("TEKLIF"))
				return "TKL";
			if(karsilastirmaMetni.Contains("FATURA"))
				return "FTR";

			return BelgeTurEtiketiGetir(belgeBasligi);
		}

		private string BelgeKimlikMetniGetir ( BelgeYazdirmaVerisi veri )
		{
			string belgeTuru = BelgeTurEtiketiGetir(veri?.BelgeBasligi);
			if(veri==null)
				return belgeTuru;

			if(veri.BelgeSiraNo.HasValue)
				return belgeTuru+" #"+veri.BelgeSiraNo.Value.ToString("N0" , _yazdirmaKulturu);

			if(!string.IsNullOrWhiteSpace(veri.BelgeNo)&&!string.Equals(veri.BelgeNo , "SEPET" , StringComparison.OrdinalIgnoreCase))
				return belgeTuru+" "+veri.BelgeNo.Trim();

			return belgeTuru;
		}

		private string BelgeTurEtiketiGetir ( string belgeBasligi )
		{
			string karsilastirmaMetni = KarsilastirmaMetniHazirla(belgeBasligi);
			if(karsilastirmaMetni.Contains("TEKLIF"))
				return "Teklif";
			if(karsilastirmaMetni.Contains("FATURA"))
				return "Fatura";

			return string.IsNullOrWhiteSpace(belgeBasligi)
				? "Belge"
				: belgeBasligi.Trim();
		}

		private string BelgeExcelBaslikMetniGetir ( BelgeYazdirmaVerisi veri )
		{
			string baslik = BosIseYerineGetir(veri?.BelgeBasligi);
			if(string.IsNullOrWhiteSpace(baslik)||baslik=="-")
				baslik=BelgeTurEtiketiGetir(veri?.BelgeBasligi);

			return baslik.ToUpper(_yazdirmaKulturu);
		}

		private string DosyaAdiParcasiTemizle ( string metin )
		{
			string sonuc = ( metin??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(sonuc)||sonuc=="-")
				return string.Empty;

			foreach(char gecersizKarakter in Path.GetInvalidFileNameChars())
				sonuc=sonuc.Replace(gecersizKarakter.ToString() , string.Empty);

			sonuc=Regex.Replace(sonuc , @"\s+" , " ").Trim();
			return sonuc.Replace(" " , "_");
		}

		private string RaporKaydetmeYoluSec ( string baslik , string uzanti , string filtre , string varsayilanOnEk )
		{
			using(SaveFileDialog kaydet = new SaveFileDialog())
			{
				string tarihMetni = DateTime.Now.ToString("yyyyMMdd_HHmmss" , CultureInfo.InvariantCulture);
				string onEk = DosyaAdiParcasiTemizle(varsayilanOnEk);
				if(string.IsNullOrWhiteSpace(onEk))
					onEk="Rapor";

				kaydet.Title=baslik;
				kaydet.Filter=filtre;
				kaydet.DefaultExt=uzanti;
				kaydet.AddExtension=true;
				kaydet.OverwritePrompt=true;
				kaydet.RestoreDirectory=true;
				kaydet.InitialDirectory=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				kaydet.FileName=onEk+"_"+tarihMetni+"."+uzanti.TrimStart('.');
				return kaydet.ShowDialog(this)==DialogResult.OK ? kaydet.FileName : null;
			}
		}

		private bool GenelToplamRaporundaVeriVarMi ()
		{
			return _genelToplamGrid!=null
				&&GenelToplamGorunurKolonlariniGetir().Count>0
				&&GenelToplamGorunurSatirlariniGetir().Count>0;
		}

		private List<DataGridViewColumn> GenelToplamGorunurKolonlariniGetir ()
		{
			List<DataGridViewColumn> kolonlar = new List<DataGridViewColumn>();
			if(_genelToplamGrid==null)
				return kolonlar;

			foreach(DataGridViewColumn kolon in _genelToplamGrid.Columns)
			{
				if(kolon!=null&&kolon.Visible)
					kolonlar.Add(kolon);
			}

			return kolonlar
				.OrderBy(k => k.DisplayIndex)
				.ToList();
		}

		private List<DataGridViewRow> GenelToplamGorunurSatirlariniGetir ()
		{
			List<DataGridViewRow> satirlar = new List<DataGridViewRow>();
			if(_genelToplamGrid==null)
				return satirlar;

			foreach(DataGridViewRow satir in _genelToplamGrid.Rows)
			{
				if(satir!=null&&!satir.IsNewRow&&satir.Visible)
					satirlar.Add(satir);
			}

			return satirlar;
		}

		private decimal GenelToplamSayisalDegerGetir ( object deger )
		{
			try
			{
				return deger==null||deger==DBNull.Value
					? 0m
					: Convert.ToDecimal(deger , CultureInfo.InvariantCulture);
			}
			catch
			{
				decimal sonuc;
				return decimal.TryParse(Convert.ToString(deger) , NumberStyles.Any , _yazdirmaKulturu , out sonuc)
					? sonuc
					: 0m;
			}
		}

		private Rectangle RectangleIcBoslukGetir ( Rectangle alan , int yatayBosluk , int dikeyBosluk )
		{
			return new Rectangle(
				alan.X+yatayBosluk ,
				alan.Y+dikeyBosluk ,
				Math.Max(1 , alan.Width-( yatayBosluk*2 )) ,
				Math.Max(1 , alan.Height-( dikeyBosluk*2 )));
		}

		private bool GenelToplamKolonSayisalMi ( DataGridViewColumn kolon )
		{
			if(kolon==null)
				return false;

			if(kolon.ValueType==typeof(decimal)
				||kolon.ValueType==typeof(double)
				||kolon.ValueType==typeof(float)
				||kolon.ValueType==typeof(int)
				||kolon.ValueType==typeof(long))
				return true;

			switch(kolon.Name)
			{
				case "Ciro":
				case "ToplamMaliyet":
				case "KarTutari":
				case "ToplamAlim":
				case "ToplamOdeme":
				case "KalanBakiye":
					return true;
				default:
					return false;
			}
		}

		private bool GenelToplamSatiriVurguluMu ( DataGridViewRow satir )
		{
			if(satir==null)
				return false;

			string detay = KarsilastirmaMetniHazirla(Convert.ToString(satir.Cells["AdSoyad"]?.Value));
			return detay.Contains("TUM SATISLAR")
				||detay.Contains("TOPTANCI GENEL TOPLAMI");
		}

		private string GenelToplamHucreMetniGetir ( DataGridViewColumn kolon , object deger )
		{
			if(GenelToplamKolonSayisalMi(kolon))
				return SatisRaporParaMetniGetir(GenelToplamSayisalDegerGetir(deger));

			return BosIseYerineGetir(Convert.ToString(deger));
		}

		private void BitmapSayfalariniYazdirmaOnizlemeAc ( List<Bitmap> sayfalar , string belgeAdi )
		{
			if(sayfalar==null||sayfalar.Count==0)
				return;

			int sayfaIndex = 0;
			using(PrintDocument belge = new PrintDocument())
			using(PrintPreviewDialog onizleme = new PrintPreviewDialog())
			{
				belge.DocumentName=string.IsNullOrWhiteSpace(belgeAdi) ? "Rapor" : belgeAdi;
				belge.DefaultPageSettings.Margins=new Margins(18 , 18 , 18 , 18);
				belge.DefaultPageSettings.Landscape=sayfalar[0].Width>sayfalar[0].Height;
				belge.BeginPrint+=( s , e ) => sayfaIndex=0;
				belge.PrintPage+=( s , e ) =>
				{
					if(sayfaIndex>=sayfalar.Count)
					{
						e.HasMorePages=false;
						return;
					}

					Bitmap sayfa = sayfalar[sayfaIndex];
					Rectangle hedefAlan = e.MarginBounds;
					float oran = Math.Min(
						hedefAlan.Width/( float )sayfa.Width ,
						hedefAlan.Height/( float )sayfa.Height);
					Size cizimBoyutu = new Size(
						Math.Max(1 , ( int )( sayfa.Width*oran )) ,
						Math.Max(1 , ( int )( sayfa.Height*oran )));
					Rectangle cizimAlani = new Rectangle(
						hedefAlan.X+( hedefAlan.Width-cizimBoyutu.Width )/2 ,
						hedefAlan.Y+( hedefAlan.Height-cizimBoyutu.Height )/2 ,
						cizimBoyutu.Width ,
						cizimBoyutu.Height);

					e.Graphics.Clear(Color.White);
					e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
					e.Graphics.SmoothingMode=SmoothingMode.HighQuality;
					e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
					e.Graphics.CompositingQuality=CompositingQuality.HighQuality;
					e.Graphics.DrawImage(sayfa , cizimAlani);

					sayfaIndex++;
					e.HasMorePages=sayfaIndex<sayfalar.Count;
				};

				onizleme.Document=belge;
				onizleme.Width=1200;
				onizleme.Height=850;
				onizleme.WindowState=FormWindowState.Maximized;
				onizleme.ShowIcon=false;
				onizleme.UseAntiAlias=false;
				onizleme.Text=belge.DocumentName+" - Yazdirma Onizleme";
				onizleme.ShowDialog(this);
			}
		}

		private List<Bitmap> GenelToplamRaporSayfalariniOlustur ()
		{
			List<Bitmap> sayfalar = new List<Bitmap>();
			List<DataGridViewColumn> kolonlar = GenelToplamGorunurKolonlariniGetir();
			List<DataGridViewRow> satirlar = GenelToplamGorunurSatirlariniGetir();
			if(kolonlar.Count==0||satirlar.Count==0)
				return sayfalar;

			const int sayfaGenisligi = 1240;
			const int sayfaYuksekligi = 1754;
			const int yatayKenarBosluk = 70;
			const int ustBosluk = 60;
			const int altBosluk = 70;
			const int ilkSayfaUstAlanYuksekligi = 250;
			const int devamSayfasiUstAlanYuksekligi = 120;
			const int tabloBaslikYuksekligi = 42;
			const int satirYuksekligi = 34;

			int tabloGenisligi = sayfaGenisligi-( yatayKenarBosluk*2 );
			int[] kolonGenislikleri = GenelToplamRaporKolonGenislikleriniGetir(kolonlar , tabloGenisligi);
			int satirIndex = 0;
			int sayfaNo = 0;

			while(satirIndex<satirlar.Count)
			{
				Bitmap sayfa = new Bitmap(sayfaGenisligi , sayfaYuksekligi , PixelFormat.Format24bppRgb);
				sayfa.SetResolution(150f , 150f);

				using(Graphics g = Graphics.FromImage(sayfa))
				using(Font baslikFont = new Font("Segoe UI" , 24f , FontStyle.Bold))
				using(Font altBaslikFont = new Font("Segoe UI" , 10.5f , FontStyle.Regular))
				using(Font kartBaslikFont = new Font("Segoe UI" , 8.8f , FontStyle.Bold))
				using(Font kartDegerFont = new Font("Segoe UI Semibold" , 13.5f , FontStyle.Bold))
				using(Font tabloBaslikFont = new Font("Segoe UI" , 9.6f , FontStyle.Bold))
				using(Font tabloFont = new Font("Segoe UI" , 9.1f , FontStyle.Regular))
				using(Font dipnotFont = new Font("Segoe UI" , 8.5f , FontStyle.Regular))
				using(Pen kenarlikKalemi = new Pen(Color.FromArgb(226 , 232 , 240)))
				using(Pen ayiriciKalemi = new Pen(Color.FromArgb(203 , 213 , 225)))
				using(SolidBrush panelFirca = new SolidBrush(Color.FromArgb(248 , 250 , 252)))
				using(SolidBrush baslikArkaPlanFirca = new SolidBrush(Color.FromArgb(15 , 118 , 110)))
				{
					g.Clear(Color.White);
					g.SmoothingMode=SmoothingMode.AntiAlias;
					g.InterpolationMode=InterpolationMode.HighQualityBicubic;

					int ustAlanYuksekligi = sayfaNo==0 ? ilkSayfaUstAlanYuksekligi : devamSayfasiUstAlanYuksekligi;
					Rectangle ustAlan = new Rectangle(yatayKenarBosluk , ustBosluk , tabloGenisligi , ustAlanYuksekligi);
					using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(ustAlan , 18))
					{
						g.FillPath(panelFirca , yol);
						g.DrawPath(kenarlikKalemi , yol);
					}

					Rectangle ustAlanIci = RectangleIcBoslukGetir(ustAlan , 28 , 24);
					RaporMetinCiz(
						g ,
						"GENEL TOPLAM RAPORU" ,
						baslikFont ,
						new Rectangle(ustAlanIci.X , ustAlanIci.Y , ustAlanIci.Width , 40) ,
						Color.FromArgb(15 , 23 , 42) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					RaporMetinCiz(
						g ,
						"Olusturulma: "+DateTime.Now.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) ,
						altBaslikFont ,
						new Rectangle(ustAlanIci.X , ustAlanIci.Y+46 , ustAlanIci.Width/2 , 24) ,
						Color.FromArgb(71 , 85 , 105) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
					RaporMetinCiz(
						g ,
						"Sayfa "+( sayfaNo+1 ).ToString(CultureInfo.InvariantCulture) ,
						altBaslikFont ,
						new Rectangle(ustAlan.Right-180 , ustAlanIci.Y+46 , 152 , 24) ,
						Color.FromArgb(71 , 85 , 105) ,
						TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

					if(sayfaNo==0)
					{
						Rectangle kartAlani = new Rectangle(ustAlanIci.X , ustAlanIci.Y+92 , ustAlanIci.Width , 108);
						GenelToplamOzetKartlariniCiz(
							g ,
							kartAlani ,
							kartBaslikFont ,
							kartDegerFont ,
							kenarlikKalemi);
					}

					int y = ustAlan.Bottom+22;
					int x = yatayKenarBosluk;

					for(int i = 0 ; i<kolonlar.Count ; i++)
					{
						Rectangle hucre = new Rectangle(x , y , kolonGenislikleri[i] , tabloBaslikYuksekligi);
						g.FillRectangle(baslikArkaPlanFirca , hucre);
						g.DrawRectangle(kenarlikKalemi , hucre);
						RaporMetinCiz(
							g ,
							kolonlar[i].HeaderText ,
							tabloBaslikFont ,
							RectangleIcBoslukGetir(hucre , 10 , 6) ,
							Color.White ,
							TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
						x+=kolonGenislikleri[i];
					}

					y+=tabloBaslikYuksekligi;
					int altSinir = sayfaYuksekligi-altBosluk;
					while(satirIndex<satirlar.Count&&y+satirYuksekligi<=altSinir)
					{
						DataGridViewRow satir = satirlar[satirIndex];
						bool vurguluSatir = GenelToplamSatiriVurguluMu(satir);
						Color satirArkaPlan = vurguluSatir
							? Color.FromArgb(236 , 253 , 245)
							: ( satirIndex%2==0 ? Color.White : Color.FromArgb(248 , 250 , 252) );

						x=yatayKenarBosluk;
						for(int i = 0 ; i<kolonlar.Count ; i++)
						{
							DataGridViewColumn kolon = kolonlar[i];
							Rectangle hucre = new Rectangle(x , y , kolonGenislikleri[i] , satirYuksekligi);
							using(SolidBrush hucreFirca = new SolidBrush(satirArkaPlan))
							{
								g.FillRectangle(hucreFirca , hucre);
							}

							g.DrawRectangle(kenarlikKalemi , hucre);
							TextFormatFlags hizalama = GenelToplamKolonSayisalMi(kolon)
								? TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis
								: TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
							RaporMetinCiz(
								g ,
								GenelToplamHucreMetniGetir(kolon , satir.Cells[kolon.Index].Value) ,
								tabloFont ,
								RectangleIcBoslukGetir(hucre , 10 , 4) ,
								vurguluSatir ? Color.FromArgb(6 , 78 , 59) : Color.FromArgb(30 , 41 , 59) ,
								hizalama);
							x+=kolonGenislikleri[i];
						}

						y+=satirYuksekligi;
						satirIndex++;
					}

					int dipnotY = sayfaYuksekligi-altBosluk+14;
					g.DrawLine(ayiriciKalemi , yatayKenarBosluk , dipnotY-10 , sayfaGenisligi-yatayKenarBosluk , dipnotY-10);
					RaporMetinCiz(
						g ,
						"Rapor, satis ve toptanci toplamlarini ayni ekranda birlestirir." ,
						dipnotFont ,
						new Rectangle(yatayKenarBosluk , dipnotY , tabloGenisligi , 20) ,
						Color.FromArgb(100 , 116 , 139) ,
						TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
				}

				sayfalar.Add(sayfa);
				sayfaNo++;
			}

			return sayfalar;
		}

		private void GenelToplamOzetKartlariniCiz ( Graphics g , Rectangle alan , Font baslikFont , Font degerFont , Pen kenarlikKalemi )
		{
			string[] basliklar =
			{
				"GENEL CIRO",
				"GENEL KAR",
				"TOPTANCI ODEME",
				"KALAN BORC"
			};
			string[] degerler =
			{
				BosIseYerineGetir(_genelToplamCiroLabel?.Text),
				BosIseYerineGetir(_genelToplamKarLabel?.Text),
				BosIseYerineGetir(_genelToplamToptanciOdemeLabel?.Text),
				BosIseYerineGetir(_genelToplamKalanBorcLabel?.Text)
			};
			Color[] ustRenkler =
			{
				Color.FromArgb(2 , 132 , 199),
				Color.FromArgb(5 , 150 , 105),
				Color.FromArgb(37 , 99 , 235),
				Color.FromArgb(220 , 38 , 38)
			};

			int bosluk = 16;
			int kartGenisligi = ( alan.Width-( bosluk*3 ) )/4;
			for(int i = 0 ; i<4 ; i++)
			{
				Rectangle kart = new Rectangle(alan.X+( i*( kartGenisligi+bosluk )) , alan.Y , kartGenisligi , alan.Height);
				using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(kart , 16))
				using(SolidBrush kartFirca = new SolidBrush(Color.White))
				using(SolidBrush ustRenkFirca = new SolidBrush(ustRenkler[i]))
				{
					g.FillPath(kartFirca , yol);
					g.DrawPath(kenarlikKalemi , yol);
					g.FillRectangle(ustRenkFirca , new Rectangle(kart.X , kart.Y , kart.Width , 8));
				}

				Rectangle kartIci = RectangleIcBoslukGetir(kart , 16 , 18);
				RaporMetinCiz(
					g ,
					basliklar[i] ,
					baslikFont ,
					new Rectangle(kartIci.X , kartIci.Y , kartIci.Width , 22) ,
					Color.FromArgb(100 , 116 , 139) ,
					TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
				RaporMetinCiz(
					g ,
					degerler[i] ,
					degerFont ,
					new Rectangle(kartIci.X , kartIci.Y+34 , kartIci.Width , 34) ,
					Color.FromArgb(15 , 23 , 42) ,
					TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
			}
		}

		private int[] GenelToplamRaporKolonGenislikleriniGetir ( IList<DataGridViewColumn> kolonlar , int toplamGenislik )
		{
			int[] sonuc = new int[kolonlar.Count];
			if(kolonlar.Count==0)
				return sonuc;

			double toplamOran = kolonlar.Sum(GenelToplamKolonGenisligiOraniniGetir);
			int kullanilanGenislik = 0;
			for(int i = 0 ; i<kolonlar.Count ; i++)
			{
				int genislik = i==kolonlar.Count-1
					? Math.Max(72 , toplamGenislik-kullanilanGenislik)
					: Math.Max(72 , ( int )Math.Round(toplamGenislik*( GenelToplamKolonGenisligiOraniniGetir(kolonlar[i])/toplamOran )));
				sonuc[i]=genislik;
				kullanilanGenislik+=genislik;
			}

			if(kolonlar.Count>0&&kullanilanGenislik!=toplamGenislik)
				sonuc[kolonlar.Count-1]+=toplamGenislik-kullanilanGenislik;

			return sonuc;
		}

		private double GenelToplamKolonGenisligiOraniniGetir ( DataGridViewColumn kolon )
		{
			if(kolon==null)
				return 1d;

			switch(kolon.Name)
			{
				case "KayitTuru":
					return 1.15d;
				case "AdSoyad":
					return 2.2d;
				case "Ciro":
				case "KarTutari":
				case "ToplamAlim":
				case "ToplamOdeme":
					return 1.35d;
				case "ToplamMaliyet":
				case "KalanBakiye":
					return 1.55d;
				case "Aciklama":
					return 2.15d;
				default:
					return Math.Max(1d , kolon.Width/95d);
			}
		}

		private List<Bitmap> BelgeOnizlemeSayfalariniOlustur ( BelgeYazdirmaVerisi veri )
		{
			List<Bitmap> sayfalar = new List<Bitmap>();
			if(veri==null)
				return sayfalar;

			BelgeYazdirmaVerisi oncekiVeri = _aktifBelgeYazdirmaVerisi;
			int oncekiSatirIndex = _aktifBelgeYazdirmaSatirIndex;
			int oncekiSayfaNo = _aktifBelgeYazdirmaSayfaNo;

			PrintDocument belge = new PrintDocument();
			PreviewPrintController onizlemeDenetleyicisi = new PreviewPrintController();
			try
			{
				_aktifBelgeYazdirmaVerisi=veri;
				_aktifBelgeYazdirmaSatirIndex=0;
				_aktifBelgeYazdirmaSayfaNo=0;

				belge.DocumentName=BelgeKimlikMetniGetir(veri);
				belge.DefaultPageSettings.Margins=new Margins(40 , 40 , 35 , 35);
				belge.PrintController=onizlemeDenetleyicisi;
				belge.BeginPrint+=BelgeYazdirmaBelgesi_BeginPrint;
				belge.PrintPage+=BelgeYazdirmaBelgesi_PrintPage;
				belge.Print();

				PreviewPageInfo[] sayfaBilgileri = onizlemeDenetleyicisi.GetPreviewPageInfo();
				foreach(PreviewPageInfo sayfaBilgisi in sayfaBilgileri)
				{
					Image kaynakSayfa = sayfaBilgisi.Image;
					if(kaynakSayfa==null)
						continue;

					try
					{
						float yatayCozunurluk = kaynakSayfa.HorizontalResolution>0 ? kaynakSayfa.HorizontalResolution : 96f;
						float dikeyCozunurluk = kaynakSayfa.VerticalResolution>0 ? kaynakSayfa.VerticalResolution : 96f;
						Bitmap kopya = new Bitmap(kaynakSayfa.Width , kaynakSayfa.Height , PixelFormat.Format24bppRgb);
						kopya.SetResolution(yatayCozunurluk , dikeyCozunurluk);
						using(Graphics g = Graphics.FromImage(kopya))
						{
							g.Clear(Color.White);
							g.InterpolationMode=InterpolationMode.HighQualityBicubic;
							g.SmoothingMode=SmoothingMode.HighQuality;
							g.DrawImage(kaynakSayfa , new Rectangle(0 , 0 , kopya.Width , kopya.Height));
						}

						sayfalar.Add(kopya);
					}
					finally
					{
						kaynakSayfa.Dispose();
					}
				}
			}
			finally
			{
				belge.Dispose();
				_aktifBelgeYazdirmaVerisi=oncekiVeri;
				_aktifBelgeYazdirmaSatirIndex=oncekiSatirIndex;
				_aktifBelgeYazdirmaSayfaNo=oncekiSayfaNo;
			}

			return sayfalar;
		}

		private void BelgePdfDosyasiOlustur ( BelgeYazdirmaVerisi veri , string dosyaYolu )
		{
			List<Bitmap> sayfalar = BelgeOnizlemeSayfalariniOlustur(veri);
			if(sayfalar.Count==0)
				throw new InvalidOperationException("PDF iÃ§in sayfa oluÅŸturulamadÄ±.");

			try
			{
				using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
				using(BinaryWriter yazar = new BinaryWriter(akim , Encoding.ASCII))
				{
					int toplamNesne = 2+(sayfalar.Count*3);
					List<long> nesneOfsetleri = new List<long>(new long[toplamNesne+1]);

					PdfAsciiYaz(yazar , "%PDF-1.4\n");
					yazar.Write(new byte[] { 0x25 , 0xC7 , 0xEC , 0x8F , 0xA2 , 0x0A });

					PdfNesnesiBaslat(yazar , nesneOfsetleri , 1);
					PdfAsciiYaz(yazar , "<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

					PdfNesnesiBaslat(yazar , nesneOfsetleri , 2);
					StringBuilder cocukSayfalar = new StringBuilder();
					for(int i = 0 ; i<sayfalar.Count ; i++)
					{
						if(cocukSayfalar.Length>0)
							cocukSayfalar.Append(' ');
						cocukSayfalar.Append(( 3+( i*3 ) ).ToString(CultureInfo.InvariantCulture)).Append(" 0 R");
					}

					PdfAsciiYaz(
						yazar ,
						"<< /Type /Pages /Count "+sayfalar.Count.ToString(CultureInfo.InvariantCulture)+" /Kids [ "+cocukSayfalar+" ] >>\nendobj\n");

					for(int i = 0 ; i<sayfalar.Count ; i++)
					{
						Bitmap sayfa = sayfalar[i];
						byte[] hamRgbBaitleri = BitmapiHamRgbBaitlerineCevir(sayfa);
						double genislikPunto = sayfa.Width*72d/( sayfa.HorizontalResolution>0 ? sayfa.HorizontalResolution : 96d );
						double yukseklikPunto = sayfa.Height*72d/( sayfa.VerticalResolution>0 ? sayfa.VerticalResolution : 96d );
						string genislikMetni = PdfOlcuMetniGetir(genislikPunto);
						string yukseklikMetni = PdfOlcuMetniGetir(yukseklikPunto);

						int sayfaNesneNo = 3+( i*3 );
						int akisNesneNo = sayfaNesneNo+1;
						int resimNesneNo = sayfaNesneNo+2;

						PdfNesnesiBaslat(yazar , nesneOfsetleri , sayfaNesneNo);
						PdfAsciiYaz(
							yazar ,
							"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "+genislikMetni+" "+yukseklikMetni+"] "+
							"/Resources << /XObject << /Im0 "+resimNesneNo.ToString(CultureInfo.InvariantCulture)+" 0 R >> >> "+
							"/Contents "+akisNesneNo.ToString(CultureInfo.InvariantCulture)+" 0 R >>\nendobj\n");

						byte[] akisBaitleri = Encoding.ASCII.GetBytes(
							"q\n"+
							genislikMetni+" 0 0 "+yukseklikMetni+" 0 0 cm\n"+
							"/Im0 Do\n"+
							"Q\n");

						PdfNesnesiBaslat(yazar , nesneOfsetleri , akisNesneNo);
						PdfAsciiYaz(yazar , "<< /Length "+akisBaitleri.Length.ToString(CultureInfo.InvariantCulture)+" >>\nstream\n");
						yazar.Write(akisBaitleri);
						PdfAsciiYaz(yazar , "endstream\nendobj\n");

						PdfNesnesiBaslat(yazar , nesneOfsetleri , resimNesneNo);
						PdfAsciiYaz(
							yazar ,
							"<< /Type /XObject /Subtype /Image /Width "+sayfa.Width.ToString(CultureInfo.InvariantCulture)+
							" /Height "+sayfa.Height.ToString(CultureInfo.InvariantCulture)+
							" /ColorSpace /DeviceRGB /BitsPerComponent 8 /Length "+
							hamRgbBaitleri.Length.ToString(CultureInfo.InvariantCulture)+" >>\nstream\n");
						yazar.Write(hamRgbBaitleri);
						PdfAsciiYaz(yazar , "\nendstream\nendobj\n");
					}

					long xrefBaslangici = yazar.BaseStream.Position;
					PdfAsciiYaz(yazar , "xref\n0 "+( toplamNesne+1 ).ToString(CultureInfo.InvariantCulture)+"\n");
					PdfAsciiYaz(yazar , "0000000000 65535 f \n");
					for(int i = 1 ; i<=toplamNesne ; i++)
					{
						PdfAsciiYaz(
							yazar ,
							nesneOfsetleri[i].ToString("0000000000" , CultureInfo.InvariantCulture)+" 00000 n \n");
					}

					PdfAsciiYaz(
						yazar ,
						"trailer\n<< /Size "+( toplamNesne+1 ).ToString(CultureInfo.InvariantCulture)+" /Root 1 0 R >>\n"+
						"startxref\n"+
						xrefBaslangici.ToString(CultureInfo.InvariantCulture)+"\n"+
						"%%EOF");
				}
			}
			finally
			{
				foreach(Bitmap sayfa in sayfalar)
					sayfa.Dispose();
			}
		}

		private void BitmapSayfalariniPdfDosyasinaYaz ( List<Bitmap> sayfalar , string dosyaYolu , string hataMesaji )
		{
			if(sayfalar==null||sayfalar.Count==0)
				throw new InvalidOperationException(string.IsNullOrWhiteSpace(hataMesaji) ? "PDF icin sayfa olusturulamadi." : hataMesaji);

			using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
			using(BinaryWriter yazar = new BinaryWriter(akim , Encoding.ASCII))
			{
				int toplamNesne = 2+( sayfalar.Count*3 );
				List<long> nesneOfsetleri = new List<long>(new long[toplamNesne+1]);

				PdfAsciiYaz(yazar , "%PDF-1.4\n");
				yazar.Write(new byte[] { 0x25 , 0xC7 , 0xEC , 0x8F , 0xA2 , 0x0A });

				PdfNesnesiBaslat(yazar , nesneOfsetleri , 1);
				PdfAsciiYaz(yazar , "<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

				PdfNesnesiBaslat(yazar , nesneOfsetleri , 2);
				StringBuilder cocukSayfalar = new StringBuilder();
				for(int i = 0 ; i<sayfalar.Count ; i++)
				{
					if(cocukSayfalar.Length>0)
						cocukSayfalar.Append(' ');
					cocukSayfalar.Append(( 3+( i*3 ) ).ToString(CultureInfo.InvariantCulture)).Append(" 0 R");
				}

				PdfAsciiYaz(
					yazar ,
					"<< /Type /Pages /Count "+sayfalar.Count.ToString(CultureInfo.InvariantCulture)+" /Kids [ "+cocukSayfalar+" ] >>\nendobj\n");

				for(int i = 0 ; i<sayfalar.Count ; i++)
				{
					Bitmap sayfa = sayfalar[i];
					byte[] hamRgbBaitleri = BitmapiHamRgbBaitlerineCevir(sayfa);
					double genislikPunto = sayfa.Width*72d/( sayfa.HorizontalResolution>0 ? sayfa.HorizontalResolution : 96d );
					double yukseklikPunto = sayfa.Height*72d/( sayfa.VerticalResolution>0 ? sayfa.VerticalResolution : 96d );
					string genislikMetni = PdfOlcuMetniGetir(genislikPunto);
					string yukseklikMetni = PdfOlcuMetniGetir(yukseklikPunto);

					int sayfaNesneNo = 3+( i*3 );
					int akisNesneNo = sayfaNesneNo+1;
					int resimNesneNo = sayfaNesneNo+2;

					PdfNesnesiBaslat(yazar , nesneOfsetleri , sayfaNesneNo);
					PdfAsciiYaz(
						yazar ,
						"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "+genislikMetni+" "+yukseklikMetni+"] "+
						"/Resources << /XObject << /Im0 "+resimNesneNo.ToString(CultureInfo.InvariantCulture)+" 0 R >> >> "+
						"/Contents "+akisNesneNo.ToString(CultureInfo.InvariantCulture)+" 0 R >>\nendobj\n");

					byte[] akisBaitleri = Encoding.ASCII.GetBytes(
						"q\n"+
						genislikMetni+" 0 0 "+yukseklikMetni+" 0 0 cm\n"+
						"/Im0 Do\n"+
						"Q\n");

					PdfNesnesiBaslat(yazar , nesneOfsetleri , akisNesneNo);
					PdfAsciiYaz(yazar , "<< /Length "+akisBaitleri.Length.ToString(CultureInfo.InvariantCulture)+" >>\nstream\n");
					yazar.Write(akisBaitleri);
					PdfAsciiYaz(yazar , "endstream\nendobj\n");

					PdfNesnesiBaslat(yazar , nesneOfsetleri , resimNesneNo);
					PdfAsciiYaz(
						yazar ,
						"<< /Type /XObject /Subtype /Image /Width "+sayfa.Width.ToString(CultureInfo.InvariantCulture)+
						" /Height "+sayfa.Height.ToString(CultureInfo.InvariantCulture)+
						" /ColorSpace /DeviceRGB /BitsPerComponent 8 /Length "+
						hamRgbBaitleri.Length.ToString(CultureInfo.InvariantCulture)+" >>\nstream\n");
					yazar.Write(hamRgbBaitleri);
					PdfAsciiYaz(yazar , "\nendstream\nendobj\n");
				}

				long xrefBaslangici = yazar.BaseStream.Position;
				PdfAsciiYaz(yazar , "xref\n0 "+( toplamNesne+1 ).ToString(CultureInfo.InvariantCulture)+"\n");
				PdfAsciiYaz(yazar , "0000000000 65535 f \n");
				for(int i = 1 ; i<=toplamNesne ; i++)
				{
					PdfAsciiYaz(
						yazar ,
						nesneOfsetleri[i].ToString("0000000000" , CultureInfo.InvariantCulture)+" 00000 n \n");
				}

				PdfAsciiYaz(
					yazar ,
					"trailer\n<< /Size "+( toplamNesne+1 ).ToString(CultureInfo.InvariantCulture)+" /Root 1 0 R >>\n"+
					"startxref\n"+
					xrefBaslangici.ToString(CultureInfo.InvariantCulture)+"\n"+
					"%%EOF");
			}
		}

		private byte[] BitmapiHamRgbBaitlerineCevir ( Bitmap kaynak )
		{
			Bitmap calismaBitmapi = kaynak;
			bool geciciBitmapOlusturuldu = false;
			if(kaynak.PixelFormat!=PixelFormat.Format24bppRgb)
			{
				calismaBitmapi = new Bitmap(kaynak.Width , kaynak.Height , PixelFormat.Format24bppRgb);
				calismaBitmapi.SetResolution(
					kaynak.HorizontalResolution>0 ? kaynak.HorizontalResolution : 96f ,
					kaynak.VerticalResolution>0 ? kaynak.VerticalResolution : 96f);
				using(Graphics g = Graphics.FromImage(calismaBitmapi))
				{
					g.Clear(Color.White);
					g.DrawImageUnscaled(kaynak , 0 , 0);
				}

				geciciBitmapOlusturuldu = true;
			}

			try
			{
				Rectangle alan = new Rectangle(0 , 0 , calismaBitmapi.Width , calismaBitmapi.Height);
				BitmapData bitmapVerisi = calismaBitmapi.LockBits(alan , ImageLockMode.ReadOnly , PixelFormat.Format24bppRgb);
				try
				{
					int satirBaytSayisi = calismaBitmapi.Width*3;
					int stride = Math.Abs(bitmapVerisi.Stride);
					byte[] rgbBaitleri = new byte[satirBaytSayisi*calismaBitmapi.Height];
					byte[] satirBaitleri = new byte[stride];

					for(int y = 0 ; y<calismaBitmapi.Height ; y++)
					{
						int satirBaslangici = bitmapVerisi.Stride>0
							? ( y*bitmapVerisi.Stride )
							: ( ( calismaBitmapi.Height-1-y )*stride );
						Marshal.Copy(IntPtr.Add(bitmapVerisi.Scan0 , satirBaslangici) , satirBaitleri , 0 , stride);

						int hedefSatirBaslangici = y*satirBaytSayisi;
						for(int x = 0 ; x<calismaBitmapi.Width ; x++)
						{
							int kaynakIndex = x*3;
							int hedefIndex = hedefSatirBaslangici+( x*3 );

							rgbBaitleri[hedefIndex]=satirBaitleri[kaynakIndex+2];
							rgbBaitleri[hedefIndex+1]=satirBaitleri[kaynakIndex+1];
							rgbBaitleri[hedefIndex+2]=satirBaitleri[kaynakIndex];
						}
					}

					return rgbBaitleri;
				}
				finally
				{
					calismaBitmapi.UnlockBits(bitmapVerisi);
				}
			}
			finally
			{
				if(geciciBitmapOlusturuldu)
					calismaBitmapi.Dispose();
			}
		}

		private void PdfAsciiYaz ( BinaryWriter yazar , string metin )
		{
			yazar.Write(Encoding.ASCII.GetBytes(metin));
		}

		private void PdfNesnesiBaslat ( BinaryWriter yazar , List<long> nesneOfsetleri , int nesneNo )
		{
			nesneOfsetleri[nesneNo]=yazar.BaseStream.Position;
			PdfAsciiYaz(yazar , nesneNo.ToString(CultureInfo.InvariantCulture)+" 0 obj\n");
		}

		private string PdfOlcuMetniGetir ( double deger )
		{
			return deger.ToString("0.###" , CultureInfo.InvariantCulture);
		}

		private void BelgeExcelDosyasiOlustur ( BelgeYazdirmaVerisi veri , string dosyaYolu )
		{
			using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
			using(ZipArchive arsiv = new ZipArchive(akim , ZipArchiveMode.Create))
			{
				ZipArsivineMetinYaz(arsiv , "[Content_Types].xml" , ExcelIcerikTurleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "_rels/.rels" , ExcelPaketIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/workbook.xml" , ExcelCalismaKitabiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/_rels/workbook.xml.rels" , ExcelCalismaKitabiIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/styles.xml" , ExcelStilleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/worksheets/sheet1.xml" , ExcelCalismaSayfasiXmlGetir(veri));
			}
		}

		private void CariHesapExcelDosyasiOlustur ( CariHesapRaporVerisi veri , string dosyaYolu )
		{
			using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
			using(ZipArchive arsiv = new ZipArchive(akim , ZipArchiveMode.Create))
			{
				ZipArsivineMetinYaz(arsiv , "[Content_Types].xml" , ExcelIcerikTurleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "_rels/.rels" , ExcelPaketIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/workbook.xml" , ExcelCalismaKitabiXmlGetir("CariHesap"));
				ZipArsivineMetinYaz(arsiv , "xl/_rels/workbook.xml.rels" , ExcelCalismaKitabiIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/styles.xml" , ExcelStilleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/worksheets/sheet1.xml" , CariHesapExcelCalismaSayfasiXmlGetir(veri));
			}
		}

		private void ToptanciBakiyeExcelDosyasiOlustur ( ToptanciBakiyeRaporVerisi veri , string dosyaYolu )
		{
			using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
			using(ZipArchive arsiv = new ZipArchive(akim , ZipArchiveMode.Create))
			{
				ZipArsivineMetinYaz(arsiv , "[Content_Types].xml" , ExcelIcerikTurleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "_rels/.rels" , ExcelPaketIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/workbook.xml" , ExcelCalismaKitabiXmlGetir("ToptanciBakiye"));
				ZipArsivineMetinYaz(arsiv , "xl/_rels/workbook.xml.rels" , ExcelCalismaKitabiIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/styles.xml" , ExcelStilleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/worksheets/sheet1.xml" , ToptanciBakiyeExcelCalismaSayfasiXmlGetir(veri));
			}
		}

		private void GenelToplamExcelDosyasiOlustur ( string dosyaYolu )
		{
			using(FileStream akim = new FileStream(dosyaYolu , FileMode.Create , FileAccess.Write , FileShare.None))
			using(ZipArchive arsiv = new ZipArchive(akim , ZipArchiveMode.Create))
			{
				ZipArsivineMetinYaz(arsiv , "[Content_Types].xml" , ExcelIcerikTurleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "_rels/.rels" , ExcelPaketIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/workbook.xml" , ExcelCalismaKitabiXmlGetir("GenelToplam"));
				ZipArsivineMetinYaz(arsiv , "xl/_rels/workbook.xml.rels" , ExcelCalismaKitabiIliskiXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/styles.xml" , ExcelStilleriXmlGetir());
				ZipArsivineMetinYaz(arsiv , "xl/worksheets/sheet1.xml" , GenelToplamExcelCalismaSayfasiXmlGetir());
			}
		}

		private string CariHesapExcelCalismaSayfasiXmlGetir ( CariHesapRaporVerisi veri )
		{
			const int toplamSutun = 8;
			StringBuilder satirXml = new StringBuilder();
			List<string> birlesimAlanlari = new List<string>();
			int satirNo = 1;
			string sonFaturaMetni = veri!=null&&veri.SonFaturaTarihi.HasValue
				? veri.SonFaturaTarihi.Value.ToString("dd.MM.yyyy" , _yazdirmaKulturu)
				: "-";

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "CARI HESAP RAPORU" , 1)));
			birlesimAlanlari.Add("A1:"+ExcelHucreReferansiGetir(toplamSutun , 1));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , YazdirmaSirketAdi , 11)));
			birlesimAlanlari.Add("A2:"+ExcelHucreReferansiGetir(toplamSutun , 2));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Olusturulma: "+( veri?.RaporTarihi??DateTime.Now ).ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) , 12)));
			birlesimAlanlari.Add("A3:"+ExcelHucreReferansiGetir(toplamSutun , 3));
			satirNo+=2;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Cari ID" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , ( veri?.CariId??0 ).ToString("N0" , _yazdirmaKulturu) , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Cari" , 3) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , BosIseYerineGetir(veri?.CariAdi) , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			birlesimAlanlari.Add("E"+satirNo.ToString(CultureInfo.InvariantCulture)+":H"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Telefon" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(veri?.Telefon) , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Son Fatura" , 3) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , sonFaturaMetni , 4) ,
				ExcelMetinHucreXmlGetir(7 , satirNo , "Hareket" , 3) ,
				ExcelSayiHucreXmlGetir(8 , satirNo , veri?.Hareketler.Count??0 , 8)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			birlesimAlanlari.Add("E"+satirNo.ToString(CultureInfo.InvariantCulture)+":F"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Toplam Fatura" , 9) ,
				ExcelSayiHucreXmlGetir(2 , satirNo , veri?.ToplamFatura??0m , 10) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Toplam Tahsilat" , 9) ,
				ExcelSayiHucreXmlGetir(5 , satirNo , veri?.ToplamTahsilat??0m , 10) ,
				ExcelMetinHucreXmlGetir(7 , satirNo , "Kalan Tutar" , 9) ,
				ExcelSayiHucreXmlGetir(8 , satirNo , veri?.KalanTutar??0m , 10)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			birlesimAlanlari.Add("E"+satirNo.ToString(CultureInfo.InvariantCulture)+":F"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo+=2;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Kaynak" , 5) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , "Islem Turu" , 5) ,
				ExcelMetinHucreXmlGetir(3 , satirNo , "Belge No" , 5) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Tarih" , 5) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , "Alinan Urun Tutari" , 5) ,
				ExcelMetinHucreXmlGetir(6 , satirNo , "Alinan Tahsilat" , 5) ,
				ExcelMetinHucreXmlGetir(7 , satirNo , "Kalan Tutar" , 5) ,
				ExcelMetinHucreXmlGetir(8 , satirNo , "Not" , 5)));
			satirNo++;

			if(veri==null||veri.Hareketler.Count==0)
			{
				satirXml.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , "Gorunur hareket kaydi bulunamadi." , 12)));
				birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":H"+satirNo.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				foreach(CariHesapRaporSatiri satir in veri.Hareketler)
				{
					satirXml.Append(ExcelSatirXmlGetir(
						satirNo ,
						ExcelMetinHucreXmlGetir(1 , satirNo , BosIseYerineGetir(satir.Kaynak) , 6) ,
						ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(satir.IslemTuru) , 6) ,
						ExcelMetinHucreXmlGetir(3 , satirNo , BosIseYerineGetir(satir.BelgeNo) , 6) ,
						ExcelMetinHucreXmlGetir(4 , satirNo , satir.Tarih.HasValue ? satir.Tarih.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) : "-" , 7) ,
						ExcelSayiHucreXmlGetir(5 , satirNo , satir.BorcTutar , 8) ,
						ExcelSayiHucreXmlGetir(6 , satirNo , satir.TahsilatTutar , 8) ,
						ExcelSayiHucreXmlGetir(7 , satirNo , satir.KalanTutar , 8) ,
						ExcelMetinHucreXmlGetir(8 , satirNo , BosIseYerineGetir(satir.Not) , 12)));
					satirNo++;
				}
			}

			double[] sutunGenislikleri = { 15d , 18d , 18d , 20d , 18d , 18d , 18d , 30d };
			StringBuilder sutunlarXml = new StringBuilder();
			sutunlarXml.Append("<cols>");
			for(int i = 0 ; i<sutunGenislikleri.Length ; i++)
			{
				sutunlarXml.Append("<col min=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" max=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" width=\"")
					.Append(sutunGenislikleri[i].ToString("0.##" , CultureInfo.InvariantCulture))
					.Append("\" customWidth=\"1\"/>");
			}
			sutunlarXml.Append("</cols>");

			StringBuilder birlesimler = new StringBuilder();
			if(birlesimAlanlari.Count>0)
			{
				birlesimler.Append("<mergeCells count=\"")
					.Append(birlesimAlanlari.Count.ToString(CultureInfo.InvariantCulture))
					.Append("\">");
				foreach(string alan in birlesimAlanlari)
					birlesimler.Append("<mergeCell ref=\"").Append(alan).Append("\"/>");
				birlesimler.Append("</mergeCells>");
			}

			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"+
				"<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>"+
				"<sheetFormatPr defaultRowHeight=\"20\"/>"+
				sutunlarXml+
				"<sheetData>"+satirXml+"</sheetData>"+
				birlesimler+
				"<pageMargins left=\"0.35\" right=\"0.35\" top=\"0.55\" bottom=\"0.55\" header=\"0.3\" footer=\"0.3\"/>"+
				"</worksheet>";
		}

		private string ToptanciBakiyeExcelCalismaSayfasiXmlGetir ( ToptanciBakiyeRaporVerisi veri )
		{
			const int toplamSutun = 6;
			StringBuilder satirXml = new StringBuilder();
			List<string> birlesimAlanlari = new List<string>();
			int satirNo = 1;
			string sonHareketMetni = veri!=null&&veri.SonHareketTarihi.HasValue
				? veri.SonHareketTarihi.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu)
				: "-";

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "TOPTANCI BAKIYE RAPORU" , 1)));
			birlesimAlanlari.Add("A1:"+ExcelHucreReferansiGetir(toplamSutun , 1));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , YazdirmaSirketAdi , 11)));
			birlesimAlanlari.Add("A2:"+ExcelHucreReferansiGetir(toplamSutun , 2));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Olusturulma: "+( veri?.RaporTarihi??DateTime.Now ).ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) , 12)));
			birlesimAlanlari.Add("A3:"+ExcelHucreReferansiGetir(toplamSutun , 3));
			satirNo+=2;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Toptanci ID" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , ( veri?.ToptanciId??0 ).ToString("N0" , _yazdirmaKulturu) , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Toptanci" , 3) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , BosIseYerineGetir(veri?.ToptanciAdi) , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			birlesimAlanlari.Add("E"+satirNo.ToString(CultureInfo.InvariantCulture)+":F"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Telefon" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(veri?.Telefon) , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Son Hareket" , 3) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , sonHareketMetni , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			birlesimAlanlari.Add("E"+satirNo.ToString(CultureInfo.InvariantCulture)+":F"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Toplam Alim" , 9) ,
				ExcelSayiHucreXmlGetir(2 , satirNo , veri?.ToplamAlim??0m , 10) ,
				ExcelMetinHucreXmlGetir(3 , satirNo , "Toplam Odeme" , 9) ,
				ExcelSayiHucreXmlGetir(4 , satirNo , veri?.ToplamOdeme??0m , 10) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , "Kalan Bakiye" , 9) ,
				ExcelSayiHucreXmlGetir(6 , satirNo , veri?.KalanBakiye??0m , 10)));
			satirNo+=2;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Islem Turu" , 5) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , "Tarih" , 5) ,
				ExcelMetinHucreXmlGetir(3 , satirNo , "Alinan Urun Tutari" , 5) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Verilen Odeme" , 5) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , "Kalan Bakiye" , 5) ,
				ExcelMetinHucreXmlGetir(6 , satirNo , "Not" , 5)));
			satirNo++;

			if(veri==null||veri.Hareketler.Count==0)
			{
				satirXml.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , "Gorunur hareket kaydi bulunamadi." , 12)));
				birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":F"+satirNo.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				foreach(ToptanciBakiyeRaporSatiri satir in veri.Hareketler)
				{
					satirXml.Append(ExcelSatirXmlGetir(
						satirNo ,
						ExcelMetinHucreXmlGetir(1 , satirNo , BosIseYerineGetir(satir.IslemTuru) , 6) ,
						ExcelMetinHucreXmlGetir(2 , satirNo , satir.Tarih.HasValue ? satir.Tarih.Value.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) : "-" , 7) ,
						ExcelSayiHucreXmlGetir(3 , satirNo , satir.BorcTutar , 8) ,
						ExcelSayiHucreXmlGetir(4 , satirNo , satir.OdemeTutar , 8) ,
						ExcelSayiHucreXmlGetir(5 , satirNo , satir.KalanBakiye , 8) ,
						ExcelMetinHucreXmlGetir(6 , satirNo , BosIseYerineGetir(satir.Not) , 12)));
					satirNo++;
				}
			}

			double[] sutunGenislikleri = { 18d , 22d , 20d , 18d , 18d , 32d };
			StringBuilder sutunlarXml = new StringBuilder();
			sutunlarXml.Append("<cols>");
			for(int i = 0 ; i<sutunGenislikleri.Length ; i++)
			{
				sutunlarXml.Append("<col min=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" max=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" width=\"")
					.Append(sutunGenislikleri[i].ToString("0.##" , CultureInfo.InvariantCulture))
					.Append("\" customWidth=\"1\"/>");
			}
			sutunlarXml.Append("</cols>");

			StringBuilder birlesimler = new StringBuilder();
			if(birlesimAlanlari.Count>0)
			{
				birlesimler.Append("<mergeCells count=\"")
					.Append(birlesimAlanlari.Count.ToString(CultureInfo.InvariantCulture))
					.Append("\">");
				foreach(string alan in birlesimAlanlari)
					birlesimler.Append("<mergeCell ref=\"").Append(alan).Append("\"/>");
				birlesimler.Append("</mergeCells>");
			}

			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"+
				"<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>"+
				"<sheetFormatPr defaultRowHeight=\"20\"/>"+
				sutunlarXml+
				"<sheetData>"+satirXml+"</sheetData>"+
				birlesimler+
				"<pageMargins left=\"0.35\" right=\"0.35\" top=\"0.55\" bottom=\"0.55\" header=\"0.3\" footer=\"0.3\"/>"+
				"</worksheet>";
		}

		private string GenelToplamExcelCalismaSayfasiXmlGetir ()
		{
			List<DataGridViewColumn> kolonlar = GenelToplamGorunurKolonlariniGetir();
			List<DataGridViewRow> satirlar = GenelToplamGorunurSatirlariniGetir();
			int toplamSutun = Math.Max(1 , kolonlar.Count);
			StringBuilder satirXml = new StringBuilder();
			List<string> birlesimAlanlari = new List<string>();
			int satirNo = 1;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "GENEL TOPLAM RAPORU" , 1)));
			birlesimAlanlari.Add("A1:"+ExcelHucreReferansiGetir(toplamSutun , 1));
			satirNo++;

			satirXml.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Olusturulma: "+DateTime.Now.ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu) , 11)));
			birlesimAlanlari.Add("A2:"+ExcelHucreReferansiGetir(toplamSutun , 2));
			satirNo+=2;

			string[] kartBasliklari =
			{
				"Genel Ciro",
				"Genel Kar",
				"Toptanci Odeme",
				"Kalan Borc"
			};
			string[] kartDegerleri =
			{
				BosIseYerineGetir(_genelToplamCiroLabel?.Text),
				BosIseYerineGetir(_genelToplamKarLabel?.Text),
				BosIseYerineGetir(_genelToplamToptanciOdemeLabel?.Text),
				BosIseYerineGetir(_genelToplamKalanBorcLabel?.Text)
			};

			List<string> ozetBaslikHucreleri = new List<string>();
			List<string> ozetDegerHucreleri = new List<string>();
			for(int i = 0 ; i<4 ; i++)
			{
				int baslangicSutunu = ( int )Math.Floor(( double )( i*toplamSutun )/4d)+1;
				int bitisSutunu = Math.Max(baslangicSutunu , ( int )Math.Floor(( double )( ( i+1 )*toplamSutun )/4d));
				ozetBaslikHucreleri.Add(ExcelMetinHucreXmlGetir(baslangicSutunu , satirNo , kartBasliklari[i] , 2));
				ozetDegerHucreleri.Add(ExcelMetinHucreXmlGetir(baslangicSutunu , satirNo+1 , kartDegerleri[i] , 4));

				if(bitisSutunu>baslangicSutunu)
				{
					birlesimAlanlari.Add(ExcelHucreReferansiGetir(baslangicSutunu , satirNo)+":"+ExcelHucreReferansiGetir(bitisSutunu , satirNo));
					birlesimAlanlari.Add(ExcelHucreReferansiGetir(baslangicSutunu , satirNo+1)+":"+ExcelHucreReferansiGetir(bitisSutunu , satirNo+1));
				}
			}

			satirXml.Append(ExcelSatirXmlGetir(satirNo , ozetBaslikHucreleri.ToArray()));
			satirNo++;
			satirXml.Append(ExcelSatirXmlGetir(satirNo , ozetDegerHucreleri.ToArray()));
			satirNo+=2;

			List<string> baslikHucreleri = new List<string>();
			for(int i = 0 ; i<kolonlar.Count ; i++)
				baslikHucreleri.Add(ExcelMetinHucreXmlGetir(i+1 , satirNo , kolonlar[i].HeaderText , 5));
			satirXml.Append(ExcelSatirXmlGetir(satirNo , baslikHucreleri.ToArray()));
			satirNo++;

			foreach(DataGridViewRow satir in satirlar)
			{
				List<string> hucreler = new List<string>();
				for(int i = 0 ; i<kolonlar.Count ; i++)
				{
					DataGridViewColumn kolon = kolonlar[i];
					object deger = satir.Cells[kolon.Index].Value;
					if(GenelToplamKolonSayisalMi(kolon))
					{
						hucreler.Add(ExcelSayiHucreXmlGetir(i+1 , satirNo , GenelToplamSayisalDegerGetir(deger) , 8));
					}
					else
					{
						int stilNo = string.Equals(kolon.Name , "Aciklama" , StringComparison.OrdinalIgnoreCase) ? 12 : 6;
						hucreler.Add(ExcelMetinHucreXmlGetir(i+1 , satirNo , BosIseYerineGetir(Convert.ToString(deger)) , stilNo));
					}
				}

				satirXml.Append(ExcelSatirXmlGetir(satirNo , hucreler.ToArray()));
				satirNo++;
			}

			StringBuilder sutunlarXml = new StringBuilder();
			sutunlarXml.Append("<cols>");
			for(int i = 0 ; i<kolonlar.Count ; i++)
			{
				sutunlarXml.Append("<col min=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" max=\"")
					.Append(( i+1 ).ToString(CultureInfo.InvariantCulture))
					.Append("\" width=\"")
					.Append(GenelToplamExcelSutunGenisligiGetir(kolonlar[i]).ToString("0.##" , CultureInfo.InvariantCulture))
					.Append("\" customWidth=\"1\"/>");
			}
			sutunlarXml.Append("</cols>");

			StringBuilder birlesimler = new StringBuilder();
			if(birlesimAlanlari.Count>0)
			{
				birlesimler.Append("<mergeCells count=\"")
					.Append(birlesimAlanlari.Count.ToString(CultureInfo.InvariantCulture))
					.Append("\">");
				foreach(string alan in birlesimAlanlari)
					birlesimler.Append("<mergeCell ref=\"").Append(alan).Append("\"/>");
				birlesimler.Append("</mergeCells>");
			}

			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"+
				"<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>"+
				"<sheetFormatPr defaultRowHeight=\"20\"/>"+
				sutunlarXml+
				"<sheetData>"+satirXml+"</sheetData>"+
				birlesimler+
				"<pageMargins left=\"0.35\" right=\"0.35\" top=\"0.55\" bottom=\"0.55\" header=\"0.3\" footer=\"0.3\"/>"+
				"</worksheet>";
		}

		private double GenelToplamExcelSutunGenisligiGetir ( DataGridViewColumn kolon )
		{
			if(kolon==null)
				return 14d;

			switch(kolon.Name)
			{
				case "KayitTuru":
					return 15d;
				case "AdSoyad":
					return 24d;
				case "Ciro":
				case "KarTutari":
				case "ToplamAlim":
					return 16d;
				case "ToplamOdeme":
				case "ToplamMaliyet":
					return 18d;
				case "KalanBakiye":
					return 20d;
				case "Aciklama":
					return 30d;
				default:
					return 16d;
			}
		}

		private void ZipArsivineMetinYaz ( ZipArchive arsiv , string yol , string icerik )
		{
			ZipArchiveEntry girdi = arsiv.CreateEntry(yol , CompressionLevel.Optimal);
			using(Stream akis = girdi.Open())
			using(StreamWriter yazar = new StreamWriter(akis , new UTF8Encoding(false)))
			{
				yazar.Write(icerik);
			}
		}

		private string ExcelCalismaSayfasiXmlGetir ( BelgeYazdirmaVerisi veri )
		{
			StringBuilder satirlar = new StringBuilder();
			List<string> birlesimAlanlari = new List<string>();
			int satirNo = 1;
			string excelBaslik = BelgeExcelBaslikMetniGetir(veri);
			string belgeNoMetni = BosIseYerineGetir(veri.BelgeNo);
			string tarihMetni = ( veri.Tarih??DateTime.Now ).ToString("dd.MM.yyyy HH:mm" , _yazdirmaKulturu);

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , excelBaslik , 1)));
			birlesimAlanlari.Add("A1:E1");
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , YazdirmaSirketAdi , 11)));
			birlesimAlanlari.Add("A2:E2");
			satirNo++;

			string firmaIletisim = string.Join(
				"  |  " ,
				new[] { YazdirmaSirketAdres , YazdirmaSirketTelefon }
					.Where(x => !string.IsNullOrWhiteSpace(x)));
			if(!string.IsNullOrWhiteSpace(firmaIletisim))
			{
				satirlar.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , firmaIletisim , 12)));
				birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
				satirNo++;
			}

			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Belge" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , excelBaslik , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Tarih" , 3) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , tarihMetni , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Belge No" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , belgeNoMetni , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Cari" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(veri.CariAdi) , 4)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "Telefon" , 3) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(veri.CariTelefon) , 4) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Genel Toplam" , 9) ,
				ExcelSayiHucreXmlGetir(5 , satirNo , veri.GenelToplam , 10)));
			birlesimAlanlari.Add("B"+satirNo.ToString(CultureInfo.InvariantCulture)+":C"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			if(veri.YapilanIsler.Count>0)
			{
				satirlar.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , "YapÄ±lan Ä°ÅŸler" , 2)));
				birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
				satirNo++;

				foreach(string yapilanIs in veri.YapilanIsler)
				{
					satirlar.Append(ExcelSatirXmlGetir(
						satirNo ,
						ExcelMetinHucreXmlGetir(1 , satirNo , BosIseYerineGetir(yapilanIs) , 12)));
					birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
					satirNo++;
				}
			}

			satirNo++;
			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , BosIseYerineGetir(veri.SatirListesiBasligi) , 2)));
			birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(1 , satirNo , "ÃœrÃ¼n AdÄ±" , 5) ,
				ExcelMetinHucreXmlGetir(2 , satirNo , "Birim" , 5) ,
				ExcelMetinHucreXmlGetir(3 , satirNo , "Miktar" , 5) ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Birim Fiyat" , 5) ,
				ExcelMetinHucreXmlGetir(5 , satirNo , "Toplam" , 5)));
			satirNo++;

			foreach(BelgeYazdirmaSatiri satir in veri.Satirlar)
			{
				satirlar.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , BosIseYerineGetir(satir.UrunAdi) , 6) ,
					ExcelMetinHucreXmlGetir(2 , satirNo , BosIseYerineGetir(satir.Birim) , 7) ,
					ExcelSayiHucreXmlGetir(3 , satirNo , satir.Miktar , 8) ,
					ExcelSayiHucreXmlGetir(4 , satirNo , satir.BirimFiyat , 8) ,
					ExcelSayiHucreXmlGetir(5 , satirNo , satir.ToplamTutar , 8)));
				satirNo++;
			}

			if(veri.DipnotSatirlari.Count>0)
			{
				satirNo++;
				satirlar.Append(ExcelSatirXmlGetir(
					satirNo ,
					ExcelMetinHucreXmlGetir(1 , satirNo , "Dipnotlar" , 2)));
				birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
				satirNo++;

				foreach(string dipnot in veri.DipnotSatirlari)
				{
					satirlar.Append(ExcelSatirXmlGetir(
						satirNo ,
						ExcelMetinHucreXmlGetir(1 , satirNo , dipnot , 12)));
					birlesimAlanlari.Add("A"+satirNo.ToString(CultureInfo.InvariantCulture)+":E"+satirNo.ToString(CultureInfo.InvariantCulture));
					satirNo++;
				}
			}

			satirNo++;
			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Ara Toplam" , 9) ,
				ExcelSayiHucreXmlGetir(5 , satirNo , veri.AraToplam , 10)));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "KDV" , 9) ,
				ExcelSayiHucreXmlGetir(5 , satirNo , veri.KdvTutari , 10)));
			satirNo++;

			satirlar.Append(ExcelSatirXmlGetir(
				satirNo ,
				ExcelMetinHucreXmlGetir(4 , satirNo , "Genel Toplam" , 9) ,
				ExcelSayiHucreXmlGetir(5 , satirNo , veri.GenelToplam , 10)));

			StringBuilder birlesimler = new StringBuilder();
			if(birlesimAlanlari.Count>0)
			{
				birlesimler.Append("<mergeCells count=\"").Append(birlesimAlanlari.Count.ToString(CultureInfo.InvariantCulture)).Append("\">");
				foreach(string alan in birlesimAlanlari)
					birlesimler.Append("<mergeCell ref=\"").Append(alan).Append("\"/>");
				birlesimler.Append("</mergeCells>");
			}

			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"+
				"<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>"+
				"<sheetFormatPr defaultRowHeight=\"20\"/>"+
				"<cols>"+
				"<col min=\"1\" max=\"1\" width=\"42\" customWidth=\"1\"/>"+
				"<col min=\"2\" max=\"2\" width=\"15\" customWidth=\"1\"/>"+
				"<col min=\"3\" max=\"3\" width=\"12\" customWidth=\"1\"/>"+
				"<col min=\"4\" max=\"4\" width=\"16\" customWidth=\"1\"/>"+
				"<col min=\"5\" max=\"5\" width=\"18\" customWidth=\"1\"/>"+
				"</cols>"+
				"<sheetData>"+satirlar+"</sheetData>"+
				birlesimler+
				"<pageMargins left=\"0.35\" right=\"0.35\" top=\"0.55\" bottom=\"0.55\" header=\"0.3\" footer=\"0.3\"/>"+
				"</worksheet>";
		}

		private string ExcelCalismaKitabiXmlGetir ()
		{
			return ExcelCalismaKitabiXmlGetir("Belge");
		}

		private string ExcelCalismaKitabiXmlGetir ( string sayfaAdi )
		{
			string temizSayfaAdi = string.IsNullOrWhiteSpace(sayfaAdi) ? "Belge" : sayfaAdi.Trim();
			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"+
				"<sheets><sheet name=\""+ExcelXmlKacis(temizSayfaAdi)+"\" sheetId=\"1\" r:id=\"rId1\"/></sheets>"+
				"</workbook>";
		}

		private string ExcelCalismaKitabiIliskiXmlGetir ()
		{
			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"+
				"<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>"+
				"<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>"+
				"</Relationships>";
		}

		private string ExcelPaketIliskiXmlGetir ()
		{
			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"+
				"<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"+
				"</Relationships>";
		}

		private string ExcelIcerikTurleriXmlGetir ()
		{
			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"+
				"<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"+
				"<Default Extension=\"xml\" ContentType=\"application/xml\"/>"+
				"<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"+
				"<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"+
				"<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>"+
				"</Types>";
		}

		private string ExcelStilleriXmlGetir ()
		{
			string anaTurkuaz = ExcelRenkKoduGetir(Color.FromArgb(0 , 179 , 179));
			string koyuYazi = ExcelRenkKoduGetir(Color.FromArgb(15 , 23 , 42));
			string vurguYazi = ExcelRenkKoduGetir(Color.FromArgb(25 , 88 , 88));
			string notYazisi = ExcelRenkKoduGetir(Color.FromArgb(100 , 116 , 139));
			string beyaz = ExcelRenkKoduGetir(Color.White);
			string toplamArkaPlan = ExcelRenkKoduGetir(Color.FromArgb(232 , 248 , 247));
			string bilgiArkaPlan = ExcelRenkKoduGetir(Color.White);
			string notArkaPlan = ExcelRenkKoduGetir(Color.FromArgb(240 , 251 , 251));
			string kenarlikRengi = ExcelRenkKoduGetir(Color.FromArgb(132 , 214 , 214));

			return
				"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"+
				"<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"+
				"<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.00\"/></numFmts>"+
				"<fonts count=\"6\">"+
				"<font><sz val=\"11\"/><color rgb=\""+koyuYazi+"\"/><name val=\"Segoe UI\"/></font>"+
				"<font><b/><sz val=\"18\"/><color rgb=\""+beyaz+"\"/><name val=\"Segoe UI\"/></font>"+
				"<font><b/><sz val=\"11\"/><color rgb=\""+vurguYazi+"\"/><name val=\"Segoe UI\"/></font>"+
				"<font><b/><sz val=\"11\"/><color rgb=\""+beyaz+"\"/><name val=\"Segoe UI\"/></font>"+
				"<font><b/><sz val=\"13\"/><color rgb=\""+vurguYazi+"\"/><name val=\"Segoe UI\"/></font>"+
				"<font><sz val=\"10\"/><color rgb=\""+notYazisi+"\"/><name val=\"Segoe UI\"/></font>"+
				"</fonts>"+
				"<fills count=\"6\">"+
				"<fill><patternFill patternType=\"none\"/></fill>"+
				"<fill><patternFill patternType=\"gray125\"/></fill>"+
				"<fill><patternFill patternType=\"solid\"><fgColor rgb=\""+anaTurkuaz+"\"/><bgColor indexed=\"64\"/></patternFill></fill>"+
				"<fill><patternFill patternType=\"solid\"><fgColor rgb=\""+toplamArkaPlan+"\"/><bgColor indexed=\"64\"/></patternFill></fill>"+
				"<fill><patternFill patternType=\"solid\"><fgColor rgb=\""+bilgiArkaPlan+"\"/><bgColor indexed=\"64\"/></patternFill></fill>"+
				"<fill><patternFill patternType=\"solid\"><fgColor rgb=\""+notArkaPlan+"\"/><bgColor indexed=\"64\"/></patternFill></fill>"+
				"</fills>"+
				"<borders count=\"2\">"+
				"<border><left/><right/><top/><bottom/><diagonal/></border>"+
				"<border><left style=\"thin\"><color rgb=\""+kenarlikRengi+"\"/></left><right style=\"thin\"><color rgb=\""+kenarlikRengi+"\"/></right><top style=\"thin\"><color rgb=\""+kenarlikRengi+"\"/></top><bottom style=\"thin\"><color rgb=\""+kenarlikRengi+"\"/></bottom><diagonal/></border>"+
				"</borders>"+
				"<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"+
				"<cellXfs count=\"13\">"+
				"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>"+
				"<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"3\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"3\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"4\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"164\" fontId=\"4\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyNumberFormat=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>"+
				"<xf numFmtId=\"0\" fontId=\"5\" fillId=\"5\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>"+
				"</cellXfs>"+
				"<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>"+
				"<dxfs count=\"0\"/>"+
				"<tableStyles count=\"0\" defaultTableStyle=\"TableStyleMedium2\" defaultPivotStyle=\"PivotStyleLight16\"/>"+
				"</styleSheet>";
		}

		private string ExcelRenkKoduGetir ( Color renk )
		{
			return "FF"+
				renk.R.ToString("X2")+
				renk.G.ToString("X2")+
				renk.B.ToString("X2");
		}

		private string ExcelSatirXmlGetir ( int satirNo , params string[] hucreler )
		{
			StringBuilder satir = new StringBuilder();
			satir.Append("<row r=\"").Append(satirNo.ToString(CultureInfo.InvariantCulture)).Append("\">");
			foreach(string hucre in hucreler)
			{
				if(!string.IsNullOrWhiteSpace(hucre))
					satir.Append(hucre);
			}

			satir.Append("</row>");
			return satir.ToString();
		}

		private string ExcelMetinHucreXmlGetir ( int sutunNo , int satirNo , string metin , int stilNo )
		{
			return
				"<c r=\""+ExcelHucreReferansiGetir(sutunNo , satirNo)+"\" t=\"inlineStr\" s=\""+stilNo.ToString(CultureInfo.InvariantCulture)+"\">"+
				"<is><t xml:space=\"preserve\">"+ExcelXmlKacis(metin??string.Empty)+"</t></is></c>";
		}

		private string ExcelSayiHucreXmlGetir ( int sutunNo , int satirNo , decimal deger , int stilNo )
		{
			return
				"<c r=\""+ExcelHucreReferansiGetir(sutunNo , satirNo)+"\" s=\""+stilNo.ToString(CultureInfo.InvariantCulture)+"\">"+
				"<v>"+deger.ToString(CultureInfo.InvariantCulture)+"</v></c>";
		}

		private string ExcelHucreReferansiGetir ( int sutunNo , int satirNo )
		{
			return ExcelSutunHarfiGetir(sutunNo)+satirNo.ToString(CultureInfo.InvariantCulture);
		}

		private string ExcelSutunHarfiGetir ( int sutunNo )
		{
			StringBuilder sonuc = new StringBuilder();
			int kalan = sutunNo;
			while(kalan>0)
			{
				int mod = ( kalan-1 )%26;
				sonuc.Insert(0 , ( char )( 'A'+mod ));
				kalan=( kalan-mod-1 )/26;
			}

			return sonuc.ToString();
		}

		private string ExcelXmlKacis ( string metin )
		{
			return ( metin??string.Empty )
				.Replace("&" , "&amp;")
				.Replace("<" , "&lt;")
				.Replace(">" , "&gt;")
				.Replace("\"" , "&quot;");
		}
	}
}
