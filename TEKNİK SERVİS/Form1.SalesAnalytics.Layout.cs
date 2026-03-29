using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	public partial class Form1
	{
		private TabPage _satisRaporTabPage;
		private TabControl _satisRaporAltTabControl;
		private TabPage _gunlukSatisTabPage;
		private TabPage _iadeTabPage;
		private TabPage _gunlukSatisToplamTabPage;
		private TabPage _aylikSatisTabPage;
		private TabPage _aylikFabrikaFaturaTabPage;
		private TabPage _aylikMusteriFaturaTabPage;
		private TabPage _toplamSatisTabPage;
		private TabPage _genelToplamTabPage;
		private DateTimePicker _gunlukSatisTarihPicker;
		private DateTimePicker _iadeTarihPicker;
		private DateTimePicker _gunlukSatisToplamTarihPicker;
		private DateTimePicker _aylikSatisAyPicker;
		private DateTimePicker _aylikFabrikaFaturaAyPicker;
		private DateTimePicker _aylikMusteriFaturaAyPicker;
		private ComboBox _gunlukSatisUrunComboBox;
		private ComboBox _iadeUrunComboBox;
		private TextBox _gunlukSatisBirimTextBox;
		private TextBox _iadeBirimTextBox;
		private TextBox _gunlukSatisMiktarTextBox;
		private TextBox _iadeMiktarTextBox;
		private TextBox _gunlukSatisBirimFiyatTextBox;
		private TextBox _iadeBirimFiyatTextBox;
		private TextBox _gunlukSatisBirimMaliyetTextBox;
		private TextBox _iadeBirimMaliyetTextBox;
		private TextBox _gunlukSatisToplamTextBox;
		private TextBox _iadeToplamTextBox;
		private TextBox _gunlukSatisNotTextBox;
		private TextBox _iadeNotTextBox;
		private TextBox _gunlukSatisAramaKutusu;
		private TextBox _iadeAramaKutusu;
		private TextBox _gunlukSatisToplamAramaKutusu;
		private TextBox _aylikSatisAramaKutusu;
		private TextBox _aylikFabrikaFaturaAramaKutusu;
		private TextBox _aylikMusteriFaturaAramaKutusu;
		private TextBox _toplamSatisAramaKutusu;
		private TextBox _genelToplamAramaKutusu;
		private DataGridView _gunlukSatisGrid;
		private DataGridView _iadeGrid;
		private DataGridView _gunlukSatisToplamGrid;
		private DataGridView _aylikSatisGrid;
		private DataGridView _aylikFabrikaFaturaGrid;
		private DataGridView _aylikMusteriFaturaGrid;
		private DataGridView _toplamSatisGrid;
		private DataGridView _genelToplamGrid;
		private Button _genelToplamYazdirButonu;
		private Button _genelToplamPdfButonu;
		private Button _genelToplamExcelButonu;
		private Label _gunlukSatisCiroLabel;
		private Label _gunlukSatisKarLabel;
		private Label _gunlukSatisKarOraniLabel;
		private Label _gunlukSatisMiktarLabel;
		private Label _iadeToplamLabel;
		private Label _iadeMaliyetLabel;
		private Label _iadeNetEtkiLabel;
		private Label _iadeMiktarLabel;
		private Label _gunlukSatisToplamCiroLabel;
		private Label _gunlukSatisToplamKarLabel;
		private Label _gunlukSatisToplamKarOraniLabel;
		private Label _gunlukSatisToplamMiktarLabel;
		private Label _aylikSatisCiroLabel;
		private Label _aylikSatisKarLabel;
		private Label _aylikSatisKarOraniLabel;
		private Label _aylikSatisMiktarLabel;
		private Label _aylikFabrikaFaturaToplamLabel;
		private Label _aylikFabrikaFaturaSayisiLabel;
		private Label _aylikFabrikaFaturaKalemLabel;
		private Label _aylikFabrikaFaturaOrtalamaLabel;
		private Label _aylikMusteriFaturaToplamLabel;
		private Label _aylikMusteriFaturaSayisiLabel;
		private Label _aylikMusteriFaturaKalemLabel;
		private Label _aylikMusteriFaturaOrtalamaLabel;
		private Label _toplamSatisCiroLabel;
		private Label _toplamSatisKarLabel;
		private Label _toplamSatisKarOraniLabel;
		private Label _toplamSatisMiktarLabel;
		private Label _genelToplamCiroLabel;
		private Label _genelToplamKarLabel;
		private Label _genelToplamToptanciOdemeLabel;
		private Label _genelToplamKalanBorcLabel;

		private void KurGunlukSatisSekmesi ()
		{
			if(_gunlukSatisSekmesiHazir||tabControl1==null)
				return;

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

			_satisRaporAltTabControl.SelectedIndexChanged-=SatisRaporAltTabControl_SelectedIndexChanged;
			_satisRaporAltTabControl.SelectedIndexChanged+=SatisRaporAltTabControl_SelectedIndexChanged;

			GunlukSatisSayfasiniOlustur();
			IadeSayfasiniOlustur();
			GunlukSatisToplamSayfasiniOlustur();
			AylikSatisSayfasiniOlustur();
			AylikFabrikaFaturaSayfasiniOlustur();
			AylikMusteriFaturaSayfasiniOlustur();
			ToplamSatisSayfasiniOlustur();
			GenelToplamSayfasiniOlustur();

			_gunlukSatisSekmesiHazir=true;
			GunlukSatisUrunListesiniYenile();
			IadeUrunListesiniYenile();
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
				"BUGÜNKÜ CİRO" ,
				"BUGÜNKÜ KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _gunlukSatisCiroLabel ,
				out _gunlukSatisKarLabel ,
				out _gunlukSatisKarOraniLabel ,
				out _gunlukSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Günlük Satış Listesi");
			GroupBox girisKutusu = SatisRaporGroupBoxOlustur("Satış Girişi");

			Size gunlukSatisFiltreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(gunlukSatisFiltreBoyutu , aramaKutusuBoyutu);
			Label tarihLabel = SatisRaporFiltreEtiketiOlustur("Tarih");
			_gunlukSatisTarihPicker=SatisRaporTarihSeciciOlustur(false , gunlukSatisFiltreBoyutu);
			_gunlukSatisTarihPicker.ValueChanged-=GunlukSatisTarihPicker_ValueChanged;
			_gunlukSatisTarihPicker.ValueChanged+=GunlukSatisTarihPicker_ValueChanged;
			_gunlukSatisAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_gunlukSatisGrid=SatisRaporGridiOlustur();
			_gunlukSatisGrid.CellClick-=GunlukSatisGrid_CellClick;
			_gunlukSatisGrid.CellClick+=GunlukSatisGrid_CellClick;

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				tarihLabel ,
				_gunlukSatisTarihPicker ,
				null ,
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

			SatisRaporFormSatiriEkle(girisLayout , 0 , "Ürün" , _gunlukSatisUrunComboBox);
			SatisRaporFormSatiriEkle(girisLayout , 1 , "Birim" , _gunlukSatisBirimTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 2 , "Miktar" , _gunlukSatisMiktarTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 3 , "Satış Fiyatı" , _gunlukSatisBirimFiyatTextBox);
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
			Button yenileButonu = SatisRaporButonuOlustur("Yenile");
			yenileButonu.Click+=(sender , e) => GunlukSatisVerileriniYenile();
			SatisRaporButonTemasiniUygula(yenileButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
			Button silButonu = SatisRaporButonuOlustur("Seçiliyi Sil");
			silButonu.Click-=GunlukSatisSilButonu_Click;
			silButonu.Click+=GunlukSatisSilButonu_Click;
			SatisRaporButonTemasiniUygula(silButonu , Color.FromArgb(241 , 245 , 249) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			Button temizleButonu = SatisRaporButonuOlustur("Temizle");
			temizleButonu.Click+=(sender , e) => GunlukSatisFormunuTemizle();
			SatisRaporButonTemasiniUygula(temizleButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));

			butonPaneli.Controls.Add(kaydetButonu , 0 , 0);
			butonPaneli.Controls.Add(yenileButonu , 1 , 0);
			butonPaneli.Controls.Add(temizleButonu , 0 , 1);
			butonPaneli.Controls.Add(silButonu , 1 , 1);
			girisLayout.Controls.Add(butonPaneli , 1 , 7);

			girisKutusu.Controls.Add(girisLayout);

			solLayout.Controls.Add(kartLayout , 0 , 0);
			solLayout.Controls.Add(listeKutusu , 0 , 1);
			anaLayout.Controls.Add(solLayout , 0 , 0);
			anaLayout.Controls.Add(girisKutusu , 1 , 0);
			_gunlukSatisTabPage.Controls.Add(anaLayout);

			SatisRaporAramaKutusuHazirla(_gunlukSatisAramaKutusu , _gunlukSatisGrid);
		}

		private void IadeSayfasiniOlustur ()
		{
			if(_iadeTabPage==null)
				return;

			_iadeTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=1,
				BackColor=_iadeTabPage.BackColor
			};
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 74f));
			anaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 26f));

			TableLayoutPanel solLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_iadeTabPage.BackColor
			};
			solLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			solLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"BUGÜNKÜ İADE" ,
				"İADE MALİYETİ" ,
				"NET ETKİ" ,
				"TOPLAM ADET" ,
				out _iadeToplamLabel ,
				out _iadeMaliyetLabel ,
				out _iadeNetEtkiLabel ,
				out _iadeMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("İade Listesi");
			GroupBox girisKutusu = SatisRaporGroupBoxOlustur("İade Girişi");

			Size filtreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu , aramaKutusuBoyutu);
			Label tarihLabel = SatisRaporFiltreEtiketiOlustur("Tarih");
			_iadeTarihPicker=SatisRaporTarihSeciciOlustur(false , filtreBoyutu);
			_iadeTarihPicker.ValueChanged-=IadeTarihPicker_ValueChanged;
			_iadeTarihPicker.ValueChanged+=IadeTarihPicker_ValueChanged;
			_iadeAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_iadeGrid=SatisRaporGridiOlustur();
			_iadeGrid.CellClick-=IadeGrid_CellClick;
			_iadeGrid.CellClick+=IadeGrid_CellClick;

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				tarihLabel ,
				_iadeTarihPicker ,
				null ,
				_iadeAramaKutusu ,
				filtreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_iadeGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			TableLayoutPanel girisLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=2,
				RowCount=8,
				Padding=new Padding(10 , 12 , 10 , 6),
				BackColor=_iadeTabPage.BackColor
			};
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 118f));
			girisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100f));
			for(int i = 0 ; i<6 ; i++)
				girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 96f));
			girisLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			_iadeUrunComboBox=SatisRaporComboBoxOlustur();
			_iadeUrunComboBox.SelectedIndexChanged-=IadeUrunComboBox_SelectedIndexChanged;
			_iadeUrunComboBox.SelectedIndexChanged+=IadeUrunComboBox_SelectedIndexChanged;
			_iadeUrunComboBox.TextChanged-=IadeUrunComboBox_TextChanged;
			_iadeUrunComboBox.TextChanged+=IadeUrunComboBox_TextChanged;
			_iadeUrunComboBox.Leave-=IadeUrunComboBox_Leave;
			_iadeUrunComboBox.Leave+=IadeUrunComboBox_Leave;

			_iadeBirimTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_iadeMiktarTextBox=SatisRaporMetinKutusuOlustur(false , false);
			_iadeBirimFiyatTextBox=SatisRaporMetinKutusuOlustur(false , false);
			_iadeBirimMaliyetTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_iadeToplamTextBox=SatisRaporMetinKutusuOlustur(true , false);
			_iadeNotTextBox=SatisRaporMetinKutusuOlustur(false , true);

			_iadeMiktarTextBox.Text="1";
			_iadeBirimFiyatTextBox.Text="0,00";
			_iadeBirimMaliyetTextBox.Text="0,00";
			_iadeToplamTextBox.Text="0,00";

			_iadeMiktarTextBox.KeyPress-=SepetSayisal_KeyPress;
			_iadeMiktarTextBox.KeyPress+=SepetSayisal_KeyPress;
			_iadeBirimFiyatTextBox.KeyPress-=SepetSayisal_KeyPress;
			_iadeBirimFiyatTextBox.KeyPress+=SepetSayisal_KeyPress;
			_iadeMiktarTextBox.TextChanged-=IadeHesapAlanlari_TextChanged;
			_iadeMiktarTextBox.TextChanged+=IadeHesapAlanlari_TextChanged;
			_iadeBirimFiyatTextBox.TextChanged-=IadeHesapAlanlari_TextChanged;
			_iadeBirimFiyatTextBox.TextChanged+=IadeHesapAlanlari_TextChanged;

			SatisRaporFormSatiriEkle(girisLayout , 0 , "Ürün" , _iadeUrunComboBox);
			SatisRaporFormSatiriEkle(girisLayout , 1 , "Birim" , _iadeBirimTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 2 , "Miktar" , _iadeMiktarTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 3 , "İade Fiyatı" , _iadeBirimFiyatTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 4 , "Birim Maliyet" , _iadeBirimMaliyetTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 5 , "Toplam" , _iadeToplamTextBox);
			SatisRaporFormSatiriEkle(girisLayout , 6 , "Not" , _iadeNotTextBox);

			TableLayoutPanel butonPaneli = new TableLayoutPanel
			{
				Dock=DockStyle.Top,
				ColumnCount=2,
				RowCount=2,
				Height=92,
				Margin=Padding.Empty,
				Padding=new Padding(0 , 8 , 0 , 0),
				BackColor=_iadeTabPage.BackColor
			};
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50f));
			butonPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 50f));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));
			butonPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute , 42f));

			Button kaydetButonu = SatisRaporButonuOlustur("İade Kaydet");
			kaydetButonu.Click-=IadeKaydetButonu_Click;
			kaydetButonu.Click+=IadeKaydetButonu_Click;
			SatisRaporButonTemasiniUygula(kaydetButonu , Color.FromArgb(234 , 88 , 12) , Color.White , Color.FromArgb(234 , 88 , 12));
			Button yenileButonu = SatisRaporButonuOlustur("Yenile");
			yenileButonu.Click+=(sender , e) => GunlukSatisVerileriniYenile();
			SatisRaporButonTemasiniUygula(yenileButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
			Button silButonu = SatisRaporButonuOlustur("Seçiliyi Sil");
			silButonu.Click-=IadeSilButonu_Click;
			silButonu.Click+=IadeSilButonu_Click;
			SatisRaporButonTemasiniUygula(silButonu , Color.FromArgb(241 , 245 , 249) , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			Button temizleButonu = SatisRaporButonuOlustur("Temizle");
			temizleButonu.Click+=(sender , e) => IadeFormunuTemizle();
			SatisRaporButonTemasiniUygula(temizleButonu , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));

			butonPaneli.Controls.Add(kaydetButonu , 0 , 0);
			butonPaneli.Controls.Add(yenileButonu , 1 , 0);
			butonPaneli.Controls.Add(temizleButonu , 0 , 1);
			butonPaneli.Controls.Add(silButonu , 1 , 1);
			girisLayout.Controls.Add(butonPaneli , 1 , 7);

			girisKutusu.Controls.Add(girisLayout);

			solLayout.Controls.Add(kartLayout , 0 , 0);
			solLayout.Controls.Add(listeKutusu , 0 , 1);
			anaLayout.Controls.Add(solLayout , 0 , 0);
			anaLayout.Controls.Add(girisKutusu , 1 , 0);
			_iadeTabPage.Controls.Add(anaLayout);

			SatisRaporAramaKutusuHazirla(_iadeAramaKutusu , _iadeGrid);
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
				"GÜNLÜK CİRO" ,
				"GÜNLÜK KAR" ,
				"KAR ORANI" ,
				"GÜNLÜK İADE" ,
				out _gunlukSatisToplamCiroLabel ,
				out _gunlukSatisToplamKarLabel ,
				out _gunlukSatisToplamKarOraniLabel ,
				out _gunlukSatisToplamMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Günlük Satış Özeti");
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

			SatisRaporAramaKutusuHazirla(_gunlukSatisToplamAramaKutusu , _gunlukSatisToplamGrid);
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
				"AYLIK CİRO" ,
				"AYLIK KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _aylikSatisCiroLabel ,
				out _aylikSatisKarLabel ,
				out _aylikSatisKarOraniLabel ,
				out _aylikSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Aylık Satış Özeti");
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

			SatisRaporAramaKutusuHazirla(_aylikSatisAramaKutusu , _aylikSatisGrid);
		}

		private void AylikFabrikaFaturaSayfasiniOlustur ()
		{
			if(_aylikFabrikaFaturaTabPage==null)
				return;

			_aylikFabrikaFaturaTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_aylikFabrikaFaturaTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"AYLIK TOPLAM" ,
				"FATURA SAYISI" ,
				"KALEM SAYISI" ,
				"ORTALAMA FATURA" ,
				out _aylikFabrikaFaturaToplamLabel ,
				out _aylikFabrikaFaturaSayisiLabel ,
				out _aylikFabrikaFaturaKalemLabel ,
				out _aylikFabrikaFaturaOrtalamaLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Aylık Fabrika Faturaları");
			Size filtreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu , aramaKutusuBoyutu);
			Label ayLabel = SatisRaporFiltreEtiketiOlustur("Ay");
			_aylikFabrikaFaturaAyPicker=SatisRaporTarihSeciciOlustur(true , filtreBoyutu);
			_aylikFabrikaFaturaAyPicker.ValueChanged-=AylikFabrikaFaturaAyPicker_ValueChanged;
			_aylikFabrikaFaturaAyPicker.ValueChanged+=AylikFabrikaFaturaAyPicker_ValueChanged;
			_aylikFabrikaFaturaAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_aylikFabrikaFaturaGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				ayLabel ,
				_aylikFabrikaFaturaAyPicker ,
				null ,
				_aylikFabrikaFaturaAramaKutusu ,
				filtreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_aylikFabrikaFaturaGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_aylikFabrikaFaturaTabPage.Controls.Add(anaLayout);

			SatisRaporAramaKutusuHazirla(_aylikFabrikaFaturaAramaKutusu , _aylikFabrikaFaturaGrid);
		}

		private void AylikMusteriFaturaSayfasiniOlustur ()
		{
			if(_aylikMusteriFaturaTabPage==null)
				return;

			_aylikMusteriFaturaTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_aylikMusteriFaturaTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"AYLIK TOPLAM" ,
				"FATURA SAYISI" ,
				"KALEM SAYISI" ,
				"ORTALAMA FATURA" ,
				out _aylikMusteriFaturaToplamLabel ,
				out _aylikMusteriFaturaSayisiLabel ,
				out _aylikMusteriFaturaKalemLabel ,
				out _aylikMusteriFaturaOrtalamaLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Aylık Müşteri Faturaları");
			Size filtreBoyutu = SatisRaporKompaktFiltreKontrolBoyutunuGetir();
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu , aramaKutusuBoyutu);
			Label ayLabel = SatisRaporFiltreEtiketiOlustur("Ay");
			_aylikMusteriFaturaAyPicker=SatisRaporTarihSeciciOlustur(true , filtreBoyutu);
			_aylikMusteriFaturaAyPicker.ValueChanged-=AylikMusteriFaturaAyPicker_ValueChanged;
			_aylikMusteriFaturaAyPicker.ValueChanged+=AylikMusteriFaturaAyPicker_ValueChanged;
			_aylikMusteriFaturaAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			_aylikMusteriFaturaGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				ayLabel ,
				_aylikMusteriFaturaAyPicker ,
				null ,
				_aylikMusteriFaturaAramaKutusu ,
				filtreBoyutu ,
				null ,
				aramaKutusuBoyutu);

			listeKutusu.Controls.Add(_aylikMusteriFaturaGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_aylikMusteriFaturaTabPage.Controls.Add(anaLayout);

			SatisRaporAramaKutusuHazirla(_aylikMusteriFaturaAramaKutusu , _aylikMusteriFaturaGrid);
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
				"GENEL CİRO" ,
				"GENEL KAR" ,
				"KAR ORANI" ,
				"TOPLAM ADET" ,
				out _toplamSatisCiroLabel ,
				out _toplamSatisKarLabel ,
				out _toplamSatisKarOraniLabel ,
				out _toplamSatisMiktarLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Genel Satış Özeti");
			Size filtreBoyutu = SatisRaporFiltreKontrolBoyutunuGetir();
			Panel filtrePaneli = SatisRaporFiltrePaneliniOlustur(filtreBoyutu);
			_toplamSatisAramaKutusu=SatisRaporAramaKutusuOlustur(filtreBoyutu);
			_toplamSatisGrid=SatisRaporGridiOlustur();

			SatisRaporFiltrePaneliniYerlesitir(
				filtrePaneli ,
				null ,
				null ,
				null ,
				_toplamSatisAramaKutusu ,
				filtreBoyutu);

			listeKutusu.Controls.Add(_toplamSatisGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_toplamSatisTabPage.Controls.Add(anaLayout);

			SatisRaporAramaKutusuHazirla(_toplamSatisAramaKutusu , _toplamSatisGrid);
		}

		private void GenelToplamSayfasiniOlustur ()
		{
			if(_genelToplamTabPage==null)
				return;

			_genelToplamTabPage.Controls.Clear();

			TableLayoutPanel anaLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=1,
				RowCount=2,
				BackColor=_genelToplamTabPage.BackColor
			};
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 122f));
			anaLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100f));

			TableLayoutPanel kartLayout = SatisKartLayoutiniOlustur(
				"GENEL CİRO" ,
				"GENEL KAR" ,
				"TOPTANCI ÖDEME" ,
				"KALAN BORÇ" ,
				out _genelToplamCiroLabel ,
				out _genelToplamKarLabel ,
				out _genelToplamToptanciOdemeLabel ,
				out _genelToplamKalanBorcLabel);

			GroupBox listeKutusu = SatisRaporGroupBoxOlustur("Finans Özeti");
			Size aramaKutusuBoyutu = SatisRaporAramaKutusuBoyutunuGetir();
			Panel filtrePaneli = GenelToplamFiltrePaneliniOlustur(aramaKutusuBoyutu);
			_genelToplamGrid=SatisRaporGridiOlustur();

			listeKutusu.Controls.Add(_genelToplamGrid);
			listeKutusu.Controls.Add(filtrePaneli);

			anaLayout.Controls.Add(kartLayout , 0 , 0);
			anaLayout.Controls.Add(listeKutusu , 0 , 1);
			_genelToplamTabPage.Controls.Add(anaLayout);
		}

		private Panel GenelToplamFiltrePaneliniOlustur ( Size aramaKutusuBoyutu )
		{
			Panel panel = new Panel
			{
				Dock=DockStyle.Top,
				Height=58,
				BackColor=_genelToplamTabPage?.BackColor??SatisRaporArkaPlanRenginiGetir(),
				Padding=new Padding(0 , 4 , 0 , 10)
			};

			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=3,
				RowCount=1,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=panel.BackColor
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 466F));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , aramaKutusuBoyutu.Width));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 42F));

			FlowLayoutPanel aksiyonPaneli = new FlowLayoutPanel
			{
				Dock=DockStyle.Fill,
				WrapContents=false,
				FlowDirection=FlowDirection.LeftToRight,
				AutoSize=false,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=panel.BackColor
			};

			_genelToplamYazdirButonu=new Button();
			HazirlaNotAksiyonButonu(_genelToplamYazdirButonu , "Yazdır" , "Print.png" , Color.FromArgb(37 , 99 , 235));
			_genelToplamYazdirButonu.Size=new Size(132 , 42);
			_genelToplamYazdirButonu.Margin=new Padding(0 , 0 , 12 , 0);
			_genelToplamYazdirButonu.Click-=GenelToplamYazdirButonu_Click;
			_genelToplamYazdirButonu.Click+=GenelToplamYazdirButonu_Click;

			_genelToplamExcelButonu=new Button();
			HazirlaNotAksiyonButonu(_genelToplamExcelButonu , "Excel'e Aktar" , "Microsoft Excel.png" , Color.FromArgb(22 , 163 , 74));
			_genelToplamExcelButonu.Size=new Size(156 , 42);
			_genelToplamExcelButonu.Margin=new Padding(0 , 0 , 12 , 0);
			_genelToplamExcelButonu.Click-=GenelToplamExcelButonu_Click;
			_genelToplamExcelButonu.Click+=GenelToplamExcelButonu_Click;

			_genelToplamPdfButonu=new Button();
			HazirlaNotAksiyonButonu(_genelToplamPdfButonu , "PDF Oluştur" , "PDF.png" , Color.White , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(254 , 202 , 202));
			_genelToplamPdfButonu.Size=new Size(154 , 42);
			_genelToplamPdfButonu.Margin=Padding.Empty;
			_genelToplamPdfButonu.Click-=GenelToplamPdfButonu_Click;
			_genelToplamPdfButonu.Click+=GenelToplamPdfButonu_Click;

			aksiyonPaneli.Controls.Add(_genelToplamYazdirButonu);
			aksiyonPaneli.Controls.Add(_genelToplamExcelButonu);
			aksiyonPaneli.Controls.Add(_genelToplamPdfButonu);

			Panel aramaPaneli = new Panel
			{
				Dock=DockStyle.Fill,
				BackColor=panel.BackColor,
				Margin=Padding.Empty,
				Padding=new Padding(0 , Math.Max(0 , (42-aramaKutusuBoyutu.Height)/2) , 0 , 0)
			};

			_genelToplamAramaKutusu=SatisRaporAramaKutusuOlustur(aramaKutusuBoyutu);
			SatisRaporAramaKutusuHazirla(_genelToplamAramaKutusu , _genelToplamGrid);
			_genelToplamAramaKutusu.BackColor=panel.BackColor;
			_genelToplamAramaKutusu.Margin=Padding.Empty;
			_genelToplamAramaKutusu.Size=aramaKutusuBoyutu;
			_genelToplamAramaKutusu.MinimumSize=aramaKutusuBoyutu;
			_genelToplamAramaKutusu.MaximumSize=aramaKutusuBoyutu;
			_genelToplamAramaKutusu.Dock=DockStyle.Top;
			aramaPaneli.Controls.Add(_genelToplamAramaKutusu);

			layout.Controls.Add(aksiyonPaneli , 0 , 0);
			layout.Controls.Add(new Panel { Dock=DockStyle.Fill, BackColor=panel.BackColor } , 1 , 0);
			layout.Controls.Add(aramaPaneli , 2 , 0);

			panel.Controls.Add(layout);
			return panel;
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
				Font=new Font("Microsoft Sans Serif" , 9f , FontStyle.Regular , GraphicsUnit.Point , 162),
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
			bool kompaktButon = butonBoyutu.Height<=24;
			Padding butonIciBosluk = icBosluk??( kompaktButon ? Padding.Empty : new Padding(12 , 6 , 12 , 6) );
			Button buton = new Button
			{
				Text=metin,
				AutoSize=false,
				Size=butonBoyutu,
				BackColor=Color.White,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				FlatStyle=FlatStyle.Flat,
				Font=new Font("Microsoft Sans Serif" , 9f , FontStyle.Regular , GraphicsUnit.Point , 162),
				Margin=Padding.Empty,
				Padding=butonIciBosluk,
				Cursor=Cursors.Hand,
				TextAlign=ContentAlignment.MiddleCenter,
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
			aramaKutusu.Font=new Font("Microsoft Sans Serif" , 9f , FontStyle.Regular , GraphicsUnit.Point , 162);
			aramaKutusu.Margin=Padding.Empty;
			aramaKutusu.Size=aramaKutusuBoyutu;
			return aramaKutusu;
		}

		private void SatisRaporAramaKutusuHazirla ( TextBox aramaKutusu , DataGridView hedefGrid = null )
		{
			AramaKutusuHazirla(aramaKutusu , hedefGrid);
			if(aramaKutusu==null)
				return;

			aramaKutusu.BackColor=SatisRaporArkaPlanRenginiGetir();
			aramaKutusu.Size=SatisRaporFiltreKontrolBoyutunuGetir();
		}

		private Color SatisRaporArkaPlanRenginiGetir ()
		{
			Color arkaPlanRengi = _satisRaporTabPage?.BackColor??this.BackColor;
			if(!arkaPlanRengi.IsEmpty&&arkaPlanRengi!=Color.Transparent&&arkaPlanRengi.A==255)
				return arkaPlanRengi;

			return SystemColors.Control;
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
	}
}

