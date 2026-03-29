using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TEKN\u0130K_SERV\u0130S
{
	public partial class Form1
	{
		private bool _faturaTahsilatTablosuVar;
		private bool _cariHesapFaturaTablosuVar;
		private bool _cariHesapSecimYukleniyor;
		private int? _cariHesapSeciliCariId;
		private int? _cariHesapSeciliTahsilatId;
		private decimal _cariHesapSeciliTahsilatTutari;
		private Label _cariHesapToplamCariDegerLabel;
		private Label _cariHesapToplamFaturaDegerLabel;
		private Label _cariHesapToplamTahsilatDegerLabel;
		private Label _cariHesapKalanDegerLabel;
		private DataGridView _cariHesapOzetGrid;
		private DataGridView _cariHesapHareketGrid;
		private ComboBox _cariHesapCariComboBox;
		private DateTimePicker _cariHesapTarihPicker;
		private TextBox _cariHesapToplamFaturaTextBox;
		private TextBox _cariHesapToplamTahsilatTextBox;
		private TextBox _cariHesapYeniTahsilatTextBox;
		private TextBox _cariHesapAciklamaTextBox;
		private TextBox _cariHesapKalanTextBox;
		private TextBox _cariHesapAramaTextBox;
		private Button _cariHesapTahsilatKaydetButonu;
		private Button _cariHesapTahsilatGuncelleButonu;
		private Button _cariHesapTahsilatSilButonu;
		private Button _cariHesapYazdirButonu;
		private Button _cariHesapPdfButonu;
		private Button _cariHesapExcelButonu;
		private Button _cariHesapTemizleButonu;

		private void EnsureCariHesapAltyapi ()
		{
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					_cariHesapFaturaTablosuVar=TabloVarMi(conn , "Faturalar");
					_faturaTahsilatTablosuVar=TabloVarMi(conn , "FaturaTahsilatlari");
					bool eskiOdemeTarihiKolonuVar = _faturaTahsilatTablosuVar&&KolonVarMi(conn , "FaturaTahsilatlari" , "OdemeTarihi");
					bool eskiOdenenTutarKolonuVar = _faturaTahsilatTablosuVar&&KolonVarMi(conn , "FaturaTahsilatlari" , "OdenenTutar");
					if(!_faturaTahsilatTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("CREATE TABLE [FaturaTahsilatlari] ([TahsilatID] AUTOINCREMENT, [CariID] LONG, [FaturaID] LONG, [TahsilatTarihi] DATETIME, [AlinanTutar] CURRENCY, [Aciklama] LONGTEXT)" , conn))
							cmd.ExecuteNonQuery();
						_faturaTahsilatTablosuVar=true;
					}

					if(!KolonVarMi(conn , "FaturaTahsilatlari" , "CariID"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [FaturaTahsilatlari] ADD COLUMN [CariID] LONG" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "FaturaTahsilatlari" , "FaturaID"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [FaturaTahsilatlari] ADD COLUMN [FaturaID] LONG" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "FaturaTahsilatlari" , "TahsilatTarihi"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [FaturaTahsilatlari] ADD COLUMN [TahsilatTarihi] DATETIME" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "FaturaTahsilatlari" , "AlinanTutar"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [FaturaTahsilatlari] ADD COLUMN [AlinanTutar] CURRENCY" , conn))
							cmd.ExecuteNonQuery();
					}

					if(!KolonVarMi(conn , "FaturaTahsilatlari" , "Aciklama"))
					{
						using(OleDbCommand cmd = new OleDbCommand("ALTER TABLE [FaturaTahsilatlari] ADD COLUMN [Aciklama] LONGTEXT" , conn))
							cmd.ExecuteNonQuery();
					}

					if(eskiOdemeTarihiKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [FaturaTahsilatlari] SET [TahsilatTarihi]=[OdemeTarihi] WHERE [TahsilatTarihi] IS NULL AND [OdemeTarihi] IS NOT NULL" , conn))
							cmd.ExecuteNonQuery();
					}

					if(eskiOdenenTutarKolonuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [FaturaTahsilatlari] SET [AlinanTutar]=[OdenenTutar] WHERE ([AlinanTutar] IS NULL OR [AlinanTutar]=0) AND [OdenenTutar] IS NOT NULL" , conn))
							cmd.ExecuteNonQuery();
					}

					if(_cariHesapFaturaTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("UPDATE [FaturaTahsilatlari] AS T INNER JOIN [Faturalar] AS F ON T.[FaturaID]=F.[FaturaID] SET T.[CariID]=F.[CariID] WHERE T.[CariID] IS NULL AND T.[FaturaID] IS NOT NULL" , conn))
							cmd.ExecuteNonQuery();
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap altyapisi kontrol hatasi: "+ex.Message);
			}
		}

		private void KurCariHesapSekmesi ()
		{
			if(tabPage27==null)
				return;

			tabPage27.SuspendLayout();
			_cariHesapSecimYukleniyor=true;
			try
			{
				tabPage27.Controls.Clear();
				tabPage27.BackColor=Color.FromArgb(241 , 245 , 249);
				tabPage27.Padding=new Padding(14);

				TableLayoutPanel rootLayout = new TableLayoutPanel
				{
					Dock=DockStyle.Fill,
					BackColor=tabPage27.BackColor,
					ColumnCount=1,
					RowCount=2,
					Margin=Padding.Empty,
					Padding=Padding.Empty
				};
				rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 188F));
				rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				Panel headerPanel = CariHesapHeaderOlustur();
				TableLayoutPanel contentLayout = new TableLayoutPanel
				{
					Dock=DockStyle.Fill,
					BackColor=tabPage27.BackColor,
					ColumnCount=2,
					RowCount=1,
					Margin=Padding.Empty,
					Padding=Padding.Empty
				};
				contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 34F));
				contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 66F));
				contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

				GroupBox detayKarti = CariHesapDetayKartiOlustur();
				detayKarti.Margin=new Padding(0 , 0 , 12 , 0);

				TableLayoutPanel sagLayout = new TableLayoutPanel
				{
					Dock=DockStyle.Fill,
					BackColor=tabPage27.BackColor,
					ColumnCount=1,
					RowCount=2,
					Margin=Padding.Empty,
					Padding=Padding.Empty
				};
				sagLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
				sagLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 46F));
				sagLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 54F));

				GroupBox listeKarti = CariHesapListeKartiOlustur("Cari Ozet Listesi" , "Tum cariler, biriken faturalar ve tahsilatlar burada gorunur." , true);
				listeKarti.Margin=new Padding(0 , 0 , 0 , 12);
				GroupBox hareketKarti = CariHesapListeKartiOlustur("Hesap Hareketleri" , "Secili carinin otomatik fatura borclari ve manuel tahsilatlari listelenir." , false);
				hareketKarti.Margin=Padding.Empty;

				sagLayout.Controls.Add(listeKarti , 0 , 0);
				sagLayout.Controls.Add(hareketKarti , 0 , 1);

				contentLayout.Controls.Add(detayKarti , 0 , 0);
				contentLayout.Controls.Add(sagLayout , 1 , 0);

				rootLayout.Controls.Add(headerPanel , 0 , 0);
				rootLayout.Controls.Add(contentLayout , 0 , 1);
				tabPage27.Controls.Add(rootLayout);

				_cariHesapCariComboBox.SelectedIndexChanged-=CariHesapCariComboBox_SelectedIndexChanged;
				_cariHesapCariComboBox.SelectedIndexChanged+=CariHesapCariComboBox_SelectedIndexChanged;

				_cariHesapYeniTahsilatTextBox.TextChanged-=CariHesapYeniTahsilatTextBox_TextChanged;
				_cariHesapYeniTahsilatTextBox.TextChanged+=CariHesapYeniTahsilatTextBox_TextChanged;
				_cariHesapYeniTahsilatTextBox.KeyPress-=SepetSayisal_KeyPress;
				_cariHesapYeniTahsilatTextBox.KeyPress+=SepetSayisal_KeyPress;

				_cariHesapTahsilatKaydetButonu.Click-=CariHesapTahsilatKaydetButonu_Click;
				_cariHesapTahsilatKaydetButonu.Click+=CariHesapTahsilatKaydetButonu_Click;
				_cariHesapTahsilatGuncelleButonu.Click-=CariHesapTahsilatGuncelleButonu_Click;
				_cariHesapTahsilatGuncelleButonu.Click+=CariHesapTahsilatGuncelleButonu_Click;
				_cariHesapTahsilatSilButonu.Click-=CariHesapTahsilatSilButonu_Click;
				_cariHesapTahsilatSilButonu.Click+=CariHesapTahsilatSilButonu_Click;
				_cariHesapYazdirButonu.Click-=CariHesapYazdirButonu_Click;
				_cariHesapYazdirButonu.Click+=CariHesapYazdirButonu_Click;
				_cariHesapPdfButonu.Click-=CariHesapPdfButonu_Click;
				_cariHesapPdfButonu.Click+=CariHesapPdfButonu_Click;
				_cariHesapExcelButonu.Click-=CariHesapExcelButonu_Click;
				_cariHesapExcelButonu.Click+=CariHesapExcelButonu_Click;
				_cariHesapTemizleButonu.Click-=CariHesapTemizleButonu_Click;
				_cariHesapTemizleButonu.Click+=CariHesapTemizleButonu_Click;

				_cariHesapOzetGrid.CellClick-=CariHesapOzetGrid_CellClick;
				_cariHesapOzetGrid.CellClick+=CariHesapOzetGrid_CellClick;
				_cariHesapHareketGrid.CellClick-=CariHesapHareketGrid_CellClick;
				_cariHesapHareketGrid.CellClick+=CariHesapHareketGrid_CellClick;

				_cariHesapAramaTextBox.TextChanged-=CariHesapAramaTextBox_TextChanged;
				_cariHesapAramaTextBox.TextChanged+=CariHesapAramaTextBox_TextChanged;

				NotGridStiliniUygula(_cariHesapOzetGrid);
				NotGridStiliniUygula(_cariHesapHareketGrid);
			}
			finally
			{
				_cariHesapSecimYukleniyor=false;
				tabPage27.ResumeLayout();
			}

			CariHesapFormTemizle(true);
			CariHesapVerileriniYenile();
		}

		private Panel CariHesapHeaderOlustur ()
		{
			Panel headerPanel = new Panel
			{
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				BorderStyle=BorderStyle.FixedSingle,
				Margin=new Padding(0 , 0 , 0 , 12),
				Padding=new Padding(20 , 18 , 20 , 18)
			};

			TableLayoutPanel headerLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ColumnCount=2,
				RowCount=2,
				Margin=Padding.Empty,
				Padding=Padding.Empty
			};
			headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 420F));
			headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 62F));
			headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

			Panel textPanel = new Panel
			{
				Dock=DockStyle.Fill,
				Margin=Padding.Empty,
				Padding=Padding.Empty,
				BackColor=Color.White
			};

			Label titleLabel = new Label
			{
				AutoSize=true,
				Font=new Font("Segoe UI" , 18F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Location=new Point(0 , 0),
				Text="Cari Hesap"
			};

			Label subtitleLabel = new Label
			{
				AutoSize=false,
				Font=new Font("Segoe UI" , 10F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				Location=new Point(0 , 34),
				Size=new Size(760 , 24),
				Text="Cariler, duzenlenen faturalar, manuel tahsilatlar ve kalan bakiyeler tek ekranda takip edilir."
			};

			textPanel.Controls.Add(titleLabel);
			textPanel.Controls.Add(subtitleLabel);

			FlowLayoutPanel badgePanel = new FlowLayoutPanel
			{
				Dock=DockStyle.Fill,
				FlowDirection=FlowDirection.LeftToRight,
				WrapContents=false,
				AutoSize=true,
				AutoSizeMode=AutoSizeMode.GrowAndShrink,
				Margin=Padding.Empty,
				Padding=new Padding(0 , 12 , 0 , 0),
				BackColor=Color.White
			};

			Label otomatikRozet = CariHesapRozetiOlustur("Faturalar Otomatik" , Color.FromArgb(239 , 246 , 255) , Color.FromArgb(30 , 64 , 175));
			Label manuelRozet = CariHesapRozetiOlustur("Tahsilat Manuel" , Color.FromArgb(236 , 253 , 245) , Color.FromArgb(6 , 95 , 70));
			otomatikRozet.Margin=new Padding(0 , 0 , 10 , 0);
			badgePanel.Controls.Add(otomatikRozet);
			badgePanel.Controls.Add(manuelRozet);

			TableLayoutPanel ozetLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ColumnCount=4,
				RowCount=1,
				Margin=new Padding(0 , 10 , 0 , 0),
				Padding=Padding.Empty
			};
			for(int i = 0 ; i<4 ; i++)
				ozetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
			ozetLayout.RowStyles.Add(new RowStyle(SizeType.Percent , 100F));

			Panel toplamCariKart = NotOzetKartiOlustur("TOPLAM CARI" , Color.FromArgb(248 , 250 , 252) , Color.FromArgb(100 , 116 , 139) , out _cariHesapToplamCariDegerLabel);
			Panel toplamFaturaKart = NotOzetKartiOlustur("TOPLAM FATURA" , Color.FromArgb(239 , 246 , 255) , Color.FromArgb(37 , 99 , 235) , out _cariHesapToplamFaturaDegerLabel);
			Panel toplamTahsilatKart = NotOzetKartiOlustur("TOPLAM TAHSILAT" , Color.FromArgb(236 , 253 , 245) , Color.FromArgb(13 , 148 , 136) , out _cariHesapToplamTahsilatDegerLabel);
			Panel kalanKart = NotOzetKartiOlustur("TOPLAM KALAN" , Color.FromArgb(255 , 247 , 237) , Color.FromArgb(234 , 88 , 12) , out _cariHesapKalanDegerLabel);

			toplamCariKart.Margin=new Padding(0 , 0 , 12 , 0);
			toplamFaturaKart.Margin=new Padding(0 , 0 , 12 , 0);
			toplamTahsilatKart.Margin=new Padding(0 , 0 , 12 , 0);
			kalanKart.Margin=Padding.Empty;

			ozetLayout.Controls.Add(toplamCariKart , 0 , 0);
			ozetLayout.Controls.Add(toplamFaturaKart , 1 , 0);
			ozetLayout.Controls.Add(toplamTahsilatKart , 2 , 0);
			ozetLayout.Controls.Add(kalanKart , 3 , 0);

			headerLayout.Controls.Add(textPanel , 0 , 0);
			headerLayout.Controls.Add(badgePanel , 1 , 0);
			headerLayout.Controls.Add(ozetLayout , 0 , 1);
			headerLayout.SetColumnSpan(ozetLayout , 2);

			headerPanel.Controls.Add(headerLayout);
			return headerPanel;
		}

		private Label CariHesapRozetiOlustur ( string metin , Color arkaPlan , Color yaziRengi )
		{
			return new Label
			{
				AutoSize=true,
				BackColor=arkaPlan,
				ForeColor=yaziRengi,
				Font=new Font("Segoe UI" , 9F , FontStyle.Bold),
				Padding=new Padding(12 , 8 , 12 , 8),
				Text=metin
			};
		}

		private GroupBox CariHesapDetayKartiOlustur ()
		{
			GroupBox kart = new GroupBox();
			HazirlaNotListeKutusu(kart , string.Empty);
			kart.BackColor=Color.White;
			kart.Padding=new Padding(18);

			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				BackColor=Color.White,
				ColumnCount=1,
				RowCount=17,
				AutoScroll=true,
				Margin=Padding.Empty,
				Padding=Padding.Empty
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 32F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 38F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 30F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 28F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 28F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 28F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 28F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 54F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 18F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 30F));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute , 100F));

			Label titleLabel = new Label
			{
				Dock=DockStyle.Fill,
				Text="Tahsilat Islemleri",
				Font=new Font("Segoe UI" , 13F , FontStyle.Bold),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				TextAlign=ContentAlignment.MiddleLeft,
				Margin=Padding.Empty
			};

			Label subtitleLabel = new Label
			{
				Dock=DockStyle.Fill,
				Text="Secili carinin tahsilatini kaydedin, manuel hareketi guncelleyin ya da silin; raporlari ayni alandan alin.",
				Font=new Font("Segoe UI" , 9.25F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				TextAlign=ContentAlignment.MiddleLeft,
				Margin=Padding.Empty
			};

			_cariHesapCariComboBox=new ComboBox();
			CariHesapComboStiliUygula(_cariHesapCariComboBox);

			_cariHesapTarihPicker=new DateTimePicker();
			CariHesapDatePickerStiliUygula(_cariHesapTarihPicker);

			_cariHesapToplamFaturaTextBox=new TextBox();
			CariHesapTextBoxStiliUygula(_cariHesapToplamFaturaTextBox , true , false);

			_cariHesapToplamTahsilatTextBox=new TextBox();
			CariHesapTextBoxStiliUygula(_cariHesapToplamTahsilatTextBox , true , false);

			_cariHesapYeniTahsilatTextBox=new TextBox();
			CariHesapTextBoxStiliUygula(_cariHesapYeniTahsilatTextBox , false , false);

			_cariHesapAciklamaTextBox=new TextBox();
			CariHesapTextBoxStiliUygula(_cariHesapAciklamaTextBox , false , true);

			_cariHesapKalanTextBox=new TextBox();
			CariHesapTextBoxStiliUygula(_cariHesapKalanTextBox , true , false);

			TableLayoutPanel buttonPanel = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				ColumnCount=4,
				RowCount=2,
				Margin=Padding.Empty,
				Padding=new Padding(0 , 2 , 0 , 0),
				BackColor=Color.White
			};
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 25F));
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute , 44F));
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute , 44F));

			_cariHesapTahsilatKaydetButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapTahsilatKaydetButonu , "Tahsil" , "Save.png" , Color.FromArgb(13 , 148 , 136));
			CariHesapKompaktButonStiliUygula(_cariHesapTahsilatKaydetButonu);
			_cariHesapTahsilatKaydetButonu.Dock=DockStyle.Fill;
			_cariHesapTahsilatKaydetButonu.Margin=new Padding(0 , 0 , 8 , 8);

			_cariHesapTahsilatGuncelleButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapTahsilatGuncelleButonu , "Guncelle" , "Renew.png" , Color.White , Color.FromArgb(15 , 23 , 42) , Color.FromArgb(148 , 163 , 184));
			CariHesapKompaktButonStiliUygula(_cariHesapTahsilatGuncelleButonu);
			_cariHesapTahsilatGuncelleButonu.Dock=DockStyle.Fill;
			_cariHesapTahsilatGuncelleButonu.Margin=new Padding(0 , 0 , 8 , 8);

			_cariHesapTahsilatSilButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapTahsilatSilButonu , "Sil" , "Delete File.png" , Color.White , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(239 , 68 , 68));
			CariHesapKompaktButonStiliUygula(_cariHesapTahsilatSilButonu);
			_cariHesapTahsilatSilButonu.Dock=DockStyle.Fill;
			_cariHesapTahsilatSilButonu.Margin=new Padding(0 , 0 , 8 , 8);

			_cariHesapTemizleButonu=new Button();
			HazirlaNotIkincilButonu(_cariHesapTemizleButonu , "Temizle" , "Broom.png");
			CariHesapKompaktButonStiliUygula(_cariHesapTemizleButonu);
			_cariHesapTemizleButonu.Dock=DockStyle.Fill;
			_cariHesapTemizleButonu.Margin=new Padding(0 , 0 , 0 , 8);

			_cariHesapYazdirButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapYazdirButonu , "Yazdir" , "Print.png" , Color.FromArgb(37 , 99 , 235));
			CariHesapKompaktButonStiliUygula(_cariHesapYazdirButonu);
			_cariHesapYazdirButonu.Dock=DockStyle.Fill;
			_cariHesapYazdirButonu.Margin=new Padding(0 , 0 , 8 , 0);

			_cariHesapExcelButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapExcelButonu , "Excel" , "Microsoft Excel.png" , Color.FromArgb(22 , 163 , 74));
			CariHesapKompaktButonStiliUygula(_cariHesapExcelButonu);
			_cariHesapExcelButonu.Dock=DockStyle.Fill;
			_cariHesapExcelButonu.Margin=new Padding(0 , 0 , 8 , 0);

			_cariHesapPdfButonu=new Button();
			HazirlaNotAksiyonButonu(_cariHesapPdfButonu , "PDF" , "PDF.png" , Color.White , Color.FromArgb(185 , 28 , 28) , Color.FromArgb(254 , 202 , 202));
			CariHesapKompaktButonStiliUygula(_cariHesapPdfButonu);
			_cariHesapPdfButonu.Dock=DockStyle.Fill;
			_cariHesapPdfButonu.Margin=new Padding(0 , 0 , 8 , 0);

			buttonPanel.Controls.Add(_cariHesapTahsilatKaydetButonu , 0 , 0);
			buttonPanel.Controls.Add(_cariHesapTahsilatGuncelleButonu , 1 , 0);
			buttonPanel.Controls.Add(_cariHesapTahsilatSilButonu , 2 , 0);
			buttonPanel.Controls.Add(_cariHesapTemizleButonu , 3 , 0);
			buttonPanel.Controls.Add(_cariHesapYazdirButonu , 0 , 1);
			buttonPanel.Controls.Add(_cariHesapExcelButonu , 1 , 1);
			buttonPanel.Controls.Add(_cariHesapPdfButonu , 2 , 1);
			buttonPanel.Controls.Add(new Panel { Dock=DockStyle.Fill , BackColor=Color.White } , 3 , 1);

			layout.Controls.Add(titleLabel , 0 , 0);
			layout.Controls.Add(subtitleLabel , 0 , 1);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Cari") , 0 , 2);
			layout.Controls.Add(_cariHesapCariComboBox , 0 , 3);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Tarih / Saat") , 0 , 4);
			layout.Controls.Add(_cariHesapTarihPicker , 0 , 5);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Toplam Fatura") , 0 , 6);
			layout.Controls.Add(_cariHesapToplamFaturaTextBox , 0 , 7);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Toplam Tahsilat") , 0 , 8);
			layout.Controls.Add(_cariHesapToplamTahsilatTextBox , 0 , 9);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Alinan Tahsilat") , 0 , 10);
			layout.Controls.Add(_cariHesapYeniTahsilatTextBox , 0 , 11);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Aciklama") , 0 , 12);
			layout.Controls.Add(_cariHesapAciklamaTextBox , 0 , 13);
			layout.Controls.Add(CariHesapAlanEtiketiOlustur("Kalan Tutar") , 0 , 14);
			layout.Controls.Add(_cariHesapKalanTextBox , 0 , 15);
			layout.Controls.Add(buttonPanel , 0 , 16);

			kart.Controls.Add(layout);
			return kart;
		}

		private GroupBox CariHesapListeKartiOlustur ( string baslik , string aciklama , bool aramaKutusuGoster )
		{
			GroupBox kart = new GroupBox();
			HazirlaNotListeKutusu(kart , string.Empty);
			kart.BackColor=Color.White;
			Size standartAramaKutusuBoyutu = AramaKutusuStandartBoyutunuGetir();
			Color aramaAlanArkaPlanRengi = kart.BackColor;
			if(aramaAlanArkaPlanRengi.IsEmpty||aramaAlanArkaPlanRengi==Color.Transparent)
				aramaAlanArkaPlanRengi=tabPage27?.BackColor??this.BackColor;

			DataGridView grid = aramaKutusuGoster ? ( _cariHesapOzetGrid??new DataGridView() ) : ( _cariHesapHareketGrid??new DataGridView() );
			if(aramaKutusuGoster)
				_cariHesapOzetGrid=grid;
			else
				_cariHesapHareketGrid=grid;

			grid.Dock=DockStyle.Fill;
			grid.Margin=Padding.Empty;
			grid.ReadOnly=true;

			Panel ustPanel = new Panel
			{
				Dock=DockStyle.Top,
				Height=aramaKutusuGoster ? 72 : 58,
				BackColor=aramaAlanArkaPlanRengi,
				Margin=Padding.Empty,
				Padding=new Padding(2 , 0 , 2 , 10)
			};

			TableLayoutPanel ustLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				BackColor=ustPanel.BackColor,
				ColumnCount=2,
				RowCount=2,
				Margin=Padding.Empty,
				Padding=Padding.Empty
			};
			ustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));
			ustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , aramaKutusuGoster ? 268F : 10F));
			ustLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 28F));
			ustLayout.RowStyles.Add(new RowStyle(SizeType.Absolute , 22F));

			Label baslikLabel = new Label
			{
				Dock=DockStyle.Fill,
				Text=baslik,
				Font=new Font("Segoe UI" , 10.5F , FontStyle.Bold),
				ForeColor=Color.FromArgb(30 , 41 , 59),
				TextAlign=ContentAlignment.MiddleLeft,
				Margin=Padding.Empty
			};

			Label aciklamaLabel = new Label
			{
				Dock=DockStyle.Fill,
				Text=aciklama,
				Font=new Font("Segoe UI" , 8.9F , FontStyle.Regular),
				ForeColor=Color.FromArgb(100 , 116 , 139),
				TextAlign=ContentAlignment.MiddleLeft,
				Margin=Padding.Empty
			};

			ustLayout.Controls.Add(baslikLabel , 0 , 0);
			ustLayout.Controls.Add(aciklamaLabel , 0 , 1);

			if(aramaKutusuGoster)
			{
				_cariHesapAramaTextBox=_cariHesapAramaTextBox!=null&&!_cariHesapAramaTextBox.IsDisposed
					? _cariHesapAramaTextBox
					: SatisRaporAramaKutusuOlustur(standartAramaKutusuBoyutu);

				int aramaKutusuDikeyBosluk = Math.Max(0 , (( 28+22 )-standartAramaKutusuBoyutu.Height)/2);
				FlowLayoutPanel aramaKutusuPanel = new FlowLayoutPanel
				{
					Dock=DockStyle.Fill,
					BackColor=aramaAlanArkaPlanRengi,
					Margin=Padding.Empty,
					Padding=new Padding(0 , aramaKutusuDikeyBosluk , 0 , 0),
					FlowDirection=FlowDirection.RightToLeft,
					WrapContents=false
				};

				AramaKutusuHazirla(_cariHesapAramaTextBox);
				_cariHesapAramaTextBox.BackColor=aramaAlanArkaPlanRengi;
				_cariHesapAramaTextBox.Dock=DockStyle.None;
				_cariHesapAramaTextBox.Anchor=AnchorStyles.Top|AnchorStyles.Right;
				_cariHesapAramaTextBox.Size=standartAramaKutusuBoyutu;
				_cariHesapAramaTextBox.MinimumSize=standartAramaKutusuBoyutu;
				_cariHesapAramaTextBox.MaximumSize=standartAramaKutusuBoyutu;
				_cariHesapAramaTextBox.Margin=Padding.Empty;

				aramaKutusuPanel.Controls.Add(_cariHesapAramaTextBox);
				ustLayout.Controls.Add(aramaKutusuPanel , 1 , 0);
				ustLayout.SetRowSpan(aramaKutusuPanel , 2);
			}

			Panel ayirici = new Panel
			{
				Dock=DockStyle.Top,
				Height=1,
				BackColor=Color.FromArgb(226 , 232 , 240)
			};

			ustPanel.Controls.Add(ustLayout);
			kart.Controls.Add(grid);
			kart.Controls.Add(ayirici);
			kart.Controls.Add(ustPanel);

			return kart;
		}

		private Label CariHesapAlanEtiketiOlustur ( string metin )
		{
			return new Label
			{
				Dock=DockStyle.Fill,
				Text=metin,
				Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold),
				ForeColor=Color.FromArgb(51 , 65 , 85),
				TextAlign=ContentAlignment.MiddleLeft,
				Margin=Padding.Empty
			};
		}

		private void CariHesapComboStiliUygula ( ComboBox comboBox )
		{
			if(comboBox==null)
				return;

			comboBox.DropDownStyle=ComboBoxStyle.DropDownList;
			comboBox.FlatStyle=FlatStyle.Flat;
			comboBox.Font=new Font("Segoe UI" , 10F , FontStyle.Regular);
			comboBox.Margin=Padding.Empty;
			comboBox.Size=new Size(260 , 30);
		}

		private void CariHesapDatePickerStiliUygula ( DateTimePicker picker )
		{
			if(picker==null)
				return;

			picker.Format=DateTimePickerFormat.Custom;
			picker.CustomFormat="dd.MM.yyyy HH:mm";
			picker.Font=new Font("Segoe UI" , 10F , FontStyle.Regular);
			picker.Margin=Padding.Empty;
			picker.Size=new Size(260 , 30);
			picker.Value=DateTime.Now;
		}

		private void CariHesapTextBoxStiliUygula ( TextBox textBox , bool readOnly , bool multiline )
		{
			if(textBox==null)
				return;

			HazirlaNotMetinKutusu(textBox , multiline);
			textBox.ReadOnly=readOnly;
			textBox.BackColor=readOnly ? Color.FromArgb(248 , 250 , 252) : Color.White;
			textBox.ForeColor=Color.FromArgb(15 , 23 , 42);
			textBox.Height=multiline ? 72 : 30;
			textBox.Margin=Padding.Empty;
		}

		private void CariHesapKompaktButonStiliUygula ( Button buton )
		{
			if(buton==null)
				return;

			buton.Font=new Font("Segoe UI" , 8.75F , FontStyle.Bold);
			buton.Padding=new Padding(10 , 0 , 12 , 0);
			buton.ImageAlign=ContentAlignment.MiddleLeft;
			buton.TextAlign=ContentAlignment.MiddleRight;
			buton.TextImageRelation=TextImageRelation.ImageBeforeText;
			buton.Margin=new Padding(0);
			buton.MinimumSize=new Size(0 , 40);
		}

		private void CariHesapVerileriniYenile ()
		{
			if(_cariHesapCariComboBox==null||_cariHesapCariComboBox.IsDisposed)
				return;

			int? hedefCariId = _cariHesapSeciliCariId;
			if(!hedefCariId.HasValue)
				hedefCariId=SeciliCariHesapIdGetir();

			CariHesapComboYenile(hedefCariId);
			CariHesapOzetListele(hedefCariId);

			int? seciliCariId = SeciliCariHesapIdGetir();
			if(seciliCariId.HasValue)
			{
				_cariHesapSeciliCariId=seciliCariId.Value;
				CariHesapOzetGridSecimiAyarla(seciliCariId.Value);
			}

			CariHesapAlaniniGuncelle();
			CariHesapHareketleriListele();
		}

		private void CariHesapComboYenile ( int? hedefCariId )
		{
			if(_cariHesapCariComboBox==null)
				return;

			DataTable dt = new DataTable();
			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbDataAdapter da = new OleDbDataAdapter("SELECT [CariID], IIF([adsoyad] IS NULL, '', [adsoyad]) AS AdSoyad FROM [Cariler] ORDER BY IIF([adsoyad] IS NULL, '', [adsoyad])" , conn))
						da.Fill(dt);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari secim listesi yuklenemedi: "+ex.Message);
			}

			_cariHesapSecimYukleniyor=true;
			try
			{
				_cariHesapCariComboBox.DataSource=null;
				_cariHesapCariComboBox.Items.Clear();
				_cariHesapCariComboBox.ValueMember="CariID";
				_cariHesapCariComboBox.DisplayMember="AdSoyad";
				_cariHesapCariComboBox.DataSource=dt;

				if(hedefCariId.HasValue&&dt.AsEnumerable().Any(x => Convert.ToInt32(x["CariID"])==hedefCariId.Value))
					_cariHesapCariComboBox.SelectedValue=hedefCariId.Value;
				else if(dt.Rows.Count>0)
					_cariHesapCariComboBox.SelectedIndex=0;
				else
					_cariHesapCariComboBox.SelectedIndex=-1;
			}
			finally
			{
				_cariHesapSecimYukleniyor=false;
			}
		}

		private int? SeciliCariHesapIdGetir ()
		{
			if(_cariHesapCariComboBox?.SelectedValue!=null&&_cariHesapCariComboBox.SelectedValue!=DBNull.Value)
			{
				int cariId;
				if(int.TryParse(_cariHesapCariComboBox.SelectedValue.ToString() , out cariId))
					return cariId;
			}

			if(_cariHesapSeciliCariId.HasValue)
				return _cariHesapSeciliCariId;

			return null;
		}

		private void CariHesapOzetListele ( int? hedefCariId )
		{
			if(_cariHesapOzetGrid==null)
				return;

			DataTable dt = new DataTable();
			dt.Columns.Add("CariID" , typeof(int));
			dt.Columns.Add("AdSoyad" , typeof(string));
			dt.Columns.Add("Telefon" , typeof(string));
			dt.Columns.Add("ToplamFatura" , typeof(decimal));
			dt.Columns.Add("ToplamTahsilat" , typeof(decimal));
			dt.Columns.Add("KalanTutar" , typeof(decimal));
			dt.Columns.Add("SonFaturaTarihi" , typeof(DateTime));

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					string arama = AramaKutusuMetniGetir(_cariHesapAramaTextBox);
					string sorgu = "SELECT [CariID], IIF([adsoyad] IS NULL, '', [adsoyad]) AS AdSoyad, IIF([telefon] IS NULL, '', [telefon]) AS Telefon FROM [Cariler]";
					if(!string.IsNullOrWhiteSpace(arama))
						sorgu+=" WHERE IIF([adsoyad] IS NULL, '', [adsoyad]) LIKE ? OR IIF([telefon] IS NULL, '', [telefon]) LIKE ? OR CSTR([CariID]) LIKE ?";
					sorgu+=" ORDER BY IIF([adsoyad] IS NULL, '', [adsoyad])";

					DataTable cariler = new DataTable();
					using(OleDbDataAdapter da = new OleDbDataAdapter(sorgu , conn))
					{
						if(!string.IsNullOrWhiteSpace(arama))
						{
							string filtre = "%"+arama+"%";
							da.SelectCommand.Parameters.AddWithValue("?" , filtre);
							da.SelectCommand.Parameters.AddWithValue("?" , filtre);
							da.SelectCommand.Parameters.AddWithValue("?" , filtre);
						}
						da.Fill(cariler);
					}

					Dictionary<int, decimal> faturaToplamlari = CariHesapFaturaToplamSozluguGetir(conn);
					Dictionary<int, decimal> tahsilatToplamlari = CariHesapTahsilatToplamSozluguGetir(conn);
					Dictionary<int, DateTime> sonFaturaTarihleri = CariHesapSonFaturaTarihSozluguGetir(conn);

					foreach(DataRow cariSatiri in cariler.Rows)
					{
						int cariId = Convert.ToInt32(cariSatiri["CariID"]);
						decimal toplamFatura = faturaToplamlari.ContainsKey(cariId) ? faturaToplamlari[cariId] : 0m;
						decimal toplamTahsilat = tahsilatToplamlari.ContainsKey(cariId) ? tahsilatToplamlari[cariId] : 0m;
						DateTime sonFaturaTarihi = sonFaturaTarihleri.ContainsKey(cariId) ? sonFaturaTarihleri[cariId] : DateTime.MinValue;

						DataRow yeniSatir = dt.NewRow();
						yeniSatir["CariID"]=cariId;
						yeniSatir["AdSoyad"]=Convert.ToString(cariSatiri["AdSoyad"])??string.Empty;
						yeniSatir["Telefon"]=Convert.ToString(cariSatiri["Telefon"])??string.Empty;
						yeniSatir["ToplamFatura"]=toplamFatura;
						yeniSatir["ToplamTahsilat"]=toplamTahsilat;
						yeniSatir["KalanTutar"]=toplamFatura-toplamTahsilat;
						if(sonFaturaTarihi!=DateTime.MinValue)
							yeniSatir["SonFaturaTarihi"]=sonFaturaTarihi;
						dt.Rows.Add(yeniSatir);
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap listesi yuklenemedi: "+ex.Message);
			}

			_cariHesapOzetGrid.DataSource=dt;
			if(_cariHesapOzetGrid.Columns.Contains("CariID"))
				_cariHesapOzetGrid.Columns["CariID"].Visible=false;
			if(_cariHesapOzetGrid.Columns.Contains("ToplamFatura"))
				_cariHesapOzetGrid.Columns["ToplamFatura"].DefaultCellStyle.Format="N2";
			if(_cariHesapOzetGrid.Columns.Contains("ToplamTahsilat"))
				_cariHesapOzetGrid.Columns["ToplamTahsilat"].DefaultCellStyle.Format="N2";
			if(_cariHesapOzetGrid.Columns.Contains("KalanTutar"))
				_cariHesapOzetGrid.Columns["KalanTutar"].DefaultCellStyle.Format="N2";
			if(_cariHesapOzetGrid.Columns.Contains("SonFaturaTarihi"))
				_cariHesapOzetGrid.Columns["SonFaturaTarihi"].DefaultCellStyle.Format="dd.MM.yyyy";
			GridBasliklariniTurkceDuzenle(_cariHesapOzetGrid);

			CariHesapOzetKartlariniGuncelle(dt);

			if(hedefCariId.HasValue)
				CariHesapOzetGridSecimiAyarla(hedefCariId.Value);
			else if(_cariHesapOzetGrid.Rows.Count>0)
				CariHesapOzetGridSecimiAyarla(Convert.ToInt32(_cariHesapOzetGrid.Rows[0].Cells["CariID"].Value));
		}

		private Dictionary<int, decimal> CariHesapFaturaToplamSozluguGetir ( OleDbConnection conn )
		{
			Dictionary<int, decimal> sonuc = new Dictionary<int, decimal>();
			if(conn==null||!_cariHesapFaturaTablosuVar)
				return sonuc;

			using(OleDbCommand cmd = new OleDbCommand("SELECT CLng(IIF([CariID] IS NULL, 0, [CariID])) AS CariID, SUM(IIF([ToplamTutar] IS NULL, 0, [ToplamTutar])) AS ToplamFatura FROM [Faturalar] GROUP BY CLng(IIF([CariID] IS NULL, 0, [CariID]))" , conn))
			using(OleDbDataReader rd = cmd.ExecuteReader())
			{
				while(rd!=null&&rd.Read())
				{
					int cariId = rd["CariID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["CariID"]);
					if(cariId<=0)
						continue;

					sonuc[cariId]=rd["ToplamFatura"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamFatura"]);
				}
			}

			return sonuc;
		}

		private Dictionary<int, decimal> CariHesapTahsilatToplamSozluguGetir ( OleDbConnection conn )
		{
			Dictionary<int, decimal> sonuc = new Dictionary<int, decimal>();
			if(conn==null||!_faturaTahsilatTablosuVar)
				return sonuc;

			string sorgu = @"SELECT CLng(IIF(T.[CariID] IS NULL, IIF(F.[CariID] IS NULL, 0, F.[CariID]), T.[CariID])) AS CariID,
								SUM(IIF(T.[AlinanTutar] IS NULL, 0, T.[AlinanTutar])) AS ToplamTahsilat
							FROM [FaturaTahsilatlari] AS T
							LEFT JOIN [Faturalar] AS F ON CLng(IIF(T.[FaturaID] IS NULL, 0, T.[FaturaID])) = F.[FaturaID]
							GROUP BY CLng(IIF(T.[CariID] IS NULL, IIF(F.[CariID] IS NULL, 0, F.[CariID]), T.[CariID]))";

			using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
			using(OleDbDataReader rd = cmd.ExecuteReader())
			{
				while(rd!=null&&rd.Read())
				{
					int cariId = rd["CariID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["CariID"]);
					if(cariId<=0)
						continue;

					sonuc[cariId]=rd["ToplamTahsilat"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTahsilat"]);
				}
			}

			return sonuc;
		}

		private Dictionary<int, DateTime> CariHesapSonFaturaTarihSozluguGetir ( OleDbConnection conn )
		{
			Dictionary<int, DateTime> sonuc = new Dictionary<int, DateTime>();
			if(conn==null||!_cariHesapFaturaTablosuVar)
				return sonuc;

			using(OleDbCommand cmd = new OleDbCommand("SELECT CLng(IIF([CariID] IS NULL, 0, [CariID])) AS CariID, MAX([FaturaTarihi]) AS SonFaturaTarihi FROM [Faturalar] GROUP BY CLng(IIF([CariID] IS NULL, 0, [CariID]))" , conn))
			using(OleDbDataReader rd = cmd.ExecuteReader())
			{
				while(rd!=null&&rd.Read())
				{
					int cariId = rd["CariID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["CariID"]);
					if(cariId<=0||rd["SonFaturaTarihi"]==DBNull.Value)
						continue;

					sonuc[cariId]=Convert.ToDateTime(rd["SonFaturaTarihi"]);
				}
			}

			return sonuc;
		}

		private void CariHesapOzetKartlariniGuncelle ( DataTable dt )
		{
			if(_cariHesapToplamCariDegerLabel==null||dt==null)
				return;

			decimal toplamFatura = 0m;
			decimal toplamTahsilat = 0m;
			decimal toplamKalan = 0m;

			foreach(DataRow satir in dt.Rows)
			{
				toplamFatura+=satir["ToplamFatura"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamFatura"]);
				toplamTahsilat+=satir["ToplamTahsilat"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["ToplamTahsilat"]);
				toplamKalan+=satir["KalanTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(satir["KalanTutar"]);
			}

			_cariHesapToplamCariDegerLabel.Text=dt.Rows.Count.ToString("N0" , _yazdirmaKulturu);
			_cariHesapToplamFaturaDegerLabel.Text=toplamFatura.ToString("N2" , _yazdirmaKulturu);
			_cariHesapToplamTahsilatDegerLabel.Text=toplamTahsilat.ToString("N2" , _yazdirmaKulturu);
			_cariHesapKalanDegerLabel.Text=toplamKalan.ToString("N2" , _yazdirmaKulturu);
		}

		private void CariHesapOzetGridSecimiAyarla ( int cariId )
		{
			if(_cariHesapOzetGrid==null||!_cariHesapOzetGrid.Columns.Contains("CariID"))
				return;

			_cariHesapOzetGrid.ClearSelection();
			foreach(DataGridViewRow row in _cariHesapOzetGrid.Rows)
			{
				if(row.IsNewRow)
					continue;

				if(row.Cells["CariID"].Value!=null&&row.Cells["CariID"].Value!=DBNull.Value&&Convert.ToInt32(row.Cells["CariID"].Value)==cariId)
				{
					row.Selected=true;
					DataGridViewCell ilkGorunurHucre = row.Cells.Cast<DataGridViewCell>().FirstOrDefault(x => x.Visible);
					if(ilkGorunurHucre!=null)
						_cariHesapOzetGrid.CurrentCell=ilkGorunurHucre;
					break;
				}
			}
		}

		private decimal CariHesapToplamFaturaGetir ( int cariId )
		{
			if(!_cariHesapFaturaTablosuVar||cariId<=0)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				using(OleDbCommand cmd = new OleDbCommand("SELECT SUM(IIF([ToplamTutar] IS NULL, 0, [ToplamTutar])) FROM [Faturalar] WHERE CLng(IIF([CariID] IS NULL, 0, [CariID]))=?" , conn))
				{
					cmd.Parameters.AddWithValue("?" , cariId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private decimal CariHesapToplamTahsilatGetir ( int cariId )
		{
			if(!_faturaTahsilatTablosuVar||cariId<=0)
				return 0m;

			using(OleDbConnection conn = new OleDbConnection(connStr))
			{
				conn.Open();
				string sorgu = @"SELECT SUM(IIF(T.[AlinanTutar] IS NULL, 0, T.[AlinanTutar]))
								FROM [FaturaTahsilatlari] AS T
								LEFT JOIN [Faturalar] AS F ON CLng(IIF(T.[FaturaID] IS NULL, 0, T.[FaturaID])) = F.[FaturaID]
								WHERE CLng(IIF(T.[CariID] IS NULL, IIF(F.[CariID] IS NULL, 0, F.[CariID]), T.[CariID]))=?";
				using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
				{
					cmd.Parameters.AddWithValue("?" , cariId);
					object sonuc = cmd.ExecuteScalar();
					return sonuc==null||sonuc==DBNull.Value ? 0m : Convert.ToDecimal(sonuc);
				}
			}
		}

		private void CariHesapAlaniniGuncelle ()
		{
			if(_cariHesapToplamFaturaTextBox==null||_cariHesapToplamTahsilatTextBox==null||_cariHesapKalanTextBox==null)
				return;

			decimal toplamFatura = 0m;
			decimal toplamTahsilat = 0m;
			int? cariId = SeciliCariHesapIdGetir();
			if(cariId.HasValue)
			{
				toplamFatura=CariHesapToplamFaturaGetir(cariId.Value);
				toplamTahsilat=CariHesapToplamTahsilatGetir(cariId.Value);
			}

			decimal yeniTahsilat = PersonelDecimalParse(_cariHesapYeniTahsilatTextBox?.Text);
			decimal duzenlemeFarki = _cariHesapSeciliTahsilatId.HasValue ? _cariHesapSeciliTahsilatTutari : 0m;
			_cariHesapToplamFaturaTextBox.Text=toplamFatura.ToString("N2" , _yazdirmaKulturu);
			_cariHesapToplamTahsilatTextBox.Text=toplamTahsilat.ToString("N2" , _yazdirmaKulturu);
			_cariHesapKalanTextBox.Text=( toplamFatura-toplamTahsilat+duzenlemeFarki-yeniTahsilat ).ToString("N2" , _yazdirmaKulturu);

			bool tutarGecerli = cariId.HasValue&&yeniTahsilat>0m;
			if(_cariHesapTahsilatKaydetButonu!=null)
				_cariHesapTahsilatKaydetButonu.Enabled=tutarGecerli&&!_cariHesapSeciliTahsilatId.HasValue;
			if(_cariHesapTahsilatGuncelleButonu!=null)
				_cariHesapTahsilatGuncelleButonu.Enabled=tutarGecerli&&_cariHesapSeciliTahsilatId.HasValue;
			if(_cariHesapTahsilatSilButonu!=null)
				_cariHesapTahsilatSilButonu.Enabled=_cariHesapSeciliTahsilatId.HasValue;
			if(_cariHesapYazdirButonu!=null)
				_cariHesapYazdirButonu.Enabled=cariId.HasValue;
			if(_cariHesapExcelButonu!=null)
				_cariHesapExcelButonu.Enabled=cariId.HasValue;
			if(_cariHesapPdfButonu!=null)
				_cariHesapPdfButonu.Enabled=cariId.HasValue;
		}

		private void CariHesapFormTemizle ( bool tarihiSifirla )
		{
			_cariHesapSeciliTahsilatId=null;
			_cariHesapSeciliTahsilatTutari=0m;
			if(_cariHesapYeniTahsilatTextBox!=null)
				_cariHesapYeniTahsilatTextBox.Clear();
			if(_cariHesapAciklamaTextBox!=null)
				_cariHesapAciklamaTextBox.Clear();
			if(tarihiSifirla&&_cariHesapTarihPicker!=null)
				_cariHesapTarihPicker.Value=DateTime.Now;
			if(_cariHesapHareketGrid!=null)
				_cariHesapHareketGrid.ClearSelection();

			CariHesapAlaniniGuncelle();
		}

		private DataTable CariHesapHareketTablosuOlustur ()
		{
			DataTable dt = new DataTable();
			dt.Columns.Add("IslemID" , typeof(int));
			dt.Columns.Add("CariID" , typeof(int));
			dt.Columns.Add("Kaynak" , typeof(string));
			dt.Columns.Add("IslemTuru" , typeof(string));
			dt.Columns.Add("BelgeNo" , typeof(string));
			dt.Columns.Add("Tarih" , typeof(DateTime));
			dt.Columns.Add("BorcTutar" , typeof(decimal));
			dt.Columns.Add("TahsilatTutar" , typeof(decimal));
			dt.Columns.Add("KalanTutar" , typeof(decimal));
			dt.Columns.Add("Aciklama" , typeof(string));
			return dt;
		}

		private void CariHesapHareketleriListele ()
		{
			if(_cariHesapHareketGrid==null)
				return;

			DataTable hareketler = CariHesapHareketTablosuOlustur();
			int? cariId = SeciliCariHesapIdGetir();
			if(!cariId.HasValue)
			{
				_cariHesapHareketGrid.DataSource=hareketler;
				GridBasliklariniTurkceDuzenle(_cariHesapHareketGrid);
				return;
			}

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();

					if(_cariHesapFaturaTablosuVar)
					{
						using(OleDbCommand cmd = new OleDbCommand("SELECT [FaturaID], IIF([FaturaNo] IS NULL, '', [FaturaNo]) AS BelgeNo, [FaturaTarihi], IIF([ToplamTutar] IS NULL, 0, [ToplamTutar]) AS ToplamTutar FROM [Faturalar] WHERE CLng(IIF([CariID] IS NULL, 0, [CariID]))=? ORDER BY [FaturaTarihi], [FaturaID]" , conn))
						{
							cmd.Parameters.AddWithValue("?" , cariId.Value);
							using(OleDbDataReader rd = cmd.ExecuteReader())
							{
								while(rd!=null&&rd.Read())
								{
									hareketler.Rows.Add(
										rd["FaturaID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["FaturaID"]) ,
										cariId.Value ,
										"Otomatik" ,
										"FATURA" ,
										Convert.ToString(rd["BelgeNo"])??string.Empty ,
										rd["FaturaTarihi"]==DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["FaturaTarihi"]) ,
										rd["ToplamTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["ToplamTutar"]) ,
										0m ,
										0m ,
										"Adina duzenlenen fatura");
								}
							}
						}
					}

					if(_faturaTahsilatTablosuVar)
					{
						string sorgu = @"SELECT T.[TahsilatID],
										T.[TahsilatTarihi],
										IIF(T.[AlinanTutar] IS NULL, 0, T.[AlinanTutar]) AS AlinanTutar,
										IIF(T.[Aciklama] IS NULL, '', T.[Aciklama]) AS Aciklama
									FROM [FaturaTahsilatlari] AS T
									LEFT JOIN [Faturalar] AS F ON CLng(IIF(T.[FaturaID] IS NULL, 0, T.[FaturaID])) = F.[FaturaID]
									WHERE CLng(IIF(T.[CariID] IS NULL, IIF(F.[CariID] IS NULL, 0, F.[CariID]), T.[CariID]))=?
									ORDER BY T.[TahsilatTarihi], T.[TahsilatID]";
						using(OleDbCommand cmd = new OleDbCommand(sorgu , conn))
						{
							cmd.Parameters.AddWithValue("?" , cariId.Value);
							using(OleDbDataReader rd = cmd.ExecuteReader())
							{
								while(rd!=null&&rd.Read())
								{
									hareketler.Rows.Add(
										rd["TahsilatID"]==DBNull.Value ? 0 : Convert.ToInt32(rd["TahsilatID"]) ,
										cariId.Value ,
										"Manuel" ,
										"TAHSILAT" ,
										string.Empty ,
										rd["TahsilatTarihi"]==DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["TahsilatTarihi"]) ,
										0m ,
										rd["AlinanTutar"]==DBNull.Value ? 0m : Convert.ToDecimal(rd["AlinanTutar"]) ,
										0m ,
										Convert.ToString(rd["Aciklama"])??string.Empty);
								}
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Cari hesap hareketleri yuklenemedi: "+ex.Message);
			}

			DataTable sirali = hareketler.Clone();
			decimal kalan = 0m;
			foreach(DataRow satir in hareketler.AsEnumerable()
				.OrderBy(x => x.Field<DateTime>("Tarih"))
				.ThenBy(x => string.Equals(x.Field<string>("IslemTuru") , "FATURA" , StringComparison.OrdinalIgnoreCase) ? 0 : 1)
				.ThenBy(x => x.Field<int>("IslemID")))
			{
				decimal borc = satir.Field<decimal>("BorcTutar");
				decimal tahsilat = satir.Field<decimal>("TahsilatTutar");
				kalan+=borc-tahsilat;

				DataRow yeniSatir = sirali.NewRow();
				yeniSatir["IslemID"]=satir["IslemID"];
				yeniSatir["CariID"]=satir["CariID"];
				yeniSatir["Kaynak"]=satir["Kaynak"];
				yeniSatir["IslemTuru"]=satir["IslemTuru"];
				yeniSatir["BelgeNo"]=satir["BelgeNo"];
				yeniSatir["Tarih"]=satir["Tarih"];
				yeniSatir["BorcTutar"]=satir["BorcTutar"];
				yeniSatir["TahsilatTutar"]=satir["TahsilatTutar"];
				yeniSatir["KalanTutar"]=kalan;
				yeniSatir["Aciklama"]=satir["Aciklama"];
				sirali.Rows.Add(yeniSatir);
			}

			_cariHesapHareketGrid.DataSource=sirali;
			if(_cariHesapHareketGrid.Columns.Contains("IslemID"))
				_cariHesapHareketGrid.Columns["IslemID"].Visible=false;
			if(_cariHesapHareketGrid.Columns.Contains("CariID"))
				_cariHesapHareketGrid.Columns["CariID"].Visible=false;
			if(_cariHesapHareketGrid.Columns.Contains("Tarih"))
				_cariHesapHareketGrid.Columns["Tarih"].DefaultCellStyle.Format="g";
			if(_cariHesapHareketGrid.Columns.Contains("BorcTutar"))
				_cariHesapHareketGrid.Columns["BorcTutar"].DefaultCellStyle.Format="N2";
			if(_cariHesapHareketGrid.Columns.Contains("TahsilatTutar"))
				_cariHesapHareketGrid.Columns["TahsilatTutar"].DefaultCellStyle.Format="N2";
			if(_cariHesapHareketGrid.Columns.Contains("KalanTutar"))
				_cariHesapHareketGrid.Columns["KalanTutar"].DefaultCellStyle.Format="N2";
			GridBasliklariniTurkceDuzenle(_cariHesapHareketGrid);
		}

		private void CariHesapCariComboBox_SelectedIndexChanged ( object sender , EventArgs e )
		{
			if(_cariHesapSecimYukleniyor)
				return;

			int? cariId = SeciliCariHesapIdGetir();
			_cariHesapSeciliCariId=cariId;
			if(cariId.HasValue)
				CariHesapOzetGridSecimiAyarla(cariId.Value);

			CariHesapFormTemizle(true);
			CariHesapAlaniniGuncelle();
			CariHesapHareketleriListele();
		}

		private void CariHesapOzetGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(_cariHesapOzetGrid==null||e.RowIndex<0||e.RowIndex>=_cariHesapOzetGrid.Rows.Count)
				return;

			DataGridViewRow row = _cariHesapOzetGrid.Rows[e.RowIndex];
			if(!_cariHesapOzetGrid.Columns.Contains("CariID")||row.Cells["CariID"].Value==null||row.Cells["CariID"].Value==DBNull.Value)
				return;

			int cariId = Convert.ToInt32(row.Cells["CariID"].Value);
			_cariHesapSeciliCariId=cariId;

			_cariHesapSecimYukleniyor=true;
			try
			{
				if(_cariHesapCariComboBox!=null)
					_cariHesapCariComboBox.SelectedValue=cariId;
			}
			finally
			{
				_cariHesapSecimYukleniyor=false;
			}

			CariHesapFormTemizle(true);
			CariHesapAlaniniGuncelle();
			CariHesapHareketleriListele();
		}

		private void CariHesapHareketGrid_CellClick ( object sender , DataGridViewCellEventArgs e )
		{
			if(_cariHesapHareketGrid==null||e.RowIndex<0||e.RowIndex>=_cariHesapHareketGrid.Rows.Count)
				return;

			DataGridViewRow row = _cariHesapHareketGrid.Rows[e.RowIndex];
			string kaynak = Convert.ToString(row.Cells["Kaynak"]?.Value)??string.Empty;
			string islemTuru = Convert.ToString(row.Cells["IslemTuru"]?.Value)??string.Empty;
			bool manuelTahsilat = string.Equals(kaynak , "Manuel" , StringComparison.OrdinalIgnoreCase)
				&&string.Equals(islemTuru , "TAHSILAT" , StringComparison.OrdinalIgnoreCase);

			if(!manuelTahsilat
				||!_cariHesapHareketGrid.Columns.Contains("IslemID")
				||row.Cells["IslemID"].Value==null
				||row.Cells["IslemID"].Value==DBNull.Value)
			{
				_cariHesapSeciliTahsilatId=null;
				_cariHesapSeciliTahsilatTutari=0m;
				if(_cariHesapYeniTahsilatTextBox!=null)
					_cariHesapYeniTahsilatTextBox.Clear();
				if(_cariHesapAciklamaTextBox!=null)
					_cariHesapAciklamaTextBox.Clear();
				if(_cariHesapTarihPicker!=null)
					_cariHesapTarihPicker.Value=DateTime.Now;
				CariHesapAlaniniGuncelle();
				return;
			}

			_cariHesapSeciliTahsilatId=Convert.ToInt32(row.Cells["IslemID"].Value);
			_cariHesapSeciliTahsilatTutari=PersonelDecimalParse(Convert.ToString(row.Cells["TahsilatTutar"]?.Value));

			if(_cariHesapYeniTahsilatTextBox!=null)
				_cariHesapYeniTahsilatTextBox.Text=_cariHesapSeciliTahsilatTutari.ToString("N2" , _yazdirmaKulturu);
			if(_cariHesapAciklamaTextBox!=null)
				_cariHesapAciklamaTextBox.Text=Convert.ToString(row.Cells["Aciklama"]?.Value)??string.Empty;
			if(_cariHesapTarihPicker!=null
				&&_cariHesapHareketGrid.Columns.Contains("Tarih")
				&&row.Cells["Tarih"].Value!=null
				&&row.Cells["Tarih"].Value!=DBNull.Value)
			{
				DateTime tarih = Convert.ToDateTime(row.Cells["Tarih"].Value);
				if(tarih<_cariHesapTarihPicker.MinDate||tarih>_cariHesapTarihPicker.MaxDate)
					tarih=DateTime.Now;
				_cariHesapTarihPicker.Value=tarih;
			}

			CariHesapAlaniniGuncelle();
		}

		private void CariHesapAramaTextBox_TextChanged ( object sender , EventArgs e )
		{
			if(_cariHesapSecimYukleniyor)
				return;

			CariHesapOzetListele(_cariHesapSeciliCariId);
		}

		private void CariHesapYeniTahsilatTextBox_TextChanged ( object sender , EventArgs e )
		{
			CariHesapAlaniniGuncelle();
		}

		private void CariHesapTahsilatKaydetButonu_Click ( object sender , EventArgs e )
		{
			int? cariId = SeciliCariHesapIdGetir();
			if(!cariId.HasValue)
			{
				MessageBox.Show("Once cari secin!");
				return;
			}

			decimal tahsilatTutari = PersonelDecimalParse(_cariHesapYeniTahsilatTextBox?.Text);
			if(tahsilatTutari<=0m)
			{
				MessageBox.Show("Alinan tahsilat tutarini girin!");
				return;
			}

			DateTime tarih = _cariHesapTarihPicker==null ? DateTime.Now : _cariHesapTarihPicker.Value;
			string aciklama = _cariHesapAciklamaTextBox?.Text?.Trim()??string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("INSERT INTO [FaturaTahsilatlari] ([CariID], [TahsilatTarihi], [AlinanTutar], [Aciklama]) VALUES (?, ?, ?, ?)" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariId.Value;
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=tahsilatTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.ExecuteNonQuery();
					}
				}

				MessageBox.Show("Tahsilat kaydedildi.");
				CariHesapFormTemizle(true);
				CariHesapVerileriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Tahsilat kaydedilemedi: "+ex.Message);
			}
		}

		private void CariHesapTahsilatGuncelleButonu_Click ( object sender , EventArgs e )
		{
			if(!_cariHesapSeciliTahsilatId.HasValue)
			{
				MessageBox.Show("Guncellenecek manuel tahsilati secin!");
				return;
			}

			int? cariId = SeciliCariHesapIdGetir();
			if(!cariId.HasValue)
			{
				MessageBox.Show("Once cari secin!");
				return;
			}

			decimal tahsilatTutari = PersonelDecimalParse(_cariHesapYeniTahsilatTextBox?.Text);
			if(tahsilatTutari<=0m)
			{
				MessageBox.Show("Alinan tahsilat tutarini girin!");
				return;
			}

			DateTime tarih = _cariHesapTarihPicker==null ? DateTime.Now : _cariHesapTarihPicker.Value;
			string aciklama = _cariHesapAciklamaTextBox?.Text?.Trim()??string.Empty;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("UPDATE [FaturaTahsilatlari] SET [CariID]=?, [TahsilatTarihi]=?, [AlinanTutar]=?, [Aciklama]=? WHERE [TahsilatID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=cariId.Value;
						cmd.Parameters.Add("?" , OleDbType.Date).Value=tarih;
						cmd.Parameters.Add("?" , OleDbType.Currency).Value=tahsilatTutari;
						cmd.Parameters.Add("?" , OleDbType.LongVarWChar).Value=string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama;
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_cariHesapSeciliTahsilatId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				MessageBox.Show("Tahsilat guncellendi.");
				CariHesapFormTemizle(true);
				CariHesapVerileriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Tahsilat guncellenemedi: "+ex.Message);
			}
		}

		private void CariHesapTahsilatSilButonu_Click ( object sender , EventArgs e )
		{
			if(!_cariHesapSeciliTahsilatId.HasValue)
			{
				MessageBox.Show("Silinecek manuel tahsilati secin!");
				return;
			}

			DialogResult onay = MessageBox.Show(
				"Secili manuel tahsilat kaydini silmek istiyor musunuz?" ,
				"Tahsilat Sil" ,
				MessageBoxButtons.YesNo ,
				MessageBoxIcon.Question);
			if(onay!=DialogResult.Yes)
				return;

			try
			{
				using(OleDbConnection conn = new OleDbConnection(connStr))
				{
					conn.Open();
					using(OleDbCommand cmd = new OleDbCommand("DELETE FROM [FaturaTahsilatlari] WHERE [TahsilatID]=?" , conn))
					{
						cmd.Parameters.Add("?" , OleDbType.Integer).Value=_cariHesapSeciliTahsilatId.Value;
						cmd.ExecuteNonQuery();
					}
				}

				MessageBox.Show("Tahsilat silindi.");
				CariHesapFormTemizle(true);
				CariHesapVerileriniYenile();
			}
			catch(Exception ex)
			{
				MessageBox.Show("Tahsilat silinemedi: "+ex.Message);
			}
		}

		private void CariHesapTemizleButonu_Click ( object sender , EventArgs e )
		{
			CariHesapFormTemizle(true);
		}
	}
}
