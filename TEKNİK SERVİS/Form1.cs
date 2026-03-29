using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb; // Access için
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	public partial class Form1 : Form
	{
		private object tabControlInside;
		private string connStr;
		private OleDbConnection baglanti;
		private DataGridView dgvKategoriYonetim;
		private DataGridView dgvMarkaYonetim;
		private TextBox txtKategoriIdYonetim;
		private TextBox txtKategoriAdiYonetim;
		private TextBox txtMarkaIdYonetim;
		private TextBox txtMarkaAdiYonetim;
		private bool _markaFiltreleniyor;
		private bool _cariDurumCariTipKolonuVar;
		private bool _faturaCariTipKolonuVar;
		private ComboBox comboBox12;
		private ComboBox _sepetCariComboBox;
		private ComboBox _sepetUrunComboBox;
		private ComboBox _sepetYapilanIsComboBox;
		private int? _sepetCariId;
		private int? _sepetUrunId;
		private int? _sepetYapilanIsId;
		private string _sepetMarka;
		private string _sepetKategori;
		private string _sepetBirim;
		private bool _sepetCariDolduruluyor;
		private bool _sepetUrunDolduruluyor;
		private bool _sepetYapilanIsDolduruluyor;
		private bool _sepetHesaplanıyor;
		private bool _sepetSatirSeciliyor;
		private bool _yapilanIsAlanlariGuncelleniyor;
		private const string SepetCariUyariMetni = "Teklifse cari bilgisi girmeyiniz";
		private const string AramaPlaceholderMetni = "Ara";
		private const string VarsayilanYapilanIsBirimi = "ADET";
		private const decimal AnaSayfaKritikStokEsigi = 10m;
		private readonly Dictionary<TextBox, DataGridView> _aramaKutusuGridEslesmeleri = new Dictionary<TextBox, DataGridView>();
		private enum BelgeKayitTuru
		{
			FabrikaFaturasi,
			SucuFaturasi,
			MusteriFaturasi,
			Teklif
		}

		private sealed class BelgePaneli
		{
			public BelgeKayitTuru Tur;
			public string CariTipAdi;
			public int? CariTipId;
			public DataGridView UstGrid;
			public DataGridView AltGrid;
			public TextBox AramaKutusu;
			public TextBox CariAdTextBox;
			public ComboBox CariAdComboBox;
			public TextBox CariTcTextBox;
			public TextBox CariTelefonTextBox;
			public TextBox UrunAdiTextBox;
			public ComboBox UrunAdiComboBox;
			public TextBox BirimTextBox;
			public TextBox MiktarTextBox;
			public TextBox BirimFiyatTextBox;
			public TextBox ToplamFiyatTextBox;
			public TextBox[] ArizaTextBoxlari;
			public Label[] ArizaLabellari;
			public ComboBox YapilanIsComboBox;
			public TextBox YapilanIsBilgiTextBox;
			public TextBox YapilanIsAdetTextBox;
			public TextBox YapilanIsFiyatTextBox;
			public Button KaydetButonu;
			public Button SatirSilButonu;
			public Button KayitSilButonu;
			public Button AktarButonu;
			public Button GuncelleButonu;
			public Button YazdirButonu;
			public Button PdfButonu;
			public Button ExcelButonu;
			public Control OzetHost;
			public Label ToplamLabel;
			public TextBox KdvTextBox;
			public Label TotalLabel;
			public decimal HeaderToplamTutar;
			public bool OzetBilgisiGuncelleniyor;
			public int? SeciliKayitId;
			public int? SeciliDetayId;
			public int? SeciliCariId;
			public int? SeciliYapilanIsId;

			public bool TeklifMi
			{
				get { return Tur==BelgeKayitTuru.Teklif; }
			}

			public string KayitIdKolonu
			{
				get { return TeklifMi ? "TeklifID" : "FaturaID"; }
			}

			public string DetayIdKolonu
			{
				get { return TeklifMi ? "TeklifDetayID" : "FaturaDetayID"; }
			}
		}

		private sealed class ArizaKaynakBilgisi
		{
			public string TabloAdi;
			public string IliskiKolonu;
			public string SiralamaKolonu;
			public List<string> ArizaKolonlari = new List<string>();
		}

		private sealed class BelgeYazdirmaSatiri
		{
			public string UrunAdi;
			public string Birim;
			public decimal Miktar;
			public decimal BirimFiyat;
			public decimal ToplamTutar;
		}

		private sealed class BelgeYazdirmaVerisi
		{
			public string BelgeBasligi;
			public string BelgeNo;
			public int? BelgeSiraNo;
			public string SatirListesiBasligi;
			public DateTime? Tarih;
			public string CariAdi;
			public string CariTelefon;
			public List<string> YapilanIsler = new List<string>();
			public List<string> DipnotSatirlari = new List<string>();
			public List<BelgeYazdirmaSatiri> Satirlar = new List<BelgeYazdirmaSatiri>();
			public decimal AraToplam;
			public decimal KdvTutari;
			public decimal GenelToplam;
		}

		private sealed class UrunAramaKaydi
		{
			public int UrunId;
			public string UrunAdi;
			public string BirimAdi;
			public string KategoriAdi;
			public string MarkaAdi;
			public string UrunGosterimAdi
			{
				get
				{
					string temizUrunAdi = ( UrunAdi??string.Empty ).Trim();
					string temizMarkaAdi = ( MarkaAdi??string.Empty ).Trim();
					if(string.IsNullOrWhiteSpace(temizMarkaAdi))
						return temizUrunAdi;
					if(string.IsNullOrWhiteSpace(temizUrunAdi))
						return temizMarkaAdi;
					return temizUrunAdi+" - "+temizMarkaAdi;
				}
			}
		}

		private sealed class CariAramaKaydi
		{
			public int CariId;
			public string AdSoyad;
			public string Tc;
			public string Telefon;
			public int? CariTipId;
			public string TipAdi;
			public string CariGosterimAdi
			{
				get { return AdSoyad??string.Empty; }
			}

			public string CariGosterimDetayi
			{
				get
				{
					string adSoyad = AdSoyad??string.Empty;
					string tipAdi = TipAdi??string.Empty;
					if(string.IsNullOrWhiteSpace(tipAdi))
						return adSoyad;

					return string.IsNullOrWhiteSpace(adSoyad)
						? "["+tipAdi.Trim()+"]"
						: adSoyad+" ["+tipAdi.Trim()+"]";
				}
			}
		}

		private sealed class StokKalemBilgisi
		{
			public int UrunId;
			public string UrunAdi;
			public decimal Miktar;
		}

		private sealed class YapilanIsKaydi
		{
			public int YapilanIsId;
			public string IsBilgisi;
			public string IsAdi;
			public string Birim;
			public decimal Adet;
			public decimal Miktar;
			public decimal Fiyat;
			public decimal ToplamFiyat;
			public string KalemGosterimAdi
			{
				get
				{
					string temizIsAdi = ( IsAdi??string.Empty ).Trim();
					if(!string.IsNullOrWhiteSpace(temizIsAdi))
						return temizIsAdi;

					return ( IsBilgisi??string.Empty ).Trim();
				}
			}
		}

		private sealed class KalemSecimBilgisi
		{
			public bool YapilanIsMi;
			public int? UrunId;
			public int? YapilanIsId;
			public string KalemAdi;
			public string Birim;
			public string IsBilgisi;
			public decimal Adet;
			public decimal Miktar;
			public decimal BirimFiyat;
		}

		private string CariGosterimDetayMetniOlustur ( string adSoyad , string tipAdi )
		{
			string temizAdSoyad = ( adSoyad??string.Empty ).Trim();
			string temizTipAdi = ( tipAdi??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizTipAdi))
				return temizAdSoyad;

			return string.IsNullOrWhiteSpace(temizAdSoyad)
				? "["+temizTipAdi+"]"
				: temizAdSoyad+" ["+temizTipAdi+"]";
		}

		private string CariAramaMetniniTemizle ( string metin )
		{
			string temizMetin = ( metin??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizMetin))
				return string.Empty;

			int acilanParantez = temizMetin.LastIndexOf('[');
			int kapananParantez = temizMetin.LastIndexOf(']');
			if(acilanParantez>0&&kapananParantez==temizMetin.Length-1&&acilanParantez<kapananParantez)
				temizMetin=temizMetin.Substring(0 , acilanParantez).TrimEnd();

			return temizMetin.Trim();
		}

		private string CariAramaMetnindenTipAdiGetir ( string metin )
		{
			string temizMetin = ( metin??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizMetin)||!temizMetin.EndsWith("]" , StringComparison.Ordinal))
				return string.Empty;

			int acilanParantez = temizMetin.LastIndexOf('[');
			int kapananParantez = temizMetin.LastIndexOf(']');
			if(acilanParantez<0||kapananParantez<=acilanParantez)
				return string.Empty;

			return temizMetin.Substring(acilanParantez+1 , kapananParantez-acilanParantez-1).Trim();
		}

		private string CariTipAdiGetir ( int? cariTipId )
		{
			if(!cariTipId.HasValue||cariTipId.Value<=0)
				return string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 TipAdi FROM CariTipi WHERE CariTipID=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipId.Value;
						object sonuc = cmd.ExecuteScalar();
						return sonuc==null||sonuc==DBNull.Value ? string.Empty : Convert.ToString(sonuc)??string.Empty;
					}
				}
			}
			catch
			{
				return string.Empty;
			}
		}

		private int? CariTipIdGetir ( OleDbConnection conn , OleDbTransaction tx , string tipAdi )
		{
			string temizTipAdi = ( tipAdi??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizTipAdi)||conn==null)
				return null;

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand("SELECT TOP 1 CariTipID FROM CariTipi WHERE UCASE(IIF(TipAdi IS NULL, '', TipAdi))=?" , conn)
				: new OleDbCommand("SELECT TOP 1 CariTipID FROM CariTipi WHERE UCASE(IIF(TipAdi IS NULL, '', TipAdi))=?" , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , temizTipAdi.ToUpper(new CultureInfo("tr-TR")));
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? ( int? )null : Convert.ToInt32(sonuc);
			}
		}

		private readonly Dictionary<BelgeKayitTuru, BelgePaneli> _belgePanelleri = new Dictionary<BelgeKayitTuru, BelgePaneli>();
		private readonly List<ArizaKaynakBilgisi> _arizaKaynaklari = new List<ArizaKaynakBilgisi>();
		private readonly CultureInfo _yazdirmaKulturu = new CultureInfo("tr-TR");
		private const int PersonelMaasDonemBaslangicGunu = 15;
		private const string YazdirmaSirketAdi = "ASLAN SIHHI TESISAT";
		private const string YazdirmaSirketAdres = "Manisa / Turgutlu";
		private const string YazdirmaSirketTelefon = "Tel: 05300000000";
		private static readonly string[] YazdirmaDipnotSatirlari =
		{
			"NOT: ASLAN SIHHİ TESİSATI TERCİH ETTİĞİNİZ İÇİN TEŞEKKÜR EDERİZ",
			"GARANTİ BANKASI IBAN: TR97 0006 2000 5450 0006 6565 61",
			"ZİRAAT BANKASI IBAN: TR64 0001 0001 9580 4252 4250 01"
		};
		private Image _yazdirmaLogoGorseli;
		private BelgeYazdirmaVerisi _aktifBelgeYazdirmaVerisi;
		private int _aktifBelgeYazdirmaSatirIndex;
		private int _aktifBelgeYazdirmaSayfaNo;
		private bool _belgePanelleriHazir;
		private bool _belgeAlanlariGuncelleniyor;
		private bool _arizaKaynaklariArastirildi;
		private bool _personelDurumKolonuVar;
		private bool _departmanMaasKolonuVar;
		private bool _personelOdemeTablosuVar;
		private bool _personelMaasDonemTablosuVar;
		private bool _toptanciTablosuVar;
		private bool _toptanciAlimTablosuVar;
		private bool _toptanciOdemeTablosuVar;
		private bool _toptanciAdiKolonuVar;
		private bool _toptanciDurumKolonuVar;
		private bool _toptanciDurumKolonuMantiksal;
		private bool _toptanciDurumMetniKolonuVar;
		private bool _personelFormYukleniyor;
		private bool _personelBakiyeSecimYukleniyor;
		private bool _personelBakiyeOdemeYukleniyor;
		private bool _toptanciFormYukleniyor;
		private bool _toptanciBakiyeSecimYukleniyor;
		private DataGridView _departmanYonetimGrid;
		private ComboBox _personelBakiyeSecimComboBox;
		private Label _personelBakiyeSecimLabel;
		private Label _personelBakiyeDonemLabel;
		private Label _personelBakiyeTarihLabel;
		private DateTimePicker _personelBakiyeTarihPicker;
		private TableLayoutPanel _personelBakiyeAksiyonPaneli;
		private Button _personelOdemeEkleButonu;
		private Button _personelOdemeGuncelleButonu;
		private Button _personelOdemeSilButonu;
		private Label _toptanciBakiyeSecimLabel;
		private ComboBox _toptanciBakiyeSecimComboBox;
		private Label _toptanciTarihLabel;
		private DateTimePicker _toptanciTarihPicker;
		private TableLayoutPanel _toptanciBakiyeAksiyonPaneli;
		private Button _toptanciBakiyeGuncelleButonu;
		private Button _toptanciBakiyeSilButonu;
		private Button _toptanciBakiyeYazdirButonu;
		private Button _toptanciBakiyeExcelButonu;
		private Button _toptanciBakiyePdfButonu;
		private int? _toptanciSeciliHareketId;
		private string _toptanciSeciliHareketTuru;
		private int? _seciliNotId;
		private bool _notSecimiYukleniyor;
		private TextBox _notBekleyenAramaKutusu;
		private TextBox _notOkunanAramaKutusu;
		private Label _notBekleyenOzetDegerLabel;
		private Label _notOkunanOzetDegerLabel;
		private Label _notToplamOzetDegerLabel;
		private Label _notSonGuncellemeOzetDegerLabel;
		private TableLayoutPanel _notDetayLayout;
		private TabPage _yapilanIsTabPage;
		private GroupBox _yapilanIsKokGroupBox;
		private GroupBox _yapilanIsListeGroupBox;
		private GroupBox _yapilanIsDetayGroupBox;
		private DataGridView _yapilanIsGrid;
		private TextBox _yapilanIsAramaTextBox;
		private TextBox _yapilanIsIdTextBox;
		private TextBox _yapilanIsBilgiTextBox;
		private TextBox _yapilanIsAdiTextBox;
		private TextBox _yapilanIsBirimTextBox;
		private TextBox _yapilanIsAdetTextBox;
		private TextBox _yapilanIsMiktarTextBox;
		private TextBox _yapilanIsFiyatTextBox;
		private TextBox _yapilanIsToplamTextBox;
		private Button _yapilanIsKaydetButonu;
		private Button _yapilanIsGuncelleButonu;
		private Button _yapilanIsSilButonu;
		private Button _yapilanIsTemizleButonu;
		private Label _yapilanIsToplamLabel;
		private Label _yapilanIsOrtalamaLabel;
		private Label _yapilanIsSonKayitLabel;
		private int? _seciliYapilanIsYonetimId;
		private int? _seciliPersonelOdemeId;
		private int? _seciliPersonelOdemeDonemId;
		private decimal _seciliPersonelOdemeTutari;
		private TextBox _departmanYonetimIdTextBox;
		private TextBox _departmanYonetimAdiTextBox;
		private TextBox _departmanYonetimMaasTextBox;
		private static readonly string[] OrtakGorselAnahtarlari =
		{
			"Close.png" ,
			"Check Mark.png" ,
			"Cancel.png" ,
			"Denied.png" ,
			"Denied.png" ,
			"Add User Male.png" ,
			"Add Male User Group.png" ,
			"lll.png" ,
			"Update User.png" ,
			"Add New.png" ,
			"Add Shopping Cart.png" ,
			"Buy.png" ,
			"Clear Shopping Cart.png" ,
			"Erase.png" ,
			"Clear Search.png" ,
			"Broom.png" ,
			"Print.png" ,
			"PDF.png" ,
			"Microsoft Excel.png" ,
			"Save.png" ,
			"Delete Column.png" ,
			"Renew.png" ,
			"ID not Verified.png" ,
			"Edit.png" ,
			"Delete Database.png" ,
			"Delete File.png" ,
			"Restart.png" ,
			"Volunteering.png" ,
			"Available Updates.png" ,
			"Downloading Updates.png" ,
			"Multiply.png" ,
			"Remove Bookmark.png" ,
			"Bookmark.png" ,
			"Synchronize.png"
		};

		static Form1 ()
		{
			if(TasarimciIslemindeCalisiyorMu())
				return;

			AppDomain.CurrentDomain.AssemblyResolve+=Form1AssemblyResolve;
		}

		public Form1 ()
		{
			InitializeComponent();
			if(TasarimModundaCalisiyorMu())
			{
				GunlukSatisTasarimYuzeyiniHazirla();
				return;
			}

			OrtakGorselListesiniHazirla();
			VarsayilanButonIkonlariniUygula();
			// ÖNEMLİ: Jet.OLEDB.4.0 yerine ACE.OLEDB.12.0 kullanmalısınız
			// Uygulama ayarlarından gelen bağlantıyı tek yerde tanımla (field'i doldur).
			connStr=TEKNİK_SERVİS.Properties.Settings.Default.TeknikServisSConnectionString;
			baglanti=new OleDbConnection(connStr);
		}

		private static Assembly Form1AssemblyResolve ( object sender , ResolveEventArgs e )
		{
			if(string.IsNullOrWhiteSpace(e?.Name))
				return null;

			AssemblyName istek = new AssemblyName(e.Name);
			Assembly yuklu = AppDomain.CurrentDomain
				.GetAssemblies()
				.FirstOrDefault(a => string.Equals(a.GetName().Name , istek.Name , StringComparison.OrdinalIgnoreCase));
			if(yuklu!=null)
				return yuklu;

			string dosyaAdi = istek.Name+".dll";
			string[] aramaDizinleri = new[]
			{
				Path.GetDirectoryName(typeof(Form1).Assembly.Location) ,
				AppDomain.CurrentDomain.BaseDirectory
			}
			.Where(dizin => !string.IsNullOrWhiteSpace(dizin))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

			foreach(string dizin in aramaDizinleri)
			{
				string adayYol = Path.Combine(dizin , dosyaAdi);
				if(File.Exists(adayYol))
					return Assembly.LoadFrom(adayYol);
			}

			string[] yedekYollar = Array.Empty<string>();
			if(string.Equals(istek.Name , "System.Resources.Extensions" , StringComparison.OrdinalIgnoreCase))
			{
				yedekYollar=new[]
				{
					@"C:\Program Files\Microsoft SQL Server Management Studio 22\Release\MSBuild\Current\Bin\System.Resources.Extensions.dll" ,
					@"C:\Program Files\dotnet\sdk\10.0.200\System.Resources.Extensions.dll"
				};
			}
			else if(string.Equals(istek.Name , "System.Memory" , StringComparison.OrdinalIgnoreCase))
			{
				yedekYollar=new[]
				{
					@"C:\Program Files\dotnet\sdk\10.0.200\TestHostNetFramework\System.Memory.dll" ,
					@"C:\Program Files (x86)\IIS Express\System.Memory.dll"
				};
			}
			else if(string.Equals(istek.Name , "System.Numerics.Vectors" , StringComparison.OrdinalIgnoreCase))
			{
				yedekYollar=new[]
				{
					@"C:\Program Files\dotnet\sdk\10.0.200\TestHostNetFramework\System.Numerics.Vectors.dll" ,
					@"C:\Program Files (x86)\IIS Express\System.Numerics.Vectors.dll"
				};
			}

			foreach(string adayYol in yedekYollar)
			{
				if(File.Exists(adayYol))
					return Assembly.LoadFrom(adayYol);
			}

			return null;
		}

		private void OrtakGorselListesiniHazirla ()
		{
			if(imageList1==null)
				return;

			imageList1.ColorDepth=ColorDepth.Depth32Bit;
			imageList1.ImageSize=new Size(45 , 45);
			imageList1.TransparentColor=Color.Transparent;
			imageList1.Images.Clear();

			OrtakGorselleriDosyalardanYukle();
			if(imageList1.Images.Count==0)
				OrtakGorselleriKaynaklardanYukle();

			OrtakGorselleriTamamla();
		}

		private bool OrtakGorselleriKaynaklardanYukle ()
		{
			if(imageList1==null)
				return false;

			try
			{
				if(imageList1.Images.Count==0)
				{
					ComponentResourceManager kaynakYoneticisi = new ComponentResourceManager(typeof(Form1));
					object imageStreamNesnesi = kaynakYoneticisi.GetObject("imageList1.ImageStream")??
						kaynakYoneticisi.GetObject("ımageList1.ImageStream");
					if(!(imageStreamNesnesi is ImageListStreamer imageStream))
						return false;

					imageList1.ImageStream=imageStream;
				}

				if(imageList1.Images.Count==0)
					return false;

				int anahtarAdedi = Math.Min(imageList1.Images.Count , OrtakGorselAnahtarlari.Length);
				for(int i = 0 ; i<anahtarAdedi ; i++)
					imageList1.Images.SetKeyName(i , OrtakGorselAnahtarlari[i]);

				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool OrtakGorselleriDosyalardanYukle ()
		{
			if(imageList1==null)
				return false;

			string[] adayDizinler = new[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "Resources" , "ButtonIcons") ,
				Path.Combine(Application.StartupPath , "Resources" , "ButtonIcons")
			}
			.Where(dizin => !string.IsNullOrWhiteSpace(dizin))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

			string kaynakDizin = adayDizinler.FirstOrDefault(Directory.Exists);
			if(string.IsNullOrWhiteSpace(kaynakDizin))
				return false;

			bool gorselYuklendi = false;
			foreach(string anahtar in OrtakGorselAnahtarlari)
			{
				string dosyaYolu = Path.Combine(kaynakDizin , anahtar);
				if(!File.Exists(dosyaYolu))
					continue;

				using(FileStream akim = new FileStream(dosyaYolu , FileMode.Open , FileAccess.Read , FileShare.Read))
				using(Image gorsel = Image.FromStream(akim))
				{
					int index = imageList1.Images.Count;
					imageList1.Images.Add((Image) gorsel.Clone());
					imageList1.Images.SetKeyName(index , anahtar);
				}

				gorselYuklendi=true;
			}

			return gorselYuklendi;
		}

		private void OrtakGorselleriTamamla ()
		{
			OrtakGorselEkleEksikse("Close.png" , "X" , Color.FromArgb(239 , 68 , 68));
			OrtakGorselEkleEksikse("Check Mark.png" , "OK" , Color.FromArgb(34 , 197 , 94));
			OrtakGorselEkleEksikse("Cancel.png" , "IP" , Color.FromArgb(245 , 158 , 11));
			OrtakGorselEkleEksikse("Denied.png" , "!" , Color.FromArgb(220 , 38 , 38));
			OrtakGorselEkleEksikse("Add User Male.png" , "K+" , Color.FromArgb(59 , 130 , 246));
			OrtakGorselEkleEksikse("Add Male User Group.png" , "G+" , Color.FromArgb(99 , 102 , 241));
			OrtakGorselEkleEksikse("lll.png" , "+" , Color.FromArgb(20 , 184 , 166));
			OrtakGorselEkleEksikse("Update User.png" , "GN" , Color.FromArgb(14 , 165 , 233));
			OrtakGorselEkleEksikse("Add New.png" , "Y+" , Color.FromArgb(22 , 163 , 74));
			OrtakGorselEkleEksikse("Add Shopping Cart.png" , "SP" , Color.FromArgb(8 , 145 , 178));
			OrtakGorselEkleEksikse("Buy.png" , "AL" , Color.FromArgb(16 , 185 , 129));
			OrtakGorselEkleEksikse("Clear Shopping Cart.png" , "SX" , Color.FromArgb(249 , 115 , 22));
			OrtakGorselEkleEksikse("Erase.png" , "TM" , Color.FromArgb(107 , 114 , 128));
			OrtakGorselEkleEksikse("Clear Search.png" , "AX" , Color.FromArgb(244 , 63 , 94));
			OrtakGorselEkleEksikse("Broom.png" , "BR" , Color.FromArgb(139 , 92 , 246));
			OrtakGorselEkleEksikse("Print.png" , "YZ" , Color.FromArgb(59 , 130 , 246));
			OrtakGorselEkleEksikse("PDF.png" , "PDF" , Color.FromArgb(220 , 38 , 38));
			OrtakGorselEkleEksikse("Microsoft Excel.png" , "XL" , Color.FromArgb(22 , 163 , 74));
			OrtakGorselEkleEksikse("Save.png" , "KY" , Color.FromArgb(37 , 99 , 235));
			OrtakGorselEkleEksikse("Delete Column.png" , "SL" , Color.FromArgb(190 , 24 , 93));
			OrtakGorselEkleEksikse("Renew.png" , "YN" , Color.FromArgb(249 , 115 , 22));
			OrtakGorselEkleEksikse("ID not Verified.png" , "ID" , Color.FromArgb(124 , 58 , 237));
			OrtakGorselEkleEksikse("Edit.png" , "DZ" , Color.FromArgb(14 , 116 , 144));
			OrtakGorselEkleEksikse("Delete Database.png" , "VT" , Color.FromArgb(185 , 28 , 28));
			OrtakGorselEkleEksikse("Delete File.png" , "DS" , Color.FromArgb(190 , 24 , 93));
			OrtakGorselEkleEksikse("Restart.png" , "RB" , Color.FromArgb(79 , 70 , 229));
			OrtakGorselEkleEksikse("Volunteering.png" , "TZ" , Color.FromArgb(217 , 119 , 6));
			OrtakGorselEkleEksikse("Available Updates.png" , "GU" , Color.FromArgb(5 , 150 , 105));
			OrtakGorselEkleEksikse("Downloading Updates.png" , "IN" , Color.FromArgb(2 , 132 , 199));
			OrtakGorselEkleEksikse("Multiply.png" , "X" , Color.FromArgb(225 , 29 , 72));
			OrtakGorselEkleEksikse("Remove Bookmark.png" , "BX" , Color.FromArgb(219 , 39 , 119));
			OrtakGorselEkleEksikse("Bookmark.png" , "BM" , Color.FromArgb(14 , 165 , 233));
			OrtakGorselEkleEksikse("Synchronize.png" , "SY" , Color.FromArgb(6 , 182 , 212));
		}

		private void OrtakGorselEkleEksikse ( string anahtar , string metin , Color arkaPlan )
		{
			if(imageList1==null||string.IsNullOrWhiteSpace(anahtar))
				return;

			if(imageList1.Images.ContainsKey(anahtar))
				return;

			OrtakGorselEkle(anahtar , metin , arkaPlan);
		}

		private void VarsayilanButonIkonlariniUygula ()
		{
			ButonIkonunuBagla(button28 , "Delete File.png");
			ButonIkonunuBagla(button12 , "Add Shopping Cart.png");
			ButonIkonunuBagla(button15 , "Delete Database.png");
			ButonIkonunuBagla(button37 , "Add Shopping Cart.png");
			ButonIkonunuBagla(button39 , "Delete Database.png");
			ButonIkonunuBagla(button60 , "Add Shopping Cart.png");
			ButonIkonunuBagla(button62 , "Delete Database.png");
			ButonIkonunuBagla(button70 , "Add Shopping Cart.png");
			ButonIkonunuBagla(button72 , "Delete Database.png");
			ButonIkonunuBagla(button74 , "Update User.png");
			ButonIkonunuBagla(button24 , "Add Shopping Cart.png");
			ButonIkonunuBagla(button66 , "Delete Database.png");
			ButonIkonunuBagla(button32 , "Delete Database.png");
			ButonIkonunuBagla(button8 , "Delete Database.png");
			ButonIkonunuBagla(button44 , "Delete Database.png");
			ButonIkonunuBagla(button49 , "Delete Database.png");
			ButonIkonunuBagla(btnCariSil , "Denied.png");
			ButonIkonunuBagla(button6 , "Denied.png");
			ButonIkonunuBagla(button20 , "Denied.png");
			ButonIkonunuBagla(button52 , "Denied.png");
			ButonIkonunuBagla(button56 , "Denied.png");
			ButonIkonunuBagla(button78 , "Denied.png");
			ButonIkonunuBagla(button33 , "Save.png");
			ButonIkonunuBagla(button31 , "Update User.png");
			ButonIkonunuBagla(button79 , "Add User Male.png");
			ButonIkonunuBagla(button77 , "Update User.png");
			MetinButonIkonunuBagla(button75 , "Save.png");
			MetinButonIkonunuBagla(button80 , "Save.png");
		}

		private void ButonIkonunuBagla ( Button buton , string imageKey )
		{
			if(buton==null||imageList1==null||string.IsNullOrWhiteSpace(imageKey))
				return;

			buton.Image=null;
			buton.ImageList=imageList1;
			buton.ImageIndex=-1;
			buton.ImageKey=imageKey;
		}

		private void MetinButonIkonunuBagla ( Button buton , string imageKey )
		{
			ButonIkonunuBagla(buton , imageKey);
			if(buton==null)
				return;

			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			if(buton.Padding==Padding.Empty)
				buton.Padding=new Padding(10 , 0 , 10 , 0);
		}

		private void OrtakGorselEkle ( string anahtar , string metin , Color arkaPlan )
		{
			if(imageList1==null)
				return;

			int index = imageList1.Images.Count;
			imageList1.Images.Add(OrtakGorselOlustur(metin , arkaPlan));
			imageList1.Images.SetKeyName(index , anahtar);
		}

		private Bitmap OrtakGorselOlustur ( string metin , Color arkaPlan )
		{
			Size boyut = imageList1!=null&&imageList1.ImageSize.Width>0&&imageList1.ImageSize.Height>0
				? imageList1.ImageSize
				: new Size(45 , 45);
			string gorselMetni = string.IsNullOrWhiteSpace(metin) ? "?" : metin.Trim().ToUpperInvariant();
			float fontBoyutu = gorselMetni.Length>=3 ? 6.3F : gorselMetni.Length==2 ? 7.4F : 11F;
			int aydinlik = (arkaPlan.R*299+arkaPlan.G*587+arkaPlan.B*114)/1000;
			Color yaziRengi = aydinlik>=165 ? Color.FromArgb(15 , 23 , 42) : Color.White;
			Bitmap bitmap = new Bitmap(boyut.Width , boyut.Height , PixelFormat.Format32bppArgb);
			Rectangle cizimAlani = new Rectangle(0 , 0 , boyut.Width-1 , boyut.Height-1);

			using(Graphics grafik = Graphics.FromImage(bitmap))
			using(GraphicsPath yol = YuvarlatilmisDikdortgenOlustur(cizimAlani , 7))
			using(SolidBrush arkaPlanFircasi = new SolidBrush(arkaPlan))
			using(Pen kenarKalemi = new Pen(Color.FromArgb(40 , Color.Black)))
			using(Font yaziTipi = new Font("Segoe UI" , fontBoyutu , FontStyle.Bold , GraphicsUnit.Point))
			{
				grafik.Clear(Color.Transparent);
				grafik.SmoothingMode=SmoothingMode.AntiAlias;
				grafik.InterpolationMode=InterpolationMode.HighQualityBicubic;
				grafik.PixelOffsetMode=PixelOffsetMode.HighQuality;
				grafik.FillPath(arkaPlanFircasi , yol);
				grafik.DrawPath(kenarKalemi , yol);

				TextRenderer.DrawText(
					grafik ,
					gorselMetni ,
					yaziTipi ,
					cizimAlani ,
					yaziRengi ,
					TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
			}

			return bitmap;
		}

		private GraphicsPath YuvarlatilmisDikdortgenOlustur ( Rectangle alan , int yaricap )
		{
			GraphicsPath yol = new GraphicsPath();
			int cap = Math.Max(1 , yaricap*2);

			yol.AddArc(alan.X , alan.Y , cap , cap , 180 , 90);
			yol.AddArc(alan.Right-cap , alan.Y , cap , cap , 270 , 90);
			yol.AddArc(alan.Right-cap , alan.Bottom-cap , cap , cap , 0 , 90);
			yol.AddArc(alan.X , alan.Bottom-cap , cap , cap , 90 , 90);
			yol.CloseFigure();
			return yol;
		}

		private void Form1_Load ( object sender , EventArgs e )
		{
			SatisUrunleriniYenile();
			FormuTamamenTemizle	();
			Temizle4();
			Temizle3();
			// İlk başta textboxları temizle
			Temizle();
			EnsureCariDurumAltyapi();
			EnsureFaturaCariTipAltyapi();
			EnsureBelgeArizaAltyapi();
			EnsureYapilanIsAltyapi();
			EnsurePersonelAltyapi();
			EnsureToptanciAltyapi();
			EnsureCariHesapAltyapi();
			EnsureNotAltyapi();
			EnsureGunlukSatisAltyapi();
			EnsureCariTipVeDurumVerileri();
			EnsureDepartmanVePersonelVerileri();
			//ürün işlemleri
			//picturebox sayı
			label97.Parent=pictureBox8;
			label28.Parent=pictureBox7;
			label21.Parent=pictureBox6;
			label19.Parent=pictureBox5;
			//cari
			label111.Parent=pictureBox12;
			label109.Parent=pictureBox11;
			label107.Parent=pictureBox10;
			label105.Parent=pictureBox9;
			//toptanci
			label228.Parent=pictureBox16;
			label229.Parent=pictureBox16;
			label225.Parent=pictureBox15;
			label226.Parent=pictureBox15;
			label223.Parent=pictureBox14;
			label224.Parent=pictureBox14;
			label221.Parent=pictureBox13;
			label222.Parent=pictureBox13;
			//personel
			label92.Parent=pictureBox1;
			label96.Parent=pictureBox2;
			label136.Parent=pictureBox3;
			label137.Parent=pictureBox4;

			//ürün
			//label başlık
			label98.Parent=pictureBox8;
			label29.Parent=pictureBox7;
			label22.Parent=pictureBox6;
			label18.Parent=pictureBox5;
			//cari
			label112.Parent=pictureBox12;
			label110.Parent=pictureBox11;
			label108.Parent=pictureBox10;
			label114.Parent=pictureBox9;
			//personel
			label93.Parent=pictureBox1;
			label95.Parent=pictureBox2;
			label134.Parent=pictureBox3;
			label135.Parent=pictureBox4;


			label97.BackColor=Color.Transparent;
			label28.BackColor=Color.Transparent;
			label21.BackColor=Color.Transparent;
			label19.BackColor=Color.Transparent;
			label98.BackColor=Color.Transparent;
			label29.BackColor=Color.Transparent;
			label22.BackColor=Color.Transparent;
			label18.BackColor=Color.Transparent;

			label92.BackColor=Color.Transparent;
			label96.BackColor=Color.Transparent;
			label136.BackColor=Color.Transparent;
			label137.BackColor=Color.Transparent;
			label93.BackColor=Color.Transparent;
			label95.BackColor=Color.Transparent;
			label134.BackColor=Color.Transparent;
			label135.BackColor=Color.Transparent;


			label111.BackColor=Color.Transparent;
			label109.BackColor=Color.Transparent;
			label107.BackColor=Color.Transparent;
			label105.BackColor=Color.Transparent;
			label112.BackColor=Color.Transparent;
			label110.BackColor=Color.Transparent;
			label108.BackColor=Color.Transparent;
			label114.BackColor=Color.Transparent;
			label228.BackColor=Color.Transparent;
			label229.BackColor=Color.Transparent;
			label225.BackColor=Color.Transparent;
			label226.BackColor=Color.Transparent;
			label223.BackColor=Color.Transparent;
			label224.BackColor=Color.Transparent;
			label221.BackColor=Color.Transparent;
			label222.BackColor=Color.Transparent;


			//öne gelmesi içim
			//label98.BringToFront();
			//label97.BringToFront();





			//personel işlemleri
			//picturebox sayı
			label93.Parent=pictureBox1;
			//label başlık
			label92.Parent=pictureBox1;

			label93.BackColor=Color.Transparent;
			label92.BackColor=Color.Transparent;


			//öne gelmesi içim
			label93.BringToFront();
			label92.BringToFront();
			//resimlerr picturebox
			pictureBox1.Paint+=pictureBox1_Paint_1;
			pictureBox2.Paint+=pictureBox2_Paint;
			pictureBox3.Paint+=pictureBox3_Paint;
			pictureBox4.Paint+=pictureBox4_Paint;
			pictureBox5.Paint+=pictureBox5_Paint;
			pictureBox6.Paint+=pictureBox6_Paint;
			pictureBox7.Paint+=pictureBox7_Paint;
			pictureBox8.Paint+=pictureBox8_Paint;
			pictureBox12.Paint+=pictureBox12_Paint;
			pictureBox11.Paint+=pictureBox11_Paint;
			pictureBox10.Paint+=pictureBox10_Paint;
			pictureBox9.Paint+=pictureBox9_Paint;
			pictureBox13.Paint+=ToptanciKartPictureBox_Paint;
			pictureBox14.Paint+=ToptanciKartPictureBox_Paint;
			pictureBox15.Paint+=ToptanciKartPictureBox_Paint;
			pictureBox16.Paint+=ToptanciKartPictureBox_Paint;
			//
			//

			//
			//

			this.Shown+=Form1_Shown;

			base.OnShown(e);

			// Ekran çözünürlüğünü al
			Rectangle screen = Screen.PrimaryScreen.WorkingArea;

			// Formu ekranın tamamını kaplayacak şekilde ayarla
			this.Location=new Point(0 , 0);
			this.Size=new Size(screen.Width , screen.Height);

			// TabControl veya ana panel dock fill yap
			tabControl1.Dock=DockStyle.Fill;
			//InitializeComponent();
			// Tüm sütunlar genişliği eşit paylaşarak toplam alanı doldurur
			DatagridviewSetting(dataGridView6);
			DatagridviewSetting(dataGridView4);
			DatagridviewSetting(dataGridView5);
			DatagridviewSetting(dataGridView18);
			DatagridviewSetting(dataGridView3);
			DatagridviewSetting(dataGridView2);
			DatagridviewSetting(dataGridView1);
			DatagridviewSetting(dataGridView9);
			DatagridviewSetting(dataGridView10);
			DatagridviewSetting(dataGridView11);
			DatagridviewSetting(dataGridView12);
			DatagridviewSetting(dataGridView13);
			DatagridviewSetting(dataGridView25);
			if(dataGridView25!=null)
				dataGridView25.MultiSelect=false;
			//urunler
			dataGridView1.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
			dataGridView1.MultiSelect=true;
			
			//CARİ
			DoldurComboBox(comboBox14 ,
"SELECT KategoriID, KategoriAdi FROM Kategoriler" ,
"KategoriAdi" ,
"KategoriID");
			EnsureCariDurumCariTipCombo();
			CariTipComboYenile();
			CariDurumComboYenile();
			cmbCariTip.SelectedIndexChanged-=CmbCariTip_SelectedIndexChanged;
			cmbCariTip.SelectedIndexChanged+=CmbCariTip_SelectedIndexChanged;
			DoldurComboBox(comboBox5 , "SELECT KategoriID, KategoriAdi FROM Kategoriler" , "KategoriAdi" , "KategoriID");
			DoldurComboBox(comboBox2 , "SELECT MarkaID, MarkaAdi  FROM Markalar" , "MarkaAdi" , "MarkaID");
			DoldurComboBox(comboBox3 , "SELECT BirimID, BirimAdi FROM Birimler" , "BirimAdi" , "BirimID");
			// Ürünleri ComboBox'a doldurma
			UrunSecimComboBoxiniDoldur(comboBox4 , false);
			//SATİSSS
			UrunSecimComboBoxiniDoldur(comboBox8 , true);
			DoldurComboBox(comboBox6 , "SELECT MarkaID, MarkaAdi FROM Markalar" , "MarkaAdi" , "MarkaID");
			DoldurComboBox(KategoriSec , "SELECT KategoriID, KategoriAdi FROM Kategoriler" , "KategoriAdi" , "KategoriID");
			MarkaKategoriUrunlerdenSenkronla();

			comboBox5.SelectedIndexChanged-=comboBox5_SelectedIndexChanged;
			comboBox5.SelectedIndexChanged+=comboBox5_SelectedIndexChanged;
			KategoriSec.SelectedIndexChanged-=KategoriSec_SelectedIndexChanged;
			KategoriSec.SelectedIndexChanged+=KategoriSec_SelectedIndexChanged;

			if(!ComboBoxMetniniSec(cmbCariTip , "Müşteri")&&cmbCariTip.Items.Count>0)
				cmbCariTip.SelectedIndex=0;
			CariDurumComboYenileByCariTip();
			textBox19.Clear();
			comboBox4.SelectedIndex=-1;
			Listele();
			Listele1();
			Listele3();
			Listele4();
			Listele5();
			Listele6();
			Listele7();
		

			textBox17.Text="0,00";
	
			// Sütunları ekle (Sütun Adı, Görünen Başlık)
			if(!dataGridView5.Columns.Contains("UrunID"))
			{
				var colId = new DataGridViewTextBoxColumn();
				colId.Name="UrunID";
				colId.HeaderText="URUN ID";
				colId.Visible=false;
				dataGridView5.Columns.Add(colId);
			}
			if(!dataGridView5.Columns.Contains("YapilanIsID"))
			{
				var colYapilanIsId = new DataGridViewTextBoxColumn();
				colYapilanIsId.Name="YapilanIsID";
				colYapilanIsId.HeaderText="YAPILAN İŞ ID";
				colYapilanIsId.Visible=false;
				dataGridView5.Columns.Add(colYapilanIsId);
			}
			if(!dataGridView5.Columns.Contains("KalemTuru"))
			{
				var colKalemTuru = new DataGridViewTextBoxColumn();
				colKalemTuru.Name="KalemTuru";
				colKalemTuru.HeaderText="KAYIT TÜRÜ";
				colKalemTuru.Visible=false;
				dataGridView5.Columns.Add(colKalemTuru);
			}
			if(!dataGridView5.Columns.Contains("IsBilgisi"))
			{
				var colIsBilgisi = new DataGridViewTextBoxColumn();
				colIsBilgisi.Name="IsBilgisi";
				colIsBilgisi.HeaderText="İŞ BİLGİSİ";
				colIsBilgisi.Visible=false;
				dataGridView5.Columns.Add(colIsBilgisi);
			}
			if(!dataGridView5.Columns.Contains("KalemAdet"))
			{
				var colKalemAdet = new DataGridViewTextBoxColumn();
				colKalemAdet.Name="KalemAdet";
				colKalemAdet.HeaderText="ADET";
				colKalemAdet.Visible=false;
				dataGridView5.Columns.Add(colKalemAdet);
			}
			if(!dataGridView5.Columns.Contains("urunadi"))
				dataGridView5.Columns.Add("urunadi" , "ÜRÜN ADI");
			if(!dataGridView5.Columns.Contains("marka"))
				dataGridView5.Columns.Add("marka" , "MARKA");
			if(!dataGridView5.Columns.Contains("kategori"))
				dataGridView5.Columns.Add("kategori" , "KATEGORİ");
			if(!dataGridView5.Columns.Contains("birim"))
				dataGridView5.Columns.Add("birim" , "BİRİM");
			if(!dataGridView5.Columns.Contains("adet"))
				dataGridView5.Columns.Add("adet" , "ADET"); // Adet sütununu unutma!
			if(!dataGridView5.Columns.Contains("SatisFiyati"))
				dataGridView5.Columns.Add("SatisFiyati" , "BİRİM FİYATI");
			if(!dataGridView5.Columns.Contains("toplamfiyat"))
				dataGridView5.Columns.Add("toplamfiyat" , "TOPLAM TUTAR");
			dataGridView5.Columns["SatisFiyati"].DefaultCellStyle.Format="C2"; // 10,00 ? şeklinde gösterir
			dataGridView5.Columns["toplamfiyat"].DefaultCellStyle.Format="C2";
			SepetBaslangicAyarla();
			UrunleriYenile();
			TumDataGridBasliklariniUygula();
			UrunYonetimSekmeleriniKur();
			KurYapilanIsSekmesi();
			KurKurlarSekmesi();
			KurDepartmanSekmesi();
			BelgePanelleriniHazirla();
			tabControl1.SelectedIndexChanged-=TabControl1_SelectedIndexChanged;
			tabControl1.SelectedIndexChanged+=TabControl1_SelectedIndexChanged;
			tabControl2.SelectedIndexChanged-=TabControl2_SelectedIndexChanged;
			tabControl2.SelectedIndexChanged+=TabControl2_SelectedIndexChanged;
			KurCariDurumSekmesi();
			BaglaCariTipIslemEventleri();
			BaglaCariDurumIslemEventleri();
			PersonelArayuzunuHazirla();
			BaglaPersonelIslemEventleri();
			KurNotlarSekmesi();
			BaglaNotIslemEventleri();
			NotFormunuTemizle();
			NotlariListele();
			PersonelListele();
			KurToptanciSekmesi();
			KurCariHesapSekmesi();
			KurGunlukSatisSekmesi();
			SatisUrunleriniAlisTablosundanGetir();
			AnaSayfaGridleriniYenile();
			AramaKutulariniHazirla();
			CariListeAramasiniHazirla();
			KartliListeAlanlariniHazirla();
			GunlukSatisVerileriniYenile();
			KullaniciOturumunuUygula();
		}

		private void AramaKutulariniHazirla ()
		{
			_aramaKutusuGridEslesmeleri.Clear();

			AramaKutusuHazirla(textBox1 , dataGridView1);
			AramaKutusuHazirla(textBox2 , dataGridView3);
			AramaKutusuHazirla(textBox38 , dataGridView18);
			AramaKutusuHazirla(textBox111 , dataGridView2);
			AramaKutusuHazirla(textBox31 , dataGridView5);

			AramaKutusuHazirla(textBox74);
			AramaKutusuHazirla(textBox77);
			AramaKutusuHazirla(textBox97);
			AramaKutusuHazirla(textBox98);
			CariTipVeDurumAramalariniKaldir();
		}

		private void CariTipVeDurumAramalariniKaldir ()
		{
			foreach(TextBox aramaKutusu in new[] { textBox15, textBox23 })
			{
				if(aramaKutusu==null)
					continue;

				_aramaKutusuGridEslesmeleri.Remove(aramaKutusu);
				aramaKutusu.TextChanged-=GenelAramaKutusu_TextChanged;
				aramaKutusu.Text=string.Empty;
				aramaKutusu.Visible=false;
				aramaKutusu.Enabled=false;
				aramaKutusu.TabStop=false;
			}
		}

		private void CariListeAramasiniHazirla ()
		{
			if(groupBox1==null)
				return;

			if(tabPage8!=null)
				tabPage8.Text="Cariler";
			if(groupBox1!=null)
				groupBox1.Text="Cari Listesi";
			if(groupBox2!=null)
				groupBox2.Text="Cari İşlemleri";

			if(textBox8!=null)
			{
				textBox8.Visible=false;
				textBox8.Enabled=false;
				textBox8.TabStop=false;
			}

			if(textBox111!=null)
			{
				textBox111.Visible=true;
				textBox111.Enabled=true;
				textBox111.TabStop=true;
				AramaKutusuGorunumunuUygula(textBox111);
			}
		}

		private void KartliListeAlanlariniHazirla ()
		{
			BaglaKartliListeYerlesimi(groupBox3 , groupBox6 , UrunListeYerlesiminiGuncelle);
			BaglaKartliListeYerlesimi(groupBox7 , groupBox37 , UrunAlisListeAramaYerlesiminiGuncelle);
			BaglaKartliListeYerlesimi(groupBox38 , groupBox40 , UrunSatisListeAramaYerlesiminiGuncelle);
			BaglaKartliListeYerlesimi(groupBox4 , groupBox1 , CariListeAramaYerlesiminiGuncelle);
			BaglaKartliListeYerlesimi(groupBox52 , groupBox54 , ToptanciListeYerlesiminiGuncelle);

			UrunListeYerlesiminiGuncelle(this , EventArgs.Empty);
			UrunAlisListeAramaYerlesiminiGuncelle(this , EventArgs.Empty);
			UrunSatisListeAramaYerlesiminiGuncelle(this , EventArgs.Empty);
			CariListeAramaYerlesiminiGuncelle(this , EventArgs.Empty);
			ToptanciListeYerlesiminiGuncelle(this , EventArgs.Empty);
		}

		private void BaglaKartliListeYerlesimi ( Control kokKapsayici , Control listeKapsayici , EventHandler yerlesimMetodu )
		{
			if(yerlesimMetodu==null)
				return;

			if(kokKapsayici!=null)
			{
				kokKapsayici.Resize-=yerlesimMetodu;
				kokKapsayici.Resize+=yerlesimMetodu;
			}

			if(listeKapsayici!=null)
			{
				listeKapsayici.Resize-=yerlesimMetodu;
				listeKapsayici.Resize+=yerlesimMetodu;
			}
		}

		private void UrunListeYerlesiminiGuncelle ( object sender , EventArgs e )
		{
			ListeKartVeAramaYerlesiminiUygula(
				groupBox3 ,
				groupBox6 ,
				dataGridView1 ,
				textBox1 ,
				panel8 ,
				panel7 ,
				panel6 ,
				panel5);
		}

		private void CariListeAramaYerlesiminiGuncelle ( object sender , EventArgs e )
		{
			ListeKartVeAramaYerlesiminiUygula(
				groupBox4 ,
				groupBox1 ,
				dataGridView2 ,
				CariListeAramaKutusuGetir() ,
				panel12 ,
				panel11 ,
				panel10 ,
				panel9);
		}

		private void UrunAlisListeAramaYerlesiminiGuncelle ( object sender , EventArgs e )
		{
			AramaKutusuKonumunuUygula(
				groupBox7 ,
				groupBox37 ,
				textBox2);
		}

		private void UrunSatisListeAramaYerlesiminiGuncelle ( object sender , EventArgs e )
		{
			AramaKutusuKonumunuUygula(
				groupBox38 ,
				groupBox40 ,
				textBox38);
		}

		private void ToptanciListeYerlesiminiGuncelle ( object sender , EventArgs e )
		{
			ListeKartVeAramaYerlesiminiUygula(
				groupBox52 ,
				groupBox54 ,
				dataGridView26 ,
				null ,
				panel17 ,
				panel16 ,
				panel15 ,
				panel14);
		}

		private void ListeKartVeAramaYerlesiminiUygula ( Control kokKapsayici , GroupBox listeKapsayici , DataGridView hedefGrid , TextBox aramaKutusu , params Panel[] kartPanelleri )
		{
			if(kokKapsayici==null||listeKapsayici==null)
				return;

			Panel[] ozetPanelleri = kartPanelleri
				.Where(p => p!=null)
				.ToArray();
			Rectangle icerikAlani = listeKapsayici.DisplayRectangle;
			int panelBosluk = 0;
			int panelUstBosluk = 0;
			int panelAltBosluk = 10;
			int panelYuksekligi = hedefGrid!=null
				? Math.Max(160 , hedefGrid.Top-icerikAlani.Top-panelAltBosluk)
				: 186;
			int mevcutX = icerikAlani.Left;

			for(int i = 0 ; i<ozetPanelleri.Length ; i++)
			{
				Panel panel = ozetPanelleri[i];
				int kalanPanelSayisi = ozetPanelleri.Length-i;
				int kalanGenislik = icerikAlani.Right-mevcutX;
				int panelGenisligi = kalanPanelSayisi==1
					? Math.Max(0 , icerikAlani.Right-mevcutX)
					: Math.Max(0 , ( kalanGenislik-( panelBosluk*( kalanPanelSayisi-1 ) ) )/kalanPanelSayisi);

				panel.Dock=DockStyle.None;
				panel.Anchor=AnchorStyles.Top|AnchorStyles.Left;
				panel.Margin=Padding.Empty;
				panel.Padding=Padding.Empty;
				panel.Location=new Point(mevcutX , icerikAlani.Top+panelUstBosluk);
				panel.Size=new Size(panelGenisligi , panelYuksekligi);
				KartPaneliGorunumunuGuncelle(panel);
				panel.BringToFront();

				mevcutX+=panelGenisligi+panelBosluk;
			}

			AramaKutusuKonumunuUygula(
				kokKapsayici ,
				listeKapsayici ,
				aramaKutusu);
		}

		private void AramaKutusuKonumunuUygula ( Control kokKapsayici , GroupBox listeKapsayici , TextBox aramaKutusu )
		{
			if(kokKapsayici==null||listeKapsayici==null||aramaKutusu==null||listeKapsayici.Parent==null)
				return;

			Size aramaKutusuBoyutu = AramaKutusuStandartBoyutunuGetir();
			Rectangle listeSiniri = kokKapsayici.RectangleToClient(listeKapsayici.Parent.RectangleToScreen(listeKapsayici.Bounds));
			Rectangle ustAlan = kokKapsayici.DisplayRectangle;
			int aramaKutusuX = Math.Max(
				ustAlan.Left ,
				listeSiniri.Right-aramaKutusuBoyutu.Width);
			int aramaKutusuY = Math.Max(
				ustAlan.Top+12 ,
				listeSiniri.Top-aramaKutusuBoyutu.Height-10);

			aramaKutusu.SuspendLayout();
			try
			{
				aramaKutusu.Dock=DockStyle.None;
				aramaKutusu.Location=new Point(aramaKutusuX , aramaKutusuY);
				aramaKutusu.Size=aramaKutusuBoyutu;
				aramaKutusu.Anchor=AnchorStyles.Top|AnchorStyles.Left;
			}
			finally
			{
				aramaKutusu.ResumeLayout();
				aramaKutusu.BringToFront();
			}
		}

		private void KartPaneliGorunumunuGuncelle ( Panel kartPaneli )
		{
			if(kartPaneli==null)
				return;

			PictureBox arkaPlanGorseli = kartPaneli.Controls.OfType<PictureBox>().FirstOrDefault();
			Control icerikKapsayici = ( Control )arkaPlanGorseli??kartPaneli;
			List<Label> etiketler = icerikKapsayici.Controls.OfType<Label>()
				.Where(etiket => !string.IsNullOrWhiteSpace(etiket.Text))
				.ToList();
			Label degerEtiketi = etiketler
				.OrderByDescending(etiket => etiket.Font.Size)
				.FirstOrDefault();
			List<Label> baslikEtiketleri = etiketler
				.Where(etiket => etiket!=degerEtiketi)
				.ToList();
			int yatayBosluk = 24;
			int mevcutY = 22;
			int kullanilabilirGenislik = Math.Max(140 , kartPaneli.Width-( yatayBosluk*2 ));

			foreach(Label baslikEtiketi in baslikEtiketleri)
			{
				baslikEtiketi.AutoSize=true;
				baslikEtiketi.MaximumSize=new Size(kullanilabilirGenislik , 0);
				baslikEtiketi.Location=new Point(yatayBosluk , mevcutY);
				mevcutY+=baslikEtiketi.Height+4;
			}

			if(degerEtiketi!=null)
			{
				degerEtiketi.AutoSize=true;
				degerEtiketi.Location=new Point(
					yatayBosluk ,
					Math.Max(mevcutY+12 , kartPaneli.Height-degerEtiketi.Height-28));
			}
		}

		private TextBox CariListeAramaKutusuGetir ()
		{
			if(textBox111!=null&&textBox111.Visible)
				return textBox111;

			return textBox8;
		}

		private void AramaKutusuHazirla ( TextBox aramaKutusu , DataGridView hedefGrid = null )
		{
			if(aramaKutusu==null)
				return;

			AramaKutusuGorunumunuUygula(aramaKutusu);

			aramaKutusu.Enter-=AramaKutusu_Enter;
			aramaKutusu.Enter+=AramaKutusu_Enter;
			aramaKutusu.Click-=AramaKutusu_Click;
			aramaKutusu.Click+=AramaKutusu_Click;
			aramaKutusu.Leave-=AramaKutusu_Leave;
			aramaKutusu.Leave+=AramaKutusu_Leave;

			aramaKutusu.TextChanged-=GenelAramaKutusu_TextChanged;
			if(hedefGrid!=null)
			{
				_aramaKutusuGridEslesmeleri[aramaKutusu]=hedefGrid;
				aramaKutusu.TextChanged+=GenelAramaKutusu_TextChanged;
			}

			if(string.IsNullOrWhiteSpace(AramaKutusuMetniGetir(aramaKutusu)))
				AramaKutusuPlaceholderiniGoster(aramaKutusu);
			else
				AramaKutusuYaziModunuUygula(aramaKutusu);

			if(hedefGrid!=null)
				GridAramaFiltresiniUygula(aramaKutusu , hedefGrid);
		}

		private void AramaKutusuGorunumunuUygula ( TextBox aramaKutusu , bool vurguKutusu = false )
		{
			if(aramaKutusu==null)
				return;

			TextBox ornekTextBox = AramaKutusuOrnekTextBoxGetir();
			Size aramaKutusuBoyutu = ornekTextBox?.Size??new Size(258 , 28);
			if(aramaKutusu.Parent is TableLayoutPanel&&aramaKutusu.Dock==DockStyle.Fill&&aramaKutusu.Width>0&&aramaKutusu.Height>0)
				aramaKutusuBoyutu=aramaKutusu.Size;

			aramaKutusu.SuspendLayout();
			try
			{
				aramaKutusu.AutoSize=false;
				aramaKutusu.Multiline=false;
				aramaKutusu.BorderStyle=BorderStyle.FixedSingle;
				aramaKutusu.BackColor=AramaKutusuArkaPlanRenginiGetir(aramaKutusu);
				aramaKutusu.Font=ornekTextBox?.Font??new Font("Microsoft Sans Serif" , 9F , FontStyle.Regular);
				aramaKutusu.Margin=ornekTextBox?.Margin??new Padding(4);
				aramaKutusu.Size=aramaKutusuBoyutu;

				if(aramaKutusu.Dock==DockStyle.Right&&aramaKutusu.Parent!=null)
				{
					Rectangle parentAlani = aramaKutusu.Parent.DisplayRectangle;
					aramaKutusu.Dock=DockStyle.None;
					aramaKutusu.Location=new Point(parentAlani.Right-aramaKutusuBoyutu.Width , parentAlani.Top);
					aramaKutusu.Anchor=AnchorStyles.Top|AnchorStyles.Right;
				}
			}
			finally
			{
				aramaKutusu.ResumeLayout();
			}
		}

		private Size AramaKutusuStandartBoyutunuGetir ()
		{
			return AramaKutusuOrnekTextBoxGetir()?.Size??new Size(258 , 28);
		}

		private TextBox AramaKutusuOrnekTextBoxGetir ()
		{
			TextBox[] ornekKutular = new[]
			{
				txtID ,
				txtTCVKN ,
				txtAdSoyad ,
				textBox10 ,
				textBox11 ,
				textBox12 ,
				textBox25 ,
				textBox26 ,
				textBox5 ,
				textBox6 ,
				textBox7
			};

			return ornekKutular.FirstOrDefault(kutu => kutu!=null&&!kutu.IsDisposed);
		}

		private Color AramaKutusuArkaPlanRenginiGetir ( TextBox aramaKutusu )
		{
			Control mevcutKontrol = aramaKutusu?.Parent;
			while(mevcutKontrol!=null)
			{
				if(mevcutKontrol.BackColor!=Color.Transparent)
					return mevcutKontrol.BackColor;

				mevcutKontrol=mevcutKontrol.Parent;
			}

			return SystemColors.Control;
		}

		private void AramaKutusu_Enter ( object sender , EventArgs e )
		{
			AramaKutusuPlaceholderiniTemizle(sender as TextBox);
		}

		private void AramaKutusu_Click ( object sender , EventArgs e )
		{
			AramaKutusuPlaceholderiniTemizle(sender as TextBox);
		}

		private void AramaKutusu_Leave ( object sender , EventArgs e )
		{
			TextBox aramaKutusu = sender as TextBox;
			if(aramaKutusu==null)
				return;

			if(string.IsNullOrWhiteSpace(aramaKutusu.Text))
				AramaKutusuPlaceholderiniGoster(aramaKutusu);
		}

		private void AramaKutusuPlaceholderiniTemizle ( TextBox aramaKutusu )
		{
			if(aramaKutusu==null)
				return;

			if(!string.Equals(( aramaKutusu.Text??string.Empty ).Trim() , AramaPlaceholderMetni , StringComparison.OrdinalIgnoreCase))
			{
				AramaKutusuYaziModunuUygula(aramaKutusu);
				return;
			}

			aramaKutusu.Text=string.Empty;
			AramaKutusuYaziModunuUygula(aramaKutusu);
		}

		private void AramaKutusuPlaceholderiniGoster ( TextBox aramaKutusu )
		{
			if(aramaKutusu==null)
				return;

			aramaKutusu.ForeColor=SystemColors.GrayText;
			aramaKutusu.TextAlign=HorizontalAlignment.Center;
			if(!string.Equals(aramaKutusu.Text , AramaPlaceholderMetni , StringComparison.Ordinal))
				aramaKutusu.Text=AramaPlaceholderMetni;
		}

		private void AramaKutusuYaziModunuUygula ( TextBox aramaKutusu )
		{
			if(aramaKutusu==null)
				return;

			aramaKutusu.ForeColor=SystemColors.WindowText;
			aramaKutusu.TextAlign=HorizontalAlignment.Left;
		}

		private string AramaKutusuMetniGetir ( TextBox aramaKutusu )
		{
			string arama = aramaKutusu?.Text?.Trim()??string.Empty;
			return string.Equals(arama , AramaPlaceholderMetni , StringComparison.OrdinalIgnoreCase) ? string.Empty : arama;
		}

		private void GenelAramaKutusu_TextChanged ( object sender , EventArgs e )
		{
			TextBox aramaKutusu = sender as TextBox;
			if(aramaKutusu==null)
				return;

			DataGridView hedefGrid;
			if(!_aramaKutusuGridEslesmeleri.TryGetValue(aramaKutusu , out hedefGrid))
				return;

			GridAramaFiltresiniUygula(aramaKutusu , hedefGrid);
		}

		private void GridAramaFiltresiniUygula ( TextBox aramaKutusu , DataGridView hedefGrid )
		{
			if(aramaKutusu==null||hedefGrid==null||hedefGrid.IsDisposed)
				return;

			string aramaMetni = AramaKutusuMetniGetir(aramaKutusu);
			try
			{
				if(hedefGrid.DataSource is BindingSource)
				{
					BindingSource kaynak = ( BindingSource )hedefGrid.DataSource;
					if(kaynak.List is DataView)
					{
						DataViewAramaFiltresiniUygula(( DataView )kaynak.List , hedefGrid , aramaMetni);
						return;
					}

					if(kaynak.DataSource is DataTable)
					{
						DataViewAramaFiltresiniUygula((( DataTable )kaynak.DataSource).DefaultView , hedefGrid , aramaMetni);
						return;
					}
				}

				if(hedefGrid.DataSource is DataView)
				{
					DataViewAramaFiltresiniUygula(( DataView )hedefGrid.DataSource , hedefGrid , aramaMetni);
					return;
				}

				if(hedefGrid.DataSource is DataTable)
				{
					DataViewAramaFiltresiniUygula((( DataTable )hedefGrid.DataSource).DefaultView , hedefGrid , aramaMetni);
					return;
				}

				GridSatirGorunurlugunuUygula(hedefGrid , aramaMetni);
			}
			catch
			{
				GridSatirGorunurlugunuUygula(hedefGrid , aramaMetni);
			}
		}

		private void DataViewAramaFiltresiniUygula ( DataView gorunum , DataGridView hedefGrid , string aramaMetni )
		{
			if(gorunum==null)
				return;

			string filtre = DataTableAramaFiltresiOlustur(gorunum.Table , hedefGrid , aramaMetni);
			try
			{
				gorunum.RowFilter=filtre;
			}
			catch
			{
				gorunum.RowFilter=string.Empty;
			}

			if(hedefGrid!=null)
				hedefGrid.ClearSelection();
		}

		private string DataTableAramaFiltresiOlustur ( DataTable tablo , DataGridView hedefGrid , string aramaMetni )
		{
			if(tablo==null||string.IsNullOrWhiteSpace(aramaMetni))
				return string.Empty;

			string filtreMetni = aramaMetni.Replace("'" , "''").Replace("[" , "[[]").Replace("%" , "[%]").Replace("*" , "[*]");
			List<string> filtreler = new List<string>();
			foreach(DataColumn kolon in tablo.Columns)
			{
				if(!GridAramaKolonuMu(kolon , hedefGrid))
					continue;

				string kolonAdi = kolon.ColumnName.Replace("]" , "]]");
				filtreler.Add("CONVERT(["+kolonAdi+"], 'System.String') LIKE '%"+filtreMetni+"%'");
			}

			return string.Join(" OR " , filtreler);
		}

		private bool GridAramaKolonuMu ( DataColumn kolon , DataGridView hedefGrid )
		{
			if(kolon==null||kolon.DataType==typeof(byte[]))
				return false;
			if(hedefGrid==null)
				return true;
			if(!hedefGrid.Columns.Contains(kolon.ColumnName))
				return false;

			return hedefGrid.Columns[kolon.ColumnName].Visible;
		}

		private void GridSatirGorunurlugunuUygula ( DataGridView hedefGrid , string aramaMetni )
		{
			if(hedefGrid==null)
				return;

			string normalizeArama = AramaMetniniNormalizeEt(aramaMetni);
			bool filtreYok = string.IsNullOrWhiteSpace(normalizeArama);

			hedefGrid.SuspendLayout();
			try
			{
				try
				{
					hedefGrid.CurrentCell=null;
				}
				catch
				{
				}

				foreach(DataGridViewRow satir in hedefGrid.Rows)
				{
					if(satir.IsNewRow)
						continue;

					bool gorunsun = filtreYok||satir.Cells.Cast<DataGridViewCell>()
						.Where(hucre => hucre.OwningColumn!=null&&hucre.OwningColumn.Visible)
						.Any(hucre => AramaMetniHucredeVarMi(hucre.Value , normalizeArama));
					satir.Visible=gorunsun;
				}
			}
			finally
			{
				hedefGrid.ResumeLayout();
				hedefGrid.ClearSelection();
			}
		}

		private bool AramaMetniHucredeVarMi ( object hucreDegeri , string normalizeArama )
		{
			if(string.IsNullOrWhiteSpace(normalizeArama))
				return true;

			string hucreMetni = AramaMetniniNormalizeEt(Convert.ToString(hucreDegeri));
			return !string.IsNullOrWhiteSpace(hucreMetni)&&hucreMetni.Contains(normalizeArama);
		}

		private void KurCariDurumSekmesi ()
		{
			if(flowLayoutPanel7==null) return;

			EnsureCariDurumCariTipCombo();

			flowLayoutPanel7.SuspendLayout();
			flowLayoutPanel7.Controls.Clear();
			flowLayoutPanel7.Controls.Add(textBox7);    // ID
			flowLayoutPanel7.Controls.Add(comboBox12);  // Cari Tipi
			flowLayoutPanel7.Controls.Add(comboBox13);  // Cari Durum
			flowLayoutPanel7.ResumeLayout();

			textBox7.ReadOnly=true;
			textBox7.BackColor=SystemColors.ControlLight;
			tabPage12.Text="Cari Durum";
			CariTipVeDurumAramalariniKaldir();
		}

		private void KurNotlarSekmesi ()
		{
			if(tabPage6==null||groupBox19==null||groupBox20==null||groupBox21==null||tableLayoutPanel8==null||flowLayoutPanel9==null)
				return;

			Panel headerPanel = new Panel();
			Panel headerTextPanel = new Panel();
			Label titleLabel = new Label();
			Label subtitleLabel = new Label();
			FlowLayoutPanel actionPanel = new FlowLayoutPanel();
			TableLayoutPanel headerLayout = new TableLayoutPanel();
			TableLayoutPanel rootLayout = new TableLayoutPanel();
			TableLayoutPanel listLayout = new TableLayoutPanel();
			TableLayoutPanel ozetLayout = new TableLayoutPanel();
			Panel detayBaslikPanel = new Panel();
			Label detayBaslikLabel = new Label();
			Label detayAciklamaLabel = new Label();
			TableLayoutPanel altAksiyonLayout = new TableLayoutPanel();

			tabPage6.SuspendLayout();
			groupBox19.SuspendLayout();
			tableLayoutPanel8.SuspendLayout();
			groupBox20.SuspendLayout();
			groupBox21.SuspendLayout();
			flowLayoutPanel9.SuspendLayout();

			try
			{
				_notBekleyenAramaKutusu=NotAramaKutusuOlusturVeyaGetir(_notBekleyenAramaKutusu);
				_notOkunanAramaKutusu=NotAramaKutusuOlusturVeyaGetir(_notOkunanAramaKutusu);

				tabPage6.BackColor=Color.FromArgb(241 , 245 , 249);
				tabPage6.Padding=new Padding(14);

				groupBox19.Dock=DockStyle.Fill;
				groupBox19.Text=string.Empty;
				groupBox19.BackColor=tabPage6.BackColor;
				groupBox19.ForeColor=Color.FromArgb(30 , 41 , 59);
				groupBox19.FlatStyle=FlatStyle.Flat;
				groupBox19.Padding=new Padding(0);

				rootLayout.Name="tableLayoutPanelNotRoot";
				rootLayout.Dock=DockStyle.Fill;
				rootLayout.BackColor=tabPage6.BackColor;
				rootLayout.Margin=new Padding(0);
				rootLayout.Padding=new Padding(0);
				rootLayout.ColumnCount=1;
				rootLayout.RowCount=2;
				rootLayout.ColumnStyles.Clear();
				rootLayout.RowStyles.Clear();
				rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 188F));
				rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				groupBox19.Controls.Clear();
				groupBox19.Controls.Add(rootLayout);

				headerPanel.BackColor=Color.White;
				headerPanel.BorderStyle=BorderStyle.FixedSingle;
				headerPanel.Dock=DockStyle.Fill;
				headerPanel.Margin=new Padding(0 , 0 , 0 , 12);
				headerPanel.Padding=new Padding(20 , 18 , 20 , 18);

				headerLayout.Dock=DockStyle.Fill;
				headerLayout.Margin=new Padding(0);
				headerLayout.Padding=new Padding(0);
				headerLayout.BackColor=Color.White;
				headerLayout.ColumnCount=2;
				headerLayout.RowCount=2;
				headerLayout.ColumnStyles.Clear();
				headerLayout.RowStyles.Clear();
				headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 434F));
				headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 64F));
				headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				headerTextPanel.Dock=DockStyle.Fill;
				headerTextPanel.Margin=new Padding(0);
				headerTextPanel.Padding=new Padding(0);
				headerTextPanel.BackColor=Color.White;

				titleLabel.AutoSize=true;
				titleLabel.Font=new Font("Segoe UI" , 18F , FontStyle.Bold);
				titleLabel.ForeColor=Color.FromArgb(15 , 23 , 42);
				titleLabel.Location=new Point(0 , 0);
				titleLabel.Text="Notlarım";

				subtitleLabel.AutoSize=false;
				subtitleLabel.Font=new Font("Segoe UI" , 10F , FontStyle.Regular);
				subtitleLabel.ForeColor=Color.FromArgb(100 , 116 , 139);
				subtitleLabel.Location=new Point(0 , 34);
				subtitleLabel.Size=new Size(680 , 24);
				subtitleLabel.Text="Bekleyen ve tamamlanan notlarınızı tek ekrandan daha düzenli ve hızlı yönetin.";

				actionPanel.AutoSize=true;
				actionPanel.AutoSizeMode=AutoSizeMode.GrowAndShrink;
				actionPanel.Dock=DockStyle.Fill;
				actionPanel.FlowDirection=FlowDirection.LeftToRight;
				actionPanel.WrapContents=false;
				actionPanel.Margin=new Padding(0);
				actionPanel.Padding=new Padding(0 , 10 , 0 , 0);
				actionPanel.MinimumSize=new Size(410 , 56);

				label75.Visible=false;
				label76.Visible=false;
				label77.Visible=false;

				HazirlaNotAksiyonButonu(button29 , "Kaydet" , "Save.png" , Color.FromArgb(13 , 148 , 136));
				HazirlaNotAksiyonButonu(button27 , "Güncelle" , "Renew.png" , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
				HazirlaNotAksiyonButonu(button28 , "Sil" , "Delete File.png" , Color.White , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
				button29.Margin=new Padding(0 , 0 , 12 , 0);
				button27.Margin=new Padding(0 , 0 , 12 , 0);
				button28.Margin=new Padding(0);
				actionPanel.Controls.Add(button29);
				actionPanel.Controls.Add(button27);
				actionPanel.Controls.Add(button28);

				ozetLayout.Dock=DockStyle.Fill;
				ozetLayout.Margin=new Padding(0 , 10 , 0 , 0);
				ozetLayout.Padding=new Padding(0);
				ozetLayout.BackColor=Color.White;
				ozetLayout.ColumnCount=4;
				ozetLayout.RowCount=1;
				ozetLayout.ColumnStyles.Clear();
				ozetLayout.RowStyles.Clear();
				for(int i = 0 ; i<4 ; i++)
					ozetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
				ozetLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				Panel bekleyenKart = NotOzetKartiOlustur("BEKLEYEN NOT" , Color.FromArgb(255 , 247 , 237) , Color.FromArgb(251 , 146 , 60) , out _notBekleyenOzetDegerLabel);
				Panel okunanKart = NotOzetKartiOlustur("TAMAMLANAN" , Color.FromArgb(236 , 253 , 245) , Color.FromArgb(13 , 148 , 136) , out _notOkunanOzetDegerLabel);
				Panel toplamKart = NotOzetKartiOlustur("TOPLAM NOT" , Color.FromArgb(239 , 246 , 255) , Color.FromArgb(37 , 99 , 235) , out _notToplamOzetDegerLabel);
				Panel sonKart = NotOzetKartiOlustur("SON GÜNCELLEME" , Color.FromArgb(248 , 250 , 252) , Color.FromArgb(100 , 116 , 139) , out _notSonGuncellemeOzetDegerLabel);

				bekleyenKart.Margin=new Padding(0 , 0 , 12 , 0);
				okunanKart.Margin=new Padding(0 , 0 , 12 , 0);
				toplamKart.Margin=new Padding(0 , 0 , 12 , 0);
				sonKart.Margin=Padding.Empty;

				ozetLayout.Controls.Add(bekleyenKart , 0 , 0);
				ozetLayout.Controls.Add(okunanKart , 1 , 0);
				ozetLayout.Controls.Add(toplamKart , 2 , 0);
				ozetLayout.Controls.Add(sonKart , 3 , 0);

				headerTextPanel.Controls.Add(titleLabel);
				headerTextPanel.Controls.Add(subtitleLabel);
				headerLayout.Controls.Add(headerTextPanel , 0 , 0);
				headerLayout.Controls.Add(actionPanel , 1 , 0);
				headerLayout.Controls.Add(ozetLayout , 0 , 1);
				headerLayout.SetColumnSpan(ozetLayout , 2);
				headerPanel.Controls.Add(headerLayout);

				tableLayoutPanel8.Dock=DockStyle.Fill;
				tableLayoutPanel8.Margin=new Padding(0);
				tableLayoutPanel8.Padding=new Padding(0);
				tableLayoutPanel8.BackColor=tabPage6.BackColor;
				tableLayoutPanel8.ColumnCount=2;
				tableLayoutPanel8.RowCount=1;
				tableLayoutPanel8.ColumnStyles.Clear();
				tableLayoutPanel8.RowStyles.Clear();
				tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 63F));
				tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 37F));
				tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				rootLayout.Controls.Add(headerPanel , 0 , 0);
				rootLayout.Controls.Add(tableLayoutPanel8 , 0 , 1);

				groupBox21.Text=string.Empty;
				groupBox21.BackColor=tabPage6.BackColor;
				groupBox21.FlatStyle=FlatStyle.Flat;
				groupBox21.Padding=new Padding(0);
				groupBox21.Margin=new Padding(0 , 0 , 12 , 0);

				listLayout.Name="tableLayoutPanelNotList";
				listLayout.Dock=DockStyle.Fill;
				listLayout.BackColor=tabPage6.BackColor;
				listLayout.Margin=new Padding(0);
				listLayout.Padding=new Padding(0);
				listLayout.ColumnCount=1;
				listLayout.RowCount=2;
				listLayout.ColumnStyles.Clear();
				listLayout.RowStyles.Clear();
				listLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				listLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 50F));
				listLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 50F));

				groupBox21.Controls.Clear();
				groupBox21.Controls.Add(listLayout);

				HazirlaNotListeKartini(groupBox33 , "Okunmayan Notlar" , dataGridView8 , _notBekleyenAramaKutusu);
				HazirlaNotListeKartini(groupBox32 , "Okunan Notlar" , dataGridView16 , _notOkunanAramaKutusu);
				groupBox33.Margin=new Padding(0 , 0 , 0 , 12);
				groupBox32.Margin=new Padding(0);
				listLayout.Controls.Add(groupBox33 , 0 , 0);
				listLayout.Controls.Add(groupBox32 , 0 , 1);

				groupBox20.Text=string.Empty;
				groupBox20.BackColor=Color.White;
				groupBox20.ForeColor=Color.FromArgb(30 , 41 , 59);
				groupBox20.FlatStyle=FlatStyle.Flat;
				groupBox20.Padding=new Padding(18);
				groupBox20.Margin=Padding.Empty;

				groupBox20.Controls.Clear();
				_notDetayLayout=new TableLayoutPanel
				{
					Dock=DockStyle.Fill,
					ColumnCount=1,
					RowCount=8,
					Margin=Padding.Empty,
					Padding=Padding.Empty,
					BackColor=Color.White
				};
				_notDetayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 56F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 24F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 38F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 24F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 38F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 24F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));
				_notDetayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 52F));
				groupBox20.Controls.Add(_notDetayLayout);

				detayBaslikPanel.Dock=DockStyle.Fill;
				detayBaslikPanel.BackColor=Color.White;
				detayBaslikPanel.Margin=Padding.Empty;
				detayBaslikPanel.Padding=Padding.Empty;

				detayBaslikLabel.AutoSize=true;
				detayBaslikLabel.Font=new Font("Segoe UI" , 12F , FontStyle.Bold);
				detayBaslikLabel.ForeColor=Color.FromArgb(15 , 23 , 42);
				detayBaslikLabel.Location=new Point(0 , 0);
				detayBaslikLabel.Text="Not Detayı";

				detayAciklamaLabel.AutoSize=true;
				detayAciklamaLabel.Font=new Font("Segoe UI" , 9.25F , FontStyle.Regular);
				detayAciklamaLabel.ForeColor=Color.FromArgb(100 , 116 , 139);
				detayAciklamaLabel.Location=new Point(0 , 28);
				detayAciklamaLabel.Text="Seçili notu düzenleyin veya yeni bir not oluşturun.";

				detayBaslikPanel.Controls.Add(detayBaslikLabel);
				detayBaslikPanel.Controls.Add(detayAciklamaLabel);

				HazirlaNotEditorEtiketi(label133 , "Başlık");
				HazirlaNotEditorEtiketi(label132 , "Tarih");
				HazirlaNotEditorEtiketi(label74 , "Not");
				HazirlaNotMetinKutusu(textBox53 , false);
				HazirlaNotMetinKutusu(textBox57 , false);
				HazirlaNotMetinKutusu(textBox73 , true);
				textBox53.Dock=DockStyle.Fill;
				textBox57.Dock=DockStyle.Fill;
				textBox73.Dock=DockStyle.Fill;

				checkBox1.AutoSize=false;
				checkBox1.Font=new Font("Segoe UI" , 9.5F , FontStyle.Regular);
				checkBox1.ForeColor=Color.FromArgb(51 , 65 , 85);
				checkBox1.Margin=Padding.Empty;
				checkBox1.Text="Okundu olarak işaretle";
				checkBox1.UseVisualStyleBackColor=true;
				checkBox1.Dock=DockStyle.Fill;

				HazirlaNotIkincilButonu(button26 , "Formu Temizle" , "Erase.png");
				button26.Margin=Padding.Empty;

				altAksiyonLayout.Dock=DockStyle.Fill;
				altAksiyonLayout.Margin=Padding.Empty;
				altAksiyonLayout.Padding=new Padding(0 , 8 , 0 , 0);
				altAksiyonLayout.BackColor=Color.White;
				altAksiyonLayout.ColumnCount=2;
				altAksiyonLayout.RowCount=1;
				altAksiyonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				altAksiyonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 156F));
				altAksiyonLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));
				altAksiyonLayout.Controls.Add(checkBox1 , 0 , 0);
				altAksiyonLayout.Controls.Add(button26 , 1 , 0);

				_notDetayLayout.Controls.Add(detayBaslikPanel , 0 , 0);
				_notDetayLayout.Controls.Add(label133 , 0 , 1);
				_notDetayLayout.Controls.Add(textBox53 , 0 , 2);
				_notDetayLayout.Controls.Add(label132 , 0 , 3);
				_notDetayLayout.Controls.Add(textBox57 , 0 , 4);
				_notDetayLayout.Controls.Add(label74 , 0 , 5);
				_notDetayLayout.Controls.Add(textBox73 , 0 , 6);
				_notDetayLayout.Controls.Add(altAksiyonLayout , 0 , 7);

				groupBox20.Resize-=GroupBox20_NotDetayResize;
				groupBox20.Resize+=GroupBox20_NotDetayResize;
				NotDetayAlaniniYenidenBoyutlandir();

				NotGridStiliniUygula(dataGridView8);
				NotGridStiliniUygula(dataGridView16);
				NotOzetKartlariniGuncelle();
			}
			finally
			{
				flowLayoutPanel9.ResumeLayout();
				groupBox21.ResumeLayout();
				groupBox20.ResumeLayout();
				tableLayoutPanel8.ResumeLayout();
				groupBox19.ResumeLayout();
				tabPage6.ResumeLayout();
			}
		}

		private void HazirlaNotAksiyonButonu ( Button buton , string metin , string imageKey , Color arkaPlan , Color? yaziRengi = null , Color? kenarRengi = null )
		{
			if(buton==null)
				return;

			buton.AutoSize=false;
			buton.Size=new Size(132 , 42);
			buton.Text=metin;
			buton.ImageList=null;
			buton.Image=NotButonGorseliOlustur(imageKey , new Size(18 , 18));
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleRight;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.Padding=new Padding(14 , 0 , 14 , 0);
			buton.Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold);
			buton.BackColor=arkaPlan;
			buton.ForeColor=yaziRengi??Color.White;
			buton.FlatStyle=FlatStyle.Flat;
			buton.FlatAppearance.BorderSize=kenarRengi.HasValue ? 1 : 0;
			buton.FlatAppearance.BorderColor=kenarRengi??arkaPlan;
			buton.FlatAppearance.MouseOverBackColor=ControlPaint.Light(arkaPlan);
			buton.FlatAppearance.MouseDownBackColor=ControlPaint.Dark(arkaPlan);
			buton.Cursor=Cursors.Hand;
			buton.UseVisualStyleBackColor=false;
		}

		private void HazirlaNotIkincilButonu ( Button buton , string metin , string imageKey )
		{
			if(buton==null)
				return;

			buton.AutoSize=false;
			buton.Height=40;
			buton.Text=metin;
			buton.ImageList=null;
			buton.Image=NotButonGorseliOlustur(imageKey , new Size(18 , 18));
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleRight;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.Padding=new Padding(12 , 0 , 14 , 0);
			buton.Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold);
			buton.BackColor=Color.White;
			buton.ForeColor=Color.FromArgb(15 , 23 , 42);
			buton.FlatStyle=FlatStyle.Flat;
			buton.FlatAppearance.BorderSize=1;
			buton.FlatAppearance.BorderColor=Color.FromArgb(148 , 163 , 184);
			buton.FlatAppearance.MouseOverBackColor=Color.FromArgb(248 , 250 , 252);
			buton.FlatAppearance.MouseDownBackColor=Color.FromArgb(226 , 232 , 240);
			buton.Margin=Padding.Empty;
			buton.Cursor=Cursors.Hand;
			buton.UseVisualStyleBackColor=false;
		}

		private void LegacyUrunAksiyonButonlariniUygula ()
		{
			HazirlaLegacyAksiyonButonu(button9 , label100 , "Kaydet" , "Save.png" , Color.FromArgb(13 , 148 , 136) , new Point(25 , 24) , new Size(110 , 54));
			HazirlaLegacyAksiyonButonu(button8 , label101 , "Sil" , "Delete File.png" , Color.White , new Point(145 , 24) , new Size(110 , 54) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			HazirlaLegacyAksiyonButonu(button3 , label99 , "Guncelle" , "Renew.png" , Color.White , new Point(265 , 24) , new Size(118 , 54) , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
			HazirlaLegacyAksiyonButonu(button1 , label1 , "Sepete Ekle" , "Buy.png" , Color.FromArgb(37 , 99 , 235) , new Point(393 , 24) , new Size(136 , 54));
			HazirlaLegacyIkincilButonu(button2 , "Temizle" , "Broom.png" , new Point(155 , 567) , new Size(258 , 44));

			HazirlaLegacyAksiyonButonu(button45 , label177 , "Kaydet" , "Save.png" , Color.FromArgb(13 , 148 , 136) , new Point(25 , 24) , new Size(110 , 54));
			HazirlaLegacyAksiyonButonu(button44 , label178 , "Sil" , "Delete File.png" , Color.White , new Point(145 , 24) , new Size(110 , 54) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			HazirlaLegacyAksiyonButonu(button43 , label176 , "Guncelle" , "Renew.png" , Color.White , new Point(265 , 24) , new Size(118 , 54) , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
			HazirlaLegacyIkincilButonu(button42 , "Temizle" , "Broom.png" , new Point(155 , 567) , new Size(258 , 44));

			HazirlaLegacyAksiyonButonu(button50 , label198 , "Kaydet" , "Save.png" , Color.FromArgb(13 , 148 , 136) , new Point(25 , 24) , new Size(110 , 54));
			HazirlaLegacyAksiyonButonu(button49 , label199 , "Sil" , "Delete File.png" , Color.White , new Point(145 , 24) , new Size(110 , 54) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			HazirlaLegacyAksiyonButonu(button48 , label197 , "Guncelle" , "Renew.png" , Color.White , new Point(265 , 24) , new Size(118 , 54) , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));

			HazirlaNotAksiyonButonu(button47 , "Toplu Zam" , "Volunteering.png" , Color.FromArgb(22 , 163 , 74));
			button47.Location=new Point(162 , 265);
			button47.Size=new Size(258 , 44);
			button47.BringToFront();
		}

		private void HazirlaLegacyAksiyonButonu ( Button buton , Label etiket , string metin , string imageKey , Color arkaPlan , Point konum , Size boyut , Color? yaziRengi = null , Color? kenarRengi = null )
		{
			if(buton==null)
				return;

			HazirlaNotAksiyonButonu(buton , metin , imageKey , arkaPlan , yaziRengi , kenarRengi);
			buton.Location=konum;
			buton.Size=boyut;
			buton.Anchor=AnchorStyles.Top|AnchorStyles.Left;
			buton.BringToFront();
			if(etiket!=null)
				etiket.Visible=false;
		}

		private void HazirlaLegacyIkincilButonu ( Button buton , string metin , string imageKey , Point konum , Size boyut )
		{
			if(buton==null)
				return;

			HazirlaNotIkincilButonu(buton , metin , imageKey);
			buton.Location=konum;
			buton.Size=boyut;
			buton.Anchor=AnchorStyles.Top|AnchorStyles.Left;
			buton.BringToFront();
		}

		private Panel NotOzetKartiOlustur ( string baslik , Color arkaPlan , Color kenarRengi , out Label degerLabel )
		{
			Panel kart = new Panel
			{
				Dock=DockStyle.Fill,
				Margin=Padding.Empty,
				Padding=new Padding(16 , 14 , 16 , 14),
				BackColor=arkaPlan,
				BorderStyle=BorderStyle.FixedSingle
			};

			Label baslikLabel = new Label
			{
				Dock=DockStyle.Top,
				Height=24,
				AutoSize=false,
				Text=baslik,
				Font=new Font("Segoe UI" , 8.75F , FontStyle.Bold),
				ForeColor=Color.FromArgb(71 , 85 , 105),
				TextAlign=ContentAlignment.MiddleLeft
			};

			degerLabel=new Label
			{
				Dock=DockStyle.Fill,
				AutoSize=false,
				Text="0",
				Font=new Font("Segoe UI" , 16F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				TextAlign=ContentAlignment.MiddleLeft
			};

			Panel vurguPaneli = new Panel
			{
				Dock=DockStyle.Top,
				Height=3,
				BackColor=kenarRengi
			};

			kart.Controls.Add(degerLabel);
			kart.Controls.Add(baslikLabel);
			kart.Controls.Add(vurguPaneli);
			return kart;
		}

		private Image NotButonGorseliOlustur ( string imageKey , Size boyut )
		{
			if(imageList1==null||string.IsNullOrWhiteSpace(imageKey)||boyut.Width<=0||boyut.Height<=0||!imageList1.Images.ContainsKey(imageKey))
				return null;

			Image kaynak = imageList1.Images[imageKey];
			if(kaynak==null)
				return null;

			Bitmap gorsel = new Bitmap(boyut.Width , boyut.Height);
			using(Graphics grafik = Graphics.FromImage(gorsel))
			{
				grafik.Clear(Color.Transparent);
				grafik.InterpolationMode=InterpolationMode.HighQualityBicubic;
				grafik.SmoothingMode=SmoothingMode.AntiAlias;
				grafik.PixelOffsetMode=PixelOffsetMode.HighQuality;
				grafik.DrawImage(kaynak , new Rectangle(Point.Empty , boyut));
			}

			return gorsel;
		}

		private void HazirlaNotEditorEtiketi ( Label etiket , string metin )
		{
			if(etiket==null)
				return;

			etiket.AutoSize=false;
			etiket.Height=22;
			etiket.Text=metin;
			etiket.Font=new Font("Segoe UI" , 10F , FontStyle.Bold);
			etiket.ForeColor=Color.FromArgb(51 , 65 , 85);
			etiket.TextAlign=ContentAlignment.BottomLeft;
			etiket.Margin=new Padding(0 , 0 , 0 , 6);
		}

		private void HazirlaNotMetinKutusu ( TextBox textBox , bool cokSatirli )
		{
			if(textBox==null)
				return;

			textBox.BorderStyle=BorderStyle.FixedSingle;
			textBox.BackColor=Color.White;
			textBox.ForeColor=Color.FromArgb(15 , 23 , 42);
			textBox.Font=new Font("Segoe UI" , 10F , FontStyle.Regular);
			textBox.Multiline=cokSatirli;
			textBox.ScrollBars=cokSatirli ? ScrollBars.Vertical : ScrollBars.None;
			textBox.AcceptsReturn=cokSatirli;
			textBox.WordWrap=cokSatirli;
			textBox.Margin=Padding.Empty;
		}

		private void HazirlaNotListeKutusu ( GroupBox kutu , string baslik )
		{
			if(kutu==null)
				return;

			kutu.Dock=DockStyle.Fill;
			kutu.Text=baslik;
			kutu.BackColor=Color.White;
			kutu.ForeColor=Color.FromArgb(30 , 41 , 59);
			kutu.FlatStyle=FlatStyle.Flat;
			kutu.Font=new Font("Segoe UI" , 10F , FontStyle.Bold);
			kutu.Padding=new Padding(12);
		}

		private TextBox NotAramaKutusuOlusturVeyaGetir ( TextBox mevcutAramaKutusu )
		{
			if(mevcutAramaKutusu!=null&&!mevcutAramaKutusu.IsDisposed)
				return mevcutAramaKutusu;

			return SatisRaporAramaKutusuOlustur(AramaKutusuStandartBoyutunuGetir());
		}

		private void HazirlaNotListeKartini ( GroupBox kutu , string baslik , DataGridView grid , TextBox aramaKutusu )
		{
			if(kutu==null||grid==null||aramaKutusu==null)
				return;

			HazirlaNotListeKutusu(kutu , string.Empty);
			Size standartAramaKutusuBoyutu = AramaKutusuStandartBoyutunuGetir();
			Color aramaAlanArkaPlanRengi = kutu.BackColor;
			if(aramaAlanArkaPlanRengi.IsEmpty||aramaAlanArkaPlanRengi==Color.Transparent)
				aramaAlanArkaPlanRengi=this.BackColor;

			Panel ustPanel = new Panel
			{
				Dock=DockStyle.Top,
				Height=standartAramaKutusuBoyutu.Height+8,
				BackColor=aramaAlanArkaPlanRengi,
				Margin=Padding.Empty,
				Padding=new Padding(2 , 0 , 2 , 8)
			};

			Panel ayirici = new Panel
			{
				Dock=DockStyle.Top,
				Height=1,
				BackColor=Color.FromArgb(226 , 232 , 240)
			};

			Label baslikLabel = new Label
			{
				Dock=DockStyle.Left,
				Width=220,
				Text=baslik,
				Font=new Font("Segoe UI" , 10.5F , FontStyle.Bold),
				ForeColor=Color.FromArgb(30 , 41 , 59),
				TextAlign=ContentAlignment.MiddleLeft
			};

			AramaKutusuHazirla(aramaKutusu , grid);
			aramaKutusu.Dock=DockStyle.Right;
			aramaKutusu.BackColor=aramaAlanArkaPlanRengi;
			aramaKutusu.Width=standartAramaKutusuBoyutu.Width;
			aramaKutusu.Height=standartAramaKutusuBoyutu.Height;
			aramaKutusu.Margin=Padding.Empty;

			grid.Dock=DockStyle.Fill;
			grid.Margin=Padding.Empty;

			ustPanel.Controls.Clear();
			ustPanel.Controls.Add(aramaKutusu);
			ustPanel.Controls.Add(baslikLabel);

			kutu.Controls.Clear();
			kutu.Controls.Add(grid);
			kutu.Controls.Add(ayirici);
			kutu.Controls.Add(ustPanel);
		}

		private void NotGridStiliniUygula ( DataGridView datagridview )
		{
			if(datagridview==null)
				return;

			DatagridviewSetting(datagridview);
			datagridview.BackgroundColor=Color.White;
			datagridview.DefaultCellStyle.BackColor=Color.White;
			datagridview.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(248 , 250 , 252);
			datagridview.BorderStyle=BorderStyle.None;
			datagridview.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;
			datagridview.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
			datagridview.GridColor=Color.FromArgb(226 , 232 , 240);
			datagridview.MultiSelect=false;
			datagridview.AllowUserToAddRows=false;
			datagridview.AllowUserToDeleteRows=false;
			datagridview.AllowUserToResizeRows=false;
			datagridview.RowHeadersVisible=false;
			datagridview.ColumnHeadersHeight=42;
			datagridview.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			datagridview.RowTemplate.Height=34;
			datagridview.DefaultCellStyle.Padding=new Padding(6 , 0 , 6 , 0);
			datagridview.DefaultCellStyle.Font=new Font("Segoe UI" , 9.5F , FontStyle.Regular);
			datagridview.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(13 , 148 , 136);
			datagridview.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;
			datagridview.DefaultCellStyle.SelectionBackColor=Color.FromArgb(204 , 251 , 241);
			datagridview.DefaultCellStyle.SelectionForeColor=Color.FromArgb(15 , 23 , 42);
			GridBasliklariniTurkceDuzenle(datagridview);
		}

		private void GroupBox20_NotDetayResize ( object sender , EventArgs e )
		{
			NotDetayAlaniniYenidenBoyutlandir();
		}

		private void NotDetayAlaniniYenidenBoyutlandir ()
		{
			if(groupBox20==null)
				return;

			if(_notDetayLayout!=null)
				_notDetayLayout.Width=Math.Max(0 , groupBox20.ClientSize.Width-groupBox20.Padding.Horizontal);

			if(checkBox1!=null)
				checkBox1.Height=28;

			if(button26!=null)
				button26.Height=40;
		}

		private void NotOzetKartlariniGuncelle ()
		{
			int bekleyenNotSayisi = NotGridSatirSayisiniGetir(dataGridView8);
			int okunanNotSayisi = NotGridSatirSayisiniGetir(dataGridView16);
			int toplamNotSayisi = bekleyenNotSayisi+okunanNotSayisi;
			DateTime? sonGuncellemeTarihi = NotGridEnYeniTarihiGetir(dataGridView8 , dataGridView16);

			if(_notBekleyenOzetDegerLabel!=null)
				_notBekleyenOzetDegerLabel.Text=bekleyenNotSayisi.ToString("N0" , CultureInfo.GetCultureInfo("tr-TR"));

			if(_notOkunanOzetDegerLabel!=null)
				_notOkunanOzetDegerLabel.Text=okunanNotSayisi.ToString("N0" , CultureInfo.GetCultureInfo("tr-TR"));

			if(_notToplamOzetDegerLabel!=null)
				_notToplamOzetDegerLabel.Text=toplamNotSayisi.ToString("N0" , CultureInfo.GetCultureInfo("tr-TR"));

			if(_notSonGuncellemeOzetDegerLabel!=null)
				_notSonGuncellemeOzetDegerLabel.Text=sonGuncellemeTarihi.HasValue
					? sonGuncellemeTarihi.Value.ToString("dd.MM.yyyy" , CultureInfo.GetCultureInfo("tr-TR"))
					: "--";
		}

		private int NotGridSatirSayisiniGetir ( DataGridView datagridview )
		{
			if(datagridview==null)
				return 0;

			return datagridview.Rows
				.Cast<DataGridViewRow>()
				.Count(satir => satir!=null&&!satir.IsNewRow);
		}

		private DateTime? NotGridEnYeniTarihiGetir ( params DataGridView[] gridler )
		{
			if(gridler==null||gridler.Length==0)
				return null;

			DateTime? enYeniTarih = null;
			foreach(DataGridView grid in gridler.Where(x => x!=null))
			{
				if(!grid.Columns.Contains("Tarih"))
					continue;

				foreach(DataGridViewRow satir in grid.Rows)
				{
					if(satir==null||satir.IsNewRow||satir.Cells["Tarih"].Value==null||satir.Cells["Tarih"].Value==DBNull.Value)
						continue;

					DateTime tarih;
					if(DateTime.TryParse(Convert.ToString(satir.Cells["Tarih"].Value) , out tarih))
					{
						if(!enYeniTarih.HasValue||tarih>enYeniTarih.Value)
							enYeniTarih=tarih;
					}
				}
			}

			return enYeniTarih;
		}

		private void NotOkunduDurumu_CheckedChanged ( object sender , EventArgs e )
		{
			if(_notSecimiYukleniyor||!_seciliNotId.HasValue||_seciliNotId.Value<=0)
				return;

			NotOkunduDurumunuDegistir(checkBox1!=null&&checkBox1.Checked);
		}

		private void NotKaydet_Click ( object sender , EventArgs e ) => NotKaydet();
		private void NotGuncelle_Click ( object sender , EventArgs e ) => NotGuncelle();
		private void NotSil_Click ( object sender , EventArgs e ) => NotSil();
		private void NotTemizle_Click ( object sender , EventArgs e ) => NotFormunuTemizle();

		private void NotKaydet ()
		{
			string baslik = textBox53?.Text?.Trim()??string.Empty;
			string notMetni = textBox73?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(baslik)&&string.IsNullOrWhiteSpace(notMetni))
			{
				MessageBox.Show("Kaydetmek için en azından başlık veya not alanını doldurun.");
				textBox53?.Focus();
				return;
			}

			try
			{
				EnsureNotAltyapi();
				DateTime tarih = NotTarihiniGetir();
				bool okundu = checkBox1?.Checked??false;

				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("INSERT INTO [Notlarim] ([Baslik], [Tarih], [NotMetni], [Okundu]) VALUES (?, ?, ?, ?)" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=string.IsNullOrWhiteSpace(baslik) ? (object)DBNull.Value : baslik;
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(notMetni) ? (object)DBNull.Value : notMetni;
						cmd.Parameters.Add("?" , OleDbType.Boolean).Value=okundu;
						cmd.ExecuteNonQuery();
					}
				}

				NotFormunuTemizle();
				NotlariListele();
				MessageBox.Show("Not kaydedildi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Not kaydedilemedi: "+ex.Message);
			}
		}

		private void NotGuncelle ()
		{
			if(!_seciliNotId.HasValue||_seciliNotId.Value<=0)
			{
				MessageBox.Show("Güncellemek için bir not seçin.");
				return;
			}

			string baslik = textBox53?.Text?.Trim()??string.Empty;
			string notMetni = textBox73?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(baslik)&&string.IsNullOrWhiteSpace(notMetni))
			{
				MessageBox.Show("Güncellemek için en azından başlık veya not alanını doldurun.");
				textBox53?.Focus();
				return;
			}

			try
			{
				EnsureNotAltyapi();
				DateTime tarih = NotTarihiniGetir();
				bool okundu = checkBox1?.Checked??false;

				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("UPDATE [Notlarim] SET [Baslik]=?, [Tarih]=?, [NotMetni]=?, [Okundu]=? WHERE [NotID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=string.IsNullOrWhiteSpace(baslik) ? (object)DBNull.Value : baslik;
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(notMetni) ? (object)DBNull.Value : notMetni;
						cmd.Parameters.Add("?" , OleDbType.Boolean).Value=okundu;
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_seciliNotId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				NotFormunuTemizle();
				NotlariListele();
				MessageBox.Show("Not güncellendi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Not güncellenemedi: "+ex.Message);
			}
		}

		private void NotSil ()
		{
			if(!_seciliNotId.HasValue||_seciliNotId.Value<=0)
			{
				MessageBox.Show("Silmek için bir not seçin.");
				return;
			}

			if(MessageBox.Show("Seçili not silinsin mi?" , "Not Sil" , MessageBoxButtons.YesNo , MessageBoxIcon.Question)!=DialogResult.Yes)
				return;

			try
			{
				EnsureNotAltyapi();
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [Notlarim] WHERE [NotID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_seciliNotId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				NotFormunuTemizle();
				NotlariListele();
				MessageBox.Show("Not silindi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Not silinemedi: "+ex.Message);
			}
		}

		private void NotFormunuTemizle ( bool gridSecimleriniTemizle = true )
		{
			_notSecimiYukleniyor=true;
			try
			{
				_seciliNotId=null;
				if(textBox53!=null) textBox53.Clear();
				if(textBox57!=null) textBox57.Text=DateTime.Now.ToString("dd.MM.yyyy HH:mm" , CultureInfo.GetCultureInfo("tr-TR"));
				if(textBox73!=null) textBox73.Clear();
				if(checkBox1!=null) checkBox1.Checked=false;
				if(gridSecimleriniTemizle)
				{
					if(dataGridView8!=null) dataGridView8.ClearSelection();
					if(dataGridView16!=null) dataGridView16.ClearSelection();
				}
			}
			finally
			{
				_notSecimiYukleniyor=false;
			}

			NotButonDurumunuGuncelle();
			textBox53?.Focus();
		}

		private void NotButonDurumunuGuncelle ()
		{
			bool secimVar = _seciliNotId.HasValue&&_seciliNotId.Value>0;
			if(button29!=null) button29.Enabled=true;
			if(button27!=null) button27.Enabled=secimVar;
			if(button28!=null) button28.Enabled=secimVar;
		}

		private void NotlariListele ()
		{
			EnsureNotAltyapi();
			NotGridVerisiniYukle(dataGridView8 , false);
			NotGridVerisiniYukle(dataGridView16 , true);
			NotOzetKartlariniGuncelle();
			AnaSayfaBugunYapilacaklarListele();
			NotButonDurumunuGuncelle();
		}

		private void NotOkunduDurumunuDegistir ( bool okundu )
		{
			if(!_seciliNotId.HasValue||_seciliNotId.Value<=0)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("UPDATE [Notlarim] SET [Okundu]=? WHERE [NotID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Boolean).Value=okundu;
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_seciliNotId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				int notId = _seciliNotId.Value;
				NotlariListele();
				NotGriddeKaydiSec(notId , okundu);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Not durumu güncellenemedi: "+ex.Message);
			}
		}

		private void NotGridVerisiniYukle ( DataGridView datagridview , bool okundu )
		{
			if(datagridview==null)
				return;

			DataTable dt = new DataTable();
			Exception sonHata = null;
			for(int deneme = 0 ; deneme<2 ; deneme++)
			{
				try
				{
					dt=new DataTable();
					using(OleDbConnection conn = new OleDbConnection(connStr))
					{
						conn.Open();
						string sorgu = @"SELECT
									[NotID],
									IIF([Baslik] IS NULL, '', [Baslik]) AS Baslik,
									IIF([Tarih] IS NULL, Now(), [Tarih]) AS Tarih,
									IIF([NotMetni] IS NULL, '', [NotMetni]) AS NotMetni,
									IIF([Okundu] IS NULL, False, [Okundu]) AS Okundu
								FROM [Notlarim]
								WHERE IIF([Okundu] IS NULL, False, [Okundu])=?
								ORDER BY IIF([Tarih] IS NULL, #01/01/2000#, [Tarih]) DESC, [NotID] DESC";
						using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						{
							da.SelectCommand.Parameters.Add("?" , OleDbType.Boolean).Value=okundu;
							da.Fill(dt);
						}
					}

					sonHata=null;
					break;
				}
				catch(Exception ex)
				{
					sonHata=ex;
					if(deneme==0&&NotTablosuBulunamadiMi(ex))
					{
						EnsureNotAltyapi();
						continue;
					}

					break;
				}
			}

			if(sonHata!=null)
				MessageBox.Show((okundu ? "Okunan" : "Okunmayan")+" notlar yüklenemedi: "+sonHata.Message);

			datagridview.DataSource=dt;
			NotGridStiliniUygula(datagridview);
			NotGridKolonlariniDuzenle(datagridview);
			datagridview.ClearSelection();
		}

		private void NotGriddeKaydiSec ( int notId , bool okundu )
		{
			DataGridView hedefGrid = okundu ? dataGridView16 : dataGridView8;
			if(hedefGrid==null)
				return;

			foreach(DataGridViewRow satir in hedefGrid.Rows)
			{
				if(satir.IsNewRow||satir.Cells["NotID"].Value==null||satir.Cells["NotID"].Value==DBNull.Value)
					continue;

				if(Convert.ToInt32(satir.Cells["NotID"].Value)==notId)
				{
					hedefGrid.ClearSelection();
					satir.Selected=true;
					if(hedefGrid.Columns.Contains("Baslik"))
						hedefGrid.CurrentCell=satir.Cells["Baslik"];
					NotSeciminiYukle(satir , hedefGrid);
					break;
				}
			}
		}

		private void NotGridKolonlariniDuzenle ( DataGridView datagridview )
		{
			if(datagridview==null)
				return;

			if(datagridview.Columns.Contains("NotID"))
				datagridview.Columns["NotID"].Visible=false;
			if(datagridview.Columns.Contains("Okundu"))
				datagridview.Columns["Okundu"].Visible=false;
			if(datagridview.Columns.Contains("Baslik"))
			{
				datagridview.Columns["Baslik"].HeaderText="BAŞLIK";
				datagridview.Columns["Baslik"].FillWeight=24F;
			}
			if(datagridview.Columns.Contains("Tarih"))
			{
				datagridview.Columns["Tarih"].HeaderText="TARİH";
				datagridview.Columns["Tarih"].DefaultCellStyle.Format="dd.MM.yyyy HH:mm";
				datagridview.Columns["Tarih"].FillWeight=18F;
			}
			if(datagridview.Columns.Contains("NotMetni"))
			{
				datagridview.Columns["NotMetni"].HeaderText="NOT";
				datagridview.Columns["NotMetni"].FillWeight=58F;
			}
		}

		private void NotGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0)
				return;

			DataGridView datagridview = sender as DataGridView;
			if(datagridview==null||e.RowIndex>=datagridview.Rows.Count)
				return;

			NotSeciminiYukle(datagridview.Rows[e.RowIndex] , datagridview);
		}

		private void NotSeciminiYukle ( DataGridViewRow satir , DataGridView kaynakGrid )
		{
			if(satir==null||satir.IsNewRow)
				return;

			_notSecimiYukleniyor=true;
			try
			{
				_seciliNotId=Convert.ToInt32(satir.Cells["NotID"].Value);
				if(textBox53!=null) textBox53.Text=Convert.ToString(satir.Cells["Baslik"].Value)??string.Empty;
				if(textBox73!=null) textBox73.Text=Convert.ToString(satir.Cells["NotMetni"].Value)??string.Empty;

				DateTime tarih = DateTime.Now;
				if(satir.Cells["Tarih"].Value!=null&&satir.Cells["Tarih"].Value!=DBNull.Value)
					tarih=Convert.ToDateTime(satir.Cells["Tarih"].Value);
				if(textBox57!=null)
					textBox57.Text=tarih.ToString("dd.MM.yyyy HH:mm" , CultureInfo.GetCultureInfo("tr-TR"));

				bool okundu = satir.Cells["Okundu"].Value!=null&&satir.Cells["Okundu"].Value!=DBNull.Value&&Convert.ToBoolean(satir.Cells["Okundu"].Value);
				if(checkBox1!=null)
					checkBox1.Checked=okundu;

				if(kaynakGrid==dataGridView8&&dataGridView16!=null)
					dataGridView16.ClearSelection();
				else if(kaynakGrid==dataGridView16&&dataGridView8!=null)
					dataGridView8.ClearSelection();
			}
			finally
			{
				_notSecimiYukleniyor=false;
			}

			NotButonDurumunuGuncelle();
		}

		private void NotTarihTextBox_Leave ( object sender , EventArgs e )
		{
			if(_notSecimiYukleniyor||textBox57==null)
				return;

			string metin = textBox57.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(metin))
			{
				textBox57.Text=DateTime.Now.ToString("dd.MM.yyyy HH:mm" , CultureInfo.GetCultureInfo("tr-TR"));
				return;
			}

			if(DateTime.TryParse(metin , CultureInfo.GetCultureInfo("tr-TR") , DateTimeStyles.AllowWhiteSpaces , out DateTime tarih))
				textBox57.Text=tarih.ToString("dd.MM.yyyy HH:mm" , CultureInfo.GetCultureInfo("tr-TR"));
		}

		private DateTime NotTarihiniGetir ()
		{
			string metin = textBox57?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(metin))
				return DateTime.Now;

			CultureInfo kultur = CultureInfo.GetCultureInfo("tr-TR");
			string[] formatlar =
			{
				"dd.MM.yyyy HH:mm",
				"dd.MM.yyyy HH:mm:ss",
				"dd.MM.yyyy",
				"g",
				"G"
			};

			if(DateTime.TryParseExact(metin , formatlar , kultur , DateTimeStyles.AllowWhiteSpaces , out DateTime tarih))
				return tarih;
			if(DateTime.TryParse(metin , kultur , DateTimeStyles.AllowWhiteSpaces , out tarih))
				return tarih;

			throw new InvalidOperationException("Tarih alanına geçerli bir tarih girin. Örnek: 25.03.2026 14:30");
		}

		private bool NotTablosuBulunamadiMi ( Exception ex )
		{
			string hataMetni = ex?.Message??string.Empty;
			return hataMetni.IndexOf("Notlarim" , StringComparison.OrdinalIgnoreCase)>=0||
				   hataMetni.IndexOf("giriş tablosunu veya 'Notlarim' sorgusunu bulamıyor" , StringComparison.OrdinalIgnoreCase)>=0;
		}

		private void EnsureCariDurumCariTipCombo ()
		{
			if(comboBox12!=null) return;

			comboBox12=new ComboBox();
			comboBox12.Name="comboBox12";
			comboBox12.FormattingEnabled=true;
			comboBox12.DropDownStyle=ComboBoxStyle.DropDownList;

			if(comboBox13!=null)
			{
				comboBox12.Font=comboBox13.Font;
				comboBox12.Size=comboBox13.Size;
				comboBox12.ItemHeight=comboBox13.ItemHeight;
				comboBox12.Margin=comboBox13.Margin;
			}
			else
			{
				comboBox12.Font=new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , 162);
				comboBox12.Size=new Size(258 , 28);
				comboBox12.ItemHeight=20;
				comboBox12.Margin=new Padding(3);
			}
		}

		private void BaglaCariTipIslemEventleri ()
		{
			button7.Click-=CariTipKaydet_Click;
			button7.Click+=CariTipKaydet_Click;
			button6.Click-=CariTipSil_Click;
			button6.Click+=CariTipSil_Click;
			button5.Click-=CariTipGuncelle_Click;
			button5.Click+=CariTipGuncelle_Click;
			button4.Click-=CariTipTemizle_Click;
			button4.Click+=CariTipTemizle_Click;

			dataGridView4.CellClick-=DataGridView4_CellClick;
			dataGridView4.CellClick+=DataGridView4_CellClick;
		}

		private void BaglaCariDurumIslemEventleri ()
		{
			button20.Click-=CariDurumSil_Click;
			button20.Click+=CariDurumSil_Click;
			button19.Click-=CariDurumGuncelle_Click;
			button19.Click+=CariDurumGuncelle_Click;
			button11.Click-=CariDurumTemizle_Click;
			button11.Click+=CariDurumTemizle_Click;

			dataGridView6.CellClick-=DataGridView6_CellClick;
			dataGridView6.CellClick+=DataGridView6_CellClick;
		}

		private void BaglaNotIslemEventleri ()
		{
			if(button29!=null)
			{
				button29.Click-=NotKaydet_Click;
				button29.Click+=NotKaydet_Click;
			}

			if(button28!=null)
			{
				button28.Click-=NotSil_Click;
				button28.Click+=NotSil_Click;
			}

			if(button27!=null)
			{
				button27.Click-=NotGuncelle_Click;
				button27.Click+=NotGuncelle_Click;
			}

			if(button26!=null)
			{
				button26.Click-=NotTemizle_Click;
				button26.Click+=NotTemizle_Click;
			}

			if(dataGridView8!=null)
			{
				dataGridView8.CellClick-=NotGrid_CellClick;
				dataGridView8.CellClick+=NotGrid_CellClick;
			}

			if(dataGridView16!=null)
			{
				dataGridView16.CellClick-=NotGrid_CellClick;
				dataGridView16.CellClick+=NotGrid_CellClick;
			}

			if(textBox57!=null)
			{
				textBox57.Leave-=NotTarihTextBox_Leave;
				textBox57.Leave+=NotTarihTextBox_Leave;
			}

			if(checkBox1!=null)
			{
				checkBox1.CheckedChanged-=NotOkunduDurumu_CheckedChanged;
				checkBox1.CheckedChanged+=NotOkunduDurumu_CheckedChanged;
			}
		}

		private void EnsureCariDurumAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					_cariDurumCariTipKolonuVar=KolonVarMi(conn , "CariDurumlari" , "CariTipID");
					if(!_cariDurumCariTipKolonuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE CariDurumlari ADD COLUMN CariTipID INTEGER" , conn))
								cmd.ExecuteNonQuery();
							_cariDurumCariTipKolonuVar=true;
						}
						catch
						{
							_cariDurumCariTipKolonuVar=false;
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari durum altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private bool KolonVarMi ( OleDbConnection conn , string tablo , string kolon )
		{
			DataTable dt = conn.GetSchema("Columns" , new string[] { null , null , tablo , kolon });
			return dt.Rows.Count>0;
		}

		private int? KolonVeriTuruGetir ( OleDbConnection conn , string tablo , string kolon )
		{
			DataTable dt = conn.GetSchema("Columns" , new string[] { null , null , tablo , kolon });
			if(dt.Rows.Count==0)
				return null;

			object veriTuru = dt.Rows[0]["DATA_TYPE"];
			if(veriTuru==null||veriTuru==DBNull.Value)
				return null;

			return Convert.ToInt32(veriTuru);
		}

		private bool TabloVarMi ( OleDbConnection conn , string tablo )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tablo))
				return false;

			DataTable dt = conn.GetSchema("Tables");
			return dt.Rows
				.Cast<DataRow>()
				.Any(r =>
					string.Equals(Convert.ToString(r["TABLE_NAME"]) , tablo , StringComparison.OrdinalIgnoreCase)&&
					string.Equals(Convert.ToString(r["TABLE_TYPE"]) , "TABLE" , StringComparison.OrdinalIgnoreCase));
		}

		private void EnsurePersonelAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					_departmanMaasKolonuVar=KolonVarMi(conn , "Departmanlar" , "VarsayilanMaas");
					if(!_departmanMaasKolonuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Departmanlar] ADD COLUMN [VarsayilanMaas] CURRENCY" , conn))
								cmd.ExecuteNonQuery();
							_departmanMaasKolonuVar=true;
						}
						catch
						{
							_departmanMaasKolonuVar=false;
						}
					}

					_personelDurumKolonuVar=KolonVarMi(conn , "Personeller" , "PersonelDurumu");
					if(!_personelDurumKolonuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Personeller] ADD COLUMN [PersonelDurumu] TEXT(50)" , conn))
								cmd.ExecuteNonQuery();
							_personelDurumKolonuVar=true;
						}
						catch
						{
							_personelDurumKolonuVar=false;
						}
					}

					_personelOdemeTablosuVar=TabloVarMi(conn , "PersonelOdemeleri");
					if(!_personelOdemeTablosuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand(
								"CREATE TABLE [PersonelOdemeleri] ([OdemeID] AUTOINCREMENT, [PersonelID] LONG, [DonemID] LONG, [OdemeTarihi] DATETIME, [OdenenTutar] CURRENCY, [Aciklama] LONGTEXT)" ,
								conn))
								cmd.ExecuteNonQuery();
							_personelOdemeTablosuVar=true;
						}
						catch
						{
							_personelOdemeTablosuVar=false;
						}
					}

					if(_personelOdemeTablosuVar)
					{
						try
						{
							if(!KolonVarMi(conn , "PersonelOdemeleri" , "PersonelID"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelOdemeleri] ADD COLUMN [PersonelID] LONG" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelOdemeleri" , "DonemID"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelOdemeleri] ADD COLUMN [DonemID] LONG" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelOdemeleri" , "OdemeTarihi"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelOdemeleri] ADD COLUMN [OdemeTarihi] DATETIME" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelOdemeleri" , "OdenenTutar"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelOdemeleri] ADD COLUMN [OdenenTutar] CURRENCY" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelOdemeleri" , "Aciklama"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelOdemeleri] ADD COLUMN [Aciklama] LONGTEXT" , conn))
									cmd.ExecuteNonQuery();
							}
						}
						catch
						{
							_personelOdemeTablosuVar=false;
						}
					}

					_personelMaasDonemTablosuVar=TabloVarMi(conn , "PersonelMaasDonemleri");
					if(!_personelMaasDonemTablosuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand(
								"CREATE TABLE [PersonelMaasDonemleri] ([DonemID] AUTOINCREMENT, [PersonelID] LONG, [DonemBaslangic] DATETIME, [DonemBitis] DATETIME, [MaasTutari] CURRENCY)" ,
								conn))
								cmd.ExecuteNonQuery();
							_personelMaasDonemTablosuVar=true;
						}
						catch
						{
							_personelMaasDonemTablosuVar=false;
						}
					}

					if(_personelMaasDonemTablosuVar)
					{
						try
						{
							if(!KolonVarMi(conn , "PersonelMaasDonemleri" , "PersonelID"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelMaasDonemleri] ADD COLUMN [PersonelID] LONG" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelMaasDonemleri" , "DonemBaslangic"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelMaasDonemleri] ADD COLUMN [DonemBaslangic] DATETIME" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelMaasDonemleri" , "DonemBitis"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelMaasDonemleri] ADD COLUMN [DonemBitis] DATETIME" , conn))
									cmd.ExecuteNonQuery();
							}

							if(!KolonVarMi(conn , "PersonelMaasDonemleri" , "MaasTutari"))
							{
								using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [PersonelMaasDonemleri] ADD COLUMN [MaasTutari] CURRENCY" , conn))
									cmd.ExecuteNonQuery();
							}
						}
						catch
						{
							_personelMaasDonemTablosuVar=false;
						}
					}

					if(_personelOdemeTablosuVar&&_personelMaasDonemTablosuVar)
					{
						PersonelMaasDonemleriniTekillestir(conn);
						PersonelOdemelerineDonemAta(conn);
						PersonelMaasDonemleriniTekillestir(conn);
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Personel altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void EnsureDepartmanVePersonelVerileri ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					DataTable departmanlar = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter("SELECT DepartmanID, DepartmanAdi FROM [Departmanlar]" , conn))
						da.Fill(departmanlar);

					string[] zorunluDepartmanlar = { "USTA" , "KALFA" , "ÇIRAK" , "MUHASEBE" };
					foreach(string departman in zorunluDepartmanlar)
					{
						bool mevcut = departmanlar.Rows
							.Cast<DataRow>()
							.Any(r => string.Equals(
								KarsilastirmaMetniHazirla(Convert.ToString(r["DepartmanAdi"])) ,
								KarsilastirmaMetniHazirla(departman) ,
								StringComparison.Ordinal));

						if(mevcut)
							continue;

						string sorgu = _departmanMaasKolonuVar
							? "INSERT INTO [Departmanlar] ([DepartmanAdi], [VarsayilanMaas]) VALUES (?, ?)"
							: "INSERT INTO [Departmanlar] ([DepartmanAdi]) VALUES (?)";
						using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
						{
							cmd.Parameters.AddWithValue("?" , departman);
							if(_departmanMaasKolonuVar)
								cmd.Parameters.Add("?" , OleDbType.Currency).Value=0m;
							cmd.ExecuteNonQuery();
						}
					}

					if(_personelDurumKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [Personeller] SET [PersonelDurumu]=IIF([AktifMi], 'AKTİF', 'PASİF') WHERE [PersonelDurumu] IS NULL OR [PersonelDurumu]=''" , conn))
							cmd.ExecuteNonQuery();
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Departman/personel başlangıç verileri oluşturulamadı: "+ex.Message);
			}
		}

		private void PersonelArayuzunuHazirla ()
		{
			if(groupBox27==null||flowLayoutPanel12==null)
				return;

			groupBox27.Text="Personel İşlemleri";
			label84.Text="PERSONEL ID :";
			label72.Text="AD SOYAD :";
			label71.Text="TELEFON :";
			label138.Text="DEPARTMAN :";
			label207.Text="AYLIK MAAŞ :";
			label208.Text="DURUM :";
			label70.Visible=false;

			textBox54.ReadOnly=true;
			textBox54.BackColor=SystemColors.ControlLight;
			textBox99.Text=string.IsNullOrWhiteSpace(textBox99.Text) ? "0,00" : textBox99.Text;
			if(textBox100!=null) textBox100.Text=string.Empty;
			if(textBox101!=null) textBox101.Text="0,00";
			if(textBox102!=null) textBox102.Text=string.Empty;
			if(textBox103!=null) textBox103.Text="0,00";
			PersonelOdemeAksiyonDurumunuGuncelle();
			comboBox7.DropDownStyle=ComboBoxStyle.DropDownList;
			comboBox1.DropDownStyle=ComboBoxStyle.DropDownList;

			DepartmanComboYenile();
			PersonelDurumComboYenile();
			PersonelBakiyeArayuzunuHazirla();
			PersonelTemizle();
		}

		private void PersonelBakiyeArayuzunuHazirla ()
		{
			if(panel13==null||dataGridView25==null)
				return;

			if(_personelBakiyeSecimLabel==null)
			{
				_personelBakiyeSecimLabel=new Label();
				_personelBakiyeSecimLabel.AutoSize=true;
				_personelBakiyeSecimLabel.Font=new Font("Microsoft Sans Serif" , 9F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_personelBakiyeSecimLabel.Name="labelPersonelBakiyeSecim";
				_personelBakiyeSecimLabel.Text="PERSONEL :";
				panel13.Controls.Add(_personelBakiyeSecimLabel);
			}

			if(_personelBakiyeSecimComboBox==null)
			{
				_personelBakiyeSecimComboBox=new ComboBox();
				_personelBakiyeSecimComboBox.DropDownStyle=ComboBoxStyle.DropDownList;
				_personelBakiyeSecimComboBox.Font=new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_personelBakiyeSecimComboBox.Name="comboBoxPersonelBakiyeSecim";
				_personelBakiyeSecimComboBox.Size=new Size(248 , 28);
				panel13.Controls.Add(_personelBakiyeSecimComboBox);
			}

			if(_personelBakiyeDonemLabel==null)
			{
				_personelBakiyeDonemLabel=new Label();
				_personelBakiyeDonemLabel.AutoSize=false;
				_personelBakiyeDonemLabel.Font=new Font("Microsoft Sans Serif" , 8.5F , FontStyle.Italic , GraphicsUnit.Point , ((byte)(162)));
				_personelBakiyeDonemLabel.Name="labelPersonelBakiyeDonem";
				_personelBakiyeDonemLabel.Size=new Size(376 , 22);
				_personelBakiyeDonemLabel.TextAlign=ContentAlignment.MiddleLeft;
				_personelBakiyeDonemLabel.Text="DÖNEM : -";
				panel13.Controls.Add(_personelBakiyeDonemLabel);
			}

			if(_personelBakiyeTarihLabel==null)
			{
				_personelBakiyeTarihLabel=new Label();
				_personelBakiyeTarihLabel.AutoSize=true;
				_personelBakiyeTarihLabel.Font=new Font("Microsoft Sans Serif" , 9F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_personelBakiyeTarihLabel.Name="labelPersonelBakiyeTarih";
				_personelBakiyeTarihLabel.Text="ÖDEME TARİH/SAAT :";
				panel13.Controls.Add(_personelBakiyeTarihLabel);
			}

			if(_personelBakiyeTarihPicker==null)
			{
				_personelBakiyeTarihPicker=new DateTimePicker();
				_personelBakiyeTarihPicker.Format=DateTimePickerFormat.Custom;
				_personelBakiyeTarihPicker.CustomFormat="dd.MM.yyyy HH:mm";
				_personelBakiyeTarihPicker.Font=new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_personelBakiyeTarihPicker.Name="dateTimePickerPersonelOdemeTarihi";
				_personelBakiyeTarihPicker.Size=new Size(248 , 27);
				_personelBakiyeTarihPicker.Value=PersonelVarsayilanOdemeTarihiGetir();
				panel13.Controls.Add(_personelBakiyeTarihPicker);
			}

			PersonelOdemeAksiyonPaneliniHazirla();

			label209.Text="PERSONEL ÖDEME TAKİP";
			panel13.Size=new Size(409 , 398);

			_personelBakiyeSecimLabel.Location=new Point(12 , 48);
			_personelBakiyeSecimComboBox.Location=new Point(140 , 44);
			_personelBakiyeDonemLabel.Location=new Point(12 , 78);
			_personelBakiyeTarihLabel.Location=new Point(12 , 108);
			_personelBakiyeTarihPicker.Location=new Point(140 , 104);

			label212.Location=new Point(12 , 148);
			textBox103.Location=new Point(140 , 144);
			label210.Location=new Point(12 , 184);
			textBox100.Location=new Point(140 , 180);
			label213.Location=new Point(12 , 220);
			textBox102.Location=new Point(140 , 216);
			label211.Location=new Point(12 , 272);
			textBox101.Location=new Point(140 , 268);
			if(_personelBakiyeAksiyonPaneli!=null)
			{
				_personelBakiyeAksiyonPaneli.Location=new Point(12 , 308);
				_personelBakiyeAksiyonPaneli.Size=new Size(376 , 84);
			}

			dataGridView25.Location=new Point(21 , panel13.Bottom+14);
			if(tabPage13!=null)
			{
				int yeniYukseklik = Math.Max(220 , tabPage13.ClientSize.Height-dataGridView25.Top-25);
				dataGridView25.Size=new Size(dataGridView25.Width , yeniYukseklik);
			}

			dataGridView25.ReadOnly=true;
			dataGridView25.MultiSelect=false;
			dataGridView25.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
			PersonelOdemeTablosunuHazirla();
		}

		private void PersonelOdemeAksiyonButonunuAyarla ( Button buton , string metin , string imageKey )
		{
			if(buton==null)
				return;

			ToptanciBakiyeAksiyonButonunuAyarla(buton , metin , imageKey);
			buton.Dock=DockStyle.Fill;
		}

		private void PersonelOdemeAksiyonPaneliniHazirla ()
		{
			if(panel13==null||button75==null)
				return;

			if(_personelBakiyeAksiyonPaneli==null)
			{
				_personelBakiyeAksiyonPaneli=new TableLayoutPanel();
				_personelBakiyeAksiyonPaneli.ColumnCount=2;
				_personelBakiyeAksiyonPaneli.RowCount=2;
				_personelBakiyeAksiyonPaneli.BackColor=Color.Transparent;
				_personelBakiyeAksiyonPaneli.Margin=Padding.Empty;
				_personelBakiyeAksiyonPaneli.Padding=Padding.Empty;
				panel13.Controls.Add(_personelBakiyeAksiyonPaneli);
			}

			_personelBakiyeAksiyonPaneli.ColumnStyles.Clear();
			_personelBakiyeAksiyonPaneli.RowStyles.Clear();
			_personelBakiyeAksiyonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50F));
			_personelBakiyeAksiyonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50F));
			_personelBakiyeAksiyonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));
			_personelBakiyeAksiyonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));

			if(_personelOdemeEkleButonu==null)
			{
				_personelOdemeEkleButonu=new Button();
				panel13.Controls.Add(_personelOdemeEkleButonu);
			}
			if(_personelOdemeGuncelleButonu==null)
			{
				_personelOdemeGuncelleButonu=new Button();
				panel13.Controls.Add(_personelOdemeGuncelleButonu);
			}
			if(_personelOdemeSilButonu==null)
			{
				_personelOdemeSilButonu=new Button();
				panel13.Controls.Add(_personelOdemeSilButonu);
			}

			PersonelOdemeAksiyonButonunuAyarla(_personelOdemeEkleButonu , "Temizle" , "Broom.png");
			PersonelOdemeAksiyonButonunuAyarla(button75 , "Kaydet" , "Save.png");
			PersonelOdemeAksiyonButonunuAyarla(_personelOdemeGuncelleButonu , "Guncelle" , "Update User.png");
			PersonelOdemeAksiyonButonunuAyarla(_personelOdemeSilButonu , "Sil" , "Delete Database.png");

			foreach(Control kontrol in new Control[] { _personelOdemeEkleButonu , button75 , _personelOdemeGuncelleButonu , _personelOdemeSilButonu })
			{
				if(kontrol.Parent!=_personelBakiyeAksiyonPaneli)
					_personelBakiyeAksiyonPaneli.Controls.Add(kontrol);
			}

			_personelBakiyeAksiyonPaneli.Controls.Clear();
			_personelBakiyeAksiyonPaneli.Controls.Add(_personelOdemeEkleButonu , 0 , 0);
			_personelBakiyeAksiyonPaneli.Controls.Add(button75 , 1 , 0);
			_personelBakiyeAksiyonPaneli.Controls.Add(_personelOdemeGuncelleButonu , 0 , 1);
			_personelBakiyeAksiyonPaneli.Controls.Add(_personelOdemeSilButonu , 1 , 1);
			PersonelOdemeButonMetniniGuncelle();
			PersonelOdemeAksiyonDurumunuGuncelle();
		}

		private void KurDepartmanSekmesi ()
		{
			if(tabPage16==null||_departmanYonetimGrid!=null)
				return;

			tabPage16.Controls.Clear();
			tabPage16.Text="Departmanlar";

			TableLayoutPanel anaLayout = new TableLayoutPanel();
			anaLayout.Dock=DockStyle.Fill;
			anaLayout.ColumnCount=2;
			anaLayout.RowCount=1;
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 32f));
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 68f));

			GroupBox girisKutusu = new GroupBox();
			girisKutusu.Text="Departman Tanımı";
			girisKutusu.Dock=DockStyle.Fill;

			Label idLabel = new Label();
			idLabel.Text="DEPARTMAN ID :";
			idLabel.TextAlign=ContentAlignment.MiddleRight;
			idLabel.Location=new Point(16 , 44);
			idLabel.Size=new Size(130 , 24);

			_departmanYonetimIdTextBox=new TextBox();
			_departmanYonetimIdTextBox.Location=new Point(160 , 40);
			_departmanYonetimIdTextBox.Size=new Size(240 , 28);
			_departmanYonetimIdTextBox.ReadOnly=true;
			_departmanYonetimIdTextBox.BackColor=SystemColors.ControlLight;

			Label adLabel = new Label();
			adLabel.Text="DEPARTMAN :";
			adLabel.TextAlign=ContentAlignment.MiddleRight;
			adLabel.Location=new Point(16 , 84);
			adLabel.Size=new Size(130 , 24);

			_departmanYonetimAdiTextBox=new TextBox();
			_departmanYonetimAdiTextBox.Location=new Point(160 , 80);
			_departmanYonetimAdiTextBox.Size=new Size(240 , 28);

			Label maasLabel = new Label();
			maasLabel.Text="VARSAYILAN MAAŞ :";
			maasLabel.TextAlign=ContentAlignment.MiddleRight;
			maasLabel.Location=new Point(16 , 124);
			maasLabel.Size=new Size(130 , 24);

			_departmanYonetimMaasTextBox=new TextBox();
			_departmanYonetimMaasTextBox.Location=new Point(160 , 120);
			_departmanYonetimMaasTextBox.Size=new Size(240 , 28);
			_departmanYonetimMaasTextBox.Text="0,00";
			_departmanYonetimMaasTextBox.KeyPress+=SepetSayisal_KeyPress;

			Button kaydetButonu = new Button();
			kaydetButonu.Text="KAYDET";
			kaydetButonu.Location=new Point(160 , 180);
			kaydetButonu.Size=new Size(240 , 42);
			kaydetButonu.Click+=DepartmanYonetimKaydet_Click;

			Button guncelleButonu = new Button();
			guncelleButonu.Text="GÜNCELLE";
			guncelleButonu.Location=new Point(160 , 232);
			guncelleButonu.Size=new Size(240 , 42);
			guncelleButonu.Click+=DepartmanYonetimGuncelle_Click;

			Button silButonu = new Button();
			silButonu.Text="SİL";
			silButonu.Location=new Point(160 , 284);
			silButonu.Size=new Size(240 , 42);
			silButonu.Click+=DepartmanYonetimSil_Click;

			Button temizleButonu = new Button();
			temizleButonu.Text="TEMİZLE";
			temizleButonu.Location=new Point(160 , 336);
			temizleButonu.Size=new Size(240 , 42);
			temizleButonu.Click+=DepartmanYonetimTemizle_Click;

			girisKutusu.Controls.Add(idLabel);
			girisKutusu.Controls.Add(_departmanYonetimIdTextBox);
			girisKutusu.Controls.Add(adLabel);
			girisKutusu.Controls.Add(_departmanYonetimAdiTextBox);
			girisKutusu.Controls.Add(maasLabel);
			girisKutusu.Controls.Add(_departmanYonetimMaasTextBox);
			girisKutusu.Controls.Add(kaydetButonu);
			girisKutusu.Controls.Add(guncelleButonu);
			girisKutusu.Controls.Add(silButonu);
			girisKutusu.Controls.Add(temizleButonu);

			GroupBox listeKutusu = new GroupBox();
			listeKutusu.Text="Departman Listesi";
			listeKutusu.Dock=DockStyle.Fill;

			_departmanYonetimGrid=new DataGridView();
			_departmanYonetimGrid.Dock=DockStyle.Fill;
			_departmanYonetimGrid.CellClick+=DepartmanYonetimGrid_CellClick;
			listeKutusu.Controls.Add(_departmanYonetimGrid);

			anaLayout.Controls.Add(girisKutusu , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 1 , 0);
			tabPage16.Controls.Add(anaLayout);

			DatagridviewSetting(_departmanYonetimGrid);
			DepartmanYonetimListele();
			DepartmanYonetimTemizle();
		}

		private void KurYapilanIsSekmesi ()
		{
			if(_yapilanIsTabPage!=null)
				return;

			TabPage hedefSekme = tabPage25;
			if(hedefSekme==null)
			{
				if(tabControl1!=null)
				{
					hedefSekme=new TabPage("Yapılan İşler");
					tabControl1.TabPages.Add(hedefSekme);
				}
				else if(tabControl2!=null)
				{
					hedefSekme=new TabPage("Yapılan İşler");
					int eklenecekIndex = Math.Min(3 , tabControl2.TabPages.Count);
					tabControl2.TabPages.Insert(eklenecekIndex , hedefSekme);
				}
			}

			if(hedefSekme==null)
				return;

			_yapilanIsTabPage=hedefSekme;
			_yapilanIsTabPage.SuspendLayout();
			_yapilanIsTabPage.Padding=new Padding(12);
			_yapilanIsTabPage.UseVisualStyleBackColor=false;
			_yapilanIsTabPage.BackColor=Color.FromArgb(245 , 247 , 250);
			_yapilanIsTabPage.Controls.Clear();
			_yapilanIsFiyatTextBox=null;
			_yapilanIsToplamTextBox=null;
			_yapilanIsToplamLabel=null;
			_yapilanIsOrtalamaLabel=null;
			_yapilanIsSonKayitLabel=null;

			Panel anaPanel = new Panel
			{
				Dock=DockStyle.Fill,
				Padding=new Padding(8),
				BackColor=Color.FromArgb(245 , 247 , 250)
			};

			TableLayoutPanel icerikLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=1,
				Padding=Padding.Empty
			};
			icerikLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 68f));
			icerikLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 32f));

			_yapilanIsListeGroupBox=new GroupBox
			{
				Text="Yapılan İş Listesi",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(12)
			};

			Panel aramaPaneli = new Panel
			{
				Dock=DockStyle.Top,
				Height=42,
				Padding=new Padding(0 , 0 , 0 , 8)
			};

			_yapilanIsAramaTextBox=new TextBox
			{
				Dock=DockStyle.Right,
				Width=260,
				Text="Ara"
			};
			aramaPaneli.Controls.Add(_yapilanIsAramaTextBox);

			_yapilanIsGrid=new DataGridView
			{
				Dock=DockStyle.Fill,
				ReadOnly=true,
				MultiSelect=false,
				SelectionMode=DataGridViewSelectionMode.FullRowSelect,
				AllowUserToAddRows=false,
				AllowUserToDeleteRows=false
			};
			_yapilanIsGrid.CellClick-=YapilanIsGrid_CellClick;
			_yapilanIsGrid.CellClick+=YapilanIsGrid_CellClick;

			_yapilanIsListeGroupBox.Text="İş Bilgisi Listesi";
			_yapilanIsListeGroupBox.Controls.Add(_yapilanIsGrid);
			_yapilanIsListeGroupBox.Controls.Add(aramaPaneli);

			_yapilanIsDetayGroupBox=new GroupBox
			{
				Text=string.Empty,
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(18)
			};

			Panel detayBaslikPanel = new Panel
			{
				Dock=DockStyle.Top,
				Height=64,
				Padding=new Padding(0 , 0 , 0 , 14),
				BackColor=Color.White
			};

			Label detayBaslikLabel = new Label
			{
				Dock=DockStyle.Top,
				Height=28,
				Text="İş Bilgisi Formu",
				Font=new Font("Segoe UI" , 12.5F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				TextAlign=ContentAlignment.MiddleLeft
			};

			Label detayAciklamaLabel = new Label
			{
				Dock=DockStyle.Fill,
				Text="İş tanımını, miktarı ve fiyatı düzenleyin. Toplam alanı otomatik hesaplanır.",
				Font=new Font("Segoe UI" , 9.25F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				TextAlign=ContentAlignment.TopLeft
			};
			detayBaslikPanel.Controls.Add(detayAciklamaLabel);
			detayBaslikPanel.Controls.Add(detayBaslikLabel);

			TableLayoutPanel detayLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Top,
				ColumnCount=1,
				RowCount=12,
				AutoSize=true,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=Color.White
			};
			detayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 72F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 36F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 36F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 36F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 36F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));
			detayLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 36F));

			_yapilanIsIdTextBox=YapilanIsDetayTextBoxOlustur();
			_yapilanIsIdTextBox.ReadOnly=true;
			_yapilanIsIdTextBox.Visible=false;
			_yapilanIsIdTextBox.TabStop=false;

			_yapilanIsBilgiTextBox=YapilanIsDetayTextBoxOlustur();
			_yapilanIsBilgiTextBox.MaxLength=500;
			_yapilanIsBilgiTextBox.Multiline=true;
			_yapilanIsBilgiTextBox.AcceptsReturn=true;
			_yapilanIsBilgiTextBox.ScrollBars=ScrollBars.Vertical;
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("İŞ BİLGİSİ") , 0 , 0);
			detayLayout.Controls.Add(_yapilanIsBilgiTextBox , 0 , 1);

			_yapilanIsAdiTextBox=YapilanIsDetayTextBoxOlustur();
			_yapilanIsAdiTextBox.MaxLength=255;
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("İŞ ADI") , 0 , 2);
			detayLayout.Controls.Add(_yapilanIsAdiTextBox , 0 , 3);

			_yapilanIsBirimTextBox=YapilanIsDetayTextBoxOlustur(VarsayilanYapilanIsBirimi);
			_yapilanIsBirimTextBox.CharacterCasing=CharacterCasing.Upper;
			_yapilanIsBirimTextBox.MaxLength=100;
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("BİRİM") , 0 , 4);
			detayLayout.Controls.Add(_yapilanIsBirimTextBox , 0 , 5);

			_yapilanIsAdetTextBox=YapilanIsDetayTextBoxOlustur("1");
			_yapilanIsAdetTextBox.Visible=false;
			_yapilanIsAdetTextBox.TabStop=false;

			_yapilanIsMiktarTextBox=YapilanIsDetayTextBoxOlustur("1");
			_yapilanIsMiktarTextBox.TextAlign=HorizontalAlignment.Right;
			_yapilanIsMiktarTextBox.TextChanged-=YapilanIsSayisalAlan_TextChanged;
			_yapilanIsMiktarTextBox.TextChanged+=YapilanIsSayisalAlan_TextChanged;
			_yapilanIsMiktarTextBox.KeyPress-=SepetSayisal_KeyPress;
			_yapilanIsMiktarTextBox.KeyPress+=SepetSayisal_KeyPress;
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("MİKTAR") , 0 , 6);
			detayLayout.Controls.Add(_yapilanIsMiktarTextBox , 0 , 7);

			_yapilanIsFiyatTextBox=YapilanIsDetayTextBoxOlustur("0,00");
			_yapilanIsFiyatTextBox.TextAlign=HorizontalAlignment.Right;
			_yapilanIsFiyatTextBox.TextChanged-=YapilanIsSayisalAlan_TextChanged;
			_yapilanIsFiyatTextBox.TextChanged+=YapilanIsSayisalAlan_TextChanged;
			_yapilanIsFiyatTextBox.KeyPress-=SepetSayisal_KeyPress;
			_yapilanIsFiyatTextBox.KeyPress+=SepetSayisal_KeyPress;
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("İŞ FİYATI") , 0 , 8);
			detayLayout.Controls.Add(_yapilanIsFiyatTextBox , 0 , 9);

			_yapilanIsToplamTextBox=YapilanIsDetayTextBoxOlustur("0,00");
			_yapilanIsToplamTextBox.ReadOnly=true;
			_yapilanIsToplamTextBox.TabStop=false;
			_yapilanIsToplamTextBox.TextAlign=HorizontalAlignment.Right;
			_yapilanIsToplamTextBox.BackColor=Color.FromArgb(248 , 250 , 252);
			_yapilanIsToplamTextBox.Font=new Font("Segoe UI" , 10F , FontStyle.Bold);
			detayLayout.Controls.Add(YapilanIsDetayEtiketiOlustur("TOPLAM FİYATI") , 0 , 10);
			detayLayout.Controls.Add(_yapilanIsToplamTextBox , 0 , 11);

			TableLayoutPanel butonPaneli = new TableLayoutPanel
			{
				Dock=DockStyle.Top,
				ColumnCount=2,
				RowCount=2,
				AutoSize=true,
				Margin=new Padding(0 , 18 , 0 , 0),
				Padding=Padding.Empty,
				BackColor=Color.White
			};
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50F));
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50F));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 44F));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 44F));

			_yapilanIsKaydetButonu=new Button();
			HazirlaNotAksiyonButonu(_yapilanIsKaydetButonu , "Kaydet" , "Save.png" , Color.FromArgb(13 , 148 , 136));
			YapilanIsAksiyonButonStiliniUygula(_yapilanIsKaydetButonu);
			_yapilanIsKaydetButonu.Margin=new Padding(0 , 0 , 8 , 8);
			_yapilanIsKaydetButonu.Click-=YapilanIsKaydetButonu_Click;
			_yapilanIsKaydetButonu.Click+=YapilanIsKaydetButonu_Click;

			_yapilanIsGuncelleButonu=new Button();
			HazirlaNotAksiyonButonu(_yapilanIsGuncelleButonu , "Güncelle" , "Renew.png" , Color.FromArgb(37 , 99 , 235));
			YapilanIsAksiyonButonStiliniUygula(_yapilanIsGuncelleButonu);
			_yapilanIsGuncelleButonu.Margin=new Padding(8 , 0 , 0 , 8);
			_yapilanIsGuncelleButonu.Click-=YapilanIsGuncelleButonu_Click;
			_yapilanIsGuncelleButonu.Click+=YapilanIsGuncelleButonu_Click;

			_yapilanIsSilButonu=new Button();
			HazirlaNotAksiyonButonu(_yapilanIsSilButonu , "Sil" , "Delete File.png" , Color.White , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(254 , 202 , 202));
			YapilanIsAksiyonButonStiliniUygula(_yapilanIsSilButonu);
			_yapilanIsSilButonu.Margin=new Padding(0 , 0 , 8 , 0);
			_yapilanIsSilButonu.Click-=YapilanIsSilButonu_Click;
			_yapilanIsSilButonu.Click+=YapilanIsSilButonu_Click;

			_yapilanIsTemizleButonu=new Button();
			HazirlaNotIkincilButonu(_yapilanIsTemizleButonu , "Temizle" , "Broom.png");
			YapilanIsAksiyonButonStiliniUygula(_yapilanIsTemizleButonu);
			_yapilanIsTemizleButonu.Margin=new Padding(8 , 0 , 0 , 0);
			_yapilanIsTemizleButonu.Click-=YapilanIsTemizleButonu_Click;
			_yapilanIsTemizleButonu.Click+=YapilanIsTemizleButonu_Click;

			butonPaneli.Controls.Add(_yapilanIsKaydetButonu , 0 , 0);
			butonPaneli.Controls.Add(_yapilanIsGuncelleButonu , 1 , 0);
			butonPaneli.Controls.Add(_yapilanIsSilButonu , 0 , 1);
			butonPaneli.Controls.Add(_yapilanIsTemizleButonu , 1 , 1);

			_yapilanIsDetayGroupBox.Controls.Add(butonPaneli);
			_yapilanIsDetayGroupBox.Controls.Add(detayLayout);
			_yapilanIsDetayGroupBox.Controls.Add(detayBaslikPanel);

			icerikLayout.Controls.Add(_yapilanIsListeGroupBox , 0 , 0);
			icerikLayout.Controls.Add(_yapilanIsDetayGroupBox , 1 , 0);

			_yapilanIsKokGroupBox=new GroupBox
			{
				Dock=DockStyle.Fill,
				Padding=new Padding(0),
				BackColor=Color.FromArgb(245 , 247 , 250)
			};
			_yapilanIsKokGroupBox.Controls.Add(icerikLayout);

			anaPanel.Controls.Add(_yapilanIsKokGroupBox);
			_yapilanIsTabPage.Controls.Add(anaPanel);
			_yapilanIsTabPage.ResumeLayout(true);

			DatagridviewSetting(_yapilanIsGrid);
			AramaKutusuHazirla(_yapilanIsAramaTextBox , _yapilanIsGrid);
			YapilanIsListesiniYenile();
			YapilanIsFormunuTemizle();
		}

		private Panel YapilanIsOzetKartiOlustur ( string baslik , Color renk , out Label degerLabel )
		{
			Panel kart = new Panel
			{
				Width=180,
				Height=72,
				Margin=new Padding(12 , 0 , 0 , 0),
				BackColor=renk
			};

			Label baslikLabel = new Label
			{
				AutoSize=true,
				Text=baslik,
				ForeColor=Color.White,
				Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold),
				Location=new Point(16 , 12)
			};

			degerLabel=new Label
			{
				AutoSize=true,
				Text="0",
				ForeColor=Color.White,
				Font=new Font("Segoe UI" , 14F , FontStyle.Bold),
				Location=new Point(16 , 34)
			};

			kart.Controls.Add(baslikLabel);
			kart.Controls.Add(degerLabel);
			return kart;
		}

		private Label YapilanIsDetayEtiketiOlustur ( string metin )
		{
			return new Label
			{
				Dock=DockStyle.Fill,
				AutoSize=false,
				Text=metin,
				Font=new Font("Segoe UI" , 9F , FontStyle.Bold),
				ForeColor=Color.FromArgb(51 , 65 , 85),
				TextAlign=ContentAlignment.BottomLeft,
				Margin=Padding.Empty
			};
		}

		private TextBox YapilanIsDetayTextBoxOlustur ( string varsayilanMetin = "" )
		{
			return new TextBox
			{
				BorderStyle=BorderStyle.FixedSingle,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Font=new Font("Segoe UI" , 10F , FontStyle.Regular),
				Dock=DockStyle.Fill,
				Margin=new Padding(0 , 0 , 0 , 12),
				MinimumSize=new Size(0 , 34),
				Text=varsayilanMetin
			};
		}

		private void YapilanIsAksiyonButonStiliniUygula ( Button buton )
		{
			if(buton==null)
				return;

			buton.Dock=DockStyle.Fill;
			buton.AutoSize=false;
			buton.Height=44;
			buton.Margin=Padding.Empty;
			buton.Font=new Font("Segoe UI" , 9.25F , FontStyle.Bold);
			buton.Padding=new Padding(12 , 0 , 14 , 0);
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleRight;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
		}

		private void YapilanIsButonDurumlariniGuncelle ()
		{
			bool secimVar = _seciliYapilanIsYonetimId.HasValue;
			if(_yapilanIsKaydetButonu!=null)
				_yapilanIsKaydetButonu.Enabled=true;
			if(_yapilanIsTemizleButonu!=null)
				_yapilanIsTemizleButonu.Enabled=true;
			if(_yapilanIsGuncelleButonu!=null)
				_yapilanIsGuncelleButonu.Enabled=secimVar;
			if(_yapilanIsSilButonu!=null)
				_yapilanIsSilButonu.Enabled=secimVar;
		}

		private Button YapilanIsYonetimButonuOlustur ( string metin , Color arkaPlan )
		{
			Button buton = new Button
			{
				Text=metin,
				Width=320,
				Height=48,
				BackColor=arkaPlan,
				ForeColor=Color.White,
				FlatStyle=FlatStyle.Flat,
				Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold),
				TextAlign=ContentAlignment.MiddleLeft,
				Padding=new Padding(12 , 0 , 12 , 0),
				Margin=new Padding(0 , 0 , 0 , 10)
			};

			buton.FlatAppearance.BorderSize=0;
			return buton;
		}

		private void YapilanIsListesiniYenile ()
		{
			if(_yapilanIsGrid==null)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					DataTable dt = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter(
						@"SELECT
							[YapilanIsID],
							IIF([IsBilgisi] IS NULL, '', [IsBilgisi]) AS IsBilgisi,
							IIF([IsAdi] IS NULL, '', [IsAdi]) AS IsAdi,
							IIF([Birim] IS NULL OR [Birim]='', '" + VarsayilanYapilanIsBirimi + @"', [Birim]) AS Birim,
							IIF([Adet] IS NULL, 0, [Adet]) AS Adet,
							IIF([Miktar] IS NULL, 0, [Miktar]) AS Miktar,
							IIF([Fiyat] IS NULL, 0, [Fiyat]) AS Fiyat,
							IIF([ToplamFiyat] IS NULL, IIF([Miktar] IS NULL, 0, [Miktar]) * IIF([Fiyat] IS NULL, 0, [Fiyat]), [ToplamFiyat]) AS ToplamFiyat
						FROM [YapilanIsler]
						ORDER BY IIF([IsAdi] IS NULL, '', [IsAdi]) ASC, [YapilanIsID] DESC" ,
						conn))
						da.Fill(dt);

					_yapilanIsGrid.DataSource=dt;
					if(_yapilanIsGrid.Columns.Contains("YapilanIsID"))
						_yapilanIsGrid.Columns["YapilanIsID"].Visible=false;
					if(_yapilanIsGrid.Columns.Contains("IsBilgisi"))
						_yapilanIsGrid.Columns["IsBilgisi"].HeaderText="İŞ BİLGİSİ";
					if(_yapilanIsGrid.Columns.Contains("IsAdi"))
						_yapilanIsGrid.Columns["IsAdi"].HeaderText="YAPILAN İŞ";
					if(_yapilanIsGrid.Columns.Contains("Birim"))
						_yapilanIsGrid.Columns["Birim"].HeaderText="BİRİM";
					if(_yapilanIsGrid.Columns.Contains("Adet"))
						_yapilanIsGrid.Columns["Adet"].Visible=false;
					if(_yapilanIsGrid.Columns.Contains("Miktar"))
						_yapilanIsGrid.Columns["Miktar"].HeaderText="MİKTAR";
					if(_yapilanIsGrid.Columns.Contains("ToplamTotal"))
					{
						_yapilanIsGrid.Columns["ToplamTotal"].HeaderText="TOPLAM / TOTAL";
						_yapilanIsGrid.Columns["ToplamTotal"].DefaultCellStyle.Format="N2";
					}

					GridBasliklariniTurkceDuzenle(_yapilanIsGrid);
					if(_yapilanIsGrid.Columns.Contains("IsAdi"))
						_yapilanIsGrid.Columns["IsAdi"].HeaderText="İŞ ADI";
					if(_yapilanIsGrid.Columns.Contains("Birim"))
						_yapilanIsGrid.Columns["Birim"].HeaderText="BİRİM";
					if(_yapilanIsGrid.Columns.Contains("Miktar"))
						_yapilanIsGrid.Columns["Miktar"].HeaderText="MİKTAR";
					if(_yapilanIsGrid.Columns.Contains("Fiyat"))
					{
						_yapilanIsGrid.Columns["Fiyat"].HeaderText="İŞ FİYATI";
						_yapilanIsGrid.Columns["Fiyat"].DefaultCellStyle.Format="N2";
					}
					if(_yapilanIsGrid.Columns.Contains("ToplamFiyat"))
					{
						_yapilanIsGrid.Columns["ToplamFiyat"].HeaderText="TOPLAM FİYATI";
						_yapilanIsGrid.Columns["ToplamFiyat"].DefaultCellStyle.Format="N2";
					}
					GridAramaFiltresiniUygula(_yapilanIsAramaTextBox , _yapilanIsGrid);
					YapilanIsOzetleriniYenile(dt);
				}

				SepetYapilanIsSecimleriniYenile();
				foreach(BelgePaneli panel in _belgePanelleri.Values)
					BelgeYapilanIsSecimleriniYenile(panel);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Yapılan iş listesi yüklenemedi: "+ex.Message);
			}
		}

		private void YapilanIsOzetleriniYenile ( DataTable dt )
		{
			if(_yapilanIsToplamLabel!=null)
				_yapilanIsToplamLabel.Text=(dt?.Rows.Count??0).ToString("N0" , _yazdirmaKulturu);

			if(_yapilanIsOrtalamaLabel!=null)
			{
				decimal ortalama = 0m;
				if(dt!=null&&dt.Rows.Count>0)
					ortalama=dt.AsEnumerable().Average(r => r["Fiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(r["Fiyat"]));
				_yapilanIsOrtalamaLabel.Text="₺"+ortalama.ToString("N2" , _yazdirmaKulturu);
			}

			if(_yapilanIsSonKayitLabel!=null)
			{
				string sonKayit = "-";
				if(dt!=null&&dt.Rows.Count>0)
					sonKayit=Convert.ToString(dt.Rows[0]["IsAdi"])??"-";
				_yapilanIsSonKayitLabel.Text=sonKayit;
			}
		}

		private void YapilanIsGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||_yapilanIsGrid==null||e.RowIndex>=_yapilanIsGrid.Rows.Count)
				return;

			YapilanIsSeciliKaydiYukle(_yapilanIsGrid.Rows[e.RowIndex]);
		}

		private void YapilanIsSeciliKaydiYukle ( DataGridViewRow satir )
		{
			if(satir==null||satir.IsNewRow)
				return;

			_seciliYapilanIsYonetimId=SatirdanIntGetir(satir , "YapilanIsID");
			if(_yapilanIsIdTextBox!=null)
				_yapilanIsIdTextBox.Text=_seciliYapilanIsYonetimId?.ToString()??string.Empty;
			if(_yapilanIsBilgiTextBox!=null)
				_yapilanIsBilgiTextBox.Text=Convert.ToString(satir.Cells["IsBilgisi"].Value)??string.Empty;
			if(_yapilanIsAdiTextBox!=null)
				_yapilanIsAdiTextBox.Text=Convert.ToString(satir.Cells["IsAdi"].Value)??string.Empty;
			if(_yapilanIsBirimTextBox!=null)
				_yapilanIsBirimTextBox.Text=Convert.ToString(satir.Cells["Birim"].Value)??VarsayilanYapilanIsBirimi;
			if(_yapilanIsAdetTextBox!=null)
				_yapilanIsAdetTextBox.Text=SepetDecimalParse(Convert.ToString(satir.Cells["Adet"].Value)).ToString("0.##" , _yazdirmaKulturu);
			if(_yapilanIsMiktarTextBox!=null)
				_yapilanIsMiktarTextBox.Text=SepetDecimalParse(Convert.ToString(satir.Cells["Miktar"].Value)).ToString("0.##" , _yazdirmaKulturu);
			if(_yapilanIsFiyatTextBox!=null&&satir.DataGridView.Columns.Contains("Fiyat"))
				_yapilanIsFiyatTextBox.Text=SepetDecimalParse(Convert.ToString(satir.Cells["Fiyat"].Value)).ToString("N2" , _yazdirmaKulturu);
			YapilanIsToplamAlaniniGuncelle();
			YapilanIsButonDurumlariniGuncelle();
		}

		private void YapilanIsFormunuTemizle ()
		{
			_seciliYapilanIsYonetimId=null;
			if(_yapilanIsIdTextBox!=null) _yapilanIsIdTextBox.Clear();
			if(_yapilanIsBilgiTextBox!=null) _yapilanIsBilgiTextBox.Clear();
			if(_yapilanIsAdiTextBox!=null) _yapilanIsAdiTextBox.Clear();
			if(_yapilanIsBirimTextBox!=null) _yapilanIsBirimTextBox.Text=VarsayilanYapilanIsBirimi;
			if(_yapilanIsAdetTextBox!=null) _yapilanIsAdetTextBox.Text="1";
			if(_yapilanIsMiktarTextBox!=null) _yapilanIsMiktarTextBox.Text="1";
			if(_yapilanIsFiyatTextBox!=null) _yapilanIsFiyatTextBox.Text="0,00";
			YapilanIsToplamAlaniniGuncelle();
			if(_yapilanIsGrid!=null) _yapilanIsGrid.ClearSelection();
			YapilanIsButonDurumlariniGuncelle();
		}

		private bool YapilanIsFormunuDogrula ( out string hataMetni )
		{
			hataMetni=string.Empty;
			if(string.IsNullOrWhiteSpace(_yapilanIsAdiTextBox?.Text))
			{
				hataMetni="Yapılan iş adı boş bırakılamaz.";
				return false;
			}
			if(string.IsNullOrWhiteSpace(_yapilanIsBirimTextBox?.Text))
			{
				hataMetni="Birim bilgisi boş bırakılamaz.";
				return false;
			}

			decimal adet = SepetDecimalParse(_yapilanIsAdetTextBox?.Text);
			decimal miktar = SepetDecimalParse(_yapilanIsMiktarTextBox?.Text);
			if(adet<=0)
			{
				hataMetni="Adet 0'dan büyük olmalıdır.";
				return false;
			}
			if(miktar<=0)
			{
				hataMetni="Miktar 0'dan büyük olmalıdır.";
				return false;
			}

			return true;
		}

		private decimal YapilanIsToplamTutariniHesapla ()
		{
			decimal miktar = SepetDecimalParse(_yapilanIsMiktarTextBox?.Text);
			decimal fiyat = YapilanIsYonetimFiyatiGetir();
			return miktar*fiyat;
		}

		private decimal YapilanIsToplamMiktariniHesapla ()
		{
			decimal adet = SepetDecimalParse(_yapilanIsAdetTextBox?.Text);
			decimal miktar = SepetDecimalParse(_yapilanIsMiktarTextBox?.Text);
			if(adet<=0)
				adet=1m;

			return adet*miktar;
		}

		private decimal YapilanIsYonetimFiyatiGetir ()
		{
			if(_yapilanIsFiyatTextBox==null)
				return 0m;

			return SepetDecimalParse(_yapilanIsFiyatTextBox.Text);
		}

		private void YapilanIsToplamAlaniniGuncelle ()
		{
			if(_yapilanIsToplamTextBox==null)
				return;

			_yapilanIsToplamTextBox.Text=YapilanIsToplamTutariniHesapla().ToString("N2" , _yazdirmaKulturu);
		}

		private void YapilanIsKaydetButonu_Click ( object sender , EventArgs e )
		{
			if(!YapilanIsFormunuDogrula(out string hataMetni))
			{
				MessageBox.Show(hataMetni);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand(
						"INSERT INTO [YapilanIsler] ([IsBilgisi], [IsAdi], [Birim], [Adet], [Miktar], [Fiyat], [ToplamFiyat]) VALUES (?, ?, ?, ?, ?, ?, ?)" ,
						conn))
					{
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(_yapilanIsBilgiTextBox?.Text) ? (object)DBNull.Value : _yapilanIsBilgiTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=_yapilanIsAdiTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=string.IsNullOrWhiteSpace(_yapilanIsBirimTextBox?.Text) ? VarsayilanYapilanIsBirimi : _yapilanIsBirimTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(SepetDecimalParse(_yapilanIsAdetTextBox?.Text));
						cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(SepetDecimalParse(_yapilanIsMiktarTextBox?.Text));
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=YapilanIsYonetimFiyatiGetir();
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=YapilanIsToplamTutariniHesapla();
						cmd.ExecuteNonQuery();
					}
				}

				YapilanIsListesiniYenile();
				YapilanIsFormunuTemizle();
				MessageBox.Show("Yapılan iş kaydedildi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Yapılan iş kaydedilemedi: "+ex.Message);
			}
		}

		private void YapilanIsGuncelleButonu_Click ( object sender , EventArgs e )
		{
			if(!_seciliYapilanIsYonetimId.HasValue)
			{
				MessageBox.Show("Güncellemek için bir kayıt seçin.");
				return;
			}

			if(!YapilanIsFormunuDogrula(out string hataMetni))
			{
				MessageBox.Show(hataMetni);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand(
						"UPDATE [YapilanIsler] SET [IsBilgisi]=?, [IsAdi]=?, [Birim]=?, [Adet]=?, [Miktar]=?, [Fiyat]=?, [ToplamFiyat]=? WHERE [YapilanIsID]=?" ,
						conn))
					{
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(_yapilanIsBilgiTextBox?.Text) ? (object)DBNull.Value : _yapilanIsBilgiTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=_yapilanIsAdiTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=string.IsNullOrWhiteSpace(_yapilanIsBirimTextBox?.Text) ? VarsayilanYapilanIsBirimi : _yapilanIsBirimTextBox.Text.Trim();
						cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(SepetDecimalParse(_yapilanIsAdetTextBox?.Text));
						cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(SepetDecimalParse(_yapilanIsMiktarTextBox?.Text));
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=YapilanIsYonetimFiyatiGetir();
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=YapilanIsToplamTutariniHesapla();
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_seciliYapilanIsYonetimId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				YapilanIsListesiniYenile();
				MessageBox.Show("Yapılan iş güncellendi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Yapılan iş güncellenemedi: "+ex.Message);
			}
		}

		private void YapilanIsSilButonu_Click ( object sender , EventArgs e )
		{
			if(!_seciliYapilanIsYonetimId.HasValue)
			{
				MessageBox.Show("Silmek için bir kayıt seçin.");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [YapilanIsler] WHERE [YapilanIsID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_seciliYapilanIsYonetimId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				YapilanIsListesiniYenile();
				YapilanIsFormunuTemizle();
				MessageBox.Show("Yapılan iş silindi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Yapılan iş silinemedi: "+ex.Message);
			}
		}

		private void YapilanIsTemizleButonu_Click ( object sender , EventArgs e )
		{
			YapilanIsFormunuTemizle();
		}

		private void YapilanIsSayisalAlan_TextChanged ( object sender , EventArgs e )
		{
			if(_yapilanIsAlanlariGuncelleniyor)
				return;

			TextBox kutu = sender as TextBox;
			if(kutu==null)
				return;

			_yapilanIsAlanlariGuncelleniyor=true;
			try
			{
				if(string.IsNullOrWhiteSpace(kutu.Text))
					kutu.Text=ReferenceEquals(kutu , _yapilanIsFiyatTextBox) ? "0,00" : "0";

				YapilanIsToplamAlaniniGuncelle();
			}
			finally
			{
				_yapilanIsAlanlariGuncelleniyor=false;
			}
		}

		private string ToptanciAdSqlIfadesi ( string tabloTakmaAdi )
		{
			if(_toptanciAdiKolonuVar)
				return "IIF(" + tabloTakmaAdi + ".[AdSoyad] IS NULL OR " + tabloTakmaAdi + ".[AdSoyad]='', IIF(" + tabloTakmaAdi + ".[ToptanciAdi] IS NULL, '', " + tabloTakmaAdi + ".[ToptanciAdi]), " + tabloTakmaAdi + ".[AdSoyad])";

			return "IIF(" + tabloTakmaAdi + ".[AdSoyad] IS NULL, '', " + tabloTakmaAdi + ".[AdSoyad])";
		}

		private string ToptanciDurumSqlIfadesi ( string tabloTakmaAdi )
		{
			if(_toptanciDurumMetniKolonuVar)
			{
				if(_toptanciDurumKolonuMantiksal)
					return "IIF(" + tabloTakmaAdi + ".[DurumMetni] IS NULL OR " + tabloTakmaAdi + ".[DurumMetni]='', IIF(" + tabloTakmaAdi + ".[Durum]=True, 'AKTİF', 'PASİF'), " + tabloTakmaAdi + ".[DurumMetni])";

				if(_toptanciDurumKolonuVar)
					return "IIF(" + tabloTakmaAdi + ".[DurumMetni] IS NULL OR " + tabloTakmaAdi + ".[DurumMetni]='', IIF(" + tabloTakmaAdi + ".[Durum] IS NULL, '', " + tabloTakmaAdi + ".[Durum]), " + tabloTakmaAdi + ".[DurumMetni])";

				return "IIF(" + tabloTakmaAdi + ".[DurumMetni] IS NULL, '', " + tabloTakmaAdi + ".[DurumMetni])";
			}

			if(_toptanciDurumKolonuMantiksal)
				return "IIF(" + tabloTakmaAdi + ".[Durum]=True, 'AKTİF', 'PASİF')";

			if(_toptanciDurumKolonuVar)
				return "IIF(" + tabloTakmaAdi + ".[Durum] IS NULL, '', " + tabloTakmaAdi + ".[Durum])";

			return "''";
		}

		private void EnsureToptanciAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					_toptanciTablosuVar=TabloVarMi(conn , "Toptancilar");
					if(!_toptanciTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("CREATE TABLE [Toptancilar] ([ToptanciID] AUTOINCREMENT, [AdSoyad] TEXT(255), [Telefon] TEXT(50), [Durum] TEXT(50))" , conn))
							cmd.ExecuteNonQuery();
						_toptanciTablosuVar=true;
					}

					_toptanciAdiKolonuVar=KolonVarMi(conn , "Toptancilar" , "ToptanciAdi");
					_toptanciDurumKolonuVar=KolonVarMi(conn , "Toptancilar" , "Durum");
					_toptanciDurumKolonuMantiksal=_toptanciDurumKolonuVar&&KolonVeriTuruGetir(conn , "Toptancilar" , "Durum")==11;
					if(!KolonVarMi(conn , "Toptancilar" , "AdSoyad"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Toptancilar] ADD COLUMN [AdSoyad] TEXT(255)" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "Toptancilar" , "Telefon"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Toptancilar] ADD COLUMN [Telefon] TEXT(50)" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!_toptanciDurumKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Toptancilar] ADD COLUMN [Durum] TEXT(50)" , conn))
							cmd.ExecuteNonQuery();
						_toptanciDurumKolonuVar=true;
						_toptanciDurumKolonuMantiksal=false;
					}
					_toptanciDurumMetniKolonuVar=KolonVarMi(conn , "Toptancilar" , "DurumMetni");
					if(!_toptanciDurumMetniKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Toptancilar] ADD COLUMN [DurumMetni] TEXT(50)" , conn))
							cmd.ExecuteNonQuery();
						_toptanciDurumMetniKolonuVar=true;
					}

					if(_toptanciAdiKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [Toptancilar] SET [AdSoyad]=[ToptanciAdi] WHERE ([AdSoyad] IS NULL OR [AdSoyad]='') AND [ToptanciAdi] IS NOT NULL AND [ToptanciAdi]<>''" , conn))
							cmd.ExecuteNonQuery();
					}
					if(_toptanciDurumMetniKolonuVar)
					{
						string durumAktarmaSorgusu = _toptanciDurumKolonuMantiksal
							? "UPDATE [Toptancilar] SET [DurumMetni]=IIF([Durum]=True, 'AKTİF', 'PASİF') WHERE [DurumMetni] IS NULL OR [DurumMetni]=''"
							: "UPDATE [Toptancilar] SET [DurumMetni]=[Durum] WHERE ([DurumMetni] IS NULL OR [DurumMetni]='') AND [Durum] IS NOT NULL";
						using(OleDbCommand cmd = new OleDbCommand(durumAktarmaSorgusu , conn))
							cmd.ExecuteNonQuery();
					}

					_toptanciAlimTablosuVar=TabloVarMi(conn , "ToptanciAlimlari");
					if(!_toptanciAlimTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("CREATE TABLE [ToptanciAlimlari] ([AlimID] AUTOINCREMENT, [ToptanciID] LONG, [Tarih] DATETIME, [Tutar] CURRENCY, [Aciklama] LONGTEXT)" , conn))
							cmd.ExecuteNonQuery();
						_toptanciAlimTablosuVar=true;
					}

					if(!KolonVarMi(conn , "ToptanciAlimlari" , "ToptanciID"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciAlimlari] ADD COLUMN [ToptanciID] LONG" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciAlimlari" , "Tarih"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciAlimlari] ADD COLUMN [Tarih] DATETIME" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciAlimlari" , "Tutar"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciAlimlari] ADD COLUMN [Tutar] CURRENCY" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciAlimlari" , "Aciklama"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciAlimlari] ADD COLUMN [Aciklama] LONGTEXT" , conn))
							cmd.ExecuteNonQuery();
					}

					_toptanciOdemeTablosuVar=TabloVarMi(conn , "ToptanciOdemeleri");
					if(!_toptanciOdemeTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("CREATE TABLE [ToptanciOdemeleri] ([OdemeID] AUTOINCREMENT, [ToptanciID] LONG, [OdemeTarihi] DATETIME, [OdenenTutar] CURRENCY, [Aciklama] LONGTEXT)" , conn))
							cmd.ExecuteNonQuery();
						_toptanciOdemeTablosuVar=true;
					}

					if(!KolonVarMi(conn , "ToptanciOdemeleri" , "ToptanciID"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciOdemeleri] ADD COLUMN [ToptanciID] LONG" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciOdemeleri" , "OdemeTarihi"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciOdemeleri] ADD COLUMN [OdemeTarihi] DATETIME" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciOdemeleri" , "OdenenTutar"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciOdemeleri] ADD COLUMN [OdenenTutar] CURRENCY" , conn))
							cmd.ExecuteNonQuery();
					}
					if(!KolonVarMi(conn , "ToptanciOdemeleri" , "Aciklama"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [ToptanciOdemeleri] ADD COLUMN [Aciklama] LONGTEXT" , conn))
							cmd.ExecuteNonQuery();
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptancı altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void EnsureNotAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					bool notTablosuVar = TabloVarMi(conn , "Notlarim");
					if(!notTablosuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand("CREATE TABLE [Notlarim] ([NotID] AUTOINCREMENT, [Baslik] TEXT(255), [Tarih] DATETIME, [NotMetni] LONGTEXT, [Okundu] YESNO)" , conn))
								cmd.ExecuteNonQuery();
						}
						catch
						{
							if(!TabloVarMi(conn , "Notlarim"))
								throw;
						}
					}

					if(!KolonVarMi(conn , "Notlarim" , "Baslik"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Notlarim] ADD COLUMN [Baslik] TEXT(255)" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "Notlarim" , "Tarih"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Notlarim] ADD COLUMN [Tarih] DATETIME" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "Notlarim" , "NotMetni"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Notlarim] ADD COLUMN [NotMetni] LONGTEXT" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "Notlarim" , "Okundu"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [Notlarim] ADD COLUMN [Okundu] YESNO" , conn))
							cmd.ExecuteNonQuery();
					}

					using(OleDbCommand cmd = new OleDbCommand("UPDATE [Notlarim] SET [Tarih]=Now() WHERE [Tarih] IS NULL" , conn))
						cmd.ExecuteNonQuery();
					using(OleDbCommand cmd = new OleDbCommand("UPDATE [Notlarim] SET [Okundu]=False WHERE [Okundu] IS NULL" , conn))
						cmd.ExecuteNonQuery();
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Not altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void EnsureYapilanIsAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					YapilanIslerTablosunuHazirla(conn);

					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "IsBilgisi" , "LONGTEXT");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "IsAdi" , "TEXT(255)");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "Birim" , "TEXT(100)");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "Adet" , "DOUBLE");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "Miktar" , "DOUBLE");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "Fiyat" , "CURRENCY");
					YapilanIsKolonunuHazirla(conn , "YapilanIsler" , "ToplamFiyat" , "CURRENCY");

					YapilanIsDetayTablosunuHazirla(conn , "TeklifDetaylari");
					YapilanIsDetayTablosunuHazirla(conn , "FaturaDetay");

					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"YapilanIsler" ,
						new[] { "Birim" } ,
						"UPDATE [YapilanIsler] SET [Birim]='" + VarsayilanYapilanIsBirimi + "' WHERE [Birim] IS NULL OR [Birim]=''");
					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"YapilanIsler" ,
						new[] { "Adet" } ,
						"UPDATE [YapilanIsler] SET [Adet]=1 WHERE [Adet] IS NULL OR [Adet]=0");
					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"YapilanIsler" ,
						new[] { "Miktar" } ,
						"UPDATE [YapilanIsler] SET [Miktar]=1 WHERE [Miktar] IS NULL OR [Miktar]=0");
					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"YapilanIsler" ,
						new[] { "Miktar" , "Fiyat" , "ToplamFiyat" } ,
						"UPDATE [YapilanIsler] SET [ToplamFiyat]=IIF([Miktar] IS NULL, 0, [Miktar]) * IIF([Fiyat] IS NULL, 0, [Fiyat])");
					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"TeklifDetaylari" ,
						new[] { "KalemTuru" , "UrunID" } ,
						"UPDATE [TeklifDetaylari] SET [KalemTuru]='ÜRÜN' WHERE ([KalemTuru] IS NULL OR [KalemTuru]='') AND [UrunID] IS NOT NULL");
					GuvenliYapilanIsGuncellemesiCalistir(
						conn ,
						"FaturaDetay" ,
						new[] { "KalemTuru" , "UrunID" } ,
						"UPDATE [FaturaDetay] SET [KalemTuru]='ÜRÜN' WHERE ([KalemTuru] IS NULL OR [KalemTuru]='') AND [UrunID] IS NOT NULL");
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Yapılan işler altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void YapilanIslerTablosunuHazirla ( OleDbConnection conn )
		{
			if(conn==null||YapilanIsTabloVarMi(conn , "YapilanIsler"))
				return;

			string[] createKomutlari =
			{
				"CREATE TABLE [YapilanIsler] ([YapilanIsID] AUTOINCREMENT)",
				"CREATE TABLE [YapilanIsler] ([YapilanIsID] COUNTER)"
			};

			Exception sonHata = null;
			foreach(string komutMetni in createKomutlari)
			{
				try
				{
					using(OleDbCommand cmd = new OleDbCommand(komutMetni , conn))
						cmd.ExecuteNonQuery();
				}
				catch(Exception ex)
				{
					sonHata=ex;
				}

				if(YapilanIsTabloVarMi(conn , "YapilanIsler"))
					return;
			}

			if(!YapilanIsTabloVarMi(conn , "YapilanIsler"))
				throw new InvalidOperationException("Yapılan işler tablosu oluşturulamadı." , sonHata);
		}

		private void YapilanIsDetayTablosunuHazirla ( OleDbConnection conn , string tabloAdi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi)||!YapilanIsTabloVarMi(conn , tabloAdi))
				return;

			YapilanIsKolonunuHazirla(conn , tabloAdi , "KalemTuru" , "TEXT(50)");
			YapilanIsKolonunuHazirla(conn , tabloAdi , "KalemAdi" , "TEXT(255)");
			YapilanIsKolonunuHazirla(conn , tabloAdi , "YapilanIsID" , "LONG");
			YapilanIsKolonunuHazirla(conn , tabloAdi , "Birim" , "TEXT(100)");
			YapilanIsKolonunuHazirla(conn , tabloAdi , "IsBilgisi" , "LONGTEXT");
			YapilanIsKolonunuHazirla(conn , tabloAdi , "Adet" , "DOUBLE");
		}

		private bool YapilanIsTabloVarMi ( OleDbConnection conn , string tabloAdi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi))
				return false;

			try
			{
				using(OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM ["+tabloAdi+"]" , conn))
				{
					cmd.ExecuteScalar();
					return true;
				}
			}
			catch(OleDbException ex)
			{
				string hataMetni = ( ex.Message??string.Empty ).ToLowerInvariant();
				if(hataMetni.Contains("giriş tablosunu")||
					hataMetni.Contains("input table")||
					hataMetni.Contains("bulamıyor")||
					hataMetni.Contains("could not find"))
					return false;

				return false;
			}
			catch
			{
				return false;
			}
		}

		private void YapilanIsKolonunuHazirla ( OleDbConnection conn , string tabloAdi , string kolonAdi , string veriTipi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi)||string.IsNullOrWhiteSpace(kolonAdi)||string.IsNullOrWhiteSpace(veriTipi))
				return;

			if(YapilanIsKolonVarMi(conn , tabloAdi , kolonAdi))
				return;

			Exception sonHata = null;
			foreach(string alternatifTip in YapilanIsAlternatifKolonTipleriniGetir(veriTipi))
			{
				try
				{
					using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE ["+tabloAdi+"] ADD COLUMN ["+kolonAdi+"] "+alternatifTip , conn))
						cmd.ExecuteNonQuery();
				}
				catch(Exception ex)
				{
					sonHata=ex;
				}

				if(YapilanIsKolonVarMi(conn , tabloAdi , kolonAdi))
					return;
			}

			if(!YapilanIsKolonVarMi(conn , tabloAdi , kolonAdi))
				throw new InvalidOperationException("["+tabloAdi+"].["+kolonAdi+"] alanı oluşturulamadı." , sonHata);
		}

		private bool YapilanIsKolonVarMi ( OleDbConnection conn , string tabloAdi , string kolonAdi )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi)||string.IsNullOrWhiteSpace(kolonAdi)||!YapilanIsTabloVarMi(conn , tabloAdi))
				return false;

			try
			{
				using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 ["+kolonAdi+"] FROM ["+tabloAdi+"]" , conn))
				{
					cmd.ExecuteScalar();
					return true;
				}
			}
			catch(OleDbException ex)
			{
				string hataMetni = ( ex.Message??string.Empty ).ToLowerInvariant();
				if(hataMetni.Contains("alan bulunamad")||
					hataMetni.Contains("field")||
					hataMetni.Contains("column")||
					hataMetni.Contains("sütun")||
					hataMetni.Contains("bulunamad"))
					return false;

				return false;
			}
			catch
			{
				return false;
			}
		}

		private IEnumerable<string> YapilanIsAlternatifKolonTipleriniGetir ( string veriTipi )
		{
			string tip = ( veriTipi??string.Empty ).Trim().ToUpperInvariant();
			switch(tip)
			{
				case "LONGTEXT":
					return new[] { "LONGTEXT" , "MEMO" };
				case "LONG":
					return new[] { "LONG" , "INTEGER" };
				case "TEXT(255)":
					return new[] { "TEXT(255)" , "TEXT" };
				case "TEXT(100)":
					return new[] { "TEXT(100)" , "TEXT" };
				case "TEXT(50)":
					return new[] { "TEXT(50)" , "TEXT" };
				default:
					return new[] { veriTipi };
			}
		}

		private void GuvenliYapilanIsGuncellemesiCalistir ( OleDbConnection conn , string tabloAdi , IEnumerable<string> gerekenKolonlar , string sorgu )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tabloAdi)||string.IsNullOrWhiteSpace(sorgu)||!YapilanIsTabloVarMi(conn , tabloAdi))
				return;

			if(gerekenKolonlar!=null)
			{
				foreach(string kolonAdi in gerekenKolonlar)
				{
					if(!YapilanIsKolonVarMi(conn , tabloAdi , kolonAdi))
						return;
				}
			}

			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				cmd.ExecuteNonQuery();
		}

		private void ToptanciDurumComboYenile ()
		{
			string seciliMetin = comboBox11?.Text??string.Empty;
			comboBox11.DataSource=null;
			comboBox11.Items.Clear();
			comboBox11.Items.Add("AKTİF");
			comboBox11.Items.Add("PASİF");
			comboBox11.Items.Add("BORÇLU");
			if(!ComboBoxMetniniSec(comboBox11 , seciliMetin))
				comboBox11.SelectedIndex=0;
		}

		private string ToptanciDurumMetniGetir ()
		{
			return (comboBox11?.Text??string.Empty).Trim().ToUpper(new CultureInfo("tr-TR"));
		}

		private bool ToptanciDurumBoolGetir ()
		{
			return !string.Equals(ToptanciDurumMetniGetir() , "PASİF" , StringComparison.OrdinalIgnoreCase);
		}

		private bool ToptanciSeciliMi ( out int toptanciId )
		{
			toptanciId=0;
			return int.TryParse(textBox65?.Text , out toptanciId);
		}

		private int? SeciliToptanciBakiyeIdGetir ()
		{
			if(_toptanciBakiyeSecimComboBox?.SelectedValue==null||_toptanciBakiyeSecimComboBox.SelectedValue==DBNull.Value)
				return null;

			if(int.TryParse(_toptanciBakiyeSecimComboBox.SelectedValue.ToString() , out int toptanciId))
				return toptanciId;

			return null;
		}

		private decimal ToptanciToplamAlimGetir ( int toptanciId )
		{
			if(!_toptanciAlimTablosuVar||toptanciId<=0)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT SUM([Tutar]) FROM [ToptanciAlimlari] WHERE [ToptanciID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , toptanciId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private decimal ToptanciToplamOdemeGetir ( int toptanciId )
		{
			if(!_toptanciOdemeTablosuVar||toptanciId<=0)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT SUM([OdenenTutar]) FROM [ToptanciOdemeleri] WHERE [ToptanciID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , toptanciId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private void ToptanciHareketFormTemizle ( bool tarihiSifirla )
		{
			if(textBox110!=null) textBox110.Clear();
			if(textBox109!=null) textBox109.Clear();
			if(textBox108!=null) textBox108.Clear();
			if(tarihiSifirla&&_toptanciTarihPicker!=null) _toptanciTarihPicker.Value=DateTime.Now;
			ToptanciSeciliHareketiTemizle();
		}

		private void ToptanciSeciliHareketiTemizle ()
		{
			_toptanciSeciliHareketId=null;
			_toptanciSeciliHareketTuru=null;
			if(dataGridView27!=null)
				dataGridView27.ClearSelection();
			ToptanciBakiyeAksiyonDurumunuGuncelle();
		}

		private void ToptanciBakiyeAksiyonButonunuAyarla ( Button buton , string metin , string imageKey )
		{
			if(buton==null)
				return;

			buton.Text=metin;
			buton.Font=new Font("Microsoft Sans Serif" , 8.5F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162)));
			buton.ImageList=null;
			buton.ImageKey=string.Empty;
			buton.ImageIndex=-1;
			buton.Image=NotButonGorseliOlustur(imageKey , new Size(20 , 20));
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleRight;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.Padding=new Padding(10 , 0 , 12 , 0);
			buton.AutoSize=false;
			buton.UseVisualStyleBackColor=true;
			buton.FlatStyle=FlatStyle.Standard;
			buton.Margin=new Padding(4);
			buton.Size=new Size(124 , 40);
			buton.Dock=DockStyle.Fill;
		}

		private void ToptanciBakiyeAksiyonPaneliniHazirla ()
		{
			if(panel18==null)
				return;

			if(_toptanciBakiyeAksiyonPaneli==null)
			{
				_toptanciBakiyeAksiyonPaneli=new TableLayoutPanel();
				_toptanciBakiyeAksiyonPaneli.ColumnCount=3;
				_toptanciBakiyeAksiyonPaneli.RowCount=2;
				_toptanciBakiyeAksiyonPaneli.BackColor=Color.Transparent;
				_toptanciBakiyeAksiyonPaneli.Margin=Padding.Empty;
				_toptanciBakiyeAksiyonPaneli.Padding=Padding.Empty;
				panel18.Controls.Add(_toptanciBakiyeAksiyonPaneli);
			}

			_toptanciBakiyeAksiyonPaneli.ColumnStyles.Clear();
			_toptanciBakiyeAksiyonPaneli.RowStyles.Clear();
			for(int i = 0 ; i<3 ; i++)
				_toptanciBakiyeAksiyonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 33.3333F));
			_toptanciBakiyeAksiyonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));
			_toptanciBakiyeAksiyonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));

			if(_toptanciBakiyeGuncelleButonu==null)
			{
				_toptanciBakiyeGuncelleButonu=new Button();
				panel18.Controls.Add(_toptanciBakiyeGuncelleButonu);
			}
			if(_toptanciBakiyeSilButonu==null)
			{
				_toptanciBakiyeSilButonu=new Button();
				panel18.Controls.Add(_toptanciBakiyeSilButonu);
			}
			if(_toptanciBakiyeYazdirButonu==null)
			{
				_toptanciBakiyeYazdirButonu=new Button();
				panel18.Controls.Add(_toptanciBakiyeYazdirButonu);
			}
			if(_toptanciBakiyeExcelButonu==null)
			{
				_toptanciBakiyeExcelButonu=new Button();
				panel18.Controls.Add(_toptanciBakiyeExcelButonu);
			}
			if(_toptanciBakiyePdfButonu==null)
			{
				_toptanciBakiyePdfButonu=new Button();
				panel18.Controls.Add(_toptanciBakiyePdfButonu);
			}

			ToptanciBakiyeAksiyonButonunuAyarla(button80 , "Kaydet" , "Save.png");
			ToptanciBakiyeAksiyonButonunuAyarla(_toptanciBakiyeGuncelleButonu , "Guncelle" , "Update User.png");
			ToptanciBakiyeAksiyonButonunuAyarla(_toptanciBakiyeSilButonu , "Sil" , "Delete Database.png");
			ToptanciBakiyeAksiyonButonunuAyarla(_toptanciBakiyeYazdirButonu , "Yazdir" , "Print.png");
			ToptanciBakiyeAksiyonButonunuAyarla(_toptanciBakiyeExcelButonu , "Excel" , "Microsoft Excel.png");
			ToptanciBakiyeAksiyonButonunuAyarla(_toptanciBakiyePdfButonu , "PDF" , "PDF.png");

			foreach(Control kontrol in new Control[] { button80 , _toptanciBakiyeGuncelleButonu , _toptanciBakiyeSilButonu , _toptanciBakiyeYazdirButonu , _toptanciBakiyeExcelButonu , _toptanciBakiyePdfButonu })
			{
				if(kontrol.Parent!=_toptanciBakiyeAksiyonPaneli)
					_toptanciBakiyeAksiyonPaneli.Controls.Add(kontrol);
			}

			_toptanciBakiyeAksiyonPaneli.Controls.Clear();
			_toptanciBakiyeAksiyonPaneli.Controls.Add(button80 , 0 , 0);
			_toptanciBakiyeAksiyonPaneli.Controls.Add(_toptanciBakiyeGuncelleButonu , 1 , 0);
			_toptanciBakiyeAksiyonPaneli.Controls.Add(_toptanciBakiyeSilButonu , 2 , 0);
			_toptanciBakiyeAksiyonPaneli.Controls.Add(_toptanciBakiyeYazdirButonu , 0 , 1);
			_toptanciBakiyeAksiyonPaneli.Controls.Add(_toptanciBakiyeExcelButonu , 1 , 1);
			_toptanciBakiyeAksiyonPaneli.Controls.Add(_toptanciBakiyePdfButonu , 2 , 1);
		}

		private void ToptanciBakiyeAksiyonDurumunuGuncelle ()
		{
			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			bool hareketSecili = _toptanciSeciliHareketId.HasValue&&!string.IsNullOrWhiteSpace(_toptanciSeciliHareketTuru);
			bool raporHazir = toptanciId.HasValue;
			decimal yeniAlim = PersonelDecimalParse(textBox110?.Text);
			decimal yeniOdeme = PersonelDecimalParse(textBox109?.Text);

			if(button80!=null)
				button80.Enabled=toptanciId.HasValue&&(yeniAlim>0m||yeniOdeme>0m);
			if(_toptanciBakiyeGuncelleButonu!=null)
				_toptanciBakiyeGuncelleButonu.Enabled=hareketSecili;
			if(_toptanciBakiyeSilButonu!=null)
				_toptanciBakiyeSilButonu.Enabled=hareketSecili;
			if(_toptanciBakiyeYazdirButonu!=null)
				_toptanciBakiyeYazdirButonu.Enabled=raporHazir;
			if(_toptanciBakiyeExcelButonu!=null)
				_toptanciBakiyeExcelButonu.Enabled=raporHazir;
			if(_toptanciBakiyePdfButonu!=null)
				_toptanciBakiyePdfButonu.Enabled=raporHazir;
		}

		private void ToptanciTemizle ()
		{
			if(textBox65!=null) textBox65.Clear();
			if(textBox104!=null) textBox104.Clear();
			if(textBox105!=null) textBox105.Clear();
			if(comboBox11!=null) comboBox11.SelectedIndex=0;
			if(dataGridView26!=null) dataGridView26.ClearSelection();
			ToptanciHareketFormTemizle(true);
			ToptanciBakiyeAlaniniGuncelle();
		}

		private void ToptanciIstatistikleriniYenile ( DataTable dt )
		{
			if(dt==null)
				return;

			label228.Text=dt.Rows.Count.ToString("N0" , _yazdirmaKulturu);
			decimal toplamBorc = dt.AsEnumerable().Sum(x => x.Field<decimal>("KalanBakiye"));
			label225.Text=toplamBorc.ToString("N2" , _yazdirmaKulturu);

			DataRow enCok = dt.AsEnumerable().OrderByDescending(x => x.Field<decimal>("ToplamAlim")).FirstOrDefault();
			DataRow enAz = dt.AsEnumerable().Where(x => x.Field<decimal>("ToplamAlim")>0m).OrderBy(x => x.Field<decimal>("ToplamAlim")).FirstOrDefault();
			label223.Text=enCok==null ? "-" : Convert.ToString(enCok["AdSoyad"]);
			label221.Text=enAz==null ? "-" : Convert.ToString(enAz["AdSoyad"]);
		}

		private void ToptanciKartGorunumunuAyarla ()
		{
			ToptanciKartBasliginiAyarla(label229 , "Toplam Toptancı Sayısı");
			ToptanciKartBasliginiAyarla(label226 , "Toplam Borç");
			ToptanciKartBasliginiAyarla(label224 , "En Çok Alım Yapılan");
			ToptanciKartBasliginiAyarla(label222 , "En Az Alım Yapılan");

			ToptanciKartDegeriniAyarla(label228 , new Size(170 , 40) , new Font("Microsoft Sans Serif" , 17.2F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162))) , new Point(30 , 110));
			ToptanciKartDegeriniAyarla(label225 , new Size(220 , 40) , new Font("Microsoft Sans Serif" , 17.2F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162))) , new Point(30 , 110));
			ToptanciKartDegeriniAyarla(label223 , new Size(250 , 46) , new Font("Microsoft Sans Serif" , 11.4F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162))) , new Point(30 , 108));
			ToptanciKartDegeriniAyarla(label221 , new Size(250 , 46) , new Font("Microsoft Sans Serif" , 11.4F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162))) , new Point(30 , 108));

			if(label227!=null)
				label227.Visible=false;
		}

		private void ToptanciUstAksiyonGorunumunuAyarla ()
		{
			ToptanciUstIkonButonunuAyarla(button79 , "Add User Male.png" , new Point(23 , 18) , new Size(90 , 76));
			ToptanciUstIkonButonunuAyarla(button78 , "Denied.png" , new Point(139 , 18) , new Size(90 , 76));
			ToptanciUstIkonButonunuAyarla(button77 , "Update User.png" , new Point(255 , 18) , new Size(96 , 76));

			ToptanciUstIkonEtiketiniAyarla(label231 , "KAYDET" , button79);
			ToptanciUstIkonEtiketiniAyarla(label232 , "SİL" , button78);
			ToptanciUstIkonEtiketiniAyarla(label230 , "GÜNCELLE" , button77);
		}

		private void ToptanciUstIkonButonunuAyarla ( Button buton , string imageKey , Point konum , Size boyut )
		{
			if(buton==null)
				return;

			int ikonBoyutu = string.Equals(imageKey , "Save.png" , StringComparison.OrdinalIgnoreCase) ? 40 : 44;

			buton.BackColor=SystemColors.Control;
			buton.BackgroundImageLayout=ImageLayout.Zoom;
			buton.FlatAppearance.BorderColor=SystemColors.Control;
			buton.FlatAppearance.BorderSize=0;
			buton.FlatAppearance.MouseOverBackColor=Color.FromArgb(224 , 224 , 224);
			buton.FlatStyle=FlatStyle.Flat;
			buton.Location=konum;
			buton.Size=boyut;
			buton.Text=string.Empty;
			buton.ImageList=null;
			buton.Image=NotButonGorseliOlustur(imageKey , new Size(ikonBoyutu , ikonBoyutu));
			buton.ImageAlign=ContentAlignment.MiddleCenter;
			buton.UseVisualStyleBackColor=false;
		}

		private void ToptanciUstIkonEtiketiniAyarla ( Label etiket , string metin , Button buton )
		{
			if(etiket==null||buton==null)
				return;

			etiket.AutoSize=true;
			etiket.Text=metin;
			etiket.Font=new Font("Microsoft Sans Serif" , 8.25F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
			etiket.Location=new Point(
				buton.Left+Math.Max(0 , ( buton.Width-etiket.PreferredWidth )/2) ,
				buton.Bottom+4);
		}

		private void ToptanciKartBasliginiAyarla ( Label label , string metin )
		{
			if(label==null)
				return;

			label.AutoSize=false;
			label.Text=metin;
			label.Font=new Font("Microsoft Sans Serif" , 10.6F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
			label.ForeColor=Color.White;
			label.Size=new Size(Math.Max(label.Width , 250) , 24);
			label.TextAlign=ContentAlignment.MiddleLeft;
			label.BringToFront();
		}

		private void ToptanciKartDegeriniAyarla ( Label label , Size boyut , Font font , Point konum )
		{
			if(label==null)
				return;

			label.AutoSize=false;
			label.Size=boyut;
			label.Font=font;
			label.Location=konum;
			label.ForeColor=Color.White;
			label.TextAlign=ContentAlignment.MiddleLeft;
			label.BringToFront();
		}

		private void ToptanciKartPictureBox_Paint ( object sender , PaintEventArgs e )
		{
			PictureBox pictureBox = sender as PictureBox;
			if(pictureBox==null)
				return;

			e.Graphics.Clear(pictureBox.BackColor);
			Image img = pictureBox.Image;
			if(img==null)
				return;

			float ikonOlcek = ReferenceEquals(pictureBox , pictureBox14) ? 0.72F : 0.82F;
			int genislik = Math.Max(24 , (int)( img.Width*ikonOlcek ));
			int yukseklik = Math.Max(24 , (int)( img.Height*ikonOlcek ));
			int x = pictureBox.ClientSize.Width-genislik-22;
			int y = ( pictureBox.ClientSize.Height-yukseklik )/2;
			e.Graphics.DrawImage(img , x , y , genislik , yukseklik);
		}

		private void ToptanciListele ()
		{
			if(dataGridView26==null)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string adIfadesi = ToptanciAdSqlIfadesi("T");
					string durumIfadesi = ToptanciDurumSqlIfadesi("T");
					string sorgu = @"SELECT T.[ToptanciID],
										" + adIfadesi + @" AS AdSoyad,
										IIF(T.[Telefon] IS NULL, '', T.[Telefon]) AS Telefon,
										" + durumIfadesi + @" AS Durum,
										(SELECT SUM(IIF(A.[Tutar] IS NULL, 0, A.[Tutar])) FROM [ToptanciAlimlari] AS A WHERE A.[ToptanciID]=T.[ToptanciID]) AS ToplamAlim,
										(SELECT SUM(IIF(O.[OdenenTutar] IS NULL, 0, O.[OdenenTutar])) FROM [ToptanciOdemeleri] AS O WHERE O.[ToptanciID]=T.[ToptanciID]) AS ToplamOdeme
									FROM [Toptancilar] AS T
									ORDER BY " + adIfadesi;

					DataTable dt = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);

					if(!dt.Columns.Contains("KalanBakiye"))
						dt.Columns.Add("KalanBakiye" , typeof(decimal));

					foreach(DataRow satir in dt.Rows)
					{
						decimal toplamAlim = satir["ToplamAlim"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamAlim"]);
						decimal toplamOdeme = satir["ToplamOdeme"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamOdeme"]);
						satir["ToplamAlim"]=toplamAlim;
						satir["ToplamOdeme"]=toplamOdeme;
						satir["KalanBakiye"]=toplamAlim-toplamOdeme;
					}

					dataGridView26.DataSource=dt;
					if(dataGridView26.Columns.Contains("ToplamAlim"))
						dataGridView26.Columns["ToplamAlim"].DefaultCellStyle.Format="N2";
					if(dataGridView26.Columns.Contains("ToplamOdeme"))
						dataGridView26.Columns["ToplamOdeme"].DefaultCellStyle.Format="N2";
					if(dataGridView26.Columns.Contains("KalanBakiye"))
						dataGridView26.Columns["KalanBakiye"].DefaultCellStyle.Format="N2";
					GridBasliklariniTurkceDuzenle(dataGridView26);
					ToptanciIstatistikleriniYenile(dt);
				}

				ToptanciBakiyeComboYenile();
				AnaSayfaGridleriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptancı listesi yüklenemedi: "+ex.Message);
			}
		}

		private void ToptanciBakiyeComboYenile ()
		{
			if(_toptanciBakiyeSecimComboBox==null)
				return;

			int? hedefToptanciId = SeciliToptanciBakiyeIdGetir();
			if(!hedefToptanciId.HasValue&&ToptanciSeciliMi(out int seciliId))
				hedefToptanciId=seciliId;

			DataTable dt = new DataTable();
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string adIfadesi = ToptanciAdSqlIfadesi("T");
					using(OleDbDataAdapter da = new OleDbDataAdapter("SELECT [ToptanciID], " + adIfadesi + " AS AdSoyad FROM [Toptancilar] AS T ORDER BY " + adIfadesi , conn))
						da.Fill(dt);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptancı seçim listesi yüklenemedi: "+ex.Message);
			}

			_toptanciBakiyeSecimYukleniyor=true;
			try
			{
				_toptanciBakiyeSecimComboBox.DataSource=null;
				_toptanciBakiyeSecimComboBox.Items.Clear();
				_toptanciBakiyeSecimComboBox.ValueMember="ToptanciID";
				_toptanciBakiyeSecimComboBox.DisplayMember="AdSoyad";
				_toptanciBakiyeSecimComboBox.DataSource=dt;

				if(hedefToptanciId.HasValue&&dt.AsEnumerable().Any(x => Convert.ToInt32(x["ToptanciID"])==hedefToptanciId.Value))
					_toptanciBakiyeSecimComboBox.SelectedValue=hedefToptanciId.Value;
				else if(dt.Rows.Count>0)
					_toptanciBakiyeSecimComboBox.SelectedIndex=0;
				else
					_toptanciBakiyeSecimComboBox.SelectedIndex=-1;
			}
			finally
			{
				_toptanciBakiyeSecimYukleniyor=false;
			}

			ToptanciBakiyeAlaniniGuncelle();
			ToptanciHareketleriListele();
		}

		private void ToptanciBakiyeAlaniniGuncelle ()
		{
			if(textBox107==null)
				return;

			decimal kalan = 0m;
			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			if(toptanciId.HasValue)
				kalan=ToptanciToplamAlimGetir(toptanciId.Value)-ToptanciToplamOdemeGetir(toptanciId.Value);

			decimal yeniAlim = PersonelDecimalParse(textBox110?.Text);
			decimal yeniOdeme = PersonelDecimalParse(textBox109?.Text);
			textBox107.Text=(kalan+yeniAlim-yeniOdeme).ToString("N2" , _yazdirmaKulturu);
			ToptanciBakiyeAksiyonDurumunuGuncelle();
		}

		private DataTable ToptanciHareketTablosuOlustur ()
		{
			DataTable dt = new DataTable();
			dt.Columns.Add("IslemID" , typeof(int));
			dt.Columns.Add("ToptanciID" , typeof(int));
			dt.Columns.Add("IslemTuru" , typeof(string));
			dt.Columns.Add("Tarih" , typeof(DateTime));
			dt.Columns.Add("BorcTutar" , typeof(decimal));
			dt.Columns.Add("OdemeTutar" , typeof(decimal));
			dt.Columns.Add("KalanBakiye" , typeof(decimal));
			dt.Columns.Add("Aciklama" , typeof(string));
			return dt;
		}

		private void ToptanciHareketleriListele ()
		{
			if(dataGridView27==null)
				return;

			DataTable dt = ToptanciHareketTablosuOlustur();
			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			if(!toptanciId.HasValue)
			{
				dataGridView27.DataSource=dt;
				GridBasliklariniTurkceDuzenle(dataGridView27);
				ToptanciSeciliHareketiTemizle();
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					using(OleDbCommand alimCmd = new OleDbCommand("SELECT [AlimID], [ToptanciID], [Tarih], IIF([Tutar] IS NULL, 0, [Tutar]) AS Tutar, IIF([Aciklama] IS NULL, '', [Aciklama]) AS Aciklama FROM [ToptanciAlimlari] WHERE [ToptanciID]=?" , conn))
					{
						alimCmd.Parameters.AddWithValue("?" , toptanciId.Value);
						using(OleDbDataReader rd = alimCmd.ExecuteReader())
						{
							while(rd!=null&&rd.Read())
							{
								dt.Rows.Add(
									rd["AlimID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["AlimID"]) ,
									rd["ToptanciID"]==DBNull.Value ? toptanciId.Value : Convert.ToInt32(rd["ToptanciID"]) ,
									"ALINAN ÜRÜN" ,
									rd["Tarih"]==DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["Tarih"]) ,
									rd["Tutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Tutar"]) ,
									0m ,
									0m ,
									Convert.ToString(rd["Aciklama"])??string.Empty);
							}
						}
					}

					using(OleDbCommand odemeCmd = new OleDbCommand("SELECT [OdemeID], [ToptanciID], [OdemeTarihi], IIF([OdenenTutar] IS NULL, 0, [OdenenTutar]) AS OdenenTutar, IIF([Aciklama] IS NULL, '', [Aciklama]) AS Aciklama FROM [ToptanciOdemeleri] WHERE [ToptanciID]=?" , conn))
					{
						odemeCmd.Parameters.AddWithValue("?" , toptanciId.Value);
						using(OleDbDataReader rd = odemeCmd.ExecuteReader())
						{
							while(rd!=null&&rd.Read())
							{
								dt.Rows.Add(
									rd["OdemeID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["OdemeID"]) ,
									rd["ToptanciID"]==DBNull.Value ? toptanciId.Value : Convert.ToInt32(rd["ToptanciID"]) ,
									"ÖDEME" ,
									rd["OdemeTarihi"]==DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["OdemeTarihi"]) ,
									0m ,
									rd["OdenenTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["OdenenTutar"]) ,
									0m ,
									Convert.ToString(rd["Aciklama"])??string.Empty);
							}
						}
					}
				}

				DataTable sirali = dt.Clone();
				decimal kalan = 0m;
				foreach(DataRow kaynakSatir in dt.AsEnumerable().OrderBy(x => x.Field<DateTime>("Tarih")).ThenBy(x => x.Field<string>("IslemTuru")).ThenBy(x => x.Field<int>("IslemID")))
				{
					decimal borc = kaynakSatir.Field<decimal>("BorcTutar");
					decimal odeme = kaynakSatir.Field<decimal>("OdemeTutar");
					kalan+=borc-odeme;

					DataRow yeni = sirali.NewRow();
					yeni["IslemID"]=kaynakSatir["IslemID"];
					yeni["ToptanciID"]=kaynakSatir["ToptanciID"];
					yeni["IslemTuru"]=kaynakSatir["IslemTuru"];
					yeni["Tarih"]=kaynakSatir["Tarih"];
					yeni["BorcTutar"]=kaynakSatir["BorcTutar"];
					yeni["OdemeTutar"]=kaynakSatir["OdemeTutar"];
					yeni["KalanBakiye"]=kalan;
					yeni["Aciklama"]=kaynakSatir["Aciklama"];
					sirali.Rows.Add(yeni);
				}

				dataGridView27.DataSource=sirali;
				if(dataGridView27.Columns.Contains("ToptanciID"))
					dataGridView27.Columns["ToptanciID"].Visible=false;
				if(dataGridView27.Columns.Contains("IslemID"))
					dataGridView27.Columns["IslemID"].Visible=false;
				if(dataGridView27.Columns.Contains("Tarih"))
					dataGridView27.Columns["Tarih"].DefaultCellStyle.Format="g";
				if(dataGridView27.Columns.Contains("BorcTutar"))
					dataGridView27.Columns["BorcTutar"].DefaultCellStyle.Format="N2";
				if(dataGridView27.Columns.Contains("OdemeTutar"))
					dataGridView27.Columns["OdemeTutar"].DefaultCellStyle.Format="N2";
				if(dataGridView27.Columns.Contains("KalanBakiye"))
					dataGridView27.Columns["KalanBakiye"].DefaultCellStyle.Format="N2";
				GridBasliklariniTurkceDuzenle(dataGridView27);
				dataGridView27.ClearSelection();
				ToptanciBakiyeAksiyonDurumunuGuncelle();
				AnaSayfaGridleriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Toptancı hareketleri listelenemedi: "+ex.Message);
			}
		}

		private void ToptanciKaydet ()
		{
			if(string.IsNullOrWhiteSpace(textBox104?.Text))
			{
				MessageBox.Show("Toptancı adı soyadı girin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string durumMetni = ToptanciDurumMetniGetir();
				List<string> alanlar = new List<string> { "[AdSoyad]" , "[Telefon]" };
				List<string> degerler = new List<string> { "?" , "?" };
				if(_toptanciAdiKolonuVar)
				{
					alanlar.Add("[ToptanciAdi]");
					degerler.Add("?");
				}
				if(_toptanciDurumMetniKolonuVar)
				{
					alanlar.Add("[DurumMetni]");
					degerler.Add("?");
				}
				if(_toptanciDurumKolonuVar)
				{
					alanlar.Add("[Durum]");
					degerler.Add("?");
				}
				string sorgu = "INSERT INTO [Toptancilar] (" + string.Join(", " , alanlar) + ") VALUES (" + string.Join(", " , degerler) + ")";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=textBox104.Text.Trim();
					cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=textBox105?.Text?.Trim()??string.Empty;
					if(_toptanciAdiKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=textBox104.Text.Trim();
					if(_toptanciDurumMetniKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=durumMetni;
					if(_toptanciDurumKolonuVar)
					{
						if(_toptanciDurumKolonuMantiksal)
							cmd.Parameters.Add("?" , OleDbType.Boolean).Value=ToptanciDurumBoolGetir();
						else
							cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=durumMetni;
					}
					cmd.ExecuteNonQuery();
				}
			}

			ToptanciTemizle();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciGuncelle ()
		{
			if(!ToptanciSeciliMi(out int toptanciId))
			{
				MessageBox.Show("Güncellenecek toptancıyı seçin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string durumMetni = ToptanciDurumMetniGetir();
				List<string> alanlar = new List<string>
				{
					"[AdSoyad]=?",
					"[Telefon]=?"
				};
				if(_toptanciAdiKolonuVar)
					alanlar.Add("[ToptanciAdi]=?");
				if(_toptanciDurumMetniKolonuVar)
					alanlar.Add("[DurumMetni]=?");
				if(_toptanciDurumKolonuVar)
					alanlar.Add("[Durum]=?");
				string sorgu = "UPDATE [Toptancilar] SET " + string.Join(", " , alanlar) + " WHERE [ToptanciID]=?";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=textBox104.Text.Trim();
					cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=textBox105?.Text?.Trim()??string.Empty;
					if(_toptanciAdiKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=textBox104.Text.Trim();
					if(_toptanciDurumMetniKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=durumMetni;
					if(_toptanciDurumKolonuVar)
					{
						if(_toptanciDurumKolonuMantiksal)
							cmd.Parameters.Add("?" , OleDbType.Boolean).Value=ToptanciDurumBoolGetir();
						else
							cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=durumMetni;
					}
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=toptanciId;
					cmd.ExecuteNonQuery();
				}
			}

			ToptanciTemizle();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciSil ()
		{
			if(!ToptanciSeciliMi(out int toptanciId))
			{
				MessageBox.Show("Silinecek toptancıyı seçin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [ToptanciAlimlari] WHERE [ToptanciID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , toptanciId);
					cmd.ExecuteNonQuery();
				}
				using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [ToptanciOdemeleri] WHERE [ToptanciID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , toptanciId);
					cmd.ExecuteNonQuery();
				}
				using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [Toptancilar] WHERE [ToptanciID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , toptanciId);
					cmd.ExecuteNonQuery();
				}
			}

			ToptanciTemizle();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			DataGridView grid = sender as DataGridView;
			if(grid==null||e.RowIndex<0||e.RowIndex>=grid.Rows.Count)
				return;

			DataGridViewRow satir = grid.Rows[e.RowIndex];
			textBox65.Text=Convert.ToString(satir.Cells["ToptanciID"].Value)??string.Empty;
			textBox104.Text=Convert.ToString(satir.Cells["AdSoyad"].Value)??string.Empty;
			textBox105.Text=Convert.ToString(satir.Cells["Telefon"].Value)??string.Empty;
			ComboBoxMetniniSec(comboBox11 , Convert.ToString(satir.Cells["Durum"].Value));

			if(_toptanciBakiyeSecimComboBox!=null&&satir.Cells["ToptanciID"].Value!=null&&satir.Cells["ToptanciID"].Value!=DBNull.Value)
			{
				_toptanciBakiyeSecimYukleniyor=true;
				try
				{
					_toptanciBakiyeSecimComboBox.SelectedValue=Convert.ToInt32(satir.Cells["ToptanciID"].Value);
				}
				finally
				{
					_toptanciBakiyeSecimYukleniyor=false;
				}
				ToptanciHareketFormTemizle(true);
				ToptanciBakiyeAlaniniGuncelle();
				ToptanciHareketleriListele();
			}
		}

		private void ToptanciBakiyeSecim_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_toptanciBakiyeSecimYukleniyor)
				return;

			ToptanciHareketFormTemizle(true);
			ToptanciBakiyeAlaniniGuncelle();
			ToptanciHareketleriListele();
		}

		private void ToptanciTutar_TextChanged ( object sender , EventArgs e ) => ToptanciBakiyeAlaniniGuncelle();

		private void ToptanciHareketKaydet_Click ( object sender , EventArgs e )
		{
			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			if(!toptanciId.HasValue)
			{
				MessageBox.Show("Önce toptancı seçin!");
				return;
			}

			decimal alimTutari = PersonelDecimalParse(textBox110?.Text);
			decimal odemeTutari = PersonelDecimalParse(textBox109?.Text);
			if(alimTutari<=0m&&odemeTutari<=0m)
			{
				MessageBox.Show("Alınan ürün veya verilen ödeme tutarı girin!");
				return;
			}

			string aciklama = textBox108?.Text?.Trim()??string.Empty;
			DateTime tarih = _toptanciTarihPicker==null ? DateTime.Now : _toptanciTarihPicker.Value;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				if(alimTutari>0m)
				{
					using(OleDbCommand cmd = new OleDbCommand("INSERT INTO [ToptanciAlimlari] ([ToptanciID], [Tarih], [Tutar], [Aciklama]) VALUES (?, ?, ?, ?)" , conn))
					{
						cmd.Parameters.AddWithValue("?" , toptanciId.Value);
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=alimTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.ExecuteNonQuery();
					}
				}
				if(odemeTutari>0m)
				{
					using(OleDbCommand cmd = new OleDbCommand("INSERT INTO [ToptanciOdemeleri] ([ToptanciID], [OdemeTarihi], [OdenenTutar], [Aciklama]) VALUES (?, ?, ?, ?)" , conn))
					{
						cmd.Parameters.AddWithValue("?" , toptanciId.Value);
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=odemeTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.ExecuteNonQuery();
					}
				}
			}

			ToptanciHareketFormTemizle(true);
			ToptanciBakiyeAlaniniGuncelle();
			ToptanciHareketleriListele();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciHareketGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			DataGridView grid = sender as DataGridView;
			if(grid==null||e.RowIndex<0||e.RowIndex>=grid.Rows.Count)
				return;

			DataGridViewRow satir = grid.Rows[e.RowIndex];
			if(satir==null||satir.IsNewRow)
				return;

			_toptanciSeciliHareketId=satir.Cells["IslemID"].Value==null||satir.Cells["IslemID"].Value==DBNull.Value
				? (int?)null
				: Convert.ToInt32(satir.Cells["IslemID"].Value);
			_toptanciSeciliHareketTuru=Convert.ToString(satir.Cells["IslemTuru"].Value)??string.Empty;

			decimal borcTutari = satir.Cells["BorcTutar"].Value==null||satir.Cells["BorcTutar"].Value==DBNull.Value ? 0m : Convert.ToDecimal(satir.Cells["BorcTutar"].Value);
			decimal odemeTutari = satir.Cells["OdemeTutar"].Value==null||satir.Cells["OdemeTutar"].Value==DBNull.Value ? 0m : Convert.ToDecimal(satir.Cells["OdemeTutar"].Value);
			if(textBox110!=null)
				textBox110.Text=borcTutari>0m ? borcTutari.ToString("N2" , _yazdirmaKulturu) : string.Empty;
			if(textBox109!=null)
				textBox109.Text=odemeTutari>0m ? odemeTutari.ToString("N2" , _yazdirmaKulturu) : string.Empty;
			if(textBox108!=null)
				textBox108.Text=Convert.ToString(satir.Cells["Aciklama"].Value)??string.Empty;
			if(_toptanciTarihPicker!=null&&satir.Cells["Tarih"].Value!=null&&satir.Cells["Tarih"].Value!=DBNull.Value)
			{
				try
				{
					_toptanciTarihPicker.Value=Convert.ToDateTime(satir.Cells["Tarih"].Value);
				}
				catch
				{
					_toptanciTarihPicker.Value=DateTime.Now;
				}
			}

			ToptanciBakiyeAlaniniGuncelle();
		}

		private void ToptanciHareketGuncelle_Click ( object sender , EventArgs e )
		{
			if(!_toptanciSeciliHareketId.HasValue||string.IsNullOrWhiteSpace(_toptanciSeciliHareketTuru))
			{
				MessageBox.Show("Guncellemek icin once hareket secin!");
				return;
			}

			int? toptanciId = SeciliToptanciBakiyeIdGetir();
			if(!toptanciId.HasValue)
			{
				MessageBox.Show("Once toptanci secin!");
				return;
			}

			decimal alimTutari = PersonelDecimalParse(textBox110?.Text);
			decimal odemeTutari = PersonelDecimalParse(textBox109?.Text);
			string aciklama = textBox108?.Text?.Trim()??string.Empty;
			DateTime tarih = _toptanciTarihPicker==null ? DateTime.Now : _toptanciTarihPicker.Value;
			bool alimHareketi = KarsilastirmaMetniHazirla(_toptanciSeciliHareketTuru).Contains("ALINAN");

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				if(alimHareketi)
				{
					if(alimTutari<=0m)
					{
						MessageBox.Show("Secili alim kaydi icin alinan urun tutari girin!");
						return;
					}

					using(OleDbCommand cmd = new OleDbCommand("UPDATE [ToptanciAlimlari] SET [ToptanciID]=?, [Tarih]=?, [Tutar]=?, [Aciklama]=? WHERE [AlimID]=?" , conn))
					{
						cmd.Parameters.AddWithValue("?" , toptanciId.Value);
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=alimTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_toptanciSeciliHareketId.Value;
						cmd.ExecuteNonQuery();
					}
				}
				else
				{
					if(odemeTutari<=0m)
					{
						MessageBox.Show("Secili odeme kaydi icin verilen odeme tutari girin!");
						return;
					}

					using(OleDbCommand cmd = new OleDbCommand("UPDATE [ToptanciOdemeleri] SET [ToptanciID]=?, [OdemeTarihi]=?, [OdenenTutar]=?, [Aciklama]=? WHERE [OdemeID]=?" , conn))
					{
						cmd.Parameters.AddWithValue("?" , toptanciId.Value);
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=odemeTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_toptanciSeciliHareketId.Value;
						cmd.ExecuteNonQuery();
					}
				}
			}

			ToptanciHareketFormTemizle(true);
			ToptanciBakiyeAlaniniGuncelle();
			ToptanciHareketleriListele();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciHareketSil_Click ( object sender , EventArgs e )
		{
			if(!_toptanciSeciliHareketId.HasValue||string.IsNullOrWhiteSpace(_toptanciSeciliHareketTuru))
			{
				MessageBox.Show("Silmek icin once hareket secin!");
				return;
			}

			if(MessageBox.Show("Secili hareket silinsin mi?" , "Toptanci Bakiye" , MessageBoxButtons.YesNo , MessageBoxIcon.Question)!=DialogResult.Yes)
				return;

			bool alimHareketi = KarsilastirmaMetniHazirla(_toptanciSeciliHareketTuru).Contains("ALINAN");
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = alimHareketi
					? "DELETE FROM [ToptanciAlimlari] WHERE [AlimID]=?"
					: "DELETE FROM [ToptanciOdemeleri] WHERE [OdemeID]=?";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , _toptanciSeciliHareketId.Value);
					cmd.ExecuteNonQuery();
				}
			}

			ToptanciHareketFormTemizle(true);
			ToptanciBakiyeAlaniniGuncelle();
			ToptanciHareketleriListele();
			ToptanciListele();
			GunlukSatisVerileriniYenile();
		}

		private void ToptanciKaydet_Click ( object sender , EventArgs e ) => ToptanciKaydet();
		private void ToptanciGuncelle_Click ( object sender , EventArgs e ) => ToptanciGuncelle();
		private void ToptanciSil_Click ( object sender , EventArgs e ) => ToptanciSil();
		private void ToptanciTemizle_Click ( object sender , EventArgs e ) => ToptanciTemizle();

		private void KurToptanciSekmesi ()
		{
			if(tabPage24==null||tabPage26==null)
				return;

			groupBox52.Text=string.Empty;
			groupBox53.Text="Toptancı İşlemleri";
			groupBox54.Text="Toptancılar";
			ToptanciKartGorunumunuAyarla();
			ToptanciUstAksiyonGorunumunuAyarla();
			label220.Text="TOPTANCI ID :";
			label219.Text="AD SOYAD :";
			label218.Text="TELEFON :";
			label214.Text="DURUM :";
			label216.Visible=false;
			label215.Visible=false;
			label217.Visible=false;
			comboBox10.Visible=false;
			textBox106.Visible=false;
			flowLayoutPanel25.Size=new Size(280 , 160);
			flowLayoutPanel25.PerformLayout();
			label214.Location=new Point(
				label214.Location.X ,
				flowLayoutPanel25.Top+comboBox11.Top+Math.Max(0 , ( comboBox11.Height-label214.Height )/2));
			textBox65.ReadOnly=true;
			textBox65.BackColor=SystemColors.ControlLight;
			comboBox11.DropDownStyle=ComboBoxStyle.DropDownList;

			button79.Click-=ToptanciKaydet_Click;
			button79.Click+=ToptanciKaydet_Click;
			button78.Click-=ToptanciSil_Click;
			button78.Click+=ToptanciSil_Click;
			button77.Click-=ToptanciGuncelle_Click;
			button77.Click+=ToptanciGuncelle_Click;
			button76.Click-=ToptanciTemizle_Click;
			button76.Click+=ToptanciTemizle_Click;

			dataGridView26.CellClick-=ToptanciGrid_CellClick;
			dataGridView26.CellClick+=ToptanciGrid_CellClick;
			textBox110.TextChanged-=ToptanciTutar_TextChanged;
			textBox110.TextChanged+=ToptanciTutar_TextChanged;
			textBox109.TextChanged-=ToptanciTutar_TextChanged;
			textBox109.TextChanged+=ToptanciTutar_TextChanged;
			textBox110.KeyPress-=SepetSayisal_KeyPress;
			textBox110.KeyPress+=SepetSayisal_KeyPress;
			textBox109.KeyPress-=SepetSayisal_KeyPress;
			textBox109.KeyPress+=SepetSayisal_KeyPress;
			button80.Click-=ToptanciHareketKaydet_Click;
			button80.Click+=ToptanciHareketKaydet_Click;
			ToptanciBakiyeAksiyonPaneliniHazirla();
			if(_toptanciBakiyeGuncelleButonu!=null)
			{
				_toptanciBakiyeGuncelleButonu.Click-=ToptanciHareketGuncelle_Click;
				_toptanciBakiyeGuncelleButonu.Click+=ToptanciHareketGuncelle_Click;
			}
			if(_toptanciBakiyeSilButonu!=null)
			{
				_toptanciBakiyeSilButonu.Click-=ToptanciHareketSil_Click;
				_toptanciBakiyeSilButonu.Click+=ToptanciHareketSil_Click;
			}
			if(_toptanciBakiyeYazdirButonu!=null)
			{
				_toptanciBakiyeYazdirButonu.Click-=ToptanciBakiyeYazdirButonu_Click;
				_toptanciBakiyeYazdirButonu.Click+=ToptanciBakiyeYazdirButonu_Click;
			}
			if(_toptanciBakiyeExcelButonu!=null)
			{
				_toptanciBakiyeExcelButonu.Click-=ToptanciBakiyeExcelButonu_Click;
				_toptanciBakiyeExcelButonu.Click+=ToptanciBakiyeExcelButonu_Click;
			}
			if(_toptanciBakiyePdfButonu!=null)
			{
				_toptanciBakiyePdfButonu.Click-=ToptanciBakiyePdfButonu_Click;
				_toptanciBakiyePdfButonu.Click+=ToptanciBakiyePdfButonu_Click;
			}

			label237.Text="TOPTANCI BAKİYE";
			panel18.Size=new Size(430 , 404);

			if(_toptanciBakiyeSecimLabel==null)
			{
				_toptanciBakiyeSecimLabel=new Label();
				_toptanciBakiyeSecimLabel.AutoSize=true;
				_toptanciBakiyeSecimLabel.Font=new Font("Microsoft Sans Serif" , 9F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_toptanciBakiyeSecimLabel.Text="TOPTANCI :";
				panel18.Controls.Add(_toptanciBakiyeSecimLabel);
			}

			if(_toptanciBakiyeSecimComboBox==null)
			{
				_toptanciBakiyeSecimComboBox=new ComboBox();
				_toptanciBakiyeSecimComboBox.DropDownStyle=ComboBoxStyle.DropDownList;
				_toptanciBakiyeSecimComboBox.Font=new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_toptanciBakiyeSecimComboBox.Size=new Size(248 , 28);
				panel18.Controls.Add(_toptanciBakiyeSecimComboBox);
			}

			if(_toptanciTarihLabel==null)
			{
				_toptanciTarihLabel=new Label();
				_toptanciTarihLabel.AutoSize=true;
				_toptanciTarihLabel.Font=new Font("Microsoft Sans Serif" , 9F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_toptanciTarihLabel.Text="TARİH / SAAT :";
				panel18.Controls.Add(_toptanciTarihLabel);
			}

			if(_toptanciTarihPicker==null)
			{
				_toptanciTarihPicker=new DateTimePicker();
				_toptanciTarihPicker.Format=DateTimePickerFormat.Custom;
				_toptanciTarihPicker.CustomFormat="dd.MM.yyyy HH:mm";
				_toptanciTarihPicker.Font=new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , ((byte)(162)));
				_toptanciTarihPicker.Size=new Size(248 , 27);
				_toptanciTarihPicker.Value=DateTime.Now;
				panel18.Controls.Add(_toptanciTarihPicker);
			}

			_toptanciBakiyeSecimComboBox.SelectedIndexChanged-=ToptanciBakiyeSecim_SelectedIndexChanged;
			_toptanciBakiyeSecimComboBox.SelectedIndexChanged+=ToptanciBakiyeSecim_SelectedIndexChanged;

			_toptanciBakiyeSecimLabel.Location=new Point(12 , 48);
			_toptanciBakiyeSecimComboBox.Location=new Point(140 , 44);
			_toptanciTarihLabel.Location=new Point(12 , 84);
			_toptanciTarihPicker.Location=new Point(140 , 80);
			label236.Text="ALINAN ÜRÜN TUTAR :";
			label236.Location=new Point(12 , 124);
			textBox110.Location=new Point(140 , 120);
			textBox110.ReadOnly=false;
			textBox110.BackColor=Color.White;
			label235.Text="VERİLEN ÖDEME :";
			label235.Location=new Point(12 , 160);
			textBox109.Location=new Point(140 , 156);
			label234.Location=new Point(12 , 196);
			textBox108.Location=new Point(140 , 192);
			textBox108.Size=new Size(248 , 44);
			label233.Location=new Point(12 , 252);
			textBox107.Location=new Point(140 , 248);
			if(_toptanciBakiyeAksiyonPaneli!=null)
			{
				_toptanciBakiyeAksiyonPaneli.Location=new Point(12 , 288);
				_toptanciBakiyeAksiyonPaneli.Size=new Size(390 , 84);
			}
			button80.Text="Kaydet";

			dataGridView27.Location=new Point(21 , panel18.Bottom+14);
			if(tabPage26!=null)
			{
				int yeniYukseklik = Math.Max(220 , tabPage26.ClientSize.Height-dataGridView27.Top-25);
				dataGridView27.Size=new Size(dataGridView27.Width , yeniYukseklik);
			}

			dataGridView26.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
			dataGridView26.MultiSelect=false;
			dataGridView27.CellClick-=ToptanciHareketGrid_CellClick;
			dataGridView27.CellClick+=ToptanciHareketGrid_CellClick;
			dataGridView27.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
			dataGridView27.MultiSelect=false;
			dataGridView27.ReadOnly=true;
			DatagridviewSetting(dataGridView26);
			DatagridviewSetting(dataGridView27);
			ToptanciBakiyeAksiyonDurumunuGuncelle();

			ToptanciDurumComboYenile();
			ToptanciListele();
			ToptanciTemizle();
		}

		private void BaglaPersonelIslemEventleri ()
		{
			button33.Click-=PersonelKaydet_Click;
			button33.Click+=PersonelKaydet_Click;
			button32.Click-=PersonelSil_Click;
			button32.Click+=PersonelSil_Click;
			button31.Click-=PersonelGuncelle_Click;
			button31.Click+=PersonelGuncelle_Click;
			button30.Click-=PersonelTemizle_Click;
			button30.Click+=PersonelTemizle_Click;

			dataGridView13.CellClick-=PersonelGrid_CellClick;
			dataGridView13.CellClick+=PersonelGrid_CellClick;
			dataGridView25.CellClick-=PersonelBakiyeGrid_CellClick;
			dataGridView25.CellClick+=PersonelBakiyeGrid_CellClick;
			
			comboBox7.SelectedIndexChanged-=PersonelDepartman_SelectedIndexChanged;
			comboBox7.SelectedIndexChanged+=PersonelDepartman_SelectedIndexChanged;
			if(_personelBakiyeSecimComboBox!=null)
			{
				_personelBakiyeSecimComboBox.SelectedIndexChanged-=PersonelBakiyePersonel_SelectedIndexChanged;
				_personelBakiyeSecimComboBox.SelectedIndexChanged+=PersonelBakiyePersonel_SelectedIndexChanged;
			}
			if(_personelBakiyeTarihPicker!=null)
			{
				_personelBakiyeTarihPicker.ValueChanged-=PersonelBakiyeTarih_ValueChanged;
				_personelBakiyeTarihPicker.ValueChanged+=PersonelBakiyeTarih_ValueChanged;
			}

			textBox100.TextChanged-=PersonelBakiyeDegisti_TextChanged;
			textBox100.TextChanged+=PersonelBakiyeDegisti_TextChanged;
			textBox99.KeyPress-=SepetSayisal_KeyPress;
			textBox99.KeyPress+=SepetSayisal_KeyPress;
			textBox100.KeyPress-=SepetSayisal_KeyPress;
			textBox100.KeyPress+=SepetSayisal_KeyPress;

			button75.Click-=PersonelOdemeEkle_Click;
			button75.Click+=PersonelOdemeEkle_Click;
			if(_personelOdemeEkleButonu!=null)
			{
				_personelOdemeEkleButonu.Click-=PersonelOdemeYeniKayit_Click;
				_personelOdemeEkleButonu.Click+=PersonelOdemeYeniKayit_Click;
			}
			if(_personelOdemeGuncelleButonu!=null)
			{
				_personelOdemeGuncelleButonu.Click-=PersonelBakiyeOdemeGuncelle_Click;
				_personelOdemeGuncelleButonu.Click+=PersonelBakiyeOdemeGuncelle_Click;
			}
			if(_personelOdemeSilButonu!=null)
			{
				_personelOdemeSilButonu.Click-=PersonelBakiyeOdemeSil_Click;
				_personelOdemeSilButonu.Click+=PersonelBakiyeOdemeSil_Click;
			}
		}

		private void PersonelDurumComboYenile ()
		{
			string seciliMetin = comboBox1.Text;
			comboBox1.DataSource=null;
			comboBox1.Items.Clear();
			comboBox1.Items.Add("AKTİF");
			comboBox1.Items.Add("PASİF");
			comboBox1.Items.Add("BORÇLU");

			if(!ComboBoxMetniniSec(comboBox1 , seciliMetin))
				comboBox1.SelectedIndex=0;
		}

		private void DepartmanComboYenile ()
		{
			if(comboBox7==null)
				return;

			object seciliDeger = comboBox7.SelectedValue;
			DoldurComboBox(comboBox7 , "SELECT DepartmanID, DepartmanAdi FROM [Departmanlar] ORDER BY DepartmanAdi" , "DepartmanAdi" , "DepartmanID");
			if(seciliDeger!=null&&seciliDeger!=DBNull.Value)
				comboBox7.SelectedValue=seciliDeger;
		}

		private int? SeciliPersonelBakiyeIdGetir ()
		{
			if(_personelBakiyeSecimComboBox?.SelectedValue==null||_personelBakiyeSecimComboBox.SelectedValue==DBNull.Value)
				return null;

			if(int.TryParse(_personelBakiyeSecimComboBox.SelectedValue.ToString() , out int personelId))
				return personelId;

			return null;
		}

		private void PersonelBakiyeComboYenile ()
		{
			if(_personelBakiyeSecimComboBox==null)
				return;

			int? hedefPersonelId = SeciliPersonelBakiyeIdGetir();
			if(!hedefPersonelId.HasValue&&PersonelSeciliMi(out int seciliPersonelId))
				hedefPersonelId=seciliPersonelId;

			DataTable dt = new DataTable();
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT P.[PersonelID],
										IIF(P.[AdSoyad] IS NULL, '', P.[AdSoyad]) &
										IIF(D.[DepartmanAdi] IS NULL OR D.[DepartmanAdi]='', '', ' - ' & D.[DepartmanAdi]) AS PersonelSecim
									FROM [Personeller] AS P
									LEFT JOIN [Departmanlar] AS D ON CLng(IIF(P.[DepartmanID] IS NULL, 0, P.[DepartmanID])) = D.[DepartmanID]
									ORDER BY IIF(P.[AdSoyad] IS NULL, '', P.[AdSoyad])";

					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Personel seçim listesi yüklenemedi: "+ex.Message);
			}

			_personelBakiyeSecimYukleniyor=true;
			try
			{
				_personelBakiyeSecimComboBox.DataSource=null;
				_personelBakiyeSecimComboBox.Items.Clear();
				_personelBakiyeSecimComboBox.ValueMember="PersonelID";
				_personelBakiyeSecimComboBox.DisplayMember="PersonelSecim";
				_personelBakiyeSecimComboBox.DataSource=dt;

				if(hedefPersonelId.HasValue&&dt.AsEnumerable().Any(x => Convert.ToInt32(x["PersonelID"])==hedefPersonelId.Value))
					_personelBakiyeSecimComboBox.SelectedValue=hedefPersonelId.Value;
				else if(dt.Rows.Count>0)
					_personelBakiyeSecimComboBox.SelectedIndex=0;
				else
					_personelBakiyeSecimComboBox.SelectedIndex=-1;
			}
			finally
			{
				_personelBakiyeSecimYukleniyor=false;
			}

			PersonelBakiyeSeciminiUygula(false);
		}

		private void PersonelBakiyePersonelSec ( int personelId , bool girisAlanlariniTemizle )
		{
			if(_personelBakiyeSecimComboBox==null)
			{
				textBox54.Text=personelId.ToString();
				PersonelBakiyeSeciminiUygula(girisAlanlariniTemizle);
				return;
			}

			_personelBakiyeSecimYukleniyor=true;
			try
			{
				_personelBakiyeSecimComboBox.SelectedValue=personelId;
			}
			finally
			{
				_personelBakiyeSecimYukleniyor=false;
			}

			PersonelBakiyeSeciminiUygula(girisAlanlariniTemizle);
		}

		private void PersonelBakiyeSeciminiUygula ( bool girisAlanlariniTemizle )
		{
			int? seciliPersonelId = SeciliPersonelBakiyeIdGetir();
			textBox54.Text=seciliPersonelId.HasValue ? seciliPersonelId.Value.ToString() : string.Empty;

			if(girisAlanlariniTemizle)
				PersonelOdemeSeciminiTemizle(true , true);

			PersonelBakiyeAlaniniGuncelle();
			PersonelBakiyeListele();
		}

		private void PersonelBakiyePersonel_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_personelBakiyeSecimYukleniyor)
				return;

			PersonelBakiyeSeciminiUygula(true);
		}

		private void PersonelListele ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					string durumAlanı = _personelDurumKolonuVar
						? "IIF(P.[PersonelDurumu] IS NULL OR P.[PersonelDurumu]='', IIF(P.[AktifMi], 'AKTİF', 'PASİF'), P.[PersonelDurumu]) AS PersonelDurumu"
						: "IIF(P.[AktifMi], 'AKTİF', 'PASİF') AS PersonelDurumu";
					string aylikMaasAlanı = PersonelAylikMaasSqlIfadesi("P" , "D");

					string sorgu = @"SELECT P.[PersonelID],
										P.[AdSoyad],
										P.[Telefon],
										P.[İseGirisTarihi],
										" + aylikMaasAlanı + @" AS AylikMaas,
										P.[DepartmanID],
										IIF(D.[DepartmanAdi] IS NULL, '', D.[DepartmanAdi]) AS DepartmanAdi,
										" + durumAlanı + @",
										P.[AktifMi]
									FROM [Personeller] AS P
									LEFT JOIN [Departmanlar] AS D ON CLng(IIF(P.[DepartmanID] IS NULL, 0, P.[DepartmanID])) = D.[DepartmanID]
									ORDER BY IIF(D.[DepartmanAdi] IS NULL, '', D.[DepartmanAdi]), IIF(P.[AdSoyad] IS NULL, '', P.[AdSoyad])";

					DataTable dt = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);

					dataGridView13.DataSource=dt;
					if(dataGridView13.Columns.Contains("DepartmanID"))
						dataGridView13.Columns["DepartmanID"].Visible=false;
					if(dataGridView13.Columns.Contains("AktifMi"))
						dataGridView13.Columns["AktifMi"].Visible=false;
					if(dataGridView13.Columns.Contains("AylikMaas"))
						dataGridView13.Columns["AylikMaas"].DefaultCellStyle.Format="N2";
					if(dataGridView13.Columns.Contains("İseGirisTarihi"))
						dataGridView13.Columns["İseGirisTarihi"].DefaultCellStyle.Format="d";

					GridBasliklariniTurkceDuzenle(dataGridView13);
					PersonelBakiyeComboYenile();
				}

				PersonelIstatistikleriniYenile();
				AnaSayfaGridleriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Personel listesi yüklenemedi: "+ex.Message);
			}
		}

		private void PersonelIstatistikleriniYenile ()
		{
			label93.Text="Toplam Personel Sayısı";
			label95.Text="Usta Ücreti";
			label134.Text="Kalfa Ücreti";
			label135.Text="Çırak Ücreti";

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					using(OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM [Personeller]" , conn))
						label92.Text=Convert.ToInt32(cmd.ExecuteScalar()).ToString();

					label96.Text=DepartmanVarsayilanMaasiGetir(conn , "USTA").ToString("N2" , _yazdirmaKulturu);
					label136.Text=DepartmanVarsayilanMaasiGetir(conn , "KALFA").ToString("N2" , _yazdirmaKulturu);
					label137.Text=DepartmanVarsayilanMaasiGetir(conn , "ÇIRAK").ToString("N2" , _yazdirmaKulturu);
				}
			}
			catch
			{
				label92.Text="0";
				label96.Text="0,00";
				label136.Text="0,00";
				label137.Text="0,00";
			}
		}

		private decimal DepartmanVarsayilanMaasiGetir ( OleDbConnection conn , string departmanAdi )
		{
			using(OleDbCommand cmd = new OleDbCommand("SELECT [DepartmanAdi], IIF([VarsayilanMaas] IS NULL, 0, [VarsayilanMaas]) AS VarsayilanMaas FROM [Departmanlar]" , conn))
			using(OleDbDataReader rd = cmd.ExecuteReader())
			{
				while(rd!=null&&rd.Read())
				{
					if(!string.Equals(
						KarsilastirmaMetniHazirla(rd["DepartmanAdi"]?.ToString()) ,
						KarsilastirmaMetniHazirla(departmanAdi) ,
						StringComparison.Ordinal))
						continue;

					return rd["VarsayilanMaas"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["VarsayilanMaas"]);
				}
			}

			return 0m;
		}

		private decimal DepartmanVarsayilanMaasiGetirById ( int departmanId )
		{
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT IIF([VarsayilanMaas] IS NULL, 0, [VarsayilanMaas]) FROM [Departmanlar] WHERE [DepartmanID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , departmanId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private string PersonelAylikMaasSqlIfadesi ( string personelTakmaAdi , string departmanTakmaAdi )
		{
			if(_departmanMaasKolonuVar)
				return "IIF(" + personelTakmaAdi + ".[AylikMaas] IS NULL OR " + personelTakmaAdi + ".[AylikMaas]=0, IIF(" + departmanTakmaAdi + ".[VarsayilanMaas] IS NULL, 0, " + departmanTakmaAdi + ".[VarsayilanMaas]), " + personelTakmaAdi + ".[AylikMaas])";

			return "IIF(" + personelTakmaAdi + ".[AylikMaas] IS NULL, 0, " + personelTakmaAdi + ".[AylikMaas])";
		}

		private DateTime PersonelMaasDonemBaslangiciGetir ( DateTime referansTarihi )
		{
			DateTime tarih = referansTarihi.Date;
			return tarih.Day>=PersonelMaasDonemBaslangicGunu
				? new DateTime(tarih.Year , tarih.Month , PersonelMaasDonemBaslangicGunu)
				: new DateTime(tarih.Year , tarih.Month , 1).AddMonths(-1).AddDays(PersonelMaasDonemBaslangicGunu-1);
		}

		private DateTime PersonelMaasDonemBitisiGetir ( DateTime donemBaslangici )
		{
			return donemBaslangici.Date.AddMonths(1).AddDays(-1);
		}

		private string PersonelMaasDonemMetniGetir ( DateTime donemBaslangici , DateTime donemBitisi )
		{
			return "DÖNEM : " + donemBaslangici.ToString("dd.MM.yyyy" , _yazdirmaKulturu) + " - " + donemBitisi.ToString("dd.MM.yyyy" , _yazdirmaKulturu);
		}

		private decimal PersonelAylikMaasiGetirById ( OleDbConnection conn , int personelId )
		{
			string maasIfadesi = PersonelAylikMaasSqlIfadesi("P" , "D");
			string sorgu = "SELECT " + maasIfadesi + " FROM [Personeller] AS P LEFT JOIN [Departmanlar] AS D ON CLng(IIF(P.[DepartmanID] IS NULL, 0, P.[DepartmanID])) = D.[DepartmanID] WHERE P.[PersonelID]=?";
			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
			{
				cmd.Parameters.AddWithValue("?" , personelId);
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
			}
		}

		private bool PersonelMaasDoneminiGetirVeyaOlustur ( OleDbConnection conn , int personelId , DateTime referansTarihi , out int donemId , out decimal maasTutari , out DateTime donemBaslangici , out DateTime donemBitisi )
		{
			donemId=0;
			maasTutari=0m;
			donemBaslangici=PersonelMaasDonemBaslangiciGetir(referansTarihi);
			donemBitisi=PersonelMaasDonemBitisiGetir(donemBaslangici);
			DateTime referansGun = referansTarihi.Date;

			if(!_personelMaasDonemTablosuVar)
				return false;

			using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [DonemID], [MaasTutari], [DonemBaslangic], [DonemBitis] FROM [PersonelMaasDonemleri] WHERE [PersonelID]=? AND [DonemBaslangic] <= ? AND [DonemBitis] >= ? ORDER BY [DonemID]" , conn))
			{
				cmd.Parameters.AddWithValue("?" , personelId);
				cmd.Parameters.Add("?" , OleDbType.Date).Value=referansGun;
				cmd.Parameters.Add("?" , OleDbType.Date).Value=referansGun;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd!=null&&rd.Read())
					{
						donemId=rd["DonemID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["DonemID"]);
						maasTutari=rd["MaasTutari"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["MaasTutari"]);
						donemBaslangici=rd["DonemBaslangic"]==DBNull.Value ? donemBaslangici : Convert.ToDateTime(rd["DonemBaslangic"]).Date;
						donemBitisi=rd["DonemBitis"]==DBNull.Value ? donemBitisi : Convert.ToDateTime(rd["DonemBitis"]).Date;
						return donemId>0;
					}
				}
			}

			maasTutari=PersonelAylikMaasiGetirById(conn , personelId);
			using(OleDbCommand ekleCmd = new OleDbCommand("INSERT INTO [PersonelMaasDonemleri] ([PersonelID], [DonemBaslangic], [DonemBitis], [MaasTutari]) VALUES (?, ?, ?, ?)" , conn))
			{
				ekleCmd.Parameters.AddWithValue("?" , personelId);
				ekleCmd.Parameters.Add("?" , OleDbType.Date).Value=donemBaslangici;
				ekleCmd.Parameters.Add("?" , OleDbType.Date).Value=donemBitisi;
				ekleCmd.Parameters.Add("?" , OleDbType.Currency).Value=maasTutari;
				ekleCmd.ExecuteNonQuery();
			}

			using(OleDbCommand kimlikCmd = new OleDbCommand("SELECT @@IDENTITY" , conn))
			{
				object sonuc = kimlikCmd.ExecuteScalar();
				donemId=sonuc==null||sonuc==DBNull.Value ? 0 : Convert.ToInt32(sonuc);
			}

			return donemId>0;
		}

		private bool PersonelAktifMaasDoneminiGetirVeyaOlustur ( int personelId , DateTime referansTarihi , out int donemId , out decimal maasTutari , out DateTime donemBaslangici , out DateTime donemBitisi )
		{
			donemId=0;
			maasTutari=0m;
			donemBaslangici=DateTime.MinValue;
			donemBitisi=DateTime.MinValue;

			if(!PersonelOdemeTablosunuHazirla()||!PersonelMaasDonemTablosunuHazirla())
				return false;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				return PersonelMaasDoneminiGetirVeyaOlustur(conn , personelId , referansTarihi , out donemId , out maasTutari , out donemBaslangici , out donemBitisi);
			}
		}

		private decimal PersonelToplamOdenenTutarGetirByDonemId ( int donemId )
		{
			if(!_personelOdemeTablosuVar||donemId<=0)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT SUM([OdenenTutar]) FROM [PersonelOdemeleri] WHERE [DonemID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , donemId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private void PersonelMaasDonemleriniTekillestir ( OleDbConnection conn )
		{
			if(conn==null||!_personelMaasDonemTablosuVar)
				return;

			DataTable donemler = new DataTable();
			using(OleDbDataAdapter da = new OleDbDataAdapter("SELECT [DonemID], [PersonelID], [DonemBaslangic], [DonemBitis] FROM [PersonelMaasDonemleri] ORDER BY [PersonelID], [DonemBaslangic], [DonemBitis], [DonemID]" , conn))
				da.Fill(donemler);

			Dictionary<string, int> tekilDonemler = new Dictionary<string, int>();
			foreach(DataRow satir in donemler.Rows)
			{
				if(satir["DonemID"]==DBNull.Value||satir["PersonelID"]==DBNull.Value||satir["DonemBaslangic"]==DBNull.Value||satir["DonemBitis"]==DBNull.Value)
					continue;

				int mevcutDonemId = Convert.ToInt32(satir["DonemID"]);
				int personelId = Convert.ToInt32(satir["PersonelID"]);
				DateTime donemBaslangici = Convert.ToDateTime(satir["DonemBaslangic"]).Date;
				DateTime donemBitisi = Convert.ToDateTime(satir["DonemBitis"]).Date;
				string anahtar = personelId.ToString(CultureInfo.InvariantCulture)+"|"+donemBaslangici.ToString("yyyyMMdd" , CultureInfo.InvariantCulture)+"|"+donemBitisi.ToString("yyyyMMdd" , CultureInfo.InvariantCulture);

				if(!tekilDonemler.TryGetValue(anahtar , out int korunacakDonemId))
				{
					tekilDonemler[anahtar]=mevcutDonemId;
					continue;
				}

				if(_personelOdemeTablosuVar&&KolonVarMi(conn , "PersonelOdemeleri" , "DonemID"))
				{
					using(OleDbCommand odemeCmd = new OleDbCommand("UPDATE [PersonelOdemeleri] SET [DonemID]=? WHERE [DonemID]=?" , conn))
					{
						odemeCmd.Parameters.AddWithValue("?" , korunacakDonemId);
						odemeCmd.Parameters.AddWithValue("?" , mevcutDonemId);
						odemeCmd.ExecuteNonQuery();
					}
				}

				using(OleDbCommand silCmd = new OleDbCommand("DELETE FROM [PersonelMaasDonemleri] WHERE [DonemID]=?" , conn))
				{
					silCmd.Parameters.AddWithValue("?" , mevcutDonemId);
					silCmd.ExecuteNonQuery();
				}
			}
		}

		private void PersonelOdemelerineDonemAta ( OleDbConnection conn )
		{
			if(conn==null||!_personelOdemeTablosuVar||!_personelMaasDonemTablosuVar||!KolonVarMi(conn , "PersonelOdemeleri" , "DonemID"))
				return;

			DataTable eksikKayitlar = new DataTable();
			using(OleDbDataAdapter da = new OleDbDataAdapter("SELECT [OdemeID], [PersonelID], [OdemeTarihi] FROM [PersonelOdemeleri] WHERE [DonemID] IS NULL OR [DonemID]=0" , conn))
				da.Fill(eksikKayitlar);

			foreach(DataRow satir in eksikKayitlar.Rows)
			{
				if(satir["PersonelID"]==DBNull.Value||satir["OdemeTarihi"]==DBNull.Value)
					continue;

				int personelId = Convert.ToInt32(satir["PersonelID"]);
				DateTime odemeTarihi = Convert.ToDateTime(satir["OdemeTarihi"]);
				if(!PersonelMaasDoneminiGetirVeyaOlustur(conn , personelId , odemeTarihi , out int donemId , out decimal _ , out DateTime _ , out DateTime _))
					continue;

				using(OleDbCommand guncelleCmd = new OleDbCommand("UPDATE [PersonelOdemeleri] SET [DonemID]=? WHERE [OdemeID]=?" , conn))
				{
					guncelleCmd.Parameters.AddWithValue("?" , donemId);
					guncelleCmd.Parameters.AddWithValue("?" , Convert.ToInt32(satir["OdemeID"]));
					guncelleCmd.ExecuteNonQuery();
				}
			}
		}

		private bool PersonelMaasDonemTablosunuHazirla ()
		{
			if(_personelMaasDonemTablosuVar)
				return true;

			EnsurePersonelAltyapi();
			return _personelMaasDonemTablosuVar;
		}

		private bool PersonelOdemeTablosunuHazirla ()
		{
			if(_personelOdemeTablosuVar&&_personelMaasDonemTablosuVar)
				return true;

			EnsurePersonelAltyapi();
			if(_personelOdemeTablosuVar&&_personelMaasDonemTablosuVar)
				return true;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					if(!TabloVarMi(conn , "PersonelOdemeleri"))
					{
						using(OleDbCommand cmd = new OleDbCommand(
							"CREATE TABLE [PersonelOdemeleri] ([OdemeID] AUTOINCREMENT, [PersonelID] LONG, [DonemID] LONG, [OdemeTarihi] DATETIME, [OdenenTutar] CURRENCY, [Aciklama] LONGTEXT)" ,
							conn))
							cmd.ExecuteNonQuery();
					}

					if(!TabloVarMi(conn , "PersonelMaasDonemleri"))
					{
						using(OleDbCommand cmd = new OleDbCommand(
							"CREATE TABLE [PersonelMaasDonemleri] ([DonemID] AUTOINCREMENT, [PersonelID] LONG, [DonemBaslangic] DATETIME, [DonemBitis] DATETIME, [MaasTutari] CURRENCY)" ,
							conn))
							cmd.ExecuteNonQuery();
					}

					_personelOdemeTablosuVar=TabloVarMi(conn , "PersonelOdemeleri");
					_personelMaasDonemTablosuVar=TabloVarMi(conn , "PersonelMaasDonemleri");
				}
			}
			catch
			{
				_personelOdemeTablosuVar=false;
				_personelMaasDonemTablosuVar=false;
			}

			return _personelOdemeTablosuVar&&_personelMaasDonemTablosuVar;
		}

		private bool DepartmanAdiKaydiVarMi ( string departmanAdi , string haricId = null )
		{
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT [DepartmanID], [DepartmanAdi] FROM [Departmanlar]" , conn))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						string mevcutAd = rd["DepartmanAdi"]?.ToString()??string.Empty;
						if(!string.Equals(
							KarsilastirmaMetniHazirla(mevcutAd) ,
							KarsilastirmaMetniHazirla(departmanAdi) ,
							StringComparison.Ordinal))
							continue;

						if(!string.IsNullOrWhiteSpace(haricId)&&Convert.ToString(rd["DepartmanID"])==haricId)
							continue;

						return true;
					}
				}
			}

			return false;
		}

		private bool PersonelAdiKaydiVarMi ( string adSoyad , string haricId = null )
		{
			string hedefAd = AramaMetniniNormalizeEt(adSoyad);
			if(string.IsNullOrWhiteSpace(hedefAd))
				return false;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT [PersonelID], [AdSoyad] FROM [Personeller]" , conn))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						string mevcutAd = AramaMetniniNormalizeEt(rd["AdSoyad"]?.ToString());
						if(!string.Equals(mevcutAd , hedefAd , StringComparison.Ordinal))
							continue;

						if(!string.IsNullOrWhiteSpace(haricId)&&Convert.ToString(rd["PersonelID"])==haricId)
							continue;

						return true;
					}
				}
			}

			return false;
		}

		private void PersonelOdemeButonMetniniGuncelle ()
		{
			if(button75!=null)
				button75.Text="Kaydet";
			if(_personelOdemeEkleButonu!=null)
				_personelOdemeEkleButonu.Text="Temizle";
			if(_personelOdemeGuncelleButonu!=null)
				_personelOdemeGuncelleButonu.Text="Guncelle";
			if(_personelOdemeSilButonu!=null)
				_personelOdemeSilButonu.Text="Sil";
		}

		private void PersonelOdemeAksiyonDurumunuGuncelle ()
		{
			bool odemeTablosuHazir = _personelOdemeTablosuVar&&_personelMaasDonemTablosuVar;
			bool personelSecili = PersonelSeciliMi(out int _);
			bool odemeSecili = _seciliPersonelOdemeId.HasValue;
			bool tutarVar = PersonelDecimalParse(textBox100?.Text)>0m;

			if(_personelOdemeEkleButonu!=null)
				_personelOdemeEkleButonu.Enabled=personelSecili&&odemeTablosuHazir;
			if(button75!=null)
				button75.Enabled=personelSecili&&odemeTablosuHazir&&!odemeSecili&&tutarVar;
			if(_personelOdemeGuncelleButonu!=null)
				_personelOdemeGuncelleButonu.Enabled=personelSecili&&odemeTablosuHazir&&odemeSecili&&tutarVar;
			if(_personelOdemeSilButonu!=null)
				_personelOdemeSilButonu.Enabled=personelSecili&&odemeTablosuHazir&&odemeSecili;
		}

		private void PersonelOdemeSeciminiTemizle ( bool girisAlanlariniTemizle , bool tarihiBuguneGetir )
		{
			_seciliPersonelOdemeId=null;
			_seciliPersonelOdemeDonemId=null;
			_seciliPersonelOdemeTutari=0m;
			PersonelOdemeButonMetniniGuncelle();
			if(dataGridView25!=null)
				dataGridView25.ClearSelection();

			if(!girisAlanlariniTemizle)
			{
				PersonelOdemeAksiyonDurumunuGuncelle();
				return;
			}

			_personelBakiyeOdemeYukleniyor=true;
			try
			{
				if(textBox100!=null) textBox100.Clear();
				if(textBox102!=null) textBox102.Clear();
				if(tarihiBuguneGetir&&_personelBakiyeTarihPicker!=null) _personelBakiyeTarihPicker.Value=PersonelVarsayilanOdemeTarihiGetir();
			}
			finally
			{
				_personelBakiyeOdemeYukleniyor=false;
			}

			PersonelOdemeAksiyonDurumunuGuncelle();
		}

		private void PersonelOdemeSeciminiYukle ( DataGridViewRow satir )
		{
			if(satir==null)
				return;

			_seciliPersonelOdemeId=satir.Cells["OdemeID"].Value==null||satir.Cells["OdemeID"].Value==DBNull.Value
				? (int?)null
				: Convert.ToInt32(satir.Cells["OdemeID"].Value);
			_seciliPersonelOdemeDonemId=satir.Cells["DonemID"].Value==null||satir.Cells["DonemID"].Value==DBNull.Value
				? (int?)null
				: Convert.ToInt32(satir.Cells["DonemID"].Value);
			_seciliPersonelOdemeTutari=PersonelDecimalParse(Convert.ToString(satir.Cells["OdenenTutar"].Value));

			_personelBakiyeOdemeYukleniyor=true;
			try
			{
				if(satir.Cells["PersonelID"].Value!=null&&satir.Cells["PersonelID"].Value!=DBNull.Value)
					textBox54.Text=Convert.ToInt32(satir.Cells["PersonelID"].Value).ToString();
				if(textBox100!=null)
					textBox100.Text=_seciliPersonelOdemeTutari.ToString("N2" , _yazdirmaKulturu);
				if(textBox102!=null)
					textBox102.Text=Convert.ToString(satir.Cells["Aciklama"].Value)??string.Empty;
				if(_personelBakiyeTarihPicker!=null&&satir.Cells["OdemeTarihi"].Value!=null&&satir.Cells["OdemeTarihi"].Value!=DBNull.Value)
					_personelBakiyeTarihPicker.Value=Convert.ToDateTime(satir.Cells["OdemeTarihi"].Value);
			}
			finally
			{
				_personelBakiyeOdemeYukleniyor=false;
			}

			PersonelOdemeButonMetniniGuncelle();
			PersonelBakiyeAlaniniGuncelle();
		}

		private void PersonelTemizle ()
		{
			if(textBox54!=null) textBox54.Clear();
			if(textBox55!=null) textBox55.Clear();
			if(textBox56!=null) textBox56.Clear();
			if(textBox99!=null) textBox99.Text="0,00";
			if(textBox100!=null) textBox100.Clear();
			if(textBox101!=null) textBox101.Text="0,00";
			if(textBox102!=null) textBox102.Clear();
			if(textBox103!=null) textBox103.Text="0,00";
			if(_personelBakiyeTarihPicker!=null)
			{
				_personelBakiyeSecimYukleniyor=true;
				try
				{
					_personelBakiyeTarihPicker.Value=PersonelVarsayilanOdemeTarihiGetir();
				}
				finally
				{
					_personelBakiyeSecimYukleniyor=false;
				}
			}
			if(comboBox7!=null) comboBox7.SelectedIndex=-1;
			if(comboBox1!=null) comboBox1.SelectedIndex=0;
			if(_personelBakiyeDonemLabel!=null) _personelBakiyeDonemLabel.Text="DÖNEM : -";
			PersonelOdemeSeciminiTemizle(false , false);
			PersonelOdemeAksiyonDurumunuGuncelle();
			if(dataGridView13!=null) dataGridView13.ClearSelection();
			if(dataGridView25!=null) dataGridView25.ClearSelection();
		}

		private decimal PersonelDecimalParse ( string metin )
		{
			if(string.IsNullOrWhiteSpace(metin))
				return 0m;

			decimal sonuc;
			string temizMetin = metin.Trim();

			if(decimal.TryParse(temizMetin , NumberStyles.Currency , _yazdirmaKulturu , out sonuc))
				return sonuc;

			if(decimal.TryParse(temizMetin , NumberStyles.Currency , CultureInfo.InvariantCulture , out sonuc))
				return sonuc;

			return 0m;
		}

		private string PersonelDurumMetniGetir ()
		{
			return (comboBox1?.Text??string.Empty).Trim().ToUpper(new CultureInfo("tr-TR"));
		}

		private bool PersonelSeciliMi ( out int personelId )
		{
			personelId=0;
			return int.TryParse(textBox54?.Text , out personelId);
		}

		private decimal PersonelAylikMaasiGetirById ( int personelId )
		{
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				return PersonelAylikMaasiGetirById(conn , personelId);
			}
		}

		private DateTime PersonelVarsayilanOdemeTarihiGetir ()
		{
			return DateTime.Now;
		}

		private DateTime PersonelKaydedilecekOdemeTarihiGetir ()
		{
			DateTime seciliTarih = _personelBakiyeTarihPicker==null ? PersonelVarsayilanOdemeTarihiGetir() : _personelBakiyeTarihPicker.Value;
			if(_seciliPersonelOdemeId.HasValue)
				return seciliTarih;

			TimeSpan simdikiSaat = DateTime.Now.TimeOfDay;
			return seciliTarih.Date.Add(simdikiSaat);
		}

		private DateTime PersonelBakiyeReferansTarihiGetir ()
		{
			return _personelBakiyeTarihPicker==null ? PersonelVarsayilanOdemeTarihiGetir() : _personelBakiyeTarihPicker.Value;
		}

		private decimal PersonelToplamOdenenTutarGetirById ( int personelId )
		{
			if(!_personelOdemeTablosuVar)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT SUM([OdenenTutar]) FROM [PersonelOdemeleri] WHERE [PersonelID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , personelId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private void PersonelBakiyeAlaniniGuncelle ()
		{
			if(textBox101==null||textBox103==null)
				return;

			bool odemeTablosuHazir = PersonelOdemeTablosunuHazirla();
			bool personelSecili = PersonelSeciliMi(out int personelId);
			DateTime referansTarihi = PersonelBakiyeReferansTarihiGetir();
			decimal aylikMaas = 0m;
			decimal girilenTutar = personelSecili ? PersonelDecimalParse(textBox100?.Text) : 0m;
			decimal toplamOdenen = 0m;
			if(_personelBakiyeDonemLabel!=null)
				_personelBakiyeDonemLabel.Text="DÖNEM : -";

			if(personelSecili&&odemeTablosuHazir&&PersonelAktifMaasDoneminiGetirVeyaOlustur(personelId , referansTarihi , out int donemId , out decimal donemMaasi , out DateTime donemBaslangici , out DateTime donemBitisi))
			{
				aylikMaas=donemMaasi;
				toplamOdenen=PersonelToplamOdenenTutarGetirByDonemId(donemId);
				if(_seciliPersonelOdemeId.HasValue&&_seciliPersonelOdemeDonemId.HasValue&&_seciliPersonelOdemeDonemId.Value==donemId)
					toplamOdenen-=_seciliPersonelOdemeTutari;
				if(_personelBakiyeDonemLabel!=null)
					_personelBakiyeDonemLabel.Text=PersonelMaasDonemMetniGetir(donemBaslangici , donemBitisi);
			}

			textBox103.Text=aylikMaas.ToString("N2" , _yazdirmaKulturu);
			textBox101.Text=(aylikMaas-toplamOdenen-girilenTutar).ToString("N2" , _yazdirmaKulturu);
			PersonelOdemeButonMetniniGuncelle();

			PersonelOdemeAksiyonDurumunuGuncelle();
		}

		private void PersonelBakiyeDegisti_TextChanged ( object sender , EventArgs e )
		{
			if(_personelBakiyeOdemeYukleniyor)
				return;

			PersonelBakiyeAlaniniGuncelle();
		}
		private void PersonelBakiyeTarih_ValueChanged ( object sender , EventArgs e )
		{
			if(_personelBakiyeSecimYukleniyor||_personelBakiyeOdemeYukleniyor)
				return;

			PersonelBakiyeAlaniniGuncelle();
			PersonelBakiyeListele();
		}

		private DataTable PersonelOdemeGecmisiTablosuOlustur ()
		{
			DataTable dt = new DataTable();
			dt.Columns.Add("OdemeID" , typeof(int));
			dt.Columns.Add("PersonelID" , typeof(int));
			dt.Columns.Add("DonemID" , typeof(int));
			dt.Columns.Add("OdemeTarihi" , typeof(DateTime));
			dt.Columns.Add("OdenenTutar" , typeof(decimal));
			dt.Columns.Add("KalanBakiye" , typeof(decimal));
			dt.Columns.Add("Aciklama" , typeof(string));
			return dt;
		}

		private void PersonelBakiyeListele ()
		{
			if(dataGridView25==null)
				return;

			try
			{
				DataTable dt = PersonelOdemeGecmisiTablosuOlustur();
				DateTime referansTarihi = PersonelBakiyeReferansTarihiGetir();
				if(!PersonelOdemeTablosunuHazirla()||!PersonelSeciliMi(out int personelId))
				{
					dataGridView25.DataSource=dt;
					GridBasliklariniTurkceDuzenle(dataGridView25);
					return;
				}

				if(!PersonelAktifMaasDoneminiGetirVeyaOlustur(personelId , referansTarihi , out int donemId , out decimal aylikMaas , out DateTime donemBaslangici , out DateTime donemBitisi))
				{
					dataGridView25.DataSource=dt;
					GridBasliklariniTurkceDuzenle(dataGridView25);
					return;
				}

				if(_personelBakiyeDonemLabel!=null)
					_personelBakiyeDonemLabel.Text=PersonelMaasDonemMetniGetir(donemBaslangici , donemBitisi);

				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT [OdemeID],
										[PersonelID],
										[DonemID],
										[OdemeTarihi],
										IIF([OdenenTutar] IS NULL, 0, [OdenenTutar]) AS OdenenTutar,
										IIF([Aciklama] IS NULL, '', [Aciklama]) AS Aciklama
									FROM [PersonelOdemeleri]
									WHERE [PersonelID]=? AND [DonemID]=?
									ORDER BY [OdemeTarihi], [OdemeID]";

					using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
					{
						cmd.Parameters.AddWithValue("?" , personelId);
						cmd.Parameters.AddWithValue("?" , donemId);
						using(OleDbDataReader rd = cmd.ExecuteReader())
						{
							decimal toplamOdenen = 0m;
							while(rd!=null&&rd.Read())
							{
								decimal odenenTutar = rd["OdenenTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["OdenenTutar"]);
								toplamOdenen+=odenenTutar;
								dt.Rows.Add(
									rd["OdemeID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["OdemeID"]) ,
									rd["PersonelID"]==DBNull.Value ? personelId : Convert.ToInt32(rd["PersonelID"]) ,
									rd["DonemID"]==DBNull.Value ? donemId : Convert.ToInt32(rd["DonemID"]) ,
									rd["OdemeTarihi"]==DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["OdemeTarihi"]) ,
									odenenTutar ,
									aylikMaas-toplamOdenen ,
									Convert.ToString(rd["Aciklama"])??string.Empty);
							}
						}
					}
				}

				dataGridView25.DataSource=dt;
				if(dataGridView25.Columns.Contains("PersonelID"))
					dataGridView25.Columns["PersonelID"].Visible=false;
				if(dataGridView25.Columns.Contains("OdemeID"))
					dataGridView25.Columns["OdemeID"].Visible=false;
				if(dataGridView25.Columns.Contains("DonemID"))
					dataGridView25.Columns["DonemID"].Visible=false;
				if(dataGridView25.Columns.Contains("OdemeTarihi"))
					dataGridView25.Columns["OdemeTarihi"].DefaultCellStyle.Format="g";
				if(dataGridView25.Columns.Contains("OdenenTutar"))
					dataGridView25.Columns["OdenenTutar"].DefaultCellStyle.Format="N2";
				if(dataGridView25.Columns.Contains("KalanBakiye"))
					dataGridView25.Columns["KalanBakiye"].DefaultCellStyle.Format="N2";

				if(_seciliPersonelOdemeId.HasValue)
				{
					foreach(DataGridViewRow satir in dataGridView25.Rows)
					{
						if(satir.Cells["OdemeID"].Value!=null&&satir.Cells["OdemeID"].Value!=DBNull.Value&&Convert.ToInt32(satir.Cells["OdemeID"].Value)==_seciliPersonelOdemeId.Value)
						{
							satir.Selected=true;
							dataGridView25.CurrentCell=satir.Cells["OdemeTarihi"];
							break;
						}
					}
				}

				GridBasliklariniTurkceDuzenle(dataGridView25);
				AnaSayfaGridleriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Personel bakiye listesi yüklenemedi: "+ex.Message);
			}
		}

		private void PersonelKaydet ()
		{
			string adSoyad = textBox55.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(adSoyad))
			{
				MessageBox.Show("Personel ad soyad girin!");
				return;
			}
			if(PersonelAdiKaydiVarMi(adSoyad))
			{
				MessageBox.Show("Bu isimde bir personel zaten kayıtlı!");
				return;
			}
			if(comboBox7.SelectedValue==null||comboBox7.SelectedValue==DBNull.Value)
			{
				MessageBox.Show("Lütfen departman seçin!");
				return;
			}

			string durum = PersonelDurumMetniGetir();
			if(string.IsNullOrWhiteSpace(durum))
			{
				MessageBox.Show("Lütfen personel durumu seçin!");
				return;
			}

			decimal maas = PersonelDecimalParse(textBox99.Text);
			if(maas<0)
			{
				MessageBox.Show("Maaş eksi olamaz!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				string sorgu = _personelDurumKolonuVar
					? "INSERT INTO [Personeller] ([AdSoyad], [Telefon], [İseGirisTarihi], [AylikMaas], [DepartmanID], [AktifMi], [PersonelDurumu]) VALUES (?, ?, ?, ?, ?, ?, ?)"
					: "INSERT INTO [Personeller] ([AdSoyad], [Telefon], [İseGirisTarihi], [AylikMaas], [DepartmanID], [AktifMi]) VALUES (?, ?, ?, ?, ?, ?)";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , adSoyad);
					cmd.Parameters.AddWithValue("?" , textBox56.Text.Trim());
					cmd.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Today;
					cmd.Parameters.Add("?" , OleDbType.Currency).Value=maas;
					cmd.Parameters.AddWithValue("?" , Convert.ToInt32(comboBox7.SelectedValue));
					cmd.Parameters.Add("?" , OleDbType.Boolean).Value=durum=="AKTİF";
					if(_personelDurumKolonuVar)
						cmd.Parameters.AddWithValue("?" , durum);
					cmd.ExecuteNonQuery();
				}
			}

			PersonelTemizle();
			PersonelListele();
			PersonelIstatistikleriniYenile();
		}

		private void PersonelGuncelle ()
		{
			int personelId;
			if(!int.TryParse(textBox54.Text , out personelId))
			{
				MessageBox.Show("Güncellenecek personeli seçin!");
				return;
			}
			string adSoyad = textBox55.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(adSoyad))
			{
				MessageBox.Show("Personel ad soyad girin!");
				return;
			}
			if(PersonelAdiKaydiVarMi(adSoyad , personelId.ToString()))
			{
				MessageBox.Show("Bu isimde başka bir personel zaten kayıtlı!");
				return;
			}
			if(comboBox7.SelectedValue==null||comboBox7.SelectedValue==DBNull.Value)
			{
				MessageBox.Show("Lütfen departman seçin!");
				return;
			}

			string durum = PersonelDurumMetniGetir();
			decimal maas = PersonelDecimalParse(textBox99.Text);

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				string sorgu = _personelDurumKolonuVar
					? "UPDATE [Personeller] SET [AdSoyad]=?, [Telefon]=?, [AylikMaas]=?, [DepartmanID]=?, [AktifMi]=?, [PersonelDurumu]=? WHERE [PersonelID]=?"
					: "UPDATE [Personeller] SET [AdSoyad]=?, [Telefon]=?, [AylikMaas]=?, [DepartmanID]=?, [AktifMi]=? WHERE [PersonelID]=?";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , adSoyad);
					cmd.Parameters.AddWithValue("?" , textBox56.Text.Trim());
					cmd.Parameters.Add("?" , OleDbType.Currency).Value=maas;
					cmd.Parameters.AddWithValue("?" , Convert.ToInt32(comboBox7.SelectedValue));
					cmd.Parameters.Add("?" , OleDbType.Boolean).Value=durum=="AKTİF";
					if(_personelDurumKolonuVar)
						cmd.Parameters.AddWithValue("?" , durum);
					cmd.Parameters.AddWithValue("?" , personelId);
					cmd.ExecuteNonQuery();
				}
			}

			PersonelTemizle();
			PersonelListele();
			PersonelIstatistikleriniYenile();
		}

		private void PersonelSil ()
		{
			int personelId;
			if(!int.TryParse(textBox54.Text , out personelId))
			{
				MessageBox.Show("Silinecek personeli seçin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				if(_personelOdemeTablosuVar)
				{
					using(OleDbCommand odemeCmd = new OleDbCommand("DELETE FROM [PersonelOdemeleri] WHERE [PersonelID]=?" , conn))
					{
						odemeCmd.Parameters.AddWithValue("?" , personelId);
						odemeCmd.ExecuteNonQuery();
					}
				}
				if(_personelMaasDonemTablosuVar)
				{
					using(OleDbCommand donemCmd = new OleDbCommand("DELETE FROM [PersonelMaasDonemleri] WHERE [PersonelID]=?" , conn))
					{
						donemCmd.Parameters.AddWithValue("?" , personelId);
						donemCmd.ExecuteNonQuery();
					}
				}
				using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [Personeller] WHERE [PersonelID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , personelId);
					cmd.ExecuteNonQuery();
				}
			}

			PersonelTemizle();
			PersonelListele();
			PersonelIstatistikleriniYenile();
		}

		private void PersonelDepartman_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_personelFormYukleniyor)
				return;

			if(comboBox7.SelectedValue==null||comboBox7.SelectedValue==DBNull.Value)
				return;

			if(!int.TryParse(comboBox7.SelectedValue.ToString() , out int departmanId))
				return;

			textBox99.Text=DepartmanVarsayilanMaasiGetirById(departmanId).ToString("N2" , _yazdirmaKulturu);
		}

		private void PersonelGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			DataGridView grid = sender as DataGridView;
			if(grid==null||e.RowIndex<0||e.RowIndex>=grid.Rows.Count)
				return;

			DataGridViewRow satir = grid.Rows[e.RowIndex];
			int personelId = satir.Cells["PersonelID"].Value==null||satir.Cells["PersonelID"].Value==DBNull.Value
				? 0
				: Convert.ToInt32(satir.Cells["PersonelID"].Value);
			textBox54.Text=personelId>0 ? personelId.ToString() : string.Empty;
			textBox55.Text=Convert.ToString(satir.Cells["AdSoyad"].Value)??string.Empty;
			textBox56.Text=Convert.ToString(satir.Cells["Telefon"].Value)??string.Empty;

			_personelFormYukleniyor=true;
			try
			{
				if(grid.Columns.Contains("DepartmanID")&&satir.Cells["DepartmanID"].Value!=null&&satir.Cells["DepartmanID"].Value!=DBNull.Value)
					comboBox7.SelectedValue=satir.Cells["DepartmanID"].Value;
				else if(grid.Columns.Contains("DepartmanAdi"))
					ComboBoxMetniniSec(comboBox7 , Convert.ToString(satir.Cells["DepartmanAdi"].Value));
			}
			finally
			{
				_personelFormYukleniyor=false;
			}

			textBox99.Text=PersonelDecimalParse(Convert.ToString(satir.Cells["AylikMaas"].Value)).ToString("N2" , _yazdirmaKulturu);

			if(grid.Columns.Contains("PersonelDurumu"))
				ComboBoxMetniniSec(comboBox1 , Convert.ToString(satir.Cells["PersonelDurumu"].Value));

			if(personelId>0)
				PersonelBakiyePersonelSec(personelId , true);
			else
				PersonelBakiyeSeciminiUygula(true);
		}

		private void PersonelBakiyeGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			DataGridView grid = sender as DataGridView;
			if(grid==null||e.RowIndex<0||e.RowIndex>=grid.Rows.Count)
				return;

			DataGridViewRow satir = grid.Rows[e.RowIndex];
			if(grid.Columns.Contains("PersonelID")&&satir.Cells["PersonelID"].Value!=null&&satir.Cells["PersonelID"].Value!=DBNull.Value)
			{
				int personelId = Convert.ToInt32(satir.Cells["PersonelID"].Value);
				if(SeciliPersonelBakiyeIdGetir()!=personelId)
					PersonelBakiyePersonelSec(personelId , false);
			}

			if(grid.Columns.Contains("OdemeID"))
				PersonelOdemeSeciminiYukle(satir);
		}

		private void PersonelKaydet_Click ( object sender , EventArgs e ) => PersonelKaydet();
		private void PersonelGuncelle_Click ( object sender , EventArgs e ) => PersonelGuncelle();
		private void PersonelSil_Click ( object sender , EventArgs e ) => PersonelSil();
		private void PersonelTemizle_Click ( object sender , EventArgs e ) => PersonelTemizle();
		private void PersonelOdemeYeniKayit_Click ( object sender , EventArgs e ) => PersonelOdemeYeniKayitHazirla();
		private void PersonelBakiyeOdemeGuncelle_Click ( object sender , EventArgs e ) => PersonelOdemeKaydetVeyaGuncelle(true);
		private void PersonelBakiyeOdemeSil_Click ( object sender , EventArgs e ) => PersonelOdemeSil();

		private void PersonelOdemeYeniKayitHazirla ()
		{
			if(!PersonelSeciliMi(out int personelId))
			{
				MessageBox.Show("Yeni odeme icin once personel secin!");
				return;
			}

			PersonelOdemeSeciminiTemizle(true , true);
			PersonelBakiyeAlaniniGuncelle();
		}

		private void PersonelOdemeKaydetVeyaGuncelle ( bool guncellemeModu )
		{
			if(!PersonelSeciliMi(out int personelId))
			{
				MessageBox.Show(guncellemeModu
					? "Odeme guncellemek icin once personel secin!"
					: "Odeme kaydetmek icin once personel secin!");
				return;
			}

			if(guncellemeModu&&!_seciliPersonelOdemeId.HasValue)
			{
				MessageBox.Show("Guncellemek icin once bir odeme secin!");
				return;
			}

			if(!guncellemeModu&&_seciliPersonelOdemeId.HasValue)
			{
				MessageBox.Show("Yeni odeme kaydi icin once Temizle butonuna tiklayin!");
				return;
			}

			decimal tutar = PersonelDecimalParse(textBox100?.Text);
			if(tutar<=0)
			{
				MessageBox.Show("Lutfen gecerli bir tutar girin!");
				return;
			}

			if(!PersonelOdemeTablosunuHazirla())
			{
				MessageBox.Show("Personel odeme tablosu hazir degil!");
				return;
			}

			DateTime odemeTarihi = PersonelKaydedilecekOdemeTarihiGetir();

			if(!PersonelAktifMaasDoneminiGetirVeyaOlustur(personelId , odemeTarihi , out int donemId , out decimal _ , out DateTime _ , out DateTime _))
			{
				MessageBox.Show("Aktif maas donemi olusturulamadi!");
				return;
			}

			string aciklama = textBox102?.Text?.Trim()??string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					if(guncellemeModu)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [PersonelOdemeleri] SET [PersonelID]=?, [DonemID]=?, [OdemeTarihi]=?, [OdenenTutar]=?, [Aciklama]=? WHERE [OdemeID]=?" , conn))
						{
							cmd.Parameters.AddWithValue("?" , personelId);
							cmd.Parameters.AddWithValue("?" , donemId);
							cmd.Parameters.Add("?" , OleDbType.Date).Value=odemeTarihi;
							cmd.Parameters.Add("?" , OleDbType.Currency).Value=tutar;
							cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
							cmd.Parameters.AddWithValue("?" , _seciliPersonelOdemeId.Value);
							cmd.ExecuteNonQuery();
						}
					}
					else
					{
						using(OleDbCommand cmd = new OleDbCommand("INSERT INTO [PersonelOdemeleri] ([PersonelID], [DonemID], [OdemeTarihi], [OdenenTutar], [Aciklama]) VALUES (?, ?, ?, ?, ?)" , conn))
						{
							cmd.Parameters.AddWithValue("?" , personelId);
							cmd.Parameters.AddWithValue("?" , donemId);
							cmd.Parameters.Add("?" , OleDbType.Date).Value=odemeTarihi;
							cmd.Parameters.Add("?" , OleDbType.Currency).Value=tutar;
							cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
							cmd.ExecuteNonQuery();
						}
					}
				}

				PersonelOdemeSeciminiTemizle(true , false);
				PersonelBakiyeAlaniniGuncelle();
				PersonelBakiyeListele();
			}
			catch(Exception ex)
			{
				MessageBox.Show((guncellemeModu ? "Odeme guncellenemedi: " : "Odeme kaydedilemedi: ")+ex.Message);
			}
		}

		private void PersonelOdemeSil ()
		{
			if(!_seciliPersonelOdemeId.HasValue)
			{
				MessageBox.Show("Silmek icin once bir odeme secin!");
				return;
			}

			if(MessageBox.Show("Secili odeme silinsin mi?" , "Personel Odeme" , MessageBoxButtons.YesNo , MessageBoxIcon.Question)!=DialogResult.Yes)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [PersonelOdemeleri] WHERE [OdemeID]=?" , conn))
					{
						cmd.Parameters.AddWithValue("?" , _seciliPersonelOdemeId.Value);
						cmd.ExecuteNonQuery();
					}
				}

				PersonelOdemeSeciminiTemizle(true , false);
				PersonelBakiyeAlaniniGuncelle();
				PersonelBakiyeListele();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Odeme silinemedi: "+ex.Message);
			}
		}

		private void PersonelOdemeEkle_Click ( object sender , EventArgs e )
		{
			PersonelOdemeKaydetVeyaGuncelle(false);
		}

		private void DepartmanYonetimListele ()
		{
			if(_departmanYonetimGrid==null)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					string sorgu = _departmanMaasKolonuVar
						? "SELECT [DepartmanID], [DepartmanAdi], IIF([VarsayilanMaas] IS NULL, 0, [VarsayilanMaas]) AS VarsayilanMaas FROM [Departmanlar] ORDER BY [DepartmanAdi]"
						: "SELECT [DepartmanID], [DepartmanAdi] FROM [Departmanlar] ORDER BY [DepartmanAdi]";
					DataTable dt = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);

					_departmanYonetimGrid.DataSource=dt;
					if(_departmanYonetimGrid.Columns.Contains("VarsayilanMaas"))
						_departmanYonetimGrid.Columns["VarsayilanMaas"].DefaultCellStyle.Format="N2";
					GridBasliklariniTurkceDuzenle(_departmanYonetimGrid);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Departman listesi yüklenemedi: "+ex.Message);
			}
		}

		private void DepartmanYonetimTemizle ()
		{
			if(_departmanYonetimIdTextBox!=null) _departmanYonetimIdTextBox.Clear();
			if(_departmanYonetimAdiTextBox!=null) _departmanYonetimAdiTextBox.Clear();
			if(_departmanYonetimMaasTextBox!=null) _departmanYonetimMaasTextBox.Text="0,00";
			if(_departmanYonetimGrid!=null) _departmanYonetimGrid.ClearSelection();
		}

		private void DepartmanYonetimKaydet ()
		{
			string departmanAdi = _departmanYonetimAdiTextBox?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(departmanAdi))
			{
				MessageBox.Show("Departman adı girin!");
				return;
			}
			if(DepartmanAdiKaydiVarMi(departmanAdi))
			{
				MessageBox.Show("Bu departman zaten kayıtlı!");
				return;
			}

			decimal maas = PersonelDecimalParse(_departmanYonetimMaasTextBox?.Text);
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = _departmanMaasKolonuVar
					? "INSERT INTO [Departmanlar] ([DepartmanAdi], [VarsayilanMaas]) VALUES (?, ?)"
					: "INSERT INTO [Departmanlar] ([DepartmanAdi]) VALUES (?)";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , departmanAdi);
					if(_departmanMaasKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=maas;
					cmd.ExecuteNonQuery();
				}
			}

			DepartmanYonetimTemizle();
			DepartmanYonetimListele();
			DepartmanComboYenile();
			PersonelIstatistikleriniYenile();
		}

		private void DepartmanYonetimGuncelle ()
		{
			if(string.IsNullOrWhiteSpace(_departmanYonetimIdTextBox?.Text))
			{
				MessageBox.Show("Güncellenecek departmanı seçin!");
				return;
			}

			string departmanAdi = _departmanYonetimAdiTextBox?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(departmanAdi))
			{
				MessageBox.Show("Departman adı girin!");
				return;
			}
			if(DepartmanAdiKaydiVarMi(departmanAdi , _departmanYonetimIdTextBox.Text))
			{
				MessageBox.Show("Bu departman zaten kayıtlı!");
				return;
			}

			decimal maas = PersonelDecimalParse(_departmanYonetimMaasTextBox?.Text);
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = _departmanMaasKolonuVar
					? "UPDATE [Departmanlar] SET [DepartmanAdi]=?, [VarsayilanMaas]=? WHERE [DepartmanID]=?"
					: "UPDATE [Departmanlar] SET [DepartmanAdi]=? WHERE [DepartmanID]=?";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , departmanAdi);
					if(_departmanMaasKolonuVar)
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=maas;
					cmd.Parameters.AddWithValue("?" , Convert.ToInt32(_departmanYonetimIdTextBox.Text));
					cmd.ExecuteNonQuery();
				}
			}

			DepartmanYonetimListele();
			DepartmanComboYenile();
			PersonelListele();
			PersonelIstatistikleriniYenile();
		}

		private void DepartmanYonetimSil ()
		{
			if(string.IsNullOrWhiteSpace(_departmanYonetimIdTextBox?.Text))
			{
				MessageBox.Show("Silinecek departmanı seçin!");
				return;
			}

			int departmanId = Convert.ToInt32(_departmanYonetimIdTextBox.Text);
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM [Personeller] WHERE [DepartmanID]=?" , conn))
				{
					kontrol.Parameters.AddWithValue("?" , departmanId);
					if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
					{
						MessageBox.Show("Bu departmanda personel olduğu için silemezsiniz!");
						return;
					}
				}

				using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [Departmanlar] WHERE [DepartmanID]=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , departmanId);
					cmd.ExecuteNonQuery();
				}
			}

			DepartmanYonetimTemizle();
			DepartmanYonetimListele();
			DepartmanComboYenile();
			PersonelIstatistikleriniYenile();
		}

		private void DepartmanYonetimGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||_departmanYonetimGrid==null||e.RowIndex>=_departmanYonetimGrid.Rows.Count)
				return;

			DataGridViewRow satir = _departmanYonetimGrid.Rows[e.RowIndex];
			_departmanYonetimIdTextBox.Text=Convert.ToString(satir.Cells["DepartmanID"].Value)??string.Empty;
			_departmanYonetimAdiTextBox.Text=Convert.ToString(satir.Cells["DepartmanAdi"].Value)??string.Empty;
			if(_departmanYonetimGrid.Columns.Contains("VarsayilanMaas"))
				_departmanYonetimMaasTextBox.Text=PersonelDecimalParse(Convert.ToString(satir.Cells["VarsayilanMaas"].Value)).ToString("N2" , _yazdirmaKulturu);
		}

		private void DepartmanYonetimKaydet_Click ( object sender , EventArgs e ) => DepartmanYonetimKaydet();
		private void DepartmanYonetimGuncelle_Click ( object sender , EventArgs e ) => DepartmanYonetimGuncelle();
		private void DepartmanYonetimSil_Click ( object sender , EventArgs e ) => DepartmanYonetimSil();
		private void DepartmanYonetimTemizle_Click ( object sender , EventArgs e ) => DepartmanYonetimTemizle();

		private void EnsureBelgeArizaAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					BelgeArizaKolonlariniEkle(conn , "Teklifler");
					BelgeArizaKolonlariniEkle(conn , "Faturalar");
				}

				_arizaKaynaklariArastirildi=false;
				_arizaKaynaklari.Clear();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Belge arıza altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void BelgeArizaKolonlariniEkle ( OleDbConnection conn , string tablo )
		{
			if(conn==null||string.IsNullOrWhiteSpace(tablo))
				return;

			for(int i = 1 ; i<=4 ; i++)
			{
				string kolonAdi = "Ariza"+i.ToString(_yazdirmaKulturu);
				if(KolonVarMi(conn , tablo , kolonAdi))
					continue;

				using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE ["+tablo+"] ADD COLUMN ["+kolonAdi+"] LONGTEXT" , conn))
					cmd.ExecuteNonQuery();
			}
		}

		private void EnsureCariTipVeDurumVerileri ()
		{
			CultureInfo tr = new CultureInfo("tr-TR");
			string[] tipler = { "MÜŞTERİ" , "SUCU" , "FABRİKA" };
			string[] durumlar = { "AKTİF" , "PASİF" , "BORÇLU" };

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					foreach(string tip in tipler)
					{
						string tipUpper = tip.ToUpper(tr);
						int? mevcutId = null;
						string mevcutAd = null;

						using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 CariTipID, TipAdi FROM CariTipi WHERE UCASE(TipAdi)=?" , conn))
						{
							cmd.Parameters.AddWithValue("?" , tipUpper);
							using(OleDbDataReader rd = cmd.ExecuteReader())
							{
								if(rd!=null&&rd.Read())
								{
									mevcutId=Convert.ToInt32(rd["CariTipID"]);
									mevcutAd=rd["TipAdi"]?.ToString();
								}
							}
						}

						if(mevcutId.HasValue)
						{
							if(!string.Equals(mevcutAd , tipUpper , StringComparison.Ordinal))
							{
								using(OleDbCommand guncelle = new OleDbCommand("UPDATE CariTipi SET TipAdi=? WHERE CariTipID=?" , conn))
								{
									guncelle.Parameters.AddWithValue("?" , tipUpper);
									guncelle.Parameters.AddWithValue("?" , mevcutId.Value);
									guncelle.ExecuteNonQuery();
								}
							}
						}
						else
						{
							using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariTipi (TipAdi) VALUES (?)" , conn))
							{
								ekle.Parameters.AddWithValue("?" , tipUpper);
								ekle.ExecuteNonQuery();
							}
						}
					}

					if(_cariDurumCariTipKolonuVar)
					{
						List<int> tipIds = new List<int>();
						using(OleDbCommand tipCmd = new OleDbCommand("SELECT CariTipID FROM CariTipi" , conn))
						using(OleDbDataReader tipRd = tipCmd.ExecuteReader())
						{
							while(tipRd!=null&&tipRd.Read())
								tipIds.Add(Convert.ToInt32(tipRd["CariTipID"]));
						}

						foreach(int tipId in tipIds)
						{
							foreach(string durum in durumlar)
							{
								string durumUpper = durum.ToUpper(tr);
								using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariTipID=?" , conn))
								{
									kontrol.Parameters.AddWithValue("?" , durumUpper);
									kontrol.Parameters.AddWithValue("?" , tipId);
									if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
										continue;
								}

								using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariDurumlari (DurumAdi, CariTipID) VALUES (?, ?)" , conn))
								{
									ekle.Parameters.AddWithValue("?" , durumUpper);
									ekle.Parameters.AddWithValue("?" , tipId);
									ekle.ExecuteNonQuery();
								}
							}
						}
					}
					else
					{
						foreach(string durum in durumlar)
						{
							string durumUpper = durum.ToUpper(tr);
							int? mevcutId = null;
							string mevcutAd = null;

							using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 CariDurumID, DurumAdi FROM CariDurumlari WHERE UCASE(DurumAdi)=?" , conn))
							{
								cmd.Parameters.AddWithValue("?" , durumUpper);
								using(OleDbDataReader rd = cmd.ExecuteReader())
								{
									if(rd!=null&&rd.Read())
									{
										mevcutId=Convert.ToInt32(rd["CariDurumID"]);
										mevcutAd=rd["DurumAdi"]?.ToString();
									}
								}
							}

							if(mevcutId.HasValue)
							{
								if(!string.Equals(mevcutAd , durumUpper , StringComparison.Ordinal))
								{
									using(OleDbCommand guncelle = new OleDbCommand("UPDATE CariDurumlari SET DurumAdi=? WHERE CariDurumID=?" , conn))
									{
										guncelle.Parameters.AddWithValue("?" , durumUpper);
										guncelle.Parameters.AddWithValue("?" , mevcutId.Value);
										guncelle.ExecuteNonQuery();
									}
								}
							}
							else
							{
								using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariDurumlari (DurumAdi) VALUES (?)" , conn))
								{
									ekle.Parameters.AddWithValue("?" , durumUpper);
									ekle.ExecuteNonQuery();
								}
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari tip/durum başlangıç verileri oluşturulamadı: "+ex.Message);
			}
		}

		private void CariTipComboYenile ()
		{
			string sorgu = "SELECT CariTipID, TipAdi FROM CariTipi ORDER BY TipAdi";
			DoldurComboBox(cmbCariTip , sorgu , "TipAdi" , "CariTipID");
			DoldurComboBox(comboBox12 , sorgu , "TipAdi" , "CariTipID");
			DoldurComboBox(comboBox9 , sorgu , "TipAdi" , "CariTipID");
			DoldurComboBox(comboCariTip , sorgu , "TipAdi" , "CariTipID");

			if(comboCariTip.Items.Count>0)
			{
				if(!ComboBoxMetniniSec(comboCariTip , "Müşteri"))
					comboCariTip.SelectedIndex=0;
			}
		}

		private void EnsureFaturaCariTipAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					_faturaCariTipKolonuVar=KolonVarMi(conn , "Faturalar" , "CariTipID");
					if(!_faturaCariTipKolonuVar)
					{
						try
						{
							using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE Faturalar ADD COLUMN CariTipID INTEGER" , conn))
								cmd.ExecuteNonQuery();
							_faturaCariTipKolonuVar=true;
						}
						catch
						{
							_faturaCariTipKolonuVar=false;
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Fatura cari tipi altyapısı kontrol hatası: "+ex.Message);
			}
		}

		private void CariDurumComboYenile ()
		{
			string sorgu = "SELECT CariDurumID, DurumAdi FROM CariDurumlari ORDER BY DurumAdi";
			DoldurComboBox(comboBox13 , sorgu , "DurumAdi" , "CariDurumID");
		}

		private void CariDurumComboYenileByCariTip ()
		{
			if(cmbCariTip==null||cmbCariDurum==null) return;
			if(cmbCariTip.SelectedValue==null||cmbCariTip.SelectedValue==DBNull.Value)
			{
				cmbCariDurum.DataSource=null;
				cmbCariDurum.Items.Clear();
				cmbCariDurum.SelectedIndex=-1;
				return;
			}

			if(!_cariDurumCariTipKolonuVar)
			{
				// Eski tablo yapısı varsa tüm durumları göster
				string sorgu = "SELECT CariDurumID, DurumAdi FROM CariDurumlari ORDER BY DurumAdi";
				DoldurComboBox(cmbCariDurum , sorgu , "DurumAdi" , "CariDurumID");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = "SELECT CariDurumID, DurumAdi FROM CariDurumlari WHERE CariTipID=? ORDER BY DurumAdi";
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
					{
						da.SelectCommand.Parameters.AddWithValue("?" , Convert.ToInt32(cmbCariTip.SelectedValue));
						DataTable dt = new DataTable();
						da.Fill(dt);

						cmbCariDurum.DataSource=null;
						cmbCariDurum.Items.Clear();
						cmbCariDurum.ValueMember="CariDurumID";
						cmbCariDurum.DisplayMember="DurumAdi";
						cmbCariDurum.DataSource=dt;
						cmbCariDurum.SelectedIndex=-1;

						if(dt.Rows.Count>0)
						{
							int aktifIndex = cmbCariDurum.FindStringExact("AKTİF");
							cmbCariDurum.SelectedIndex=aktifIndex>=0?aktifIndex:0;
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari durum combobox yüklenemedi: "+ex.Message);
			}
		}

		private void CmbCariTip_SelectedIndexChanged ( object sender , EventArgs e )
		{
			CariDurumComboYenileByCariTip();
		}
		

		public void DatagridviewSetting ( DataGridView datagridview )

		{
			// 1?? Genel arka plan rengini formun arka planıyla aynı yap
			datagridview.BackgroundColor=this.BackColor; // Formun arka planıyla eşleşir
			datagridview.DefaultCellStyle.BackColor=this.BackColor; // Tüm hücreler
			datagridview.AlternatingRowsDefaultCellStyle.BackColor=this.BackColor; // AlternatingRows

			// 2?? Satır ve sütun çizgilerini kaldır
			datagridview.CellBorderStyle=DataGridViewCellBorderStyle.None; // Hücre çizgilerini kaldırır
			datagridview.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None; // Sütun başlık çizgileri
			datagridview.RowHeadersVisible=false; // Satır başlığı çizgilerini kaldırır

			// 3?? Seçim rengini form arka planına uygun yap (isteğe bağlı)
			datagridview.DefaultCellStyle.SelectionBackColor=Color.LightGray; // Seçim rengi
			datagridview.DefaultCellStyle.SelectionForeColor=Color.Black; // Seçim yazı rengi

			// 4?? Genel stil ayarları
			datagridview.EnableHeadersVisualStyles=false; // Header stilleri Windows teması ile karışmasın
			datagridview.ColumnHeadersDefaultCellStyle.BackColor=this.BackColor;
			datagridview.ColumnHeadersDefaultCellStyle.ForeColor=Color.Black; // Başlık yazısı rengi
			datagridview.SelectionMode=DataGridViewSelectionMode.FullRowSelect; // Tüm satırı seç

			// 5?? Otomatik sütun boyutu
			datagridview.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
			// seçili satır ve başlık
			datagridview.DefaultCellStyle.SelectionBackColor=Color.FromArgb(0 , 192 , 192);
			datagridview.DefaultCellStyle.SelectionForeColor=Color.Black;

			datagridview.EnableHeadersVisualStyles=false;
			datagridview.EnableHeadersVisualStyles=false;
			datagridview.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(0 , 179 , 179);
			datagridview.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;
			datagridview.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI" , 10 , FontStyle.Bold);
			datagridview.ReadOnly=true;
			datagridview.EditMode=DataGridViewEditMode.EditProgrammatically;
			//datagridview.AutoGenerateColumns=true;
			datagridview.DataBindingComplete-=DataGridView_DataBindingComplete;
			datagridview.DataBindingComplete+=DataGridView_DataBindingComplete;
			GridBasliklariniTurkceDuzenle(datagridview);

			


		}
	
		private void DataGridView_DataBindingComplete ( object sender , DataGridViewBindingCompleteEventArgs e )
		{
			if(sender is DataGridView dgv)
			{
				GridBasliklariniTurkceDuzenle(dgv);
				GriddeArizaKolonlariniGizle(dgv);
				GridiIlkSatiraKaydir(dgv);
			}
		}

		private void GridiIlkSatiraKaydir ( DataGridView datagridview )
		{
			if(datagridview==null||datagridview.IsDisposed)
				return;

			void uygula ()
			{
				if(datagridview.IsDisposed||datagridview.Rows.Count==0)
					return;

				DataGridViewRow ilkGorunurSatir = datagridview.Rows
					.Cast<DataGridViewRow>()
					.FirstOrDefault(r => !r.IsNewRow&&r.Visible);

				if(ilkGorunurSatir==null)
					return;

				try
				{
					datagridview.FirstDisplayedScrollingRowIndex=ilkGorunurSatir.Index;
				}
				catch
				{
				}
			}

			try
			{
				datagridview.BeginInvoke(( MethodInvoker )uygula);
			}
			catch
			{
				uygula();
			}
		}

		private void GridBasliklariniTurkceDuzenle ( DataGridView datagridview )
		{
			CultureInfo tr = new CultureInfo("tr-TR");
			foreach(DataGridViewColumn kolon in datagridview.Columns)
			{
				string kaynakBaslik = !string.IsNullOrWhiteSpace(kolon.DataPropertyName)
					? kolon.DataPropertyName
					: (string.IsNullOrWhiteSpace(kolon.HeaderText) ? kolon.Name : kolon.HeaderText);

				kolon.HeaderText=TurkceBaslikGetir(kaynakBaslik).ToUpper(tr);
			}
		}

		private void GriddeArizaKolonlariniGizle ( DataGridView datagridview )
		{
			if(datagridview==null) return;

			foreach(DataGridViewColumn kolon in datagridview.Columns)
			{
				string[] adaylar =
				{
					kolon.Name,
					kolon.DataPropertyName,
					kolon.HeaderText
				};

				if(adaylar.Any(ArizaKolonuMu))
					kolon.Visible=false;
			}
		}

		private static bool ArizaKolonuMu ( string metin )
		{
			if(string.IsNullOrWhiteSpace(metin))
				return false;

			return metin.IndexOf("ariza" , StringComparison.OrdinalIgnoreCase)>=0||
				metin.IndexOf("arıza" , StringComparison.OrdinalIgnoreCase)>=0;
		}

		private string TurkceBaslikGetir ( string kaynakBaslik )
		{
			if(string.IsNullOrWhiteSpace(kaynakBaslik)) return kaynakBaslik;

			string ham = kaynakBaslik.Trim();
			string kucuk = ham.ToLower(new CultureInfo("tr-TR"));

			if(kucuk.EndsWith("id")) return "ID";

			switch(kucuk)
			{
				case "urunid": return "Ürün ID";
				case "urunadi": return "Ürün Adı";
				case "urunalisid": return "Ürün Alış ID";
				case "urunsatisfiyatid": return "Ürün Satış Fiyatı ID";
				case "kategoriid": return "Kategori ID";
				case "kategoriadi": return "Kategori Adı";
				case "markaid": return "Marka ID";
				case "markaadi": return "Marka Adı";
				case "birimid": return "Birim ID";
				case "birimadi": return "Birim Adı";
				case "stokmiktari": return "Stok Miktarı";
				case "aktifmi": return "Aktif mi?";
				case "cariid": return "Cari ID";
				case "caritipid":
				case "caritipiid": return "Cari Tipi ID";
				case "caridurumid": return "Cari Durum ID";
				case "tipadi": return "Cari Tipi";
				case "durumadi": return "Durum";
				case "adsoyad": return "Ad Soyad";
				case "telefon": return "Telefon";
				case "adres": return "Adres";
				case "tc": return "TC/VKN";
				case "toptanciid": return "Toptancı ID";
				case "personelid": return "Personel ID";
				case "departmanid": return "Departman ID";
				case "departmanadi": return "Departman";
				case "aylikmaas": return "Aylık Maaş";
				case "odenentutar": return "Ödenen Tutar";
				case "kaynak": return "Kaynak";
				case "kayitturu": return "Kayıt Türü";
				case "kayitno": return "Kayıt No";
				case "urunsayisi": return "Ürün Sayısı";
				case "toplamstok": return "Toplam Stok";
				case "odemetutar": return "Verilen Ödeme";
				case "toplamodenentutar": return "Toplam Ödenen Tutar";
				case "toplamalim": return "Toplam Alınan Ürün";
				case "toplamodeme": return "Toplam Ödeme";
				case "islemturu": return "İşlem Türü";
				case "borctutar": return "Alınan Ürün Tutarı";
				case "kalanbakiye": return "Kalan Bakiye";
				case "odemetarihi": return "Ödeme Tarihi";
				case "tahsilattutar":
				case "alinantutar": return "Alinan Tahsilat";
				case "toplamfatura": return "Toplam Fatura";
				case "toplamtahsilat": return "Toplam Tahsilat";
				case "kalantutar": return "Kalan Tutar";
				case "sonfaturatarihi": return "Son Fatura Tarihi";
				case "aciklama": return "Not";
				case "varsayilanmaas": return "Varsayılan Maaş";
				case "personeldurumu": return "Personel Durumu";
				case "isegiristarihi":
				case "ısegiristarihi":
				case "İsegiristarihi": return "İşe Giriş Tarihi";
				case "fiyat": return "Fiyat";
				case "satisfiyati": return "Satış Fiyatı";
				case "birimalisfiyati": return "Birim Alış Fiyatı";
				case "netalisfiyati": return "Net Alış Fiyatı";
				case "iskontoorani": return "İskonto Oranı";
				case "zamorani": return "Zam Oranı";
				case "tarih": return "Tarih";
				case "müşteri grubu": return "Müşteri Grubu";
			}

			string baslik = ham.Replace("_" , " ");
			baslik=Regex.Replace(baslik , @"(?<=[a-zçğıöşü])(?=[A-ZÇĞİÖŞÜ])" , " ");
			baslik=Regex.Replace(baslik , @"(?<=[A-Za-zÇĞİÖŞÜçğıöşü])(?=[0-9])" , " ");
			baslik=Regex.Replace(baslik , @"\s+" , " ").Trim();

			CultureInfo tr = new CultureInfo("tr-TR");
			string[] kelimeler = baslik.Split(' ');
			for(int i = 0 ; i<kelimeler.Length ; i++)
			{
				string kelime = kelimeler[i].ToLower(tr);
				switch(kelime)
				{
					case "urun": kelimeler[i]="Ürün"; break;
					case "satis": kelimeler[i]="Satış"; break;
					case "alis": kelimeler[i]="Alış"; break;
					case "adi": kelimeler[i]="Adı"; break;
					case "miktari": kelimeler[i]="Miktarı"; break;
					case "iskonto": kelimeler[i]="İskonto"; break;
					case "orani": kelimeler[i]="Oranı"; break;
					case "cari": kelimeler[i]="Cari"; break;
					case "tipi": kelimeler[i]="Tipi"; break;
					case "durum": kelimeler[i]="Durum"; break;
					case "marka": kelimeler[i]="Marka"; break;
					case "kategori": kelimeler[i]="Kategori"; break;
					case "birim": kelimeler[i]="Birim"; break;
					case "id": kelimeler[i]="ID"; break;
					default: kelimeler[i]=tr.TextInfo.ToTitleCase(kelime); break;
				}
			}

			return string.Join(" " , kelimeler);
		}

		private string KarsilastirmaMetniHazirla ( string metin )
		{
			if(string.IsNullOrWhiteSpace(metin))
				return string.Empty;

			return metin
				.Trim()
				.ToUpperInvariant()
				.Replace('Ç' , 'C')
				.Replace('Ğ' , 'G')
				.Replace('İ' , 'I')
				.Replace('Ö' , 'O')
				.Replace('Ş' , 'S')
				.Replace('Ü' , 'U');
		}

		private string ComboBoxIcinBirebirMetinGetir ( ComboBox comboBox , string hedefMetin )
		{
			if(comboBox==null||comboBox.Items.Count==0||string.IsNullOrWhiteSpace(hedefMetin))
				return hedefMetin??string.Empty;

			string hedef = KarsilastirmaMetniHazirla(hedefMetin);
			for(int i = 0 ; i<comboBox.Items.Count ; i++)
			{
				string mevcutMetin = comboBox.GetItemText(comboBox.Items[i]);
				if(string.Equals(KarsilastirmaMetniHazirla(mevcutMetin) , hedef , StringComparison.Ordinal))
					return mevcutMetin;
			}

			return hedefMetin??string.Empty;
		}

		private void ComboBoxMetinSeciminiGuvenliAyarla ( ComboBox comboBox )
		{
			if(comboBox==null||comboBox.IsDisposed||!comboBox.IsHandleCreated)
				return;

			try
			{
				int metinUzunlugu = comboBox.Text?.Length??0;
				comboBox.SelectionStart=metinUzunlugu;
				comboBox.SelectionLength=0;
			}
			catch(ArgumentOutOfRangeException)
			{
			}
			catch(ArgumentException)
			{
			}
			catch(InvalidOperationException)
			{
			}
		}

		private bool ComboBoxMetniniSec ( ComboBox comboBox , string hedefMetin )
		{
			if(comboBox==null||comboBox.Items.Count==0||string.IsNullOrWhiteSpace(hedefMetin))
				return false;

			string hedef = KarsilastirmaMetniHazirla(hedefMetin);
			for(int i = 0 ; i<comboBox.Items.Count ; i++)
			{
				if(string.Equals(KarsilastirmaMetniHazirla(comboBox.GetItemText(comboBox.Items[i])) , hedef , StringComparison.Ordinal))
				{
					comboBox.SelectedIndex=i;
					return true;
				}
			}

			return false;
		}

		private void TumDataGridBasliklariniUygula ()
		{
			GridBasliklariniTurkceDuzenle(dataGridView1);
			GridBasliklariniTurkceDuzenle(dataGridView2);
			GridBasliklariniTurkceDuzenle(dataGridView3);
			GridBasliklariniTurkceDuzenle(dataGridView4);
			GridBasliklariniTurkceDuzenle(dataGridView5);
			GridBasliklariniTurkceDuzenle(dataGridView6);
			GridBasliklariniTurkceDuzenle(dataGridView8);
			GridBasliklariniTurkceDuzenle(dataGridView10);
			GridBasliklariniTurkceDuzenle(dataGridView11);
			GridBasliklariniTurkceDuzenle(dataGridView12);
			GridBasliklariniTurkceDuzenle(dataGridView13);
			GridBasliklariniTurkceDuzenle(dataGridView25);
			GridBasliklariniTurkceDuzenle(dataGridView14);
			GridBasliklariniTurkceDuzenle(dataGridView15);
			GridBasliklariniTurkceDuzenle(dataGridView16);
			GridBasliklariniTurkceDuzenle(dataGridView18);
		}


		private IEnumerable<Control> TumKontrolleriGetir ( Control parent )
		{
			foreach(Control child in parent.Controls)
			{
				yield return child;
				foreach(Control nested in TumKontrolleriGetir(child))
					yield return nested;
			}
		}
		private void groupBox1_Enter ( object sender , EventArgs e )
		{

		}

		private void Form1_Shown ( object sender , EventArgs e )
		{
			this.WindowState=FormWindowState.Normal;
			this.StartPosition=FormStartPosition.Manual;
			this.Bounds=Screen.FromHandle(this.Handle).WorkingArea;
			UrunYonetimSekmeleriniKur();
			KullaniciOturumunuUygula();
		}



		private void pictureBox1_Paint_1 ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox1.BackColor); // önce eski resmi temizle
			Image img = pictureBox1.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox1.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox1.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}

			// Paint olayını bağlayalım
		}

		private void pictureBox5_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox5.BackColor); // önce eski resmi temizle
			Image img = pictureBox5.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox5.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox5.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}

			// Paint olayını bağlayalım
		}

		private void groupBox11_Enter ( object sender , EventArgs e )
		{

		}

		private void pictureBox6_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox6.BackColor); // önce eski resmi temizle
			Image img = pictureBox6.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox6.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox6.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}

		}

		private void pictureBox7_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox7.BackColor); // önce eski resmi temizle
			Image img = pictureBox7.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox7.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox7.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox8_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox8.BackColor); // önce eski resmi temizle
			Image img = pictureBox8.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox8.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox8.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox12_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox12.BackColor); // önce eski resmi temizle
			Image img = pictureBox12.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox12.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox12.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox11_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox11.BackColor); // önce eski resmi temizle
			Image img = pictureBox11.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox11.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox11.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox10_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox10.BackColor); // önce eski resmi temizle
			Image img = pictureBox10.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox10.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox10.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox9_Click ( object sender , EventArgs e )
		{

		}

		private void pictureBox9_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox9.BackColor); // önce eski resmi temizle
			Image img = pictureBox9.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox9.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox9.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox2_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox2.BackColor); // önce eski resmi temizle
			Image img = pictureBox2.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox2.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox2.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}
		}

		private void pictureBox3_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox3.BackColor); // önce eski resmi temizle
			Image img = pictureBox3.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox3.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox3.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}

		}

		private void pictureBox4_Paint ( object sender , PaintEventArgs e )
		{
			e.Graphics.Clear(pictureBox4.BackColor); // önce eski resmi temizle
			Image img = pictureBox4.Image;
			if(img!=null)
			{
				// X koordinatı: sağa yaslamak
				int x = pictureBox4.ClientSize.Width-img.Width-20;
				// Y koordinatı: dikey ortalamak
				int y = (pictureBox4.ClientSize.Height-img.Height)/2;

				e.Graphics.DrawImage(img , x , y , img.Width , img.Height);
			}

		}
		public void SatisUrunleriniAlisTablosundanGetir ()
		{
			try
			{
				UrunSecimComboBoxiniDoldur(comboBox8 , true);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Ürün listesi getirilirken hata: "+ex.Message);
			}
			finally
			{
				if(baglanti.State==ConnectionState.Open)
					baglanti.Close();
			}

			// İlk açılış ve liste yenilemelerinde net fiyatları 0,00 göster
			SatisNetFiyatlariniSifirla();
		}

		private void SatisNetFiyatlariniSifirla ()
		{
			textBox17.Text="0,00";
			textBox84.Text="0,00";
		}

		//hepsi
		private void DoldurComboBox ( ComboBox cmb , string sorgu , string display , string value )
		{
			try
			{
				if(cmb==null)
					return;

				if(baglanti==null)
				{
					MessageBox.Show("Bağlantı tanımlı değil");
					return;
				}

				if(baglanti.State==ConnectionState.Closed)
					baglanti.Open();

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);

				cmb.DataSource=null;
				cmb.Items.Clear();

				cmb.ValueMember=value;
				cmb.DisplayMember=display;
				cmb.DataSource=dt;

				cmb.SelectedIndex=-1;
			}
			catch(Exception ex)
			{
				MessageBox.Show("ComboBox hatası: "+ex.Message);
			}
			finally
			{
				if(baglanti!=null)
					baglanti.Close();
			}
		}

		private string UrunGosterimMetniGetir ( string urunAdi , string markaAdi )
		{
			string temizUrunAdi = ( urunAdi??string.Empty ).Trim();
			string temizMarkaAdi = ( markaAdi??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizMarkaAdi))
				return temizUrunAdi;
			if(string.IsNullOrWhiteSpace(temizUrunAdi))
				return temizMarkaAdi;
			return temizUrunAdi+" - "+temizMarkaAdi;
		}

		private string UrunAdiniNormallestir ( string urunAdi )
		{
			return string.Join(" " , ( urunAdi??string.Empty ).Split(new[] { ' ' } , StringSplitOptions.RemoveEmptyEntries)).Trim();
		}

		private int? UrunKayitKimlikDegeriGetir ( object deger )
		{
			if(deger==null||deger==DBNull.Value)
				return null;

			try
			{
				int sayi = Convert.ToInt32(deger);
				return sayi>0 ? sayi : (int?)null;
			}
			catch
			{
				string metin = Convert.ToString(deger)??string.Empty;
				if(string.IsNullOrWhiteSpace(metin))
					return null;

				if(int.TryParse(metin , NumberStyles.Integer , CultureInfo.CurrentCulture , out int sayi)||
				   int.TryParse(metin , NumberStyles.Integer , CultureInfo.InvariantCulture , out sayi))
					return sayi>0 ? sayi : (int?)null;

				decimal ondalikDeger;
				if(decimal.TryParse(metin , NumberStyles.Number , CultureInfo.CurrentCulture , out ondalikDeger)||
				   decimal.TryParse(metin , NumberStyles.Number , CultureInfo.InvariantCulture , out ondalikDeger))
				{
					sayi=decimal.ToInt32(decimal.Truncate(ondalikDeger));
					return sayi>0 ? sayi : (int?)null;
				}
			}

			return null;
		}

		private bool AyniUrunKaydiVarMi ( string urunAdi , object kategoriId , object markaId , int? haricUrunId = null )
		{
			string normalizeUrunAdi = UrunAdiniNormallestir(urunAdi);
			if(string.IsNullOrWhiteSpace(normalizeUrunAdi))
				return false;

			int? hedefKategoriId = UrunKayitKimlikDegeriGetir(kategoriId);
			int? hedefMarkaId = UrunKayitKimlikDegeriGetir(markaId);

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					string sorgu = "SELECT [UrunID], [UrunAdi], [KategoriID], [MarkaID] FROM [Urunler]";
					if(haricUrunId.HasValue)
						sorgu+=" WHERE [UrunID]<>?";

					using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
					{
						if(haricUrunId.HasValue)
							cmd.Parameters.AddWithValue("?" , haricUrunId.Value);

						using(OleDbDataReader rd = cmd.ExecuteReader())
						{
							while(rd!=null&&rd.Read())
							{
								string mevcutUrunAdi = UrunAdiniNormallestir(Convert.ToString(rd["UrunAdi"]));
								int? mevcutKategoriId = UrunKayitKimlikDegeriGetir(rd["KategoriID"]);
								int? mevcutMarkaId = UrunKayitKimlikDegeriGetir(rd["MarkaID"]);
								if(string.Equals(mevcutUrunAdi , normalizeUrunAdi , StringComparison.OrdinalIgnoreCase) &&
								   mevcutKategoriId==hedefKategoriId &&
								   mevcutMarkaId==hedefMarkaId)
									return true;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Urun tekrar kontrolu yapilamadi: "+ex.Message);
			}

			return false;
		}

		private void UrunSecimComboBoxiniDoldur ( ComboBox comboBox , bool sadeceAlisiOlanlar )
		{
			if(comboBox==null)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					string sorgu = @"SELECT
									U.[UrunID],
									IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) AS UrunAdi,
									IIF(M.[MarkaAdi] IS NULL, '', M.[MarkaAdi]) AS MarkaAdi,
									IIF(M.[MarkaAdi] IS NULL OR M.[MarkaAdi]='',
										IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]),
										IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) & ' - ' & M.[MarkaAdi]) AS UrunSecim
								FROM [Urunler] AS U
								LEFT JOIN [Markalar] AS M ON CLng(IIF(U.[MarkaID] IS NULL, 0, U.[MarkaID])) = M.[MarkaID]";

					if(sadeceAlisiOlanlar)
					{
						sorgu+=@"
								WHERE EXISTS (
									SELECT 1
									FROM [UrunAlis] AS A
									WHERE CLng(IIF(A.[UrunID] IS NULL, 0, A.[UrunID])) = U.[UrunID]
								)";
					}

					sorgu+=@"
								ORDER BY IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) ASC,
										 IIF(M.[MarkaAdi] IS NULL, '', M.[MarkaAdi]) ASC";

					OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn);
					DataTable dt = new DataTable();
					da.Fill(dt);

					comboBox.DataSource=null;
					comboBox.Items.Clear();
					comboBox.AutoCompleteMode=AutoCompleteMode.SuggestAppend;
					comboBox.AutoCompleteSource=AutoCompleteSource.ListItems;
					comboBox.DisplayMember="UrunSecim";
					comboBox.ValueMember="UrunID";
					comboBox.DataSource=dt;
					comboBox.SelectedIndex=-1;
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Ürün seçimi yüklenemedi: "+ex.Message);
			}
		}

		public void Listele7 () // Markaların listelendiği metodun adı
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// Access için özel parantezli JOIN sorgusu
				string sorgu = @"SELECT M.MarkaID, M.MarkaAdi, K.KategoriAdi 
                         FROM (Markalar AS M 
                         LEFT JOIN MarkaKategori AS MK ON M.MarkaID = MK.MarkaID) 
                         LEFT JOIN Kategoriler AS K ON MK.KategoriID = K.KategoriID 
                         ORDER BY M.MarkaID ASC";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);

				dgvMarkaYonetim.DataSource=dt;
						if(dgvMarkaYonetim.Columns.Contains("KategoriID"))
							dgvMarkaYonetim.Columns["KategoriID"].Visible=false;

				// Sütun başlıklarını düzenle (Sizin mevcut metodunuzu çağırır)
				GridBasliklariniTurkceDuzenle(dgvMarkaYonetim);

				baglanti.Close();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Listeleme hatası: "+ex.Message);
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}
		public void MarkaListesiniKategoriyeGoreDoldur ( ComboBox markaCmb , object kategoriId )
		{
			if(_markaFiltreleniyor)
				return;

			_markaFiltreleniyor=true;
			bool baglantiAcildi = false;

			try
			{
				if(kategoriId==null||kategoriId==DBNull.Value)
				{
					DoldurComboBox(markaCmb , "SELECT MarkaID, MarkaAdi FROM Markalar ORDER BY MarkaAdi" , "MarkaAdi" , "MarkaID");
				}
				else
				{
					if(baglanti.State==ConnectionState.Closed)
					{
						baglanti.Open();
						baglantiAcildi=true;
					}

					// Marka listeleme kısmındaki mevcut sorguyu bu JOIN'li yapı ile değiştirin
					string sorgu = @"SELECT DISTINCT M.MarkaID, M.MarkaAdi 
                 FROM (Markalar AS M 
                 INNER JOIN MarkaKategori AS MK ON M.MarkaID = MK.MarkaID) 
                 WHERE MK.KategoriID = ?
                 ORDER BY M.MarkaAdi ASC";

					OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
					da.SelectCommand.Parameters.AddWithValue("?" , kategoriId);

					DataTable dt = new DataTable();
					da.Fill(dt);

					markaCmb.DataSource=null;
					markaCmb.Items.Clear();

					markaCmb.ValueMember="MarkaID";
					markaCmb.DisplayMember="MarkaAdi";
					markaCmb.DataSource=dt;
					markaCmb.SelectedIndex=-1;
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Marka listeleme hatası: "+ex.Message);
			}
			finally
			{
				if(baglantiAcildi&&baglanti.State==ConnectionState.Open) baglanti.Close();
				_markaFiltreleniyor=false;
			}
		}

		private void MarkaKategoriBagla ( int markaId , int kategoriId )
		{
			if(markaId<=0||kategoriId<=0)
				return;

			bool baglantiAcildi = false;

			try
			{
				if(baglanti.State==ConnectionState.Closed)
				{
					baglanti.Open();
					baglantiAcildi=true;
				}

				string kontrol = "SELECT COUNT(*) FROM MarkaKategori WHERE MarkaID=? AND KategoriID=?";
				OleDbCommand kontrolCmd = new OleDbCommand(kontrol , baglanti);
				kontrolCmd.Parameters.AddWithValue("?" , markaId);
				kontrolCmd.Parameters.AddWithValue("?" , kategoriId);

				int sayi = Convert.ToInt32(kontrolCmd.ExecuteScalar());

				if(sayi==0)
				{
					string ekle = "INSERT INTO MarkaKategori (MarkaID, KategoriID) VALUES (?, ?)";
					OleDbCommand ekleCmd = new OleDbCommand(ekle , baglanti);
					ekleCmd.Parameters.AddWithValue("?" , markaId);
					ekleCmd.Parameters.AddWithValue("?" , kategoriId);
					ekleCmd.ExecuteNonQuery();
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Marka-Kategori bağlama hatası: "+ex.Message);
			}
			finally
			{
				if(baglantiAcildi&&baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		private void MarkaKategoriUrunlerdenSenkronla ()
		{
			bool baglantiAcildi = false;

			try
			{
				if(baglanti.State==ConnectionState.Closed)
				{
					baglanti.Open();
					baglantiAcildi=true;
				}

				string sorgu = @"INSERT INTO MarkaKategori (KategoriID, MarkaID)
								 SELECT DISTINCT U.KategoriID, U.MarkaID
								 FROM Urunler AS U
								 LEFT JOIN MarkaKategori AS MK
									 ON U.KategoriID = MK.KategoriID AND U.MarkaID = MK.MarkaID
								 WHERE U.KategoriID IS NOT NULL AND U.MarkaID IS NOT NULL AND MK.MKID IS NULL";

				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
				cmd.ExecuteNonQuery();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Marka-Kategori senkron hatası: "+ex.Message);
			}
			finally
			{
				if(baglantiAcildi&&baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		//ürünhesap
		private void HesaplaNetFiyat ()
		{
			double alisFiyati = 0, iskonto = 0;

			// Güvenli sayı dönüşümü
			double.TryParse(textBox9.Text , out alisFiyati);
			double.TryParse(textBox16.Text , out iskonto);

			// Hesaplama: Net = Alış - (Alış * İskonto / 100)
			double netFiyat = alisFiyati-(alisFiyati*iskonto/100);

			// Sonucu net fiyat kutusuna yaz (2 basamaklı formatta)
			textBox17.Text=netFiyat.ToString("N2");
		}
		public void UrunleriYenile ()
		{
			try
			{
				UrunSecimComboBoxiniDoldur(comboBox4 , false);
				textBox19.Clear();
				comboBox4.SelectedIndex=-1;
				comboBox4.Text="";
			}
			catch(Exception ex)
			{
				MessageBox.Show("ComboBox Yenileme Hatası: "+ex.Message);
			}
		}
		public void SatisUrunleriniYenile ()
		{
			try
			{
				UrunSecimComboBoxiniDoldur(comboBox8 , true);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Satış ürün yenileme hatası: "+ex.Message);
			}
			finally
			{
				if(baglanti!=null)
					baglanti.Close();
			}
		}

		private void FormuTamamenTemizle ()
		{// ... diğer temizleme kodların ...
			textBox84.Text="0,00"; // Kutuyu 0,00 yap

			// Grid'in seçimini burada da temizlemek garantiye alır
			if(dataGridView18.Rows.Count>0)
			{
				dataGridView18.ClearSelection();
			}
		}
		public void Listele6 ()
	
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				string sorgu = _cariDurumCariTipKolonuVar
					? @"SELECT CD.CariDurumID,
                               CD.DurumAdi,
                               CD.CariTipID,
                               CT.TipAdi
                          FROM CariDurumlari AS CD
                          LEFT JOIN CariTipi AS CT ON CD.CariTipID = CT.CariTipID
                          ORDER BY CD.CariDurumID ASC"
					: "SELECT CariDurumID, DurumAdi FROM CariDurumlari ORDER BY CariDurumID ASC";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);

				// Veriyi DataGridView'e aktar
				dataGridView6.DataSource=dt;

				if(dataGridView6.Columns.Contains("CariTipID"))
					dataGridView6.Columns["CariTipID"].Visible=false;

				GridBasliklariniTurkceDuzenle(dataGridView6);
				dataGridView6.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
				GridAramaFiltresiniUygula(textBox23 , dataGridView6);

				baglanti.Close();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari durum listesi yüklenemedi: "+ex.Message);
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}


		//public void Listele6 ()
		//{
		//	decimal bulunanFiyat = 0;
		//	try
		//	{
		//		if(baglanti.State==ConnectionState.Closed) baglanti.Open();

		//		// Access'teki CariFiyatlari tablosundan fiyatı sorguluyoruz
		//		string sorgu = "SELECT Fiyat FROM CariFiyat WHERE CariTipID = @tip AND UrunID = @urun";
		//		OleDbCommand komut = new OleDbCommand(sorgu , baglanti);

		//		// Access parametreleri sırayla okuduğu için ekleme sırası önemlidir
		//		komut.Parameters.AddWithValue("@tip" , CariTipID);
		//		komut.Parameters.AddWithValue("@urun" , UrunID);

		//		object sonuc = komut.ExecuteScalar();

		//		if(sonuc!=null&&sonuc!=DBNull.Value)
		//		{
		//			bulunanFiyat=Convert.ToDecimal(sonuc);
		//		}
		//		else
		//		{
		//			// Eğer özel fiyat tanımlanmamışsa, ürünün genel SatisFiyat'ını al
		//			bulunanFiyat=Convert.ToDecimal(dataGridView1.CurrentRow.Cells["Fiyat"].Value??0);
		//		}
		//	}
		//	catch(Exception ex)
		//	{
		//		MessageBox.Show("Fiyat çekme hatası: "+ex.Message);
		//	}
		//	finally
		//	{
		//		baglanti.Close();
		//	}
		//	//return bulunanFiyat;

		//}

		public void Listele5 ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					// .accdb için bu bağlantı açılırken Provider ACE.OLEDB.12.0 olmalı
					conn.Open();

					string query = "SELECT CariTipID, TipAdi FROM [CariTipi] ORDER BY TipAdi";
					using(OleDbDataAdapter da = new OleDbDataAdapter(query , conn))
					{
						DataTable dt = new DataTable();
						da.Fill(dt);
						dataGridView4.DataSource=dt;
						GridAramaFiltresiniUygula(textBox15 , dataGridView4);
					}
				}
			}
			catch(OleDbException ex)
			{
				// "Tanınmayan veritabanı biçimi" hatası buraya düşerse 1. adımdaki Provider'ı kontrol edin.
				MessageBox.Show("Veritabanı bağlantı hatası: "+ex.Message);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Beklenmedik bir hata: "+ex.Message);
			}
		}

		private void CariTipKaydet_Click ( object sender , EventArgs e ) => CariTipKaydet();
		private void CariTipSil_Click ( object sender , EventArgs e ) => CariTipSil();
		private void CariTipGuncelle_Click ( object sender , EventArgs e ) => CariTipGuncelle();
		private void CariTipTemizle_Click ( object sender , EventArgs e ) => CariTipTemizle();

		private void CariTipKaydet ()
		{
			CultureInfo tr = new CultureInfo("tr-TR");
			string tip = textBox6.Text?.Trim()??"";
			if(string.IsNullOrWhiteSpace(tip))
			{
				MessageBox.Show("Cari tipi girin!");
				return;
			}

			string tipUpper = tip.ToUpper(tr);
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM CariTipi WHERE UCASE(TipAdi)=?" , conn))
				{
					kontrol.Parameters.AddWithValue("?" , tipUpper);
					if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
					{
						MessageBox.Show("Bu cari tipi zaten kayıtlı!");
						return;
					}
				}

				using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariTipi (TipAdi) VALUES (?)" , conn))
				{
					ekle.Parameters.AddWithValue("?" , tipUpper);
					ekle.ExecuteNonQuery();
				}
			}

			Listele5();
			CariTipComboYenile();
			CariTipTemizle();
		}

		private void CariTipSil ()
		{
			if(string.IsNullOrWhiteSpace(textBox5.Text))
			{
				MessageBox.Show("Silmek için bir cari tipi seçin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand sil = new OleDbCommand("DELETE FROM CariTipi WHERE CariTipID=?" , conn))
				{
					sil.Parameters.AddWithValue("?" , Convert.ToInt32(textBox5.Text));
					sil.ExecuteNonQuery();
				}
			}

			Listele5();
			CariTipComboYenile();
			CariTipTemizle();
		}

		private void CariTipGuncelle ()
		{
			if(string.IsNullOrWhiteSpace(textBox5.Text))
			{
				MessageBox.Show("Güncellemek için bir cari tipi seçin!");
				return;
			}

			CultureInfo tr = new CultureInfo("tr-TR");
			string tip = textBox6.Text?.Trim()??"";
			if(string.IsNullOrWhiteSpace(tip))
			{
				MessageBox.Show("Cari tipi girin!");
				return;
			}

			string tipUpper = tip.ToUpper(tr);
			int id = Convert.ToInt32(textBox5.Text);

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM CariTipi WHERE UCASE(TipAdi)=? AND CariTipID<>?" , conn))
				{
					kontrol.Parameters.AddWithValue("?" , tipUpper);
					kontrol.Parameters.AddWithValue("?" , id);
					if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
					{
						MessageBox.Show("Bu cari tipi zaten kayıtlı!");
						return;
					}
				}

				using(OleDbCommand guncelle = new OleDbCommand("UPDATE CariTipi SET TipAdi=? WHERE CariTipID=?" , conn))
				{
					guncelle.Parameters.AddWithValue("?" , tipUpper);
					guncelle.Parameters.AddWithValue("?" , id);
					guncelle.ExecuteNonQuery();
				}
			}

			Listele5();
			CariTipComboYenile();
			CariTipTemizle();
		}

		private void CariTipTemizle ()
		{
			textBox5.Clear();
			textBox6.Clear();
			dataGridView4.ClearSelection();
		}

		private void DataGridView4_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||e.RowIndex>=dataGridView4.Rows.Count) return;

			DataGridViewRow satir = dataGridView4.Rows[e.RowIndex];
			if(dataGridView4.Columns.Contains("CariTipID"))
				textBox5.Text=satir.Cells["CariTipID"].Value?.ToString()??"";
			if(dataGridView4.Columns.Contains("TipAdi"))
				textBox6.Text=satir.Cells["TipAdi"].Value?.ToString()??"";
		}

		private void CariDurumSil_Click ( object sender , EventArgs e ) => CariDurumSil();
		private void CariDurumGuncelle_Click ( object sender , EventArgs e ) => CariDurumGuncelle();
		private void CariDurumTemizle_Click ( object sender , EventArgs e ) => CariDurumTemizle();

		private void CariDurumKaydet ()
		{
			CultureInfo tr = new CultureInfo("tr-TR");
			string durum = comboBox13.Text?.Trim()??"";
			if(string.IsNullOrWhiteSpace(durum))
			{
				MessageBox.Show("Cari durum girin!");
				return;
			}

			string durumUpper = durum.ToUpper(tr);
			object cariTipId = comboBox12.SelectedValue;
			if(cariTipId==null||cariTipId==DBNull.Value)
				cariTipId=DBNull.Value;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				if(_cariDurumCariTipKolonuVar)
				{
					string kontrolSql = cariTipId==DBNull.Value
						? "SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariTipID IS NULL"
						: "SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariTipID=?";

					using(OleDbCommand kontrol = new OleDbCommand(kontrolSql , conn))
					{
						kontrol.Parameters.AddWithValue("?" , durumUpper);
						if(cariTipId!=DBNull.Value)
							kontrol.Parameters.AddWithValue("?" , cariTipId);

						if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
						{
							MessageBox.Show("Bu cari durum zaten kayıtlı!");
							return;
						}
					}

					using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariDurumlari (DurumAdi, CariTipID) VALUES (?, ?)" , conn))
					{
						ekle.Parameters.AddWithValue("?" , durumUpper);
						ekle.Parameters.AddWithValue("?" , cariTipId);
						ekle.ExecuteNonQuery();
					}
				}
				else
				{
					using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=?" , conn))
					{
						kontrol.Parameters.AddWithValue("?" , durumUpper);
						if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
						{
							MessageBox.Show("Bu cari durum zaten kayıtlı!");
							return;
						}
					}

					using(OleDbCommand ekle = new OleDbCommand("INSERT INTO CariDurumlari (DurumAdi) VALUES (?)" , conn))
					{
						ekle.Parameters.AddWithValue("?" , durumUpper);
						ekle.ExecuteNonQuery();
					}
				}
			}

			CariDurumComboYenile();
			Listele6();
			CariDurumTemizle();
		}

		private void CariDurumSil ()
		{
			int id;
			if(!int.TryParse(textBox7.Text , out id))
			{
				MessageBox.Show("Silmek için bir cari durum seçin!");
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand sil = new OleDbCommand("DELETE FROM CariDurumlari WHERE CariDurumID=?" , conn))
				{
					sil.Parameters.AddWithValue("?" , id);
					sil.ExecuteNonQuery();
				}
			}

			CariDurumComboYenile();
			Listele6();
			CariDurumTemizle();
		}

		private void CariDurumGuncelle ()
		{
			int id;
			if(!int.TryParse(textBox7.Text , out id))
			{
				MessageBox.Show("Güncellemek için bir cari durum seçin!");
				return;
			}

			CultureInfo tr = new CultureInfo("tr-TR");
			string durum = comboBox13.Text?.Trim()??"";
			if(string.IsNullOrWhiteSpace(durum))
			{
				MessageBox.Show("Cari durum girin!");
				return;
			}

			string durumUpper = durum.ToUpper(tr);
			object cariTipId = comboBox12.SelectedValue;
			if(cariTipId==null||cariTipId==DBNull.Value)
				cariTipId=DBNull.Value;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				if(_cariDurumCariTipKolonuVar)
				{
					string kontrolSql = cariTipId==DBNull.Value
						? "SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariTipID IS NULL AND CariDurumID<>?"
						: "SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariTipID=? AND CariDurumID<>?";

					using(OleDbCommand kontrol = new OleDbCommand(kontrolSql , conn))
					{
						kontrol.Parameters.AddWithValue("?" , durumUpper);
						if(cariTipId!=DBNull.Value)
							kontrol.Parameters.AddWithValue("?" , cariTipId);
						kontrol.Parameters.AddWithValue("?" , id);

						if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
						{
							MessageBox.Show("Bu cari durum zaten kayıtlı!");
							return;
						}
					}

					using(OleDbCommand guncelle = new OleDbCommand("UPDATE CariDurumlari SET DurumAdi=?, CariTipID=? WHERE CariDurumID=?" , conn))
					{
						guncelle.Parameters.AddWithValue("?" , durumUpper);
						guncelle.Parameters.AddWithValue("?" , cariTipId);
						guncelle.Parameters.AddWithValue("?" , id);
						guncelle.ExecuteNonQuery();
					}
				}
				else
				{
					using(OleDbCommand kontrol = new OleDbCommand("SELECT COUNT(*) FROM CariDurumlari WHERE UCASE(DurumAdi)=? AND CariDurumID<>?" , conn))
					{
						kontrol.Parameters.AddWithValue("?" , durumUpper);
						kontrol.Parameters.AddWithValue("?" , id);
						if(Convert.ToInt32(kontrol.ExecuteScalar())>0)
						{
							MessageBox.Show("Bu cari durum zaten kayıtlı!");
							return;
						}
					}

					using(OleDbCommand guncelle = new OleDbCommand("UPDATE CariDurumlari SET DurumAdi=? WHERE CariDurumID=?" , conn))
					{
						guncelle.Parameters.AddWithValue("?" , durumUpper);
						guncelle.Parameters.AddWithValue("?" , id);
						guncelle.ExecuteNonQuery();
					}
				}
			}

			CariDurumComboYenile();
			Listele6();
			CariDurumTemizle();
		}

		private void CariDurumTemizle ()
		{
			textBox7.Clear();
			comboBox12.SelectedIndex=-1;
			comboBox13.SelectedIndex=-1;
			comboBox13.Text=string.Empty;
			dataGridView6.ClearSelection();
		}

		private void DataGridView6_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||e.RowIndex>=dataGridView6.Rows.Count) return;

			DataGridViewRow satir = dataGridView6.Rows[e.RowIndex];
			if(dataGridView6.Columns.Contains("CariDurumID"))
				textBox7.Text=satir.Cells["CariDurumID"].Value?.ToString()??"";
			if(dataGridView6.Columns.Contains("DurumAdi"))
				comboBox13.Text=satir.Cells["DurumAdi"].Value?.ToString()??"";

			if(dataGridView6.Columns.Contains("CariTipID")&&satir.Cells["CariTipID"].Value!=null&&satir.Cells["CariTipID"].Value!=DBNull.Value)
				comboBox12.SelectedValue=satir.Cells["CariTipID"].Value;
			else
				comboBox12.SelectedIndex=-1;
		}
		
		public void Listele4 ()
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// Görsellerine göre tam uyumlu sorgu:
				// CariTipi tablosundaki sütun adın 'ID' olduğu için CT.ID kullandık.
				string sorgu = @"SELECT
                               USF.UrunSatisFiyatID,
                               U.UrunID,
                               U.UrunAdi,
                               IIF(M.MarkaAdi IS NULL, '', M.MarkaAdi) AS MarkaAdi,
                               IIF(K.KategoriAdi IS NULL, '', K.KategoriAdi) AS KategoriAdi,
                               CT.TipAdi,
                               USF.ZamOrani,
                               USF.SatisFiyati,
                               USF.Tarih
                        FROM ((((UrunSatisFiyat AS USF
                        INNER JOIN Urunler AS U ON USF.UrunID = U.UrunID)
                        LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
                        LEFT JOIN Kategoriler AS K ON CLng(IIF(U.KategoriID IS NULL, 0, U.KategoriID)) = K.KategoriID)
                        INNER JOIN CariTipi AS CT ON CLng(IIF(USF.CariTipiID IS NULL, 0, USF.CariTipiID)) = CT.CariTipID)
                        ORDER BY USF.UrunSatisFiyatID ASC";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);
			
				dataGridView18.DataSource=dt;
				if(dataGridView18.Columns.Contains("UrunID"))
					dataGridView18.Columns["UrunID"].Visible=false;
				// comboBox8'in özelliklerini kodla veya Properties panelinden ayarla:
				comboBox8.AutoCompleteMode=AutoCompleteMode.SuggestAppend;
				comboBox8.AutoCompleteSource=AutoCompleteSource.ListItems;
				if(dataGridView18.Columns.Contains("UrunAdi"))
					dataGridView18.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
				if(dataGridView18.Columns.Contains("MarkaAdi"))
					dataGridView18.Columns["MarkaAdi"].HeaderText="MARKA";
				if(dataGridView18.Columns.Contains("KategoriAdi"))
					dataGridView18.Columns["KategoriAdi"].HeaderText="KATEGORİ";
				dataGridView18.Columns["Tarih"].HeaderText="TARİH";
				dataGridView18.ClearSelection(); // Grid üzerindeki otomatik mavi seçimi kaldırır
				Temizle4();         // Kutuların içini boşaltır
				SatisUrunleriniYenile();
				GridAramaFiltresiniUygula(textBox38 , dataGridView18);

				// Başlıkları düzelt
				//dataGridView18.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
				//dataGridView18.Columns["CariTipi"].HeaderText="CARİ GRUBU";

				baglanti.Close();
			}
			catch(Exception ex)
			{
				// Hata mesajını daha detaylı görmek için:
				MessageBox.Show("Sorgu Hatası: "+ex.Message+"\nLütfen sütun isimlerini kontrol edin.");
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}
		//alislistele
		public void Listele3 ()
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// Bu sorgu 'Canlı' bağlantı kurar. 
				// Urunler tablosunda adı değiştirdiğin an burası da değişmiş olur.
				string sorgu = @"SELECT
                         A.UrunAlisID,
                         U.UrunID,
                         U.UrunAdi,
                         IIF(M.MarkaAdi IS NULL, '', M.MarkaAdi) AS MarkaAdi,
                         IIF(K.KategoriAdi IS NULL, '', K.KategoriAdi) AS KategoriAdi,
                         A.BirimAlisFiyati,
                         A.IskontoOrani,
                         A.NetAlisFiyati,
                         A.Tarih
                         FROM ((UrunAlis AS A
                         INNER JOIN Urunler AS U ON A.UrunID = U.UrunID)
                         LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
                         LEFT JOIN Kategoriler AS K ON CLng(IIF(U.KategoriID IS NULL, 0, U.KategoriID)) = K.KategoriID
                         ORDER BY A.UrunAlisID ASC";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);

				dataGridView3.DataSource=dt;

				// Başlıkları tekrar set etmeyi unutma
				if(dataGridView3.Columns.Contains("UrunID"))
					dataGridView3.Columns["UrunID"].Visible=false;
				dataGridView3.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
				if(dataGridView3.Columns.Contains("MarkaAdi"))
					dataGridView3.Columns["MarkaAdi"].HeaderText="MARKA";
				if(dataGridView3.Columns.Contains("KategoriAdi"))
					dataGridView3.Columns["KategoriAdi"].HeaderText="KATEGORİ";
				dataGridView3.ClearSelection();
				// ... diğer başlıklar ...
				Temizle();
				GridAramaFiltresiniUygula(textBox2 , dataGridView3);
				baglanti.Close();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		//ürün listele
		public void Listele1 ()
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// JOIN kısmına U.UrunID, U.KategoriID gibi anahtar sütunları ekledik
				string sorgu = @"SELECT U.UrunID, U.UrunAdi, U.KategoriID, K.KategoriAdi, 
                         U.MarkaID, M.MarkaAdi, U.BirimID, B.BirimAdi, 
                         U.StokMiktari, U.AktifMi
                         FROM (((Urunler AS U
                         LEFT JOIN Kategoriler AS K ON U.KategoriID = K.KategoriID)
                         LEFT JOIN Markalar AS M ON U.MarkaID = M.MarkaID)
                         LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID)";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);
				dataGridView1.DataSource=dt;

				// Başlıkları Güzelleştir ve Büyük Harf Yap
				dataGridView1.Columns["UrunID"].HeaderText="ID";
				dataGridView1.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
				dataGridView1.Columns["KategoriAdi"].HeaderText="KATEGORİ";
				dataGridView1.Columns["MarkaAdi"].HeaderText="MARKA";
				dataGridView1.Columns["BirimAdi"].HeaderText="BİRİM";
				dataGridView1.Columns["StokMiktari"].HeaderText="STOK MİKTARI";
				dataGridView1.Columns["AktifMi"].HeaderText="DURUM";

				// Kullanıcının görmesine gerek olmayan ID sütunlarını gizle (ama veriler arkada dursun)
				dataGridView1.Columns["KategoriID"].Visible=false;
				dataGridView1.Columns["MarkaID"].Visible=false;
				dataGridView1.Columns["BirimID"].Visible=false;
				// Görsel Hizalama (Opsiyonel ama şık durur)

				dataGridView1.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI" , 10 , FontStyle.Bold);
				UrunIstatistikleriniYenile(dt);
				AnaSayfaGridleriniYenile();
				GridAramaFiltresiniUygula(textBox1 , dataGridView1);


				baglanti.Close();
			}
			catch(Exception ex) { UrunIstatistikleriniYenile(null); MessageBox.Show("Listeleme Hatası: "+ex.Message); baglanti.Close(); }
		}

		private void UrunIstatistikleriniYenile ( DataTable dt )
		{
			UrunKartGorunumunuAyarla();

			int toplamUrun = dt?.Rows.Count??0;
			int toplamKategori = 0;
			string enYuksekUrun = "-";
			string enDusukUrun = "-";
			bool enYuksekBulundu = false;
			bool enDusukBulundu = false;

			try
			{
				if(baglanti.State!=ConnectionState.Open)
					baglanti.Open();

				using(OleDbCommand cmd = new OleDbCommand("SELECT COUNT(*) FROM [Kategoriler]" , baglanti))
					toplamKategori=Convert.ToInt32(cmd.ExecuteScalar());

				string yuksekSorgu = @"SELECT TOP 1 U.[UrunAdi], USF.[SatisFiyati]
									FROM [UrunSatisFiyat] AS USF
									INNER JOIN [Urunler] AS U ON USF.[UrunID]=U.[UrunID]
									WHERE USF.[SatisFiyati] IS NOT NULL
									ORDER BY USF.[SatisFiyati] DESC, U.[UrunAdi]";
				using(OleDbCommand cmd = new OleDbCommand(yuksekSorgu , baglanti))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd!=null&&rd.Read())
					{
						enYuksekUrun=Convert.ToString(rd["UrunAdi"])??"-";
						enYuksekBulundu=true;
					}
				}

				string dusukSorgu = @"SELECT TOP 1 U.[UrunAdi], USF.[SatisFiyati]
									FROM [UrunSatisFiyat] AS USF
									INNER JOIN [Urunler] AS U ON USF.[UrunID]=U.[UrunID]
									WHERE USF.[SatisFiyati] IS NOT NULL
									ORDER BY USF.[SatisFiyati] ASC, U.[UrunAdi]";
				using(OleDbCommand cmd = new OleDbCommand(dusukSorgu , baglanti))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd!=null&&rd.Read())
					{
						enDusukUrun=Convert.ToString(rd["UrunAdi"])??"-";
						enDusukBulundu=true;
					}
				}
			}
			catch
			{
				toplamKategori=0;
				enYuksekUrun="-";
				enDusukUrun="-";
				enYuksekBulundu=false;
				enDusukBulundu=false;
			}

			label97.Text=toplamUrun.ToString("N0" , _yazdirmaKulturu);
			label21.Text=toplamKategori.ToString("N0" , _yazdirmaKulturu);
			label28.Text=enYuksekBulundu ? enYuksekUrun : "-";
			label19.Text=enDusukBulundu ? enDusukUrun : "-";
		}

		private void UrunKartGorunumunuAyarla ()
		{
			label98.Text="Toplam Ürün Sayısı";
			label22.Text="Toplam Kategori Sayısı";
			label29.Text="En Yüksek Fiyatlı Ürün";
			label18.Text="En Düşük Fiyatlı Ürün";

			label97.AutoSize=true;
			label21.AutoSize=true;
			label97.Font=new Font("Microsoft Sans Serif" , 19.8F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162)));
			label21.Font=new Font("Microsoft Sans Serif" , 19.8F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162)));

			Size ortakCevapBoyutu = UrunKartCevapBoyutunuGetir();
			UrunKartCevapLabeliniAyarla(label28 , ortakCevapBoyutu);
			UrunKartCevapLabeliniAyarla(label19 , ortakCevapBoyutu);

			if(label20!=null)
				label20.Visible=false;
		}

		private Size UrunKartCevapBoyutunuGetir ()
		{
			int varsayilanGenislik = 220;
			int varsayilanYukseklik = 64;
			int solKartGenisligi = label28?.Parent==null ? varsayilanGenislik : Math.Max(160 , label28.Parent.ClientSize.Width-label28.Left-12);
			int sagKartGenisligi = label19?.Parent==null ? varsayilanGenislik : Math.Max(160 , label19.Parent.ClientSize.Width-label19.Left-12);
			return new Size(Math.Min(solKartGenisligi , sagKartGenisligi) , varsayilanYukseklik);
		}

		private void UrunKartCevapLabeliniAyarla ( Label cevapLabeli , Size hedefBoyut )
		{
			if(cevapLabeli==null)
				return;

			Point mevcutKonum = cevapLabeli.Location;
			cevapLabeli.AutoSize=false;
			cevapLabeli.Font=new Font("Microsoft Sans Serif" , 12.5F , FontStyle.Bold , GraphicsUnit.Point , ((byte)(162)));
			cevapLabeli.Location=mevcutKonum;
			cevapLabeli.Size=hedefBoyut;
			cevapLabeli.TextAlign=ContentAlignment.MiddleLeft;
			cevapLabeli.AutoEllipsis=true;
			cevapLabeli.BringToFront();
		}

		private void AnaSayfaGridleriniYenile ()
		{
			AnaSayfaKritikSeviyeListele();
			AnaSayfaFihristListele();
			AnaSayfaKategoriUrunListele();
			AnaSayfaBugunYapilacaklarListele();
		}

		private void TabControl1_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(tabControl1==null||dataGridView9==null||tabControl1.SelectedTab==null)
				return;

			if(string.Equals(tabControl1.SelectedTab.Text , "AnaSayfa" , StringComparison.OrdinalIgnoreCase))
				AnaSayfaGridleriniYenile();
		}

		private void AnaSayfaKritikSeviyeListele ()
		{
			if(dataGridView9==null)
				return;

			DataTable dt = KritikSeviyeTablosunuOlustur();
			try
			{
				DataTable hamVeriTablosu = new DataTable();
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT
					U.[UrunID],
					IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) AS UrunAdi,
					U.[MarkaID],
					U.[StokMiktari]
								FROM [Urunler] AS U";
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(hamVeriTablosu);
				}

				Dictionary<int, string> markaSozlugu = KritikMarkaSozlugunuGetir();

				foreach(DataRow satir in hamVeriTablosu.Rows)
				{
					decimal stokMiktari = KritikStokMiktariGetir(satir["StokMiktari"]);
					if(stokMiktari>=AnaSayfaKritikStokEsigi)
						continue;

					DataRow yeniSatir = dt.NewRow();
					yeniSatir["UrunID"]=satir["UrunID"]==DBNull.Value ? 0 : Convert.ToInt32(satir["UrunID"]);
					yeniSatir["UrunAdi"]=Convert.ToString(satir["UrunAdi"])??string.Empty;
					int markaId = KritikTamSayiGetir(satir["MarkaID"]);
					yeniSatir["MarkaAdi"]=markaSozlugu.TryGetValue(markaId , out string markaAdi) ? markaAdi : string.Empty;
					yeniSatir["StokMiktari"]=stokMiktari;
					dt.Rows.Add(yeniSatir);
				}

				if(dt.Rows.Count>0)
					dt=dt.DefaultView.ToTable(false , "UrunID" , "UrunAdi" , "MarkaAdi" , "StokMiktari");
			}
			catch
			{
				dt=KritikSeviyeTablosunuOlustur();
			}

			dataGridView9.DataSource=dt;
			DatagridviewSetting(dataGridView9);
			KritikSeviyeGridStiliUygula();
			if(dataGridView9.Columns.Contains("UrunID"))
				dataGridView9.Columns["UrunID"].Visible=false;
			if(dataGridView9.Columns.Contains("StokMiktari"))
				dataGridView9.Columns["StokMiktari"].DefaultCellStyle.Format="N0";
			dataGridView9.ClearSelection();
		}

		private DataTable KritikSeviyeTablosunuOlustur ()
		{
			DataTable dt = new DataTable();
			dt.Columns.Add("UrunID" , typeof(int));
			dt.Columns.Add("UrunAdi" , typeof(string));
			dt.Columns.Add("MarkaAdi" , typeof(string));
			dt.Columns.Add("StokMiktari" , typeof(decimal));
			dt.DefaultView.Sort="StokMiktari ASC, UrunAdi ASC";
			return dt;
		}

		private Dictionary<int, string> KritikMarkaSozlugunuGetir ()
		{
			Dictionary<int, string> markaSozlugu = new Dictionary<int, string>();

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("SELECT [MarkaID], IIF([MarkaAdi] IS NULL, '', [MarkaAdi]) AS MarkaAdi FROM [Markalar]" , conn))
					using(OleDbDataReader reader = cmd.ExecuteReader())
					{
						while(reader!=null&&reader.Read())
						{
							int markaId = KritikTamSayiGetir(reader["MarkaID"]);
							if(markaId<=0||markaSozlugu.ContainsKey(markaId))
								continue;

							markaSozlugu[markaId]=Convert.ToString(reader["MarkaAdi"])??string.Empty;
						}
					}
				}
			}
			catch
			{
				return markaSozlugu;
			}

			return markaSozlugu;
		}

		private int KritikTamSayiGetir ( object deger )
		{
			if(deger==null||deger==DBNull.Value)
				return 0;

			try
			{
				return Convert.ToInt32(deger);
			}
			catch
			{
				string metin = Convert.ToString(deger)??string.Empty;
				if(string.IsNullOrWhiteSpace(metin))
					return 0;

				if(int.TryParse(metin , NumberStyles.Integer , CultureInfo.CurrentCulture , out int sayi)||
				   int.TryParse(metin , NumberStyles.Integer , CultureInfo.InvariantCulture , out sayi))
					return sayi;

				decimal ondalik = SepetDecimalParse(metin);
				return decimal.ToInt32(decimal.Truncate(ondalik));
			}
		}

		private decimal KritikStokMiktariGetir ( object deger )
		{
			if(deger==null||deger==DBNull.Value)
				return 0m;

			try
			{
				return Convert.ToDecimal(deger);
			}
			catch
			{
				return SepetDecimalParse(Convert.ToString(deger));
			}
		}

		private bool KritikSeviyedeAktifUrunMu ( object deger )
		{
			if(deger==null||deger==DBNull.Value)
				return true;

			if(deger is bool mantiksalDeger)
				return mantiksalDeger;

			string metin = Convert.ToString(deger)??string.Empty;
			if(string.IsNullOrWhiteSpace(metin))
				return true;

			if(bool.TryParse(metin , out bool sonuc))
				return sonuc;

			switch(KarsilastirmaMetniHazirla(metin))
			{
				case "1":
				case "-1":
				case "AKTIF":
				case "TRUE":
				case "EVET":
					return true;
				case "0":
				case "FALSE":
				case "PASIF":
				case "HAYIR":
					return false;
				default:
					return true;
			}
		}

		private void KritikSeviyeGridStiliUygula ()
		{
			AnaMenuGridDevExpressTemasiUygula(dataGridView9);
		}

		private void AnaMenuGridDevExpressTemasiUygula ( DataGridView datagridview )
		{
			if(datagridview==null)
				return;

			Color formUyumluArkaPlan = AnaMenuGridBosAlanRenginiGetir(datagridview);
			Color yaziRengi = Color.Black;
			Color headerYaziRengi = Color.White;
			Color headerArkaPlan = Color.FromArgb(0 , 179 , 179);
			Color izgaraRengi = Color.FromArgb(118 , 208 , 208);

			datagridview.BackgroundColor=formUyumluArkaPlan;
			datagridview.BorderStyle=BorderStyle.FixedSingle;
			datagridview.CellBorderStyle=DataGridViewCellBorderStyle.Single;
			datagridview.GridColor=izgaraRengi;
			datagridview.RowHeadersVisible=false;
			datagridview.AllowUserToAddRows=false;
			datagridview.AllowUserToDeleteRows=false;
			datagridview.AllowUserToResizeRows=false;
			datagridview.ReadOnly=true;
			datagridview.EnableHeadersVisualStyles=false;
			datagridview.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;
			datagridview.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			datagridview.ColumnHeadersHeight=34;
			datagridview.RowTemplate.Height=30;
			datagridview.DefaultCellStyle.Padding=new Padding(6 , 0 , 6 , 0);
			datagridview.DefaultCellStyle.Font=new Font("Tahoma" , 8.5F , FontStyle.Regular);
			datagridview.DefaultCellStyle.BackColor=Color.FromArgb(0 , 179 , 179);
			datagridview.DefaultCellStyle.ForeColor=yaziRengi;
			datagridview.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(35 , 192 , 192);
			datagridview.AlternatingRowsDefaultCellStyle.ForeColor=yaziRengi;
			datagridview.DefaultCellStyle.SelectionBackColor=Color.FromArgb(204 , 239 , 239);
			datagridview.DefaultCellStyle.SelectionForeColor=Color.Black;
			datagridview.RowsDefaultCellStyle.BackColor=Color.FromArgb(0 , 179 , 179);
			datagridview.RowsDefaultCellStyle.ForeColor=yaziRengi;
			datagridview.RowsDefaultCellStyle.SelectionBackColor=Color.FromArgb(204 , 239 , 239);
			datagridview.RowsDefaultCellStyle.SelectionForeColor=Color.Black;
			datagridview.ColumnHeadersDefaultCellStyle.BackColor=headerArkaPlan;
			datagridview.ColumnHeadersDefaultCellStyle.ForeColor=headerYaziRengi;
			datagridview.ColumnHeadersDefaultCellStyle.SelectionBackColor=headerArkaPlan;
			datagridview.ColumnHeadersDefaultCellStyle.SelectionForeColor=headerYaziRengi;
			datagridview.ColumnHeadersDefaultCellStyle.Font=new Font("Tahoma" , 8.5F , FontStyle.Bold);
			datagridview.DefaultCellStyle.WrapMode=DataGridViewTriState.False;

			datagridview.CellPainting-=AnaMenuGrid_CellPainting;
			datagridview.CellPainting+=AnaMenuGrid_CellPainting;
			datagridview.Paint-=AnaMenuGrid_Paint;
			datagridview.Paint+=AnaMenuGrid_Paint;
		}

		private Color AnaMenuGridBosAlanRenginiGetir ( Control control )
		{
			Control mevcutKontrol = control?.Parent;
			while(mevcutKontrol!=null)
			{
				Color arkaPlanRengi = mevcutKontrol.BackColor;
				if(!arkaPlanRengi.IsEmpty&&arkaPlanRengi!=Color.Transparent&&arkaPlanRengi.A==255)
					return arkaPlanRengi;

				mevcutKontrol=mevcutKontrol.Parent;
			}

			Color formArkaPlanRengi = this.BackColor;
			if(!formArkaPlanRengi.IsEmpty&&formArkaPlanRengi!=Color.Transparent&&formArkaPlanRengi.A==255)
				return formArkaPlanRengi;

			return SystemColors.Control;
		}

		private void FihristGridStiliUygula ()
		{
			AnaMenuGridDevExpressTemasiUygula(dataGridView10);
		}

		private void KategoriUrunGridStiliUygula ()
		{
			AnaMenuGridDevExpressTemasiUygula(dataGridView11);
		}

		private void BugunYapilacaklarGridStiliUygula ()
		{
			AnaMenuGridDevExpressTemasiUygula(dataGridView12);
		}

		private void AnaMenuGrid_Paint ( object sender , PaintEventArgs e )
		{
			if(!(sender is DataGridView datagridview))
				return;

			using(Pen cerceveKalemi = new Pen(Color.FromArgb(151 , 208 , 208)))
				e.Graphics.DrawRectangle(cerceveKalemi , 0 , 0 , datagridview.Width-1 , datagridview.Height-1);
		}

		private void AnaMenuGrid_CellPainting ( object sender , DataGridViewCellPaintingEventArgs e )
		{
			if(!(sender is DataGridView datagridview))
				return;

			if(datagridview!=dataGridView9&&datagridview!=dataGridView10&&datagridview!=dataGridView11&&datagridview!=dataGridView12)
				return;

			bool headerHucresi = e.RowIndex<0;
			if(headerHucresi)
			{
				e.Handled=true;
				using(LinearGradientBrush headerFirca = new LinearGradientBrush(e.CellBounds , Color.FromArgb(0 , 170 , 170) , Color.FromArgb(79 , 214 , 214) , LinearGradientMode.Horizontal))
					e.Graphics.FillRectangle(headerFirca , e.CellBounds);
				using(Pen headerKalemi = new Pen(Color.FromArgb(120 , 200 , 200)))
					e.Graphics.DrawRectangle(headerKalemi , e.CellBounds.X , e.CellBounds.Y , e.CellBounds.Width-1 , e.CellBounds.Height-1);
				TextRenderer.DrawText(
					e.Graphics ,
					Convert.ToString(e.FormattedValue)??string.Empty ,
					e.CellStyle.Font??datagridview.ColumnHeadersDefaultCellStyle.Font??datagridview.Font ,
					Rectangle.Inflate(e.CellBounds , -6 , 0) ,
					Color.White ,
					TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
				return;
			}

			if(e.ColumnIndex<0)
				return;

			bool secili = (e.State&DataGridViewElementStates.Selected)==DataGridViewElementStates.Selected;
			Color baslangicRenk;
			Color bitisRenk;
			Color yaziRengi;

			if(secili)
			{
				baslangicRenk=Color.FromArgb(204 , 239 , 239);
				bitisRenk=Color.FromArgb(186 , 230 , 230);
				yaziRengi=Color.Black;
			}
			else if(e.RowIndex%2==0)
			{
				baslangicRenk=Color.FromArgb(0 , 168 , 168);
				bitisRenk=Color.FromArgb(51 , 201 , 201);
				yaziRengi=Color.Black;
			}
			else
			{
				baslangicRenk=Color.FromArgb(0 , 156 , 156);
				bitisRenk=Color.FromArgb(40 , 188 , 188);
				yaziRengi=Color.Black;
			}

			e.Handled=true;
			using(LinearGradientBrush firca = new LinearGradientBrush(e.CellBounds , baslangicRenk , bitisRenk , LinearGradientMode.Horizontal))
				e.Graphics.FillRectangle(firca , e.CellBounds);
			using(Pen izgaraKalemi = new Pen(Color.FromArgb(132 , 214 , 214)))
				e.Graphics.DrawRectangle(izgaraKalemi , e.CellBounds.X , e.CellBounds.Y , e.CellBounds.Width-1 , e.CellBounds.Height-1);

			Rectangle icerikAlani = Rectangle.Inflate(e.CellBounds , -6 , 0);
			TextFormatFlags hizalama = TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis;
			if(e.CellStyle.Alignment==DataGridViewContentAlignment.MiddleRight||e.CellStyle.Alignment==DataGridViewContentAlignment.TopRight||e.CellStyle.Alignment==DataGridViewContentAlignment.BottomRight)
				hizalama|=TextFormatFlags.Right;
			else if(e.CellStyle.Alignment==DataGridViewContentAlignment.MiddleCenter||e.CellStyle.Alignment==DataGridViewContentAlignment.TopCenter||e.CellStyle.Alignment==DataGridViewContentAlignment.BottomCenter)
				hizalama|=TextFormatFlags.HorizontalCenter;
			else
				hizalama|=TextFormatFlags.Left;

			TextRenderer.DrawText(
				e.Graphics ,
				Convert.ToString(e.FormattedValue)??string.Empty ,
				e.CellStyle.Font??datagridview.Font ,
				icerikAlani ,
				yaziRengi ,
				hizalama);
		}

		private void AnaSayfaFihristListele ()
		{
			if(dataGridView10==null)
				return;

			DataTable dt = new DataTable();
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT
									IIF(C.[adsoyad] IS NULL, '', C.[adsoyad]) AS AdSoyad,
									IIF(C.[telefon] IS NULL, '', C.[telefon]) AS Telefon
								FROM (([Cariler] AS C
								LEFT JOIN [CariDurumlari] AS CD ON CLng(IIF(C.[CariDurumID] IS NULL, 0, C.[CariDurumID])) = CD.[CariDurumID])
								LEFT JOIN [CariTipi] AS CT ON CLng(IIF(C.[CariTipID] IS NULL, 0, C.[CariTipID])) = CT.[CariTipID])
								WHERE UCase(IIF(CD.[DurumAdi] IS NULL, '', CD.[DurumAdi]))='AKTİF'
								  AND UCase(IIF(CT.[TipAdi] IS NULL, '', CT.[TipAdi])) IN ('MÜŞTERİ', 'FABRİKA')
								  AND (IIF(C.[adsoyad] IS NULL, '', C.[adsoyad])<>'' OR IIF(C.[telefon] IS NULL, '', C.[telefon])<>'')
								ORDER BY IIF(C.[adsoyad] IS NULL, '', C.[adsoyad]) ASC,
										IIF(C.[telefon] IS NULL, '', C.[telefon]) ASC";
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);
				}
			}
			catch
			{
				dt=new DataTable();
			}

			dataGridView10.DataSource=dt;
			DatagridviewSetting(dataGridView10);
			FihristGridStiliUygula();
			if(dataGridView10.Columns.Contains("AdSoyad"))
				dataGridView10.Columns["AdSoyad"].HeaderText="AD SOYAD";
			if(dataGridView10.Columns.Contains("Telefon"))
				dataGridView10.Columns["Telefon"].HeaderText="TELEFON";
			dataGridView10.ClearSelection();
		}

		private void AnaSayfaKategoriUrunListele ()
		{
			if(dataGridView11==null)
				return;

			DataTable dt = new DataTable();
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT
									K.[KategoriID],
									IIF(K.[KategoriAdi] IS NULL, '', K.[KategoriAdi]) AS KategoriAdi,
									COUNT(U.[UrunID]) AS UrunSayisi,
									SUM(IIF(U.[StokMiktari] IS NULL, 0, U.[StokMiktari])) AS ToplamStok
								FROM [Kategoriler] AS K
								LEFT JOIN [Urunler] AS U ON CLng(IIF(U.[KategoriID] IS NULL, 0, U.[KategoriID])) = K.[KategoriID]
								GROUP BY K.[KategoriID], K.[KategoriAdi]
								ORDER BY COUNT(U.[UrunID]) DESC, IIF(K.[KategoriAdi] IS NULL, '', K.[KategoriAdi]) ASC";
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);
				}
			}
			catch
			{
				dt=new DataTable();
			}

			dataGridView11.DataSource=dt;
			DatagridviewSetting(dataGridView11);
			KategoriUrunGridStiliUygula();
			if(dataGridView11.Columns.Contains("KategoriID"))
				dataGridView11.Columns["KategoriID"].Visible=false;
			if(dataGridView11.Columns.Contains("UrunSayisi"))
				dataGridView11.Columns["UrunSayisi"].DefaultCellStyle.Format="N0";
			if(dataGridView11.Columns.Contains("ToplamStok"))
				dataGridView11.Columns["ToplamStok"].DefaultCellStyle.Format="N0";
			dataGridView11.ClearSelection();
		}

		private void AnaSayfaBugunYapilacaklarListele ()
		{
			if(dataGridView12==null)
				return;

			DataTable dt = new DataTable();
			try
			{
				EnsureNotAltyapi();
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					string sorgu = @"SELECT
									IIF(N.[Baslik] IS NULL OR N.[Baslik]='', 'Not ' & CStr(N.[NotID]), N.[Baslik]) AS Baslik,
									N.[Tarih] AS Tarih,
									IIF(N.[NotMetni] IS NULL, '', N.[NotMetni]) AS Aciklama
								FROM [Notlarim] AS N
								WHERE N.[Tarih] IS NOT NULL
								  AND DateValue(N.[Tarih])=Date()
								  AND IIF(N.[Okundu] IS NULL, False, N.[Okundu])=False
								ORDER BY N.[Tarih] DESC,
										IIF(N.[Baslik] IS NULL, '', N.[Baslik]) ASC";
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
						da.Fill(dt);
				}
			}
			catch
			{
				dt=new DataTable();
			}

			dataGridView12.DataSource=dt;
			DatagridviewSetting(dataGridView12);
			BugunYapilacaklarGridStiliUygula();
			if(dataGridView12.Columns.Contains("Baslik"))
				dataGridView12.Columns["Baslik"].HeaderText="NOT BAŞLIĞI";
			if(dataGridView12.Columns.Contains("Tarih"))
			{
				dataGridView12.Columns["Tarih"].HeaderText="TARİH";
				dataGridView12.Columns["Tarih"].DefaultCellStyle.Format="g";
			}
			if(dataGridView12.Columns.Contains("Aciklama"))
				dataGridView12.Columns["Aciklama"].HeaderText="AÇIKLAMA";
			dataGridView12.ClearSelection();
		}
		//CARİ LİSTELE
		private void Listele ()
		{
			try
			{
				// INNER JOIN kullanarak ID yerine isimleri çekiyoruz
				string sorgu = @"SELECT 
                            C.CariID AS [CariID], 
                            C.tc AS [tc], 
                            C.adsoyad AS [adsoyad], 
                            C.telefon AS [telefon], 
                            C.adres AS [adres], 
                            D.DurumAdi AS [DurumAdi], 
                            T.TipAdi AS [TipAdi],
                            C.CariDurumID AS [CariDurumID], 
                            C.CariTipID AS [CariTipID] 
                         FROM ((Cariler AS C
                         LEFT JOIN CariDurumlari AS D ON CLng(IIF(C.CariDurumID IS NULL, 0, C.CariDurumID)) = D.CariDurumID)
                         LEFT JOIN CariTipi AS T ON CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = T.CariTipID)
                         ORDER BY IIF(T.TipAdi IS NULL, '', T.TipAdi), IIF(C.adsoyad IS NULL, '', C.adsoyad)";

				OleDbDataAdapter da = new OleDbDataAdapter(sorgu , baglanti);
				DataTable dt = new DataTable();
				da.Fill(dt);
				dataGridView2.DataSource=dt;

				// Kullanıcının görmesine gerek olmayan ID sütunlarını gizleyelim
				if(dataGridView2.Columns.Contains("CariDurumID"))
					dataGridView2.Columns["CariDurumID"].Visible=false;
				if(dataGridView2.Columns.Contains("CariTipID"))
					dataGridView2.Columns["CariTipID"].Visible=false;

				// Başlıkları güzelleştirelim
				dataGridView2.Columns["tc"].HeaderText="TC/VKN";
				dataGridView2.Columns["DurumAdi"].HeaderText="DURUM";
				dataGridView2.Columns["TipAdi"].HeaderText="CARİ TİPİ";
				dataGridView2.Columns["CariID"].HeaderText="ID";
				dataGridView2.Columns["adsoyad"].HeaderText="AD SOYAD";
				dataGridView2.Columns["telefon"].HeaderText="TELEFON";
				dataGridView2.Columns["adres"].HeaderText="ADRES ";
				// Görsel Hizalama (Opsiyonel ama şık durur)

				dataGridView2.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI" , 10 , FontStyle.Bold);
				CariIstatistikleriniYenile(dt);
				AnaSayfaGridleriniYenile();
				GridAramaFiltresiniUygula(CariListeAramaKutusuGetir() , dataGridView2);
				baglanti.Close();


			}
			catch(Exception ex)
			{
				CariIstatistikleriniYenile(null);
				MessageBox.Show("Listeleme Hatası: "+ex.Message);
			}

		}
		private void CariIstatistikleriniYenile ( DataTable dt )
		{
			label112.Text="Toplam Cari Sayısı";
			label110.Text="Toplam Sucu Sayısı";
			label108.Text="Toplam Fabrika Sayısı";
			label114.Text="Toplam Müşteri Sayısı";

			int toplamCari = 0;
			int sucuSayisi = 0;
			int fabrikaSayisi = 0;
			int musteriSayisi = 0;

			if(dt!=null)
			{
				toplamCari=dt.Rows.Count;
				foreach(DataRow satir in dt.Rows)
				{
					string tip = KarsilastirmaMetniHazirla(Convert.ToString(satir["TipAdi"]));
					if(string.IsNullOrWhiteSpace(tip))
						continue;

					if(tip.Contains("SUCU"))
						sucuSayisi++;
					else if(tip.Contains("FABR"))
						fabrikaSayisi++;
					else if(tip.Contains("MUST"))
						musteriSayisi++;
				}
			}

			label111.Text=toplamCari.ToString("N0" , _yazdirmaKulturu);
			label109.Text=sucuSayisi.ToString("N0" , _yazdirmaKulturu);
			label107.Text=fabrikaSayisi.ToString("N0" , _yazdirmaKulturu);
			label105.Text=musteriSayisi.ToString("N0" , _yazdirmaKulturu);
		}

		private void Temizle4 ()
		{
			// Ürünlere Toplu Zam Yap Paneli
			textBox3.Clear();           // Zam oranı kutusu
			KategoriSec.SelectedIndex=-1;
			comboBox6.SelectedIndex=-1;

			// Ürün İşlemleri Paneli (Sağ taraftaki kutular)
			// Bu isimleri projedeki kendi isimlerinle (Name özelliği) kontrol et:
			textBox83.Clear();          // ID kutusu
			comboBox8.SelectedIndex=-1;
			comboBox9.SelectedIndex=-1;// Ürün Adı kutusu
			textBox84.Text="0,00"; // Satış Fiyatı kutusu
			textBox82.Clear();    // Sağdaki Zam Oranı kutusu (48,50 yazan yer)

			dataGridView18.ClearSelection();
			// Formu tazelemek için
			this.Refresh();
		}
		//netalistemizle
		private void Temizle3 ()
		{
			textBox19.Clear();
			comboBox4.SelectedIndex=-1;
			textBox9.Clear();
			textBox16.Clear();
			textBox17.Clear();
			textBox17.Text="0,00";

		}

		//ürün temizle
		private void Temizle2 ()
		{
			// ID ve Metin Kutuları
			textBox11.Clear();
			textBox10.Clear(); // Ürün Adı
			textBox12.Clear(); // Stok Miktarı

			// Seçim Kutuları (ComboBox)
			// SelectedIndex = -1 yaparak seçimi kaldırıyoruz
			comboBox5.SelectedIndex=-1; // Kategori
			comboBox2.SelectedIndex=-1; // Marka
			comboBox3.SelectedIndex=-1; // Birim

			// Durum (CheckBox)
			checkBox2.Checked=false;

			// Varsa DataGridView seçimini de kaldıralım
			if(dataGridView1.Rows.Count>0)
			{
				dataGridView1.ClearSelection();
			}
		}
		//CARİ TEMİZLE
		private void Temizle ()
		{
			txtID.Clear();
			txtTCVKN.Clear();
			txtAdSoyad.Clear();
			txtTelefon.Clear();
			txtAdres.Clear();
			cmbCariDurum.SelectedIndex=-1;
			cmbCariTip.SelectedIndex=-1;
		}
		//cari ekle
		private void btnEkle_Click ( object sender , EventArgs e )
		{

		}

		private void btnCariSil_Click ( object sender , EventArgs e )
		{
			
		}

		private void btnCariGuncelle_Click ( object sender , EventArgs e )
		{
			
		}

		private void btnCariTemizle_Click ( object sender , EventArgs e )
		{
			Temizle();
		}

		private void dataGridView2_CellMouseEnter ( object sender , DataGridViewCellEventArgs e )
		{

		}

		private void dataGridView2_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			
		}

		private void button6_Click ( object sender , EventArgs e )
		{

		}

		private void button6_Click_1 ( object sender , EventArgs e )
		{

		}

		private void flowLayoutPanel5_Paint ( object sender , PaintEventArgs e )
		{

		}

		private void KategoriSec_SelectedIndexChanged ( object sender , EventArgs e )
		{
			MarkaListesiniKategoriyeGoreDoldur(comboBox6 , KategoriSec.SelectedValue);
		}

		private void comboBox5_SelectedIndexChanged ( object sender , EventArgs e )
		{
			MarkaListesiniKategoriyeGoreDoldur(comboBox2 , comboBox5.SelectedValue);
		}
	
		private void button9_Click ( object sender , EventArgs e )
		{
			try
			{
				string urunAdi = UrunAdiniNormallestir(textBox10.Text);
				if(string.IsNullOrWhiteSpace(urunAdi))
				{
					MessageBox.Show("Urun adini girin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
					return;
				}

				if(AyniUrunKaydiVarMi(urunAdi , comboBox5.SelectedValue , comboBox2.SelectedValue))
				{
					MessageBox.Show("Ayni urun adi, kategori ve marka birlikte zaten kayitli. Farkli kategori veya markayla kaydedebilirsiniz." , "Tekrar Eden Urun" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
					return;
				}

				textBox10.Text=urunAdi;
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// Urunler tablosuna ekleme
				string sorgu = "INSERT INTO Urunler (UrunAdi, KategoriID, MarkaID, BirimID, StokMiktari, AktifMi) VALUES (?, ?, ?, ?, ?, ?)";
				OleDbCommand komut = new OleDbCommand(sorgu , baglanti);

				komut.Parameters.AddWithValue("?" , urunAdi); // UrunAdi
				komut.Parameters.AddWithValue("?" , comboBox5.SelectedValue??DBNull.Value); // KategoriID
				komut.Parameters.AddWithValue("?" , comboBox2.SelectedValue??DBNull.Value); // MarkaID
				komut.Parameters.AddWithValue("?" , comboBox3.SelectedValue??DBNull.Value); // BirimID

				int stok = 0;
				int.TryParse(textBox12.Text , out stok);
				komut.Parameters.AddWithValue("?" , stok); // StokMiktari

				komut.Parameters.AddWithValue("?" , checkBox2.Checked); // AktifMi

				komut.ExecuteNonQuery();

				int markaId;
				int kategoriId;
				if(int.TryParse(Convert.ToString(comboBox2.SelectedValue) , out markaId) &&
				   int.TryParse(Convert.ToString(comboBox5.SelectedValue) , out kategoriId))
				{
					MarkaKategoriBagla(markaId , kategoriId);
				}


				// Yeni eklenen ürünün UrunID'sini al
				OleDbCommand cmdID = new OleDbCommand("SELECT @@IDENTITY" , baglanti);
				int yeniUrunID = Convert.ToInt32(cmdID.ExecuteScalar());

				// UrunSatisFiyat tablosuna ekleme
                string satisSorgu = "INSERT INTO UrunSatisFiyat (UrunID, CariTipiID, SatisFiyati, ZamOrani, AktifMi) VALUES (?, ?, ?, ?, ?)";
				OleDbCommand cmdSatis = new OleDbCommand(satisSorgu , baglanti);

				cmdSatis.Parameters.AddWithValue("?" , yeniUrunID); // UrunID
				cmdSatis.Parameters.AddWithValue("?" , 1);           // Varsayılan CariTipiID
				cmdSatis.Parameters.AddWithValue("?" , 0);           // Varsayılan SatisFiyati
				cmdSatis.Parameters.AddWithValue("?" , 0);           // Varsayılan ZamOrani
				cmdSatis.Parameters.AddWithValue("?" , true);        // AktifMi

				cmdSatis.ExecuteNonQuery();

				MessageBox.Show("Ürün başarıyla eklendi!" , "Bilgi" , MessageBoxButtons.OK , MessageBoxIcon.Information);

				// Ekranı güncelleme metodları
				Listele1();
				Listele3();
				Listele4();
				Temizle2();
				UrunleriYenile();

			}
			catch(Exception hata)
			{
				MessageBox.Show("Hata oluştu: "+hata.Message);
			}
			finally
			{
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}

		}

		private void button3_Click ( object sender , EventArgs e )
		{
			if(string.IsNullOrWhiteSpace(textBox11.Text))
			{
				MessageBox.Show("Lutfen tablodan bir urun secin! ID bos olamaz." , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			int urunID;
			if(!int.TryParse(textBox11.Text , out urunID))
			{
				MessageBox.Show("Gecersiz Urun ID!");
				return;
			}

			string urunAdi = UrunAdiniNormallestir(textBox10.Text);
			if(string.IsNullOrWhiteSpace(urunAdi))
			{
				MessageBox.Show("Urun adini girin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			if(AyniUrunKaydiVarMi(urunAdi , comboBox5.SelectedValue , comboBox2.SelectedValue , urunID))
			{
				MessageBox.Show("Ayni urun adi, kategori ve marka birlikte zaten kayitli. Lutfen bu urun icin farkli kategori veya marka secin." , "Tekrar Eden Urun" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}
			// 1. ID Kontrolü (Hatanın ana sebebi ID'nin boş olması olabilir)

			if(string.IsNullOrWhiteSpace(textBox11.Text))
			{
				MessageBox.Show("Lütfen tablodan bir ürün seçin! ID boş olamaz." , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				string sorgu = "UPDATE Urunler SET UrunAdi=@p1, KategoriID=@p2, MarkaID=@p3, BirimID=@p4, StokMiktari=@p5, AktifMi=@p6 WHERE UrunID=@pID";
				OleDbCommand komut = new OleDbCommand(sorgu , baglanti);

				komut.Parameters.AddWithValue("@p1" , urunAdi);
				komut.Parameters.AddWithValue("@p2" , comboBox5.SelectedValue??DBNull.Value);
				komut.Parameters.AddWithValue("@p3" , comboBox2.SelectedValue??DBNull.Value);
				komut.Parameters.AddWithValue("@p4" , comboBox3.SelectedValue??DBNull.Value);

				// Stok Miktarı boşsa 0 kabul et (Hata almamak için)
				int stok = 0;
				int.TryParse(textBox12.Text , out stok);
				komut.Parameters.AddWithValue("@p5" , stok);

				komut.Parameters.AddWithValue("@p6" , checkBox2.Checked);

				// ID değerini sayıya çevirirken hata almamak için TryParse
				komut.Parameters.AddWithValue("@pID" , urunID);

				komut.ExecuteNonQuery();

				int markaId;
				int kategoriId;
				if(int.TryParse(Convert.ToString(comboBox2.SelectedValue) , out markaId) &&
				   int.TryParse(Convert.ToString(comboBox5.SelectedValue) , out kategoriId))
				{
					MarkaKategoriBagla(markaId , kategoriId);
				}

				baglanti.Close();
				MessageBox.Show("Ürün başarıyla güncellendi." , "Başarılı" , MessageBoxButtons.OK , MessageBoxIcon.Information);
				Listele1();
				Listele3();
				Listele4();
				Temizle2();
				UrunleriYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Güncelleme Hatası: "+ex.Message);
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		private void dataGridView1_CellContentClick ( object sender , DataGridViewCellEventArgs e )
		{

		}

		private void dataGridView1_CellClick ( object sender , DataGridViewCellEventArgs e )
		{

		}

		private void dataGridView1_CellClick_1 ( object sender , DataGridViewCellEventArgs e )
		{
			// 1. Satırın geçerli olduğundan (başlık veya boş satır olmadığından) emin ol
			if(e.RowIndex>=0&&e.RowIndex<dataGridView1.Rows.Count&&!dataGridView1.Rows[e.RowIndex].IsNewRow)
			{
				try
				{
					DataGridViewRow satir = dataGridView1.Rows[e.RowIndex];

					// 2. ID ve Metin Kutularını Doldur
					// Not: Listele1 sorgunda "UrunID" sütunu mutlaka çekilmiş olmalı
					textBox11.Text=satir.Cells["UrunID"].Value?.ToString()??"";
					textBox10.Text=satir.Cells["UrunAdi"].Value?.ToString()??"";
					textBox12.Text=satir.Cells["StokMiktari"].Value?.ToString()??"";

					// 3. Kategori ComboBox (comboBox5) Seçimi
					if(satir.Cells["KategoriID"].Value!=null)
						comboBox5.SelectedValue=satir.Cells["KategoriID"].Value;

					// 4. Marka ComboBox (comboBox2) Seçimi
					if(satir.Cells["MarkaID"].Value!=null)
						comboBox2.SelectedValue=satir.Cells["MarkaID"].Value;

					// 5. Birim ComboBox (comboBox3) Seçimi
					// Burada .Text yerine .SelectedValue kullanıyoruz ki kutuda "1" yerine "ADET" yazsın
					if(satir.Cells["BirimID"].Value!=null)
						comboBox3.SelectedValue=satir.Cells["BirimID"].Value;

					// 6. Aktiflik Durumu (CheckBox)
					if(satir.Cells["AktifMi"].Value!=null)
						checkBox2.Checked=Convert.ToBoolean(satir.Cells["AktifMi"].Value);

					// Görsel geri bildirim için satırı seçili yap
					satir.Selected=true;
				}
				catch(Exception ex)
				{
					MessageBox.Show("Hücre verisi çekilirken hata oluştu: "+ex.Message);
				}
			}
		}


		private void button8_Click ( object sender , EventArgs e )
		{
			// Boş satıra tıklanırsa hata vermemesi için kontrol
			if(dataGridView1.CurrentRow==null||dataGridView1.CurrentRow.IsNewRow) return;

			try
			{
				// 1. Seçili ID'yi al (Hangi sütundaysa o ismi yaz)
				int seciliID = Convert.ToInt32(dataGridView1.CurrentRow.Cells["UrunID"].Value);

				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// 2. KRİTİK NOKTA: Sadece tek bir ID'yi sil
				// Sakın "DELETE FROM Urunler" yazma, sonuna mutlaka WHERE ekle!
				string sorgu = "DELETE FROM Urunler WHERE UrunID = ?";
				OleDbCommand komut = new OleDbCommand(sorgu , baglanti);
				komut.Parameters.AddWithValue("?" , seciliID);

				komut.ExecuteNonQuery();
				baglanti.Close();

				MessageBox.Show("Ürün başarıyla silindi.");

				// 3. TABLOYU SIFIRLAMA KODLARINI ÇALIŞTIRMA!
				// Sadece listeyi yenile, ID'ler bırak olduğu gibi kalsın.
				Listele1();
				Listele3();
				Listele4();
				UrunleriYenile();
				Temizle2();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Silme hatası: "+ex.Message);
			}
		}


		private void button2_Click ( object sender , EventArgs e )
		{
			Temizle2();
		}

		private void dataGridView1_RowPostPaint ( object sender , DataGridViewRowPostPaintEventArgs e )
		{

		}

		private void button45_Click ( object sender , EventArgs e )
		{
			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// 1. ADIM: Ürünün zaten kayıtlı olup olmadığını kontrol et
				string kontrolSorgu = "SELECT COUNT(*) FROM UrunAlis WHERE UrunID = ?";
				OleDbCommand kontrolKomut = new OleDbCommand(kontrolSorgu , baglanti);
				kontrolKomut.Parameters.AddWithValue("?" , comboBox4.SelectedValue??DBNull.Value);

				int kayitSayisi = Convert.ToInt32(kontrolKomut.ExecuteScalar());

				if(kayitSayisi>0)
				{
					MessageBox.Show("Bu ürün zaten kayıtlı!" , "Uyarı" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
					return; // İşlemi durdur, aşağıya geçme
				}

				// 2. ADIM: Eğer kayıt yoksa ekleme işlemine devam et
				string sorgu = "INSERT INTO UrunAlis (UrunID, BirimAlisFiyati, IskontoOrani, NetAlisFiyati, Tarih) VALUES (?, ?, ?, ?, ?)";
				OleDbCommand komut = new OleDbCommand(sorgu , baglanti);

				komut.Parameters.AddWithValue("?" , comboBox4.SelectedValue??DBNull.Value);
				komut.Parameters.AddWithValue("?" , double.Parse(string.IsNullOrEmpty(textBox9.Text) ? "0" : textBox9.Text));
				komut.Parameters.AddWithValue("?" , double.Parse(string.IsNullOrEmpty(textBox16.Text) ? "0" : textBox16.Text));
				komut.Parameters.AddWithValue("?" , double.Parse(string.IsNullOrEmpty(textBox17.Text) ? "0" : textBox17.Text));
				komut.Parameters.AddWithValue("?" , DateTime.Now.ToShortDateString());

				komut.ExecuteNonQuery();
				baglanti.Close();

				MessageBox.Show("Ürün alış kaydı başarıyla eklendi!" , "Başarılı");

				Listele3();
			
				UrunleriYenile();
				Temizle3();
				SatisUrunleriniAlisTablosundanGetir();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		private void textBox9_TextChanged ( object sender , EventArgs e )
		{
			HesaplaNetFiyat();
		}

		private void textBox16_TextChanged ( object sender , EventArgs e )
		{
			HesaplaNetFiyat();
		}

		// 1. Hesaplama metodun (zaten yazmışsın, textBox17 olarak güncelledim)
		// 1. ASIL HESAPLAMA METODUN
		void SatisFiyatiHesapla ()
		{
			try
			{
				double netAlis = 0;
				double zamOrani = 0;

				// Nokta / virgül karmaşasını önle
				string alisMetni = textBox17.Text.Replace("." , ",");
				string zamMetni = textBox82.Text.Replace("." , ",");

				double.TryParse(alisMetni , out netAlis);
				double.TryParse(zamMetni , out zamOrani);

				// Satış = Net Alış + (Net Alış * Zam / 100)
				double satisFiyati = netAlis+(netAlis*zamOrani/100);

				// Sonucu yaz (2 ondalık)
				textBox84.Text=satisFiyati.ToString("N2");
			}
			catch
			{
				textBox84.Text="0,00";
			}
		}

		// Bu metodu txtZamOrani ve txtNetAlisFiyati'nın TextChanged olaylarına bağla
		private void comboBox9_SelectedIndexChanged ( object sender , EventArgs e )
		{
		}

		private void button50_Click ( object sender , EventArgs e )
		{
// Önce hesaplama yap
SatisFiyatiHesapla();

			if(comboBox8.SelectedIndex==-1||comboBox9.SelectedIndex==-1)
			{
				MessageBox.Show("Ürün ve Cari Tip seçmelisiniz!");
				return;
			}

			try
			{
				if(baglanti.State==ConnectionState.Closed)
					baglanti.Open();

				int urunID = Convert.ToInt32(comboBox8.SelectedValue);
				int cariTipID = Convert.ToInt32(comboBox9.SelectedValue);

				double zamOrani = 0;
				double satisFiyati = 0;

				double.TryParse(textBox82.Text.Replace("." , ",") , out zamOrani);
				double.TryParse(textBox84.Text.Replace("." , ",") , out satisFiyati);

				// BUGÜNÜN TARİHİ
				DateTime bugun = DateTime.Today;

				// Eğer kayıt varsa UPDATE, yoksa INSERT yap
				string kontrol = "SELECT COUNT(*) FROM UrunSatisFiyat WHERE UrunID=? AND CariTipiID=?";
				OleDbCommand cmdKontrol = new OleDbCommand(kontrol , baglanti);
				cmdKontrol.Parameters.AddWithValue("?" , urunID);
				cmdKontrol.Parameters.AddWithValue("?" , cariTipID);

				int kayit = Convert.ToInt32(cmdKontrol.ExecuteScalar());

				if(kayit>0)
				{
					// UPDATE
					string guncelle = "UPDATE UrunSatisFiyat SET ZamOrani=?, SatisFiyati=?, Tarih=? WHERE UrunID=? AND CariTipiID=?";
					OleDbCommand cmdUpdate = new OleDbCommand(guncelle , baglanti);

					cmdUpdate.Parameters.AddWithValue("?" , zamOrani);
					cmdUpdate.Parameters.AddWithValue("?" , satisFiyati);
					cmdUpdate.Parameters.AddWithValue("?" , bugun);
					cmdUpdate.Parameters.AddWithValue("?" , urunID);
					cmdUpdate.Parameters.AddWithValue("?" , cariTipID);

					cmdUpdate.ExecuteNonQuery();

					MessageBox.Show("Satış fiyatı güncellendi!");
				}
				else
				{
					// INSERT
					string ekle = "INSERT INTO UrunSatisFiyat (UrunID, CariTipiID, ZamOrani, SatisFiyati, Tarih) VALUES (?, ?, ?, ?, ?)";
					OleDbCommand cmdInsert = new OleDbCommand(ekle , baglanti);

					cmdInsert.Parameters.AddWithValue("?" , urunID);
					cmdInsert.Parameters.AddWithValue("?" , cariTipID);
					cmdInsert.Parameters.AddWithValue("?" , zamOrani);
					cmdInsert.Parameters.AddWithValue("?" , satisFiyati);
					cmdInsert.Parameters.AddWithValue("?" , bugun);

					cmdInsert.ExecuteNonQuery();

					MessageBox.Show("Satış fiyatı eklendi!");
				}

				// Listeleme ve Temizlik
				Listele4();
				Temizle4();
				Temizle3();
				dataGridView18.ClearSelection();
				FormuTamamenTemizle();
				SatisUrunleriniYenile();
				textBox17.Text="0,00";
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
			}
			finally
			{
				baglanti.Close();
			}




			
		}

		private void textBox82_TextChanged ( object sender , EventArgs e )
		{
			SatisFiyatiHesapla();
		}

		private void textBox84_TextChanged ( object sender , EventArgs e )
		{

		}

		private void textBox17_TextChanged ( object sender , EventArgs e )
		{
			SatisFiyatiHesapla();
		}

		private void button49_Click ( object sender , EventArgs e )
		{// 1. KONTROL: Eğer hiçbir satır seçili değilse uyar
			if(dataGridView18.SelectedRows.Count==0&&dataGridView18.CurrentRow==null)
			{
				MessageBox.Show("Lütfen silmek istediğiniz satırı listeden seçin!" , "Hata");
				return;
			}

			// 2. ONAY ALMA
			DialogResult onay = MessageBox.Show("Bu fiyat kaydını silmek istediğinize emin misiniz?" , "Silme Onayı" , MessageBoxButtons.YesNo , MessageBoxIcon.Question);

			if(onay==DialogResult.Yes)
			{
				try
				{
					// ID'yi güvenli bir şekilde alıyoruz
					int silinecekID = Convert.ToInt32(dataGridView18.CurrentRow.Cells["UrunSatisFiyatID"].Value);

					if(baglanti.State==ConnectionState.Closed) baglanti.Open();

					string sorgu = "DELETE FROM UrunSatisFiyat WHERE UrunSatisFiyatID = ?";
					OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
					cmd.Parameters.AddWithValue("?" , silinecekID);

					cmd.ExecuteNonQuery();
					MessageBox.Show("Kayıt başarıyla silindi.");
					SatisUrunleriniYenile();

					Listele4(); // Listeyi tazele
					Temizle4();
				}
				catch(Exception ex)
				{
					MessageBox.Show("Silme hatası: "+ex.Message);
				}
				finally
				{
					if(baglanti.State==ConnectionState.Open) baglanti.Close();
				}
			}
		}



		private void dataGridView18_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			// Başlığa tıklanırsa (RowIndex -1 olur) işlem yapma
			if(e.RowIndex<0) return;

			try
			{
				// Tıklanan satırı bir değişkene alıyoruz
				DataGridViewRow satir = dataGridView18.Rows[e.RowIndex];
				textBox83.Text=satir.Cells["UrunSatisFiyatID"].Value.ToString();
				if(dataGridView18.Columns.Contains("UrunID")&&satir.Cells["UrunID"].Value!=null&&satir.Cells["UrunID"].Value!=DBNull.Value)
					comboBox8.SelectedValue=satir.Cells["UrunID"].Value;
				else
					comboBox8.Text=satir.Cells["UrunAdi"].Value.ToString();

				// Sorguda 'CT.TipAdi' seçtiğin için ismi "TipAdi"
				comboBox9.Text=satir.Cells["TipAdi"].Value.ToString();

				// Zam Oranı ve Satış Fiyatı
				textBox82.Text=satir.Cells["ZamOrani"].Value.ToString();
				textBox84.Text=satir.Cells["SatisFiyati"].Value.ToString();

				// Bonus: Güncelleme veya Silme işlemi için ID'yi bir yerde saklamak istersen:
				// labelGizliID.Text = satir.Cells["UrunSatisFiyatID"].Value.ToString();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Veri taşıma hatası: "+ex.Message);
			}
		}

		private void comboBox8_SelectedIndexChanged ( object sender , EventArgs e )
		{// Eğer seçim yapılmadıysa işlem yapma
			if(comboBox8.SelectedValue==null||comboBox8.SelectedIndex==-1) return;

			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// ÖNEMLİ: Tablo adının ve Sütun adının veritabanıyla AYNI olduğundan emin ol!
				// Eğer tablo adı Alis ise burayı "FROM Alis" yap.
				string sorgu = "SELECT TOP 1 NetAlisFiyati FROM UrunAlis WHERE UrunID = ? ORDER BY Tarih DESC, UrunAlisID DESC";

				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
				cmd.Parameters.AddWithValue("?" , comboBox8.SelectedValue);

				object sonuc = cmd.ExecuteScalar();

				if(sonuc!=null&&sonuc!=DBNull.Value)
				{
					// Fiyatı kutuya yaz (Örn: 100)
					textBox17.Text=Convert.ToDecimal(sonuc).ToString("N2" , _yazdirmaKulturu);

					// Fiyat geldiği an otomatik satışı hesapla
					SatisFiyatiHesapla();
				}
				else
				{
					textBox17.Text="0,00";
					//MessageBox.Show("Bu ürüne ait bir alış fiyatı bulunamadı!" , "Uyarı");
				}
			}
			catch(Exception ex)
			{
				// Hatanın tam yerini görmek için ex.Message kullanıyoruz
				MessageBox.Show("Hata Detayı: "+ex.Message);
			}
			finally
			{
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		private void button44_Click ( object sender , EventArgs e )
		{
			{
				// 1. KONTROL: Seçili satır var mı?
				if(dataGridView3.CurrentRow==null)
				{
					MessageBox.Show("Lütfen silmek istediğiniz alış kaydını listeden seçin!");
					return;
				}

				// 2. ONAY ALMA
				DialogResult onay = MessageBox.Show("Bu alış kaydını silmek istediğinize emin misiniz?" , "Silme Onayı" , MessageBoxButtons.YesNo , MessageBoxIcon.Question);

				if(onay==DialogResult.Yes)
				{
					try
					{
						// ID'yi alıyoruz (Sütun adının UrunAlisID olduğundan emin ol)
						int seciliID = Convert.ToInt32(dataGridView3.CurrentRow.Cells["UrunAlisID"].Value);

						if(baglanti.State==ConnectionState.Closed) baglanti.Open();

						// SQL Sorgusu
						string sorgu = "DELETE FROM UrunAlis WHERE UrunAlisID = ?";
						OleDbCommand komut = new OleDbCommand(sorgu , baglanti);
						komut.Parameters.AddWithValue("?" , seciliID);

						komut.ExecuteNonQuery();
						MessageBox.Show("Alış kaydı başarıyla silindi.");

						// Listeyi yenile ve kutuları temizle
						Listele3(); // Alış listesini yenileyen metodun
						Listele4();
						SatisUrunleriniAlisTablosundanGetir();
						Temizle3(); // Kutuları boşaltan metodun
					}
					catch(Exception ex)
					{
						MessageBox.Show("Silme hatası: "+ex.Message);
					}
					finally
					{
						if(baglanti.State==ConnectionState.Open) baglanti.Close();
					}
				}
			}
		}

		private void comboBox4_SelectedIndexChanged ( object sender , EventArgs e )
		{

		}

		private void button48_Click ( object sender , EventArgs e )
		{// ID kontrolü: textBox83 boşsa güncelleme yapamaz
			if(string.IsNullOrEmpty(textBox83.Text))
			{
				MessageBox.Show("Lütfen listeden güncellenecek bir kayıt seçin!");
				return;
			}

			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// SQL Sorgusu
                string sorgu = "UPDATE UrunSatisFiyat SET UrunID=@p1, CariTipiID=@p2, SatisFiyati=@p3, ZamOrani=@p4 WHERE UrunSatisFiyatID=@p5";

				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);

				// Parametreleri ekliyoruz (Sıralama sorgu ile aynı olmalı)
				cmd.Parameters.AddWithValue("@p1" , comboBox8.SelectedValue); // Ürün ID
				cmd.Parameters.AddWithValue("@p2" , comboBox9.SelectedValue); // Cari Tipi ID
				cmd.Parameters.AddWithValue("@p3" , Convert.ToDouble(textBox84.Text)); // Satış Fiyatı
				cmd.Parameters.AddWithValue("@p4" , Convert.ToDouble(textBox82.Text)); // Zam Oranı
                cmd.Parameters.AddWithValue("@p5" , Convert.ToInt32(textBox83.Text));  // Guncellenecek Kayit ID

				cmd.ExecuteNonQuery();
				MessageBox.Show("Satış bilgileri başarıyla güncellendi.");

				Listele4(); // Tabloyu yeniler
				Temizle4(); // Formu temizler
			}
			catch(Exception ex)
			{
				MessageBox.Show("Güncelleme sırasında hata oluştu: "+ex.Message);
			}
			finally
			{
				baglanti.Close();
			}
		}

		private void button47_Click ( object sender , EventArgs e )
		{
			
		
			// 1. BOŞ DEĞER KONTROLÜ
			if(string.IsNullOrEmpty(textBox3.Text)||KategoriSec.SelectedIndex==-1||comboBox6.SelectedIndex==-1)
			{
				MessageBox.Show("Lütfen Zam Oranı, Kategori ve Marka alanlarını doldurun!" , "Uyarı" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			try
			{
				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				// Zam oranını textBox3'ten alıyoruz
				double girilenZamOrani = Convert.ToDouble(textBox3.Text);

				// 2. SEÇİLEN ÜRÜNLERİ VE NET ALIŞ FİYATLARINI ÇEK
				string urunCekmeSorgusu = @"SELECT USF.UrunSatisFiyatID, 
                                    (SELECT TOP 1 NetAlisFiyati FROM UrunAlis WHERE UrunAlis.UrunID = USF.UrunID ORDER BY UrunAlisID DESC) as SonAlisFiyat
                                    FROM UrunSatisFiyat AS USF
                                    INNER JOIN Urunler AS U ON USF.UrunID = U.UrunID
                                    WHERE U.KategoriID = @kat AND U.MarkaID = @marka";

				OleDbCommand cekKomut = new OleDbCommand(urunCekmeSorgusu , baglanti);
				cekKomut.Parameters.AddWithValue("@kat" , KategoriSec.SelectedValue);
				cekKomut.Parameters.AddWithValue("@marka" , comboBox6.SelectedValue);

				DataTable dtUrunler = new DataTable();
				OleDbDataAdapter da = new OleDbDataAdapter(cekKomut);
				da.Fill(dtUrunler);

				if(dtUrunler.Rows.Count==0)
				{
					MessageBox.Show("Seçilen kriterlere uygun ürün bulunamadı.");
					return;
				}

				// 3. ONAY KUTUSU
				DialogResult onay = MessageBox.Show($"{dtUrunler.Rows.Count} adet ürünün fiyatı girilen değer %{girilenZamOrani} oranına göre güncellenecek. Emin misiniz?" , "Onay" , MessageBoxButtons.YesNo);
				if(onay==DialogResult.No) return;

				// 4. GÜNCELLEME DÖNGÜSÜ
				int sayac = 0;
				foreach(DataRow satir in dtUrunler.Rows)
				{
					// Eğer ürünün NetAlisFiyati girilmemişse atla
					if(satir["SonAlisFiyat"]==DBNull.Value) continue;

					int id = Convert.ToInt32(satir["UrunSatisFiyatID"]);
					double alisFiyati = Convert.ToDouble(satir["SonAlisFiyat"]);
					double yeniSatisFiyati = alisFiyati*(1+girilenZamOrani/100);

					// UPDATE sorgusunda sütun isimlerini [] içine alarak Access'i zorla
					// Hatalı olan yer: SET ZamOrani = ?, SatisFiyati = ?
					// Doğru olan:
					string guncelleSorgu = "UPDATE UrunSatisFiyat SET [ZamOrani] = ?, [SatisFiyati] = ? WHERE [UrunSatisFiyatID] = ?";

					using(OleDbCommand guncelleKomut = new OleDbCommand(guncelleSorgu , baglanti))
					{
						// PARAMETRE SIRALAMASI ÇOK ÖNEMLİ (Hata almamak için bu sırayı bozma):
						guncelleKomut.Parameters.AddWithValue("?" , girilenZamOrani); // 1. soru işareti
						guncelleKomut.Parameters.AddWithValue("?" , yeniSatisFiyati); // 2. soru işareti
						guncelleKomut.Parameters.AddWithValue("?" , id);               // 3. soru işareti (WHERE için)

						guncelleKomut.ExecuteNonQuery();
						sayac++;
					}
					
					
				}

				MessageBox.Show($"{sayac} adet ürün başarıyla güncellendi! ?");
				// TEMİZLEME İŞLEMİ BURADA BAŞLIYOR:

				
				Listele4(); // Grid'i yenile
				Temizle4();
				dataGridView18.ClearSelection(); // Mavi seçimi kaldır ki geri dolmasın
				//FormuTamamenTemizle(); // Tüm formu temizleyen metod (eğer varsa)
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
			}
			finally { baglanti.Close(); }
		}
		

		private void textBox3_KeyPress ( object sender , KeyPressEventArgs e )
		{
			// Sadece sayıları, silme tuşunu (Backspace) ve virgülü kabul et
			if(!char.IsControl(e.KeyChar)&&!char.IsDigit(e.KeyChar)&&(e.KeyChar!=','))
			{
				e.Handled=true; // Eğer basılan tuş bunlardan biri değilse, işlemi iptal et (yazma)
			}

			// Eğer zaten bir virgül varsa, ikinci bir virgülün yazılmasını engelle
			if((e.KeyChar==',')&&((sender as TextBox).Text.IndexOf(',')>-1))
			{
				e.Handled=true;
			}
		}

		private void button43_Click ( object sender , EventArgs e )
		{
			if(dataGridView3.CurrentRow==null)
			{
				MessageBox.Show("Güncellenecek satırı seçin!");
				return;
			}

			try
			{
				baglanti.Open();
				string sorgu = @"UPDATE UrunAlis 
                         SET BirimAlisFiyati = @birimFiyat,
                             IskontoOrani = @iskonto,
                             NetAlisFiyati = @netFiyat
                         WHERE UrunAlisID = @id";

				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
				cmd.Parameters.AddWithValue("@birimFiyat" , Convert.ToDouble(textBox9.Text));
				cmd.Parameters.AddWithValue("@iskonto" , Convert.ToDouble(textBox16.Text));
				cmd.Parameters.AddWithValue("@netFiyat" , Convert.ToDouble(textBox17.Text));
				cmd.Parameters.AddWithValue("@id" , Convert.ToInt32(dataGridView3.CurrentRow.Cells["UrunAlisID"].Value));


				cmd.ExecuteNonQuery();
				MessageBox.Show("Ürün alış bilgisi güncellendi!");

				// Listeyi ve TextBox’ları yenile
				Listele3();
				Listele4();
				SatisUrunleriniAlisTablosundanGetir();
				Temizle3();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
			}
			finally
			{
				baglanti.Close();
			}
		}

		private void dataGridView3_CellClick ( object sender , DataGridViewCellEventArgs e )
		{// Başlığa tıklanırsa (RowIndex -1 olur) işlem yapma
			if(e.RowIndex<0) return;

			try
			{
				// Tıklanan satırı bir değişkene alıyoruz
				DataGridViewRow satir = dataGridView3.Rows[e.RowIndex];
				textBox19.Text=satir.Cells["UrunAlisID"].Value?.ToString()??"";
				
				if(dataGridView3.Columns.Contains("UrunID")&&satir.Cells["UrunID"].Value!=null&&satir.Cells["UrunID"].Value!=DBNull.Value)
					comboBox4.SelectedValue=satir.Cells["UrunID"].Value;
				else
					comboBox4.Text=satir.Cells["UrunAdi"].Value.ToString();


				// TextBox’lara veri aktar
				textBox9.Text=satir.Cells["BirimAlisFiyati"].Value.ToString();
				textBox16.Text=satir.Cells["IskontoOrani"].Value.ToString();
				textBox17.Text=satir.Cells["NetAlisFiyati"].Value.ToString();
				
				// Bonus: Güncelleme veya Silme işlemi için ID'yi bir yerde saklamak istersen:
				// labelGizliID.Text = satir.Cells["UrunSatisFiyatID"].Value.ToString();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Veri taşıma hatası: "+ex.Message);
			}
			

			
			
		}

		private void button42_Click ( object sender , EventArgs e )
		{
			Temizle3();
		}

		private void button1_Click ( object sender , EventArgs e )
		{
			if(dataGridView1.SelectedRows.Count==0&&dataGridView1.CurrentRow==null)
			{
				MessageBox.Show("Lütfen en az bir ürün seçin!");
				return;
			}

			int cariTipID = 0;
			try
			{
				string seciliCariTip = comboCariTip.Text?.Trim()??"";
				if(string.IsNullOrWhiteSpace(seciliCariTip))
				{
					MessageBox.Show("Lütfen cari tipi seçin!");
					return;
				}

				if(baglanti.State==ConnectionState.Closed) baglanti.Open();

				string cariTipSorgu = "SELECT TOP 1 CariTipID FROM CariTipi WHERE UCASE(TipAdi)=?";
				using(OleDbCommand cmdCariTip = new OleDbCommand(cariTipSorgu , baglanti))
				{
					cmdCariTip.Parameters.AddWithValue("?" , seciliCariTip.ToUpper());
					object cariTipSonuc = cmdCariTip.ExecuteScalar();
					if(cariTipSonuc==null||cariTipSonuc==DBNull.Value)
					{
						MessageBox.Show("Seçilen cari tipi bulunamadı!");
						return;
					}
					cariTipID=Convert.ToInt32(cariTipSonuc);
				}

				List<DataGridViewRow> seciliSatirlar = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
					.Where(r => !r.IsNewRow)
					.ToList();

				if(seciliSatirlar.Count==0&&dataGridView1.CurrentRow!=null)
					seciliSatirlar.Add(dataGridView1.CurrentRow);

				int eklenenUrun = 0;
				int fiyatiOlmayanUrun = 0;
				DataGridViewRow sonIslenenSatir = null;

				foreach(DataGridViewRow seciliSatir in seciliSatirlar)
				{
					if(seciliSatir.Cells["UrunID"].Value==null) continue;

					int urunID = Convert.ToInt32(seciliSatir.Cells["UrunID"].Value);
					string urun = seciliSatir.Cells["UrunAdi"].Value?.ToString()??"";
					string marka = seciliSatir.Cells["MarkaAdi"].Value?.ToString()??"";
					string kategori = seciliSatir.Cells["KategoriAdi"].Value?.ToString()??"";
					string birim = seciliSatir.Cells["BirimAdi"].Value?.ToString()??"";
					string urunGosterim = UrunGosterimMetniGetir(urun , marka);

					string fiyatSorgu = "SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? AND CariTipiID=? ORDER BY UrunSatisFiyatID DESC";
					decimal fiyat;

					using(OleDbCommand cmdFiyat = new OleDbCommand(fiyatSorgu , baglanti))
					{
						cmdFiyat.Parameters.AddWithValue("?" , urunID);
						cmdFiyat.Parameters.AddWithValue("?" , cariTipID);
						object fiyatSonuc = cmdFiyat.ExecuteScalar();

						if(fiyatSonuc==null||fiyatSonuc==DBNull.Value)
						{
							fiyatiOlmayanUrun++;
							continue;
						}

						fiyat=Convert.ToDecimal(fiyatSonuc);
					}

					bool urunBulundu = false;
					foreach(DataGridViewRow row in dataGridView5.Rows)
					{
						if(row.IsNewRow) continue;

						bool ayniUrun = false;
						if(row.Cells["UrunID"].Value!=null&&row.Cells["UrunID"].Value!=DBNull.Value)
						{
							int mevcutId;
							if(int.TryParse(row.Cells["UrunID"].Value.ToString() , out mevcutId)&&mevcutId==urunID)
								ayniUrun=true;
						}
						if(!ayniUrun&&row.Cells["urunadi"].Value!=null&&(row.Cells["urunadi"].Value.ToString()==urun||row.Cells["urunadi"].Value.ToString()==urunGosterim))
							ayniUrun=true;

						if(ayniUrun)
						{
							decimal mevcutAdet = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
							if(mevcutAdet<=0m)
								mevcutAdet=SepetDecimalParse(Convert.ToString(row.Cells["urunadi"].Value));

							SepetUrunSatiriniDoldur(row , urunID , urun , marka , kategori , birim , mevcutAdet+1m , fiyat);
							urunBulundu=true;
							sonIslenenSatir=row;
							break;
						}
					}

					if(!urunBulundu)
						sonIslenenSatir=SepetUrunSatiriEkle(urunID , urun , marka , kategori , birim , 1m , fiyat);

					eklenenUrun++;
				}

				if(eklenenUrun==0&&fiyatiOlmayanUrun>0)
					MessageBox.Show("Seçilen cari tipine ait fiyat bulunamadığı için ürünler sepete eklenemedi.");
				else if(fiyatiOlmayanUrun>0)
					MessageBox.Show($"{eklenenUrun} ürün sepete eklendi. {fiyatiOlmayanUrun} üründe seçilen cari tipine ait fiyat bulunamadı.");

				if(sonIslenenSatir!=null)
					SepetSatirSec(sonIslenenSatir);

				SepetGenelToplamHesapla();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Sepete ekleme hatası: "+ex.Message);
			}
			finally
			{
				if(baglanti.State==ConnectionState.Open) baglanti.Close();
			}
		}

		private ComboBox MetinKutusuYerineComboBoxOlustur ( TextBox kaynakTextBox , string comboAdi )
		{
			if(kaynakTextBox==null||kaynakTextBox.Parent==null)
				return null;

			Control parent = kaynakTextBox.Parent;
			int childIndex = parent.Controls.GetChildIndex(kaynakTextBox);

			ComboBox comboBox = new ComboBox
			{
				Name=comboAdi,
				Font=kaynakTextBox.Font,
				BackColor=kaynakTextBox.BackColor,
				ForeColor=kaynakTextBox.ForeColor,
				Size=kaynakTextBox.Size,
				Margin=kaynakTextBox.Margin,
				Location=kaynakTextBox.Location,
				Anchor=kaynakTextBox.Anchor,
				Dock=kaynakTextBox.Dock,
				TabIndex=kaynakTextBox.TabIndex,
				DropDownStyle=ComboBoxStyle.DropDown,
				AutoCompleteMode=AutoCompleteMode.None,
				AutoCompleteSource=AutoCompleteSource.None,
				FormattingEnabled=true,
				IntegralHeight=false,
				MaxDropDownItems=8,
				DropDownWidth=kaynakTextBox.Width+1,
				Text=kaynakTextBox.Text
			};

			parent.SuspendLayout();
			try
			{
				parent.Controls.Add(comboBox);
				parent.Controls.SetChildIndex(comboBox , childIndex);
				kaynakTextBox.Visible=false;
				kaynakTextBox.TabStop=false;
			}
			finally
			{
				parent.ResumeLayout();
			}

			return comboBox;
		}

		private void ComboBoxVeriKaynaginiYukle<T> ( ComboBox comboBox , IEnumerable<T> kayitlar , string displayMember , string mevcutMetin )
		{
			if(comboBox==null||comboBox.IsDisposed)
				return;

			List<T> kaynakListe = kayitlar?.ToList()??new List<T>();
			comboBox.BeginUpdate();
			try
			{
				comboBox.DataSource=null;
				comboBox.Items.Clear();
				comboBox.DisplayMember=string.Empty;
				if(!string.IsNullOrWhiteSpace(displayMember))
					comboBox.DisplayMember=displayMember;

				foreach(T kayit in kaynakListe)
					comboBox.Items.Add(kayit);

				comboBox.Text=ComboBoxIcinBirebirMetinGetir(comboBox , mevcutMetin);
				ComboBoxMetinSeciminiGuvenliAyarla(comboBox);
			}
			finally
			{
				comboBox.EndUpdate();
			}
		}

		private List<T> ComboBoxIcinKayitlariFiltrele<T> ( IEnumerable<T> kayitlar , string aramaMetni , Func<T , string> metinGetir )
		{
			List<T> kaynakListe = kayitlar?.Where(kayit => kayit!=null).ToList()??new List<T>();
			if(metinGetir==null)
				return kaynakListe;

			string normalizeArama = AramaMetniniNormalizeEt(aramaMetni);
			if(string.IsNullOrWhiteSpace(normalizeArama))
				return kaynakListe;

			return kaynakListe
				.Select(kayit =>
				{
					string gosterimMetni = metinGetir(kayit)??string.Empty;
					string normalizeAday = AramaMetniniNormalizeEt(gosterimMetni);
					int baslangicIndexi = normalizeAday.IndexOf(normalizeArama , StringComparison.Ordinal);
					if(baslangicIndexi<0)
						return null;

					bool baslangicta = baslangicIndexi==0;
					bool baslangicSiniri = baslangicta||MetinAramaSiniriVarMi(normalizeAday[baslangicIndexi-1] , normalizeAday[baslangicIndexi]);
					int bitisIndexi = baslangicIndexi+normalizeArama.Length;
					bool bitisSiniri = bitisIndexi>=normalizeAday.Length||
						MetinAramaSiniriVarMi(normalizeAday[bitisIndexi-1] , normalizeAday[bitisIndexi]);

					int puan;
					if(baslangicta&&bitisSiniri)
						puan=0;
					else if(baslangicSiniri&&bitisSiniri)
						puan=1;
					else if(baslangicta)
						puan=2;
					else if(baslangicSiniri||bitisSiniri)
						puan=3;
					else
						puan=4;

					return new
					{
						Kayit=kayit,
						Puan=puan,
						Baslangic=baslangicIndexi,
						EkKarakter=Math.Max(0 , normalizeAday.Length-normalizeArama.Length),
						GosterimMetni=gosterimMetni
					};
				})
				.Where(kayit => kayit!=null)
				.OrderBy(kayit => kayit.Puan)
				.ThenBy(kayit => kayit.Baslangic)
				.ThenBy(kayit => kayit.EkKarakter)
				.ThenBy(kayit => kayit.GosterimMetni , StringComparer.CurrentCultureIgnoreCase)
				.Take(50)
				.Select(kayit => kayit.Kayit)
				.ToList();
		}

		private void ComboBoxEslesmeleriniGoster ( ComboBox comboBox , string aramaMetni )
		{
			if(comboBox==null||comboBox.IsDisposed||!comboBox.IsHandleCreated)
				return;
			if(!comboBox.Focused)
				return;

			bool listeAcikOlsun = !string.IsNullOrWhiteSpace(aramaMetni)&&comboBox.Items.Count>0;
			if(!listeAcikOlsun)
				return;

			comboBox.BeginInvoke((Action)(() =>
			{
				if(comboBox.IsDisposed||!comboBox.IsHandleCreated)
					return;

				try
				{
					if(!comboBox.Focused||comboBox.Items.Count==0)
						return;

					comboBox.DroppedDown=true;
					ComboBoxMetinSeciminiGuvenliAyarla(comboBox);
				}
				catch(ArgumentOutOfRangeException)
				{
				}
				catch(ArgumentException)
				{
				}
				catch(InvalidOperationException)
				{
				}
			}));
		}

		private int? ComboBoxSeciliTamSayiDegeriGetir ( ComboBox comboBox )
		{
			if(comboBox==null||comboBox.SelectedValue==null||comboBox.SelectedValue==DBNull.Value)
				return null;

			int deger;
			return int.TryParse(comboBox.SelectedValue.ToString() , out deger) ? deger : ( int? )null;
		}

		private List<CariAramaKaydi> CariSecimKayitlariniGetir ( int? cariTipId = null , string cariTipAdi = null )
		{
			List<CariAramaKaydi> kayitlar = new List<CariAramaKaydi>();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();

				string sorgu = @"SELECT C.[CariID],
								IIF(C.[adsoyad] IS NULL, '', C.[adsoyad]) AS AdSoyad,
								IIF(C.[tc] IS NULL, '', C.[tc]) AS Tc,
								IIF(C.[telefon] IS NULL, '', C.[telefon]) AS Telefon,
								C.[CariTipID],
								IIF(T.[TipAdi] IS NULL, '', T.[TipAdi]) AS TipAdi
							FROM [Cariler] AS C
							LEFT JOIN [CariTipi] AS T ON CLng(IIF(C.[CariTipID] IS NULL, 0, C.[CariTipID])) = T.[CariTipID]";

				sorgu+=" WHERE 1=1";
				if(cariTipId.HasValue)
					sorgu+=" AND CLng(IIF(C.[CariTipID] IS NULL, 0, C.[CariTipID])) = ?";
				else if(!string.IsNullOrWhiteSpace(cariTipAdi))
					sorgu+=" AND IIF(T.[TipAdi] IS NULL, '', T.[TipAdi]) LIKE ?";

				sorgu+=" ORDER BY IIF(C.[adsoyad] IS NULL, '', C.[adsoyad]) ASC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					if(cariTipId.HasValue)
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipId.Value;
					else if(!string.IsNullOrWhiteSpace(cariTipAdi))
						cmd.Parameters.AddWithValue("?" , "%"+cariTipAdi+"%");

					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							kayitlar.Add(new CariAramaKaydi
							{
								CariId=Convert.ToInt32(rd["CariID"]),
								AdSoyad=Convert.ToString(rd["AdSoyad"])??string.Empty,
								Tc=Convert.ToString(rd["Tc"])??string.Empty,
								Telefon=Convert.ToString(rd["Telefon"])??string.Empty,
								CariTipId=rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"]),
								TipAdi=Convert.ToString(rd["TipAdi"])??string.Empty
							});
						}
					}
				}
			}

			return kayitlar;
		}

		private List<UrunAramaKaydi> UrunSecimKayitlariniGetir ()
		{
			List<UrunAramaKaydi> kayitlar = new List<UrunAramaKaydi>();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT U.[UrunID],
								IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) AS UrunAdi,
								IIF(K.[KategoriAdi] IS NULL, '', K.[KategoriAdi]) AS KategoriAdi,
								IIF(M.[MarkaAdi] IS NULL, '', M.[MarkaAdi]) AS MarkaAdi,
								IIF(B.[BirimAdi] IS NULL, '', B.[BirimAdi]) AS BirimAdi
							FROM (([Urunler] AS U
							LEFT JOIN [Kategoriler] AS K ON U.[KategoriID] = K.[KategoriID])
							LEFT JOIN [Markalar] AS M ON U.[MarkaID] = M.[MarkaID])
							LEFT JOIN [Birimler] AS B ON U.[BirimID] = B.[BirimID]
							ORDER BY IIF(U.[UrunAdi] IS NULL, '', U.[UrunAdi]) ASC,
									 IIF(M.[MarkaAdi] IS NULL, '', M.[MarkaAdi]) ASC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						kayitlar.Add(new UrunAramaKaydi
						{
							UrunId=Convert.ToInt32(rd["UrunID"]),
							UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
							KategoriAdi=Convert.ToString(rd["KategoriAdi"])??string.Empty,
							MarkaAdi=Convert.ToString(rd["MarkaAdi"])??string.Empty,
							BirimAdi=Convert.ToString(rd["BirimAdi"])??string.Empty
						});
					}
				}
			}

			return kayitlar;
		}

		private List<YapilanIsKaydi> YapilanIsSecimKayitlariniGetir ()
		{
			List<YapilanIsKaydi> kayitlar = new List<YapilanIsKaydi>();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT
								[YapilanIsID],
								IIF([IsBilgisi] IS NULL, '', [IsBilgisi]) AS IsBilgisi,
								IIF([IsAdi] IS NULL, '', [IsAdi]) AS IsAdi,
								IIF([Birim] IS NULL OR [Birim]='', '" + VarsayilanYapilanIsBirimi + @"', [Birim]) AS Birim,
								IIF([Adet] IS NULL, 0, [Adet]) AS Adet,
								IIF([Miktar] IS NULL, 0, [Miktar]) AS Miktar,
								IIF([Fiyat] IS NULL, 0, [Fiyat]) AS Fiyat,
								IIF([ToplamFiyat] IS NULL, IIF([Miktar] IS NULL, 0, [Miktar]) * IIF([Fiyat] IS NULL, 0, [Fiyat]), [ToplamFiyat]) AS ToplamFiyat
							FROM [YapilanIsler]
							ORDER BY IIF([IsAdi] IS NULL, '', [IsAdi]) ASC";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						kayitlar.Add(new YapilanIsKaydi
						{
							YapilanIsId=Convert.ToInt32(rd["YapilanIsID"]),
							IsBilgisi=Convert.ToString(rd["IsBilgisi"])??string.Empty,
							IsAdi=Convert.ToString(rd["IsAdi"])??string.Empty,
							Birim=Convert.ToString(rd["Birim"])??VarsayilanYapilanIsBirimi,
							Adet=rd["Adet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Adet"]),
							Miktar=rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]),
							Fiyat=rd["Fiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Fiyat"]),
							ToplamFiyat=rd["ToplamFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamFiyat"])
						});
					}
				}
			}

			return kayitlar;
		}

		private void SepetAranabilirAlanlariHazirla ()
		{
			if(_sepetCariComboBox==null&&textBox24!=null)
				_sepetCariComboBox=MetinKutusuYerineComboBoxOlustur(textBox24 , "comboBoxSepetCari");
			if(_sepetUrunComboBox==null&&textBox27!=null)
				_sepetUrunComboBox=MetinKutusuYerineComboBoxOlustur(textBox27 , "comboBoxSepetUrun");
			if(_sepetYapilanIsComboBox==null&&textBox34!=null)
				_sepetYapilanIsComboBox=MetinKutusuYerineComboBoxOlustur(textBox34 , "comboBoxSepetYapilanIs");
		}

		private void SepetDetayGorunumunuUrunIslemleriyleEsitle ()
		{
			Font kontrolFont = comboBox8?.Font??new Font("Microsoft Sans Serif" , 10.2F , FontStyle.Regular , GraphicsUnit.Point , 162);
			Size comboBoyutu = comboBox8?.Size??new Size(259 , 28);
			Padding comboMargin = comboBox8?.Margin??new Padding(3);

			foreach(ComboBox combo in new[] { _sepetCariComboBox, _sepetUrunComboBox, _sepetYapilanIsComboBox })
			{
				if(combo==null)
					continue;

				combo.Font=kontrolFont;
				combo.Size=comboBoyutu;
				combo.Margin=comboMargin;
				combo.DropDownWidth=combo==_sepetUrunComboBox ? Math.Max(comboBoyutu.Width , 420) : comboBoyutu.Width;
				combo.MaxDropDownItems=combo==_sepetUrunComboBox ? 14 : 8;
			}

			label38.Text="ÜRÜN ARA :";
			label30.Text="YAPILAN İŞ :";
			label42.Text="İŞ BİLGİSİ :";
			label43.Text="ADET :";
			label44.Text="SATIŞ FİYATI :";
			label36.Text="SEPETE EKLE";
			if(dataGridView5?.Columns.Contains("urunadi")==true)
				dataGridView5.Columns["urunadi"].HeaderText="ÜRÜN ADI";
			if(dataGridView5?.Columns.Contains("adet")==true)
				dataGridView5.Columns["adet"].HeaderText="MİKTAR";
			if(dataGridView5?.Columns.Contains("toplamfiyat")==true)
				dataGridView5.Columns["toplamfiyat"].HeaderText="TOPLAM FİYATI";
			if(textBox35!=null)
			{
				textBox35.ReadOnly=true;
				textBox35.BackColor=SystemColors.ControlLight;
			}
			if(textBox36!=null)
			{
				textBox36.Text="1";
				textBox36.ReadOnly=true;
				textBox36.BackColor=SystemColors.ControlLight;
				textBox36.Visible=false;
				textBox36.Enabled=false;
				textBox36.TabStop=false;
			}
			if(textBox4!=null)
			{
				textBox4.Text="0,00";
				textBox4.ReadOnly=true;
				textBox4.BackColor=SystemColors.ControlLight;
				textBox4.Visible=false;
				textBox4.Enabled=false;
				textBox4.TabStop=false;
			}
			if(label43!=null)
				label43.Visible=false;
			if(label44!=null)
				label44.Visible=false;
			if(textBox32!=null)
			{
				textBox32.ReadOnly=true;
				textBox32.BackColor=SystemColors.ControlLight;
			}
		}

		private void SepetCariSecimleriniYenile ()
		{
			if(_sepetCariComboBox==null)
				return;

			string mevcutMetin = SepetCariUyariMetniAktifMi() ? string.Empty : SepetCariGirisMetniGetir();
			Color mevcutRenk = _sepetCariComboBox.ForeColor;
			_sepetCariDolduruluyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					_sepetCariComboBox ,
					ComboBoxIcinKayitlariFiltrele(
						CariSecimKayitlariniGetir() ,
						mevcutMetin ,
						kayit => kayit.CariGosterimDetayi) ,
					nameof(CariAramaKaydi.CariGosterimDetayi) ,
					mevcutMetin);
				_sepetCariComboBox.ForeColor=mevcutRenk;
			}
			finally
			{
				_sepetCariDolduruluyor=false;
			}
		}

		private void SepetUrunSecimleriniYenile ()
		{
			if(_sepetUrunComboBox==null)
				return;

			string mevcutMetin = _sepetUrunComboBox.Text;
			_sepetUrunDolduruluyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					_sepetUrunComboBox ,
					ComboBoxIcinKayitlariFiltrele(
						UrunSecimKayitlariniGetir() ,
						mevcutMetin ,
						kayit => kayit.UrunGosterimAdi) ,
					nameof(UrunAramaKaydi.UrunGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_sepetUrunDolduruluyor=false;
			}
		}

		private void SepetYapilanIsSecimleriniYenile ()
		{
			if(_sepetYapilanIsComboBox==null)
				return;

			string mevcutMetin = _sepetYapilanIsComboBox.Text;
			_sepetYapilanIsDolduruluyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					_sepetYapilanIsComboBox ,
					ComboBoxIcinKayitlariFiltrele(YapilanIsSecimKayitlariniGetir() , mevcutMetin , kayit => kayit.KalemGosterimAdi) ,
					nameof(YapilanIsKaydi.KalemGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_sepetYapilanIsDolduruluyor=false;
			}
		}

		private string SepetCariGirisMetniGetir ()
		{
			if(_sepetCariComboBox!=null)
				return _sepetCariComboBox.Text?.Trim()??string.Empty;

			return textBox24?.Text?.Trim()??string.Empty;
		}

		private void SepetCariGirisMetniniAyarla ( string metin , Color? yaziRengi = null )
		{
			if(_sepetCariComboBox!=null)
			{
				_sepetCariComboBox.Text=metin??string.Empty;
				if(yaziRengi.HasValue)
					_sepetCariComboBox.ForeColor=yaziRengi.Value;
				return;
			}

			if(textBox24!=null)
			{
				textBox24.Text=metin??string.Empty;
				if(yaziRengi.HasValue)
					textBox24.ForeColor=yaziRengi.Value;
			}
		}

		private void SepetCariGirisTemizle ()
		{
			SepetCariGirisMetniniAyarla(string.Empty , SystemColors.WindowText);
		}

		private string SepetUrunGirisMetniGetir ()
		{
			if(_sepetUrunComboBox!=null)
				return _sepetUrunComboBox.Text?.Trim()??string.Empty;

			return textBox27?.Text?.Trim()??string.Empty;
		}

		private void SepetUrunGirisMetniniAyarla ( string metin )
		{
			if(_sepetUrunComboBox!=null)
			{
				_sepetUrunComboBox.Text=metin??string.Empty;
				return;
			}

			if(textBox27!=null)
				textBox27.Text=metin??string.Empty;
		}

		private void SepetUrunGirisTemizle ()
		{
			SepetUrunGirisMetniniAyarla(string.Empty);
		}

		private void SepetUrunSeciminiTemizle ()
		{
			_sepetUrunId=null;
			_sepetMarka=null;
			_sepetKategori=null;
			_sepetBirim=null;

			if(textBox28!=null)
				textBox28.Clear();
			if(textBox30!=null)
				textBox30.Text="0,00";

			SepetSatirToplamHesapla();
		}

		private bool SepetUrunMetniKaydaTamEslesiyorMu ( string arama , UrunAramaKaydi urunKaydi )
		{
			if(urunKaydi==null)
				return false;

			string temizArama = KarsilastirmaMetniHazirla(arama);
			if(string.IsNullOrWhiteSpace(temizArama))
				return false;

			return string.Equals(temizArama , KarsilastirmaMetniHazirla(urunKaydi.UrunAdi) , StringComparison.Ordinal)||
				string.Equals(temizArama , KarsilastirmaMetniHazirla(urunKaydi.UrunGosterimAdi) , StringComparison.Ordinal);
		}

		private void SepetUrunKaydiniUygula ( UrunAramaKaydi urunKaydi , bool metniGuncelle )
		{
			if(urunKaydi==null)
			{
				SepetUrunSeciminiTemizle();
				return;
			}

			_sepetUrunId=urunKaydi.UrunId;
			_sepetKategori=urunKaydi.KategoriAdi;
			_sepetMarka=urunKaydi.MarkaAdi;
			_sepetBirim=urunKaydi.BirimAdi;
			SepetYapilanIsSeciminiTemizle(true);

			_sepetUrunDolduruluyor=true;
			try
			{
				if(metniGuncelle)
					SepetUrunGirisMetniniAyarla(urunKaydi.UrunGosterimAdi);
				if(textBox28!=null)
					textBox28.Text=_sepetBirim;
				if(textBox29!=null)
					textBox29.Text="1";
			}
			finally
			{
				_sepetUrunDolduruluyor=false;
			}

			SepetUrunFiyatGuncelle();
		}

		private bool SepetUrunComboSeciminiUygula ( bool metniGuncelle )
		{
			if(_sepetUrunComboBox==null)
				return false;

			UrunAramaKaydi urunKaydi = _sepetUrunComboBox.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null&&_sepetUrunComboBox.Items.Count>0)
				urunKaydi=_sepetUrunComboBox.Items[0] as UrunAramaKaydi;
			if(urunKaydi==null)
				return false;

			SepetUrunKaydiniUygula(urunKaydi , metniGuncelle);
			return true;
		}

		private void SepetYapilanIsSeciminiTemizle ( bool alanMetniniTemizle )
		{
			_sepetYapilanIsId=null;
			if(alanMetniniTemizle)
			{
				_sepetYapilanIsDolduruluyor=true;
				try
				{
					if(_sepetYapilanIsComboBox!=null)
						_sepetYapilanIsComboBox.Text=string.Empty;
				}
				finally
				{
					_sepetYapilanIsDolduruluyor=false;
				}
			}

			if(textBox35!=null) textBox35.Clear();
			if(textBox36!=null) textBox36.Text="1";
			if(textBox4!=null) textBox4.Text="0,00";
		}

		private void SepetYapilanIsBilgileriniDoldur ( YapilanIsKaydi kayit , bool metniAyarla )
		{
			if(kayit==null)
			{
				SepetYapilanIsSeciminiTemizle(metniAyarla);
				return;
			}

			_sepetYapilanIsId=kayit.YapilanIsId;
			if(metniAyarla&&_sepetYapilanIsComboBox!=null)
			{
				_sepetYapilanIsDolduruluyor=true;
				try
				{
					_sepetYapilanIsComboBox.Text=kayit.KalemGosterimAdi;
				}
				finally
				{
					_sepetYapilanIsDolduruluyor=false;
				}
			}

			if(textBox35!=null)
				textBox35.Text=YapilanIsTanimiMetniGetir(kayit);
			if(textBox36!=null)
				textBox36.Text=kayit.Adet<=0 ? "1" : kayit.Adet.ToString("0.##" , _yazdirmaKulturu);
			if(textBox4!=null)
				textBox4.Text=kayit.Fiyat.ToString("N2" , _yazdirmaKulturu);
			if(textBox28!=null)
				textBox28.Text=string.IsNullOrWhiteSpace(kayit.Birim) ? VarsayilanYapilanIsBirimi : kayit.Birim;
			if(textBox29!=null)
				textBox29.Text=(kayit.Miktar<=0 ? 1m : kayit.Miktar).ToString("0.##" , _yazdirmaKulturu);
			if(textBox30!=null)
				textBox30.Text=kayit.Fiyat.ToString("N2" , _yazdirmaKulturu);
			if(textBox32!=null)
			{
				decimal toplamFiyat = kayit.ToplamFiyat>0
					? kayit.ToplamFiyat
					: ( kayit.Miktar<=0 ? 1m : kayit.Miktar ) *kayit.Fiyat;
				textBox32.Text=toplamFiyat.ToString("N2" , _yazdirmaKulturu);
			}
			SepetSatirToplamHesapla();
		}

		private void BelgeYapilanIsSeciminiTemizle ( BelgePaneli panel , bool alanMetniniTemizle )
		{
			if(panel==null)
				return;

			panel.SeciliYapilanIsId=null;
			if(alanMetniniTemizle&&panel.YapilanIsComboBox!=null)
				panel.YapilanIsComboBox.Text=string.Empty;
			if(panel.YapilanIsBilgiTextBox!=null)
				panel.YapilanIsBilgiTextBox.Clear();
			if(panel.YapilanIsAdetTextBox!=null)
				panel.YapilanIsAdetTextBox.Text="1";
			if(panel.YapilanIsFiyatTextBox!=null)
				panel.YapilanIsFiyatTextBox.Text="0,00";
		}

		private void BelgeYapilanIsBilgileriniDoldur ( BelgePaneli panel , YapilanIsKaydi kayit , bool metniAyarla )
		{
			if(panel==null)
				return;

			if(kayit==null)
			{
				BelgeYapilanIsSeciminiTemizle(panel , metniAyarla);
				return;
			}

			panel.SeciliYapilanIsId=kayit.YapilanIsId;
			_belgeAlanlariGuncelleniyor=true;
			try
			{
				if(metniAyarla&&panel.YapilanIsComboBox!=null)
					panel.YapilanIsComboBox.Text=kayit.KalemGosterimAdi;
				if(panel.YapilanIsBilgiTextBox!=null)
					panel.YapilanIsBilgiTextBox.Text=YapilanIsTanimiMetniGetir(kayit);
				if(panel.YapilanIsAdetTextBox!=null)
					panel.YapilanIsAdetTextBox.Text=kayit.Adet<=0 ? "1" : kayit.Adet.ToString("0.##" , _yazdirmaKulturu);
				if(panel.YapilanIsFiyatTextBox!=null)
					panel.YapilanIsFiyatTextBox.Text=kayit.Fiyat.ToString("N2" , _yazdirmaKulturu);
				if(panel.BirimTextBox!=null)
					panel.BirimTextBox.Text=string.IsNullOrWhiteSpace(kayit.Birim) ? VarsayilanYapilanIsBirimi : kayit.Birim;
				if(panel.MiktarTextBox!=null)
					panel.MiktarTextBox.Text=(kayit.Miktar<=0 ? 1m : kayit.Miktar).ToString("0.##" , _yazdirmaKulturu);
				if(panel.BirimFiyatTextBox!=null)
					panel.BirimFiyatTextBox.Text=kayit.Fiyat.ToString("N2" , _yazdirmaKulturu);
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}

			BelgeToplamKutusuGuncelle(panel);
		}

		private void BelgeAranabilirAlanlariHazirla ( BelgePaneli panel )
		{
			if(panel==null)
				return;

			if(panel.CariAdComboBox==null&&panel.CariAdTextBox!=null)
				panel.CariAdComboBox=MetinKutusuYerineComboBoxOlustur(panel.CariAdTextBox , panel.CariAdTextBox.Name+"_Combo");
			if(panel.UrunAdiComboBox==null&&panel.UrunAdiTextBox!=null)
				panel.UrunAdiComboBox=MetinKutusuYerineComboBoxOlustur(panel.UrunAdiTextBox , panel.UrunAdiTextBox.Name+"_Combo");
			if(panel.YapilanIsComboBox==null&&panel.ArizaTextBoxlari!=null&&panel.ArizaTextBoxlari.Length>0)
				panel.YapilanIsComboBox=MetinKutusuYerineComboBoxOlustur(panel.ArizaTextBoxlari[0] , panel.ArizaTextBoxlari[0].Name+"_Combo");

			if(panel.UrunAdiComboBox!=null)
			{
				panel.UrunAdiComboBox.DropDownWidth=Math.Max(panel.UrunAdiComboBox.Width , 420);
				panel.UrunAdiComboBox.MaxDropDownItems=14;
			}
		}

		private void BelgeCariSecimleriniYenile ( BelgePaneli panel )
		{
			if(panel?.CariAdComboBox==null)
				return;

			string mevcutMetin = panel.CariAdComboBox.Text;
			_belgeAlanlariGuncelleniyor=true;
			try
			{
				IEnumerable<CariAramaKaydi> kayitlar = panel.TeklifMi
					? CariSecimKayitlariniGetir()
					: CariSecimKayitlariniGetir(panel.CariTipId , panel.CariTipId.HasValue ? null : panel.CariTipAdi);
				Func<CariAramaKaydi, string> gosterimMetniGetir = panel.TeklifMi
					? new Func<CariAramaKaydi, string>(kayit => kayit.CariGosterimDetayi)
					: kayit => kayit.CariGosterimAdi;
				string displayMember = panel.TeklifMi
					? nameof(CariAramaKaydi.CariGosterimDetayi)
					: nameof(CariAramaKaydi.CariGosterimAdi);
				ComboBoxVeriKaynaginiYukle(
					panel.CariAdComboBox ,
					ComboBoxIcinKayitlariFiltrele(kayitlar , mevcutMetin , gosterimMetniGetir) ,
					displayMember ,
					mevcutMetin);
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private void BelgeUrunSecimleriniYenile ( BelgePaneli panel )
		{
			if(panel?.UrunAdiComboBox==null)
				return;

			string mevcutMetin = panel.UrunAdiComboBox.Text;
			_belgeAlanlariGuncelleniyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					panel.UrunAdiComboBox ,
					ComboBoxIcinKayitlariFiltrele(UrunSecimKayitlariniGetir() , mevcutMetin , kayit => kayit.UrunGosterimAdi) ,
					nameof(UrunAramaKaydi.UrunGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private bool BelgeUrunMetniKaydaTamEslesiyorMu ( string arama , UrunAramaKaydi urunKaydi )
		{
			if(urunKaydi==null)
				return false;

			string temizArama = KarsilastirmaMetniHazirla(arama);
			if(string.IsNullOrWhiteSpace(temizArama))
				return false;

			return string.Equals(temizArama , KarsilastirmaMetniHazirla(urunKaydi.UrunAdi) , StringComparison.Ordinal)||
				string.Equals(temizArama , KarsilastirmaMetniHazirla(urunKaydi.UrunGosterimAdi) , StringComparison.Ordinal);
		}

		private decimal BelgeUrunFiyatiniGetir ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int urunId )
		{
			if(conn==null||panel==null||urunId<=0)
				return 0m;

			int? cariTipId = panel.CariTipId;
			if(!cariTipId.HasValue&&panel.SeciliCariId.HasValue)
				cariTipId=CariyeAitCariTipIdGetir(conn , tx , panel.SeciliCariId.Value);

			string sorgu = cariTipId.HasValue
				? "SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? AND CLng(IIF(CariTipiID IS NULL, 0, CariTipiID))=? ORDER BY UrunSatisFiyatID DESC"
				: "SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? ORDER BY UrunSatisFiyatID DESC";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				if(cariTipId.HasValue)
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipId.Value;

				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
			}
		}

		private void BelgeUrunKaydiniUygula ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , UrunAramaKaydi urunKaydi , bool metniGuncelle )
		{
			if(panel==null||conn==null)
				return;

			if(urunKaydi==null)
			{
				BelgeUrunSeciminiTemizle(panel);
				BelgeToplamKutusuGuncelle(panel);
				return;
			}

			decimal fiyat = BelgeUrunFiyatiniGetir(conn , tx , panel , urunKaydi.UrunId);

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				BelgeYapilanIsSeciminiTemizle(panel , true);
				if(metniGuncelle)
					BelgeUrunMetniniAyarla(panel , urunKaydi.UrunGosterimAdi);
				if(panel.BirimTextBox!=null)
					panel.BirimTextBox.Text=urunKaydi.BirimAdi;
				if(panel.MiktarTextBox!=null&&SepetDecimalParse(panel.MiktarTextBox.Text)<=0)
					panel.MiktarTextBox.Text="1";
				if(panel.BirimFiyatTextBox!=null)
					panel.BirimFiyatTextBox.Text=fiyat.ToString("N2" , _yazdirmaKulturu);
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}

			BelgeToplamKutusuGuncelle(panel);
		}

		private bool BelgeUrunComboSeciminiUygula ( BelgePaneli panel , bool metniGuncelle )
		{
			if(panel?.UrunAdiComboBox==null)
				return false;

			UrunAramaKaydi urunKaydi = panel.UrunAdiComboBox.SelectedItem as UrunAramaKaydi;
			if(urunKaydi==null&&panel.UrunAdiComboBox.Items.Count>0)
				urunKaydi=panel.UrunAdiComboBox.Items[0] as UrunAramaKaydi;
			if(urunKaydi==null)
				return false;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				BelgeUrunKaydiniUygula(conn , null , panel , urunKaydi , metniGuncelle);
			}

			return true;
		}

		private void BelgeYapilanIsSecimleriniYenile ( BelgePaneli panel )
		{
			if(panel?.YapilanIsComboBox==null)
				return;

			string mevcutMetin = panel.YapilanIsComboBox.Text;
			_belgeAlanlariGuncelleniyor=true;
			try
			{
				ComboBoxVeriKaynaginiYukle(
					panel.YapilanIsComboBox ,
					ComboBoxIcinKayitlariFiltrele(YapilanIsSecimKayitlariniGetir() , mevcutMetin , kayit => kayit.KalemGosterimAdi) ,
					nameof(YapilanIsKaydi.KalemGosterimAdi) ,
					mevcutMetin);
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private string BelgeCariMetniGetir ( BelgePaneli panel )
		{
			if(panel?.CariAdComboBox!=null)
				return CariAramaMetniniTemizle(panel.CariAdComboBox.Text);

			return CariAramaMetniniTemizle(panel?.CariAdTextBox?.Text);
		}

		private void BelgeCariMetniniAyarla ( BelgePaneli panel , string metin )
		{
			if(panel?.CariAdComboBox!=null)
			{
				panel.CariAdComboBox.Text=metin??string.Empty;
				return;
			}

			if(panel?.CariAdTextBox!=null)
				panel.CariAdTextBox.Text=metin??string.Empty;
		}

		private string BelgeUrunMetniGetir ( BelgePaneli panel )
		{
			if(panel?.UrunAdiComboBox!=null)
				return panel.UrunAdiComboBox.Text?.Trim()??string.Empty;

			return panel?.UrunAdiTextBox?.Text?.Trim()??string.Empty;
		}

		private void BelgeUrunMetniniAyarla ( BelgePaneli panel , string metin )
		{
			if(panel?.UrunAdiComboBox!=null)
			{
				panel.UrunAdiComboBox.Text=metin??string.Empty;
				return;
			}

			if(panel?.UrunAdiTextBox!=null)
				panel.UrunAdiTextBox.Text=metin??string.Empty;
		}

		private void BelgeUrunMetniniTemizle ( BelgePaneli panel )
		{
			BelgeUrunMetniniAyarla(panel , string.Empty);
		}

		private void BelgeUrunSeciminiTemizle ( BelgePaneli panel )
		{
			if(panel==null)
				return;

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				if(panel.BirimTextBox!=null)
					panel.BirimTextBox.Clear();
				if(panel.BirimFiyatTextBox!=null)
					panel.BirimFiyatTextBox.Text="0,00";
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private void SepetCariComboBox_DropDown ( object sender , EventArgs e )
		{
			SepetCariSecimleriniYenile();
		}

		private void SepetUrunComboBox_DropDown ( object sender , EventArgs e )
		{
			SepetUrunSecimleriniYenile();
		}

		private void SepetUrunComboBox_SelectionChangeCommitted ( object sender , EventArgs e )
		{
			SepetUrunComboSeciminiUygula(true);
		}

		private void SepetUrunComboBox_KeyDown ( object sender , KeyEventArgs e )
		{
			if(_sepetUrunComboBox==null)
				return;

			if(e.KeyCode==Keys.Down&&!_sepetUrunComboBox.DroppedDown&&_sepetUrunComboBox.Items.Count>0)
			{
				_sepetUrunComboBox.DroppedDown=true;
				e.Handled=true;
				return;
			}

			if(( e.KeyCode==Keys.Enter||e.KeyCode==Keys.Tab )&&_sepetUrunComboBox.Items.Count>0)
			{
				if(SepetUrunComboSeciminiUygula(true))
				{
					if(e.KeyCode==Keys.Enter)
					{
						e.SuppressKeyPress=true;
						e.Handled=true;
					}
				}
			}
		}

		private void BelgeCariComboBox_DropDown ( object sender , EventArgs e )
		{
			BelgeCariSecimleriniYenile(BelgePaneliniGetir(sender));
		}

		private void BelgeUrunComboBox_DropDown ( object sender , EventArgs e )
		{
			BelgeUrunSecimleriniYenile(BelgePaneliniGetir(sender));
		}

		private void BelgeUrunComboBox_SelectionChangeCommitted ( object sender , EventArgs e )
		{
			BelgeUrunComboSeciminiUygula(BelgePaneliniGetir(sender) , true);
		}

		private void BelgeUrunComboBox_KeyDown ( object sender , KeyEventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			ComboBox comboBox = panel?.UrunAdiComboBox;
			if(comboBox==null)
				return;

			if(e.KeyCode==Keys.Down&&!comboBox.DroppedDown&&comboBox.Items.Count>0)
			{
				comboBox.DroppedDown=true;
				e.Handled=true;
				return;
			}

			if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Tab)
			{
				if(BelgeUrunComboSeciminiUygula(panel , true)&&e.KeyCode==Keys.Enter)
				{
					e.SuppressKeyPress=true;
					e.Handled=true;
				}
			}
		}

		private void BelgeYapilanIsComboBox_DropDown ( object sender , EventArgs e )
		{
			BelgeYapilanIsSecimleriniYenile(BelgePaneliniGetir(sender));
		}

		private void SepetYapilanIsComboBox_DropDown ( object sender , EventArgs e )
		{
			SepetYapilanIsSecimleriniYenile();
		}

		private void SepetBaslangicAyarla ()
		{
			SepetAranabilirAlanlariHazirla();
			SepetDetayGorunumunuUrunIslemleriyleEsitle();

			// Olay bağlama
			if(_sepetCariComboBox!=null)
			{
				_sepetCariComboBox.TextChanged-=SepetCariAra_TextChanged;
				_sepetCariComboBox.TextChanged+=SepetCariAra_TextChanged;
				_sepetCariComboBox.Enter-=SepetCariTextBox_Enter;
				_sepetCariComboBox.Enter+=SepetCariTextBox_Enter;
				_sepetCariComboBox.Click-=SepetCariTextBox_Click;
				_sepetCariComboBox.Click+=SepetCariTextBox_Click;
				_sepetCariComboBox.Leave-=SepetCariTextBox_Leave;
				_sepetCariComboBox.Leave+=SepetCariTextBox_Leave;
				_sepetCariComboBox.DropDown-=SepetCariComboBox_DropDown;
				_sepetCariComboBox.DropDown+=SepetCariComboBox_DropDown;
			}
			if(textBox25!=null)
			{
				textBox25.ReadOnly=true;
				textBox25.TabStop=false;
			}
			if(textBox26!=null)
			{
				textBox26.ReadOnly=true;
				textBox26.TabStop=false;
			}
			if(_sepetUrunComboBox!=null)
			{
				_sepetUrunComboBox.TextChanged-=SepetUrunAra_TextChanged;
				_sepetUrunComboBox.TextChanged+=SepetUrunAra_TextChanged;
				_sepetUrunComboBox.DropDown-=SepetUrunComboBox_DropDown;
				_sepetUrunComboBox.DropDown+=SepetUrunComboBox_DropDown;
				_sepetUrunComboBox.SelectionChangeCommitted-=SepetUrunComboBox_SelectionChangeCommitted;
				_sepetUrunComboBox.SelectionChangeCommitted+=SepetUrunComboBox_SelectionChangeCommitted;
				_sepetUrunComboBox.KeyDown-=SepetUrunComboBox_KeyDown;
				_sepetUrunComboBox.KeyDown+=SepetUrunComboBox_KeyDown;
			}
			if(_sepetYapilanIsComboBox!=null)
			{
				_sepetYapilanIsComboBox.TextChanged-=SepetYapilanIs_TextChanged;
				_sepetYapilanIsComboBox.TextChanged+=SepetYapilanIs_TextChanged;
				_sepetYapilanIsComboBox.DropDown-=SepetYapilanIsComboBox_DropDown;
				_sepetYapilanIsComboBox.DropDown+=SepetYapilanIsComboBox_DropDown;
			}
			if(textBox28!=null)
			{
				textBox28.TextChanged-=SepetAdet_TextChanged;
				textBox28.KeyPress-=SepetSayisal_KeyPress;
				textBox28.ReadOnly=true;
				textBox28.TabStop=false;
			}
			if(textBox29!=null)
			{
				textBox29.TextChanged-=SepetAdet_TextChanged;
				textBox29.TextChanged+=SepetAdet_TextChanged;
				textBox29.KeyPress-=SepetSayisal_KeyPress;
				textBox29.KeyPress+=SepetSayisal_KeyPress;
			}
			if(textBox30!=null)
			{
				textBox30.TextChanged-=SepetFiyat_TextChanged;
				textBox30.TextChanged+=SepetFiyat_TextChanged;
				textBox30.KeyPress-=SepetSayisal_KeyPress;
				textBox30.KeyPress+=SepetSayisal_KeyPress;
			}
			if(textBox39!=null)
			{
				textBox39.TextChanged-=SepetKdv_TextChanged;
				textBox39.TextChanged+=SepetKdv_TextChanged;
				textBox39.KeyPress-=SepetSayisal_KeyPress;
				textBox39.KeyPress+=SepetSayisal_KeyPress;
			}

			if(comboCariTip!=null)
			{
				comboCariTip.SelectedIndexChanged-=SepetCariTipDegisti;
				comboCariTip.SelectedIndexChanged+=SepetCariTipDegisti;
			}

			SepetCariSecimleriniYenile();
			SepetUrunSecimleriniYenile();
			SepetYapilanIsSecimleriniYenile();

			if(dataGridView5!=null)
			{
				dataGridView5.CellClick-=DataGridView5_CellClick;
				dataGridView5.CellClick+=DataGridView5_CellClick;
				dataGridView5.CellValueChanged-=DataGridView5_CellValueChanged;
				dataGridView5.CellValueChanged+=DataGridView5_CellValueChanged;
				dataGridView5.RowsAdded-=DataGridView5_RowsAdded;
				dataGridView5.RowsAdded+=DataGridView5_RowsAdded;
				dataGridView5.RowsRemoved-=DataGridView5_RowsRemoved;
				dataGridView5.RowsRemoved+=DataGridView5_RowsRemoved;
				dataGridView5.CurrentCellDirtyStateChanged-=DataGridView5_CurrentCellDirtyStateChanged;
				dataGridView5.CurrentCellDirtyStateChanged+=DataGridView5_CurrentCellDirtyStateChanged;
			}

			if(button16!=null)
			{
				button16.Click-=SepetUrunEkle_Click;
				button16.Click+=SepetUrunEkle_Click;
			}
			if(button14!=null)
			{
				button14.Click-=SepetSeciliSatirSil_Click;
				button14.Click+=SepetSeciliSatirSil_Click;
			}
			if(button15!=null)
			{
				button15.Click-=SepetTemizle_Click;
				button15.Click+=SepetTemizle_Click;
			}
			if(button12!=null)
			{
				button12.Click-=SepetKaydet_Click;
				button12.Click+=SepetKaydet_Click;
			}
			if(button13!=null)
			{
				button13.Click-=SepetYazdirButonu_Click;
				button13.Click+=SepetYazdirButonu_Click;
			}
			if(button17!=null)
			{
				button17.Click-=SepetPdfButonu_Click;
				button17.Click+=SepetPdfButonu_Click;
			}
			if(button18!=null)
			{
				button18.Click-=SepetExcelButonu_Click;
				button18.Click+=SepetExcelButonu_Click;
			}

			// Varsayılan değerler
			if(textBox29!=null&&!_sepetUrunDolduruluyor)
				textBox29.Text="1";
			if(textBox28!=null&&!_sepetUrunDolduruluyor)
				textBox28.Clear();
			if(textBox39!=null)
				textBox39.Text="0,00";

			if(label46!=null)
				label46.Text="0,00";
			if(label48!=null)
				label48.Text="0,00";

			SepetCariUyariMetniniGoster();
		}

		private void SepetCariTipDegisti ( object sender , EventArgs e )
		{
			// Cari tipi değişince seçili ürünün fiyatını güncelle
			if(_sepetUrunId.HasValue)
				SepetUrunFiyatGuncelle();
			SepetCariSecimleriniYenile();
		}

		private bool SepetCariUyariMetniAktifMi ()
		{
			string metin = KarsilastirmaMetniHazirla(SepetCariGirisMetniGetir());
			if(string.IsNullOrWhiteSpace(metin))
				return false;

			return metin==KarsilastirmaMetniHazirla(SepetCariUyariMetni)
				||metin=="TEKLIFSE CARI BILGISI GIRMEYINIZ!."
				||metin=="FATURA DEGILSE CARI BILGISI GIRMEYINIZ";
		}

		private string SepetCariAdMetniGetir ()
		{
			string metin = SepetCariGirisMetniGetir();
			return SepetCariUyariMetniAktifMi() ? string.Empty : CariAramaMetniniTemizle(metin);
		}

		private void SepetCariUyariMetniniGoster ()
		{
			if(_sepetCariComboBox==null&&textBox24==null)
				return;
			if(!string.IsNullOrWhiteSpace(SepetCariGirisMetniGetir())&&!SepetCariUyariMetniAktifMi())
				return;

			_sepetCariDolduruluyor=true;
			try
			{
				SepetCariGirisMetniniAyarla(SepetCariUyariMetni , SystemColors.GrayText);
			}
			finally
			{
				_sepetCariDolduruluyor=false;
			}
		}

		private void SepetCariUyariMetniniTemizle ()
		{
			if((_sepetCariComboBox==null&&textBox24==null)||!SepetCariUyariMetniAktifMi())
				return;

			_sepetCariDolduruluyor=true;
			try
			{
				SepetCariGirisTemizle();
			}
			finally
			{
				_sepetCariDolduruluyor=false;
			}
		}

		private void SepetCariTextBox_Enter ( object sender , EventArgs e )
		{
			SepetCariUyariMetniniTemizle();
		}

		private void SepetCariTextBox_Click ( object sender , EventArgs e )
		{
			SepetCariUyariMetniniTemizle();
		}

		private void SepetCariTextBox_Leave ( object sender , EventArgs e )
		{
			if(string.IsNullOrWhiteSpace(SepetCariGirisMetniGetir())&&!_sepetCariId.HasValue)
				SepetCariUyariMetniniGoster();
		}

		private void SepetCariAra_TextChanged ( object sender , EventArgs e )
		{
			if(_sepetCariDolduruluyor) return;

			string arama = SepetCariAdMetniGetir();
			SepetCariSecimleriniYenile();
			ComboBoxEslesmeleriniGoster(_sepetCariComboBox , arama);
			_sepetCariId=null;

			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
			{
				SepetCariBilgileriniTemizle();
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					int cariId;
					string adSoyad;
					string tc;
					string tel;
					int? cariTipId;
					if(SepetCariKaydiniBul(conn , null , out cariId , out adSoyad , out tc , out tel , out cariTipId))
					{
						SepetCariBilgileriniDoldur(cariId , adSoyad , tc , tel , cariTipId);
					}
					else
						SepetCariBilgileriniTemizle();
				}
			}
			catch
			{
				SepetCariBilgileriniTemizle();
			}
		}

		private void SepetCariBilgileriniDoldur ( OleDbDataReader rd )
		{
			int? cariTipId = rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"]);
			SepetCariBilgileriniDoldur(
				Convert.ToInt32(rd["CariID"]) ,
				rd["adsoyad"]?.ToString()??"" ,
				rd["tc"]?.ToString()??"" ,
				rd["telefon"]?.ToString()??"" ,
				cariTipId);
		}

		private void SepetCariBilgileriniDoldur ( int cariId , string adSoyad , string tc , string tel , int? cariTipId )
		{
			_sepetCariId=cariId;
			string tipAdi = CariTipAdiGetir(cariTipId);

			_sepetCariDolduruluyor=true;
			try
			{
				SepetCariGirisMetniniAyarla(CariGosterimDetayMetniOlustur(adSoyad , tipAdi) , SystemColors.WindowText);
				if(textBox25!=null)
					textBox25.Text=tc;
				if(textBox26!=null)
					textBox26.Text=tel;
			}
			finally
			{
				_sepetCariDolduruluyor=false;
			}

			if(comboCariTip!=null&&cariTipId.HasValue)
			{
				int seciliTipId;
				bool ayniTipSecili = comboCariTip.SelectedValue!=null&&
					comboCariTip.SelectedValue!=DBNull.Value&&
					int.TryParse(comboCariTip.SelectedValue.ToString() , out seciliTipId)&&
					seciliTipId==cariTipId.Value;
				if(!ayniTipSecili)
					comboCariTip.SelectedValue=cariTipId.Value;
			}
		}

		private void SepetCariBilgileriniTemizle ()
		{
			_sepetCariId=null;
			if(textBox25!=null)
				textBox25.Clear();
			if(textBox26!=null)
				textBox26.Clear();
			if(string.IsNullOrWhiteSpace(SepetCariGirisMetniGetir()))
				SepetCariUyariMetniniGoster();
		}

		private bool SepetteCariGirisiVarMi ()
		{
			return !string.IsNullOrWhiteSpace(SepetCariAdMetniGetir())||
				!string.IsNullOrWhiteSpace(textBox25?.Text)||
				!string.IsNullOrWhiteSpace(textBox26?.Text);
		}

		private CariAramaKaydi EnUygunCariKaydiniBul ( OleDbConnection conn , OleDbTransaction tx , string cariAdi , int? cariTipId )
		{
			string normalizeArama = AramaMetniniNormalizeEt(cariAdi);
			if(string.IsNullOrWhiteSpace(normalizeArama))
				return null;

			string sorgu = @"SELECT CariID,
								IIF(adsoyad IS NULL, '', adsoyad) AS adsoyad,
								IIF(tc IS NULL, '', tc) AS tc,
								IIF(telefon IS NULL, '', telefon) AS telefon,
								CariTipID
							FROM Cariler";
			if(cariTipId.HasValue)
				sorgu+=" WHERE CLng(IIF(CariTipID IS NULL, 0, CariTipID)) = ?";
			sorgu+=" ORDER BY IIF(adsoyad IS NULL, '', adsoyad)";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				if(cariTipId.HasValue)
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipId.Value;

				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					CariAramaKaydi birebirKayit = null;
					bool birdenFazlaBirebirVar = false;
					CariAramaKaydi enIyiKayit = null;
					string enIyiNormalizeAd = string.Empty;
					int enIyiPuan = int.MaxValue;
					int enIyiBaslangic = int.MaxValue;
					int enIyiEkKarakter = int.MaxValue;
					bool birdenFazlaAyniKaliteVar = false;

					while(rd!=null&&rd.Read())
					{
						string adayAdSoyad = rd["adsoyad"]?.ToString()??string.Empty;
						int adayCariId = Convert.ToInt32(rd["CariID"]);
						string adayNormalizeAd = AramaMetniniNormalizeEt(adayAdSoyad);
						if(string.Equals(adayNormalizeAd , normalizeArama , StringComparison.Ordinal))
						{
							if(birebirKayit==null)
							{
								birebirKayit=new CariAramaKaydi
								{
									CariId=adayCariId,
									AdSoyad=adayAdSoyad,
									Tc=rd["tc"]?.ToString()??string.Empty,
									Telefon=rd["telefon"]?.ToString()??string.Empty,
									CariTipId=rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"])
								};
							}
							else if(adayCariId!=birebirKayit.CariId)
								birdenFazlaBirebirVar=true;

							continue;
						}

						int puan;
						int baslangicIndexi;
						int ekKarakter;
						if(!CariAramaPuaniHesapla(cariAdi , adayAdSoyad , out puan , out baslangicIndexi , out ekKarakter))
							continue;

						bool dahaIyiKayit = enIyiKayit==null||
							puan<enIyiPuan||
							(puan==enIyiPuan&&baslangicIndexi<enIyiBaslangic)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter<enIyiEkKarakter)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter==enIyiEkKarakter&&
								string.Compare(adayAdSoyad , enIyiKayit.AdSoyad , true , CultureInfo.CurrentCulture)<0);

						if(dahaIyiKayit)
						{
							enIyiPuan=puan;
							enIyiBaslangic=baslangicIndexi;
							enIyiEkKarakter=ekKarakter;
							enIyiNormalizeAd=adayNormalizeAd;
							birdenFazlaAyniKaliteVar=false;
							enIyiKayit=new CariAramaKaydi
							{
								CariId=Convert.ToInt32(rd["CariID"]),
								AdSoyad=adayAdSoyad,
								Tc=rd["tc"]?.ToString()??string.Empty,
								Telefon=rd["telefon"]?.ToString()??string.Empty,
								CariTipId=rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"])
							};
							continue;
						}

						bool ayniKalite = puan==enIyiPuan&&
							baslangicIndexi==enIyiBaslangic&&
							ekKarakter==enIyiEkKarakter&&
							Convert.ToInt32(rd["CariID"])!=enIyiKayit.CariId;
						if(ayniKalite)
							birdenFazlaAyniKaliteVar=true;
					}

					if(birdenFazlaBirebirVar)
						return null;
					if(birebirKayit!=null)
						return birebirKayit;

					if(birdenFazlaAyniKaliteVar&&enIyiPuan>0)
						return null;

					return enIyiKayit;
				}
			}
		}

		private bool CariAramaPuaniHesapla ( string arama , string adayAdSoyad , out int puan , out int baslangicIndexi , out int ekKarakter )
		{
			puan=int.MaxValue;
			baslangicIndexi=int.MaxValue;
			ekKarakter=int.MaxValue;

			string normalizeArama = AramaMetniniNormalizeEt(arama);
			string normalizeAday = AramaMetniniNormalizeEt(adayAdSoyad);
			if(string.IsNullOrWhiteSpace(normalizeArama)||string.IsNullOrWhiteSpace(normalizeAday))
				return false;

			if(string.Equals(normalizeArama , normalizeAday , StringComparison.Ordinal))
			{
				puan=0;
				baslangicIndexi=0;
				ekKarakter=0;
				return true;
			}

			baslangicIndexi=normalizeAday.IndexOf(normalizeArama , StringComparison.Ordinal);
			if(baslangicIndexi<0)
				return false;

			bool baslangicta = baslangicIndexi==0;
			bool baslangicSiniri = baslangicta||MetinAramaSiniriVarMi(normalizeAday[baslangicIndexi-1] , normalizeAday[baslangicIndexi]);
			int bitisIndexi = baslangicIndexi+normalizeArama.Length;
			bool bitisSiniri = bitisIndexi>=normalizeAday.Length||
				MetinAramaSiniriVarMi(normalizeAday[bitisIndexi-1] , normalizeAday[bitisIndexi]);

			if(baslangicta&&bitisSiniri)
				puan=1;
			else if(baslangicSiniri&&bitisSiniri)
				puan=2;
			else if(baslangicta)
				puan=3;
			else if(baslangicSiniri||bitisSiniri)
				puan=4;
			else
				puan=5;

			ekKarakter=Math.Max(0 , normalizeAday.Length-normalizeArama.Length);
			return true;
		}

		private string AramaMetniniNormalizeEt ( string metin )
		{
			if(string.IsNullOrWhiteSpace(metin))
				return string.Empty;

			StringBuilder sb = new StringBuilder(metin.Length);
			bool sonBosluk = false;

			foreach(char hamKarakter in metin.Trim())
			{
				char karakter = char.ToLowerInvariant(hamKarakter);
				switch(karakter)
				{
					case 'ç':
						karakter='c';
						break;
					case 'ğ':
						karakter='g';
						break;
					case 'ı':
					case 'i':
						karakter='i';
						break;
					case 'ö':
						karakter='o';
						break;
					case 'ş':
						karakter='s';
						break;
					case 'ü':
						karakter='u';
						break;
				}

				if(char.IsWhiteSpace(karakter))
				{
					if(sonBosluk)
						continue;
					sb.Append(' ');
					sonBosluk=true;
					continue;
				}

				sb.Append(karakter);
				sonBosluk=false;
			}

			return sb.ToString();
		}

		private bool MetinAramaSiniriVarMi ( char solKarakter , char sagKarakter )
		{
			if(char.IsWhiteSpace(solKarakter)||char.IsWhiteSpace(sagKarakter))
				return true;
			if(!char.IsLetterOrDigit(solKarakter)||!char.IsLetterOrDigit(sagKarakter))
				return true;
			if(char.IsDigit(solKarakter)!=char.IsDigit(sagKarakter))
				return true;

			return false;
		}

		private bool SepetCariKaydiniBul ( OleDbConnection conn , OleDbTransaction tx , out int cariId , out string adSoyad , out string tc , out string tel , out int? cariTipId )
		{
			cariId=0;
			adSoyad=string.Empty;
			tc=string.Empty;
			tel=string.Empty;
			cariTipId=null;

			string cariGirisMetni = SepetCariGirisMetniGetir();
			string cariAdi = SepetCariAdMetniGetir();
			string cariTc = textBox25?.Text?.Trim()??string.Empty;
			string cariTelefon = textBox26?.Text?.Trim()??string.Empty;
			if(string.IsNullOrWhiteSpace(cariAdi)&&string.IsNullOrWhiteSpace(cariTc)&&string.IsNullOrWhiteSpace(cariTelefon))
				return false;

			int? seciliCariTipId = CariTipIdGetir(conn , tx , CariAramaMetnindenTipAdiGetir(cariGirisMetni));
			if(!string.IsNullOrWhiteSpace(cariAdi)&&string.IsNullOrWhiteSpace(cariTc)&&string.IsNullOrWhiteSpace(cariTelefon))
			{
				CariAramaKaydi cariKaydi = EnUygunCariKaydiniBul(conn , tx , cariAdi , seciliCariTipId);
				if(cariKaydi==null)
					return false;

				cariId=cariKaydi.CariId;
				adSoyad=cariKaydi.AdSoyad;
				tc=cariKaydi.Tc;
				tel=cariKaydi.Telefon;
				cariTipId=cariKaydi.CariTipId;
				return true;
			}

			string sorgu = @"SELECT TOP 1 CariID, tc, telefon, adsoyad, CariTipID
							FROM Cariler
							WHERE 1=1";

			if(!string.IsNullOrWhiteSpace(cariAdi))
				sorgu+=" AND IIF(adsoyad IS NULL, '', adsoyad) LIKE ?";
			if(!string.IsNullOrWhiteSpace(cariTc))
				sorgu+=" AND IIF(tc IS NULL, '', tc) LIKE ?";
			if(!string.IsNullOrWhiteSpace(cariTelefon))
				sorgu+=" AND IIF(telefon IS NULL, '', telefon) LIKE ?";

			List<string> siralamaParcalari = new List<string>();
			if(!string.IsNullOrWhiteSpace(cariAdi))
				siralamaParcalari.Add("IIF(IIF(adsoyad IS NULL, '', adsoyad)=?, 0, IIF(IIF(adsoyad IS NULL, '', adsoyad) LIKE ?, 1, 2))");
			if(!string.IsNullOrWhiteSpace(cariTc))
				siralamaParcalari.Add("IIF(IIF(tc IS NULL, '', tc)=?, 0, 1)");
			if(!string.IsNullOrWhiteSpace(cariTelefon))
				siralamaParcalari.Add("IIF(IIF(telefon IS NULL, '', telefon)=?, 0, 1)");
			if(seciliCariTipId.HasValue)
				siralamaParcalari.Add("IIF(CLng(IIF(CariTipID IS NULL, 0, CariTipID))=?, 0, 1)");
			siralamaParcalari.Add("IIF(adsoyad IS NULL, '', adsoyad)");
			sorgu+=" ORDER BY "+string.Join(", " , siralamaParcalari);

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				if(!string.IsNullOrWhiteSpace(cariAdi))
					cmd.Parameters.AddWithValue("?" , "%"+cariAdi+"%");
				if(!string.IsNullOrWhiteSpace(cariTc))
					cmd.Parameters.AddWithValue("?" , "%"+cariTc+"%");
				if(!string.IsNullOrWhiteSpace(cariTelefon))
					cmd.Parameters.AddWithValue("?" , "%"+cariTelefon+"%");

				if(!string.IsNullOrWhiteSpace(cariAdi))
				{
					cmd.Parameters.AddWithValue("?" , cariAdi);
					cmd.Parameters.AddWithValue("?" , cariAdi+"%");
				}
				if(!string.IsNullOrWhiteSpace(cariTc))
					cmd.Parameters.AddWithValue("?" , cariTc);
				if(!string.IsNullOrWhiteSpace(cariTelefon))
					cmd.Parameters.AddWithValue("?" , cariTelefon);
				if(seciliCariTipId.HasValue)
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=seciliCariTipId.Value;

				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd==null||!rd.Read())
						return false;

					cariId=Convert.ToInt32(rd["CariID"]);
					adSoyad=rd["adsoyad"]?.ToString()??string.Empty;
					tc=rd["tc"]?.ToString()??string.Empty;
					tel=rd["telefon"]?.ToString()??string.Empty;
					cariTipId=rd["CariTipID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["CariTipID"]);
					return true;
				}
			}
		}

		private int? SepetCariIdCoz ( OleDbConnection conn , OleDbTransaction tx )
		{
			int cariId;
			string adSoyad;
			string tc;
			string tel;
			int? cariTipId;
			if(!SepetCariKaydiniBul(conn , tx , out cariId , out adSoyad , out tc , out tel , out cariTipId))
				return null;

			SepetCariBilgileriniDoldur(cariId , adSoyad , tc , tel , cariTipId);
			return cariId;
		}

		private UrunAramaKaydi EnUygunUrunKaydiniBul ( OleDbConnection conn , OleDbTransaction tx , string arama )
		{
			string temizArama = ( arama??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizArama))
				return null;

			string sorgu = @"SELECT U.UrunID, U.UrunAdi,
								IIF(K.KategoriAdi IS NULL, '', K.KategoriAdi) AS KategoriAdi,
								IIF(M.MarkaAdi IS NULL, '', M.MarkaAdi) AS MarkaAdi,
								IIF(B.BirimAdi IS NULL, '', B.BirimAdi) AS BirimAdi
							FROM ((Urunler AS U
							LEFT JOIN Kategoriler AS K ON U.KategoriID = K.KategoriID)
							LEFT JOIN Markalar AS M ON U.MarkaID = M.MarkaID)
							LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
							WHERE IIF(U.UrunAdi IS NULL, '', U.UrunAdi) LIKE ?
							   OR IIF(M.MarkaAdi IS NULL, '', M.MarkaAdi) LIKE ?
							   OR IIF(M.MarkaAdi IS NULL OR M.MarkaAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), IIF(U.UrunAdi IS NULL, '', U.UrunAdi) & ' - ' & M.MarkaAdi) LIKE ?
							ORDER BY U.UrunAdi ASC, M.MarkaAdi ASC";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");

				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					UrunAramaKaydi enIyiKayit = null;
					int enIyiPuan = int.MaxValue;
					int enIyiBaslangic = int.MaxValue;
					int enIyiEkKarakter = int.MaxValue;
					string enIyiKarsilastirmaMetni = string.Empty;

					while(rd!=null&&rd.Read())
					{
						string adayUrunAdi = rd["UrunAdi"]?.ToString()??string.Empty;
						string adayMarkaAdi = rd["MarkaAdi"]?.ToString()??string.Empty;
						string adayGosterimMetni = UrunGosterimMetniGetir(adayUrunAdi , adayMarkaAdi);

						int urunAdiPuani;
						int urunAdiBaslangici;
						bool urunAdiEslesmesi = UrunAramaPuaniHesapla(temizArama , adayUrunAdi , out urunAdiPuani , out urunAdiBaslangici);

						int gosterimPuani;
						int gosterimBaslangici;
						bool gosterimEslesmesi = UrunAramaPuaniHesapla(temizArama , adayGosterimMetni , out gosterimPuani , out gosterimBaslangici);

						if(!urunAdiEslesmesi&&!gosterimEslesmesi)
							continue;

						int puan;
						int baslangicIndexi;
						string karsilastirmaMetni;
						if(gosterimEslesmesi&&(!urunAdiEslesmesi||gosterimPuani<urunAdiPuani||(gosterimPuani==urunAdiPuani&&gosterimBaslangici<urunAdiBaslangici)))
						{
							puan=gosterimPuani;
							baslangicIndexi=gosterimBaslangici;
							karsilastirmaMetni=adayGosterimMetni;
						}
						else
						{
							puan=urunAdiPuani;
							baslangicIndexi=urunAdiBaslangici;
							karsilastirmaMetni=adayUrunAdi;
						}

						int ekKarakter = Math.Max(0 , karsilastirmaMetni.Trim().Length-temizArama.Length);
						bool dahaIyiKayit = enIyiKayit==null||
							puan<enIyiPuan||
							(puan==enIyiPuan&&baslangicIndexi<enIyiBaslangic)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter<enIyiEkKarakter)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter==enIyiEkKarakter&&
								string.Compare(karsilastirmaMetni , enIyiKarsilastirmaMetni , true , CultureInfo.CurrentCulture)<0);
						if(!dahaIyiKayit)
							continue;

						enIyiPuan=puan;
						enIyiBaslangic=baslangicIndexi;
						enIyiEkKarakter=ekKarakter;
						enIyiKarsilastirmaMetni=karsilastirmaMetni;
						enIyiKayit=new UrunAramaKaydi
						{
							UrunId=Convert.ToInt32(rd["UrunID"]),
							UrunAdi=adayUrunAdi,
							BirimAdi=rd["BirimAdi"]?.ToString()??string.Empty,
							KategoriAdi=rd["KategoriAdi"]?.ToString()??string.Empty,
							MarkaAdi=adayMarkaAdi
						};
					}

					return enIyiKayit;
				}
			}
		}

		private YapilanIsKaydi EnUygunYapilanIsKaydiniBul ( OleDbConnection conn , OleDbTransaction tx , string arama )
		{
			string temizArama = ( arama??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizArama))
				return null;

			string sorgu = @"SELECT
								[YapilanIsID],
								IIF([IsBilgisi] IS NULL, '', [IsBilgisi]) AS IsBilgisi,
								IIF([IsAdi] IS NULL, '', [IsAdi]) AS IsAdi,
								IIF([Birim] IS NULL OR [Birim]='', '" + VarsayilanYapilanIsBirimi + @"', [Birim]) AS Birim,
								IIF([Adet] IS NULL, 0, [Adet]) AS Adet,
								IIF([Miktar] IS NULL, 0, [Miktar]) AS Miktar,
								IIF([Fiyat] IS NULL, 0, [Fiyat]) AS Fiyat,
								IIF([ToplamFiyat] IS NULL, IIF([Miktar] IS NULL, 0, [Miktar]) * IIF([Fiyat] IS NULL, 0, [Fiyat]), [ToplamFiyat]) AS ToplamFiyat
							FROM [YapilanIsler]
							WHERE IIF([IsAdi] IS NULL, '', [IsAdi]) LIKE ?
							   OR IIF([IsBilgisi] IS NULL, '', [IsBilgisi]) LIKE ?
							   OR (IIF([IsAdi] IS NULL, '', [IsAdi]) & ' - ' & IIF([IsBilgisi] IS NULL, '', [IsBilgisi])) LIKE ?
							ORDER BY IIF([IsAdi] IS NULL, '', [IsAdi]) ASC";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");
				cmd.Parameters.AddWithValue("?" , "%"+temizArama+"%");

				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					YapilanIsKaydi enIyiKayit = null;
					int enIyiPuan = int.MaxValue;
					int enIyiBaslangic = int.MaxValue;
					int enIyiEkKarakter = int.MaxValue;
					string enIyiKarsilastirmaMetni = string.Empty;

					while(rd!=null&&rd.Read())
					{
						string adayIsAdi = Convert.ToString(rd["IsAdi"])??string.Empty;
						string adayBilgi = Convert.ToString(rd["IsBilgisi"])??string.Empty;
						string adayGosterimMetni = string.IsNullOrWhiteSpace(adayBilgi) ? adayIsAdi : adayIsAdi+" - "+adayBilgi;

						int isAdiPuani;
						int isAdiBaslangici;
						bool isAdiEslesmesi = UrunAramaPuaniHesapla(temizArama , adayIsAdi , out isAdiPuani , out isAdiBaslangici);

						int gosterimPuani;
						int gosterimBaslangici;
						bool gosterimEslesmesi = UrunAramaPuaniHesapla(temizArama , adayGosterimMetni , out gosterimPuani , out gosterimBaslangici);

						if(!isAdiEslesmesi&&!gosterimEslesmesi)
							continue;

						int puan;
						int baslangicIndexi;
						string karsilastirmaMetni;
						if(gosterimEslesmesi&&(!isAdiEslesmesi||gosterimPuani<isAdiPuani||(gosterimPuani==isAdiPuani&&gosterimBaslangici<isAdiBaslangici)))
						{
							puan=gosterimPuani;
							baslangicIndexi=gosterimBaslangici;
							karsilastirmaMetni=adayGosterimMetni;
						}
						else
						{
							puan=isAdiPuani;
							baslangicIndexi=isAdiBaslangici;
							karsilastirmaMetni=adayIsAdi;
						}

						int ekKarakter = Math.Max(0 , karsilastirmaMetni.Trim().Length-temizArama.Length);
						bool dahaIyiKayit = enIyiKayit==null||
							puan<enIyiPuan||
							(puan==enIyiPuan&&baslangicIndexi<enIyiBaslangic)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter<enIyiEkKarakter)||
							(puan==enIyiPuan&&baslangicIndexi==enIyiBaslangic&&ekKarakter==enIyiEkKarakter&&
								string.Compare(karsilastirmaMetni , enIyiKarsilastirmaMetni , true , CultureInfo.CurrentCulture)<0);
						if(!dahaIyiKayit)
							continue;

						enIyiPuan=puan;
						enIyiBaslangic=baslangicIndexi;
						enIyiEkKarakter=ekKarakter;
						enIyiKarsilastirmaMetni=karsilastirmaMetni;
						enIyiKayit=new YapilanIsKaydi
						{
							YapilanIsId=Convert.ToInt32(rd["YapilanIsID"]),
							IsBilgisi=adayBilgi,
							IsAdi=adayIsAdi,
							Birim=Convert.ToString(rd["Birim"])??VarsayilanYapilanIsBirimi,
							Adet=rd["Adet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Adet"]),
							Miktar=rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]),
							Fiyat=rd["Fiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Fiyat"]),
							ToplamFiyat=rd["ToplamFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamFiyat"])
						};
					}

					return enIyiKayit;
				}
			}
		}

		private bool UrunAramaPuaniHesapla ( string arama , string urunAdi , out int puan , out int baslangicIndexi )
		{
			puan=int.MaxValue;
			baslangicIndexi=int.MaxValue;

			string temizArama = ( arama??string.Empty ).Trim();
			string temizUrunAdi = ( urunAdi??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizArama)||string.IsNullOrWhiteSpace(temizUrunAdi))
				return false;

			if(string.Equals(temizArama , temizUrunAdi , StringComparison.CurrentCultureIgnoreCase))
			{
				puan=0;
				baslangicIndexi=0;
				return true;
			}

			baslangicIndexi=CultureInfo.CurrentCulture.CompareInfo.IndexOf(temizUrunAdi , temizArama , CompareOptions.IgnoreCase);
			if(baslangicIndexi<0)
				return false;

			bool baslangicta = baslangicIndexi==0;
			bool baslangicSiniri = baslangicta||UrunAramaSiniriVarMi(temizUrunAdi[baslangicIndexi-1] , temizUrunAdi[baslangicIndexi]);
			int bitisIndexi = baslangicIndexi+temizArama.Length;
			bool bitisSiniri = bitisIndexi>=temizUrunAdi.Length||
				UrunAramaSiniriVarMi(temizUrunAdi[bitisIndexi-1] , temizUrunAdi[bitisIndexi]);

			if(baslangicta&&bitisSiniri)
				puan=1;
			else if(baslangicSiniri&&bitisSiniri)
				puan=2;
			else if(baslangicta)
				puan=3;
			else if(baslangicSiniri||bitisSiniri)
				puan=4;
			else
				puan=5;

			return true;
		}

		private bool UrunAramaSiniriVarMi ( char solKarakter , char sagKarakter )
		{
			if(char.IsWhiteSpace(solKarakter)||char.IsWhiteSpace(sagKarakter))
				return true;
			if(!char.IsLetterOrDigit(solKarakter)||!char.IsLetterOrDigit(sagKarakter))
				return true;
			if(char.IsDigit(solKarakter)!=char.IsDigit(sagKarakter))
				return true;

			return false;
		}

		private void SepetUrunAra_TextChanged ( object sender , EventArgs e )
		{
			if(_sepetUrunDolduruluyor) return;

			string arama = SepetUrunGirisMetniGetir();
			SepetUrunSecimleriniYenile();
			ComboBoxEslesmeleriniGoster(_sepetUrunComboBox , arama);
			SepetUrunSeciminiTemizle();

			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
			{
				if(textBox29!=null) textBox29.Text="1";
				if(textBox32!=null) textBox32.Text="0,00";
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					UrunAramaKaydi urunKaydi = EnUygunUrunKaydiniBul(conn , null , arama);
					if(urunKaydi!=null)
					{
						bool tekAdayVar = _sepetUrunComboBox!=null&&_sepetUrunComboBox.Items.Count==1;
						bool tamEslesmeVar = SepetUrunMetniKaydaTamEslesiyorMu(arama , urunKaydi);
						if(tamEslesmeVar||tekAdayVar)
							SepetUrunKaydiniUygula(urunKaydi , tamEslesmeVar);
					}
				}
			}
			catch
			{
				_sepetUrunDolduruluyor=false;
				SepetUrunSeciminiTemizle();
			}
		}

		private void SepetYapilanIs_TextChanged ( object sender , EventArgs e )
		{
			if(_sepetYapilanIsDolduruluyor) return;

			string arama = _sepetYapilanIsComboBox?.Text?.Trim()??string.Empty;
			SepetYapilanIsSecimleriniYenile();
			ComboBoxEslesmeleriniGoster(_sepetYapilanIsComboBox , arama);
			SepetYapilanIsSeciminiTemizle(false);

			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					YapilanIsKaydi kayit = EnUygunYapilanIsKaydiniBul(conn , null , arama);
					if(kayit!=null)
					{
						SepetUrunSeciminiTemizle();
						SepetUrunGirisTemizle();
						SepetYapilanIsBilgileriniDoldur(kayit , true);
					}
				}
			}
			catch
			{
				SepetYapilanIsSeciminiTemizle(false);
			}
		}

		private void SepetUrunFiyatGuncelle ()
		{
			if(!_sepetUrunId.HasValue) return;

			decimal fiyat = 0;
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					int? cariTipId = SepetCariTipIdGetir(conn);
					string sorgu = cariTipId.HasValue
						? "SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? AND CLng(IIF(CariTipiID IS NULL, 0, CariTipiID))=? ORDER BY UrunSatisFiyatID DESC"
						: "SELECT TOP 1 SatisFiyati FROM UrunSatisFiyat WHERE UrunID=? ORDER BY UrunSatisFiyatID DESC";
					using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
					{
						cmd.Parameters.AddWithValue("?" , _sepetUrunId.Value);
						if(cariTipId.HasValue)
							cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipId.Value;

						object sonuc = cmd.ExecuteScalar();
						if(sonuc!=null&&sonuc!=DBNull.Value)
							fiyat=Convert.ToDecimal(sonuc);
					}
				}
			}
			catch
			{
				fiyat=0;
			}

			if(textBox30!=null)
				textBox30.Text=fiyat.ToString("N2");

			SepetSatirToplamHesapla();
		}

		private int? SepetCariTipIdGetir ( OleDbConnection conn )
		{
			if(comboCariTip!=null&&comboCariTip.SelectedValue!=null&&comboCariTip.SelectedValue!=DBNull.Value)
			{
				int id;
				if(int.TryParse(comboCariTip.SelectedValue.ToString() , out id))
					return id;
			}

			string tipAdi = comboCariTip?.Text?.Trim();
			if(string.IsNullOrWhiteSpace(tipAdi))
				return null;

			string sorgu = @"SELECT TOP 1 CariTipID
							FROM CariTipi
							WHERE TipAdi LIKE ?
							ORDER BY IIF(TipAdi=?, 0, IIF(TipAdi LIKE ?, 1, 2)), CariTipID";
			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
			{
				cmd.Parameters.AddWithValue("?" , "%"+tipAdi+"%");
				cmd.Parameters.AddWithValue("?" , tipAdi);
				cmd.Parameters.AddWithValue("?" , tipAdi+"%");
				object sonuc = cmd.ExecuteScalar();
				if(sonuc!=null&&sonuc!=DBNull.Value)
					return Convert.ToInt32(sonuc);
			}

			return null;
		}

		private int? CariyeAitCariTipIdGetir ( OleDbConnection conn , OleDbTransaction tx , int cariId )
		{
			using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 CariTipID FROM Cariler WHERE CariID=?" , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , cariId);
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? (int?)null : Convert.ToInt32(sonuc);
			}
		}

		private BelgeKayitTuru CariTiptenBelgeTuruGetir ( int? cariTipId , string tipAdi )
		{
			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.FabrikaFaturasi , out BelgePaneli fabrikaPaneli)&&
				fabrikaPaneli.CariTipId.HasValue&&cariTipId==fabrikaPaneli.CariTipId)
				return BelgeKayitTuru.FabrikaFaturasi;
			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.SucuFaturasi , out BelgePaneli sucuPaneli)&&
				sucuPaneli.CariTipId.HasValue&&cariTipId==sucuPaneli.CariTipId)
				return BelgeKayitTuru.SucuFaturasi;
			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.MusteriFaturasi , out BelgePaneli musteriPaneli)&&
				musteriPaneli.CariTipId.HasValue&&cariTipId==musteriPaneli.CariTipId)
				return BelgeKayitTuru.MusteriFaturasi;

			string buyukTip = KarsilastirmaMetniHazirla(tipAdi);
			if(buyukTip.Contains("FABR"))
				return BelgeKayitTuru.FabrikaFaturasi;
			if(buyukTip.Contains("SUCU"))
				return BelgeKayitTuru.SucuFaturasi;

			return BelgeKayitTuru.MusteriFaturasi;
		}

		private void SepetteSecilenCariTipiniGetir ( OleDbConnection conn , OleDbTransaction tx , int cariId , out int? cariTipId , out string tipAdi )
		{
			cariTipId=SepetCariTipIdGetir(conn);
			tipAdi=comboCariTip?.Text?.Trim()??string.Empty;
			if(cariTipId.HasValue)
				return;

			cariTipId=CariyeAitCariTipIdGetir(conn , tx , cariId);
			if(cariTipId.HasValue)
			{
				using(OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 TipAdi FROM CariTipi WHERE CariTipID=?" , conn , tx))
				{
					cmd.Parameters.AddWithValue("?" , cariTipId.Value);
					object sonuc = cmd.ExecuteScalar();
					tipAdi=sonuc?.ToString()??string.Empty;
				}
			}
		}

		private void SepetAdet_TextChanged ( object sender , EventArgs e )
		{
			if(_sepetSatirSeciliyor) return;
			SepetSatirToplamHesapla();
		}

		private void SepetFiyat_TextChanged ( object sender , EventArgs e )
		{
			if(_sepetSatirSeciliyor) return;
			SepetSatirToplamHesapla();
		}

		private void SepetKdv_TextChanged ( object sender , EventArgs e )
		{
			SepetGenelToplamHesapla();
		}

		private void SepetSatirToplamHesapla ()
		{
			if(_sepetHesaplanıyor) return;
			_sepetHesaplanıyor=true;

			decimal adet = SepetDecimalParse(textBox29?.Text);

			decimal fiyat = SepetDecimalParse(textBox30?.Text);
			decimal toplam = adet*fiyat;

			if(textBox32!=null)
				textBox32.Text=toplam.ToString("N2" , _yazdirmaKulturu);

			_sepetHesaplanıyor=false;
		}

		private void SepetGenelToplamHesapla ()
		{
			if(_sepetHesaplanıyor) return;
			_sepetHesaplanıyor=true;

			decimal genelToplam = 0;
			if(dataGridView5!=null)
			{
				foreach(DataGridViewRow row in dataGridView5.Rows)
				{
					if(row.IsNewRow) continue;

					decimal satirToplam = SepetDecimalParse(Convert.ToString(row.Cells["toplamfiyat"].Value));
					if(satirToplam<=0)
					{
						decimal adet = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
						decimal fiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
						satirToplam=adet*fiyat;
						row.Cells["toplamfiyat"].Value=satirToplam;
					}
					genelToplam+=satirToplam;
				}
			}

			decimal total = SepetToplamTutarHesapla(genelToplam);

			if(label46!=null)
				label46.Text=genelToplam.ToString("N2" , _yazdirmaKulturu);
			if(label48!=null)
				label48.Text=total.ToString("N2" , _yazdirmaKulturu);

			_sepetHesaplanıyor=false;
		}

		private decimal SepetToplamTutarHesapla ( decimal araToplam )
		{
			decimal kdvOrani = SepetDecimalParse(textBox39?.Text);
			if(araToplam<=0||kdvOrani<=0)
				return araToplam;

			decimal kdvTutari = araToplam*kdvOrani/100m;
			return araToplam+kdvTutari;
		}

		private decimal SepetDecimalParse ( string text )
		{
			if(string.IsNullOrWhiteSpace(text))
				return 0;

			string temizMetin = text.Trim();
			decimal sonuc;
			int sonVirgul = temizMetin.LastIndexOf(',');
			int sonNokta = temizMetin.LastIndexOf('.');

			if(sonVirgul>=0&&sonNokta>=0)
			{
				if(sonVirgul>sonNokta)
				{
					if(decimal.TryParse(temizMetin , NumberStyles.Currency , _yazdirmaKulturu , out sonuc))
						return sonuc;

					string trAday = temizMetin.Replace("." , string.Empty);
					if(decimal.TryParse(trAday , NumberStyles.Currency , _yazdirmaKulturu , out sonuc))
						return sonuc;
				}
				else
				{
					if(decimal.TryParse(temizMetin , NumberStyles.Currency , CultureInfo.InvariantCulture , out sonuc))
						return sonuc;

					string invariantAday = temizMetin.Replace("," , string.Empty);
					if(decimal.TryParse(invariantAday , NumberStyles.Currency , CultureInfo.InvariantCulture , out sonuc))
						return sonuc;
				}
			}

			if(sonVirgul>=0&&decimal.TryParse(temizMetin , NumberStyles.Currency , _yazdirmaKulturu , out sonuc))
				return sonuc;

			if(sonNokta>=0&&decimal.TryParse(temizMetin , NumberStyles.Currency , CultureInfo.InvariantCulture , out sonuc))
				return sonuc;

			if(decimal.TryParse(temizMetin , NumberStyles.Currency , _yazdirmaKulturu , out sonuc))
				return sonuc;

			if(decimal.TryParse(temizMetin , NumberStyles.Currency , CultureInfo.InvariantCulture , out sonuc))
				return sonuc;

			return 0;
		}

		private void SepetSayisal_KeyPress ( object sender , KeyPressEventArgs e )
		{
			if(!char.IsControl(e.KeyChar)&&!char.IsDigit(e.KeyChar)&&(e.KeyChar!=','))
				e.Handled=true;

			if((e.KeyChar==',')&&((sender as TextBox).Text.IndexOf(',')>-1))
				e.Handled=true;
		}

		private bool SatirKolonuVarMi ( DataGridViewRow row , string kolonAdi )
		{
			return row!=null&&row.DataGridView!=null&&!string.IsNullOrWhiteSpace(kolonAdi)&&row.DataGridView.Columns.Contains(kolonAdi);
		}

		private bool SatirYapilanIsKalemiMi ( DataGridViewRow row )
		{
			int? yapilanIsId = SatirdanIntGetir(row , "YapilanIsID");
			if(yapilanIsId.HasValue&&yapilanIsId.Value>0)
				return true;

			if(!SatirKolonuVarMi(row , "KalemTuru"))
				return false;

			string kalemTuru = AramaMetniniNormalizeEt(Convert.ToString(row.Cells["KalemTuru"].Value));
			return kalemTuru.Contains("yapilan is")||kalemTuru.Contains("hizmet");
		}

		private object BosMetniDbNullYap ( string metin )
		{
			return string.IsNullOrWhiteSpace(metin) ? (object)DBNull.Value : metin.Trim();
		}

		private YapilanIsKaydi YapilanIsKaydiniGetir ( OleDbConnection conn , OleDbTransaction tx , int yapilanIsId )
		{
			if(conn==null||yapilanIsId<=0)
				return null;

			const string sorgu = @"SELECT
									[YapilanIsID],
									IIF([IsBilgisi] IS NULL, '', [IsBilgisi]) AS IsBilgisi,
									IIF([IsAdi] IS NULL, '', [IsAdi]) AS IsAdi,
									IIF([Birim] IS NULL OR [Birim]='', '" + VarsayilanYapilanIsBirimi + @"', [Birim]) AS Birim,
									IIF([Adet] IS NULL, 0, [Adet]) AS Adet,
									IIF([Miktar] IS NULL, 0, [Miktar]) AS Miktar,
									IIF([Fiyat] IS NULL, 0, [Fiyat]) AS Fiyat,
									IIF([ToplamFiyat] IS NULL, IIF([Miktar] IS NULL, 0, [Miktar]) * IIF([Fiyat] IS NULL, 0, [Fiyat]), [ToplamFiyat]) AS ToplamFiyat
								FROM [YapilanIsler]
								WHERE [YapilanIsID]=?";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=yapilanIsId;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd==null||!rd.Read())
						return null;

					return new YapilanIsKaydi
					{
						YapilanIsId=Convert.ToInt32(rd["YapilanIsID"]),
						IsBilgisi=Convert.ToString(rd["IsBilgisi"])??string.Empty,
						IsAdi=Convert.ToString(rd["IsAdi"])??string.Empty,
						Birim=Convert.ToString(rd["Birim"])??VarsayilanYapilanIsBirimi,
						Adet=rd["Adet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Adet"]),
						Miktar=rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]),
						Fiyat=rd["Fiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Fiyat"]),
						ToplamFiyat=rd["ToplamFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamFiyat"])
					};
				}
			}
		}

		private string YapilanIsTanimiMetniGetir ( YapilanIsKaydi kayit )
		{
			if(kayit==null)
				return string.Empty;

			return string.IsNullOrWhiteSpace(kayit.KalemGosterimAdi)
				? ( kayit.IsBilgisi??string.Empty ).Trim()
				: kayit.KalemGosterimAdi.Trim();
		}

		private UrunAramaKaydi UrunKaydiniGetir ( OleDbConnection conn , OleDbTransaction tx , int urunId )
		{
			if(conn==null||urunId<=0)
				return null;

			const string sorgu = @"SELECT
									U.UrunID,
									IIF(U.UrunAdi IS NULL, '', U.UrunAdi) AS UrunAdi,
									IIF(K.KategoriAdi IS NULL, '', K.KategoriAdi) AS KategoriAdi,
									IIF(M.MarkaAdi IS NULL, '', M.MarkaAdi) AS MarkaAdi,
									IIF(B.BirimAdi IS NULL, '', B.BirimAdi) AS BirimAdi
								FROM ((Urunler AS U
								LEFT JOIN Kategoriler AS K ON U.KategoriID = K.KategoriID)
								LEFT JOIN Markalar AS M ON U.MarkaID = M.MarkaID)
								LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
								WHERE U.UrunID=?";

			using(OleDbCommand cmd = tx==null
				? new OleDbCommand(sorgu , conn)
				: new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd==null||!rd.Read())
						return null;

					return new UrunAramaKaydi
					{
						UrunId=Convert.ToInt32(rd["UrunID"]),
						UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
						KategoriAdi=Convert.ToString(rd["KategoriAdi"])??string.Empty,
						MarkaAdi=Convert.ToString(rd["MarkaAdi"])??string.Empty,
						BirimAdi=Convert.ToString(rd["BirimAdi"])??string.Empty
					};
				}
			}
		}

		private void SepetUrunSatiriniDoldur ( DataGridViewRow row , int urunId , string urunAdi , string markaAdi , string kategoriAdi , string birimAdi , decimal miktar , decimal birimFiyat )
		{
			if(row==null||row.IsNewRow)
				return;

			decimal temizMiktar = miktar<=0m ? 1m : miktar;
			decimal temizBirimFiyat = birimFiyat<0m ? 0m : birimFiyat;

			row.Cells["UrunID"].Value=urunId;
			row.Cells["YapilanIsID"].Value=DBNull.Value;
			row.Cells["KalemTuru"].Value="ÜRÜN";
			row.Cells["IsBilgisi"].Value=DBNull.Value;
			row.Cells["KalemAdet"].Value=DBNull.Value;
			row.Cells["urunadi"].Value=string.IsNullOrWhiteSpace(urunAdi) ? string.Empty : urunAdi.Trim();
			row.Cells["marka"].Value=string.IsNullOrWhiteSpace(markaAdi) ? string.Empty : markaAdi.Trim();
			row.Cells["kategori"].Value=string.IsNullOrWhiteSpace(kategoriAdi) ? string.Empty : kategoriAdi.Trim();
			row.Cells["birim"].Value=string.IsNullOrWhiteSpace(birimAdi) ? string.Empty : birimAdi.Trim();
			row.Cells["adet"].Value=temizMiktar;
			row.Cells["SatisFiyati"].Value=temizBirimFiyat;
			row.Cells["toplamfiyat"].Value=temizMiktar*temizBirimFiyat;
		}

		private DataGridViewRow SepetUrunSatiriEkle ( int urunId , string urunAdi , string markaAdi , string kategoriAdi , string birimAdi , decimal miktar , decimal birimFiyat )
		{
			if(dataGridView5==null)
				return null;

			int satirIndex = dataGridView5.Rows.Add();
			if(satirIndex<0||satirIndex>=dataGridView5.Rows.Count)
				return null;

			DataGridViewRow row = dataGridView5.Rows[satirIndex];
			SepetUrunSatiriniDoldur(row , urunId , urunAdi , markaAdi , kategoriAdi , birimAdi , miktar , birimFiyat);
			return row;
		}

		private void SepetUrunSatiriniNormalizeEtGerekirse ( DataGridViewRow row )
		{
			if(row==null||row.IsNewRow||SatirYapilanIsKalemiMi(row))
				return;

			int? urunId = SatirdanIntGetir(row , "UrunID");
			if(!urunId.HasValue||urunId.Value<=0)
				return;

			string mevcutKalemAdi = Convert.ToString(row.Cells["urunadi"].Value)??string.Empty;
			string mevcutMarkaAdi = Convert.ToString(row.Cells["marka"].Value)??string.Empty;
			string yapilanIsHam = Convert.ToString(row.Cells["YapilanIsID"].Value)??string.Empty;
			bool kayikSatir = !string.IsNullOrWhiteSpace(yapilanIsHam)&&!SatirdanIntGetir(row , "YapilanIsID").HasValue;

			decimal miktar = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
			if(miktar<=0m)
				miktar=SepetDecimalParse(Convert.ToString(row.Cells["urunadi"].Value));
			if(miktar<=0m)
				miktar=1m;

			decimal birimFiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
			if(birimFiyat<=0m)
				birimFiyat=SepetDecimalParse(Convert.ToString(row.Cells["marka"].Value));
			if(birimFiyat<=0m)
				birimFiyat=SepetDecimalParse(Convert.ToString(row.Cells["kategori"].Value));

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					UrunAramaKaydi urunKaydi = UrunKaydiniGetir(conn , null , urunId.Value);
					if(urunKaydi==null)
						return;

					string beklenenGosterim = UrunGosterimMetniGetir(urunKaydi.UrunAdi , urunKaydi.MarkaAdi);
					bool markaEkliKalemAdi = !string.IsNullOrWhiteSpace(mevcutKalemAdi)&&
						string.Equals(mevcutKalemAdi.Trim() , beklenenGosterim , StringComparison.CurrentCultureIgnoreCase);

					if(!kayikSatir&&!markaEkliKalemAdi)
						return;

					if(!kayikSatir)
					{
						row.Cells["urunadi"].Value=string.IsNullOrWhiteSpace(urunKaydi.UrunAdi) ? mevcutKalemAdi : urunKaydi.UrunAdi.Trim();
						return;
					}

					SepetUrunSatiriniDoldur(
						row ,
						urunKaydi.UrunId ,
						string.IsNullOrWhiteSpace(urunKaydi.UrunAdi) ? urunKaydi.UrunGosterimAdi : urunKaydi.UrunAdi ,
						urunKaydi.MarkaAdi ,
						urunKaydi.KategoriAdi ,
						urunKaydi.BirimAdi ,
						miktar ,
						birimFiyat);
				}
			}
			catch
			{
			}
		}

		private KalemSecimBilgisi SepetteSeciliKalemBilgisiniOlustur ( OleDbConnection conn , OleDbTransaction tx )
		{
			string yapilanIsMetni = _sepetYapilanIsComboBox?.Text?.Trim()??string.Empty;
			if(_sepetYapilanIsId.HasValue||!string.IsNullOrWhiteSpace(yapilanIsMetni))
			{
				YapilanIsKaydi yapilanIsKaydi = _sepetYapilanIsId.HasValue
					? YapilanIsKaydiniGetir(conn , tx , _sepetYapilanIsId.Value)
					: null;
				if(yapilanIsKaydi==null&&!string.IsNullOrWhiteSpace(yapilanIsMetni))
					yapilanIsKaydi=EnUygunYapilanIsKaydiniBul(conn , tx , yapilanIsMetni);
				if(yapilanIsKaydi==null)
					return null;

				string yapilanIsTanimi = YapilanIsTanimiMetniGetir(yapilanIsKaydi);

				return new KalemSecimBilgisi
				{
					YapilanIsMi=true,
					YapilanIsId=yapilanIsKaydi.YapilanIsId,
					KalemAdi=yapilanIsKaydi.KalemGosterimAdi,
					Birim=string.IsNullOrWhiteSpace(textBox28?.Text) ? ( string.IsNullOrWhiteSpace(yapilanIsKaydi.Birim) ? VarsayilanYapilanIsBirimi : yapilanIsKaydi.Birim ) : textBox28.Text.Trim(),
					IsBilgisi=string.IsNullOrWhiteSpace(textBox35?.Text) ? yapilanIsTanimi : textBox35.Text.Trim(),
					Adet=SepetDecimalParse(textBox36?.Text)<=0 ? ( yapilanIsKaydi.Adet<=0 ? 1m : yapilanIsKaydi.Adet ) : SepetDecimalParse(textBox36?.Text),
					Miktar=SepetDecimalParse(textBox29?.Text)<=0 ? ( yapilanIsKaydi.Miktar<=0 ? 1m : yapilanIsKaydi.Miktar ) : SepetDecimalParse(textBox29?.Text),
					BirimFiyat=SepetDecimalParse(textBox30?.Text)<=0 ? yapilanIsKaydi.Fiyat : SepetDecimalParse(textBox30?.Text)
				};
			}

			string urunMetni = SepetUrunGirisMetniGetir();
			if(!_sepetUrunId.HasValue&&string.IsNullOrWhiteSpace(urunMetni))
				return null;

			UrunAramaKaydi urunKaydi = _sepetUrunId.HasValue
				? UrunKaydiniGetir(conn , tx , _sepetUrunId.Value)
				: null;
			if(urunKaydi==null&&!string.IsNullOrWhiteSpace(urunMetni))
				urunKaydi=EnUygunUrunKaydiniBul(conn , tx , urunMetni);
			if(urunKaydi==null)
				return null;

			return new KalemSecimBilgisi
			{
				YapilanIsMi=false,
				UrunId=urunKaydi.UrunId,
				KalemAdi=string.IsNullOrWhiteSpace(urunKaydi.UrunAdi) ? urunKaydi.UrunGosterimAdi : urunKaydi.UrunAdi,
				Birim=string.IsNullOrWhiteSpace(textBox28?.Text) ? urunKaydi.BirimAdi : textBox28.Text.Trim(),
				Miktar=SepetDecimalParse(textBox29?.Text)<=0 ? 1m : SepetDecimalParse(textBox29?.Text),
				BirimFiyat=SepetDecimalParse(textBox30?.Text)
			};
		}

		private KalemSecimBilgisi SepetSatirindanKalemBilgisiGetir ( OleDbConnection conn , OleDbTransaction tx , DataGridViewRow row )
		{
			if(row==null||row.IsNewRow)
				return null;

			SepetUrunSatiriniNormalizeEtGerekirse(row);

			bool yapilanIsMi = SatirYapilanIsKalemiMi(row);
			decimal miktar = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
			decimal birimFiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
			string kalemAdi = Convert.ToString(row.Cells["urunadi"].Value)??string.Empty;
			string birim = Convert.ToString(row.Cells["birim"].Value)??string.Empty;

			if(yapilanIsMi)
			{
				int? yapilanIsId = SatirdanIntGetir(row , "YapilanIsID");
				YapilanIsKaydi yapilanIsKaydi = yapilanIsId.HasValue
					? YapilanIsKaydiniGetir(conn , tx , yapilanIsId.Value)
					: null;
				string yapilanIsTanimi = !string.IsNullOrWhiteSpace(kalemAdi)
					? kalemAdi
					: YapilanIsTanimiMetniGetir(yapilanIsKaydi);

				return new KalemSecimBilgisi
				{
					YapilanIsMi=true,
					YapilanIsId=yapilanIsKaydi!=null ? (int?)yapilanIsKaydi.YapilanIsId : yapilanIsId,
					KalemAdi=string.IsNullOrWhiteSpace(kalemAdi) ? ( yapilanIsKaydi?.KalemGosterimAdi??string.Empty ) : kalemAdi,
					Birim=string.IsNullOrWhiteSpace(birim) ? ( string.IsNullOrWhiteSpace(yapilanIsKaydi?.Birim) ? VarsayilanYapilanIsBirimi : yapilanIsKaydi.Birim ) : birim,
					IsBilgisi=yapilanIsTanimi,
					Adet=SatirKolonuVarMi(row , "KalemAdet")&&SepetDecimalParse(Convert.ToString(row.Cells["KalemAdet"].Value))>0
						? SepetDecimalParse(Convert.ToString(row.Cells["KalemAdet"].Value))
						: ( yapilanIsKaydi?.Adet>0 ? yapilanIsKaydi.Adet : 1m ),
					Miktar=miktar,
					BirimFiyat=birimFiyat<=0 ? ( yapilanIsKaydi?.Fiyat??0m ) : birimFiyat
				};
			}

			int? urunId = SatirdanIntGetir(row , "UrunID");
			UrunAramaKaydi urunKaydi = urunId.HasValue
				? UrunKaydiniGetir(conn , tx , urunId.Value)
				: null;
			if(urunKaydi==null&&!string.IsNullOrWhiteSpace(kalemAdi))
				urunKaydi=EnUygunUrunKaydiniBul(conn , tx , kalemAdi);
			if(urunKaydi==null)
				return null;

			return new KalemSecimBilgisi
			{
				YapilanIsMi=false,
				UrunId=urunKaydi.UrunId,
				KalemAdi=string.IsNullOrWhiteSpace(kalemAdi) ? ( string.IsNullOrWhiteSpace(urunKaydi.UrunAdi) ? urunKaydi.UrunGosterimAdi : urunKaydi.UrunAdi ) : kalemAdi,
				Birim=string.IsNullOrWhiteSpace(birim) ? urunKaydi.BirimAdi : birim,
				Miktar=miktar,
				BirimFiyat=birimFiyat
			};
		}

		private KalemSecimBilgisi BelgedenKalemBilgisiCoz ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel )
		{
			if(conn==null||panel==null)
				return null;

			string yapilanIsMetni = panel.YapilanIsComboBox?.Text?.Trim()??string.Empty;
			if(panel.SeciliYapilanIsId.HasValue||!string.IsNullOrWhiteSpace(yapilanIsMetni))
			{
				YapilanIsKaydi yapilanIsKaydi = panel.SeciliYapilanIsId.HasValue
					? YapilanIsKaydiniGetir(conn , tx , panel.SeciliYapilanIsId.Value)
					: null;
				if(yapilanIsKaydi==null&&!string.IsNullOrWhiteSpace(yapilanIsMetni))
					yapilanIsKaydi=EnUygunYapilanIsKaydiniBul(conn , tx , yapilanIsMetni);
				if(yapilanIsKaydi==null)
					return null;

				string yapilanIsTanimi = YapilanIsTanimiMetniGetir(yapilanIsKaydi);

				return new KalemSecimBilgisi
				{
					YapilanIsMi=true,
					YapilanIsId=yapilanIsKaydi.YapilanIsId,
					KalemAdi=yapilanIsKaydi.KalemGosterimAdi,
					Birim=string.IsNullOrWhiteSpace(panel.BirimTextBox?.Text) ? ( string.IsNullOrWhiteSpace(yapilanIsKaydi.Birim) ? VarsayilanYapilanIsBirimi : yapilanIsKaydi.Birim ) : panel.BirimTextBox.Text.Trim(),
					IsBilgisi=string.IsNullOrWhiteSpace(panel.YapilanIsBilgiTextBox?.Text) ? yapilanIsTanimi : panel.YapilanIsBilgiTextBox.Text.Trim(),
					Adet=SepetDecimalParse(panel.YapilanIsAdetTextBox?.Text)<=0 ? ( yapilanIsKaydi.Adet<=0 ? 1m : yapilanIsKaydi.Adet ) : SepetDecimalParse(panel.YapilanIsAdetTextBox?.Text),
					Miktar=SepetDecimalParse(panel.MiktarTextBox?.Text)<=0 ? ( yapilanIsKaydi.Miktar<=0 ? 1m : yapilanIsKaydi.Miktar ) : SepetDecimalParse(panel.MiktarTextBox?.Text),
					BirimFiyat=SepetDecimalParse(panel.BirimFiyatTextBox?.Text)<=0 ? yapilanIsKaydi.Fiyat : SepetDecimalParse(panel.BirimFiyatTextBox?.Text)
				};
			}

			int urunId;
			string urunAdi;
			string birimAdi;
			if(!BelgedenUrunBilgisiCoz(conn , tx , BelgeUrunMetniGetir(panel) , out urunId , out urunAdi , out birimAdi))
				return null;

			return new KalemSecimBilgisi
			{
				YapilanIsMi=false,
				UrunId=urunId,
				KalemAdi=urunAdi,
				Birim=string.IsNullOrWhiteSpace(panel.BirimTextBox?.Text) ? birimAdi : panel.BirimTextBox.Text.Trim(),
				Miktar=SepetDecimalParse(panel.MiktarTextBox?.Text)<=0 ? 1m : SepetDecimalParse(panel.MiktarTextBox?.Text),
				BirimFiyat=SepetDecimalParse(panel.BirimFiyatTextBox?.Text)
			};
		}

		private bool StokTakibiGerektirir ( KalemSecimBilgisi kalem )
		{
			return kalem!=null&&!kalem.YapilanIsMi&&kalem.UrunId.HasValue&&kalem.UrunId.Value>0&&kalem.Miktar>0m;
		}

		private string StokMiktariMetniGetir ( decimal miktar )
		{
			return miktar==decimal.Truncate(miktar)
				? miktar.ToString("N0" , _yazdirmaKulturu)
				: miktar.ToString("N2" , _yazdirmaKulturu);
		}

		private void UrunStokDegisimiUygula ( OleDbConnection conn , OleDbTransaction tx , int urunId , decimal stokDegisimi , string urunAdi )
		{
			if(conn==null||urunId<=0||stokDegisimi==0m)
				return;

			decimal mevcutStok = 0m;
			string mevcutUrunAdi = ( urunAdi??string.Empty ).Trim();

			using(OleDbCommand cmd = new OleDbCommand("SELECT StokMiktari, IIF(UrunAdi IS NULL, '', UrunAdi) AS UrunAdi FROM Urunler WHERE UrunID=?" , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd==null||!rd.Read())
						throw new InvalidOperationException("Stok güncellenecek ürün bulunamadı.");

					mevcutStok=rd["StokMiktari"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["StokMiktari"]);
					if(string.IsNullOrWhiteSpace(mevcutUrunAdi))
						mevcutUrunAdi=( rd["UrunAdi"]?.ToString()??string.Empty ).Trim();
				}
			}

			decimal yeniStok = mevcutStok+stokDegisimi;
			if(yeniStok<0m)
			{
				string gosterimAdi = string.IsNullOrWhiteSpace(mevcutUrunAdi) ? "Seçili ürün" : mevcutUrunAdi;
				throw new InvalidOperationException(
					gosterimAdi+" için yeterli stok yok. Mevcut stok: "+StokMiktariMetniGetir(mevcutStok)+
					", istenen miktar: "+StokMiktariMetniGetir(Math.Abs(stokDegisimi))+".");
			}

			using(OleDbCommand guncelle = new OleDbCommand("UPDATE Urunler SET StokMiktari=? WHERE UrunID=?" , conn , tx))
			{
				guncelle.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(yeniStok);
				guncelle.Parameters.Add("?" , OleDbType.Integer).Value=urunId;
				guncelle.ExecuteNonQuery();
			}
		}

		private void FaturaKalemiIcinStokDus ( OleDbConnection conn , OleDbTransaction tx , KalemSecimBilgisi kalem )
		{
			if(!StokTakibiGerektirir(kalem))
				return;

			UrunStokDegisimiUygula(
				conn ,
				tx ,
				kalem.UrunId.Value ,
				-kalem.Miktar ,
				string.IsNullOrWhiteSpace(kalem.KalemAdi) ? string.Empty : kalem.KalemAdi);
		}

		private void StokKaleminiIadeEt ( OleDbConnection conn , OleDbTransaction tx , StokKalemBilgisi kalem )
		{
			if(kalem==null||kalem.UrunId<=0||kalem.Miktar<=0m)
				return;

			UrunStokDegisimiUygula(conn , tx , kalem.UrunId , kalem.Miktar , kalem.UrunAdi);
		}

		private StokKalemBilgisi FaturaDetayStokKaleminiGetir ( OleDbConnection conn , OleDbTransaction tx , int faturaDetayId )
		{
			using(OleDbCommand cmd = new OleDbCommand(
				@"SELECT FD.UrunID,
						IIF(FD.Miktar IS NULL, 0, FD.Miktar) AS Miktar,
						IIF(U.UrunAdi IS NULL, '', U.UrunAdi) AS UrunAdi
					FROM FaturaDetay AS FD
					LEFT JOIN Urunler AS U ON CLng(IIF(FD.UrunID IS NULL, 0, FD.UrunID)) = U.UrunID
					WHERE FD.FaturaDetayID=?" ,
				conn ,
				tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=faturaDetayId;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd==null||!rd.Read()||rd["UrunID"]==DBNull.Value)
						return null;

					decimal miktar = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
					if(miktar<=0m)
						return null;

					return new StokKalemBilgisi
					{
						UrunId=Convert.ToInt32(rd["UrunID"]),
						UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
						Miktar=miktar
					};
				}
			}
		}

		private List<StokKalemBilgisi> FaturaStokKalemleriniGetir ( OleDbConnection conn , OleDbTransaction tx , int faturaId )
		{
			Dictionary<int, StokKalemBilgisi> sonuc = new Dictionary<int, StokKalemBilgisi>();

			using(OleDbCommand cmd = new OleDbCommand(
				@"SELECT FD.UrunID,
						IIF(FD.Miktar IS NULL, 0, FD.Miktar) AS Miktar,
						IIF(U.UrunAdi IS NULL, '', U.UrunAdi) AS UrunAdi
					FROM FaturaDetay AS FD
					LEFT JOIN Urunler AS U ON CLng(IIF(FD.UrunID IS NULL, 0, FD.UrunID)) = U.UrunID
					WHERE FD.FaturaID=?" ,
				conn ,
				tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=faturaId;
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						if(rd["UrunID"]==DBNull.Value)
							continue;

						int urunId = Convert.ToInt32(rd["UrunID"]);
						decimal miktar = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
						if(urunId<=0||miktar<=0m)
							continue;

						if(!sonuc.TryGetValue(urunId , out StokKalemBilgisi stokKalemi))
						{
							stokKalemi=new StokKalemBilgisi
							{
								UrunId=urunId,
								UrunAdi=Convert.ToString(rd["UrunAdi"])??string.Empty,
								Miktar=0m
							};
							sonuc.Add(urunId , stokKalemi);
						}

						stokKalemi.Miktar+=miktar;
					}
				}
			}

			return sonuc.Values.ToList();
		}

		private void StokDegisimindenSonraEkranlariYenile ()
		{
			try
			{
				Listele1();
			}
			catch
			{
			}

			try
			{
				AnaSayfaGridleriniYenile();
			}
			catch
			{
			}

			try
			{
				GunlukSatisUrunListesiniYenile();
			}
			catch
			{
			}
		}

		private void FaturaDetayKaleminiEkle ( OleDbConnection conn , OleDbTransaction tx , int faturaId , KalemSecimBilgisi kalem )
		{
			FaturaKalemiIcinStokDus(conn , tx , kalem);

			using(OleDbCommand cmdDetay = new OleDbCommand("INSERT INTO FaturaDetay (FaturaID, UrunID, YapilanIsID, KalemTuru, KalemAdi, Birim, IsBilgisi, Adet, Miktar, SatisFiyati) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)" , conn , tx))
			{
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=faturaId;
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=kalem.UrunId.HasValue ? (object)kalem.UrunId.Value : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=kalem.YapilanIsId.HasValue ? (object)kalem.YapilanIsId.Value : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=kalem.YapilanIsMi ? "YAPILAN İŞ" : "ÜRÜN";
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=kalem.KalemAdi??string.Empty;
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=BosMetniDbNullYap(kalem.Birim);
				cmdDetay.Parameters.Add("?" , OleDbType.LongVarWChar).Value=BosMetniDbNullYap(kalem.IsBilgisi);
				cmdDetay.Parameters.Add("?" , OleDbType.Double).Value=kalem.YapilanIsMi ? (object)Convert.ToDouble(kalem.Adet) : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(kalem.Miktar);
				cmdDetay.Parameters.Add("?" , OleDbType.Currency).Value=kalem.BirimFiyat;
				cmdDetay.ExecuteNonQuery();
			}
		}

		private void TeklifDetayKaleminiEkle ( OleDbConnection conn , OleDbTransaction tx , int teklifId , KalemSecimBilgisi kalem )
		{
			using(OleDbCommand cmdDetay = new OleDbCommand("INSERT INTO TeklifDetaylari (TeklifID, UrunID, YapilanIsID, KalemTuru, KalemAdi, Birim, IsBilgisi, Adet, Miktar, BirimFiyat, AraToplam) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)" , conn , tx))
			{
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=teklifId;
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=kalem.UrunId.HasValue ? (object)kalem.UrunId.Value : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.Integer).Value=kalem.YapilanIsId.HasValue ? (object)kalem.YapilanIsId.Value : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=kalem.YapilanIsMi ? "YAPILAN İŞ" : "ÜRÜN";
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=kalem.KalemAdi??string.Empty;
				cmdDetay.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=BosMetniDbNullYap(kalem.Birim);
				cmdDetay.Parameters.Add("?" , OleDbType.LongVarWChar).Value=BosMetniDbNullYap(kalem.IsBilgisi);
				cmdDetay.Parameters.Add("?" , OleDbType.Double).Value=kalem.YapilanIsMi ? (object)Convert.ToDouble(kalem.Adet) : DBNull.Value;
				cmdDetay.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(kalem.Miktar);
				cmdDetay.Parameters.Add("?" , OleDbType.Currency).Value=kalem.BirimFiyat;
				cmdDetay.Parameters.Add("?" , OleDbType.Currency).Value=kalem.Miktar*kalem.BirimFiyat;
				cmdDetay.ExecuteNonQuery();
			}
		}

		private void FaturaDetayKaleminiGuncelle ( OleDbConnection conn , OleDbTransaction tx , int faturaDetayId , KalemSecimBilgisi kalem )
		{
			using(OleDbCommand cmd = new OleDbCommand("UPDATE FaturaDetay SET UrunID=?, YapilanIsID=?, KalemTuru=?, KalemAdi=?, Birim=?, IsBilgisi=?, Adet=?, Miktar=?, SatisFiyati=? WHERE FaturaDetayID=?" , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kalem.UrunId.HasValue ? (object)kalem.UrunId.Value : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kalem.YapilanIsId.HasValue ? (object)kalem.YapilanIsId.Value : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=kalem.YapilanIsMi ? "YAPILAN İŞ" : "ÜRÜN";
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=kalem.KalemAdi??string.Empty;
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=BosMetniDbNullYap(kalem.Birim);
				cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=BosMetniDbNullYap(kalem.IsBilgisi);
				cmd.Parameters.Add("?" , OleDbType.Double).Value=kalem.YapilanIsMi ? (object)Convert.ToDouble(kalem.Adet) : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(kalem.Miktar);
				cmd.Parameters.Add("?" , OleDbType.Currency).Value=kalem.BirimFiyat;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=faturaDetayId;
				cmd.ExecuteNonQuery();
			}
		}

		private void TeklifDetayKaleminiGuncelle ( OleDbConnection conn , OleDbTransaction tx , int teklifDetayId , KalemSecimBilgisi kalem )
		{
			using(OleDbCommand cmd = new OleDbCommand("UPDATE TeklifDetaylari SET UrunID=?, YapilanIsID=?, KalemTuru=?, KalemAdi=?, Birim=?, IsBilgisi=?, Adet=?, Miktar=?, BirimFiyat=?, AraToplam=? WHERE TeklifDetayID=?" , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kalem.UrunId.HasValue ? (object)kalem.UrunId.Value : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kalem.YapilanIsId.HasValue ? (object)kalem.YapilanIsId.Value : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 50).Value=kalem.YapilanIsMi ? "YAPILAN İŞ" : "ÜRÜN";
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=kalem.KalemAdi??string.Empty;
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 100).Value=BosMetniDbNullYap(kalem.Birim);
				cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=BosMetniDbNullYap(kalem.IsBilgisi);
				cmd.Parameters.Add("?" , OleDbType.Double).Value=kalem.YapilanIsMi ? (object)Convert.ToDouble(kalem.Adet) : DBNull.Value;
				cmd.Parameters.Add("?" , OleDbType.Double).Value=Convert.ToDouble(kalem.Miktar);
				cmd.Parameters.Add("?" , OleDbType.Currency).Value=kalem.BirimFiyat;
				cmd.Parameters.Add("?" , OleDbType.Currency).Value=kalem.Miktar*kalem.BirimFiyat;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=teklifDetayId;
				cmd.ExecuteNonQuery();
			}
		}

		private void SepetUrunEkle_Click ( object sender , EventArgs e )
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					KalemSecimBilgisi kalem = SepetteSeciliKalemBilgisiniOlustur(conn , null);
					if(kalem==null)
					{
						MessageBox.Show("Lütfen ürün veya yapılan iş seçin!");
						return;
					}

					if(string.IsNullOrWhiteSpace(kalem.KalemAdi))
					{
						MessageBox.Show("Ürün veya yapılan iş adı boş olamaz!");
						return;
					}

					if(kalem.Miktar<=0)
					{
						MessageBox.Show("Miktar bilgisini girin!");
						return;
					}

					decimal toplam = kalem.Miktar*kalem.BirimFiyat;
					bool kalemBulundu = false;
					foreach(DataGridViewRow row in dataGridView5.Rows)
					{
						if(row.IsNewRow)
							continue;

						if(kalem.YapilanIsMi)
						{
							int? satirYapilanIsId = SatirdanIntGetir(row , "YapilanIsID");
							if(!satirYapilanIsId.HasValue||!kalem.YapilanIsId.HasValue||satirYapilanIsId.Value!=kalem.YapilanIsId.Value)
								continue;

							row.Cells["UrunID"].Value=DBNull.Value;
							row.Cells["YapilanIsID"].Value=kalem.YapilanIsId.Value;
							row.Cells["KalemTuru"].Value="YAPILAN İŞ";
							row.Cells["IsBilgisi"].Value=kalem.IsBilgisi??string.Empty;
							row.Cells["KalemAdet"].Value=kalem.Adet;
							row.Cells["urunadi"].Value=kalem.KalemAdi;
							row.Cells["marka"].Value=string.Empty;
							row.Cells["kategori"].Value=string.Empty;
							row.Cells["birim"].Value=kalem.Birim??VarsayilanYapilanIsBirimi;
							row.Cells["adet"].Value=kalem.Miktar;
							row.Cells["SatisFiyati"].Value=kalem.BirimFiyat;
							row.Cells["toplamfiyat"].Value=toplam;
							kalemBulundu=true;
							SepetSatirSec(row);
							break;
						}

						int? satirUrunId = SatirdanIntGetir(row , "UrunID");
						if(!satirUrunId.HasValue||!kalem.UrunId.HasValue||satirUrunId.Value!=kalem.UrunId.Value)
							continue;

						row.Cells["YapilanIsID"].Value=DBNull.Value;
						row.Cells["KalemTuru"].Value="ÜRÜN";
						row.Cells["IsBilgisi"].Value=DBNull.Value;
						row.Cells["KalemAdet"].Value=DBNull.Value;
						row.Cells["urunadi"].Value=kalem.KalemAdi;
						row.Cells["marka"].Value=_sepetMarka??string.Empty;
						row.Cells["kategori"].Value=_sepetKategori??string.Empty;
						row.Cells["birim"].Value=kalem.Birim??string.Empty;
						row.Cells["adet"].Value=kalem.Miktar;
						row.Cells["SatisFiyati"].Value=kalem.BirimFiyat;
						row.Cells["toplamfiyat"].Value=toplam;
						kalemBulundu=true;
						SepetSatirSec(row);
						break;
					}

					if(!kalemBulundu)
					{
						dataGridView5.Rows.Add(
							kalem.UrunId.HasValue ? (object)kalem.UrunId.Value : DBNull.Value ,
							kalem.YapilanIsId.HasValue ? (object)kalem.YapilanIsId.Value : DBNull.Value ,
							kalem.YapilanIsMi ? "YAPILAN İŞ" : "ÜRÜN" ,
							string.IsNullOrWhiteSpace(kalem.IsBilgisi) ? (object)DBNull.Value : kalem.IsBilgisi ,
							kalem.YapilanIsMi ? (object)kalem.Adet : DBNull.Value ,
							kalem.KalemAdi ,
							kalem.YapilanIsMi ? string.Empty : _sepetMarka??string.Empty ,
							kalem.YapilanIsMi ? string.Empty : _sepetKategori??string.Empty ,
							kalem.Birim??( kalem.YapilanIsMi ? VarsayilanYapilanIsBirimi : string.Empty ) ,
							kalem.Miktar ,
							kalem.BirimFiyat ,
							toplam);

						if(dataGridView5.Rows.Count>0)
						{
							DataGridViewRow eklenenSatir = dataGridView5.Rows
								.Cast<DataGridViewRow>()
								.LastOrDefault(r => !r.IsNewRow&&
									(kalem.YapilanIsMi
										? SatirdanIntGetir(r , "YapilanIsID")==kalem.YapilanIsId
										: SatirdanIntGetir(r , "UrunID")==kalem.UrunId));
							SepetSatirSec(eklenenSatir);
						}
					}
				}

				SepetGenelToplamHesapla();
			}
			catch(Exception ex)
			{
			MessageBox.Show("Ürün / hizmet ekleme hatası: "+ex.Message);
			}
		}

		private void SepetSeciliSatirSil_Click ( object sender , EventArgs e )
		{
			if(dataGridView5==null||dataGridView5.CurrentRow==null||dataGridView5.CurrentRow.IsNewRow)
			{
				MessageBox.Show("Silmek için bir satır seçin!");
				return;
			}

			dataGridView5.Rows.Remove(dataGridView5.CurrentRow);
			SepetGenelToplamHesapla();
		}

		private void SepetTemizle_Click ( object sender , EventArgs e )
		{
			if(dataGridView5!=null)
			{
				dataGridView5.Rows.Clear();
				dataGridView5.ClearSelection();
			}

			SepetUrunGirisTemizle();
			SepetYapilanIsSeciminiTemizle(true);
			if(textBox28!=null) textBox28.Clear();
			if(textBox29!=null) textBox29.Text="1";
			if(textBox30!=null) textBox30.Text="0,00";
			if(textBox32!=null) textBox32.Text="0,00";

			SepetCariGirisTemizle();
			if(textBox25!=null) textBox25.Clear();
			if(textBox26!=null) textBox26.Clear();

			_sepetCariId=null;
			_sepetUrunId=null;
			_sepetYapilanIsId=null;
			_sepetMarka=null;
			_sepetKategori=null;
			_sepetBirim=null;

			if(textBox39!=null) textBox39.Text="0,00";
			if(label46!=null) label46.Text="0,00";
			if(label48!=null) label48.Text="0,00";
			SepetCariUyariMetniniGoster();
		}

		private void SepetKaydet_Click ( object sender , EventArgs e )
		{
			if(dataGridView5==null||dataGridView5.Rows.Count==0||dataGridView5.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
			{
			MessageBox.Show("Sepet boş! Önce ürün veya hizmet ekleyin.");
				return;
			}

			SepetGenelToplamHesapla();
			decimal genelToplam = SepetDecimalParse(label46?.Text);
			decimal total = SepetToplamTutarHesapla(genelToplam);
			int kayitId = 0;
			BelgeKayitTuru hedefTur = BelgeKayitTuru.Teklif;
			int? sepetCariId = null;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							sepetCariId=SepetCariIdCoz(conn , tx);
							bool cariGirisiVar = SepetteCariGirisiVarMi();
							if(cariGirisiVar&&!sepetCariId.HasValue)
								throw new InvalidOperationException("Girilen cari bulunamadı. Lütfen cari adını kontrol edin.");

							bool faturaMi = sepetCariId.HasValue;
							if(faturaMi)
							{
								hedefTur=CariyeGoreBelgeTuruGetir(conn , tx , sepetCariId.Value);
								kayitId=SepetiFaturayaKaydet(conn , tx , sepetCariId.Value , total);
							}
							else
							{
								hedefTur=BelgeKayitTuru.Teklif;
								kayitId=SepetiTeklifeKaydet(conn , tx , total);
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

				if(_belgePanelleri.ContainsKey(hedefTur))
				{
					_belgePanelleri[hedefTur].SeciliKayitId=kayitId;
					_belgePanelleri[hedefTur].SeciliDetayId=null;
				}

				BelgeListeleriniYenile();
				if(sepetCariId.HasValue)
					StokDegisimindenSonraEkranlariYenile();
				SepetKayitSekmesiniAc(hedefTur);
				MessageBox.Show(sepetCariId.HasValue ? "Sepet girilen cariye gore faturalandirildi." : "Cari bilgisi olmadigi icin sepet teklife aktarildi.");
				SepetTemizle_Click(sender , e);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Sepet kaydetme hatası: "+ex.Message);
			}
		}

		private int FaturaBasligiOlustur ( OleDbConnection conn , OleDbTransaction tx , int cariId , decimal toplamTutar )
		{
			using(OleDbCommand cmdFatura = new OleDbCommand("INSERT INTO Faturalar (CariID, FaturaNo, FaturaTarihi, ToplamTutar, Durum) VALUES (?, ?, ?, ?, ?)" , conn , tx))
			{
				cmdFatura.Parameters.Add("?" , OleDbType.Integer).Value=cariId;
				cmdFatura.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=YeniBelgeNoUret("FTR");
				cmdFatura.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now;
				cmdFatura.Parameters.Add("?" , OleDbType.Currency).Value=toplamTutar;
				cmdFatura.Parameters.Add("?" , OleDbType.Boolean).Value=true;
				cmdFatura.ExecuteNonQuery();
			}

			int faturaId;
			using(OleDbCommand cmdId = new OleDbCommand("SELECT @@IDENTITY" , conn , tx))
				faturaId=Convert.ToInt32(cmdId.ExecuteScalar());

			return faturaId;
		}

		private int SepetiFaturayaKaydet ( OleDbConnection conn , OleDbTransaction tx , int cariId , decimal toplamTutar )
		{
			int faturaId = FaturaBasligiOlustur(conn , tx , cariId , toplamTutar);

			int detaySayisi = 0;
			foreach(DataGridViewRow row in dataGridView5.Rows)
			{
				if(row.IsNewRow) continue;

				KalemSecimBilgisi kalem = SepetSatirindanKalemBilgisiGetir(conn , tx , row);
				if(kalem==null||string.IsNullOrWhiteSpace(kalem.KalemAdi)||kalem.Miktar<=0)
					continue;

				FaturaDetayKaleminiEkle(conn , tx , faturaId , kalem);

				detaySayisi++;
			}

			if(detaySayisi==0)
					throw new InvalidOperationException("Kaydedilecek geçerli ürün veya hizmet bulunamadı.");

			return faturaId;
		}

		private int TeklifiFaturayaAktar ( OleDbConnection conn , OleDbTransaction tx , int teklifId , int cariId )
		{
			int faturaId = FaturaBasligiOlustur(conn , tx , cariId , 0m);
			int detaySayisi = 0;
			decimal toplamTutar = 0m;

			using(OleDbCommand cmdDetaylar = new OleDbCommand("SELECT UrunID, YapilanIsID, KalemTuru, KalemAdi, Birim, IsBilgisi, Adet, Miktar, BirimFiyat, AraToplam FROM TeklifDetaylari WHERE TeklifID=? ORDER BY TeklifDetayID" , conn , tx))
			{
				cmdDetaylar.Parameters.Add("?" , OleDbType.Integer).Value=teklifId;
				using(OleDbDataReader rd = cmdDetaylar.ExecuteReader())
				{
					while(rd!=null&&rd.Read())
					{
						decimal miktar = rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]);
						if(miktar<=0)
							continue;

						decimal birimFiyat = rd["BirimFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["BirimFiyat"]);
						KalemSecimBilgisi kalem = new KalemSecimBilgisi
						{
								YapilanIsMi=rd["YapilanIsID"]!=DBNull.Value||AramaMetniniNormalizeEt(Convert.ToString(rd["KalemTuru"])).Contains("yapilan is"),
							UrunId=rd["UrunID"]==DBNull.Value ? (int?)null : Convert.ToInt32(rd["UrunID"]),
							YapilanIsId=rd["YapilanIsID"]==DBNull.Value ? (int?)null : Convert.ToInt32(rd["YapilanIsID"]),
							KalemAdi=Convert.ToString(rd["KalemAdi"])??string.Empty,
							Birim=Convert.ToString(rd["Birim"])??string.Empty,
							IsBilgisi=Convert.ToString(rd["IsBilgisi"])??string.Empty,
							Adet=rd["Adet"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Adet"]),
							Miktar=miktar,
							BirimFiyat=birimFiyat
						};
						if(string.IsNullOrWhiteSpace(kalem.KalemAdi)&&!kalem.UrunId.HasValue&&!kalem.YapilanIsId.HasValue)
							continue;

						FaturaDetayKaleminiEkle(conn , tx , faturaId , kalem);

						toplamTutar+=rd["AraToplam"]==DBNull.Value ? miktar*birimFiyat : Convert.ToDecimal(rd["AraToplam"]);
						detaySayisi++;
					}
				}
			}

			if(detaySayisi==0)
					throw new InvalidOperationException("Aktarılacak geçerli teklif satırı bulunamadı.");

			using(OleDbCommand cmdToplam = new OleDbCommand("UPDATE Faturalar SET ToplamTutar=? WHERE FaturaID=?" , conn , tx))
			{
				cmdToplam.Parameters.Add("?" , OleDbType.Currency).Value=toplamTutar;
				cmdToplam.Parameters.Add("?" , OleDbType.Integer).Value=faturaId;
				cmdToplam.ExecuteNonQuery();
			}

			if(KolonVarMi(conn , "Teklifler" , "Ariza1")&&KolonVarMi(conn , "Faturalar" , "Ariza1"))
			{
				using(OleDbCommand cmdOku = new OleDbCommand("SELECT Ariza1, Ariza2, Ariza3, Ariza4 FROM Teklifler WHERE TeklifID=?" , conn , tx))
				{
					cmdOku.Parameters.Add("?" , OleDbType.Integer).Value=teklifId;
					using(OleDbDataReader rd = cmdOku.ExecuteReader())
					{
						if(rd!=null&&rd.Read())
						{
							using(OleDbCommand cmdYaz = new OleDbCommand("UPDATE Faturalar SET Ariza1=?, Ariza2=?, Ariza3=?, Ariza4=? WHERE FaturaID=?" , conn , tx))
							{
								for(int i = 0 ; i<4 ; i++)
								{
									object deger = rd["Ariza"+(i+1).ToString(_yazdirmaKulturu)];
									cmdYaz.Parameters.Add("?" , OleDbType.LongVarWChar).Value=deger==DBNull.Value ? (object)DBNull.Value : Convert.ToString(deger);
								}

								cmdYaz.Parameters.Add("?" , OleDbType.Integer).Value=faturaId;
								cmdYaz.ExecuteNonQuery();
							}
						}
					}
				}
			}

			using(OleDbCommand detaySil = new OleDbCommand("DELETE FROM TeklifDetaylari WHERE TeklifID=?" , conn , tx))
			{
				detaySil.Parameters.Add("?" , OleDbType.Integer).Value=teklifId;
				detaySil.ExecuteNonQuery();
			}

			using(OleDbCommand kayitSil = new OleDbCommand("DELETE FROM Teklifler WHERE TeklifID=?" , conn , tx))
			{
				kayitSil.Parameters.Add("?" , OleDbType.Integer).Value=teklifId;
				kayitSil.ExecuteNonQuery();
			}

			return faturaId;
		}

		private int SepetiTeklifeKaydet ( OleDbConnection conn , OleDbTransaction tx , decimal toplamTutar )
		{
			using(OleDbCommand cmdTeklif = new OleDbCommand("INSERT INTO Teklifler (CariID, TeklifNo, TeklifTarihi, GecerlilikTarihi, ToplamTutar, Durum) VALUES (?, ?, ?, ?, ?, ?)" , conn , tx))
			{
				cmdTeklif.Parameters.Add("?" , OleDbType.Integer).Value=DBNull.Value;
				cmdTeklif.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=YeniBelgeNoUret("TKL");
				cmdTeklif.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now;
				cmdTeklif.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now.AddDays(30);
				cmdTeklif.Parameters.Add("?" , OleDbType.Currency).Value=toplamTutar;
				cmdTeklif.Parameters.Add("?" , OleDbType.Boolean).Value=true;
				cmdTeklif.ExecuteNonQuery();
			}

			int teklifId;
			using(OleDbCommand cmdId = new OleDbCommand("SELECT @@IDENTITY" , conn , tx))
				teklifId=Convert.ToInt32(cmdId.ExecuteScalar());

			int detaySayisi = 0;
			foreach(DataGridViewRow row in dataGridView5.Rows)
			{
				if(row.IsNewRow) continue;

				KalemSecimBilgisi kalem = SepetSatirindanKalemBilgisiGetir(conn , tx , row);
				if(kalem==null||string.IsNullOrWhiteSpace(kalem.KalemAdi)||kalem.Miktar<=0)
					continue;

				TeklifDetayKaleminiEkle(conn , tx , teklifId , kalem);

				detaySayisi++;
			}

			if(detaySayisi==0)
					throw new InvalidOperationException("Kaydedilecek geçerli ürün veya hizmet bulunamadı.");

			return teklifId;
		}

		private string YeniBelgeNoUret ( string onEk )
		{
			return onEk+"-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
		}

		private BelgeKayitTuru CariyeGoreBelgeTuruGetir ( OleDbConnection conn , OleDbTransaction tx , int cariId )
		{
			string sorgu = @"SELECT TOP 1 C.CariTipID, IIF(T.TipAdi IS NULL, '', T.TipAdi) AS TipAdi
							FROM Cariler AS C
							LEFT JOIN CariTipi AS T ON CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = T.CariTipID
							WHERE C.CariID = ?";

			int? cariTipId = null;
			string tipAdi = string.Empty;
			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , cariId);
				using(OleDbDataReader rd = cmd.ExecuteReader())
				{
					if(rd!=null&&rd.Read())
					{
						if(rd["CariTipID"]!=DBNull.Value)
							cariTipId=Convert.ToInt32(rd["CariTipID"]);
						tipAdi=rd["TipAdi"]?.ToString()??string.Empty;
					}
				}
			}

			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.FabrikaFaturasi , out BelgePaneli fabrikaPaneli)&&
				fabrikaPaneli.CariTipId.HasValue&&cariTipId==fabrikaPaneli.CariTipId)
				return BelgeKayitTuru.FabrikaFaturasi;
			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.SucuFaturasi , out BelgePaneli sucuPaneli)&&
				sucuPaneli.CariTipId.HasValue&&cariTipId==sucuPaneli.CariTipId)
				return BelgeKayitTuru.SucuFaturasi;
			if(_belgePanelleri.TryGetValue(BelgeKayitTuru.MusteriFaturasi , out BelgePaneli musteriPaneli)&&
				musteriPaneli.CariTipId.HasValue&&cariTipId==musteriPaneli.CariTipId)
				return BelgeKayitTuru.MusteriFaturasi;

			string buyukTip = KarsilastirmaMetniHazirla(tipAdi);
			if(buyukTip.Contains("FABR"))
				return BelgeKayitTuru.FabrikaFaturasi;
			if(buyukTip.Contains("SUCU"))
				return BelgeKayitTuru.SucuFaturasi;

			return BelgeKayitTuru.MusteriFaturasi;
		}

		private void SepetKayitSekmesiniAc ( BelgeKayitTuru tur )
		{
			if(tabControl1!=null&&tabPage4!=null)
				tabControl1.SelectedTab=tabPage4;

			if(tabControl6==null)
				return;

			switch(tur)
			{
				case BelgeKayitTuru.FabrikaFaturasi:
					if(tabPage20!=null)
						tabControl6.SelectedTab=tabPage20;
					break;
				case BelgeKayitTuru.SucuFaturasi:
					if(tabPage21!=null)
						tabControl6.SelectedTab=tabPage21;
					break;
				case BelgeKayitTuru.MusteriFaturasi:
					if(tabPage23!=null)
						tabControl6.SelectedTab=tabPage23;
					break;
				default:
					if(tabPage22!=null)
						tabControl6.SelectedTab=tabPage22;
					else if(tabPage19!=null)
						tabControl6.SelectedTab=tabPage19;
					break;
			}
		}

		private void BelgePanelleriniHazirla ()
		{
			if(_belgePanelleriHazir) return;

			_belgePanelleri.Clear();
			_belgePanelleri[BelgeKayitTuru.FabrikaFaturasi]=new BelgePaneli
			{
				Tur=BelgeKayitTuru.FabrikaFaturasi,
				CariTipAdi="Fabrika",
				UstGrid=dataGridView15,
				AltGrid=dataGridView14,
				AramaKutusu=textBox74,
				CariAdTextBox=textBox59,
				CariTcTextBox=textBox60,
				CariTelefonTextBox=textBox61,
				UrunAdiTextBox=textBox43,
				BirimTextBox=textBox44,
				MiktarTextBox=textBox51,
				BirimFiyatTextBox=textBox52,
				ToplamFiyatTextBox=textBox58,
				ArizaTextBoxlari=new[] { textBox37, textBox40, textBox41, textBox42 },
				ArizaLabellari=new[] { label123, label122, label121, label103 },
				KaydetButonu=button40,
				SatirSilButonu=button38,
				KayitSilButonu=button39,
				GuncelleButonu=button37,
				YazdirButonu=button36,
				PdfButonu=button35,
				ExcelButonu=button34,
				OzetHost=groupBox29,
				ToplamLabel=label119
			};

			_belgePanelleri[BelgeKayitTuru.SucuFaturasi]=new BelgePaneli
			{
				Tur=BelgeKayitTuru.SucuFaturasi,
				CariTipAdi="Sucu",
				UstGrid=dataGridView17,
				AltGrid=dataGridView21,
				AramaKutusu=textBox77,
				CariAdTextBox=textBox72,
				CariTcTextBox=textBox75,
				CariTelefonTextBox=textBox76,
				UrunAdiTextBox=textBox67,
				BirimTextBox=textBox68,
				MiktarTextBox=textBox69,
				BirimFiyatTextBox=textBox70,
				ToplamFiyatTextBox=textBox71,
				ArizaTextBoxlari=new[] { textBox62, textBox63, textBox64, textBox66 },
				ArizaLabellari=new[] { label154, label153, label152, label151 },
				KaydetButonu=button63,
				SatirSilButonu=button61,
				KayitSilButonu=button62,
				GuncelleButonu=button60,
				YazdirButonu=button59,
				PdfButonu=button41,
				ExcelButonu=button25,
				OzetHost=groupBox46,
				ToplamLabel=label149
			};

			_belgePanelleri[BelgeKayitTuru.MusteriFaturasi]=new BelgePaneli
			{
				Tur=BelgeKayitTuru.MusteriFaturasi,
				CariTipAdi="Müşteri",
				UstGrid=dataGridView22,
				AltGrid=dataGridView23,
				AramaKutusu=textBox97,
				CariAdTextBox=textBox94,
				CariTcTextBox=textBox95,
				CariTelefonTextBox=textBox96,
				UrunAdiTextBox=textBox89,
				BirimTextBox=textBox90,
				MiktarTextBox=textBox91,
				BirimFiyatTextBox=textBox92,
				ToplamFiyatTextBox=textBox93,
				ArizaTextBoxlari=new[] { textBox78, textBox79, textBox80, textBox81 },
				ArizaLabellari=new[] { label187, label186, label181, label175 },
				KaydetButonu=button73,
				SatirSilButonu=button71,
				KayitSilButonu=button72,
				GuncelleButonu=button70,
				YazdirButonu=button69,
				PdfButonu=button68,
				ExcelButonu=button64,
				OzetHost=groupBox49,
				ToplamLabel=label173
			};

			_belgePanelleri[BelgeKayitTuru.Teklif]=new BelgePaneli
			{
				Tur=BelgeKayitTuru.Teklif,
				CariTipAdi="Teklif",
				UstGrid=dataGridView7,
				AltGrid=dataGridView24,
				AramaKutusu=textBox98,
				CariAdTextBox=textBox48,
				CariTcTextBox=textBox49,
				CariTelefonTextBox=textBox50,
				UrunAdiTextBox=textBox22,
				BirimTextBox=textBox33,
				MiktarTextBox=textBox45,
				BirimFiyatTextBox=textBox46,
				ToplamFiyatTextBox=textBox47,
				ArizaTextBoxlari=new[] { textBox13, textBox14, textBox20, textBox21 },
				ArizaLabellari=new[] { label54, label53, label26, label25 },
				KaydetButonu=button67,
				SatirSilButonu=button65,
				KayitSilButonu=button66,
				AktarButonu=button24,
				GuncelleButonu=button74,
				YazdirButonu=button23,
				PdfButonu=button22,
				ExcelButonu=button21,
				OzetHost=groupBox34
			};

			BelgeCariTipKimlikleriniYukle();

			label120.Text="GÜNCELLE";
			label150.Text="GÜNCELLE";
			label174.Text="GÜNCELLE";
			label85.Text="SEPETİ AKTAR";

			foreach(BelgePaneli panel in _belgePanelleri.Values)
			{
				BelgeOzetKontrolleriniHazirla(panel);
				BelgePaneliniHazirla(panel);
			}

			_belgePanelleriHazir=true;
			BelgeListeleriniYenile();
		}

		private void BelgeCariTipKimlikleriniYukle ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("SELECT CariTipID, TipAdi FROM CariTipi" , conn))
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							if(rd["CariTipID"]==DBNull.Value)
								continue;

							int cariTipId = Convert.ToInt32(rd["CariTipID"]);
							string tipAdi = KarsilastirmaMetniHazirla(rd["TipAdi"]?.ToString());
							if(tipAdi.Contains("FABR")&&_belgePanelleri.ContainsKey(BelgeKayitTuru.FabrikaFaturasi))
								_belgePanelleri[BelgeKayitTuru.FabrikaFaturasi].CariTipId=cariTipId;
							else if(tipAdi.Contains("SUCU")&&_belgePanelleri.ContainsKey(BelgeKayitTuru.SucuFaturasi))
								_belgePanelleri[BelgeKayitTuru.SucuFaturasi].CariTipId=cariTipId;
							else if(tipAdi.Contains("MUST")&&_belgePanelleri.ContainsKey(BelgeKayitTuru.MusteriFaturasi))
								_belgePanelleri[BelgeKayitTuru.MusteriFaturasi].CariTipId=cariTipId;
						}
					}
				}
			}
			catch
			{
				foreach(BelgePaneli panel in _belgePanelleri.Values)
					panel.CariTipId=null;
			}
		}

		private void BelgeOzetKontrolleriniHazirla ( BelgePaneli panel )
		{
			if(panel==null||panel.OzetHost==null)
				return;

			Label toplamBaslik = panel.OzetHost.Controls
				.OfType<Label>()
				.FirstOrDefault(l => !ReferenceEquals(l , panel.ToplamLabel)&&
					string.Equals(( l.Text??string.Empty ).Trim() , "TOPLAM :" , StringComparison.OrdinalIgnoreCase));
			if(toplamBaslik==null)
			{
				toplamBaslik=new Label
				{
					Name=BelgeOzetKontrolAdiGetir(panel , "ToplamBaslik")
				};
				panel.OzetHost.Controls.Add(toplamBaslik);
			}

			if(panel.ToplamLabel==null)
			{
				panel.ToplamLabel=new Label
				{
					Name=BelgeOzetKontrolAdiGetir(panel , "ToplamDeger")
				};
				panel.OzetHost.Controls.Add(panel.ToplamLabel);
			}

			Label kdvBaslik = BelgeOzetKontrolunuGetir<Label>(panel.OzetHost , BelgeOzetKontrolAdiGetir(panel , "KdvBaslik"));
			if(kdvBaslik==null)
			{
				kdvBaslik=new Label
				{
					Name=BelgeOzetKontrolAdiGetir(panel , "KdvBaslik")
				};
				panel.OzetHost.Controls.Add(kdvBaslik);
			}

			if(panel.KdvTextBox==null)
			{
				panel.KdvTextBox=BelgeOzetKontrolunuGetir<TextBox>(panel.OzetHost , BelgeOzetKontrolAdiGetir(panel , "KdvDeger"));
				if(panel.KdvTextBox==null)
				{
					panel.KdvTextBox=new TextBox
					{
						Name=BelgeOzetKontrolAdiGetir(panel , "KdvDeger")
					};
					panel.OzetHost.Controls.Add(panel.KdvTextBox);
				}
			}

			Label totalBaslik = BelgeOzetKontrolunuGetir<Label>(panel.OzetHost , BelgeOzetKontrolAdiGetir(panel , "TotalBaslik"));
			if(totalBaslik==null)
			{
				totalBaslik=new Label
				{
					Name=BelgeOzetKontrolAdiGetir(panel , "TotalBaslik")
				};
				panel.OzetHost.Controls.Add(totalBaslik);
			}

			if(panel.TotalLabel==null)
			{
				panel.TotalLabel=BelgeOzetKontrolunuGetir<Label>(panel.OzetHost , BelgeOzetKontrolAdiGetir(panel , "TotalDeger"));
				if(panel.TotalLabel==null)
				{
					panel.TotalLabel=new Label
					{
						Name=BelgeOzetKontrolAdiGetir(panel , "TotalDeger")
					};
					panel.OzetHost.Controls.Add(panel.TotalLabel);
				}
			}

			BelgeOzetEtiketStiliUygula(toplamBaslik , label45 , "TOPLAM :");
			BelgeOzetDegerStiliUygula(panel.ToplamLabel , label46);
			BelgeOzetEtiketStiliUygula(kdvBaslik , label47 , "KDV (%) :");
			BelgeOzetKdvKutusuStiliUygula(panel.KdvTextBox , textBox39);
			BelgeOzetEtiketStiliUygula(totalBaslik , label49 , "TOTAL :");
			BelgeOzetDegerStiliUygula(panel.TotalLabel , label48);
			toplamBaslik.BringToFront();
			panel.ToplamLabel.BringToFront();
			kdvBaslik.BringToFront();
			panel.KdvTextBox.BringToFront();
			totalBaslik.BringToFront();
			panel.TotalLabel.BringToFront();
			BelgeOzetBilgileriniGuncelle(panel , 0m , 0m);
		}

		private string BelgeOzetKontrolAdiGetir ( BelgePaneli panel , string kontrolAdi )
		{
			return "BelgeOzet_"+panel.Tur+"_"+kontrolAdi;
		}

		private T BelgeOzetKontrolunuGetir<T> ( Control host , string kontrolAdi ) where T : Control
		{
			if(host==null||string.IsNullOrWhiteSpace(kontrolAdi))
				return null;

			return host.Controls.Find(kontrolAdi , false).OfType<T>().FirstOrDefault();
		}

		private void BelgeOzetEtiketStiliUygula ( Label etiket , Label kaynak , string metin )
		{
			if(etiket==null)
				return;

			if(kaynak!=null)
			{
				etiket.AutoSize=kaynak.AutoSize;
				etiket.Font=kaynak.Font;
				etiket.ForeColor=kaynak.ForeColor;
				etiket.Location=kaynak.Location;
				etiket.Margin=kaynak.Margin;
				etiket.Anchor=kaynak.Anchor;
				etiket.TextAlign=kaynak.TextAlign;
				etiket.BackColor=kaynak.BackColor;
			}
			else
			{
				etiket.AutoSize=true;
				etiket.Font=new Font("Microsoft Sans Serif" , 9f , FontStyle.Bold , GraphicsUnit.Point , ( (byte)( 162 ) ));
				etiket.ForeColor=SystemColors.ActiveCaptionText;
				etiket.Location=new Point(1228 , 45);
				etiket.Margin=new Padding(4 , 0 , 4 , 0);
			}

			etiket.Text=metin;
		}

		private void BelgeOzetDegerStiliUygula ( Label etiket , Label kaynak )
		{
			if(etiket==null)
				return;

			if(kaynak!=null)
			{
				etiket.AutoSize=kaynak.AutoSize;
				etiket.Font=kaynak.Font;
				etiket.ForeColor=kaynak.ForeColor;
				etiket.Location=kaynak.Location;
				etiket.Margin=kaynak.Margin;
				etiket.Anchor=kaynak.Anchor;
				etiket.TextAlign=kaynak.TextAlign;
				etiket.BackColor=kaynak.BackColor;
			}
			else
			{
				etiket.AutoSize=true;
				etiket.Font=new Font("Microsoft Sans Serif" , 10.2f , FontStyle.Bold , GraphicsUnit.Point , ( (byte)( 162 ) ));
				etiket.ForeColor=SystemColors.ActiveCaptionText;
				etiket.Location=new Point(1347 , 45);
				etiket.Margin=new Padding(4 , 0 , 4 , 0);
			}

			if(string.IsNullOrWhiteSpace(etiket.Text))
				etiket.Text="0,00";
		}

		private void BelgeOzetKdvKutusuStiliUygula ( TextBox kutu , TextBox kaynak )
		{
			if(kutu==null)
				return;

			if(kaynak!=null)
			{
				kutu.BackColor=kaynak.BackColor;
				kutu.Font=kaynak.Font;
				kutu.ForeColor=kaynak.ForeColor;
				kutu.Location=kaynak.Location;
				kutu.Margin=kaynak.Margin;
				kutu.Multiline=kaynak.Multiline;
				kutu.Size=kaynak.Size;
				kutu.Anchor=kaynak.Anchor;
				kutu.TextAlign=kaynak.TextAlign;
				kutu.BorderStyle=kaynak.BorderStyle;
			}
			else
			{
				kutu.BackColor=SystemColors.Control;
				kutu.Font=new Font("Microsoft Sans Serif" , 9f , FontStyle.Regular , GraphicsUnit.Point , ( (byte)( 162 ) ));
				kutu.ForeColor=SystemColors.ActiveCaptionText;
				kutu.Location=new Point(1351 , 79);
				kutu.Margin=new Padding(4);
				kutu.Multiline=true;
				kutu.Size=new Size(121 , 30);
			}

			if(string.IsNullOrWhiteSpace(kutu.Text))
				kutu.Text="0,00";
		}

		private void BelgeOzetBilgileriniGuncelle ( BelgePaneli panel , decimal araToplam , decimal genelToplam )
		{
			if(panel==null)
				return;

			panel.OzetBilgisiGuncelleniyor=true;
			try
			{
				if(panel.ToplamLabel!=null)
					panel.ToplamLabel.Text=araToplam.ToString("N2");

				decimal kdvOrani = BelgeKdvOraniHesapla(araToplam , genelToplam);
				if(panel.KdvTextBox!=null)
					panel.KdvTextBox.Text=kdvOrani.ToString("N2");

				if(panel.TotalLabel!=null)
					panel.TotalLabel.Text=genelToplam.ToString("N2");

				panel.HeaderToplamTutar=genelToplam;
			}
			finally
			{
				panel.OzetBilgisiGuncelleniyor=false;
			}
		}

		private decimal BelgeKdvOraniHesapla ( decimal araToplam , decimal genelToplam )
		{
			if(araToplam<=0||genelToplam<=araToplam)
				return 0m;

			return ( ( genelToplam-araToplam ) /araToplam ) *100m;
		}

		private decimal BelgeKdvOraniGetir ( BelgePaneli panel )
		{
			return SepetDecimalParse(panel?.KdvTextBox?.Text);
		}

		private decimal BelgeToplamTutarHesapla ( BelgePaneli panel , decimal araToplam )
		{
			decimal kdvOrani = BelgeKdvOraniGetir(panel);
			if(araToplam<=0||kdvOrani<=0)
				return araToplam;

			return araToplam+( araToplam*kdvOrani/100m );
		}

		private decimal BelgeOzetAraToplaminiGetir ( BelgePaneli panel )
		{
			return SepetDecimalParse(panel?.ToplamLabel?.Text);
		}

		private decimal BelgeDetayGridindenAraToplamHesapla ( BelgePaneli panel )
		{
			if(panel?.AltGrid==null||!panel.AltGrid.Columns.Contains("ToplamFiyat"))
				return 0m;

			decimal araToplam = 0m;
			foreach(DataGridViewRow row in panel.AltGrid.Rows)
			{
				if(row.IsNewRow)
					continue;

				araToplam+=SepetDecimalParse(Convert.ToString(row.Cells["ToplamFiyat"].Value));
			}

			return araToplam;
		}

		private void BelgePaneliniHazirla ( BelgePaneli panel )
		{
			if(panel==null) return;

			BelgeAranabilirAlanlariHazirla(panel);
			if(panel.ArizaTextBoxlari!=null)
			{
				panel.YapilanIsBilgiTextBox=panel.ArizaTextBoxlari.Length>1 ? panel.ArizaTextBoxlari[1] : null;
				panel.YapilanIsAdetTextBox=panel.ArizaTextBoxlari.Length>2 ? panel.ArizaTextBoxlari[2] : null;
				panel.YapilanIsFiyatTextBox=panel.ArizaTextBoxlari.Length>3 ? panel.ArizaTextBoxlari[3] : null;
			}
			if(panel.ArizaLabellari!=null&&panel.ArizaLabellari.Length>=4)
			{
				panel.ArizaLabellari[0].Text="YAPILAN İŞ :";
				panel.ArizaLabellari[1].Text="İŞ BİLGİSİ :";
				panel.ArizaLabellari[2].Text="ADET :";
				panel.ArizaLabellari[3].Text="SATIŞ FİYATI :";
			}

			if(panel.UstGrid!=null)
			{
				DatagridviewSetting(panel.UstGrid);
				panel.UstGrid.MultiSelect=false;
				panel.UstGrid.Tag=panel;
				panel.UstGrid.CellClick-=BelgeUstGrid_CellClick;
				panel.UstGrid.CellClick+=BelgeUstGrid_CellClick;
			}

			if(panel.AltGrid!=null)
			{
				DatagridviewSetting(panel.AltGrid);
				panel.AltGrid.MultiSelect=false;
				panel.AltGrid.Tag=panel;
				panel.AltGrid.CellClick-=BelgeAltGrid_CellClick;
				panel.AltGrid.CellClick+=BelgeAltGrid_CellClick;
			}

			if(panel.AramaKutusu!=null)
			{
				panel.AramaKutusu.Tag=panel;
				panel.AramaKutusu.TextChanged-=BelgeArama_TextChanged;
				panel.AramaKutusu.TextChanged+=BelgeArama_TextChanged;
			}

			if(panel.KdvTextBox!=null)
			{
				panel.KdvTextBox.Tag=panel;
				panel.KdvTextBox.TextChanged-=BelgeKdv_TextChanged;
				panel.KdvTextBox.TextChanged+=BelgeKdv_TextChanged;
				panel.KdvTextBox.Leave-=BelgeKdv_Leave;
				panel.KdvTextBox.Leave+=BelgeKdv_Leave;
				panel.KdvTextBox.KeyPress-=SepetSayisal_KeyPress;
				panel.KdvTextBox.KeyPress+=SepetSayisal_KeyPress;
			}

			if(panel.CariAdComboBox!=null)
			{
				panel.CariAdComboBox.Tag=panel;
				panel.CariAdComboBox.TextChanged-=BelgeCari_TextChanged;
				panel.CariAdComboBox.TextChanged+=BelgeCari_TextChanged;
				panel.CariAdComboBox.DropDown-=BelgeCariComboBox_DropDown;
				panel.CariAdComboBox.DropDown+=BelgeCariComboBox_DropDown;
			}

			if(panel.CariTcTextBox!=null)
			{
				panel.CariTcTextBox.ReadOnly=true;
				panel.CariTcTextBox.TabStop=false;
			}

			if(panel.CariTelefonTextBox!=null)
			{
				panel.CariTelefonTextBox.ReadOnly=true;
				panel.CariTelefonTextBox.TabStop=false;
			}

			if(panel.UrunAdiComboBox!=null)
			{
				panel.UrunAdiComboBox.Tag=panel;
				panel.UrunAdiComboBox.TextChanged-=BelgeUrun_TextChanged;
				panel.UrunAdiComboBox.TextChanged+=BelgeUrun_TextChanged;
				panel.UrunAdiComboBox.DropDown-=BelgeUrunComboBox_DropDown;
				panel.UrunAdiComboBox.DropDown+=BelgeUrunComboBox_DropDown;
				panel.UrunAdiComboBox.SelectionChangeCommitted-=BelgeUrunComboBox_SelectionChangeCommitted;
				panel.UrunAdiComboBox.SelectionChangeCommitted+=BelgeUrunComboBox_SelectionChangeCommitted;
				panel.UrunAdiComboBox.KeyDown-=BelgeUrunComboBox_KeyDown;
				panel.UrunAdiComboBox.KeyDown+=BelgeUrunComboBox_KeyDown;
			}
			if(panel.YapilanIsComboBox!=null)
			{
				panel.YapilanIsComboBox.Tag=panel;
				panel.YapilanIsComboBox.TextChanged-=BelgeYapilanIs_TextChanged;
				panel.YapilanIsComboBox.TextChanged+=BelgeYapilanIs_TextChanged;
				panel.YapilanIsComboBox.DropDown-=BelgeYapilanIsComboBox_DropDown;
				panel.YapilanIsComboBox.DropDown+=BelgeYapilanIsComboBox_DropDown;
			}

			if(panel.BirimTextBox!=null)
			{
				panel.BirimTextBox.ReadOnly=true;
				panel.BirimTextBox.TabStop=false;
			}

			if(panel.MiktarTextBox!=null)
			{
				panel.MiktarTextBox.Tag=panel;
				panel.MiktarTextBox.TextChanged-=BelgeSayisal_TextChanged;
				panel.MiktarTextBox.TextChanged+=BelgeSayisal_TextChanged;
				panel.MiktarTextBox.KeyPress-=SepetSayisal_KeyPress;
				panel.MiktarTextBox.KeyPress+=SepetSayisal_KeyPress;
			}

			if(panel.BirimFiyatTextBox!=null)
			{
				panel.BirimFiyatTextBox.Tag=panel;
				panel.BirimFiyatTextBox.TextChanged-=BelgeSayisal_TextChanged;
				panel.BirimFiyatTextBox.TextChanged+=BelgeSayisal_TextChanged;
				panel.BirimFiyatTextBox.KeyPress-=SepetSayisal_KeyPress;
				panel.BirimFiyatTextBox.KeyPress+=SepetSayisal_KeyPress;
			}

			if(panel.ToplamFiyatTextBox!=null)
			{
				panel.ToplamFiyatTextBox.ReadOnly=true;
				panel.ToplamFiyatTextBox.TabStop=false;
			}
			if(panel.YapilanIsBilgiTextBox!=null)
			{
				panel.YapilanIsBilgiTextBox.ReadOnly=true;
				panel.YapilanIsBilgiTextBox.TabStop=false;
				panel.YapilanIsBilgiTextBox.BackColor=SystemColors.ControlLight;
			}
			if(panel.YapilanIsAdetTextBox!=null)
			{
				panel.YapilanIsAdetTextBox.Text="1";
				panel.YapilanIsAdetTextBox.ReadOnly=true;
				panel.YapilanIsAdetTextBox.TabStop=false;
				panel.YapilanIsAdetTextBox.BackColor=SystemColors.ControlLight;
				panel.YapilanIsAdetTextBox.Visible=false;
				panel.YapilanIsAdetTextBox.Enabled=false;
			}
			if(panel.YapilanIsFiyatTextBox!=null)
			{
				panel.YapilanIsFiyatTextBox.ReadOnly=true;
				panel.YapilanIsFiyatTextBox.TabStop=false;
				panel.YapilanIsFiyatTextBox.BackColor=SystemColors.ControlLight;
				panel.YapilanIsFiyatTextBox.Visible=false;
				panel.YapilanIsFiyatTextBox.Enabled=false;
			}
			if(panel.ArizaLabellari!=null&&panel.ArizaLabellari.Length>=4)
			{
				panel.ArizaLabellari[0].Text="İŞ ADI :";
				panel.ArizaLabellari[1].Text="İŞ BİLGİSİ :";
				panel.ArizaLabellari[2].Visible=false;
				panel.ArizaLabellari[3].Visible=false;
			}

			if(panel.KaydetButonu!=null)
			{
				panel.KaydetButonu.Tag=panel;
				panel.KaydetButonu.Click-=BelgeKaydetButonu_Click;
				panel.KaydetButonu.Click+=BelgeKaydetButonu_Click;
			}

			if(panel.SatirSilButonu!=null)
			{
				panel.SatirSilButonu.Tag=panel;
				panel.SatirSilButonu.Click-=BelgeDetaySilButonu_Click;
				panel.SatirSilButonu.Click+=BelgeDetaySilButonu_Click;
			}

			if(panel.KayitSilButonu!=null)
			{
				panel.KayitSilButonu.Tag=panel;
				panel.KayitSilButonu.Click-=BelgeKayitSilButonu_Click;
				panel.KayitSilButonu.Click+=BelgeKayitSilButonu_Click;
			}

			if(panel.AktarButonu!=null)
			{
				if(panel.AktarButonu.ImageList!=null)
					panel.AktarButonu.ImageKey="Add Shopping Cart.png";
				panel.AktarButonu.Tag=panel;
				panel.AktarButonu.Click-=BelgeAktarButonu_Click;
				panel.AktarButonu.Click+=BelgeAktarButonu_Click;
			}

			if(panel.GuncelleButonu!=null)
			{
				if(panel.GuncelleButonu.ImageList!=null)
					panel.GuncelleButonu.ImageKey="Update User.png";
				panel.GuncelleButonu.Tag=panel;
				panel.GuncelleButonu.Click-=BelgeGuncelleButonu_Click;
				panel.GuncelleButonu.Click+=BelgeGuncelleButonu_Click;
			}

			if(panel.YazdirButonu!=null)
			{
				panel.YazdirButonu.Tag=panel;
				panel.YazdirButonu.Click-=BelgeYazdirButonu_Click;
				panel.YazdirButonu.Click+=BelgeYazdirButonu_Click;
			}

			if(panel.PdfButonu!=null)
			{
				panel.PdfButonu.Tag=panel;
				panel.PdfButonu.Click-=BelgePdfButonu_Click;
				panel.PdfButonu.Click+=BelgePdfButonu_Click;
			}

			if(panel.ExcelButonu!=null)
			{
				panel.ExcelButonu.Tag=panel;
				panel.ExcelButonu.Click-=BelgeExcelButonu_Click;
				panel.ExcelButonu.Click+=BelgeExcelButonu_Click;
			}

			BelgeCariSecimleriniYenile(panel);
			BelgeUrunSecimleriniYenile(panel);
			BelgeYapilanIsSecimleriniYenile(panel);
		}

		private void BelgeListeleriniYenile ()
		{
			if(_belgePanelleri.Count==0) return;

			foreach(BelgePaneli panel in _belgePanelleri.Values)
				BelgeKayitlariniYukle(panel , panel.SeciliKayitId , panel.SeciliDetayId);

			CariHesapVerileriniYenile();
			AnaSayfaGridleriniYenile();
			GunlukSatisVerileriniYenile();
		}

		private void BelgeKayitlariniYukle ( BelgePaneli panel , int? seciliKayitId , int? seciliDetayId )
		{
			if(panel==null||panel.UstGrid==null) return;

			if(panel.TeklifMi)
				TeklifKayitlariniYukle(panel , seciliKayitId , seciliDetayId);
			else
				FaturaBasliklariniYukle(panel , seciliKayitId , seciliDetayId);
		}

		private void FaturaBasliklariniYukle ( BelgePaneli panel , int? seciliKayitId , int? seciliDetayId )
		{
			string arama = BelgeAramaMetniGetir(panel.AramaKutusu);
			DataTable dt = new DataTable();

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT 
								F.FaturaID,
								F.FaturaTarihi,
								F.CariID,
								IIF(C.adsoyad IS NULL, '', C.adsoyad) AS CariAdi,
								IIF(C.tc IS NULL, '', C.tc) AS CariTc,
								IIF(C.telefon IS NULL, '', C.telefon) AS CariTelefon,
								F.ToplamTutar
							FROM (Faturalar AS F
							LEFT JOIN Cariler AS C ON CLng(IIF(F.CariID IS NULL, 0, F.CariID)) = C.CariID)
							LEFT JOIN CariTipi AS T ON CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = T.CariTipID
							WHERE 1=1";

				if(panel.CariTipId.HasValue)
					sorgu+=" AND CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = ?";
				else if(!string.IsNullOrWhiteSpace(panel.CariTipAdi))
					sorgu+=" AND IIF(T.TipAdi IS NULL, '', T.TipAdi) LIKE ?";

				if(!string.IsNullOrWhiteSpace(arama))
					sorgu+=" AND (CSTR(F.FaturaID) LIKE ? OR IIF(C.adsoyad IS NULL, '', C.adsoyad) LIKE ? OR FORMAT(F.FaturaTarihi, 'dd.mm.yyyy') LIKE ?)";

				sorgu+=" ORDER BY F.FaturaTarihi DESC, F.FaturaID DESC";

				using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
				{
					if(panel.CariTipId.HasValue)
						da.SelectCommand.Parameters.Add("?" , OleDbType.Integer).Value=panel.CariTipId.Value;
					else if(!string.IsNullOrWhiteSpace(panel.CariTipAdi))
						da.SelectCommand.Parameters.AddWithValue("?" , "%"+panel.CariTipAdi+"%");

					if(!string.IsNullOrWhiteSpace(arama))
					{
						string filtre = "%"+arama+"%";
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
					}
					da.Fill(dt);
				}
			}

			panel.UstGrid.DataSource=dt;
			BelgeGridGorunumunuHazirla(panel.UstGrid , "CariID");

			DataGridViewRow seciliSatir = BelgeGridSatiriBul(panel.UstGrid , panel.KayitIdKolonu , seciliKayitId);
			if(seciliSatir==null&&panel.UstGrid.Rows.Count>0)
				seciliSatir=panel.UstGrid.Rows[0];

			if(seciliSatir!=null)
				FaturaBaslikSatiriniSec(panel , seciliSatir , seciliDetayId);
			else
				BelgePaneliniTemizle(panel);
		}

		private void TeklifKayitlariniYukle ( BelgePaneli panel , int? seciliKayitId , int? seciliDetayId )
		{
			string arama = BelgeAramaMetniGetir(panel.AramaKutusu);
			DataTable dt = new DataTable();

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT
								T.TeklifID,
								T.TeklifTarihi,
								T.CariID,
								CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) AS CariTipID,
								IIF(C.adsoyad IS NULL, '', C.adsoyad) AS CariAdi,
								IIF(C.tc IS NULL, '', C.tc) AS CariTc,
								IIF(C.telefon IS NULL, '', C.telefon) AS CariTelefon,
								IIF(CT.TipAdi IS NULL, '', CT.TipAdi) AS CariTipi,
								T.ToplamTutar
							FROM (Teklifler AS T
							LEFT JOIN Cariler AS C ON CLng(IIF(T.CariID IS NULL, 0, T.CariID)) = C.CariID)
							LEFT JOIN CariTipi AS CT ON CLng(IIF(C.CariTipID IS NULL, 0, C.CariTipID)) = CT.CariTipID
							WHERE 1=1";

				if(!string.IsNullOrWhiteSpace(arama))
					sorgu+=" AND (CSTR(T.TeklifID) LIKE ? OR IIF(C.adsoyad IS NULL, '', C.adsoyad) LIKE ? OR FORMAT(T.TeklifTarihi, 'dd.mm.yyyy') LIKE ? OR IIF(CT.TipAdi IS NULL, '', CT.TipAdi) LIKE ?)";

				sorgu+=" ORDER BY T.TeklifTarihi DESC, T.TeklifID DESC";

				using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
				{
					if(!string.IsNullOrWhiteSpace(arama))
					{
						string filtre = "%"+arama+"%";
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						da.SelectCommand.Parameters.AddWithValue("?" , filtre);
					}
					da.Fill(dt);
				}
			}

			panel.UstGrid.DataSource=dt;
			BelgeGridGorunumunuHazirla(panel.UstGrid , "CariID" , "CariTipID");

			DataGridViewRow seciliSatir = null;
			if(seciliKayitId.HasValue)
				seciliSatir=BelgeGridSatiriBul(panel.UstGrid , panel.KayitIdKolonu , seciliKayitId);
			if(seciliSatir==null&&panel.UstGrid.Rows.Count>0)
				seciliSatir=panel.UstGrid.Rows[0];

			if(seciliSatir!=null)
				TeklifBaslikSatiriniSec(panel , seciliSatir , seciliDetayId);
			else
				BelgePaneliniTemizle(panel);
		}

		private void TeklifDetaylariniYukle ( BelgePaneli panel , int? seciliDetayId )
		{
			if(panel==null||panel.AltGrid==null||!panel.SeciliKayitId.HasValue)
			{
				if(panel?.AltGrid!=null)
					panel.AltGrid.DataSource=null;
				BelgeDetayAlanlariniTemizle(panel);
				BelgeOzetBilgileriniGuncelle(panel , 0m , 0m);
				return;
			}

			DataTable dt = new DataTable();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT
								TD.TeklifDetayID,
								TD.TeklifID,
								TD.UrunID,
								TD.YapilanIsID,
								IIF(TD.KalemTuru IS NULL, '', TD.KalemTuru) AS KalemTuru,
								IIF(TD.IsBilgisi IS NULL, '', TD.IsBilgisi) AS IsBilgisi,
								IIF(TD.Adet IS NULL, 0, TD.Adet) AS Adet,
								IIF(TD.KalemAdi IS NULL OR TD.KalemAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), TD.KalemAdi) AS UrunAdi,
								IIF(TD.Birim IS NULL OR TD.Birim='', IIF(B.BirimAdi IS NULL, '', B.BirimAdi), TD.Birim) AS Birim,
								TD.Miktar,
								TD.BirimFiyat AS BirimFiyat,
								TD.AraToplam AS ToplamFiyat
							FROM ((TeklifDetaylari AS TD
							LEFT JOIN Urunler AS U ON CLng(IIF(TD.UrunID IS NULL, 0, TD.UrunID)) = U.UrunID)
							LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
							LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
							WHERE CLng(IIF(TD.TeklifID IS NULL, 0, TD.TeklifID)) = ?
							ORDER BY TD.TeklifDetayID";

				using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
				{
					da.SelectCommand.Parameters.Add("?" , OleDbType.Integer).Value=panel.SeciliKayitId.Value;
					da.Fill(dt);
				}
			}

			panel.AltGrid.DataSource=dt;
			BelgeGridGorunumunuHazirla(panel.AltGrid , "TeklifID" , "UrunID" , "YapilanIsID" , "KalemTuru" , "IsBilgisi" , "Adet");

			DataGridViewRow seciliSatir = BelgeGridSatiriBul(panel.AltGrid , panel.DetayIdKolonu , seciliDetayId);
			if(seciliSatir==null&&panel.AltGrid.Rows.Count>0)
				seciliSatir=panel.AltGrid.Rows[0];

			if(seciliSatir!=null)
				BelgeDetaySatiriniSec(panel , seciliSatir);
			else
				BelgeDetayAlanlariniTemizle(panel);

			decimal araToplam = BelgeDetayGridindenAraToplamHesapla(panel);
			decimal genelToplam = panel.HeaderToplamTutar>0 ? panel.HeaderToplamTutar : BelgeToplamTutarHesapla(panel , araToplam);
			BelgeOzetBilgileriniGuncelle(panel , araToplam , genelToplam);
		}

		private void FaturaDetaylariniYukle ( BelgePaneli panel , int? seciliDetayId )
		{
			if(panel==null||panel.AltGrid==null||!panel.SeciliKayitId.HasValue)
			{
				if(panel?.AltGrid!=null)
					panel.AltGrid.DataSource=null;
				BelgeDetayAlanlariniTemizle(panel);
				BelgeOzetBilgileriniGuncelle(panel , 0m , 0m);
				return;
			}

			DataTable dt = new DataTable();
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT
								FD.FaturaDetayID,
								FD.FaturaID,
								FD.UrunID,
								FD.YapilanIsID,
								IIF(FD.KalemTuru IS NULL, '', FD.KalemTuru) AS KalemTuru,
								IIF(FD.IsBilgisi IS NULL, '', FD.IsBilgisi) AS IsBilgisi,
								IIF(FD.Adet IS NULL, 0, FD.Adet) AS Adet,
								IIF(FD.KalemAdi IS NULL OR FD.KalemAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), FD.KalemAdi) AS UrunAdi,
								IIF(FD.Birim IS NULL OR FD.Birim='', IIF(B.BirimAdi IS NULL, '', B.BirimAdi), FD.Birim) AS Birim,
								FD.Miktar,
								FD.SatisFiyati AS BirimFiyat,
								(IIF(FD.Miktar IS NULL, 0, FD.Miktar) * IIF(FD.SatisFiyati IS NULL, 0, FD.SatisFiyati)) AS ToplamFiyat
							FROM ((FaturaDetay AS FD
							LEFT JOIN Urunler AS U ON CLng(IIF(FD.UrunID IS NULL, 0, FD.UrunID)) = U.UrunID)
							LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
							LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
							WHERE CLng(IIF(FD.FaturaID IS NULL, 0, FD.FaturaID)) = ?
							ORDER BY FD.FaturaDetayID";

				using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
				{
					da.SelectCommand.Parameters.Add("?" , OleDbType.Integer).Value=panel.SeciliKayitId.Value;
					da.Fill(dt);
				}
			}

			panel.AltGrid.DataSource=dt;
			BelgeGridGorunumunuHazirla(panel.AltGrid , "FaturaID" , "UrunID" , "YapilanIsID" , "KalemTuru" , "IsBilgisi" , "Adet");

			DataGridViewRow seciliSatir = BelgeGridSatiriBul(panel.AltGrid , panel.DetayIdKolonu , seciliDetayId);
			if(seciliSatir==null&&panel.AltGrid.Rows.Count>0)
				seciliSatir=panel.AltGrid.Rows[0];

			if(seciliSatir!=null)
				BelgeDetaySatiriniSec(panel , seciliSatir);
			else
				BelgeDetayAlanlariniTemizle(panel);

			decimal araToplam = BelgeDetayGridindenAraToplamHesapla(panel);
			decimal genelToplam = panel.HeaderToplamTutar>0 ? panel.HeaderToplamTutar : BelgeToplamTutarHesapla(panel , araToplam);
			BelgeOzetBilgileriniGuncelle(panel , araToplam , genelToplam);
		}

		private void BelgeGridGorunumunuHazirla ( DataGridView grid , params string[] gizlenecekKolonlar )
		{
			if(grid==null) return;

			GridBasliklariniTurkceDuzenle(grid);
			GriddeArizaKolonlariniGizle(grid);
			foreach(string kolon in gizlenecekKolonlar)
			{
				if(grid.Columns.Contains(kolon))
					grid.Columns[kolon].Visible=false;
			}

			if(grid.Columns.Contains("FaturaID"))
				grid.Columns["FaturaID"].HeaderText="ID";
			if(grid.Columns.Contains("TeklifID"))
				grid.Columns["TeklifID"].HeaderText="ID";
			if(grid.Columns.Contains("FaturaTarihi"))
				grid.Columns["FaturaTarihi"].HeaderText="TARİH";
			if(grid.Columns.Contains("TeklifTarihi"))
				grid.Columns["TeklifTarihi"].HeaderText="TARİH";
			if(grid.Columns.Contains("CariAdi"))
				grid.Columns["CariAdi"].HeaderText="CARİ";
			if(grid.Columns.Contains("UrunAdi"))
				grid.Columns["UrunAdi"].HeaderText="ÜRÜN ADI";
			if(grid.Columns.Contains("ToplamTutar"))
				grid.Columns["ToplamTutar"].HeaderText="TOPLAM";
			if(grid.Columns.Contains("BirimFiyat"))
				grid.Columns["BirimFiyat"].HeaderText="SATIŞ FİYATI";
			if(grid.Columns.Contains("ToplamFiyat"))
				grid.Columns["ToplamFiyat"].HeaderText="TOPLAM FİYAT";
		}

		private void BelgeUstGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			DataGridView grid = sender as DataGridView;
			if(panel==null||grid==null||e.RowIndex>=grid.Rows.Count) return;

			if(panel.TeklifMi)
				TeklifBaslikSatiriniSec(panel , grid.Rows[e.RowIndex] , null);
			else
				FaturaBaslikSatiriniSec(panel , grid.Rows[e.RowIndex] , null);
		}

		private void BelgeAltGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			DataGridView grid = sender as DataGridView;
			if(panel==null||grid==null||e.RowIndex>=grid.Rows.Count) return;

			BelgeDetaySatiriniSec(panel , grid.Rows[e.RowIndex]);
		}

		private void BelgeArama_TextChanged ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null) return;

			BelgeKayitlariniYukle(panel , panel.SeciliKayitId , panel.SeciliDetayId);
		}

		private void BelgeCari_TextChanged ( object sender , EventArgs e )
		{
			if(_belgeAlanlariGuncelleniyor) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null) return;

			string arama = BelgeCariMetniGetir(panel);
			BelgeCariSecimleriniYenile(panel);
			ComboBoxEslesmeleriniGoster(panel.CariAdComboBox , arama);
			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
			{
				panel.SeciliCariId=null;
				if(panel.TeklifMi)
					panel.CariTipId=null;
				BelgeCariAlanlariniDoldur(panel , arama , string.Empty , string.Empty , null);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					int? cariId = BelgedenCariIdCoz(conn , null , panel , false);
					if(!cariId.HasValue)
						BelgeCariAlanlariniDoldur(panel , arama , string.Empty , string.Empty , null);
				}
			}
			catch
			{
				panel.SeciliCariId=null;
				if(panel.TeklifMi)
					panel.CariTipId=null;
				BelgeCariAlanlariniDoldur(panel , arama , string.Empty , string.Empty , null);
			}
		}

		private void BelgeUrun_TextChanged ( object sender , EventArgs e )
		{
			if(_belgeAlanlariGuncelleniyor) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null) return;

			string arama = BelgeUrunMetniGetir(panel);
			BelgeUrunSecimleriniYenile(panel);
			ComboBoxEslesmeleriniGoster(panel.UrunAdiComboBox , arama);
			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
			{
				BelgeUrunSeciminiTemizle(panel);
				BelgeToplamKutusuGuncelle(panel);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					UrunAramaKaydi urunKaydi = EnUygunUrunKaydiniBul(conn , null , arama);
					if(urunKaydi!=null)
					{
						bool tekAdayVar = panel.UrunAdiComboBox!=null&&panel.UrunAdiComboBox.Items.Count==1;
						bool tamEslesmeVar = BelgeUrunMetniKaydaTamEslesiyorMu(arama , urunKaydi);
						if(tamEslesmeVar||tekAdayVar)
							BelgeUrunKaydiniUygula(conn , null , panel , urunKaydi , tamEslesmeVar);
					}
				}
			}
			catch
			{
				_belgeAlanlariGuncelleniyor=false;
				BelgeUrunSeciminiTemizle(panel);
			}

			BelgeToplamKutusuGuncelle(panel);
		}

		private void BelgeYapilanIs_TextChanged ( object sender , EventArgs e )
		{
			if(_belgeAlanlariGuncelleniyor) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null) return;

			string arama = panel.YapilanIsComboBox?.Text?.Trim()??string.Empty;
			BelgeYapilanIsSecimleriniYenile(panel);
			ComboBoxEslesmeleriniGoster(panel.YapilanIsComboBox , arama);
			BelgeYapilanIsSeciminiTemizle(panel , false);
			if(string.IsNullOrWhiteSpace(arama)||arama.Length<2)
			{
				BelgeToplamKutusuGuncelle(panel);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					YapilanIsKaydi kayit = EnUygunYapilanIsKaydiniBul(conn , null , arama);
					if(kayit!=null)
					{
						BelgeUrunSeciminiTemizle(panel);
						BelgeUrunMetniniTemizle(panel);
						BelgeYapilanIsBilgileriniDoldur(panel , kayit , true);
					}
				}
			}
			catch
			{
				BelgeYapilanIsSeciminiTemizle(panel , false);
			}
		}

		private void BelgeSayisal_TextChanged ( object sender , EventArgs e )
		{
			if(_belgeAlanlariGuncelleniyor) return;

			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null) return;

			BelgeToplamKutusuGuncelle(panel);
		}

		private void BelgeKdv_TextChanged ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null||panel.OzetBilgisiGuncelleniyor)
				return;

			decimal araToplam = BelgeOzetAraToplaminiGetir(panel);
			decimal genelToplam = BelgeToplamTutarHesapla(panel , araToplam);
			if(panel.TotalLabel!=null)
				panel.TotalLabel.Text=genelToplam.ToString("N2");
			panel.HeaderToplamTutar=genelToplam;
		}

		private void BelgeKdv_Leave ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel?.KdvTextBox==null)
				return;

			panel.KdvTextBox.Text=BelgeKdvOraniGetir(panel).ToString("N2");
		}

		private void BelgeKaydetButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null)
			{
				MessageBox.Show("Önce bir kayıt seçin.");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							int kayitId = panel.SeciliKayitId??0;
							if(panel.TeklifMi&&!panel.SeciliKayitId.HasValue)
								kayitId=TeklifBasligiOlustur(conn , tx , panel);
							else if(!panel.SeciliKayitId.HasValue)
								throw new InvalidOperationException("Önce bir kayıt seçin.");

							BelgeUstKaydiniGuncelle(conn , tx , panel , kayitId);

							decimal carpan = BelgeToplamCarpaniGetir(conn , tx , panel , kayitId);
							KalemSecimBilgisi kalem = BelgedenKalemBilgisiCoz(conn , tx , panel);
							if(kalem==null||string.IsNullOrWhiteSpace(kalem.KalemAdi))
								throw new InvalidOperationException("Ürün veya yapılan iş bulunamadı.");
							if(kalem.Miktar<=0)
								throw new InvalidOperationException("Miktar 0'dan büyük olmalıdır.");

							if(panel.TeklifMi)
								TeklifDetayKaleminiEkle(conn , tx , kayitId , kalem);
							else
								FaturaDetayKaleminiEkle(conn , tx , kayitId , kalem);

							BelgeHeaderToplamGuncelle(conn , tx , panel , kayitId , carpan);
							tx.Commit();
							panel.SeciliKayitId=kayitId;
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				BelgeKayitlariniYukle(panel , panel.SeciliKayitId , null);
				if(!panel.TeklifMi)
					StokDegisimindenSonraEkranlariYenile();
				MessageBox.Show("Satır kaydedildi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Kayıt hatası: "+ex.Message);
			}
		}

		private void BelgeAktarButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null||!panel.TeklifMi||!panel.SeciliKayitId.HasValue)
			{
				MessageBox.Show("Önce bir teklif seçin.");
				return;
			}

			if(string.IsNullOrWhiteSpace(BelgeCariMetniGetir(panel)))
			{
				MessageBox.Show("Aktarmak için cari girin.");
				return;
			}

			try
			{
				int faturaId = 0;
				BelgeKayitTuru hedefTur = BelgeKayitTuru.MusteriFaturasi;

				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							BelgeUstKaydiniGuncelle(conn , tx , panel , panel.SeciliKayitId.Value);
							int? cariId = panel.SeciliCariId??BelgedenCariIdCoz(conn , tx , panel , true);
							hedefTur=CariyeGoreBelgeTuruGetir(conn , tx , cariId.Value);
							faturaId=TeklifiFaturayaAktar(conn , tx , panel.SeciliKayitId.Value , cariId.Value);
							tx.Commit();
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				if(_belgePanelleri.ContainsKey(hedefTur))
				{
					_belgePanelleri[hedefTur].SeciliKayitId=faturaId;
					_belgePanelleri[hedefTur].SeciliDetayId=null;
				}

				BelgeListeleriniYenile();
				StokDegisimindenSonraEkranlariYenile();
				SepetKayitSekmesiniAc(hedefTur);
				MessageBox.Show("Teklif girilen cariye gore faturaya aktarildi ve tekliflerden silindi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Aktarma hatası: "+ex.Message);
			}
		}

		private void BelgeGuncelleButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null||!panel.SeciliKayitId.HasValue)
			{
				MessageBox.Show("Önce bir kayıt seçin.");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							BelgeUstKaydiniGuncelle(conn , tx , panel , panel.SeciliKayitId.Value);
							decimal carpan = BelgeToplamCarpaniGetir(conn , tx , panel , panel.SeciliKayitId.Value);

							if(panel.SeciliDetayId.HasValue)
							{
								KalemSecimBilgisi kalem = BelgedenKalemBilgisiCoz(conn , tx , panel);
								if(kalem==null||string.IsNullOrWhiteSpace(kalem.KalemAdi))
									throw new InvalidOperationException("Ürün veya yapılan iş bulunamadı.");
								if(kalem.Miktar<=0)
									throw new InvalidOperationException("Miktar 0'dan büyük olmalıdır.");

								if(panel.TeklifMi)
									TeklifDetayKaleminiGuncelle(conn , tx , panel.SeciliDetayId.Value , kalem);
								else
								{
									StokKalemBilgisi eskiStokKalemi = FaturaDetayStokKaleminiGetir(conn , tx , panel.SeciliDetayId.Value);
									StokKaleminiIadeEt(conn , tx , eskiStokKalemi);
									FaturaKalemiIcinStokDus(conn , tx , kalem);
									FaturaDetayKaleminiGuncelle(conn , tx , panel.SeciliDetayId.Value , kalem);
								}
							}

							BelgeHeaderToplamGuncelle(conn , tx , panel , panel.SeciliKayitId.Value , carpan);

							tx.Commit();
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				BelgeListeleriniYenile();
				if(!panel.TeklifMi)
					StokDegisimindenSonraEkranlariYenile();
				MessageBox.Show("Kayıt güncellendi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Güncelleme hatası: "+ex.Message);
			}
		}

		private void BelgeDetaySilButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null||!panel.SeciliKayitId.HasValue||!panel.SeciliDetayId.HasValue)
			{
				MessageBox.Show("Silmek için bir satır seçin.");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							decimal carpan = BelgeToplamCarpaniGetir(conn , tx , panel , panel.SeciliKayitId.Value);
							if(!panel.TeklifMi)
								StokKaleminiIadeEt(conn , tx , FaturaDetayStokKaleminiGetir(conn , tx , panel.SeciliDetayId.Value));
							string sorgu = panel.TeklifMi
								? "DELETE FROM TeklifDetaylari WHERE TeklifDetayID=?"
								: "DELETE FROM FaturaDetay WHERE FaturaDetayID=?";

							using(OleDbCommand cmd = new OleDbCommand(sorgu , conn , tx))
							{
								cmd.Parameters.AddWithValue("?" , panel.SeciliDetayId.Value);
								cmd.ExecuteNonQuery();
							}

							BelgeHeaderToplamGuncelle(conn , tx , panel , panel.SeciliKayitId.Value , carpan);
							tx.Commit();
						}
						catch
						{
							tx.Rollback();
							throw;
						}
					}
				}

				panel.SeciliDetayId=null;
				BelgeKayitlariniYukle(panel , panel.SeciliKayitId , null);
				if(!panel.TeklifMi)
					StokDegisimindenSonraEkranlariYenile();
				MessageBox.Show("Satır silindi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Silme hatası: "+ex.Message);
			}
		}

		private void BelgeKayitSilButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null||!panel.SeciliKayitId.HasValue)
			{
				MessageBox.Show("Silmek için bir kayıt seçin.");
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbTransaction tx = conn.BeginTransaction())
					{
						try
						{
							if(panel.TeklifMi)
							{
								using(OleDbCommand detaySil = new OleDbCommand("DELETE FROM TeklifDetaylari WHERE TeklifID=?" , conn , tx))
								{
									detaySil.Parameters.AddWithValue("?" , panel.SeciliKayitId.Value);
									detaySil.ExecuteNonQuery();
								}

								using(OleDbCommand kayitSil = new OleDbCommand("DELETE FROM Teklifler WHERE TeklifID=?" , conn , tx))
								{
									kayitSil.Parameters.AddWithValue("?" , panel.SeciliKayitId.Value);
									kayitSil.ExecuteNonQuery();
								}
							}
							else
							{
								foreach(StokKalemBilgisi stokKalemi in FaturaStokKalemleriniGetir(conn , tx , panel.SeciliKayitId.Value))
									StokKaleminiIadeEt(conn , tx , stokKalemi);

								using(OleDbCommand tahsilatSil = new OleDbCommand("DELETE FROM FaturaTahsilatlari WHERE FaturaID=?" , conn , tx))
								{
									tahsilatSil.Parameters.AddWithValue("?" , panel.SeciliKayitId.Value);
									tahsilatSil.ExecuteNonQuery();
								}

								using(OleDbCommand detaySil = new OleDbCommand("DELETE FROM FaturaDetay WHERE FaturaID=?" , conn , tx))
								{
									detaySil.Parameters.AddWithValue("?" , panel.SeciliKayitId.Value);
									detaySil.ExecuteNonQuery();
								}

								using(OleDbCommand kayitSil = new OleDbCommand("DELETE FROM Faturalar WHERE FaturaID=?" , conn , tx))
								{
									kayitSil.Parameters.AddWithValue("?" , panel.SeciliKayitId.Value);
									kayitSil.ExecuteNonQuery();
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

				panel.SeciliKayitId=null;
				panel.SeciliDetayId=null;
				BelgeListeleriniYenile();
				if(!panel.TeklifMi)
					StokDegisimindenSonraEkranlariYenile();
				MessageBox.Show("Kayıt silindi.");
			}
			catch(Exception ex)
			{
				MessageBox.Show("Kayıt silme hatası: "+ex.Message);
			}
		}

		private void FaturaBaslikSatiriniSec ( BelgePaneli panel , DataGridViewRow row , int? seciliDetayId )
		{
			if(panel==null||row==null||row.IsNewRow) return;

			panel.SeciliKayitId=SatirdanIntGetir(row , "FaturaID");
			panel.SeciliCariId=SatirdanIntGetir(row , "CariID");

			BelgeGridSatiriniSec(panel.UstGrid , row);
			BelgeCariAlanlariniDoldur(
				panel ,
				Convert.ToString(row.Cells["CariAdi"].Value) ,
				Convert.ToString(row.Cells["CariTc"].Value) ,
				Convert.ToString(row.Cells["CariTelefon"].Value) ,
				panel.SeciliCariId ,
				panel.CariTipId ,
				SatirKolonuVarMi(row , "CariTipi") ? Convert.ToString(row.Cells["CariTipi"].Value) : null);

			panel.HeaderToplamTutar=SepetDecimalParse(Convert.ToString(row.Cells["ToplamTutar"].Value));

			FaturaDetaylariniYukle(panel , seciliDetayId);
			BelgeArizaAlanlariniYukle(panel);
		}

		private void TeklifBaslikSatiriniSec ( BelgePaneli panel , DataGridViewRow row , int? seciliDetayId )
		{
			if(panel==null||row==null||row.IsNewRow) return;

			panel.SeciliKayitId=SatirdanIntGetir(row , "TeklifID");
			panel.SeciliCariId=SatirdanIntGetir(row , "CariID");
			panel.CariTipId=SatirdanIntGetir(row , "CariTipID");

			BelgeGridSatiriniSec(panel.UstGrid , row);
			BelgeCariAlanlariniDoldur(
				panel ,
				Convert.ToString(row.Cells["CariAdi"].Value) ,
				Convert.ToString(row.Cells["CariTc"].Value) ,
				Convert.ToString(row.Cells["CariTelefon"].Value) ,
				panel.SeciliCariId);

			panel.HeaderToplamTutar=SepetDecimalParse(Convert.ToString(row.Cells["ToplamTutar"].Value));

			TeklifDetaylariniYukle(panel , seciliDetayId);
			BelgeArizaAlanlariniYukle(panel);
		}

		private void BelgeDetaySatiriniSec ( BelgePaneli panel , DataGridViewRow row )
		{
			if(panel==null||row==null||row.IsNewRow) return;

			panel.SeciliDetayId=SatirdanIntGetir(row , panel.DetayIdKolonu);
			BelgeGridSatiriniSec(panel.AltGrid , row);
			bool yapilanIsSatiri = SatirYapilanIsKalemiMi(row);

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				if(yapilanIsSatiri)
				{
					panel.SeciliYapilanIsId=SatirdanIntGetir(row , "YapilanIsID");
					BelgeUrunMetniniTemizle(panel);
					if(panel.YapilanIsComboBox!=null)
						panel.YapilanIsComboBox.Text=Convert.ToString(row.Cells["UrunAdi"].Value)??string.Empty;
					if(panel.YapilanIsBilgiTextBox!=null)
						panel.YapilanIsBilgiTextBox.Text=Convert.ToString(row.Cells["UrunAdi"].Value)??string.Empty;
					if(panel.YapilanIsAdetTextBox!=null)
					{
						decimal kalemAdedi = SatirKolonuVarMi(row , "Adet")
							? SepetDecimalParse(Convert.ToString(row.Cells["Adet"].Value))
							: 0m;
						panel.YapilanIsAdetTextBox.Text=( kalemAdedi<=0 ? 1m : kalemAdedi ).ToString("0.##" , _yazdirmaKulturu);
					}
				}
				else
				{
					panel.SeciliYapilanIsId=null;
					BelgeYapilanIsSeciminiTemizle(panel , true);
					BelgeUrunMetniniAyarla(panel , Convert.ToString(row.Cells["UrunAdi"].Value)??string.Empty);
				}

				if(panel.BirimTextBox!=null)
					panel.BirimTextBox.Text=Convert.ToString(row.Cells["Birim"].Value)??string.Empty;
				if(panel.MiktarTextBox!=null)
					panel.MiktarTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["Miktar"].Value)).ToString("0.##" , new CultureInfo("tr-TR"));
				if(panel.BirimFiyatTextBox!=null)
					panel.BirimFiyatTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["BirimFiyat"].Value)).ToString("N2");
				if(panel.YapilanIsFiyatTextBox!=null)
					panel.YapilanIsFiyatTextBox.Text=yapilanIsSatiri
						? SepetDecimalParse(Convert.ToString(row.Cells["BirimFiyat"].Value)).ToString("N2")
						: "0,00";
				if(panel.ToplamFiyatTextBox!=null)
					panel.ToplamFiyatTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["ToplamFiyat"].Value)).ToString("N2");
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private void TeklifSatiriniSec ( BelgePaneli panel , DataGridViewRow row )
		{
			if(panel==null||row==null||row.IsNewRow) return;

			panel.SeciliKayitId=SatirdanIntGetir(row , "TeklifID");
			panel.SeciliDetayId=SatirdanIntGetir(row , "TeklifDetayID");
			panel.SeciliCariId=SatirdanIntGetir(row , "CariID");
			panel.CariTipId=SatirdanIntGetir(row , "CariTipID");

			BelgeGridSatiriniSec(panel.UstGrid , row);
			BelgeCariAlanlariniDoldur(
				panel ,
				Convert.ToString(row.Cells["CariAdi"].Value) ,
				Convert.ToString(row.Cells["CariTc"].Value) ,
				Convert.ToString(row.Cells["CariTelefon"].Value) ,
				panel.SeciliCariId ,
				panel.CariTipId ,
				SatirKolonuVarMi(row , "CariTipi") ? Convert.ToString(row.Cells["CariTipi"].Value) : null);

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				BelgeUrunMetniniAyarla(panel , Convert.ToString(row.Cells["UrunAdi"].Value)??string.Empty);
				if(panel.BirimTextBox!=null)
					panel.BirimTextBox.Text=Convert.ToString(row.Cells["Birim"].Value)??string.Empty;
				if(panel.MiktarTextBox!=null)
					panel.MiktarTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["Miktar"].Value)).ToString("0.##" , new CultureInfo("tr-TR"));
				if(panel.BirimFiyatTextBox!=null)
					panel.BirimFiyatTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["BirimFiyat"].Value)).ToString("N2");
				if(panel.ToplamFiyatTextBox!=null)
					panel.ToplamFiyatTextBox.Text=SepetDecimalParse(Convert.ToString(row.Cells["ToplamFiyat"].Value)).ToString("N2");
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}

			BelgeArizaAlanlariniYukle(panel);
		}

		private void BelgeCariAlanlariniDoldur ( BelgePaneli panel , string cariAd , string cariTc , string cariTelefon , int? cariId , int? cariTipId = null , string cariTipAdi = null )
		{
			if(panel==null) return;

			string tipAdi = !string.IsNullOrWhiteSpace(cariTipAdi)
				? cariTipAdi
				: CariTipAdiGetir(cariTipId??panel.CariTipId);
			string gosterimMetni = panel.TeklifMi
				? CariGosterimDetayMetniOlustur(cariAd , tipAdi)
				: ( cariAd??string.Empty );

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				BelgeCariMetniniAyarla(panel , gosterimMetni);
				if(panel.CariTcTextBox!=null)
					panel.CariTcTextBox.Text=cariTc??string.Empty;
				if(panel.CariTelefonTextBox!=null)
					panel.CariTelefonTextBox.Text=cariTelefon??string.Empty;
				panel.SeciliCariId=cariId;
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private void BelgeDetayAlanlariniTemizle ( BelgePaneli panel )
		{
			if(panel==null) return;

			_belgeAlanlariGuncelleniyor=true;
			try
			{
				panel.SeciliDetayId=null;
				panel.SeciliYapilanIsId=null;
				BelgeUrunMetniniTemizle(panel);
				BelgeYapilanIsSeciminiTemizle(panel , true);
				if(panel.BirimTextBox!=null) panel.BirimTextBox.Clear();
				if(panel.MiktarTextBox!=null) panel.MiktarTextBox.Text="1";
				if(panel.BirimFiyatTextBox!=null) panel.BirimFiyatTextBox.Text="0,00";
				if(panel.ToplamFiyatTextBox!=null) panel.ToplamFiyatTextBox.Text="0,00";
			}
			finally
			{
				_belgeAlanlariGuncelleniyor=false;
			}
		}

		private void BelgePaneliniTemizle ( BelgePaneli panel )
		{
			if(panel==null) return;

			panel.SeciliKayitId=null;
			panel.SeciliDetayId=null;
			panel.SeciliCariId=null;
			panel.HeaderToplamTutar=0m;
			if(panel.TeklifMi)
				panel.CariTipId=null;
			BelgeCariAlanlariniDoldur(panel , string.Empty , string.Empty , string.Empty , null);
			BelgeDetayAlanlariniTemizle(panel);
			if(panel.AltGrid!=null)
				panel.AltGrid.DataSource=null;
			BelgeOzetBilgileriniGuncelle(panel , 0m , 0m);
		}

		private void BelgeToplamKutusuGuncelle ( BelgePaneli panel )
		{
			if(panel==null||panel.ToplamFiyatTextBox==null) return;

			decimal miktar = SepetDecimalParse(panel.MiktarTextBox?.Text);
			decimal birimFiyat = SepetDecimalParse(panel.BirimFiyatTextBox?.Text);
			panel.ToplamFiyatTextBox.Text=(miktar*birimFiyat).ToString("N2" , _yazdirmaKulturu);
		}

		private BelgePaneli BelgePaneliniGetir ( object sender )
		{
			Control control = sender as Control;
			return control?.Tag as BelgePaneli;
		}

		private string BelgeAramaMetniGetir ( TextBox aramaKutusu )
		{
			return AramaKutusuMetniGetir(aramaKutusu);
		}

		private DataGridViewRow BelgeGridSatiriBul ( DataGridView grid , string kolonAdi , int? aranacakId )
		{
			if(grid==null||string.IsNullOrWhiteSpace(kolonAdi)||!aranacakId.HasValue||!grid.Columns.Contains(kolonAdi))
				return null;

			foreach(DataGridViewRow row in grid.Rows)
			{
				if(row.IsNewRow) continue;

				int satirId;
				if(int.TryParse(Convert.ToString(row.Cells[kolonAdi].Value) , out satirId)&&satirId==aranacakId.Value)
					return row;
			}

			return null;
		}

		private void BelgeGridSatiriniSec ( DataGridView grid , DataGridViewRow row )
		{
			if(grid==null||row==null||row.IsNewRow) return;

			grid.ClearSelection();
			row.Selected=true;
			DataGridViewCell ilkGorunen = row.Cells.Cast<DataGridViewCell>().FirstOrDefault(c => grid.Columns[c.ColumnIndex].Visible);
			if(ilkGorunen!=null)
				grid.CurrentCell=ilkGorunen;
		}

		private int? SatirdanIntGetir ( DataGridViewRow row , string kolonAdi )
		{
			if(row==null||string.IsNullOrWhiteSpace(kolonAdi)||!row.DataGridView.Columns.Contains(kolonAdi))
				return null;

			int deger;
			return int.TryParse(Convert.ToString(row.Cells[kolonAdi].Value) , out deger) ? deger : (int?)null;
		}

		private void BelgeUstKaydiniGuncelle ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId )
		{
			int? cariId = BelgedenCariIdCoz(conn , tx , panel , !panel.TeklifMi);
			if(panel.TeklifMi)
			{
				using(OleDbCommand cmd = new OleDbCommand("UPDATE Teklifler SET CariID=? WHERE TeklifID=?" , conn , tx))
				{
					cmd.Parameters.AddWithValue("?" , (object)cariId??DBNull.Value);
					cmd.Parameters.AddWithValue("?" , kayitId);
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using(OleDbCommand cmd = new OleDbCommand("UPDATE Faturalar SET CariID=? WHERE FaturaID=?" , conn , tx))
				{
					cmd.Parameters.AddWithValue("?" , cariId.Value);
					cmd.Parameters.AddWithValue("?" , kayitId);
					cmd.ExecuteNonQuery();
				}
			}

			if(panel.YapilanIsComboBox==null)
				BelgeArizaAlanlariniKaydet(conn , tx , panel , kayitId);
		}

		private void BelgeArizaAlanlariniKaydet ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId )
		{
			if(conn==null||panel?.ArizaTextBoxlari==null||kayitId<=0||panel.YapilanIsComboBox!=null)
				return;

			string tabloAdi = panel.TeklifMi ? "Teklifler" : "Faturalar";
			string idKolonu = panel.TeklifMi ? "TeklifID" : "FaturaID";
			if(!KolonVarMi(conn , tabloAdi , "Ariza1"))
				return;

			string[] arizaDegerleri = new string[4];
			for(int i = 0 ; i<arizaDegerleri.Length ; i++)
			{
				TextBox kutu = i<panel.ArizaTextBoxlari.Length ? panel.ArizaTextBoxlari[i] : null;
				arizaDegerleri[i]=kutu?.Text?.Trim()??string.Empty;
			}

			using(OleDbCommand cmd = new OleDbCommand(
				"UPDATE ["+tabloAdi+"] SET [Ariza1]=?, [Ariza2]=?, [Ariza3]=?, [Ariza4]=? WHERE ["+idKolonu+"]=?" ,
				conn ,
				tx))
			{
				foreach(string arizaDegeri in arizaDegerleri)
					cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(arizaDegeri) ? (object)DBNull.Value : arizaDegeri;

				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kayitId;
				cmd.ExecuteNonQuery();
			}
		}

		private int? BelgedenCariIdCoz ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , bool zorunlu )
		{
			string cariAdi = BelgeCariMetniGetir(panel);
			if(string.IsNullOrWhiteSpace(cariAdi))
			{
				panel.SeciliCariId=null;
				if(panel.TeklifMi)
					panel.CariTipId=null;
				if(panel.CariTcTextBox!=null) panel.CariTcTextBox.Clear();
				if(panel.CariTelefonTextBox!=null) panel.CariTelefonTextBox.Clear();
				if(zorunlu)
					throw new InvalidOperationException("Cari seçmeden işlem yapılamaz.");
				return null;
			}

			CariAramaKaydi cariKaydi = EnUygunCariKaydiniBul(conn , tx , cariAdi , panel.CariTipId);
			if(cariKaydi!=null)
			{
				if(panel.TeklifMi)
					panel.CariTipId=cariKaydi.CariTipId;
				BelgeCariAlanlariniDoldur(panel , cariKaydi.AdSoyad , cariKaydi.Tc , cariKaydi.Telefon , cariKaydi.CariId , cariKaydi.CariTipId , cariKaydi.TipAdi);
				return cariKaydi.CariId;
			}

			if(zorunlu)
				throw new InvalidOperationException("Yazdığınız cari bulunamadı.");

			panel.SeciliCariId=null;
			if(panel.TeklifMi)
				panel.CariTipId=null;
			if(panel.CariTcTextBox!=null) panel.CariTcTextBox.Clear();
			if(panel.CariTelefonTextBox!=null) panel.CariTelefonTextBox.Clear();
			return null;
		}

		private bool BelgedenUrunBilgisiCoz ( OleDbConnection conn , OleDbTransaction tx , string urunMetni , out int urunId , out string urunAdi , out string birimAdi )
		{
			urunId=0;
			urunAdi=string.Empty;
			birimAdi=string.Empty;

			string arama = (urunMetni??string.Empty).Trim();
			if(string.IsNullOrWhiteSpace(arama))
				return false;

			UrunAramaKaydi urunKaydi = EnUygunUrunKaydiniBul(conn , tx , arama);
			if(urunKaydi!=null)
			{
				urunId=urunKaydi.UrunId;
				urunAdi=string.IsNullOrWhiteSpace(urunKaydi.UrunAdi) ? urunKaydi.UrunGosterimAdi : urunKaydi.UrunAdi;
				birimAdi=urunKaydi.BirimAdi;
				return true;
			}

			return false;
		}

		private decimal BelgeToplamCarpaniGetir ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId )
		{
			decimal kdvOrani = BelgeKdvOraniGetir(panel);
			if(kdvOrani>0)
				return 1m+( kdvOrani/100m );

			decimal ozetAraToplam = BelgeOzetAraToplaminiGetir(panel);
			decimal ozetGenelToplam = SepetDecimalParse(panel?.TotalLabel?.Text);
			if(ozetAraToplam>0&&ozetGenelToplam>0)
				return ozetGenelToplam/ozetAraToplam;

			decimal mevcutToplam = BelgeHeaderToplamGetir(conn , tx , panel , kayitId);
			decimal araToplam = BelgeAltToplamGetir(conn , tx , panel , kayitId);
			if(araToplam<=0||mevcutToplam<=0)
				return 1m;

			return mevcutToplam/araToplam;
		}

		private decimal BelgeHeaderToplamGetir ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId )
		{
			string sorgu = panel.TeklifMi
				? "SELECT ToplamTutar FROM Teklifler WHERE TeklifID=?"
				: "SELECT ToplamTutar FROM Faturalar WHERE FaturaID=?";

			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , kayitId);
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0 : Convert.ToDecimal(sonuc);
			}
		}

		private decimal BelgeAltToplamGetir ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId )
		{
			string sorgu = panel.TeklifMi
				? "SELECT SUM(IIF(AraToplam IS NULL, 0, AraToplam)) FROM TeklifDetaylari WHERE TeklifID=?"
				: "SELECT SUM(IIF(Miktar IS NULL, 0, Miktar) * IIF(SatisFiyati IS NULL, 0, SatisFiyati)) FROM FaturaDetay WHERE FaturaID=?";

			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.AddWithValue("?" , kayitId);
				object sonuc = cmd.ExecuteScalar();
				return sonuc==null||sonuc==DBNull.Value ? 0 : Convert.ToDecimal(sonuc);
			}
		}

		private void BelgeHeaderToplamGuncelle ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel , int kayitId , decimal carpan )
		{
			decimal araToplam = BelgeAltToplamGetir(conn , tx , panel , kayitId);
			decimal toplamTutar = araToplam*(carpan<=0 ? 1m : carpan);
			string sorgu = panel.TeklifMi
				? "UPDATE Teklifler SET ToplamTutar=? WHERE TeklifID=?"
				: "UPDATE Faturalar SET ToplamTutar=? WHERE FaturaID=?";

			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Currency).Value=toplamTutar;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=kayitId;
				cmd.ExecuteNonQuery();
			}
		}

		private int TeklifBasligiOlustur ( OleDbConnection conn , OleDbTransaction tx , BelgePaneli panel )
		{
			int? cariId = BelgedenCariIdCoz(conn , tx , panel , true);
			using(OleDbCommand cmd = new OleDbCommand("INSERT INTO Teklifler (CariID, TeklifNo, TeklifTarihi, GecerlilikTarihi, ToplamTutar, Durum) VALUES (?, ?, ?, ?, ?, ?)" , conn , tx))
			{
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariId.Value;
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=YeniBelgeNoUret("TKL");
				cmd.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now;
				cmd.Parameters.Add("?" , OleDbType.Date).Value=DateTime.Now.AddDays(30);
				cmd.Parameters.Add("?" , OleDbType.Currency).Value=0m;
				cmd.Parameters.Add("?" , OleDbType.Boolean).Value=true;
				cmd.ExecuteNonQuery();
			}

			using(OleDbCommand cmdId = new OleDbCommand("SELECT @@IDENTITY" , conn , tx))
				return Convert.ToInt32(cmdId.ExecuteScalar());
		}

		private void BelgeArizaAlanlariniTemizle ( BelgePaneli panel )
		{
			if(panel?.ArizaTextBoxlari==null) return;

			foreach(TextBox kutu in panel.ArizaTextBoxlari)
			{
				if(kutu!=null)
					kutu.Clear();
			}
		}

		private void BelgeArizaAlanlariniYukle ( BelgePaneli panel )
		{
			if(panel?.YapilanIsComboBox!=null)
				return;

			BelgeArizaAlanlariniTemizle(panel);
			if(panel==null) return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					ArizaKaynakBilgisi kaynak = BelgeyeUygunArizaKaynagiGetir(conn , panel);
					if(kaynak==null||kaynak.ArizaKolonlari.Count==0)
						return;

					object iliskiDegeri = string.Equals(kaynak.IliskiKolonu , "CariID" , StringComparison.OrdinalIgnoreCase)
						? (object)panel.SeciliCariId
						: panel.SeciliKayitId;

					if(iliskiDegeri==null)
						return;

					string kolonListesi = string.Join(", " , kaynak.ArizaKolonlari.Select(k => "["+k+"]"));
					string sorgu = "SELECT TOP 1 "+kolonListesi+" FROM ["+kaynak.TabloAdi+"] WHERE ["+kaynak.IliskiKolonu+"]=?" ;
					if(!string.IsNullOrWhiteSpace(kaynak.SiralamaKolonu))
						sorgu+=" ORDER BY ["+kaynak.SiralamaKolonu+"] DESC";

					using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
					{
						cmd.Parameters.AddWithValue("?" , iliskiDegeri);
						using(OleDbDataReader rd = cmd.ExecuteReader())
						{
							if(rd!=null&&rd.Read())
							{
								for(int i = 0 ; i<panel.ArizaTextBoxlari.Length&&i<kaynak.ArizaKolonlari.Count ; i++)
								{
									TextBox kutu = panel.ArizaTextBoxlari[i];
									if(kutu!=null)
										kutu.Text=rd[kaynak.ArizaKolonlari[i]]?.ToString()??string.Empty;
								}
							}
						}
					}
				}
			}
			catch
			{
				BelgeArizaAlanlariniTemizle(panel);
			}
		}

		private List<string> BelgedekiArizaMetinleriniGetir ( BelgePaneli panel )
		{
			List<string> sonuc = new List<string>();
			if(panel?.ArizaTextBoxlari==null||panel.YapilanIsComboBox!=null)
				return sonuc;

			for(int i = 0 ; i<panel.ArizaTextBoxlari.Length ; i++)
			{
				string metin = panel.ArizaTextBoxlari[i]?.Text?.Trim()??string.Empty;
				if(string.IsNullOrWhiteSpace(metin))
					continue;

				sonuc.Add(metin.ToUpper(_yazdirmaKulturu));
			}

			return sonuc;
		}

		private string YazdirmaYapilanIsMetniGetir ( string kalemAdi , string isBilgisi )
		{
			string temizKalemAdi = ( kalemAdi??string.Empty ).Trim();
			if(!string.IsNullOrWhiteSpace(temizKalemAdi))
				return temizKalemAdi;

			return ( isBilgisi??string.Empty ).Trim();
		}

		private void YazdirmaYapilanIsMetniniEkle ( List<string> hedefListe , HashSet<string> benzersizMetinler , string kalemAdi , string isBilgisi )
		{
			if(hedefListe==null||benzersizMetinler==null)
				return;

			string yapilanIsMetni = YazdirmaYapilanIsMetniGetir(kalemAdi , isBilgisi);
			if(string.IsNullOrWhiteSpace(yapilanIsMetni))
				return;

			if(benzersizMetinler.Add(yapilanIsMetni))
				hedefListe.Add(yapilanIsMetni);
		}

		private ArizaKaynakBilgisi BelgeyeUygunArizaKaynagiGetir ( OleDbConnection conn , BelgePaneli panel )
		{
			if(!_arizaKaynaklariArastirildi)
				ArizaKaynaklariniKesfet(conn);

			if(_arizaKaynaklari.Count==0)
				return null;

			string tercihTablo = panel!=null
				? (panel.TeklifMi ? "Teklifler" : "Faturalar")
				: string.Empty;
			if(!string.IsNullOrWhiteSpace(tercihTablo))
			{
				ArizaKaynakBilgisi dogrudanKaynak = _arizaKaynaklari.FirstOrDefault(k =>
					string.Equals(k.TabloAdi , tercihTablo , StringComparison.OrdinalIgnoreCase));
				if(dogrudanKaynak!=null)
					return dogrudanKaynak;
			}

			string[] oncelik = panel.TeklifMi
				? new[] { "TeklifID" , "CariID" , "FaturaID" }
				: new[] { "FaturaID" , "CariID" , "TeklifID" };

			foreach(string iliskiKolonu in oncelik)
			{
				foreach(ArizaKaynakBilgisi kaynak in _arizaKaynaklari)
				{
					if(string.Equals(kaynak.IliskiKolonu , iliskiKolonu , StringComparison.OrdinalIgnoreCase))
						return kaynak;
				}
			}

			return null;
		}

		private void ArizaKaynaklariniKesfet ( OleDbConnection conn )
		{
			_arizaKaynaklariArastirildi=true;
			_arizaKaynaklari.Clear();

			try
			{
				DataTable schema = conn.GetSchema("Columns");
				Dictionary<string, List<string>> tabloKolonlari = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
				foreach(DataRow row in schema.Rows)
				{
					string tablo = Convert.ToString(row["TABLE_NAME"]);
					string kolon = Convert.ToString(row["COLUMN_NAME"]);
					if(string.IsNullOrWhiteSpace(tablo)||string.IsNullOrWhiteSpace(kolon))
						continue;

					if(!tabloKolonlari.ContainsKey(tablo))
						tabloKolonlari[tablo]=new List<string>();

					tabloKolonlari[tablo].Add(kolon);
				}

				foreach(KeyValuePair<string, List<string>> item in tabloKolonlari)
				{
					List<string> kolonlar = item.Value;
					List<string> arizaKolonlari = kolonlar
						.Where(k => k.IndexOf("ariza" , StringComparison.OrdinalIgnoreCase)>=0)
						.OrderBy(ArizaKolonSirasiGetir)
						.Take(4)
						.ToList();

					if(arizaKolonlari.Count==0)
						continue;

					string iliskiKolonu = null;
					if(kolonlar.Any(k => string.Equals(k , "FaturaID" , StringComparison.OrdinalIgnoreCase)))
						iliskiKolonu="FaturaID";
					else if(kolonlar.Any(k => string.Equals(k , "TeklifID" , StringComparison.OrdinalIgnoreCase)))
						iliskiKolonu="TeklifID";
					else if(kolonlar.Any(k => string.Equals(k , "CariID" , StringComparison.OrdinalIgnoreCase)))
						iliskiKolonu="CariID";

					if(string.IsNullOrWhiteSpace(iliskiKolonu))
						continue;

					string siralamaKolonu = kolonlar.FirstOrDefault(k =>
						k.EndsWith("ID" , StringComparison.OrdinalIgnoreCase)||
						k.IndexOf("Tarih" , StringComparison.OrdinalIgnoreCase)>=0||
						k.IndexOf("Kayit" , StringComparison.OrdinalIgnoreCase)>=0);

					_arizaKaynaklari.Add(new ArizaKaynakBilgisi
					{
						TabloAdi=item.Key,
						IliskiKolonu=iliskiKolonu,
						SiralamaKolonu=siralamaKolonu,
						ArizaKolonlari=arizaKolonlari
					});
				}
			}
			catch
			{
				_arizaKaynaklari.Clear();
			}
		}

		private int ArizaKolonSirasiGetir ( string kolonAdi )
		{
			if(string.IsNullOrWhiteSpace(kolonAdi))
				return int.MaxValue;

			Match match = Regex.Match(kolonAdi , @"(\d+)");
			if(match.Success)
			{
				int sira;
				if(int.TryParse(match.Groups[1].Value , out sira))
					return sira;
			}

			return int.MaxValue-1;
		}

		private void SepetYazdirButonu_Click ( object sender , EventArgs e )
		{
			try
			{
				BelgeYazdirmaVerisi veri = SepetYazdirmaVerisiniHazirla();
				if(veri!=null)
					BelgeYazdirmaOnizlemeAc(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Sepet yazdırma hatası: "+ex.Message);
			}
		}

		private void BelgeYazdirButonu_Click ( object sender , EventArgs e )
		{
			BelgePaneli panel = BelgePaneliniGetir(sender);
			if(panel==null)
				return;

			try
			{
				BelgeYazdirmaVerisi veri = BelgeYazdirmaVerisiniHazirla(panel);
				if(veri!=null)
					BelgeYazdirmaOnizlemeAc(veri);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Belge yazdırma hatası: "+ex.Message);
			}
		}

		private BelgeYazdirmaVerisi SepetYazdirmaVerisiniHazirla ()
		{
			if(dataGridView5==null)
				return null;

			List<BelgeYazdirmaSatiri> satirlar = new List<BelgeYazdirmaSatiri>();
			List<string> yapilanIsler = new List<string>();
			HashSet<string> benzersizYapilanIsler = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
			foreach(DataGridViewRow row in dataGridView5.Rows)
			{
				if(row==null||row.IsNewRow)
					continue;

				string urunAdi = Convert.ToString(row.Cells["urunadi"].Value)??string.Empty;
				if(string.IsNullOrWhiteSpace(urunAdi))
					continue;

				if(SatirYapilanIsKalemiMi(row))
				{
					YazdirmaYapilanIsMetniniEkle(
						yapilanIsler ,
						benzersizYapilanIsler ,
						urunAdi ,
						SatirKolonuVarMi(row , "IsBilgisi") ? Convert.ToString(row.Cells["IsBilgisi"].Value) : string.Empty);
				}

				decimal miktar = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
				decimal birimFiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
				decimal toplam = SepetDecimalParse(Convert.ToString(row.Cells["toplamfiyat"].Value));
				if(toplam<=0)
				{
					toplam=miktar*birimFiyat;
				}

				satirlar.Add(new BelgeYazdirmaSatiri
				{
					UrunAdi=urunAdi,
					Birim=Convert.ToString(row.Cells["birim"].Value)??string.Empty,
					Miktar=miktar,
					BirimFiyat=birimFiyat,
					ToplamTutar=toplam
				});
			}

			if(satirlar.Count==0)
			{
				MessageBox.Show("Yazdırılacak sepet satırı bulunamadı.");
				return null;
			}

			decimal araToplam = satirlar.Sum(s => s.ToplamTutar);
			decimal kdvOrani = SepetDecimalParse(textBox39?.Text);
			decimal kdvTutari = araToplam>0&&kdvOrani>0 ? araToplam*kdvOrani/100m : 0m;
			decimal genelToplam = SepetDecimalParse(label48?.Text);
			if(genelToplam<=0)
				genelToplam=araToplam+kdvTutari;
			else
				kdvTutari=Math.Max(0m , genelToplam-araToplam);

			string cariAdi = SepetCariAdMetniGetir();
			bool teklifMi = string.IsNullOrWhiteSpace(cariAdi);
			return new BelgeYazdirmaVerisi
			{
				BelgeBasligi=teklifMi ? "TEKLIF" : "FATURA",
				BelgeNo="SEPET",
				SatirListesiBasligi=teklifMi ? "İŞ TEKLİFİ" : "YAPILAN İŞLER",
				Tarih=DateTime.Now,
				CariAdi=BosIseYerineGetir(cariAdi),
				CariTelefon=BosIseYerineGetir(textBox26?.Text),
				YapilanIsler=yapilanIsler,
				Satirlar=satirlar,
				AraToplam=araToplam,
				KdvTutari=kdvTutari,
				GenelToplam=genelToplam
			};
		}

		private BelgeYazdirmaVerisi BelgeYazdirmaVerisiniHazirla ( BelgePaneli panel )
		{
			if(panel==null)
				return null;

			if(!panel.SeciliKayitId.HasValue)
			{
				DataGridViewRow seciliSatir = panel.UstGrid?.CurrentRow;
				if(seciliSatir!=null&&!seciliSatir.IsNewRow)
				{
					if(panel.TeklifMi)
						TeklifBaslikSatiriniSec(panel , seciliSatir , panel.SeciliDetayId);
					else
						FaturaBaslikSatiriniSec(panel , seciliSatir , panel.SeciliDetayId);
				}
			}

			if(!panel.SeciliKayitId.HasValue)
			{
				MessageBox.Show("Yazdırmak için önce bir kayıt seçin.");
				return null;
			}

			BelgeYazdirmaVerisi veri = new BelgeYazdirmaVerisi
			{
				BelgeBasligi=panel.TeklifMi ? "TEKLIF" : "FATURA",
				BelgeSiraNo=panel.SeciliKayitId,
				SatirListesiBasligi=
					panel.Tur==BelgeKayitTuru.Teklif
						? "İŞ TEKLİFİ"
						: panel.Tur==BelgeKayitTuru.FabrikaFaturasi||
						  panel.Tur==BelgeKayitTuru.MusteriFaturasi
						? "YAPILAN İŞLER"
						: "MALZEME LİSTESİ"
			};
			if(panel.Tur==BelgeKayitTuru.Teklif||
				panel.Tur==BelgeKayitTuru.FabrikaFaturasi||
				panel.Tur==BelgeKayitTuru.MusteriFaturasi)
			{
				veri.DipnotSatirlari.AddRange(YazdirmaDipnotSatirlari);
			}
			List<string> arizaMetinleri = BelgedekiArizaMetinleriniGetir(panel);
			HashSet<string> benzersizYapilanIsler = new HashSet<string>(veri.YapilanIsler , StringComparer.CurrentCultureIgnoreCase);

			decimal headerToplam = 0m;
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string baslikSorgu = panel.TeklifMi
					? @"SELECT
							T.TeklifNo AS BelgeNo,
							T.TeklifTarihi AS BelgeTarihi,
							T.ToplamTutar,
							IIF(C.adsoyad IS NULL, '', C.adsoyad) AS CariAdi,
							IIF(C.telefon IS NULL, '', C.telefon) AS CariTelefon
						FROM Teklifler AS T
						LEFT JOIN Cariler AS C ON CLng(IIF(T.CariID IS NULL, 0, T.CariID)) = C.CariID
						WHERE T.TeklifID=?"
					: @"SELECT
							F.FaturaNo AS BelgeNo,
							F.FaturaTarihi AS BelgeTarihi,
							F.ToplamTutar,
							IIF(C.adsoyad IS NULL, '', C.adsoyad) AS CariAdi,
							IIF(C.telefon IS NULL, '', C.telefon) AS CariTelefon
						FROM Faturalar AS F
						LEFT JOIN Cariler AS C ON CLng(IIF(F.CariID IS NULL, 0, F.CariID)) = C.CariID
						WHERE F.FaturaID=?";

				using(OleDbCommand cmd = new OleDbCommand(baslikSorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=panel.SeciliKayitId.Value;
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						if(rd==null||!rd.Read())
						{
							MessageBox.Show("Seçili kayıt yazdırma için bulunamadı.");
							return null;
						}

						veri.BelgeNo=rd["BelgeNo"]==DBNull.Value ? string.Empty : Convert.ToString(rd["BelgeNo"]);
						veri.CariAdi=BosIseYerineGetir(rd["CariAdi"]?.ToString());
						veri.CariTelefon=BosIseYerineGetir(rd["CariTelefon"]?.ToString());
						if(rd["BelgeTarihi"]!=DBNull.Value)
							veri.Tarih=Convert.ToDateTime(rd["BelgeTarihi"]);
						headerToplam=rd["ToplamTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTutar"]);
					}
				}

				veri.YapilanIsler.AddRange(arizaMetinleri);
				foreach(string arizaMetni in arizaMetinleri)
				{
					if(!string.IsNullOrWhiteSpace(arizaMetni))
						benzersizYapilanIsler.Add(arizaMetni);
				}

				string detaySorgu = panel.TeklifMi
					? @"SELECT
							TD.YapilanIsID,
							IIF(TD.KalemTuru IS NULL, '', TD.KalemTuru) AS KalemTuru,
							IIF(TD.IsBilgisi IS NULL, '', TD.IsBilgisi) AS IsBilgisi,
							IIF(TD.KalemAdi IS NULL OR TD.KalemAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), TD.KalemAdi) AS UrunAdi,
							IIF(TD.Birim IS NULL OR TD.Birim='', IIF(B.BirimAdi IS NULL, '', B.BirimAdi), TD.Birim) AS Birim,
							TD.Miktar,
							IIF(TD.BirimFiyat IS NULL, 0, TD.BirimFiyat) AS BirimFiyat,
							IIF(TD.AraToplam IS NULL, 0, TD.AraToplam) AS ToplamFiyat
						FROM ((TeklifDetaylari AS TD
						LEFT JOIN Urunler AS U ON CLng(IIF(TD.UrunID IS NULL, 0, TD.UrunID)) = U.UrunID)
						LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
						LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
						WHERE CLng(IIF(TD.TeklifID IS NULL, 0, TD.TeklifID)) = ?
						ORDER BY TD.TeklifDetayID"
					: @"SELECT
							FD.YapilanIsID,
							IIF(FD.KalemTuru IS NULL, '', FD.KalemTuru) AS KalemTuru,
							IIF(FD.IsBilgisi IS NULL, '', FD.IsBilgisi) AS IsBilgisi,
							IIF(FD.KalemAdi IS NULL OR FD.KalemAdi='', IIF(U.UrunAdi IS NULL, '', U.UrunAdi), FD.KalemAdi) AS UrunAdi,
							IIF(FD.Birim IS NULL OR FD.Birim='', IIF(B.BirimAdi IS NULL, '', B.BirimAdi), FD.Birim) AS Birim,
							FD.Miktar,
							IIF(FD.SatisFiyati IS NULL, 0, FD.SatisFiyati) AS BirimFiyat,
							(IIF(FD.Miktar IS NULL, 0, FD.Miktar) * IIF(FD.SatisFiyati IS NULL, 0, FD.SatisFiyati)) AS ToplamFiyat
						FROM ((FaturaDetay AS FD
						LEFT JOIN Urunler AS U ON CLng(IIF(FD.UrunID IS NULL, 0, FD.UrunID)) = U.UrunID)
						LEFT JOIN Markalar AS M ON CLng(IIF(U.MarkaID IS NULL, 0, U.MarkaID)) = M.MarkaID)
						LEFT JOIN Birimler AS B ON U.BirimID = B.BirimID
						WHERE CLng(IIF(FD.FaturaID IS NULL, 0, FD.FaturaID)) = ?
						ORDER BY FD.FaturaDetayID";

				using(OleDbCommand cmd = new OleDbCommand(detaySorgu , conn))
				{
					cmd.Parameters.Add("?" , OleDbType.Integer).Value=panel.SeciliKayitId.Value;
					using(OleDbDataReader rd = cmd.ExecuteReader())
					{
						while(rd!=null&&rd.Read())
						{
							int? yapilanIsId = rd["YapilanIsID"]==DBNull.Value ? ( int? )null : Convert.ToInt32(rd["YapilanIsID"]);
							string kalemTuru = Convert.ToString(rd["KalemTuru"])??string.Empty;
							bool yapilanIsSatiri = yapilanIsId.HasValue&&yapilanIsId.Value>0;
							if(!yapilanIsSatiri&&!string.IsNullOrWhiteSpace(kalemTuru))
							{
								string normalizeKalemTuru = AramaMetniniNormalizeEt(kalemTuru);
								yapilanIsSatiri=normalizeKalemTuru.Contains("yapilan is")||normalizeKalemTuru.Contains("hizmet");
							}

							string urunAdi = rd["UrunAdi"]?.ToString()??string.Empty;
							string isBilgisi = rd["IsBilgisi"]?.ToString()??string.Empty;
							if(yapilanIsSatiri)
								YazdirmaYapilanIsMetniniEkle(veri.YapilanIsler , benzersizYapilanIsler , urunAdi , isBilgisi);

							veri.Satirlar.Add(new BelgeYazdirmaSatiri
							{
								UrunAdi=urunAdi,
								Birim=rd["Birim"]?.ToString()??string.Empty,
								Miktar=rd["Miktar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["Miktar"]),
								BirimFiyat=rd["BirimFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["BirimFiyat"]),
								ToplamTutar=rd["ToplamFiyat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamFiyat"])
							});
						}
					}
				}
			}

			if(veri.Satirlar.Count==0)
			{
				MessageBox.Show("Seçili kayda ait yazdırılacak satır bulunamadı.");
				return null;
			}

			veri.AraToplam=veri.Satirlar.Sum(s => s.ToplamTutar);
			veri.GenelToplam=headerToplam>0 ? headerToplam : veri.AraToplam;
			veri.KdvTutari=Math.Max(0m , veri.GenelToplam-veri.AraToplam);
			if(!veri.Tarih.HasValue)
				veri.Tarih=DateTime.Now;

			return veri;
		}

		private void BelgeYazdirmaOnizlemeAc ( BelgeYazdirmaVerisi veri )
		{
			if(veri==null)
				return;

			_aktifBelgeYazdirmaVerisi=veri;
			_aktifBelgeYazdirmaSatirIndex=0;
			_aktifBelgeYazdirmaSayfaNo=0;
			using(PrintDocument belge = new PrintDocument())
			using(PrintPreviewDialog onizleme = new PrintPreviewDialog())
			{
				belge.DocumentName=( veri.BelgeBasligi??"Belge" )+" "+( veri.BelgeNo??string.Empty );
				belge.DefaultPageSettings.Margins=new Margins(40 , 40 , 35 , 35);
				belge.BeginPrint+=BelgeYazdirmaBelgesi_BeginPrint;
				belge.PrintPage+=BelgeYazdirmaBelgesi_PrintPage;

				onizleme.Document=belge;
				onizleme.Width=1200;
				onizleme.Height=850;
				onizleme.WindowState=FormWindowState.Maximized;
				onizleme.ShowIcon=false;
				onizleme.UseAntiAlias=true;
				onizleme.ShowDialog(this);
			}
		}

		private void BelgeYazdirmaBelgesi_BeginPrint ( object sender , PrintEventArgs e )
		{
			_aktifBelgeYazdirmaSatirIndex=0;
			_aktifBelgeYazdirmaSayfaNo=0;
		}

		private void BelgeYazdirmaBelgesi_PrintPage ( object sender , PrintPageEventArgs e )
		{
			if(_aktifBelgeYazdirmaVerisi==null)
			{
				e.HasMorePages=false;
				return;
			}

			_aktifBelgeYazdirmaSayfaNo++;

			Graphics g = e.Graphics;
			g.SmoothingMode=SmoothingMode.AntiAlias;

			Rectangle sayfa = e.MarginBounds;
			int sol = sayfa.Left;
			int sag = sayfa.Right;
			int y = sayfa.Top;
			int genislik = sayfa.Width;
			bool ilkSayfa = _aktifBelgeYazdirmaSayfaNo==1;

			using(Font firmaFont = new Font("Arial" , 16f , FontStyle.Bold))
			using(Font bilgiFont = new Font("Arial" , 10.5f , FontStyle.Regular))
			using(Font baslikFont = new Font("Arial" , 20f , FontStyle.Bold))
			using(Font bolumFont = new Font("Arial" , 10.8f , FontStyle.Bold))
			using(Font tabloBaslikFont = new Font("Arial" , 10.5f , FontStyle.Bold))
			using(Font satirFont = new Font("Arial" , 10.3f , FontStyle.Regular))
			using(Font dipnotFont = new Font("Arial" , 9.6f , FontStyle.Regular))
			using(Font dipnotKalinFont = new Font("Arial" , 9.6f , FontStyle.Bold))
			using(Font toplamFont = new Font("Arial" , 11.2f , FontStyle.Bold))
			using(Pen cizgiKalemi = new Pen(Color.Black , 1.2f))
			using(Pen dipnotKalemi = new Pen(Color.FromArgb(210 , 210 , 210) , 1f))
			using(Brush siyah = new SolidBrush(Color.Black))
			using(Brush gri = new SolidBrush(Color.DimGray))
			using(Brush dipnotFirca = new SolidBrush(Color.Black))
			using(StringFormat sagaHizali = new StringFormat())
			using(StringFormat ortayaHizali = new StringFormat())
			using(StringFormat satirMetniFormati = new StringFormat())
			{
				sagaHizali.Alignment=StringAlignment.Far;
				sagaHizali.LineAlignment=StringAlignment.Near;
				ortayaHizali.Alignment=StringAlignment.Center;
				ortayaHizali.LineAlignment=StringAlignment.Center;
				satirMetniFormati.Trimming=StringTrimming.EllipsisWord;
				satirMetniFormati.FormatFlags=StringFormatFlags.LineLimit;
				bool dipnotBoslukEkle = _aktifBelgeYazdirmaVerisi.DipnotSatirlari.Exists(
					x => x.StartsWith("NOT:" , StringComparison.OrdinalIgnoreCase));
				int dipnotYuksekligi = _aktifBelgeYazdirmaVerisi.DipnotSatirlari.Count>0
					? (_aktifBelgeYazdirmaVerisi.DipnotSatirlari.Count*20)+18+(dipnotBoslukEkle?20:0)
					: 0;

				if(ilkSayfa)
				{
					Rectangle logoAlani = new Rectangle(sol , y+4 , 158 , 110);
					YazdirmaLogoCiz(g , logoAlani);

					int firmaX = logoAlani.Right+10;
					g.DrawString(YazdirmaSirketAdi , firmaFont , siyah , firmaX , y+18);
					g.DrawString(YazdirmaSirketAdres , bilgiFont , siyah , firmaX , y+48);
					g.DrawString(YazdirmaSirketTelefon , bilgiFont , siyah , firmaX , y+69);

					RectangleF baslikAlan = new RectangleF(sag-230 , y+18 , 230 , 30);
					g.DrawString(BosIseYerineGetir(_aktifBelgeYazdirmaVerisi.BelgeBasligi) , baslikFont , siyah , baslikAlan , sagaHizali);
					g.DrawString((_aktifBelgeYazdirmaVerisi.Tarih??DateTime.Now).ToString("dd.MM.yyyy") , bilgiFont , gri , new RectangleF(sag-230 , y+53 , 230 , 18) , sagaHizali);
					if(!string.IsNullOrWhiteSpace(_aktifBelgeYazdirmaVerisi.BelgeNo))
						g.DrawString(_aktifBelgeYazdirmaVerisi.BelgeNo , bilgiFont , gri , new RectangleF(sag-230 , y+74 , 230 , 18) , sagaHizali);

					y+=128;
					g.DrawString("MÜŞTERİ BİLGİLERİ" , bolumFont , siyah , sol+4 , y);

					int musteriBilgiY = y+24;
					g.DrawString("Ad Soyad: "+BosIseYerineGetir(_aktifBelgeYazdirmaVerisi.CariAdi) , bilgiFont , siyah , sol+4 , musteriBilgiY);
					g.DrawString("Telefon: "+BosIseYerineGetir(_aktifBelgeYazdirmaVerisi.CariTelefon) , bilgiFont , siyah , sol+4 , musteriBilgiY+21);

					int toplamBlokX = sol+( int )(genislik*0.56f);
					int toplamEtiketGenislik = 165;
					int toplamDegerGenislik = sag-toplamBlokX-toplamEtiketGenislik;
					g.DrawString("Ara Fiyat:" , bilgiFont , siyah , new RectangleF(toplamBlokX , y , toplamEtiketGenislik , 20));
					g.DrawString(ParaMetniGetir(_aktifBelgeYazdirmaVerisi.AraToplam) , bilgiFont , siyah , new RectangleF(toplamBlokX+toplamEtiketGenislik , y , toplamDegerGenislik , 20) , sagaHizali);
					g.DrawString("KDV:" , bilgiFont , siyah , new RectangleF(toplamBlokX , y+24 , toplamEtiketGenislik , 20));
					g.DrawString(ParaMetniGetir(_aktifBelgeYazdirmaVerisi.KdvTutari) , bilgiFont , siyah , new RectangleF(toplamBlokX+toplamEtiketGenislik , y+24 , toplamDegerGenislik , 20) , sagaHizali);
					g.DrawString("Toplam Fiyat:" , toplamFont , siyah , new RectangleF(toplamBlokX , y+48 , toplamEtiketGenislik , 22));
					g.DrawString(ParaMetniGetir(_aktifBelgeYazdirmaVerisi.GenelToplam) , toplamFont , siyah , new RectangleF(toplamBlokX+toplamEtiketGenislik , y+48 , toplamDegerGenislik , 22) , sagaHizali);

					int musteriAltY = musteriBilgiY+41;
					int toplamAltY = y+70;
					y=Math.Max(musteriAltY , toplamAltY)+28;
				}
				else
				{
					y+=6;
				}

				if(ilkSayfa)
				{
					string satirBasligi = string.IsNullOrWhiteSpace(_aktifBelgeYazdirmaVerisi.SatirListesiBasligi)
						? "MALZEME LİSTESİ"
						: _aktifBelgeYazdirmaVerisi.SatirListesiBasligi;

					int ortaBaslikX = sol+(genislik/2);
					int yapiAltiY = y+8;
					g.DrawLine(cizgiKalemi , sol , yapiAltiY , ortaBaslikX-78 , yapiAltiY);
					g.DrawString(satirBasligi , bolumFont , siyah , new RectangleF(ortaBaslikX-112 , y-2 , 224 , 22) , ortayaHizali);
					g.DrawLine(cizgiKalemi , ortaBaslikX+78 , yapiAltiY , sag , yapiAltiY);
					y+=42;

					foreach(string yapilanIsMetni in _aktifBelgeYazdirmaVerisi.YapilanIsler)
					{
						SizeF aciklamaBoyutu = g.MeasureString(BosIseYerineGetir(yapilanIsMetni) , satirFont , Math.Max(10 , genislik-12));
						int aciklamaYuksekligi = Math.Max(24 , ( int )Math.Ceiling(aciklamaBoyutu.Height)+4);
						RectangleF aciklamaRect = new RectangleF(sol+4 , y , genislik-8 , aciklamaYuksekligi);
						g.DrawString(BosIseYerineGetir(yapilanIsMetni) , satirFont , siyah , aciklamaRect , satirMetniFormati);
						y+=aciklamaYuksekligi;
					}

					if(_aktifBelgeYazdirmaVerisi.YapilanIsler.Count>0)
						y+=10;
				}
				else
				{
					y+=6;
				}

				int urunGenisligi = ( int )(genislik*0.42f);
				int birimGenisligi = ( int )(genislik*0.12f);
				int miktarGenisligi = ( int )(genislik*0.11f);
				int birimFiyatGenisligi = ( int )(genislik*0.15f);
				int toplamGenisligi = genislik-urunGenisligi-birimGenisligi-miktarGenisligi-birimFiyatGenisligi;

				int urunX = sol;
				int birimX = urunX+urunGenisligi;
				int miktarX = birimX+birimGenisligi;
				int birimFiyatX = miktarX+miktarGenisligi;
				int toplamX = birimFiyatX+birimFiyatGenisligi;

				RectangleF urunBaslikRect = new RectangleF(urunX+4 , y , urunGenisligi-10 , 18);
				RectangleF birimBaslikRect = new RectangleF(birimX+4 , y , birimGenisligi-8 , 18);
				RectangleF miktarBaslikRect = new RectangleF(miktarX+4 , y , miktarGenisligi-10 , 18);
				RectangleF birimFiyatBaslikRect = new RectangleF(birimFiyatX+4 , y , birimFiyatGenisligi-10 , 18);
				RectangleF toplamBaslikRect = new RectangleF(toplamX+4 , y , toplamGenisligi-10 , 18);

				g.DrawString("ÜRÜN ADI" , tabloBaslikFont , siyah , urunBaslikRect);
				g.DrawString("BİRİM" , tabloBaslikFont , siyah , birimBaslikRect , sagaHizali);
				g.DrawString("MİKTAR" , tabloBaslikFont , siyah , miktarBaslikRect , sagaHizali);
				g.DrawString("BİRİM FİYAT" , tabloBaslikFont , siyah , birimFiyatBaslikRect , sagaHizali);
				g.DrawString("TOPLAM" , tabloBaslikFont , siyah , toplamBaslikRect , sagaHizali);
				y+=20;
				g.DrawLine(cizgiKalemi , sol , y , sag , y);
				y+=8;

				bool satirYazildi=false;
				int sayfaAltBosluk = 18+dipnotYuksekligi;
				while(_aktifBelgeYazdirmaSatirIndex<_aktifBelgeYazdirmaVerisi.Satirlar.Count)
				{
					BelgeYazdirmaSatiri satir = _aktifBelgeYazdirmaVerisi.Satirlar[_aktifBelgeYazdirmaSatirIndex];
					SizeF urunBoyutu = g.MeasureString(BosIseYerineGetir(satir.UrunAdi) , satirFont , Math.Max(10 , urunGenisligi-12));
					int satirYuksekligi = Math.Max(24 , ( int )Math.Ceiling(urunBoyutu.Height)+4);

					if(y+satirYuksekligi+sayfaAltBosluk>sayfa.Bottom)
					{
						if(!satirYazildi&&ilkSayfa)
						{
							e.HasMorePages=true;
							return;
						}

						if(satirYazildi)
							break;

						satirYuksekligi=Math.Max(24 , sayfa.Bottom-y-sayfaAltBosluk);
					}

					RectangleF urunRect = new RectangleF(urunX+4 , y , urunGenisligi-10 , satirYuksekligi);
					RectangleF birimRect = new RectangleF(birimX+4 , y , birimGenisligi-8 , satirYuksekligi);
					RectangleF miktarRect = new RectangleF(miktarX+4 , y , miktarGenisligi-10 , satirYuksekligi);
					RectangleF birimFiyatRect = new RectangleF(birimFiyatX+4 , y , birimFiyatGenisligi-10 , satirYuksekligi);
					RectangleF toplamRect = new RectangleF(toplamX+4 , y , toplamGenisligi-10 , satirYuksekligi);

					g.DrawString(BosIseYerineGetir(satir.UrunAdi) , satirFont , siyah , urunRect , satirMetniFormati);
					g.DrawString(BosIseYerineGetir(satir.Birim) , satirFont , siyah , birimRect , sagaHizali);
					g.DrawString(satir.Miktar.ToString("0.##" , _yazdirmaKulturu) , satirFont , siyah , miktarRect , sagaHizali);
					g.DrawString(ParaMetniGetir(satir.BirimFiyat) , satirFont , siyah , birimFiyatRect , sagaHizali);
					g.DrawString(ParaMetniGetir(satir.ToplamTutar) , satirFont , siyah , toplamRect , sagaHizali);

					y+=satirYuksekligi;
					_aktifBelgeYazdirmaSatirIndex++;
					satirYazildi=true;
				}

				g.DrawLine(cizgiKalemi , sol , y+4 , sag , y+4);

				if(_aktifBelgeYazdirmaSatirIndex>=_aktifBelgeYazdirmaVerisi.Satirlar.Count&&
					_aktifBelgeYazdirmaVerisi.DipnotSatirlari.Count>0)
				{
					int dipnotUstY = sayfa.Bottom-dipnotYuksekligi;
					g.DrawLine(dipnotKalemi , sol , dipnotUstY , sag , dipnotUstY);

					int dipnotY = dipnotUstY+8;
					foreach(string dipnotSatiri in _aktifBelgeYazdirmaVerisi.DipnotSatirlari)
					{
						bool notSatiri = dipnotSatiri.StartsWith("NOT:" , StringComparison.OrdinalIgnoreCase);
						Brush aktifDipnotFirca = dipnotSatiri.StartsWith("NOT:" , StringComparison.OrdinalIgnoreCase)
							? siyah
							: dipnotFirca;
						Font aktifDipnotFont = notSatiri
							? dipnotKalinFont
							: dipnotFont;
						g.DrawString(
							dipnotSatiri ,
							aktifDipnotFont ,
							aktifDipnotFirca ,
							new RectangleF(sol+4 , dipnotY , genislik-8 , 18));
						dipnotY+=20;
						if(notSatiri)
							dipnotY+=20;
					}
				}
			}

			e.HasMorePages=_aktifBelgeYazdirmaSatirIndex<_aktifBelgeYazdirmaVerisi.Satirlar.Count;
		}

		private void YazdirmaLogoCiz ( Graphics g , Rectangle alan )
		{
			if(g==null)
				return;

			Image logo = YazdirmaLogoGorseliniGetir();
			if(logo!=null)
			{
				Rectangle hedef = SolaYasliLogoAlaniGetir(alan , logo.Size);
				g.DrawImage(logo , hedef);
				return;
			}

			Rectangle yaziAlani = new Rectangle(alan.X , alan.Y-2 , alan.Width , ( int )(alan.Height*0.58f));

			using(Font logoFont = LogoFontunuUydur(g , "AST" , yaziAlani , "Arial Black" , FontStyle.Bold , 46f , 24f))
			using(Pen griKalem = new Pen(Color.FromArgb(170 , 170 , 170) , 3.6f))
			using(Pen siyahKalem = new Pen(Color.FromArgb(40 , 40 , 40) , 2.2f))
			using(Brush siyah = new SolidBrush(Color.Black))
			using(GraphicsPath yay = new GraphicsPath())
			{
				SizeF yaziBoyutu = g.MeasureString("AST" , logoFont , PointF.Empty , StringFormat.GenericTypographic);
				float yaziX = yaziAlani.X+((yaziAlani.Width-yaziBoyutu.Width)/2f);
				float yaziY = yaziAlani.Y+Math.Max(0f , ((yaziAlani.Height-yaziBoyutu.Height)/2f)-1f);
				g.DrawString("AST" , logoFont , siyah , yaziX , yaziY , StringFormat.GenericTypographic);

				yay.AddBezier(
					new Point(alan.X+6 , alan.Bottom-20) ,
					new Point(alan.X+38 , alan.Bottom-34) ,
					new Point(alan.Right-36 , alan.Bottom-34) ,
					new Point(alan.Right-6 , alan.Bottom-19));
				g.DrawPath(griKalem , yay);

				Rectangle taban = new Rectangle(alan.X+(alan.Width/2)-19 , alan.Bottom-19 , 38 , 12);
				g.DrawEllipse(siyahKalem , taban);
			}
		}

		private Image YazdirmaLogoGorseliniGetir ()
		{
			if(_yazdirmaLogoGorseli!=null)
				return _yazdirmaLogoGorseli;

			string[] adayYollar =
			{
				Path.Combine(Application.StartupPath , "Resources" , "ast-logo.png"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "Resources" , "ast-logo.png"),
				Path.Combine(Application.StartupPath , ".." , ".." , "Resources" , "ast-logo.png"),
				Path.Combine(Application.StartupPath , ".." , ".." , "Resources" , "ast-logo.png.png"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory , ".." , ".." , "Resources" , "ast-logo.png"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory , ".." , ".." , "Resources" , "ast-logo.png.png"),
				Path.Combine(Application.StartupPath , "ast-logo.png"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "ast-logo.png")
			};

			foreach(string yol in adayYollar)
			{
				try
				{
					if(!File.Exists(yol))
						continue;

					using(Image img = Image.FromFile(yol))
						_yazdirmaLogoGorseli=new Bitmap(img);
					return _yazdirmaLogoGorseli;
				}
				catch
				{
				}
			}

			return null;
		}

		private Rectangle SolaYasliLogoAlaniGetir ( Rectangle alan , Size kaynakBoyut )
		{
			if(kaynakBoyut.Width<=0||kaynakBoyut.Height<=0)
				return alan;

			float oran = Math.Min(( float )alan.Width/kaynakBoyut.Width , ( float )alan.Height/kaynakBoyut.Height);
			int hedefGenislik = Math.Max(1 , ( int )(kaynakBoyut.Width*oran));
			int hedefYukseklik = Math.Max(1 , ( int )(kaynakBoyut.Height*oran));
			int x = alan.X;
			int y = alan.Y+((alan.Height-hedefYukseklik)/2);
			return new Rectangle(x , y , hedefGenislik , hedefYukseklik);
		}

		private Font LogoFontunuUydur ( Graphics g , string metin , Rectangle alan , string fontAdi , FontStyle stil , float baslangicBoyutu , float minimumBoyut )
		{
			for(float boyut = baslangicBoyutu ; boyut>=minimumBoyut ; boyut-=1f)
			{
				Font aday = new Font(fontAdi , boyut , stil , GraphicsUnit.Point);
				SizeF olcu = g.MeasureString(metin , aday , PointF.Empty , StringFormat.GenericTypographic);
				if(olcu.Width<=alan.Width*0.98f&&olcu.Height<=alan.Height*0.95f)
					return aday;
				aday.Dispose();
			}

			return new Font(fontAdi , minimumBoyut , stil , GraphicsUnit.Point);
		}

		private string BosIseYerineGetir ( string metin )
		{
			return string.IsNullOrWhiteSpace(metin) ? "-" : metin.Trim();
		}

		private string ParaMetniGetir ( decimal tutar )
		{
			return "₺"+tutar.ToString("N2" , _yazdirmaKulturu);
		}

		private void DataGridView5_CellValueChanged ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0) return;

			if(dataGridView5.Columns[e.ColumnIndex].Name=="adet"||dataGridView5.Columns[e.ColumnIndex].Name=="SatisFiyati")
			{
				DataGridViewRow row = dataGridView5.Rows[e.RowIndex];
				decimal adet = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
				decimal fiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
				row.Cells["toplamfiyat"].Value=adet*fiyat;
			}

			SepetGenelToplamHesapla();
			GridAramaFiltresiniUygula(textBox31 , dataGridView5);
		}

		private void DataGridView5_RowsAdded ( object sender , DataGridViewRowsAddedEventArgs e )
		{
			SepetGenelToplamHesapla();
			GridAramaFiltresiniUygula(textBox31 , dataGridView5);
		}

		private void DataGridView5_RowsRemoved ( object sender , DataGridViewRowsRemovedEventArgs e )
		{
			SepetGenelToplamHesapla();
			GridAramaFiltresiniUygula(textBox31 , dataGridView5);
		}

		private void DataGridView5_CurrentCellDirtyStateChanged ( object sender , EventArgs e )
		{
			if(dataGridView5.IsCurrentCellDirty)
				dataGridView5.CommitEdit(DataGridViewDataErrorContexts.Commit);
		}

		private void DataGridView5_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0||e.RowIndex>=dataGridView5.Rows.Count) return;
			SepetSatirSec(dataGridView5.Rows[e.RowIndex]);
		}

		private void SepetSatirSec ( DataGridViewRow row )
		{
			if(row==null||row.IsNewRow) return;

			SepetUrunSatiriniNormalizeEtGerekirse(row);

			_sepetSatirSeciliyor=true;
			_sepetUrunDolduruluyor=true;
			try
			{
				bool yapilanIsSatiri = SatirYapilanIsKalemiMi(row);
				int urunId;
				if(!yapilanIsSatiri&&int.TryParse(Convert.ToString(row.Cells["UrunID"].Value) , out urunId))
					_sepetUrunId=urunId;
				else
					_sepetUrunId=null;

				_sepetYapilanIsId=yapilanIsSatiri ? SatirdanIntGetir(row , "YapilanIsID") : null;
				_sepetMarka=yapilanIsSatiri ? string.Empty : Convert.ToString(row.Cells["marka"].Value)??"";
				_sepetKategori=yapilanIsSatiri ? string.Empty : Convert.ToString(row.Cells["kategori"].Value)??"";
				_sepetBirim=Convert.ToString(row.Cells["birim"].Value)??"";

				if(yapilanIsSatiri)
				{
					SepetUrunGirisTemizle();
					_sepetYapilanIsDolduruluyor=true;
					try
					{
						if(_sepetYapilanIsComboBox!=null)
							_sepetYapilanIsComboBox.Text=Convert.ToString(row.Cells["urunadi"].Value)??string.Empty;
					}
					finally
					{
						_sepetYapilanIsDolduruluyor=false;
					}

					if(textBox35!=null)
						textBox35.Text=Convert.ToString(row.Cells["urunadi"].Value)??string.Empty;
					if(textBox36!=null)
					{
						decimal kalemAdedi = SatirKolonuVarMi(row , "KalemAdet")
							? SepetDecimalParse(Convert.ToString(row.Cells["KalemAdet"].Value))
							: 0m;
						textBox36.Text=( kalemAdedi<=0 ? 1m : kalemAdedi ).ToString("0.##" , _yazdirmaKulturu);
					}
					if(textBox4!=null)
						textBox4.Text=SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value)).ToString("N2");
				}
				else
				{
					SepetYapilanIsSeciminiTemizle(true);
					SepetUrunGirisMetniniAyarla(Convert.ToString(row.Cells["urunadi"].Value)??string.Empty);
				}

				if(textBox28!=null) textBox28.Text=_sepetBirim;
				if(textBox29!=null)
				{
					decimal adet = SepetDecimalParse(Convert.ToString(row.Cells["adet"].Value));
					textBox29.Text=adet.ToString("0.##" , new CultureInfo("tr-TR"));
				}
				if(textBox30!=null)
				{
					decimal fiyat = SepetDecimalParse(Convert.ToString(row.Cells["SatisFiyati"].Value));
					textBox30.Text=fiyat.ToString("N2");
				}
				if(textBox32!=null)
				{
					decimal toplam = SepetDecimalParse(Convert.ToString(row.Cells["toplamfiyat"].Value));
					textBox32.Text=toplam.ToString("N2");
				}

				row.Selected=true;
				if(dataGridView5.CurrentCell==null||dataGridView5.CurrentCell.RowIndex!=row.Index)
					dataGridView5.CurrentCell=row.Cells.Cast<DataGridViewCell>()
						.FirstOrDefault(c => dataGridView5.Columns[c.ColumnIndex].Visible);
			}
			finally
			{
				_sepetUrunDolduruluyor=false;
				_sepetSatirSeciliyor=false;
			}
		}

		private void comboCariTip_SelectedIndexChanged ( object sender , EventArgs e )
		{
		}

		private void btnEkle_Click_1 ( object sender , EventArgs e )
		{
			// Boş kontrol
			if(string.IsNullOrEmpty(txtTCVKN.Text)||string.IsNullOrEmpty(txtAdSoyad.Text)||
			   string.IsNullOrEmpty(txtTelefon.Text)||string.IsNullOrEmpty(txtAdres.Text)||
			   cmbCariDurum.SelectedIndex==-1||cmbCariTip.SelectedIndex==-1)
			{
				MessageBox.Show("Tüm alanları doldurun!");
				return;
			}

			try
			{
				int cariDurumID = Convert.ToInt32(cmbCariDurum.SelectedValue);
				int cariTipID = Convert.ToInt32(cmbCariTip.SelectedValue);

				baglanti.Open();

				// --- KONTROL ADIMI BAŞLANGIÇ ---
				// Aynı isimde biri var mı bakıyoruz
				string kontrolSorgu = "SELECT COUNT(*) FROM Cariler WHERE adsoyad = @adsoyad";
				OleDbCommand kontrolCmd = new OleDbCommand(kontrolSorgu , baglanti);
				kontrolCmd.Parameters.AddWithValue("@adsoyad" , txtAdSoyad.Text.Trim());

				int kayitSayisi = Convert.ToInt32(kontrolCmd.ExecuteScalar());

				if(kayitSayisi>0)
				{
					MessageBox.Show("Bu isimde bir cari zaten kayıtlı!" , "Uyarı" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
					return; // İşlemi burada kesiyoruz, aşağıdaki INSERT çalışmaz.
				}
				// --- KONTROL ADIMI BİTİŞ ---

				string sorgu = "INSERT INTO Cariler (tc, adsoyad, telefon, adres, CariDurumID, CariTipID) "+
							   "VALUES (?, ?, ?, ?, ?, ?)";
				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
				cmd.Parameters.Clear();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtTCVKN.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtAdSoyad.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtTelefon.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtAdres.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariDurumID;
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariTipID;

				cmd.ExecuteNonQuery();
				MessageBox.Show("Cari başarıyla eklendi!");

				Temizle3();
				Listele();
				CariHesapVerileriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
			}
			finally
			{
				if(baglanti.State==ConnectionState.Open)
					baglanti.Close();
			}



		}

		private void btnCariSil_Click_1 ( object sender , EventArgs e )
		{

			if(string.IsNullOrEmpty(txtID.Text))
			{
				// İkonlu Uyarı Mesajı (Warning)
				MessageBox.Show("Lütfen silinecek bir kayıt seçin!" , "Uyarı" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			// Kullanıcıya Soru Sor (Question İkonu)
			DialogResult secim = MessageBox.Show("Bu cari kaydını silmek istediğinize emin misiniz?" , "Silme Onayı" , MessageBoxButtons.YesNo , MessageBoxIcon.Question);

			if(secim==DialogResult.Yes)
			{
				try
				{
					baglanti.Open();
					string silSorgu = "DELETE FROM Cariler WHERE CariID=@id";
					OleDbCommand cmdSil = new OleDbCommand(silSorgu , baglanti);
					cmdSil.Parameters.AddWithValue("@id" , txtID.Text);
					cmdSil.ExecuteNonQuery();

					// Sayaç sıfırlama kontrolü
					string kontrolSorgu = "SELECT COUNT(*) FROM Cariler";
					OleDbCommand cmdKontrol = new OleDbCommand(kontrolSorgu , baglanti);
					int kayitSayisi = (int)cmdKontrol.ExecuteScalar();

					if(kayitSayisi==0)
					{
						string sifirlaSorgu = "ALTER TABLE Cariler ALTER COLUMN CariID COUNTER(1,1)";
						new OleDbCommand(sifirlaSorgu , baglanti).ExecuteNonQuery();
						// Bilgi Mesajı (Information)
						MessageBox.Show("Kayıt silindi ve veritabanı sıfırlandı." , "Bilgi" , MessageBoxButtons.OK , MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show("Cari başarıyla silindi." , "Başarılı" , MessageBoxButtons.OK , MessageBoxIcon.Information);
					}

					Temizle();
					Listele();
					CariHesapVerileriniYenile();
				}
				catch(Exception ex)
				{
					// Hata Mesajı (Error)
					MessageBox.Show("Hata oluştu: "+ex.Message , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Error);
				}
				finally { baglanti.Close(); }
			}
		}

		private void btnCariGuncelle_Click_1 ( object sender , EventArgs e )
		{
			if(string.IsNullOrEmpty(txtID.Text))
			{
				MessageBox.Show("Güncellenecek ID girin!");
				return;
			}

			try
			{
				baglanti.Open();
				string sorgu = "UPDATE Cariler SET tc=?, adsoyad=?, telefon=?, adres=?, "+
							   "CariDurumID=?, CariTipID=? WHERE CariID=?";
				OleDbCommand cmd = new OleDbCommand(sorgu , baglanti);
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtTCVKN.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtAdSoyad.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtTelefon.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.VarWChar , 255).Value=txtAdres.Text.Trim();
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=Convert.ToInt32(cmbCariDurum.SelectedValue);
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=Convert.ToInt32(cmbCariTip.SelectedValue);
				cmd.Parameters.Add("?" , OleDbType.Integer).Value=Convert.ToInt32(txtID.Text);

				cmd.ExecuteNonQuery();
				MessageBox.Show("Cari güncellendi!");
				Temizle();
				Listele();
				CariHesapVerileriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata: "+ex.Message);
			}
			finally
			{
				baglanti.Close();
			}


		}

		private void btnCariTemizle_Click_1 ( object sender , EventArgs e )
		{
			Temizle();

		}

		private void dataGridView2_CellContentClick ( object sender , DataGridViewCellEventArgs e )
		{//1.Güvenlik Kontrolü: Başlık satırı (-1) veya en alttaki boş satır(IsNewRow) değilse işlem yap

			if(e.RowIndex>=0&&e.RowIndex<dataGridView2.Rows.Count&&!dataGridView2.Rows[e.RowIndex].IsNewRow)
			{
				try
				{
					DataGridViewRow satir = dataGridView2.Rows[e.RowIndex];

					// 2. Verileri Güvenli Bir Şekilde Aktar (Null Kontrolü ile)
					// ?.Value?.ToString() ?? "" -> Eğer değer null ise hata verme, boş metin yap.

					txtID.Text=satir.Cells["CariID"].Value?.ToString()??"";
					txtTCVKN.Text=satir.Cells["tc"].Value?.ToString()??"";
					txtAdSoyad.Text=satir.Cells["adsoyad"].Value?.ToString()??"";
					txtTelefon.Text=satir.Cells["telefon"].Value?.ToString()??"";
					txtAdres.Text=satir.Cells["adres"].Value?.ToString()??"";

					// 3. ComboBox'ları Eşle (SelectedValue kullanırken null kontrolü)
					if(satir.Cells["CariDurumID"].Value!=null)
					{
						cmbCariDurum.SelectedValue=satir.Cells["CariDurumID"].Value;
					}

					if(satir.Cells["CariTipID"].Value!=null)
					{
						cmbCariTip.SelectedValue=satir.Cells["CariTipID"].Value;
					}

					// Görsel geri bildirim için satırı seçili yapalım
					satir.Selected=true;
				}
				catch(Exception ex)
				{
					// Eğer hala "Nesne başvurusu" hatası alıyorsanız, 
					// sütun isimlerinden biri (Örn: "CariID") kesinlikle yanlıştır.
					// Bu durumda Cells["CariID"] yerine Cells[0] gibi indeks kullanmalısınız.
				}
			}


		}


		private void TabControl2_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(tabControl2.SelectedTab==tabPage17||tabControl2.SelectedTab==tabPage18)
				UrunYonetimSekmeleriniKur();
		}

		private void UrunYonetimSekmeleriniKur ()
		{
			if(dgvKategoriYonetim==dataGridView19&&dgvMarkaYonetim==dataGridView20) return;

			dgvKategoriYonetim=dataGridView19;
			dgvMarkaYonetim=dataGridView20;
			txtKategoriIdYonetim=textBox86;
			txtKategoriAdiYonetim=textBox85;
			txtMarkaIdYonetim=textBox88;
			txtMarkaAdiYonetim=textBox87;

			DatagridviewSetting(dgvKategoriYonetim);
			DatagridviewSetting(dgvMarkaYonetim);
			YonetimButonStilleriniUygula();

			dgvKategoriYonetim.CellClick-=DgvKategoriYonetim_CellClick;
			dgvKategoriYonetim.CellClick+=DgvKategoriYonetim_CellClick;
			dgvMarkaYonetim.CellClick-=DgvMarkaYonetim_CellClick;
			dgvMarkaYonetim.CellClick+=DgvMarkaYonetim_CellClick;

			button51.Click-=Button51_Click;
			button51.Click+=Button51_Click;
			button52.Click-=Button52_Click;
			button52.Click+=Button52_Click;
			button53.Click-=Button53_Click;
			button53.Click+=Button53_Click;
			button54.Click-=Button54_Click;
			button54.Click+=Button54_Click;
			button55.Click-=Button55_Click;
			button55.Click+=Button55_Click;
			button56.Click-=Button56_Click;
			button56.Click+=Button56_Click;
			button57.Click-=Button57_Click;
			button57.Click+=Button57_Click;
			button58.Click-=Button58_Click;
			button58.Click+=Button58_Click;

			KategoriYonetimListele();
			MarkaYonetimListele();
		}

		private void YonetimButonStilleriniUygula ()
		{
			ButonIkonStiliUygula(button51 , "lll.png" , new Point(24 , 214));
			ButonIkonStiliUygula(button52 , "Denied.png" , new Point(24 , 286));
			ButonIkonStiliUygula(button53 , "Update User.png" , new Point(24 , 358));
			TemizleButonStiliUygula(button54 , new Point(24 , 430));

			ButonIkonStiliUygula(button55 , "lll.png" , new Point(24 , 214));
			ButonIkonStiliUygula(button56 , "Denied.png" , new Point(24 , 286));
			ButonIkonStiliUygula(button57 , "Update User.png" , new Point(24 , 358));
			TemizleButonStiliUygula(button58 , new Point(24 , 430));
		}

		private void ButonIkonStiliUygula ( Button buton , string imageKey , Point konum , int? imageIndex = null )
		{
			buton.BackColor=SystemColors.Control;
			buton.BackgroundImageLayout=ImageLayout.Zoom;
			buton.FlatAppearance.BorderColor=SystemColors.Control;
			buton.FlatAppearance.MouseOverBackColor=Color.FromArgb(224 , 224 , 224);
			buton.FlatStyle=FlatStyle.Flat;
			buton.ImageList=imageList1;
			buton.ImageKey=imageKey??string.Empty;
			buton.ImageIndex=imageIndex??-1;
			buton.Location=konum;
			buton.Size=new Size(258 , 62);
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleCenter;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.UseVisualStyleBackColor=false;
		}

		private void TemizleButonStiliUygula ( Button buton , Point konum )
		{
			buton.BackColor=SystemColors.Control;
			buton.BackgroundImageLayout=ImageLayout.Zoom;
			buton.FlatAppearance.BorderColor=SystemColors.Control;
			buton.FlatAppearance.MouseOverBackColor=Color.FromArgb(224 , 224 , 224);
			buton.FlatStyle=FlatStyle.Flat;
			buton.ImageKey="Erase.png";
			buton.ImageList=imageList1;
			buton.Location=konum;
			buton.Size=new Size(258 , 62);
			buton.TextAlign=ContentAlignment.MiddleCenter;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.UseVisualStyleBackColor=false;
		}
		private bool YonetimKaydiVarMi ( string tabloAdi , string alanAdi , string deger , string idAlanAdi , string haricId = null )
		{
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = $"SELECT COUNT(*) FROM {tabloAdi} WHERE {alanAdi}=?";
				if(!string.IsNullOrWhiteSpace(haricId))
					sorgu+=$" AND {idAlanAdi}<>?";

				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , deger.Trim());
					if(!string.IsNullOrWhiteSpace(haricId))
						cmd.Parameters.AddWithValue("?" , Convert.ToInt32(haricId));

					return Convert.ToInt32(cmd.ExecuteScalar())>0;
				}
			}
		}

		private void Button51_Click ( object sender , EventArgs e ) => KategoriYonetimEkle();
		private void Button52_Click ( object sender , EventArgs e ) => KategoriYonetimSil();
		private void Button53_Click ( object sender , EventArgs e ) => KategoriYonetimGuncelle();
		private void Button54_Click ( object sender , EventArgs e ) => KategoriYonetimTemizle();
		private void Button55_Click ( object sender , EventArgs e ) => MarkaYonetimEkle();
		private void Button56_Click ( object sender , EventArgs e ) => MarkaYonetimSil();
		private void Button57_Click ( object sender , EventArgs e ) => MarkaYonetimGuncelle();
		private void Button58_Click ( object sender , EventArgs e ) => MarkaYonetimTemizle();

		private void DgvKategoriYonetim_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0) return;

			DataGridViewRow satir = dgvKategoriYonetim.Rows[e.RowIndex];
			txtKategoriIdYonetim.Text=satir.Cells["KategoriID"].Value?.ToString()??"";
			txtKategoriAdiYonetim.Text=satir.Cells["KategoriAdi"].Value?.ToString()??"";
		}

		private void DgvMarkaYonetim_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(e.RowIndex<0) return;

			DataGridViewRow satir = dgvMarkaYonetim.Rows[e.RowIndex];
			txtMarkaIdYonetim.Text=satir.Cells["MarkaID"].Value?.ToString()??"";
			txtMarkaAdiYonetim.Text=satir.Cells["MarkaAdi"].Value?.ToString()??"";

			object kategoriIdObj = null;
			string kategoriAdi = null;

			if(dgvMarkaYonetim.Columns.Contains("KategoriID"))
				kategoriIdObj=satir.Cells["KategoriID"].Value;

			if(dgvMarkaYonetim.Columns.Contains("KategoriAdi"))
				kategoriAdi=satir.Cells["KategoriAdi"].Value?.ToString();

			if(kategoriIdObj!=null && kategoriIdObj!=DBNull.Value)
			{
				int kategoriId;
				if(int.TryParse(kategoriIdObj.ToString() , out kategoriId))
					comboBox14.SelectedValue=kategoriId;
				else
					comboBox14.SelectedValue=kategoriIdObj;

				if(comboBox14.SelectedIndex<0 && !string.IsNullOrWhiteSpace(kategoriAdi))
					comboBox14.SelectedIndex=comboBox14.FindStringExact(kategoriAdi);
			}
			else if(!string.IsNullOrWhiteSpace(kategoriAdi))
			{
				comboBox14.SelectedIndex=comboBox14.FindStringExact(kategoriAdi);
			}
			else
			{
				comboBox14.SelectedIndex=-1;
			}
		}

		private void KategoriYonetimListele ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					// conn.Open(); -> Yazmasan da Fill metodu bunu senin için yapar.
					OleDbDataAdapter da = new OleDbDataAdapter("SELECT KategoriID, KategoriAdi FROM Kategoriler ORDER BY KategoriID ASC" , conn);
					DataTable dt = new DataTable();
					da.Fill(dt); // Bağlantıyı açar, veriyi çeker, bağlantıyı kapatır.

					dgvKategoriYonetim.DataSource=dt;
					TumDataGridBasliklariniUygula();
					AnaSayfaGridleriniYenile();
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Listeleme hatası: "+ex.Message);
			}
		}

		private void MarkaYonetimListele ()
		{
			// Bağlantı cümlesinde ACE.OLEDB.12.0 olduğundan EMİN OL!
			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				try
				{
					// DataAdapter bağlantıyı kendi açıp kapatabilir, 
					// ama manuel açmak da hata değildir.
					string sorgu = @"SELECT M.MarkaID, M.MarkaAdi, K.KategoriID, K.KategoriAdi
                             FROM (Markalar AS M
                             LEFT JOIN MarkaKategori AS MK ON M.MarkaID = MK.MarkaID)
                             LEFT JOIN Kategoriler AS K ON MK.KategoriID = K.KategoriID
                             ORDER BY M.MarkaID ASC";
					OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn);
					DataTable dt = new DataTable();
					da.Fill(dt);

					dgvMarkaYonetim.DataSource=dt;
						if(dgvMarkaYonetim.Columns.Contains("KategoriID"))
							dgvMarkaYonetim.Columns["KategoriID"].Visible=false;
					TumDataGridBasliklariniUygula();
				}
				catch(Exception ex)
				{
					// Hata gelirse tam olarak ne olduğunu görelim
					MessageBox.Show("Hata Detayı: "+ex.Message);
				}
			}
		}

		private void KategoriYonetimEkle ()
		{
			if(string.IsNullOrWhiteSpace(txtKategoriAdiYonetim.Text))
			{
				MessageBox.Show("Kategori adini girin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			if(YonetimKaydiVarMi("Kategoriler" , "KategoriAdi" , txtKategoriAdiYonetim.Text , "KategoriID"))
			{
				MessageBox.Show("Bu kategori zaten kayitli!" , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Error);
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				OleDbCommand cmd = new OleDbCommand("INSERT INTO Kategoriler (KategoriAdi) VALUES (?)" , conn);
				cmd.Parameters.AddWithValue("?" , txtKategoriAdiYonetim.Text.Trim());
				cmd.ExecuteNonQuery();
			}

			KategoriYonetimTemizle();
			KategoriYonetimListele();
			UrunYonetimComboYenile();
		}

		private void KategoriYonetimSil ()
		{
			if(string.IsNullOrWhiteSpace(txtKategoriIdYonetim.Text))
			{
				MessageBox.Show("Lutfen silinecek kategoriyi secin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				OleDbCommand cmd = new OleDbCommand("DELETE FROM Kategoriler WHERE KategoriID=?" , conn);
				cmd.Parameters.AddWithValue("?" , Convert.ToInt32(txtKategoriIdYonetim.Text));
				cmd.ExecuteNonQuery();
			}

			KategoriYonetimTemizle();
			KategoriYonetimListele();
			UrunYonetimComboYenile();
		}

		private void KategoriYonetimGuncelle ()
		{
			if(string.IsNullOrWhiteSpace(txtKategoriIdYonetim.Text)||string.IsNullOrWhiteSpace(txtKategoriAdiYonetim.Text))
			{
				MessageBox.Show("Guncellemek icin kategori secin ve adi doldurun!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			if(YonetimKaydiVarMi("Kategoriler" , "KategoriAdi" , txtKategoriAdiYonetim.Text , "KategoriID" , txtKategoriIdYonetim.Text))
			{
				MessageBox.Show("Bu kategori zaten kayitli!" , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Error);
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				OleDbCommand cmd = new OleDbCommand("UPDATE Kategoriler SET KategoriAdi=? WHERE KategoriID=?" , conn);
				cmd.Parameters.AddWithValue("?" , txtKategoriAdiYonetim.Text.Trim());
				cmd.Parameters.AddWithValue("?" , Convert.ToInt32(txtKategoriIdYonetim.Text));
				cmd.ExecuteNonQuery();
			}

			KategoriYonetimListele();
			UrunYonetimComboYenile();
		}

		private void KategoriYonetimTemizle ()
		{
			txtKategoriIdYonetim.Clear();
			txtKategoriAdiYonetim.Clear();
			dgvKategoriYonetim.ClearSelection();
		}
		private void MarkaYonetimEkle ()
		{
			if(string.IsNullOrWhiteSpace(txtMarkaAdiYonetim.Text))
			{
				MessageBox.Show("Marka adini girin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			if(YonetimKaydiVarMi("Markalar" , "MarkaAdi" , txtMarkaAdiYonetim.Text , "MarkaID"))
			{
				MessageBox.Show("Bu marka zaten kayitli!" , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Error);
				return;
			}

			if(comboBox14.SelectedValue==null || comboBox14.SelectedValue==DBNull.Value)
			{
				MessageBox.Show("Lutfen kategori secin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}
			int markaId = 0;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("INSERT INTO Markalar (MarkaAdi) VALUES (?)" , conn))
				{
					cmd.Parameters.AddWithValue("?" , txtMarkaAdiYonetim.Text.Trim());
					cmd.ExecuteNonQuery();
					cmd.CommandText="SELECT @@IDENTITY";
					markaId=Convert.ToInt32(cmd.ExecuteScalar());
				}
			}

			if(markaId>0&&comboBox14.SelectedValue!=null&&comboBox14.SelectedValue!=DBNull.Value)
			{
				int kategoriId = Convert.ToInt32(comboBox14.SelectedValue);
				MarkaKategoriBagla(markaId , kategoriId);
			}

			MarkaYonetimTemizle();
			MarkaYonetimListele();
			UrunYonetimComboYenile();
		}
		private void MarkaYonetimSil ()
		{
			if(string.IsNullOrWhiteSpace(txtMarkaIdYonetim.Text))
			{
				MessageBox.Show("Lutfen silinecek markayi secin!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				OleDbCommand cmd = new OleDbCommand("DELETE FROM Markalar WHERE MarkaID=?" , conn);
				cmd.Parameters.AddWithValue("?" , Convert.ToInt32(txtMarkaIdYonetim.Text));
				cmd.ExecuteNonQuery();
			}

			MarkaYonetimTemizle();
			MarkaYonetimListele();
			UrunYonetimComboYenile();
		}

				private void MarkaYonetimGuncelle ()
		{
			if(string.IsNullOrWhiteSpace(txtMarkaIdYonetim.Text)||string.IsNullOrWhiteSpace(txtMarkaAdiYonetim.Text))
			{
				MessageBox.Show("Guncellemek icin marka secin ve adi doldurun!" , "Uyari" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
				return;
			}

			if(YonetimKaydiVarMi("Markalar" , "MarkaAdi" , txtMarkaAdiYonetim.Text , "MarkaID" , txtMarkaIdYonetim.Text))
			{
				MessageBox.Show("Bu marka zaten kayitli!" , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Error);
				return;
			}

			int markaId = Convert.ToInt32(txtMarkaIdYonetim.Text);

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				OleDbCommand cmd = new OleDbCommand("UPDATE Markalar SET MarkaAdi=? WHERE MarkaID=?" , conn);
				cmd.Parameters.AddWithValue("?" , txtMarkaAdiYonetim.Text.Trim());
				cmd.Parameters.AddWithValue("?" , markaId);
				cmd.ExecuteNonQuery();

				if(comboBox14.SelectedValue!=null && comboBox14.SelectedValue!=DBNull.Value)
				{
					int kategoriId = Convert.ToInt32(comboBox14.SelectedValue);

					// Marka-Kategori ilişkisini güncelle
					OleDbCommand sil = new OleDbCommand("DELETE FROM MarkaKategori WHERE MarkaID=?" , conn);
					sil.Parameters.AddWithValue("?" , markaId);
					sil.ExecuteNonQuery();

					OleDbCommand ekle = new OleDbCommand("INSERT INTO MarkaKategori (MarkaID, KategoriID) VALUES (?, ?)" , conn);
					ekle.Parameters.AddWithValue("?" , markaId);
					ekle.Parameters.AddWithValue("?" , kategoriId);
					ekle.ExecuteNonQuery();
				}
			}

			MarkaYonetimListele();
			UrunYonetimComboYenile();
		}
private void MarkaYonetimTemizle ()
		{
			txtMarkaIdYonetim.Clear();
			txtMarkaAdiYonetim.Clear();
			dgvMarkaYonetim.ClearSelection();
comboBox14.SelectedIndex=-1;
		}

		private void UrunYonetimComboYenile ()
		{
			DoldurComboBox(comboBox5 , "SELECT KategoriID, KategoriAdi FROM Kategoriler" , "KategoriAdi" , "KategoriID");
			DoldurComboBox(KategoriSec , "SELECT KategoriID, KategoriAdi FROM Kategoriler" , "KategoriAdi" , "KategoriID");
			DoldurComboBox(comboBox14 , "SELECT KategoriID, KategoriAdi FROM Kategoriler" , "KategoriAdi" , "KategoriID");
			DoldurComboBox(comboBox2 , "SELECT MarkaID, MarkaAdi FROM Markalar" , "MarkaAdi" , "MarkaID");
			DoldurComboBox(comboBox6 , "SELECT MarkaID, MarkaAdi FROM Markalar" , "MarkaAdi" , "MarkaID");
			GunlukSatisUrunListesiniYenile();
		}
		private void CariFiyatKaydet_Click ( object sender , EventArgs e )
		{
			try
			{
				CariDurumKaydet();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Hata oluştu: "+ex.Message);
			}
		}
}
}















