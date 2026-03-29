using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace TEKNİK_SERVİS
{
	internal sealed class LoginForm : Form
	{
		private readonly GradientPanel _heroPanel;
		private readonly Panel _loginPanel;
		private readonly TextBox _kullaniciAdiTextBox;
		private readonly TextBox _sifreTextBox;
		private readonly Label _durumLabel;
		private readonly Button _girisButonu;
		private readonly Button _iptalButonu;

		internal LoginForm ()
		{
			Text="ASLAN SIHHİ TESİSAT";
			StartPosition=FormStartPosition.CenterScreen;
			FormBorderStyle=FormBorderStyle.FixedSingle;
			MaximizeBox=false;
			MinimizeBox=false;
			ShowIcon=false;
			BackColor=Color.FromArgb(232 , 238 , 245);
			ClientSize=new Size(1040 , 620);
			Font=new Font("Segoe UI" , 9.5F , FontStyle.Regular);

			TableLayoutPanel rootLayout = new TableLayoutPanel
			{
				Dock=DockStyle.Fill,
				Padding=new Padding(24),
				ColumnCount=2,
				RowCount=1,
				BackColor=BackColor
			};
			rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute , 390F));
			rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent , 100F));

			_heroPanel=new GradientPanel
			{
				Dock=DockStyle.Fill,
				Margin=Padding.Empty,
				Padding=new Padding(30),
				StartColor=Color.FromArgb(9 , 45 , 87),
				EndColor=Color.FromArgb(18 , 108 , 145),
				Angle=90F
			};

			_loginPanel=new Panel
			{
				Dock=DockStyle.Fill,
				Margin=new Padding(24 , 0 , 0 , 0),
				BackColor=Color.White,
				Padding=new Padding(44 , 36 , 44 , 30)
			};

			rootLayout.Controls.Add(_heroPanel , 0 , 0);
			rootLayout.Controls.Add(_loginPanel , 1 , 0);
			Controls.Add(rootLayout);

			_kullaniciAdiTextBox=new TextBox
			{
				BorderStyle=BorderStyle.FixedSingle,
				Font=new Font("Segoe UI" , 12F , FontStyle.Regular),
				Width=320
			};

			_sifreTextBox=new TextBox
			{
				BorderStyle=BorderStyle.FixedSingle,
				Font=new Font("Segoe UI" , 12F , FontStyle.Regular),
				UseSystemPasswordChar=true,
				Width=320
			};

			_durumLabel=new Label
			{
				AutoSize=false,
				Dock=DockStyle.Top,
				Height=24,
				ForeColor=Color.FromArgb(185 , 28 , 28),
				Font=new Font("Segoe UI" , 9.25F , FontStyle.Bold),
				TextAlign=ContentAlignment.MiddleLeft
			};

			_girisButonu=new Button
			{
				Text="GİRİŞ YAP",
				BackColor=Color.FromArgb(15 , 118 , 110),
				ForeColor=Color.White,
				FlatStyle=FlatStyle.Flat,
				Font=new Font("Segoe UI" , 10.5F , FontStyle.Bold),
				Width=180,
				Height=46,
				Cursor=Cursors.Hand
			};
			_girisButonu.FlatAppearance.BorderSize=0;
			_girisButonu.Click+=GirisButonu_Click;

			_iptalButonu=new Button
			{
				Text="KAPAT",
				BackColor=Color.FromArgb(241 , 245 , 249),
				ForeColor=Color.FromArgb(15 , 23 , 42),
				FlatStyle=FlatStyle.Flat,
				Font=new Font("Segoe UI" , 10.5F , FontStyle.Bold),
				Width=140,
				Height=46,
				Cursor=Cursors.Hand,
				DialogResult=DialogResult.Cancel
			};
			_iptalButonu.FlatAppearance.BorderColor=Color.FromArgb(203 , 213 , 225);
			_iptalButonu.FlatAppearance.BorderSize=1;

			BuildHeroPanel();
			BuildLoginPanel();
			PopulateLoginPanel();

			AcceptButton=_girisButonu;
			CancelButton=_iptalButonu;

			_kullaniciAdiTextBox.TextChanged+=GirdiAlanlari_TextChanged;
			_sifreTextBox.TextChanged+=GirdiAlanlari_TextChanged;
			Resize+=LoginForm_Resize;
			Shown+=LoginForm_Shown;
		}

		internal AppUserSession AuthenticatedUser { get; private set; }

		private void BuildHeroPanel ()
		{
			_heroPanel.Controls.Clear();

			PictureBox logoPictureBox = new PictureBox
			{
				Size=new Size(334 , 260),
				Location=new Point(20 , 116),
				BackColor=Color.Transparent,
				SizeMode=PictureBoxSizeMode.Zoom,
				Image=LogoResmiYukle()
			};

			Label titleLabel = new Label
			{
				Text="SIHH\u0130 TES\u0130SAT" + Environment.NewLine + "TEKN\u0130K FORMU",
				ForeColor=Color.White,
				Font=new Font("Segoe UI" , 20F , FontStyle.Bold),
				AutoSize=false,
				BackColor=Color.Transparent,
				Location=new Point(36 , 404),
				Size=new Size(292 , 84),
				TextAlign=ContentAlignment.TopCenter
			};

			_heroPanel.Controls.Add(logoPictureBox);
			_heroPanel.Controls.Add(titleLabel);
		}

		private void BuildLoginPanel ()
		{
			Label headingLabel = new Label
			{
				Text="GİRİŞ",
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Font=new Font("Segoe UI" , 24F , FontStyle.Bold),
				AutoSize=true,
				Location=new Point(42 , 58)
			};

			_loginPanel.Controls.Add(headingLabel);
		}

		private void PopulateLoginPanel ()
		{
			Label kullaniciAdiLabel = InputLabelOlustur("KULLANICI ADI" , new Point(42 , 158));
			_kullaniciAdiTextBox.Location=new Point(42 , 184);

			Label sifreLabel = InputLabelOlustur("ŞİFRE" , new Point(42 , 252));
			_sifreTextBox.Location=new Point(42 , 278);

			_durumLabel.Location=new Point(42 , 334);
			_durumLabel.Width=428;

			FlowLayoutPanel butonPanel = new FlowLayoutPanel
			{
				Location=new Point(42 , 382),
				Size=new Size(428 , 60),
				FlowDirection=FlowDirection.LeftToRight,
				WrapContents=false,
				BackColor=Color.Transparent
			};
			butonPanel.Controls.Add(_girisButonu);
			butonPanel.Controls.Add(_iptalButonu);

			_loginPanel.Controls.Add(kullaniciAdiLabel);
			_loginPanel.Controls.Add(_kullaniciAdiTextBox);
			_loginPanel.Controls.Add(sifreLabel);
			_loginPanel.Controls.Add(_sifreTextBox);
			_loginPanel.Controls.Add(_durumLabel);
			_loginPanel.Controls.Add(butonPanel);
		}

		private Label InputLabelOlustur ( string metin , Point konum )
		{
			return new Label
			{
				Text=metin,
				ForeColor=Color.FromArgb(15 , 23 , 42),
				Font=new Font("Segoe UI" , 9.5F , FontStyle.Bold),
				AutoSize=true,
				Location=konum
			};
		}

		private void GirisButonu_Click ( object sender , EventArgs e )
		{
			LoginAttempt();
		}

		private void GirdiAlanlari_TextChanged ( object sender , EventArgs e )
		{
			_durumLabel.Text=string.Empty;
		}

		private Image LogoResmiYukle ()
		{
			const string logoYolu = @"C:\Users\Neriman\Pictures\trasnparan buton\logo-Photoroom (1).png";

			return GorselYukle(logoYolu);
		}

		private Image GorselYukle ( string dosyaYolu )
		{
			try
			{
				if(string.IsNullOrWhiteSpace(dosyaYolu)||!File.Exists(dosyaYolu))
					return null;

				using(FileStream stream = new FileStream(dosyaYolu , FileMode.Open , FileAccess.Read))
				using(Image image = Image.FromStream(stream))
					return new Bitmap(image);
			}
			catch
			{
				return null;
			}
		}

		private void LoginAttempt ()
		{
			AppUserSession hesap = AppAuthentication.KullaniciDogrula(_kullaniciAdiTextBox.Text , _sifreTextBox.Text);
			if(hesap==null)
			{
				AuthenticatedUser=null;
				_durumLabel.Text="Kullanıcı adı veya şifre hatalı.";
				_sifreTextBox.SelectAll();
				_sifreTextBox.Focus();
				return;
			}

			AuthenticatedUser=hesap;
			DialogResult=DialogResult.OK;
			Close();
		}

		private void LoginForm_Shown ( object sender , EventArgs e )
		{
			ApplyRoundedAppearance();
			_kullaniciAdiTextBox.Focus();
		}

		private void LoginForm_Resize ( object sender , EventArgs e )
		{
			ApplyRoundedAppearance();
		}

		private void ApplyRoundedAppearance ()
		{
			RoundedRegionUygula(_heroPanel , 30);
			RoundedRegionUygula(_loginPanel , 30);
			RoundedRegionUygula(_girisButonu , 16);
			RoundedRegionUygula(_iptalButonu , 16);
		}

		private void RoundedRegionUygula ( Control control , int radius )
		{
			if(control==null||control.Width<=0||control.Height<=0)
				return;

			using(GraphicsPath path = RoundedPathOlustur(new Rectangle(0 , 0 , control.Width , control.Height) , radius))
				control.Region=new Region(path);
		}

		private GraphicsPath RoundedPathOlustur ( Rectangle bounds , int radius )
		{
			GraphicsPath path = new GraphicsPath();
			int diameter = Math.Max(2 , radius*2);
			Rectangle arc = new Rectangle(bounds.Location , new Size(diameter , diameter));

			path.AddArc(arc , 180 , 90);
			arc.X=bounds.Right-diameter;
			path.AddArc(arc , 270 , 90);
			arc.Y=bounds.Bottom-diameter;
			path.AddArc(arc , 0 , 90);
			arc.X=bounds.Left;
			path.AddArc(arc , 90 , 90);
			path.CloseFigure();
			return path;
		}

		private sealed class GradientPanel : Panel
		{
			public GradientPanel ()
			{
				DoubleBuffered=true;
				StartColor=Color.FromArgb(15 , 23 , 42);
				EndColor=Color.FromArgb(15 , 118 , 110);
				Angle=120F;
			}

			public Color StartColor { get; set; }
			public Color EndColor { get; set; }
			public float Angle { get; set; }

			protected override void OnPaintBackground ( PaintEventArgs e )
			{
				using(LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle , StartColor , EndColor , Angle))
					e.Graphics.FillRectangle(brush , ClientRectangle);
			}
		}
	}
}
