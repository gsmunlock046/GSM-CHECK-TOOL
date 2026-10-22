using GSMCheckTool.Services;
using System.Drawing.Drawing2D;
using System.Text;
namespace GSMCheckTool;

public sealed class MainForm : Form
{
    readonly Color bg=Color.FromArgb(10,15,26), panel=Color.FromArgb(18,27,43), panel2=Color.FromArgb(23,35,54), blue=Color.FromArgb(38,132,255), white=Color.FromArgb(240,245,252), muted=Color.FromArgb(151,168,190);
    readonly ComboBox devices=new(); readonly DataGridView grid=new(); Label status=new(); Button scan=new(); Button read=new(); Button export=new(); DeviceInfo? current;
    public MainForm()
    {
        Text="GSM CHECK TOOL"; StartPosition=FormStartPosition.CenterScreen; MinimumSize=new Size(1000,680); Size=new Size(1240,820);
        BackColor=bg; ForeColor=white; Font=new Font("Segoe UI",10); BuildUI(); Shown+=async(_,_)=>await ScanAsync();
    }
    void BuildUI()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(22),BackColor=bg};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,76)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,165)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); Controls.Add(root);
        var header=new Panel{Dock=DockStyle.Fill,BackColor=bg}; root.Controls.Add(header,0,0);
        var logo=new Panel{Location=new Point(0,8),Size=new Size(54,54),BackColor=blue};
        logo.Paint+=(_,e)=>{e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; using var p=new Pen(Color.White,3);e.Graphics.DrawArc(p,12,12,30,30,35,280);e.Graphics.DrawLine(p,26,14,26,39);e.Graphics.DrawLine(p,17,27,35,27);}; header.Controls.Add(logo);
        header.Controls.Add(new Label{Text="GSM CHECK TOOL",Font=new Font("Segoe UI",19,FontStyle.Bold),ForeColor=white,AutoSize=true,Location=new Point(68,5)});
        header.Controls.Add(new Label{Text="DIAGNÓSTICO USB  •  ANDROID + IPHONE",Font=new Font("Segoe UI",9,FontStyle.Bold),ForeColor=muted,AutoSize=true,Location=new Point(70,40)});
        var pill=new Label{Text="DEVICE INSPECTOR",AutoSize=true,ForeColor=Color.FromArgb(151,202,255),BackColor=panel2,Padding=new Padding(13,8,13,8),Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(Width-270,17)};
        header.Controls.Add(pill); header.Resize+=(_,_)=>pill.Left=header.ClientSize.Width-pill.Width-8;
        var hero=new Panel{Dock=DockStyle.Fill,BackColor=panel,Padding=new Padding(20)}; root.Controls.Add(hero,0,1);
        hero.Paint+=(_,e)=>{using var p=new Pen(Color.FromArgb(42,59,82));e.Graphics.DrawRectangle(p,0,0,hero.Width-1,hero.Height-1);};
        hero.Controls.Add(new Label{Text="Leia seu aparelho. Conheça cada detalhe.",Font=new Font("Segoe UI",17,FontStyle.Bold),ForeColor=white,AutoSize=true,Location=new Point(20,15)});
        hero.Controls.Add(new Label{Text="Conecte via USB, autorize o acesso no aparelho e inicie a leitura.",Font=new Font("Segoe UI",10),ForeColor=muted,AutoSize=true,Location=new Point(22,49)});
        devices.DropDownStyle=ComboBoxStyle.DropDownList; devices.Location=new Point(22,91); devices.Width=380; devices.Height=34; devices.BackColor=panel2; devices.ForeColor=white; devices.FlatStyle=FlatStyle.Flat; hero.Controls.Add(devices);
        scan=MakeButton("↻  BUSCAR APARELHOS",blue); scan.Location=new Point(420,88); scan.Size=new Size(190,40); scan.Click+=async(_,_)=>await ScanAsync(); hero.Controls.Add(scan);
        read=MakeButton("⌕  LER INFORMAÇÕES",Color.FromArgb(36,54,78)); read.Location=new Point(620,88); read.Size=new Size(190,40); read.Click+=async(_,_)=>await ReadAsync(); hero.Controls.Add(read);
        export=MakeButton("⇩  EXPORTAR RELATÓRIO",Color.FromArgb(36,54,78)); export.Location=new Point(820,88); export.Size=new Size(200,40); export.Click+=(_,_)=>ExportReport(); hero.Controls.Add(export);
        status=new Label{Text="Pronto para conectar",Dock=DockStyle.Bottom,Height=24,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft}; hero.Controls.Add(status);
        var body=new Panel{Dock=DockStyle.Fill,BackColor=panel,Padding=new Padding(12)}; root.Controls.Add(body,0,2);
        body.Controls.Add(new Label{Text="RELATÓRIO DO DISPOSITIVO",Dock=DockStyle.Top,Height=34,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Color.FromArgb(151,202,255),TextAlign=ContentAlignment.MiddleLeft});
        grid.Dock=DockStyle.Fill; grid.BackgroundColor=panel; grid.BorderStyle=BorderStyle.None; grid.GridColor=Color.FromArgb(38,52,73); grid.EnableHeadersVisualStyles=false; grid.ColumnCount=2;
        grid.Columns[0].Name="Informação"; grid.Columns[0].Width=270; grid.Columns[1].Name="Valor"; grid.Columns[1].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.RowHeadersVisible=false; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.ReadOnly=true; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;
        grid.DefaultCellStyle.BackColor=panel; grid.DefaultCellStyle.ForeColor=white; grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(32,75,127); grid.DefaultCellStyle.SelectionForeColor=Color.White; grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
        grid.ColumnHeadersDefaultCellStyle.BackColor=panel2; grid.ColumnHeadersDefaultCellStyle.ForeColor=white; grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold); grid.ColumnHeadersHeight=38; grid.RowTemplate.Height=34; body.Controls.Add(grid);
        var footer=new Label{Text="GSM CHECK TOOL  •  Os dados dependem do fabricante, das permissões e da versão do sistema.",Dock=DockStyle.Bottom,Height=28,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft}; body.Controls.Add(footer); footer.BringToFront();
    }
    Button MakeButton(string text,Color color){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=color,ForeColor=white,Font=new Font("Segoe UI",9,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;return b;}
    async Task ScanAsync()
    {
        scan.Enabled=false; status.Text="Procurando dispositivos USB...";
        try
        {
            var found=await DeviceReader.DetectAsync(); devices.Items.Clear();
            foreach(var d in found) devices.Items.Add(new DeviceChoice(d));
            if(devices.Items.Count>0){devices.SelectedIndex=0;status.Text=$"{devices.Items.Count} aparelho(s) detectado(s).";}
            else status.Text="Nenhum aparelho detectado. Verifique cabo, drivers e autorização USB.";
        }
        catch(Exception ex){status.Text="Erro na busca: "+ex.Message;}
        finally{scan.Enabled=true;}
    }
    async Task ReadAsync()
    {
        if(devices.SelectedItem is not DeviceChoice choice){status.Text="Conecte e selecione um aparelho primeiro.";return;}
        read.Enabled=false; grid.Rows.Clear(); status.Text="Lendo informações do dispositivo...";
        try
        {
            current=choice.Info.Platform=="Android"?await DeviceReader.ReadAndroidAsync(choice.Info.Id):await DeviceReader.ReadIPhoneAsync(choice.Info.Id);
            grid.Rows.Add("Plataforma",current.Platform); grid.Rows.Add("Identificador USB / UDID",current.Id);
            foreach(var f in current.Fields) grid.Rows.Add(f.Key,f.Value);
            status.Text="Leitura concluída. Confira os campos não disponibilizados pelo sistema.";
        }
        catch(Exception ex){status.Text="Não foi possível ler: "+ex.Message;MessageBox.Show(this,ex.Message,"GSM CHECK TOOL",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        finally{read.Enabled=true;}
    }
    void ExportReport()
    {
        if(current==null){MessageBox.Show(this,"Faça a leitura de um aparelho antes de exportar.","GSM CHECK TOOL",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        using var sfd=new SaveFileDialog{Filter="Relatório de texto (*.txt)|*.txt",FileName=$"GSM_CHECK_{current.Platform}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"};
        if(sfd.ShowDialog(this)!=DialogResult.OK)return;
        var sb=new StringBuilder(); sb.AppendLine("GSM CHECK TOOL — RELATÓRIO"); sb.AppendLine("Data: "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Plataforma: "+current.Platform); sb.AppendLine("ID/UDID: "+current.Id); sb.AppendLine(new string('-',60));
        foreach(var f in current.Fields) sb.AppendLine($"{f.Key}: {f.Value}");
        File.WriteAllText(sfd.FileName,sb.ToString(),Encoding.UTF8); status.Text="Relatório exportado: "+sfd.FileName;
    }
    sealed class DeviceChoice{public DeviceInfo Info{get;} public DeviceChoice(DeviceInfo info)=>Info=info; public override string ToString()=> $"{Info.Platform}  |  {Info.Id}";}
}