#nullable disable
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices; 

namespace VMIXCaster
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class AppConfig
    {
        public string VmixIp { get; set; } = "127.0.0.1";
        public string PastaRede { get; set; } = @"C:\VmixVideos";
        
        public int VmixInput { get; set; } = 10;
        public int InputIntervalo { get; set; } = 5; 
        public int InputAudioCarimbo { get; set; } = 9; 
        
        public bool AtivarCorteAutomatico { get; set; } = true;
        public bool CorteAutomaticoAoBaixar { get; set; } = true; 
        
        public int InputGradeClipes { get; set; } = 4;
        public int InputVinhetasVideo { get; set; } = 3; 
        public int InputVinhetasAudio { get; set; } = 8; 
    }

    public class MainForm : Form
    {
        // =========================================================
        // IMPORTAÇÃO DO WINDOWS PARA AS TECLAS GLOBAIS (FIFINE)
        // =========================================================
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int WM_HOTKEY = 0x0312;
        // =========================================================

        private TabControl tabControl;
        private TabPage tabPrincipal;
        private TabPage tabConfig;
        
        private TextBox txtLogs;
        private Label lblStatusServidor;
        private Button btnAutomacaoRadio; 
        private Button btnToggleAutoYt; 
        
        private TextBox txtIpVmix;
        private TextBox txtPastaRede;
        private TextBox txtInputVmix;
        private TextBox txtInputIntervalo;
        private TextBox txtInputAudioCarimbo;
        
        private CheckBox chkCorteAutomatico;
        private CheckBox chkCorteAutoAoBaixar; 
        
        private TextBox txtInputGradeClipes;
        private TextBox txtInputVinhetasVideo;
        private TextBox txtInputVinhetasAudio; 
        private Button btnSalvarConfig;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isExiting = false;

        private HttpListener listener;
        private bool isRunning = false;
        private AppConfig config;
        private string configPath = "config.json";

        private bool automacaoLigada = false;
        private bool loopRodando = false;
        private int ultimaVinhetaTocada = 0; 

        private bool modoAutomaticoYt = true;
        private bool ytIntervaloAtivo = false;
        private bool ytTemProximoFila = false;
        private int ytProximoIndexFila = 1;

        private static readonly HttpClient httpClient = new HttpClient();

        public MainForm()
        {
            this.Text = "vMix Caster Azul - v1.0 (PRO Broadcast)";
            this.Size = new Size(540, 680); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            try { this.Icon = new Icon(@"assets\favicon.ico"); } catch { }

            CarregarConfiguracoes();
            InicializarComponentes();
            ConfigurarBandejaDoWindows();
            
            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await VerificarEDownloadYtDlp();
            IniciarServidorHttp();
        }

        private async Task VerificarEDownloadYtDlp()
        {
            string ytDlpPath = "yt-dlp.exe";
            
            if (!File.Exists(ytDlpPath))
            {
                Logar("=======================================================");
                Logar("[SISTEMA] Motor yt-dlp não encontrado na pasta.");
                Logar("[SISTEMA] Baixando automaticamente a versão mais recente...");
                Logar("[SISTEMA] Isso pode demorar alguns segundos, aguarde...");
                Logar("=======================================================\n");
                
                lblStatusServidor.Text = "Status do Servidor: Baixando motor de vídeo (Aguarde...)";
                lblStatusServidor.ForeColor = Color.DarkOrange;

                try
                {
                    byte[] fileBytes = await httpClient.GetByteArrayAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
                    await File.WriteAllBytesAsync(ytDlpPath, fileBytes);
                    
                    Logar("[SISTEMA] Download concluído com sucesso! Sistema pronto.");
                }
                catch (Exception ex)
                {
                    Logar($"[ERRO CRÍTICO] Falha ao baixar o yt-dlp automaticamente: {ex.Message}");
                    lblStatusServidor.Text = "Status do Servidor: Falha ao baixar motor de vídeo!";
                    lblStatusServidor.ForeColor = Color.Red;
                    return;
                }
            }
            else
            {
                Logar("[SISTEMA] Motor yt-dlp detectado. Inicialização rápida concluída.\n");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(this.Handle, 1, MOD_CONTROL | MOD_SHIFT, (int)Keys.F1);
            RegisterHotKey(this.Handle, 2, MOD_CONTROL | MOD_SHIFT, (int)Keys.F2);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == 1) DefinirModoAutomaticoYt(true);
                else if (id == 2) DefinirModoAutomaticoYt(false);
            }
            base.WndProc(ref m);
        }

        private void CarregarConfiguracoes()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    config = new AppConfig();
                }
            }
            else
            {
                config = new AppConfig();
                SalvarConfiguracoesArquivo();
            }
        }

        private void SalvarConfiguracoesArquivo()
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        private void InicializarComponentes()
        {
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            tabPrincipal = new TabPage("Principal");
            tabConfig = new TabPage("Configurações");

            tabControl.TabPages.Add(tabPrincipal);
            tabControl.TabPages.Add(tabConfig);
            this.Controls.Add(tabControl);

            // ================= ABA PRINCIPAL =================
            lblStatusServidor = new Label();
            lblStatusServidor.Text = "Status do Servidor: Aguardando...";
            lblStatusServidor.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblStatusServidor.ForeColor = Color.DarkGoldenrod;
            lblStatusServidor.Location = new Point(15, 12); 
            lblStatusServidor.AutoSize = true;
            tabPrincipal.Controls.Add(lblStatusServidor);

            btnToggleAutoYt = new Button();
            ActualizarBotaoAutoUI();
            btnToggleAutoYt.Location = new Point(310, 8); 
            btnToggleAutoYt.Size = new Size(195, 28);
            btnToggleAutoYt.FlatStyle = FlatStyle.Flat;
            btnToggleAutoYt.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnToggleAutoYt.Cursor = Cursors.Hand;
            btnToggleAutoYt.Click += (s, e) => AlternarModoAutomaticoYt();
            tabPrincipal.Controls.Add(btnToggleAutoYt);

            btnAutomacaoRadio = new Button();
            btnAutomacaoRadio.Text = "▶ ATIVAR AUTOMAÇÃO DA RÁDIO (MODO AUTOMÁTICO)";
            btnAutomacaoRadio.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnAutomacaoRadio.Location = new Point(15, 45); 
            btnAutomacaoRadio.Size = new Size(490, 45);
            btnAutomacaoRadio.BackColor = Color.DarkGreen;
            btnAutomacaoRadio.ForeColor = Color.White;
            btnAutomacaoRadio.FlatStyle = FlatStyle.Flat;
            btnAutomacaoRadio.Click += BtnAutomacaoRadio_Click;
            tabPrincipal.Controls.Add(btnAutomacaoRadio);

            txtLogs = new TextBox();
            txtLogs.Multiline = true;
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = ScrollBars.Vertical;
            txtLogs.BackColor = Color.FromArgb(20, 20, 20);
            txtLogs.ForeColor = Color.FromArgb(0, 255, 128);
            txtLogs.Font = new Font("Consolas", 9);
            txtLogs.Location = new Point(15, 100); 
            txtLogs.Size = new Size(490, 480); 
            tabPrincipal.Controls.Add(txtLogs);

            // ================= ABA CONFIGURAÇÕES =================
            int yPos = 20;

            Label lbl1 = new Label { Text = "IP do vMix (Use 127.0.0.1 no mesmo PC):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtIpVmix = new TextBox { Text = config.VmixIp, Location = new Point(20, yPos + 20), Size = new Size(450, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl1, txtIpVmix });
            yPos += 55;

            Label lbl2 = new Label { Text = "Pasta de Rede (Destino dos Vídeos do YouTube):", Location = new Point(20, yPos), AutoSize = true };
            txtPastaRede = new TextBox { Text = config.PastaRede, Location = new Point(20, yPos + 20), Size = new Size(450, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl2, txtPastaRede });
            yPos += 55;

            Label lbl3 = new Label { Text = "Input do YouTube (Lista):", Location = new Point(20, yPos), AutoSize = true };
            txtInputVmix = new TextBox { Text = config.VmixInput.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl3, txtInputVmix });
            yPos += 55;

            Label lbl4 = new Label { Text = "Input de Cartela/Intervalo (Fallback do YouTube):", Location = new Point(20, yPos), AutoSize = true };
            txtInputIntervalo = new TextBox { Text = config.InputIntervalo.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl4, txtInputIntervalo });
            yPos += 55;

            Label lblNovaCartela = new Label { Text = "Input do Áudio/Vinheta da Cartela (Carimbo):", Location = new Point(20, yPos), AutoSize = true, ForeColor = Color.DarkBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtInputAudioCarimbo = new TextBox { Text = config.InputAudioCarimbo.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lblNovaCartela, txtInputAudioCarimbo });
            yPos += 55;

            chkCorteAutomatico = new CheckBox { Text = "Ativar corte automático para a Cartela no final do YT", Checked = config.AtivarCorteAutomatico, Location = new Point(20, yPos), AutoSize = true };
            tabConfig.Controls.Add(chkCorteAutomatico);
            yPos += 30;

            chkCorteAutoAoBaixar = new CheckBox { Text = "Ao baixar o vídeo, cortar automaticamente para o input da lista", Checked = config.CorteAutomaticoAoBaixar, Location = new Point(20, yPos), AutoSize = true };
            tabConfig.Controls.Add(chkCorteAutoAoBaixar);
            yPos += 35;

            Label lblSep = new Label { Text = "--------------- CONFIGURAÇÕES DA AUTOMAÇÃO DE RÁDIO ---------------", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(20, yPos), Size = new Size(450, 20) };
            tabConfig.Controls.Add(lblSep);
            yPos += 30;

            Label lbl5 = new Label { Text = "Input da Grade de Clipes/Músicas (Lista Principal):", Location = new Point(20, yPos), AutoSize = true };
            txtInputGradeClipes = new TextBox { Text = config.InputGradeClipes.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl5, txtInputGradeClipes });
            yPos += 55;

            Label lbl6 = new Label { Text = "Input da Vinheta Visual (VÍDEO da tela):", Location = new Point(20, yPos), AutoSize = true };
            txtInputVinhetasVideo = new TextBox { Text = config.InputVinhetasVideo.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl6, txtInputVinhetasVideo });
            yPos += 55;

            Label lbl7 = new Label { Text = "Input das Vinhetas de Áudio (Lista de arquivos .WAV):", Location = new Point(20, yPos), AutoSize = true };
            txtInputVinhetasAudio = new TextBox { Text = config.InputVinhetasAudio.ToString(), Location = new Point(20, yPos + 20), Size = new Size(100, 23) };
            tabConfig.Controls.AddRange(new Control[] { lbl7, txtInputVinhetasAudio });
            yPos += 55;

            btnSalvarConfig = new Button { Text = "Salvar Todas as Configurações", Location = new Point(20, yPos), Size = new Size(200, 35), BackColor = Color.FromArgb(21, 34, 67), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSalvarConfig.Click += BtnSalvarConfig_Click;
            tabConfig.Controls.Add(btnSalvarConfig);
        }

        private void AlternarModoAutomaticoYt()
        {
            DefinirModoAutomaticoYt(!modoAutomaticoYt);
        }

        private void DefinirModoAutomaticoYt(bool estado)
        {
            if (modoAutomaticoYt == estado) return; 
            
            modoAutomaticoYt = estado;
            ActualizarBotaoAutoUI();
            Logar(modoAutomaticoYt ? "[-] YOUTUBE: AUTOMÁTICO LIGADO" : "[!] YOUTUBE: MANUAL LIGADO");
        }

        private void ActualizarBotaoAutoUI()
        {
            if (btnToggleAutoYt.InvokeRequired)
            {
                btnToggleAutoYt.Invoke(new Action(ActualizarBotaoAutoUI));
            }
            else
            {
                if (modoAutomaticoYt)
                {
                    btnToggleAutoYt.Text = "⚡ YT Automático: LIGADO";
                    btnToggleAutoYt.BackColor = Color.FromArgb(16, 185, 129);
                    btnToggleAutoYt.ForeColor = Color.White;
                }
                else
                {
                    btnToggleAutoYt.Text = "⏸️ YT Automático: DESLIGADO";
                    btnToggleAutoYt.BackColor = Color.FromArgb(239, 68, 68);
                    btnToggleAutoYt.ForeColor = Color.White;
                }
            }
        }

        private void BtnAutomacaoRadio_Click(object sender, EventArgs e)
        {
            automacaoLigada = !automacaoLigada;
            if (automacaoLigada)
            {
                btnAutomacaoRadio.Text = "⏹ DESATIVAR AUTOMAÇÃO DA RÁDIO (MODO MANUAL)";
                btnAutomacaoRadio.BackColor = Color.DarkRed;
                _ = MotorAutomacaoRadio();
            }
            else
            {
                btnAutomacaoRadio.Text = "▶ ATIVAR AUTOMAÇÃO DA RÁDIO (MODO AUTOMÁTICO)";
                btnAutomacaoRadio.BackColor = Color.DarkGreen;
                Logar("\n[AUTOMAÇÃO] Sistema pausado. O operador assumiu o controle manual.\n");
            }
        }

        private async Task MotorAutomacaoYT()
        {
            Logar("\n=======================================================");
            Logar("[AUTOMAÇÃO YT] MODO YOUTUBE ATIVADO! Motor contínuo...");
            Logar("=======================================================\n");

            string baseUrl = $"http://{config.VmixIp}:8088/api/";
            bool carimboPreparado = false;
            int erroCount = 0;

            while (isRunning)
            {
                await Task.Delay(50); 

                if (!modoAutomaticoYt || !config.AtivarCorteAutomatico) 
                {
                    carimboPreparado = false;
                    ytIntervaloAtivo = false;
                    continue;
                }

                try
                {
                    string xml = await httpClient.GetStringAsync(baseUrl);
                    erroCount = 0; 
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    XmlNode activeNode = doc.SelectSingleNode("//active");
                    if (activeNode == null) continue;
                    int activeInput = int.Parse(activeNode.InnerText);

                    if (activeInput == config.VmixInput)
                    {
                        ytIntervaloAtivo = false; 

                        XmlNode inputNode = doc.SelectSingleNode($"//input[@number='{config.VmixInput}']");
                        if (inputNode != null)
                        {
                            long position = 0, duration = 0;
                            string state = inputNode.Attributes["state"]?.Value ?? "";

                            if (inputNode.Attributes["position"] != null) position = long.Parse(inputNode.Attributes["position"].Value);
                            if (inputNode.Attributes["duration"] != null) duration = long.Parse(inputNode.Attributes["duration"].Value);
                            long remaining = duration - position;

                            if (duration > 0 && remaining <= 20000 && remaining > 500 && !carimboPreparado)
                            {
                                if (config.InputAudioCarimbo > 0)
                                {
                                    Logar($"[Automação YT] Faltam 20s. Preparando Áudio (Input {config.InputAudioCarimbo})...");
                                    await httpClient.GetAsync($"{baseUrl}?Function=NextItem&Input={config.InputAudioCarimbo}");
                                }
                                carimboPreparado = true;
                            }

                            if (duration > 0 && (remaining <= 500 || state.Equals("Paused", StringComparison.OrdinalIgnoreCase)))
                            {
                                Logar("[Automação YT] Clipe acabou. Cortando para a Cartela + Áudio...");

                                ytTemProximoFila = false;
                                XmlNodeList items = inputNode.SelectNodes("list/item");
                                if (items != null)
                                {
                                    int selectedIndex = 0;
                                    for(int i = 0; i < items.Count; i++)
                                    {
                                        XmlAttribute attr = items[i].Attributes["selected"];
                                        if (attr != null && attr.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        {
                                            selectedIndex = i; break;
                                        }
                                    }
                                    if (items.Count > 0 && selectedIndex < items.Count - 1)
                                    {
                                        ytTemProximoFila = true;
                                        ytProximoIndexFila = selectedIndex + 2; 
                                    }
                                }

                                if (!carimboPreparado && config.InputAudioCarimbo > 0)
                                {
                                    await httpClient.GetAsync($"{baseUrl}?Function=NextItem&Input={config.InputAudioCarimbo}");
                                    await Task.Delay(50);
                                }
                                if (config.InputAudioCarimbo > 0)
                                {
                                    await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.InputAudioCarimbo}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.InputAudioCarimbo}");
                                }
                                
                                await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.InputIntervalo}");
                                await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.InputIntervalo}");
                                await httpClient.GetAsync($"{baseUrl}?Function=Fade&Input={config.InputIntervalo}&Duration=500");
                                
                                await Task.Delay(500); 
                                await httpClient.GetAsync($"{baseUrl}?Function=Pause&Input={config.VmixInput}");
                                
                                carimboPreparado = false;
                                ytIntervaloAtivo = true; 
                                await Task.Delay(1000); 
                            }
                        }
                    }
                    else if (activeInput == config.InputIntervalo && ytIntervaloAtivo)
                    {
                        carimboPreparado = false;

                        XmlNode carimboNode = doc.SelectSingleNode($"//input[@number='{config.InputAudioCarimbo}']");
                        if (carimboNode != null)
                        {
                            long position = 0, duration = 0;
                            string state = carimboNode.Attributes["state"]?.Value ?? "";

                            if (carimboNode.Attributes["position"] != null) position = long.Parse(carimboNode.Attributes["position"].Value);
                            if (carimboNode.Attributes["duration"] != null) duration = long.Parse(carimboNode.Attributes["duration"].Value);
                            long remaining = duration - position;

                            if (duration > 0 && (remaining <= 500 || state.Equals("Paused", StringComparison.OrdinalIgnoreCase)))
                            {
                                Logar("[Automação YT] Áudio do intervalo acabou. Avaliando a fila...");

                                if (ytTemProximoFila)
                                {
                                    Logar($"[Automação YT] Fila continua! Voltando para o clipe do YT...");
                                    await httpClient.GetAsync($"{baseUrl}?Function=SelectIndex&Input={config.VmixInput}&Value={ytProximoIndexFila}");
                                    await Task.Delay(50);
                                    await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.VmixInput}");
                                    await Task.Delay(50);
                                    await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.VmixInput}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Fade&Input={config.VmixInput}&Duration=500");
                                }
                                else
                                {
                                    Logar("[Automação YT] Fila finalizada. Mantendo a Cartela no ar.");
                                }

                                ytIntervaloAtivo = false;
                                await Task.Delay(1500); 
                            }
                        }
                        else
                        {
                            ytIntervaloAtivo = false;
                        }
                    }
                    else
                    {
                        carimboPreparado = false;
                        ytIntervaloAtivo = false; 
                    }
                }
                catch (Exception ex)
                {
                    if (erroCount == 0) Logar($"[Erro Motor YT] Conexão falhou ({ex.Message})");
                    erroCount++;
                }
            }
        }

        private async Task MotorAutomacaoRadio()
        {
            if (loopRodando) return; 
            loopRodando = true;
            
            Logar("\n=======================================================");
            Logar("[AUTOMAÇÃO] MODO RÁDIO ATIVADO! Vigiando os cortes...");
            Logar("=======================================================\n");
            
            string baseUrl = $"http://{config.VmixIp}:8088/api/";
            Random random = new Random();
            bool vinhetaPreparada = false; 

            try
            {
                while (automacaoLigada && isRunning)
                {
                    await Task.Delay(50); 
                    
                    try
                    {
                        string xml = await httpClient.GetStringAsync(baseUrl);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);

                        XmlNode activeNode = doc.SelectSingleNode("//active");
                        if (activeNode == null) continue;
                        int activeInput = int.Parse(activeNode.InnerText);

                        if (activeInput == config.InputGradeClipes)
                        {
                            XmlNode inputNode = doc.SelectSingleNode($"//input[@number='{config.InputGradeClipes}']");
                            if (inputNode != null)
                            {
                                long position = 0;
                                long duration = 0;
                                if (inputNode.Attributes["position"] != null) position = long.Parse(inputNode.Attributes["position"].Value);
                                if (inputNode.Attributes["duration"] != null) duration = long.Parse(inputNode.Attributes["duration"].Value);
                                long remaining = duration - position;

                                if (duration > 0 && remaining <= 20000 && remaining > 500 && !vinhetaPreparada)
                                {
                                    Logar("[Automação Rádio] Faltam 20s. Sorteando vinheta de áudio silenciosamente...");
                                    
                                    XmlNodeList vinhetas = doc.SelectNodes($"//input[@number='{config.InputVinhetasAudio}']/list/item");
                                    if (vinhetas != null && vinhetas.Count > 0)
                                    {
                                        int vinhetaSorteada = random.Next(1, vinhetas.Count + 1);
                                        while (vinhetas.Count > 1 && vinhetaSorteada == ultimaVinhetaTocada)
                                        {
                                            vinhetaSorteada = random.Next(1, vinhetas.Count + 1);
                                        }
                                        ultimaVinhetaTocada = vinhetaSorteada;

                                        await httpClient.GetAsync($"{baseUrl}?Function=SelectIndex&Input={config.InputVinhetasAudio}&Value={vinhetaSorteada}");
                                    }
                                    
                                    vinhetaPreparada = true; 
                                }

                                if (duration > 0 && remaining <= 500)
                                {
                                    Logar("[Automação Rádio] Clipe no fim. Disparando Ação Dupla (Vídeo + Áudio)...");
                                    
                                    if (!vinhetaPreparada)
                                    {
                                        XmlNodeList vinhetas = doc.SelectNodes($"//input[@number='{config.InputVinhetasAudio}']/list/item");
                                        if (vinhetas != null && vinhetas.Count > 0)
                                        {
                                            int vinhetaSorteada = random.Next(1, vinhetas.Count + 1);
                                            while (vinhetas.Count > 1 && vinhetaSorteada == ultimaVinhetaTocada)
                                            {
                                                vinhetaSorteada = random.Next(1, vinhetas.Count + 1);
                                            }
                                            ultimaVinhetaTocada = vinhetaSorteada;
                                            await httpClient.GetAsync($"{baseUrl}?Function=SelectIndex&Input={config.InputVinhetasAudio}&Value={vinhetaSorteada}");
                                            await Task.Delay(50);
                                        }
                                    }
                                    
                                    await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.InputVinhetasAudio}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.InputVinhetasAudio}");
                                    
                                    await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.InputVinhetasVideo}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.InputVinhetasVideo}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Fade&Input={config.InputVinhetasVideo}&Duration=500");
                                    
                                    vinhetaPreparada = false; 
                                    await Task.Delay(1500); 
                                }
                            }
                        }
                        else if (activeInput == config.InputVinhetasVideo)
                        {
                            vinhetaPreparada = false; 

                            XmlNode inputNode = doc.SelectSingleNode($"//input[@number='{config.InputVinhetasVideo}']");
                            if (inputNode != null)
                            {
                                long position = 0;
                                long duration = 0;
                                if (inputNode.Attributes["position"] != null) position = long.Parse(inputNode.Attributes["position"].Value);
                                if (inputNode.Attributes["duration"] != null) duration = long.Parse(inputNode.Attributes["duration"].Value);
                                long remaining = duration - position;

                                if (duration > 0 && remaining <= 500)
                                {
                                    Logar("[Automação Rádio] Vinheta visual no fim. Avançando Grade de Clipes...");
                                    
                                    int proximoIndex = 1;
                                    XmlNode clipeNode = doc.SelectSingleNode($"//input[@number='{config.InputGradeClipes}']");
                                    if (clipeNode != null)
                                    {
                                        int currentIndex = 1;
                                        if (clipeNode.Attributes["selectedIndex"] != null)
                                        {
                                            currentIndex = int.Parse(clipeNode.Attributes["selectedIndex"].Value);
                                        }
                                        
                                        XmlNodeList totalClipes = clipeNode.SelectNodes("list/item");
                                        int total = totalClipes != null ? totalClipes.Count : 1;
                                        
                                        proximoIndex = currentIndex + 1;
                                        if (proximoIndex > total) proximoIndex = 1;
                                    }

                                    await httpClient.GetAsync($"{baseUrl}?Function=SelectIndex&Input={config.InputGradeClipes}&Value={proximoIndex}");
                                    await Task.Delay(50);
                                    
                                    await httpClient.GetAsync($"{baseUrl}?Function=Restart&Input={config.InputGradeClipes}");
                                    await Task.Delay(50);
                                    await httpClient.GetAsync($"{baseUrl}?Function=Play&Input={config.InputGradeClipes}");
                                    await httpClient.GetAsync($"{baseUrl}?Function=Fade&Input={config.InputGradeClipes}&Duration=500");
                                    
                                    await Task.Delay(1500); 
                                }
                            }
                        }
                        else 
                        {
                            vinhetaPreparada = false;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            finally
            {
                loopRodando = false;
            }
        }

        private void ConfigurarBandejaDoWindows()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exibir aplicativo", null, ShowApp_Click);
            trayMenu.Items.Add("-"); 
            trayMenu.Items.Add("Encerrar Servidor", null, ExitApp_Click);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "vMix Caster Azul - Rodando";
            try { trayIcon.Icon = new Icon(@"assets\favicon.ico"); } 
            catch { trayIcon.Icon = SystemIcons.Information; } 
            
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            
            trayIcon.DoubleClick += ShowApp_Click;
        }

        private void ShowApp_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void ExitApp_Click(object sender, EventArgs e)
        {
            isExiting = true;
            trayIcon.Visible = false;
            Application.Exit(); 
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isExiting)
            {
                e.Cancel = true; 
                this.Hide();     
                this.ShowInTaskbar = false; 
                
                trayIcon.ShowBalloonTip(2000, "VMIXCaster Oculto", "O servidor continua rodando em segundo plano. Clique com o botão direito no ícone para acessar.", ToolTipIcon.Info);
                return;
            }

            isRunning = false;
            try { listener?.Stop(); } catch { }
            base.OnFormClosing(e);
        }

        private void BtnSalvarConfig_Click(object sender, EventArgs e)
        {
            try
            {
                config.VmixIp = txtIpVmix.Text.Trim();
                config.PastaRede = txtPastaRede.Text.Trim();
                config.VmixInput = int.Parse(txtInputVmix.Text.Trim());
                config.InputIntervalo = int.Parse(txtInputIntervalo.Text.Trim());
                config.InputAudioCarimbo = int.Parse(txtInputAudioCarimbo.Text.Trim());
                
                config.AtivarCorteAutomatico = chkCorteAutomatico.Checked;
                config.CorteAutomaticoAoBaixar = chkCorteAutoAoBaixar.Checked; 
                
                config.InputGradeClipes = int.Parse(txtInputGradeClipes.Text.Trim());
                config.InputVinhetasVideo = int.Parse(txtInputVinhetasVideo.Text.Trim());
                config.InputVinhetasAudio = int.Parse(txtInputVinhetasAudio.Text.Trim());

                SalvarConfiguracoesArquivo();
                Logar("[-] Configurações atualizadas com sucesso!");
                MessageBox.Show("Configurações salvas com sucesso!", "VMIXCaster", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabPrincipal;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações.\nDetalhes: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Logar(string mensagem)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.Invoke(new Action<string>(Logar), mensagem);
            }
            else
            {
                txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {mensagem}\r\n");
            }
        }

        private async Task<string> ObterTituloYouTube(string url)
        {
            try
            {
                string oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json";
                string jsonResponse = await httpClient.GetStringAsync(oembedUrl);
                
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                string title = doc.RootElement.GetProperty("title").GetString();

                char[] invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    title = title.Replace(c, '-');
                }
                
                return title.Trim().TrimEnd('.'); 
            }
            catch
            {
                return "Clipe_YT_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            }
        }

        private async void EnviarRespostaJson(HttpListenerResponse response, string json)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch { }
        }

        private void IniciarServidorHttp()
        {
            Task.Run(async () =>
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:5050/");

                try
                {
                    listener.Start();
                    isRunning = true;
                    this.Invoke(new Action(() => {
                        lblStatusServidor.Text = "Status do Servidor HTTP: Rodando (Porta 5050)";
                        lblStatusServidor.ForeColor = Color.Green;
                    }));
                    Logar("Servidor de Extensão do YouTube iniciado.");
                    
                    _ = MotorAutomacaoYT();
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => {
                        lblStatusServidor.Text = "Status do Servidor: Erro ao iniciar";
                        lblStatusServidor.ForeColor = Color.Red;
                    }));
                    Logar($"Erro ao iniciar HttpListener: {ex.Message}");
                    return;
                }

                while (isRunning)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        var request = context.Request;
                        var response = context.Response;

                        response.AddHeader("Access-Control-Allow-Origin", "*");
                        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                        if (request.HttpMethod == "OPTIONS")
                        {
                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.Close();
                            continue;
                        }

                        if (request.Url.AbsolutePath == "/toggle-auto")
                        {
                            AlternarModoAutomaticoYt();
                            EnviarRespostaJson(response, $"{{\"automatico\": {modoAutomaticoYt.ToString().ToLower()}}}");
                            continue;
                        }
                        if (request.Url.AbsolutePath == "/auto-on")
                        {
                            DefinirModoAutomaticoYt(true);
                            EnviarRespostaJson(response, "{\"status\": \"ligado\"}");
                            continue;
                        }
                        if (request.Url.AbsolutePath == "/auto-off")
                        {
                            DefinirModoAutomaticoYt(false);
                            EnviarRespostaJson(response, "{\"status\": \"desligado\"}");
                            continue;
                        }

                        if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/enviar")
                        {
                            using StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
                            string body = await reader.ReadToEndAsync();

                            using JsonDocument doc = JsonDocument.Parse(body);
                            string url = doc.RootElement.GetProperty("url").GetString();
                            string quality = doc.RootElement.GetProperty("quality").GetString();
                            
                            EnviarRespostaJson(response, "{\"status\": \"ok\"}");

                            _ = ProcessarVideoEvMix(url, quality);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logar($"Erro na requisição HTTP: {ex.Message}");
                    }
                }
            });
        }

        private async Task ProcessarVideoEvMix(string url, string quality)
        {
            try
            {
                if (!Directory.Exists(config.PastaRede))
                {
                    Directory.CreateDirectory(config.PastaRede);
                }

                string tituloVideo = await ObterTituloYouTube(url);
                string arquivoSaida = Path.Combine(config.PastaRede, $"{tituloVideo}.mp4");

                string formatString = quality == "1080"
                    ? "\"bestvideo[ext=mp4][vcodec^=avc][height<=1080][fps<=60]+bestaudio[ext=m4a]/best[ext=mp4][vcodec^=avc]/best\""
                    : "\"22/best[ext=mp4][vcodec^=avc][height<=720]/bestvideo[ext=mp4][vcodec^=avc][height<=720]+bestaudio[ext=m4a]/best\"";

                string argumentos = $"--no-cache-dir --no-playlist -N 16 --no-mtime -f {formatString} -o \"{arquivoSaida}\" \"{url}\"";

                Logar($"\n[Baixando do YT] Arquivo: {tituloVideo}.mp4");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "yt-dlp.exe",
                    Arguments = argumentos,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            if (e.Data.Contains("[download] Destination") || e.Data.Contains("[Merger]") || e.Data.Contains("has already been downloaded"))
                            {
                                Logar($"> {e.Data}");
                            }
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            Logar($"[Aviso yt-dlp] {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();
                }

                if (File.Exists(arquivoSaida))
                {
                    Logar("Arquivo pronto. Acionando Lista de Eventos do vMix...");
                    await EnviarComandosVmix(arquivoSaida);
                }
                else
                {
                    Logar("Erro Crítico: O arquivo MP4 não foi encontrado após o processo.");
                }
            }
            catch (Exception ex)
            {
                Logar($"Erro no processamento: {ex.Message}");
            }
        }

        // =========================================================
        // MÓDULO INTELIGENTE DE ENVIO PARA A LISTA
        // =========================================================
        private async Task EnviarComandosVmix(string caminhoArquivo)
        {
            string baseUrl = $"http://{config.VmixIp}:8088/api/?Function=";
            string arquivoUrlEncoded = Uri.EscapeDataString(caminhoArquivo);
            
            bool ytEstaNoAr = false;
            long position = 0;
            long duration = 0;

            try
            {
                string xmlStr = await httpClient.GetStringAsync($"http://{config.VmixIp}:8088/api/");
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlStr);
                
                // 1. Verifica se o vídeo está na tela
                XmlNode activeNode = doc.SelectSingleNode("//active");
                if (activeNode != null)
                {
                    int activeInput = int.Parse(activeNode.InnerText);
                    if (activeInput == config.VmixInput || (activeInput == config.InputIntervalo && ytIntervaloAtivo)) 
                    {
                        ytEstaNoAr = true;
                    }
                }

                // 2. Coleta a situação atual do input do YouTube (para a inteligência de lista)
                XmlNode inputNode = doc.SelectSingleNode($"//input[@number='{config.VmixInput}']");
                if (inputNode != null)
                {
                    if (inputNode.Attributes["position"] != null) position = long.Parse(inputNode.Attributes["position"].Value);
                    if (inputNode.Attributes["duration"] != null) duration = long.Parse(inputNode.Attributes["duration"].Value);
                }
            }
            catch { }

            try
            {
                if (modoAutomaticoYt && config.CorteAutomaticoAoBaixar)
                {
                    // === MODO CORTE SECO AO VIVO ===
                    if (!ytEstaNoAr)
                    {
                        await httpClient.GetAsync($"{baseUrl}ListRemoveAll&Input={config.VmixInput}");
                        await Task.Delay(100); 

                        await httpClient.GetAsync($"{baseUrl}ListAdd&Input={config.VmixInput}&Value={arquivoUrlEncoded}");
                        await Task.Delay(100); 

                        await httpClient.GetAsync($"{baseUrl}SelectIndex&Input={config.VmixInput}&Value=1");
                        await Task.Delay(100);

                        await httpClient.GetAsync($"{baseUrl}Restart&Input={config.VmixInput}");
                        await Task.Delay(50);

                        await httpClient.GetAsync($"{baseUrl}Play&Input={config.VmixInput}");
                        await httpClient.GetAsync($"{baseUrl}Fade&Input={config.VmixInput}&Duration=1000");
                        
                        Logar("====== NOVA SEQUÊNCIA DO YT NO AR COM FADE! ======\n");
                    }
                    else
                    {
                        await httpClient.GetAsync($"{baseUrl}ListAdd&Input={config.VmixInput}&Value={arquivoUrlEncoded}");
                        Logar("====== [FILA YT] Vídeo enfileirado na sequência atual! ======\n");
                    }
                }
                else
                {
                    // === MODO DE PREPARAÇÃO INTELIGENTE DA LISTA ===
                    // Adiciona o vídeo silenciosamente no fundo da lista
                    await httpClient.GetAsync($"{baseUrl}ListAdd&Input={config.VmixInput}&Value={arquivoUrlEncoded}");
                    await Task.Delay(100);

                    if (!ytEstaNoAr)
                    {
                        long remaining = duration > 0 ? duration - position : 0;

                        // A MÁGICA ACONTECE AQUI:
                        // Se a lista estiver vazia (duration = 0) 
                        // OU se o vídeo que tava nela já acabou de tocar inteiro (remaining <= 500)
                        if (duration == 0 || remaining <= 500)
                        {
                            // A gente lê de novo para descobrir qual é o index desse vídeo novo
                            string xmlStr2 = await httpClient.GetStringAsync($"http://{config.VmixIp}:8088/api/");
                            XmlDocument doc2 = new XmlDocument();
                            doc2.LoadXml(xmlStr2);
                            XmlNode inputNode2 = doc2.SelectSingleNode($"//input[@number='{config.VmixInput}']");
                            
                            if (inputNode2 != null)
                            {
                                XmlNodeList items = inputNode2.SelectNodes("list/item");
                                int totalItems = items != null ? items.Count : 1;
                                
                                // O Vmix seleciona o vídeo novo e dá Rewind para ficar no ponto!
                                await httpClient.GetAsync($"{baseUrl}SelectIndex&Input={config.VmixInput}&Value={totalItems}");
                                await Task.Delay(50);
                                await httpClient.GetAsync($"{baseUrl}Restart&Input={config.VmixInput}");
                                
                                Logar("====== [LISTA YT] Vídeo adicionado e PREPARADO para ir pro ar! ======\n");
                            }
                        }
                        else
                        {
                            // Já tem vídeo bom engatilhado esperando para tocar, então não mexemos
                            Logar("====== [LISTA YT] Vídeo enfileirado silenciosamente atrás do atual ======\n");
                        }
                    }
                    else
                    {
                        Logar("====== [LISTA YT] Vídeo enfileirado na sequência que já está rodando! ======\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Logar($"Erro ao comunicar com a API do vMix: {ex.Message}");
            }
        }
    }
}