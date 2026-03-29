using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace TEKNİK_SERVİS
{
	public partial class Form1
	{
		private sealed class TcmbKurKaydi
		{
			public string Kod;
			public string Adi;
			public int Birim;
			public decimal? DovizAlis;
			public decimal? DovizSatis;
			public decimal? EfektifAlis;
			public decimal? EfektifSatis;
			public string CaprazKur;
		}

		private sealed class TcmbKurSonucu
		{
			public DateTime Tarih;
			public string BultenNo;
			public string KaynakUrl;
			public List<TcmbKurKaydi> Kurlar = new List<TcmbKurKaydi>();
		}

		private sealed class KurCeviriciDovizSecenegi
		{
			public string Kod;
			public string Adi;
			public int Birim;
			public TcmbKurKaydi KurKaydi;

			public override string ToString ()
			{
				return string.IsNullOrWhiteSpace(Adi) ? Kod : Kod+" - "+Adi;
			}
		}

		private sealed class KurCeviriciKurTuruSecenegi
		{
			public string Anahtar;
			public string Baslik;

			public override string ToString ()
			{
				return Baslik;
			}
		}

		private TabPage _kurlarTabPage;
		private TabControl _kurlarIcerikTabControl;
		private DataGridView _kurlarGrid;
		private DateTimePicker _kurlarTarihPicker;
		private Button _kurlarYenileButonu;
		private Label _kurlarDurumLabel;
		private Label _kurlarBultenLabel;
		private Label _kurlarKaynakLabel;
		private Label _kurlarSonGuncellemeLabel;
		private ComboBox _kurCeviriciDovizComboBox;
		private ComboBox _kurCeviriciKurTuruComboBox;
		private TextBox _kurCeviriciTlTextBox;
		private TextBox _kurCeviriciSonucTextBox;
		private Label _kurCeviriciTarihDegerLabel;
		private Label _kurCeviriciBirimBilgiLabel;
		private Label _kurCeviriciDurumLabel;
		private Label _kurCeviriciSeciliKurLabel;
		private Label _kurCeviriciSeciliAciklamaLabel;
		private Label _kurCeviriciAlisDegerLabel;
		private Label _kurCeviriciSatisDegerLabel;
		private Label _kurCeviriciEfektifAlisDegerLabel;
		private Label _kurCeviriciEfektifSatisDegerLabel;
		private readonly Dictionary<string, Label> _kurlarOzetAlisEtiketleri = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, Label> _kurlarOzetSatisEtiketleri = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
		private readonly HttpClient _tcmbHttpClient = new HttpClient
		{
			Timeout=TimeSpan.FromSeconds(15)
		};
		private bool _kurlarYukleniyor;
		private DateTime? _kurlarYuklenenSecimTarihi;
		private TcmbKurSonucu _kurlarSonYuklenenSonuc;

		private void KurKurlarSekmesi ()
		{
			if(tabControl1==null)
				return;

			if(_kurlarTabPage==null)
			{
				_kurlarTabPage=tabControl1.TabPages
					.Cast<TabPage>()
					.FirstOrDefault(s => string.Equals(s.Text , "Kurlar" , StringComparison.OrdinalIgnoreCase));

				if(_kurlarTabPage==null)
				{
					_kurlarTabPage=new TabPage("Kurlar");
					int isBilgisiIndex = tabPage25!=null ? tabControl1.TabPages.IndexOf(tabPage25) : -1;
					int eklenecekIndex = isBilgisiIndex>=0 ? isBilgisiIndex+1 : tabControl1.TabPages.Count;
					tabControl1.TabPages.Insert(Math.Min(eklenecekIndex , tabControl1.TabPages.Count) , _kurlarTabPage);
				}
			}

			_kurlarTabPage.SuspendLayout();
			_kurlarTabPage.Padding=new Padding(12);
			_kurlarTabPage.UseVisualStyleBackColor=false;
			_kurlarTabPage.BackColor=Color.FromArgb(245 , 247 , 250);
			_kurlarTabPage.Controls.Clear();
			_kurlarOzetAlisEtiketleri.Clear();
			_kurlarOzetSatisEtiketleri.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=Color.FromArgb(245 , 247 , 250)
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 190F));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

			TableLayoutPanel ustLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=3,
				RowCount=1,
				Margin=Padding.Empty
			};
			ustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 330F));
			ustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			ustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 360F));

			GroupBox bilgiKutusu = new GroupBox
			{
				Text="TCMB Kurlari",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42)
			};

			Label baslikLabel = new Label
			{
				AutoSize=false,
				Text="Merkez Bankasi kurlari",
				Font=new Font("Segoe UI" , 16F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Location=new Point(18 , 28),
				Size=new Size(280 , 32)
			};

			Label aciklamaLabel = new Label
			{
				AutoSize=false,
				Text="Secili tarihe ait doviz alim-satis bilgileri TCMB resmi XML yayimindan alinir.",
				Font=new Font("Segoe UI" , 9.5F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				Location=new Point(18 , 66),
				Size=new Size(285 , 58)
			};

			bilgiKutusu.Controls.Add(aciklamaLabel);
			bilgiKutusu.Controls.Add(baslikLabel);

			TableLayoutPanel kartLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=4,
				RowCount=1,
				Padding=new Padding(12 , 10 , 12 , 0),
				Margin=Padding.Empty
			};
			for(int i = 0 ; i<4 ; i++)
				kartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));

			kartLayout.Controls.Add(KurOzetKartiOlustur("USD" , "USD" , "Amerikan Dolari" , Color.FromArgb(15 , 118 , 110)) , 0 , 0);
			kartLayout.Controls.Add(KurOzetKartiOlustur("EUR" , "EUR" , "Euro" , Color.FromArgb(37 , 99 , 235)) , 1 , 0);
			kartLayout.Controls.Add(KurOzetKartiOlustur("GBP" , "GBP" , "Sterlin" , Color.FromArgb(234 , 88 , 12)) , 2 , 0);
			kartLayout.Controls.Add(KurOzetKartiOlustur("CHF" , "CHF" , "Isvicre Frangi" , Color.FromArgb(124 , 58 , 237)) , 3 , 0);

			GroupBox yonetimKutusu = new GroupBox
			{
				Text="Sorgu",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42)
			};

			Label tarihLabel = new Label
			{
				AutoSize=true,
				Text="Tarih",
				Font=new Font("Segoe UI" , 9.2F , FontStyle.Bold),
				Location=new Point(18 , 34)
			};
			_kurlarTarihPicker=new DateTimePicker
			{
				Format=DateTimePickerFormat.Custom,
				CustomFormat="dd.MM.yyyy",
				Font=new Font("Microsoft Sans Serif" , 10F , FontStyle.Regular),
				Location=new Point(18 , 58),
				Size=new Size(204 , 26),
				MaxDate=DateTime.Today,
				Value=DateTime.Today
			};
			_kurlarTarihPicker.ValueChanged-=KurlarTarihPicker_ValueChanged;
			_kurlarTarihPicker.ValueChanged+=KurlarTarihPicker_ValueChanged;

			_kurlarYenileButonu=new Button
			{
				Text="Kurlari Yenile",
				Font=new Font("Microsoft Sans Serif" , 9F , FontStyle.Bold),
				Location=new Point(18 , 96),
				Size=new Size(204 , 40),
				UseVisualStyleBackColor=true
			};
			MetinButonIkonunuBagla(_kurlarYenileButonu , "Renew.png");
			_kurlarYenileButonu.Click-=KurlarYenileButonu_Click;
			_kurlarYenileButonu.Click+=KurlarYenileButonu_Click;

			_kurlarDurumLabel=KurlarBilgiEtiketiOlustur("Durum: Hazir");
			_kurlarDurumLabel.Location=new Point(18 , 156);
			_kurlarDurumLabel.Size=new Size(314 , 20);

			_kurlarBultenLabel=KurlarBilgiEtiketiOlustur("Bulten No: -");
			_kurlarBultenLabel.Location=new Point(18 , 182);

			_kurlarSonGuncellemeLabel=KurlarBilgiEtiketiOlustur("Veri Tarihi: -");
			_kurlarSonGuncellemeLabel.Location=new Point(18 , 208);
			_kurlarSonGuncellemeLabel.Size=new Size(314 , 20);

			_kurlarKaynakLabel=KurlarBilgiEtiketiOlustur("Kaynak: TCMB");
			_kurlarKaynakLabel.Location=new Point(18 , 234);
			_kurlarKaynakLabel.Size=new Size(314 , 38);

			yonetimKutusu.Controls.Add(tarihLabel);
			yonetimKutusu.Controls.Add(_kurlarTarihPicker);
			yonetimKutusu.Controls.Add(_kurlarYenileButonu);
			yonetimKutusu.Controls.Add(_kurlarDurumLabel);
			yonetimKutusu.Controls.Add(_kurlarBultenLabel);
			yonetimKutusu.Controls.Add(_kurlarSonGuncellemeLabel);
			yonetimKutusu.Controls.Add(_kurlarKaynakLabel);

			GroupBox listeKutusu = new GroupBox
			{
				Text="TCMB Kur Listesi",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(12)
			};

			_kurlarGrid=new DataGridView
			{
				Dock=DockStyle.Fill,
				AllowUserToAddRows=false,
				AllowUserToDeleteRows=false,
				AllowUserToResizeRows=false,
				MultiSelect=false,
				SelectionMode=DataGridViewSelectionMode.FullRowSelect
			};
			DatagridviewSetting(_kurlarGrid);
			listeKutusu.Controls.Add(_kurlarGrid);

			ustLayout.Controls.Add(bilgiKutusu , 0 , 0);
			ustLayout.Controls.Add(kartLayout , 1 , 0);
			ustLayout.Controls.Add(yonetimKutusu , 2 , 0);

			anaLayout.Controls.Add(ustLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);

			_kurlarIcerikTabControl=new TabControl
			{
				Dock=DockStyle.Fill
			};

			TabPage kurlarListeSekmesi = new TabPage("TCMB Kurlari")
			{
				BackColor=Color.FromArgb(245 , 247 , 250),
				Padding=Padding.Empty
			};
			kurlarListeSekmesi.Controls.Add(anaLayout);

			TabPage kurCeviriciSekmesi = new TabPage("TL Cevirici")
			{
				BackColor=Color.FromArgb(245 , 247 , 250),
				Padding=new Padding(12)
			};
			kurCeviriciSekmesi.Controls.Add(KurCeviriciSekmesiniOlustur());

			_kurlarIcerikTabControl.TabPages.Add(kurlarListeSekmesi);
			_kurlarIcerikTabControl.TabPages.Add(kurCeviriciSekmesi);

			_kurlarTabPage.Controls.Add(_kurlarIcerikTabControl);
			_kurlarTabPage.Enter-=KurlarTabPage_Enter;
			_kurlarTabPage.Enter+=KurlarTabPage_Enter;
			_kurlarTabPage.ResumeLayout(true);
		}

		private Label KurlarBilgiEtiketiOlustur ( string metin )
		{
			return new Label
			{
				AutoSize=false,
				Text=metin,
				Font=new Font("Segoe UI" , 9F , FontStyle.Regular),
				ForeColor=Color.FromArgb(71 , 85 , 105),
				Size=new Size(320 , 20)
			};
		}

		private Panel KurOzetKartiOlustur ( string kod , string baslik , string altMetin , Color arkaPlan )
		{
			Panel kart = new Panel
			{
				Dock=DockStyle.Fill,
				BackColor=arkaPlan,
				Margin=new Padding(6 , 8 , 6 , 8)
			};

			Label baslikLabel = new Label
			{
				AutoSize=false,
				Text=baslik,
				Font=new Font("Segoe UI" , 16F , FontStyle.Bold),
				ForeColor=Color.White,
				Location=new Point(16 , 14),
				Size=new Size(90 , 30)
			};

			Label altLabel = new Label
			{
				AutoSize=false,
				Text=altMetin,
				Font=new Font("Segoe UI" , 8.6F , FontStyle.Regular),
				ForeColor=Color.FromArgb(230 , 255 , 255 , 255),
				Location=new Point(16 , 42),
				Size=new Size(180 , 18)
			};

			Label alisBaslikLabel = new Label
			{
				AutoSize=true,
				Text="Alis",
				Font=new Font("Segoe UI" , 8.6F , FontStyle.Bold),
				ForeColor=Color.FromArgb(220 , 255 , 255 , 255),
				Location=new Point(16 , 78)
			};

			Label alisDegerLabel = new Label
			{
				AutoSize=false,
				Text="-",
				Font=new Font("Segoe UI" , 12.5F , FontStyle.Bold),
				ForeColor=Color.White,
				Location=new Point(16 , 96),
				Size=new Size(120 , 26)
			};

			Label satisBaslikLabel = new Label
			{
				AutoSize=true,
				Text="Satis",
				Font=new Font("Segoe UI" , 8.6F , FontStyle.Bold),
				ForeColor=Color.FromArgb(220 , 255 , 255 , 255),
				Location=new Point(16 , 126)
			};

			Label satisDegerLabel = new Label
			{
				AutoSize=false,
				Text="-",
				Font=new Font("Segoe UI" , 12.5F , FontStyle.Bold),
				ForeColor=Color.White,
				Location=new Point(16 , 144),
				Size=new Size(120 , 26)
			};

			kart.Controls.Add(baslikLabel);
			kart.Controls.Add(altLabel);
			kart.Controls.Add(alisBaslikLabel);
			kart.Controls.Add(alisDegerLabel);
			kart.Controls.Add(satisBaslikLabel);
			kart.Controls.Add(satisDegerLabel);

			_kurlarOzetAlisEtiketleri[kod]=alisDegerLabel;
			_kurlarOzetSatisEtiketleri[kod]=satisDegerLabel;

			return kart;
		}

		private Control KurCeviriciSekmesiniOlustur ()
		{
			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=1,
				BackColor=Color.FromArgb(245 , 247 , 250)
			};
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 460F));
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));

			GroupBox ceviriciKutusu = new GroupBox
			{
				Text="TL Cevirici",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(14)
			};

			TableLayoutPanel girisLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=8,
				BackColor=Color.White
			};
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 148F));
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 54F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 34F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 34F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 54F));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

			Label aciklamaLabel = new Label
			{
				AutoSize=false,
				Dock=DockStyle.Fill,
				Text="Yuklenen TCMB kuruna gore girdiginiz Turk Lirasi tutarini secili dovize cevirir.",
				Font=new Font("Segoe UI" , 9.4F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				Margin=new Padding(0 , 0 , 0 , 10)
			};
			girisLayout.Controls.Add(aciklamaLabel , 0 , 0);
			girisLayout.SetColumnSpan(aciklamaLabel , 2);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("Yuklenen Tarih") , 0 , 1);
			_kurCeviriciTarihDegerLabel=KurCeviriciDegerEtiketiOlustur(9.2F , false , Color.FromArgb(15 , 23 , 42));
			girisLayout.Controls.Add(_kurCeviriciTarihDegerLabel , 1 , 1);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("Doviz") , 0 , 2);
			_kurCeviriciDovizComboBox=new ComboBox
			{
				Dock=DockStyle.Fill,
				DropDownStyle=ComboBoxStyle.DropDownList,
				Font=new Font("Segoe UI" , 10F , FontStyle.Regular),
				Enabled=false
			};
			_kurCeviriciDovizComboBox.SelectedIndexChanged-=KurCeviriciAlanlariDegisti;
			_kurCeviriciDovizComboBox.SelectedIndexChanged+=KurCeviriciAlanlariDegisti;
			girisLayout.Controls.Add(_kurCeviriciDovizComboBox , 1 , 2);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("Kur Turu") , 0 , 3);
			_kurCeviriciKurTuruComboBox=new ComboBox
			{
				Dock=DockStyle.Fill,
				DropDownStyle=ComboBoxStyle.DropDownList,
				Font=new Font("Segoe UI" , 10F , FontStyle.Regular),
				Enabled=false
			};
			_kurCeviriciKurTuruComboBox.SelectedIndexChanged-=KurCeviriciAlanlariDegisti;
			_kurCeviriciKurTuruComboBox.SelectedIndexChanged+=KurCeviriciAlanlariDegisti;
			KurCeviriciKurTurleriniDoldur();
			girisLayout.Controls.Add(_kurCeviriciKurTuruComboBox , 1 , 3);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("TL Tutari") , 0 , 4);
			_kurCeviriciTlTextBox=new TextBox
			{
				Dock=DockStyle.Fill,
				Font=new Font("Segoe UI" , 10F , FontStyle.Regular),
				Enabled=false
			};
			_kurCeviriciTlTextBox.TextChanged-=KurCeviriciAlanlariDegisti;
			_kurCeviriciTlTextBox.TextChanged+=KurCeviriciAlanlariDegisti;
			_kurCeviriciTlTextBox.KeyPress-=SepetSayisal_KeyPress;
			_kurCeviriciTlTextBox.KeyPress+=SepetSayisal_KeyPress;
			girisLayout.Controls.Add(_kurCeviriciTlTextBox , 1 , 4);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("Kur Bilgisi") , 0 , 5);
			_kurCeviriciBirimBilgiLabel=KurCeviriciDegerEtiketiOlustur(9.2F , true , Color.FromArgb(15 , 118 , 110));
			girisLayout.Controls.Add(_kurCeviriciBirimBilgiLabel , 1 , 5);

			girisLayout.Controls.Add(KurCeviriciAlanEtiketiOlustur("Doviz Karsiligi") , 0 , 6);
			_kurCeviriciSonucTextBox=new TextBox
			{
				Dock=DockStyle.Fill,
				ReadOnly=true,
				TabStop=false,
				BackColor=Color.FromArgb(248 , 250 , 252),
				Font=new Font("Segoe UI Semibold" , 14F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				TextAlign=HorizontalAlignment.Left
			};
			girisLayout.Controls.Add(_kurCeviriciSonucTextBox , 1 , 6);

			_kurCeviriciDurumLabel=new Label
			{
				AutoSize=false,
				Dock=DockStyle.Top,
				Text="Kurlari yukledikten sonra TL tutarini girerek ceviri yapabilirsiniz.",
				Font=new Font("Segoe UI" , 9F , FontStyle.Regular),
				ForeColor=Color.FromArgb(71 , 85 , 105),
				Margin=new Padding(0 , 10 , 0 , 0)
			};
			girisLayout.Controls.Add(_kurCeviriciDurumLabel , 0 , 7);
			girisLayout.SetColumnSpan(_kurCeviriciDurumLabel , 2);

			ceviriciKutusu.Controls.Add(girisLayout);

			GroupBox ozetKutusu = new GroupBox
			{
				Text="Secili Kur Ozeti",
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Padding=new Padding(14)
			};

			TableLayoutPanel ozetLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=7,
				BackColor=Color.White
			};
			ozetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 170F));
			ozetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			for(int i = 0 ; i<7 ; i++)
				ozetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , i<2 ? 46F : 40F));
			ozetLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

			_kurCeviriciSeciliKurLabel=KurCeviriciDegerEtiketiOlustur(18F , true , Color.FromArgb(37 , 99 , 235));
			_kurCeviriciSeciliKurLabel.Margin=new Padding(0 , 0 , 0 , 0);
			ozetLayout.Controls.Add(_kurCeviriciSeciliKurLabel , 0 , 0);
			ozetLayout.SetColumnSpan(_kurCeviriciSeciliKurLabel , 2);

			_kurCeviriciSeciliAciklamaLabel=KurCeviriciDegerEtiketiOlustur(10F , false , Color.FromArgb(100 , 116 , 139));
			_kurCeviriciSeciliAciklamaLabel.Margin=new Padding(0 , -6 , 0 , 6);
			ozetLayout.Controls.Add(_kurCeviriciSeciliAciklamaLabel , 0 , 1);
			ozetLayout.SetColumnSpan(_kurCeviriciSeciliAciklamaLabel , 2);

			KurCeviriciDetaySatiriEkle(ozetLayout , 2 , "Doviz Alis" , out _kurCeviriciAlisDegerLabel);
			KurCeviriciDetaySatiriEkle(ozetLayout , 3 , "Doviz Satis" , out _kurCeviriciSatisDegerLabel);
			KurCeviriciDetaySatiriEkle(ozetLayout , 4 , "Efektif Alis" , out _kurCeviriciEfektifAlisDegerLabel);
			KurCeviriciDetaySatiriEkle(ozetLayout , 5 , "Efektif Satis" , out _kurCeviriciEfektifSatisDegerLabel);

			Label notLabel = new Label
			{
				AutoSize=false,
				Dock=DockStyle.Fill,
				Text="Hesaplama formulu: TL tutari / secili kur x birim",
				Font=new Font("Segoe UI" , 9F , FontStyle.Italic),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				Margin=new Padding(0 , 8 , 0 , 0)
			};
			ozetLayout.Controls.Add(notLabel , 0 , 6);
			ozetLayout.SetColumnSpan(notLabel , 2);

			ozetKutusu.Controls.Add(ozetLayout);

			anaLayout.Controls.Add(ceviriciKutusu , 0 , 0);
			anaLayout.Controls.Add(ozetKutusu , 1 , 0);

			if(_kurCeviriciKurTuruComboBox!=null&&_kurCeviriciKurTuruComboBox.Items.Count>0&&_kurCeviriciKurTuruComboBox.SelectedIndex<0)
				_kurCeviriciKurTuruComboBox.SelectedIndex=0;
			KurCeviriciKurlariGuncelle(null);
			return anaLayout;
		}

		private Label KurCeviriciAlanEtiketiOlustur ( string metin )
		{
			return new Label
			{
				AutoSize=false,
				Dock=DockStyle.Fill,
				Text=metin,
				TextAlign=ContentAlignment.MiddleLeft,
				Font=new Font("Segoe UI" , 9.2F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Margin=new Padding(0 , 6 , 12 , 6)
			};
		}

		private Label KurCeviriciDegerEtiketiOlustur ( float fontBoyutu , bool kalin , Color yaziRengi )
		{
			return new Label
			{
				AutoSize=false,
				Dock=DockStyle.Fill,
				Text="-",
				TextAlign=ContentAlignment.MiddleLeft,
				Font=new Font("Segoe UI" , fontBoyutu , kalin ? FontStyle.Bold : FontStyle.Regular),
				ForeColor=yaziRengi,
				Margin=new Padding(0 , 6 , 0 , 6)
			};
		}

		private void KurCeviriciDetaySatiriEkle ( TableLayoutPanel layout , int satirIndex , string baslik , out Label degerLabel )
		{
			Label baslikLabel = KurCeviriciAlanEtiketiOlustur(baslik);
			baslikLabel.Font=new Font("Segoe UI" , 9F , FontStyle.Bold);
			layout.Controls.Add(baslikLabel , 0 , satirIndex);

			degerLabel=KurCeviriciDegerEtiketiOlustur(10F , true , Color.FromArgb(15 , 23 , 42));
			layout.Controls.Add(degerLabel , 1 , satirIndex);
		}

		private void KurCeviriciKurTurleriniDoldur ()
		{
			if(_kurCeviriciKurTuruComboBox==null)
				return;

			_kurCeviriciKurTuruComboBox.Items.Clear();
			_kurCeviriciKurTuruComboBox.Items.Add(new KurCeviriciKurTuruSecenegi { Anahtar="DovizSatis" , Baslik="Doviz Satis" });
			_kurCeviriciKurTuruComboBox.Items.Add(new KurCeviriciKurTuruSecenegi { Anahtar="DovizAlis" , Baslik="Doviz Alis" });
			_kurCeviriciKurTuruComboBox.Items.Add(new KurCeviriciKurTuruSecenegi { Anahtar="EfektifSatis" , Baslik="Efektif Satis" });
			_kurCeviriciKurTuruComboBox.Items.Add(new KurCeviriciKurTuruSecenegi { Anahtar="EfektifAlis" , Baslik="Efektif Alis" });
		}

		private void KurCeviriciAlanlariDegisti ( object sender , EventArgs e )
		{
			KurCeviriciHesaplamayiGuncelle();
		}

		private void KurCeviriciKurlariGuncelle ( TcmbKurSonucu sonuc )
		{
			_kurlarSonYuklenenSonuc=sonuc;
			if(_kurCeviriciDovizComboBox==null)
				return;

			string seciliKod = ( _kurCeviriciDovizComboBox.SelectedItem as KurCeviriciDovizSecenegi )?.Kod;

			_kurCeviriciDovizComboBox.BeginUpdate();
			_kurCeviriciDovizComboBox.Items.Clear();

			if(sonuc!=null)
			{
				foreach(TcmbKurKaydi kayit in sonuc.Kurlar.Where(KurCeviriciKullanilabilirKurVarMi))
				{
					_kurCeviriciDovizComboBox.Items.Add(new KurCeviriciDovizSecenegi
					{
						Kod=kayit.Kod,
						Adi=string.IsNullOrWhiteSpace(kayit.Adi) ? kayit.Kod : kayit.Adi,
						Birim=Math.Max(1 , kayit.Birim),
						KurKaydi=kayit
					});
				}
			}

			_kurCeviriciDovizComboBox.EndUpdate();

			bool veriHazir = _kurCeviriciDovizComboBox.Items.Count>0;
			_kurCeviriciDovizComboBox.Enabled=veriHazir;
			if(_kurCeviriciKurTuruComboBox!=null)
				_kurCeviriciKurTuruComboBox.Enabled=veriHazir;
			if(_kurCeviriciTlTextBox!=null)
				_kurCeviriciTlTextBox.Enabled=veriHazir;

			if(veriHazir)
			{
				int seciliIndex = 0;
				for(int i = 0 ; i<_kurCeviriciDovizComboBox.Items.Count ; i++)
				{
					KurCeviriciDovizSecenegi secenek = _kurCeviriciDovizComboBox.Items[i] as KurCeviriciDovizSecenegi;
					if(secenek==null)
						continue;

					if(!string.IsNullOrWhiteSpace(seciliKod)&&string.Equals(secenek.Kod , seciliKod , StringComparison.OrdinalIgnoreCase))
					{
						seciliIndex=i;
						break;
					}

					if(string.Equals(secenek.Kod , "USD" , StringComparison.OrdinalIgnoreCase))
						seciliIndex=i;
				}

				_kurCeviriciDovizComboBox.SelectedIndex=seciliIndex;
			}
			else
			{
				_kurCeviriciDovizComboBox.SelectedIndex=-1;
			}

			KurCeviriciHesaplamayiGuncelle();
		}

		private bool KurCeviriciKullanilabilirKurVarMi ( TcmbKurKaydi kayit )
		{
			return kayit!=null&&(
				KurCeviricideKullanilacakDegerGetir(kayit , "DovizSatis").HasValue||
				KurCeviricideKullanilacakDegerGetir(kayit , "DovizAlis").HasValue||
				KurCeviricideKullanilacakDegerGetir(kayit , "EfektifSatis").HasValue||
				KurCeviricideKullanilacakDegerGetir(kayit , "EfektifAlis").HasValue);
		}

		private decimal? KurCeviricideKullanilacakDegerGetir ( TcmbKurKaydi kayit , string anahtar )
		{
			if(kayit==null)
				return null;

			switch(( anahtar??string.Empty ).Trim())
			{
				case "DovizAlis":
					return kayit.DovizAlis;
				case "EfektifAlis":
					return kayit.EfektifAlis;
				case "EfektifSatis":
					return kayit.EfektifSatis;
				default:
					return kayit.DovizSatis;
			}
		}

		private void KurCeviriciHesaplamayiGuncelle ()
		{
			if(_kurCeviriciTarihDegerLabel==null
				||_kurCeviriciDovizComboBox==null
				||_kurCeviriciKurTuruComboBox==null
				||_kurCeviriciSeciliKurLabel==null
				||_kurCeviriciSeciliAciklamaLabel==null
				||_kurCeviriciAlisDegerLabel==null
				||_kurCeviriciSatisDegerLabel==null
				||_kurCeviriciEfektifAlisDegerLabel==null
				||_kurCeviriciEfektifSatisDegerLabel==null
				||_kurCeviriciBirimBilgiLabel==null
				||_kurCeviriciSonucTextBox==null
				||_kurCeviriciDurumLabel==null)
				return;

			_kurCeviriciTarihDegerLabel.Text=_kurlarSonYuklenenSonuc==null
				? "-"
				: _kurlarSonYuklenenSonuc.Tarih.ToString("dd.MM.yyyy" , _yazdirmaKulturu);

			KurCeviriciDovizSecenegi seciliDoviz = _kurCeviriciDovizComboBox?.SelectedItem as KurCeviriciDovizSecenegi;
			KurCeviriciKurTuruSecenegi seciliKurTuru = _kurCeviriciKurTuruComboBox?.SelectedItem as KurCeviriciKurTuruSecenegi;
			TcmbKurKaydi kayit = seciliDoviz?.KurKaydi;

			_kurCeviriciSeciliKurLabel.Text=seciliDoviz?.Kod??"-";
			_kurCeviriciSeciliAciklamaLabel.Text=seciliDoviz==null
				? "Kur bilgisi bekleniyor"
				: ( string.IsNullOrWhiteSpace(seciliDoviz.Adi) ? seciliDoviz.Kod : seciliDoviz.Adi );
			_kurCeviriciAlisDegerLabel.Text=TcmbKurDegerMetniGetir(kayit?.DovizAlis);
			_kurCeviriciSatisDegerLabel.Text=TcmbKurDegerMetniGetir(kayit?.DovizSatis);
			_kurCeviriciEfektifAlisDegerLabel.Text=TcmbKurDegerMetniGetir(kayit?.EfektifAlis);
			_kurCeviriciEfektifSatisDegerLabel.Text=TcmbKurDegerMetniGetir(kayit?.EfektifSatis);

			if(seciliDoviz==null||seciliKurTuru==null||kayit==null)
			{
				if(_kurCeviriciBirimBilgiLabel!=null)
					_kurCeviriciBirimBilgiLabel.Text="-";
				if(_kurCeviriciSonucTextBox!=null)
					_kurCeviriciSonucTextBox.Text=string.Empty;
				if(_kurCeviriciDurumLabel!=null)
					_kurCeviriciDurumLabel.Text="Kurlari yukledikten sonra bir doviz secerek TL tutarini cevirin.";
				return;
			}

			decimal? seciliKurDegeri = KurCeviricideKullanilacakDegerGetir(kayit , seciliKurTuru.Anahtar);
			if(!seciliKurDegeri.HasValue||seciliKurDegeri.Value<=0m)
			{
				if(_kurCeviriciBirimBilgiLabel!=null)
					_kurCeviriciBirimBilgiLabel.Text="Secili kur turu icin deger bulunamadi";
				if(_kurCeviriciSonucTextBox!=null)
					_kurCeviriciSonucTextBox.Text=string.Empty;
				if(_kurCeviriciDurumLabel!=null)
					_kurCeviriciDurumLabel.Text="Farkli bir kur turu secin veya yeni tarih icin kurlari yenileyin.";
				return;
			}

			if(_kurCeviriciBirimBilgiLabel!=null)
			{
				_kurCeviriciBirimBilgiLabel.Text=
					Math.Max(1 , seciliDoviz.Birim).ToString("N0" , _yazdirmaKulturu)+" "+
					seciliDoviz.Kod+" = "+
					seciliKurDegeri.Value.ToString("N4" , _yazdirmaKulturu)+" TL";
			}

			decimal tlTutari = SepetDecimalParse(_kurCeviriciTlTextBox?.Text);
			if(tlTutari<=0m)
			{
				if(_kurCeviriciSonucTextBox!=null)
					_kurCeviriciSonucTextBox.Text=string.Empty;
				if(_kurCeviriciDurumLabel!=null)
					_kurCeviriciDurumLabel.Text="Ceviri icin TL tutari girin.";
				return;
			}

			decimal dovizTutari = tlTutari*Math.Max(1 , seciliDoviz.Birim)/seciliKurDegeri.Value;
			if(_kurCeviriciSonucTextBox!=null)
				_kurCeviriciSonucTextBox.Text=dovizTutari.ToString("N4" , _yazdirmaKulturu)+" "+seciliDoviz.Kod;
			if(_kurCeviriciDurumLabel!=null)
				_kurCeviriciDurumLabel.Text=tlTutari.ToString("N2" , _yazdirmaKulturu)+" TL tutari secili kura gore hesaplandi.";
		}

		private async void KurlarTabPage_Enter ( object sender , EventArgs e )
		{
			if(_kurlarGrid==null||_kurlarGrid.Rows.Count>0&&_kurlarYuklenenSecimTarihi.HasValue&&_kurlarYuklenenSecimTarihi.Value.Date==_kurlarTarihPicker.Value.Date)
				return;

			await TcmbKurlariniYukleAsync(false);
		}

		private async void KurlarYenileButonu_Click ( object sender , EventArgs e )
		{
			await TcmbKurlariniYukleAsync(true);
		}

		private async void KurlarTarihPicker_ValueChanged ( object sender , EventArgs e )
		{
			await TcmbKurlariniYukleAsync(true);
		}

		private async Task TcmbKurlariniYukleAsync ( bool kullaniciIstegi )
		{
			if(_kurlarYukleniyor||_kurlarGrid==null||_kurlarTarihPicker==null)
				return;

			DateTime seciliTarih = _kurlarTarihPicker.Value.Date;
			if(!kullaniciIstegi&&_kurlarYuklenenSecimTarihi.HasValue&&_kurlarYuklenenSecimTarihi.Value.Date==seciliTarih&&_kurlarGrid.Rows.Count>0)
				return;

			_kurlarYukleniyor=true;
			KurlarYuklemeDurumunuAyarla(true , "Durum: TCMB kurlari yukleniyor...");

			try
			{
				TcmbKurSonucu sonuc = await TcmbKurSonucunuGetirAsync(seciliTarih);
				KurlarGridiniDoldur(sonuc);
				KurlarOzetleriniGuncelle(sonuc);
				KurCeviriciKurlariGuncelle(sonuc);
				_kurlarYuklenenSecimTarihi=seciliTarih;
				_kurlarDurumLabel.Text="Durum: "+sonuc.Kurlar.Count.ToString("N0" , _yazdirmaKulturu)+" kur kaydi yuklendi";
				_kurlarBultenLabel.Text="Bulten No: "+( string.IsNullOrWhiteSpace(sonuc.BultenNo) ? "-" : sonuc.BultenNo );
				_kurlarSonGuncellemeLabel.Text="Veri Tarihi: "+sonuc.Tarih.ToString("dd.MM.yyyy" , _yazdirmaKulturu);
				_kurlarKaynakLabel.Text="Kaynak: "+sonuc.KaynakUrl;
			}
			catch(Exception ex)
			{
				_kurlarDurumLabel.Text="Durum: Veri alinamadi";
				_kurlarBultenLabel.Text="Bulten No: -";
				_kurlarSonGuncellemeLabel.Text="Veri Tarihi: -";
				_kurlarKaynakLabel.Text="Kaynak: TCMB";
				KurlarOzetleriniTemizle();
				_kurlarGrid.DataSource=null;
				KurCeviriciKurlariGuncelle(null);
				if(kullaniciIstegi||_kurlarYuklenenSecimTarihi==null)
					MessageBox.Show("TCMB kurlari alinamadi: "+ex.Message , "Kurlar" , MessageBoxButtons.OK , MessageBoxIcon.Warning);
			}
			finally
			{
				KurlarYuklemeDurumunuAyarla(false , _kurlarDurumLabel.Text);
				_kurlarYukleniyor=false;
			}
		}

		private void KurlarYuklemeDurumunuAyarla ( bool yukleniyor , string durumMetni )
		{
			if(_kurlarYenileButonu!=null)
				_kurlarYenileButonu.Enabled=!yukleniyor;
			if(_kurlarTarihPicker!=null)
				_kurlarTarihPicker.Enabled=!yukleniyor;
			if(_kurlarDurumLabel!=null&&!string.IsNullOrWhiteSpace(durumMetni))
				_kurlarDurumLabel.Text=durumMetni;
			if(_kurlarTabPage!=null)
				_kurlarTabPage.Cursor=yukleniyor ? Cursors.WaitCursor : Cursors.Default;
		}

		private async Task<TcmbKurSonucu> TcmbKurSonucunuGetirAsync ( DateTime tarih )
		{
			ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;

			string kaynakUrl = TcmbKurKaynakUrliniGetir(tarih);
			using(HttpResponseMessage yanit = await _tcmbHttpClient.GetAsync(kaynakUrl))
			{
				if(yanit.StatusCode==HttpStatusCode.NotFound)
					throw new InvalidOperationException("Secili tarihe ait kur dosyasi bulunamadi.");

				yanit.EnsureSuccessStatusCode();
				string xmlIcerigi = await yanit.Content.ReadAsStringAsync();
				XDocument dokuman = XDocument.Parse(xmlIcerigi);
				XElement kok = dokuman.Root;
				if(kok==null)
					throw new InvalidOperationException("TCMB veri dosyasi bos dondu.");

				TcmbKurSonucu sonuc = new TcmbKurSonucu
				{
					Tarih=TcmbKurTarihiniGetir(kok , tarih),
					BultenNo=(string)kok.Attribute("Bulten_No")??string.Empty,
					KaynakUrl=kaynakUrl
				};

				foreach(XElement kurElemani in kok.Elements("Currency"))
				{
					string kod = ((string)kurElemani.Attribute("CurrencyCode")??(string)kurElemani.Attribute("Kod")??string.Empty).Trim().ToUpperInvariant();
					if(string.IsNullOrWhiteSpace(kod))
						continue;

					TcmbKurKaydi kayit = new TcmbKurKaydi
					{
						Kod=kod,
						Adi=((string)kurElemani.Element("Isim")??(string)kurElemani.Element("CurrencyName")??string.Empty).Trim(),
						Birim=TcmbIntGetir((string)kurElemani.Element("Unit") , 1),
						DovizAlis=TcmbDecimalGetir((string)kurElemani.Element("ForexBuying")),
						DovizSatis=TcmbDecimalGetir((string)kurElemani.Element("ForexSelling")),
						EfektifAlis=TcmbDecimalGetir((string)kurElemani.Element("BanknoteBuying")),
						EfektifSatis=TcmbDecimalGetir((string)kurElemani.Element("BanknoteSelling")),
						CaprazKur=TcmbCaprazKurMetniGetir(kurElemani)
					};
					sonuc.Kurlar.Add(kayit);
				}

				if(sonuc.Kurlar.Count==0)
					throw new InvalidOperationException("TCMB kur listesi okunamadi.");

				return sonuc;
			}
		}

		private string TcmbKurKaynakUrliniGetir ( DateTime tarih )
		{
			DateTime bugun = DateTime.Today;
			if(tarih.Date==bugun)
				return "https://www.tcmb.gov.tr/kurlar/today.xml";

			return "https://www.tcmb.gov.tr/kurlar/"+
				tarih.ToString("yyyyMM" , CultureInfo.InvariantCulture)+"/"+
				tarih.ToString("ddMMyyyy" , CultureInfo.InvariantCulture)+".xml";
		}

		private DateTime TcmbKurTarihiniGetir ( XElement kok , DateTime varsayilanTarih )
		{
			string[] adaylar =
			{
				(string)kok.Attribute("Tarih"),
				(string)kok.Attribute("Date")
			};

			foreach(string aday in adaylar)
			{
				if(string.IsNullOrWhiteSpace(aday))
					continue;

				DateTime tarih;
				if(DateTime.TryParseExact(aday , "dd.MM.yyyy" , _yazdirmaKulturu , DateTimeStyles.None , out tarih)
					||DateTime.TryParseExact(aday , "MM/dd/yyyy" , CultureInfo.InvariantCulture , DateTimeStyles.None , out tarih)
					||DateTime.TryParse(aday , _yazdirmaKulturu , DateTimeStyles.None , out tarih))
					return tarih.Date;
			}

			return varsayilanTarih.Date;
		}

		private int TcmbIntGetir ( string metin , int varsayilanDeger )
		{
			int sonuc;
			return int.TryParse(( metin??string.Empty ).Trim() , NumberStyles.Integer , CultureInfo.InvariantCulture , out sonuc)
				? sonuc
				: varsayilanDeger;
		}

		private decimal? TcmbDecimalGetir ( string metin )
		{
			string temizMetin = ( metin??string.Empty ).Trim();
			if(string.IsNullOrWhiteSpace(temizMetin))
				return null;

			decimal sonuc;
			if(decimal.TryParse(temizMetin , NumberStyles.Any , CultureInfo.InvariantCulture , out sonuc))
				return sonuc;
			if(decimal.TryParse(temizMetin , NumberStyles.Any , _yazdirmaKulturu , out sonuc))
				return sonuc;

			return null;
		}

		private string TcmbCaprazKurMetniGetir ( XElement kurElemani )
		{
			string caprazDiger = ((string)kurElemani.Element("CrossRateOther")??string.Empty).Trim();
			if(!string.IsNullOrWhiteSpace(caprazDiger))
				return caprazDiger;

			string caprazUsd = ((string)kurElemani.Element("CrossRateUSD")??string.Empty).Trim();
			if(!string.IsNullOrWhiteSpace(caprazUsd))
				return caprazUsd;

			return string.Empty;
		}

		private void KurlarGridiniDoldur ( TcmbKurSonucu sonuc )
		{
			if(_kurlarGrid==null)
				return;

			DataTable dt = new DataTable();
			dt.Columns.Add("Kod" , typeof(string));
			dt.Columns.Add("DovizAdi" , typeof(string));
			dt.Columns.Add("Birim" , typeof(int));
			dt.Columns.Add("DovizAlis" , typeof(decimal));
			dt.Columns.Add("DovizSatis" , typeof(decimal));
			dt.Columns.Add("EfektifAlis" , typeof(decimal));
			dt.Columns.Add("EfektifSatis" , typeof(decimal));
			dt.Columns.Add("CaprazKur" , typeof(string));

			foreach(TcmbKurKaydi kayit in sonuc.Kurlar)
			{
				DataRow satir = dt.NewRow();
				satir["Kod"]=kayit.Kod;
				satir["DovizAdi"]=string.IsNullOrWhiteSpace(kayit.Adi) ? kayit.Kod : kayit.Adi;
				satir["Birim"]=kayit.Birim;
				satir["DovizAlis"]=kayit.DovizAlis.HasValue ? (object)kayit.DovizAlis.Value : DBNull.Value;
				satir["DovizSatis"]=kayit.DovizSatis.HasValue ? (object)kayit.DovizSatis.Value : DBNull.Value;
				satir["EfektifAlis"]=kayit.EfektifAlis.HasValue ? (object)kayit.EfektifAlis.Value : DBNull.Value;
				satir["EfektifSatis"]=kayit.EfektifSatis.HasValue ? (object)kayit.EfektifSatis.Value : DBNull.Value;
				satir["CaprazKur"]=kayit.CaprazKur;
				dt.Rows.Add(satir);
			}

			_kurlarGrid.DataSource=dt;
			GridBasliklariniTurkceDuzenle(_kurlarGrid);

			if(_kurlarGrid.Columns.Contains("Kod"))
			{
				_kurlarGrid.Columns["Kod"].HeaderText="KOD";
				_kurlarGrid.Columns["Kod"].FillWeight=65F;
			}
			if(_kurlarGrid.Columns.Contains("DovizAdi"))
			{
				_kurlarGrid.Columns["DovizAdi"].HeaderText="DOVIZ ADI";
				_kurlarGrid.Columns["DovizAdi"].FillWeight=180F;
			}
			if(_kurlarGrid.Columns.Contains("Birim"))
			{
				_kurlarGrid.Columns["Birim"].HeaderText="BIRIM";
				_kurlarGrid.Columns["Birim"].FillWeight=60F;
				_kurlarGrid.Columns["Birim"].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;
			}

			string[] sayisalKolonlar = { "DovizAlis" , "DovizSatis" , "EfektifAlis" , "EfektifSatis" };
			foreach(string kolonAdi in sayisalKolonlar)
			{
				if(!_kurlarGrid.Columns.Contains(kolonAdi))
					continue;

				_kurlarGrid.Columns[kolonAdi].DefaultCellStyle.Format="N4";
				_kurlarGrid.Columns[kolonAdi].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
				_kurlarGrid.Columns[kolonAdi].FillWeight=95F;
			}

			if(_kurlarGrid.Columns.Contains("DovizAlis"))
				_kurlarGrid.Columns["DovizAlis"].HeaderText="DOVIZ ALIS";
			if(_kurlarGrid.Columns.Contains("DovizSatis"))
				_kurlarGrid.Columns["DovizSatis"].HeaderText="DOVIZ SATIS";
			if(_kurlarGrid.Columns.Contains("EfektifAlis"))
				_kurlarGrid.Columns["EfektifAlis"].HeaderText="EFEKTIF ALIS";
			if(_kurlarGrid.Columns.Contains("EfektifSatis"))
				_kurlarGrid.Columns["EfektifSatis"].HeaderText="EFEKTIF SATIS";
			if(_kurlarGrid.Columns.Contains("CaprazKur"))
			{
				_kurlarGrid.Columns["CaprazKur"].HeaderText="CAPRAZ KUR";
				_kurlarGrid.Columns["CaprazKur"].FillWeight=90F;
			}

			_kurlarGrid.ClearSelection();
		}

		private void KurlarOzetleriniGuncelle ( TcmbKurSonucu sonuc )
		{
			KurOzetDegeriniYaz("USD" , sonuc);
			KurOzetDegeriniYaz("EUR" , sonuc);
			KurOzetDegeriniYaz("GBP" , sonuc);
			KurOzetDegeriniYaz("CHF" , sonuc);
		}

		private void KurOzetDegeriniYaz ( string kod , TcmbKurSonucu sonuc )
		{
			TcmbKurKaydi kayit = sonuc?.Kurlar.FirstOrDefault(k => string.Equals(k.Kod , kod , StringComparison.OrdinalIgnoreCase));
			if(_kurlarOzetAlisEtiketleri.ContainsKey(kod))
				_kurlarOzetAlisEtiketleri[kod].Text=TcmbKurDegerMetniGetir(kayit?.DovizAlis);
			if(_kurlarOzetSatisEtiketleri.ContainsKey(kod))
				_kurlarOzetSatisEtiketleri[kod].Text=TcmbKurDegerMetniGetir(kayit?.DovizSatis);
		}

		private void KurlarOzetleriniTemizle ()
		{
			foreach(Label etiket in _kurlarOzetAlisEtiketleri.Values)
				etiket.Text="-";
			foreach(Label etiket in _kurlarOzetSatisEtiketleri.Values)
				etiket.Text="-";
		}

		private string TcmbKurDegerMetniGetir ( decimal? deger )
		{
			return deger.HasValue
				? deger.Value.ToString("N4" , _yazdirmaKulturu)
				: "-";
		}
	}
}
