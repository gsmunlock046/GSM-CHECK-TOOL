using GSMCheckTool.Services;
using System.Drawing.Drawing2D;
using System.Text;

namespace GSMCheckTool;

public sealed class MainForm : Form
{
    static readonly Color Bg=Color.FromArgb(6,10,20), CardBg=Color.FromArgb(12,21,37), Card2=Color.FromArgb(16,31,52), RowAlt=Color.FromArgb(14,27,46), Line=Color.FromArgb(35,58,86), Blue=Color.FromArgb(0,132,255), Cyan=Color.FromArgb(0,213,255), White=Color.FromArgb(239,246,255), Muted=Color.FromArgb(143,164,190), Green=Color.FromArgb(0,213,126);
    readonly ComboBox devices=new(); readonly DataGridView grid=new(); readonly Label status=new(); readonly DevicePreview preview=new(); readonly Label modelTitle=new(), platformTag=new(), detailCount=new(); Panel storageCard=new(); Label storageValue=new(); readonly System.Windows.Forms.Timer autoDetectTimer=new(){Interval=3000};
    Button scan=new(),read=new(),export=new(),clear=new(); DeviceInfo? current; bool busy;
    public MainForm()
    {
        Text="GSM CHECK TOOL"; StartPosition=FormStartPosition.CenterScreen; MinimumSize=new Size(1000,680); Size=new Size(1370,850);
        BackColor=Bg; ForeColor=White; Font=new Font("Segoe UI",9.5f); DoubleBuffered=true; BuildUI(); Shown+=async(_,_)=>{await ScanAsync(true);autoDetectTimer.Start();}; autoDetectTimer.Tick+=async(_,_)=>await AutoDetectAsync(); FormClosed+=(_,_)=>autoDetectTimer.Dispose();
    }
    void BuildUI()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(18,14,18,8),BackColor=Bg};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,86)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,62)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,30)); Controls.Add(root);
        var header=new Panel{Dock=DockStyle.Fill,BackColor=Bg}; root.Controls.Add(header,0,0);
        header.Controls.Add(new BrandMark{Location=new Point(0,3),Size=new Size(66,66)});
        header.Controls.Add(new Label{Text="GSM CHECK TOOL",Font=new Font("Segoe UI",24,FontStyle.Bold),ForeColor=White,AutoSize=true,Location=new Point(80,1)});
        header.Controls.Add(new Label{Text="DIAGNÓSTICO INTELIGENTE DE DISPOSITIVOS",Font=new Font("Segoe UI",9,FontStyle.Bold),ForeColor=Color.FromArgb(105,190,255),AutoSize=true,Location=new Point(83,43)});
        var version=new Label{Text="ANDROID  •  IPHONE",AutoSize=true,ForeColor=Cyan,BackColor=Card2,Padding=new Padding(14,9,14,9),Anchor=AnchorStyles.Top|AnchorStyles.Right};header.Controls.Add(version);header.Resize+=(_,_)=>version.Location=new Point(header.ClientSize.Width-version.Width-4,12);
        header.Paint+=(_,e)=>{using var p=new Pen(Line);e.Graphics.DrawLine(p,0,header.Height-2,header.Width,header.Height-2);};
        var toolbar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,BackColor=Bg};toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,174));toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,174));toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));root.Controls.Add(toolbar,0,1);
        devices.Dock=DockStyle.Fill;devices.Margin=new Padding(0,8,10,8);devices.DropDownStyle=ComboBoxStyle.DropDownList;devices.BackColor=Card2;devices.ForeColor=White;devices.FlatStyle=FlatStyle.Flat;toolbar.Controls.Add(devices,0,0);
        scan=MakeButton("↻  BUSCAR",Blue);scan.Margin=new Padding(0,6,8,6);scan.Dock=DockStyle.Fill;scan.Click+=async(_,_)=>await ScanAsync();toolbar.Controls.Add(scan,1,0);
        read=MakeButton("⌕  LER DADOS",Green);read.Margin=new Padding(0,6,8,6);read.Dock=DockStyle.Fill;read.Click+=async(_,_)=>await ReadAsync();toolbar.Controls.Add(read,2,0);
        clear=MakeButton("LIMPAR",Card2);clear.Margin=new Padding(0,6,8,6);clear.Dock=DockStyle.Fill;clear.Click+=(_,_)=>ClearReport();toolbar.Controls.Add(clear,3,0);
        export=MakeButton("⇩  EXPORTAR",Card2);export.Margin=new Padding(0,6,0,6);export.Dock=DockStyle.Fill;export.Click+=(_,_)=>ExportReport();toolbar.Controls.Add(export,4,0);
        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Bg};content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(content,0,2);
        var left=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Bg,Padding=new Padding(0,0,12,0)};left.RowStyles.Add(new RowStyle(SizeType.Percent,64));left.RowStyles.Add(new RowStyle(SizeType.Percent,36));content.Controls.Add(left,0,0);
        var device=Card("DISPOSITIVO");left.Controls.Add(device,0,0);preview.Dock=DockStyle.Fill;device.Controls.Add(preview);
        var foot=new Panel{Dock=DockStyle.Bottom,Height=76,BackColor=CardBg};device.Controls.Add(foot);
        modelTitle.Text="Nenhum aparelho conectado";modelTitle.Font=new Font("Segoe UI",13,FontStyle.Bold);modelTitle.ForeColor=White;modelTitle.TextAlign=ContentAlignment.MiddleCenter;modelTitle.Dock=DockStyle.Top;modelTitle.Height=36;
        platformTag.Text="Conecte por USB";platformTag.Font=new Font("Segoe UI",9);platformTag.ForeColor=Cyan;platformTag.TextAlign=ContentAlignment.TopCenter;platformTag.Dock=DockStyle.Fill;foot.Controls.Add(platformTag);foot.Controls.Add(modelTitle);
        storageCard=Card("RESUMO DA LEITURA");left.Controls.Add(storageCard,0,1);
        storageValue=new Label{Text="Aguardando leitura",Dock=DockStyle.Fill,ForeColor=Muted,Padding=new Padding(12,8,8,8),TextAlign=ContentAlignment.MiddleLeft};storageCard.Controls.Add(storageValue);
        var report=Card("INFORMAÇÕES ENCONTRADAS");content.Controls.Add(report,1,0);
        var reportHeader=new Panel{Dock=DockStyle.Top,Height=36,BackColor=CardBg};report.Controls.Add(reportHeader);
        detailCount.Text="0 campos";detailCount.Dock=DockStyle.Right;detailCount.Width=120;detailCount.ForeColor=Muted;detailCount.TextAlign=ContentAlignment.MiddleRight;reportHeader.Controls.Add(detailCount);
        reportHeader.Controls.Add(new Label{Text="Somente dados retornados pelo aparelho",Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleLeft});
        grid.Dock=DockStyle.Fill;grid.BackgroundColor=CardBg;grid.BorderStyle=BorderStyle.None;grid.GridColor=Line;grid.EnableHeadersVisualStyles=false;grid.ColumnCount=2;
        grid.Columns[0].Name="INFORMAÇÃO";grid.Columns[0].Width=220;grid.Columns[1].Name="VALOR";grid.Columns[1].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.RowHeadersVisible=false;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.ReadOnly=true;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;
        grid.DefaultCellStyle.BackColor=CardBg;grid.DefaultCellStyle.ForeColor=White;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(21,67,111);grid.DefaultCellStyle.SelectionForeColor=White;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.DefaultCellStyle.Padding=new Padding(9,6,9,6);grid.AlternatingRowsDefaultCellStyle.BackColor=RowAlt;grid.AlternatingRowsDefaultCellStyle.ForeColor=White;grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor=Card2;grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.FromArgb(152,212,255);grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);grid.ColumnHeadersHeight=38;grid.RowTemplate.Height=38;report.Controls.Add(grid);
        status.Text="●  Pronto — conecte o aparelho, autorize o acesso e clique em Buscar.";status.Dock=DockStyle.Fill;status.Height=30;status.ForeColor=Muted;status.TextAlign=ContentAlignment.MiddleLeft;root.Controls.Add(status,0,3);
    }
    static Panel Card(string title)
    {
        var p=new ModernCard{Dock=DockStyle.Fill,BackColor=CardBg,Margin=new Padding(0,0,0,10),Padding=new Padding(12)};
        p.Controls.Add(new Label{Text=title,Dock=DockStyle.Top,Height=30,ForeColor=Color.FromArgb(142,207,255),Font=new Font("Segoe UI",9,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});
        return p;
    }
    static Button MakeButton(string text,Color color){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=color,ForeColor=White,Font=new Font("Segoe UI",9,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Line;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(Math.Min(255,color.R+18),Math.Min(255,color.G+18),Math.Min(255,color.B+18));b.FlatAppearance.MouseDownBackColor=Color.FromArgb(Math.Max(0,color.R-18),Math.Max(0,color.G-18),Math.Max(0,color.B-18));return b;}
    async Task ScanAsync(bool autoRead=false)
    {
        if(busy)return;busy=true;scan.Enabled=false;status.Text="●  Procurando dispositivos USB...";
        try
        {
            var found=await DeviceReader.DetectAsync();var previous=(devices.SelectedItem as DeviceChoice)?.Info.Id;devices.BeginUpdate();devices.Items.Clear();foreach(var d in found)devices.Items.Add(new DeviceChoice(d));devices.EndUpdate();
            if(found.Count>0){var index=found.FindIndex(d=>d.Id==previous);devices.SelectedIndex=index>=0?index:0;status.Text=$"●  {found.Count} aparelho(s) reconhecido(s) automaticamente.";} 
            else status.Text="●  Nenhum aparelho autorizado. Verifique o cabo, confirme “Confiar neste computador” no iPhone ou autorize a Depuração USB no Android.";
        }
        catch(Exception ex){status.Text="Falha ao procurar aparelhos: "+ex.Message;}
        finally{scan.Enabled=true;busy=false;}
        if(autoRead && devices.Items.Count>0) await ReadAsync();
    }
    async Task AutoDetectAsync()
    {
        if(busy || IsDisposed || !IsHandleCreated)return;
        try
        {
            var found=await DeviceReader.DetectAsync();
            var existing=devices.Items.Cast<object>().OfType<DeviceChoice>().Select(x=>x.Info.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ids=found.Select(x=>x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if(existing.SetEquals(ids))return;
            var selected=(devices.SelectedItem as DeviceChoice)?.Info.Id;
            devices.BeginUpdate();devices.Items.Clear();foreach(var d in found)devices.Items.Add(new DeviceChoice(d));devices.EndUpdate();
            var index=found.FindIndex(x=>x.Id.Equals(selected,StringComparison.OrdinalIgnoreCase));
            bool newlyFound=found.Any(x=>!existing.Contains(x.Id));
            if(found.Count>0)devices.SelectedIndex=index>=0?index:0;
            if(newlyFound){status.Text="●  Novo aparelho reconhecido — lendo dados automaticamente...";await ReadAsync();}
            else if(found.Count==0){current=null;grid.Rows.Clear();detailCount.Text="0 campos";modelTitle.Text="Nenhum aparelho conectado";platformTag.Text="Conecte por USB";preview.Model="";preview.Platform="";preview.Invalidate();storageValue.Text="Aguardando aparelho";status.Text="●  Aparelho desconectado. Aguardando conexão USB...";}
            else status.Text=$"●  Dispositivos atualizados: {found.Count} conectado(s).";
        }
        catch(Exception ex){status.Text="Falha na detecção automática: "+ex.Message;}
    }
    async Task ReadAsync()
    {
        if(busy)return;
        if(devices.SelectedItem is not DeviceChoice choice){status.Text="●  Nenhum aparelho selecionado. Clique em Buscar primeiro.";return;}
        busy=true;read.Enabled=false;grid.Rows.Clear();status.Text="●  Lendo informações reais do aparelho...";
        try
        {
            current=choice.Info.Platform=="Android"?await DeviceReader.ReadAndroidAsync(choice.Info.Id):await DeviceReader.ReadIPhoneAsync(choice.Info.Id);
            grid.Rows.Clear();
            AddRow("Plataforma",current.Platform);AddRow("Identificador USB",current.Id);
            foreach(var f in current.Fields.OrderBy(x=>GroupOrder(x.Key)).ThenBy(x=>x.Key))AddRow(f.Key,f.Value);
            var model=First(current.Fields,"Modelo comercial","Modelo","Identificador do modelo","Nome do dispositivo","Produto","Fabricante")??current.Platform;
            modelTitle.Text=model;platformTag.Text=current.Platform+"  •  Conectado por USB";preview.Platform=current.Platform;preview.Model=model;preview.Invalidate();
            var capacity=First(current.Fields,"Capacidade total","TotalDiskCapacity","Armazenamento /data");
            storageValue.Text=capacity is null? $"{grid.Rows.Count} informações lidas\nDetalhes exibidos ao lado." : $"{grid.Rows.Count} informações lidas\nCapacidade: {capacity}";
            detailCount.Text=$"{grid.Rows.Count} campos";status.Text=$"●  Leitura concluída — {grid.Rows.Count} campos válidos exibidos.";
        }
        catch(Exception ex){status.Text="Falha na leitura: "+ex.Message;MessageBox.Show(this,ex.Message,"GSM CHECK TOOL",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        finally{read.Enabled=true;busy=false;}
    }
    void AddRow(string label,string? value){if(!Useful(value))return;grid.Rows.Add(label,value!.Trim());}
    static bool Useful(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return false;var v=value.Trim();
        if(v=="-"||v.Equals("null",StringComparison.OrdinalIgnoreCase)||v.Equals("unknown",StringComparison.OrdinalIgnoreCase)||v.Equals("none",StringComparison.OrdinalIgnoreCase))return false;
        string[] bad={"não disponível","nao disponivel","não disponibilizado","nao disponibilizado","não exposta","nao exposta","não encontrado","nao encontrado","not available","unknown key","bloqueado pela segurança android"};
        return !bad.Any(x=>v.Contains(x,StringComparison.OrdinalIgnoreCase));
    }
    static string? First(Dictionary<string,string> fields,params string[] names){foreach(var name in names)if(fields.TryGetValue(name,out var v)&&Useful(v))return v;return null;}
    static int GroupOrder(string key)=>key.Contains("Fabricante")||key.Contains("Marca")||key.Contains("Modelo")||key.Contains("Nome")?0:key.Contains("Android")||key.Contains("iOS")||key.Contains("Build")||key.Contains("Versão")?1:key.Contains("Bateria")||key.Contains("Carga")||key.Contains("Temperatura")?2:key.Contains("IMEI")||key.Contains("Serial")||key.Contains("UDID")||key.Contains("Identificador")?3:4;
    void ClearReport(){current=null;grid.Rows.Clear();detailCount.Text="0 campos";modelTitle.Text="Nenhum aparelho conectado";platformTag.Text="Conecte por USB";preview.Model="";preview.Platform="";preview.Invalidate();storageValue.Text="Aguardando leitura";status.Text="●  Relatório limpo. A detecção automática continua ativa."; }
    void ExportReport()
    {
        if(current==null){MessageBox.Show(this,"Faça a leitura de um aparelho antes de exportar.","GSM CHECK TOOL",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        using var sfd=new SaveFileDialog{Filter="Relatório de texto (*.txt)|*.txt",FileName=$"GSM_CHECK_{current.Platform}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"};if(sfd.ShowDialog(this)!=DialogResult.OK)return;
        var sb=new StringBuilder();sb.AppendLine("GSM CHECK TOOL — RELATÓRIO");sb.AppendLine("Data: "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        foreach(DataGridViewRow row in grid.Rows)sb.AppendLine($"{row.Cells[0].Value}: {row.Cells[1].Value}");
        File.WriteAllText(sfd.FileName,sb.ToString(),Encoding.UTF8);status.Text="●  Relatório exportado: "+sfd.FileName;
    }
    sealed class DeviceChoice{public DeviceInfo Info{get;}public DeviceChoice(DeviceInfo i)=>Info=i;public override string ToString()=> $"{Info.Platform}  |  {Info.Id}";}
}
sealed class ModernCard : Panel
{
    public ModernCard(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var pen=new Pen(Color.FromArgb(40,66,98),1);e.Graphics.DrawRound(new Rectangle(0,0,Width-1,Height-1),12,pen);}
}
sealed class BrandMark : Panel
{
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using var grad=new LinearGradientBrush(ClientRectangle,Color.FromArgb(0,180,255),Color.FromArgb(0,60,180),45f);g.FillRound(grad,new Rectangle(1,1,Width-2,Height-2),15);using var p=new Pen(Color.White,3);g.DrawRound(new Rectangle(17,8,30,47),7,p);g.DrawLine(p,25,15,39,15);g.DrawLine(p,26,47,38,47);using var lens=new Pen(Color.FromArgb(0,235,255),4);g.DrawEllipse(lens,31,29,23,23);g.DrawLine(lens,49,48,61,60);using var f=new Font("Segoe UI",6.5f,FontStyle.Bold);g.DrawString("GSM",f,Brushes.White,12,56);}
}
static class RoundDrawing{public static void FillRound(this Graphics g,Brush b,Rectangle r,int d){using var p=Path(r,d);g.FillPath(b,p);}public static void DrawRound(this Graphics g,Rectangle r,int d,Pen pen){using var p=Path(r,d);g.DrawPath(pen,p);}static GraphicsPath Path(Rectangle r,int d){var p=new GraphicsPath();p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}}
sealed class DevicePreview : Panel
{
    public string Platform{get;set;}="";public string Model{get;set;}="";
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(13,23,40));int h=Math.Min(245,Height-30);if(h<80)return;int w=(int)(h*.52),x=(Width-w)/2,y=Math.Max(8,(Height-h)/2-8);using var body=new LinearGradientBrush(new Rectangle(x,y,w,h),Color.FromArgb(100,126,160),Color.FromArgb(13,22,39),35f);g.FillRound(body,new Rectangle(x,y,w,h),22);using var edge=new Pen(Color.FromArgb(130,178,225),2);g.DrawRound(new Rectangle(x,y,w,h),22,edge);using var screen=new LinearGradientBrush(new Rectangle(x+6,y+7,w-12,h-14),Color.FromArgb(3,38,78),Color.FromArgb(0,5,16),90f);g.FillRound(screen,new Rectangle(x+6,y+7,w-12,h-14),17);using var glow=new Pen(Color.FromArgb(0,154,255),2);g.DrawLine(glow,x+10,y+h/3,x+w-10,y+h/3);using var notch=new SolidBrush(Color.FromArgb(3,6,15));if(Platform=="iPhone")g.FillRound(notch,new Rectangle(x+w/3,y+9,w/3,h/18+5),7);else g.FillEllipse(notch,x+w/2-5,y+10,10,10);using var font=new Font("Segoe UI",8,FontStyle.Bold);using var brush=new SolidBrush(Color.FromArgb(135,210,255));var label=Model.Length>26?Model[..26]+"…":Model;var s=g.MeasureString(label,font);g.DrawString(label,font,brush,(Width-s.Width)/2,Math.Min(Height-20,y+h+5));}
}
