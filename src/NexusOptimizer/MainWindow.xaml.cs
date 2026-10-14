using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NexusOptimizer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly List<string> _log = new();
    private readonly PerformanceCounter? _cpuCounter;
    private string _page = "dashboard";

    public MainWindow()
    {
        InitializeComponent();
        try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpuCounter.NextValue(); } catch { }
        UpdateDashboard();
        _timer.Tick += (_, _) => UpdateDashboard();
        _timer.Start();
        AddLog("Application démarrée.");
    }

    private void UpdateDashboard()
    {
        try
        {
            double cpu = _cpuCounter?.NextValue() ?? 0;
            CpuValue.Text = $"{Math.Clamp(cpu, 0, 100):0}%"; CpuBar.Value = Math.Clamp(cpu, 0, 100);
            var total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var used = Process.GetProcesses().Sum(p => { try { return (long)p.WorkingSet64; } catch { return 0L; } });
            double ram = total > 0 ? Math.Clamp(used * 100.0 / total, 0, 100) : 0;
            RamValue.Text = $"{ram:0}%"; RamBar.Value = ram;
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.Equals("C:\\", StringComparison.OrdinalIgnoreCase));
            if (drive != null)
            {
                double disk = (drive.TotalSize - drive.AvailableFreeSpace) * 100.0 / drive.TotalSize;
                DiskValue.Text = $"{disk:0}%"; DiskBar.Value = disk;
            }
            SystemInfoText.Text = $"{Environment.OSVersion}\n{Environment.ProcessorCount} processeurs logiques\n{Process.GetProcesses().Length} processus détectés";
        }
        catch (Exception ex) { MainMessage.Text = "Lecture des performances partielle : " + ex.Message; }
    }

    private void SetPage(string title, string subtitle, string message)
    {
        PageTitle.Text = title; PageSubtitle.Text = subtitle; MainMessage.Text = message;
    }
    private void Dashboard_Click(object s, RoutedEventArgs e) { _page = "dashboard"; SetPage("Vue d'ensemble", "Surveillez votre système et préparez votre prochaine session.", ""); }
    private void Optimize_Click(object s, RoutedEventArgs e)
    {
        _page = "optimize";
        SetPage("Optimisation gaming", "Choisissez des réglages réversibles et comprenez leur effet.", "");
        var p = new StackPanel();
        p.Children.Add(Header("PROFILS WINDOWS"));
        p.Children.Add(Body("Le profil haute performance peut augmenter la consommation électrique et la chauffe. Il est réversible."));
        p.Children.Add(ActionButton("Activer Haute performance", EnableHighPerformance_Click));
        p.Children.Add(ActionButton("Restaurer le profil Équilibré", RestoreBalanced_Click));
        p.Children.Add(ActionButton("Ouvrir les paramètres du Mode Jeu", GameMode_Click));
        p.Children.Add(Header("CONSEILS SANS RISQUE"));
        p.Children.Add(Body("Ferme les applications dont tu n'as pas besoin, vérifie les pilotes depuis les sites officiels et laisse les protections Windows activées."));
        ShowDynamic(p);
    }
    private void Processes_Click(object s, RoutedEventArgs e)
    {
        _page = "processes"; SetPage("Gestion des processus", "Trie les applications par mémoire et ferme uniquement ce que tu reconnais.", "");
        var p = new StackPanel();
        p.Children.Add(ActionButton("Actualiser la liste", (_, _) => Processes_Click(s, e)));
        var list = Process.GetProcesses().Select(proc => {
            long mem = 0; try { mem = proc.WorkingSet64; } catch { }
            return new { Proc = proc, Mem = mem, Name = SafeName(proc) };
        }).OrderByDescending(x => x.Mem).Take(30).ToList();
        foreach (var item in list)
        {
            var row = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            var b = new Button { Content = "Terminer", Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(8, 0, 0, 0) };
            b.Click += (_, _) => StopProcess(item.Proc, item.Name);
            DockPanel.SetDock(b, Dock.Right); row.Children.Add(b);
            row.Children.Add(Body($"{item.Name}  •  {item.Mem / 1024 / 1024} Mo  •  PID {item.Proc.Id}"));
            p.Children.Add(row);
        }
        ShowDynamic(p);
    }
    private void Cleanup_Click(object s, RoutedEventArgs e)
    {
        _page = "cleanup"; SetPage("Nettoyage", "Analyse les fichiers temporaires avant de décider de les supprimer.", "");
        var p = new StackPanel();
        p.Children.Add(Body("NEXUS ne touche qu'au dossier temporaire de l'utilisateur. Les fichiers verrouillés ou inaccessibles sont ignorés."));
        p.Children.Add(ActionButton("Analyser l'espace récupérable", (_, _) => AnalyzeTemp(p)));
        p.Children.Add(ActionButton("Nettoyer les fichiers temporaires", (_, _) => CleanTemp()));
        ShowDynamic(p);
    }
    private void Settings_Click(object s, RoutedEventArgs e)
    {
        _page = "settings"; SetPage("Paramètres & journal", "État de l'application, historique et restauration.", "");
        var p = new StackPanel();
        p.Children.Add(Header("JOURNAL DES ACTIONS"));
        p.Children.Add(Body(_log.Count == 0 ? "Aucune action enregistrée." : string.Join("\n", _log.TakeLast(30))));
        p.Children.Add(ActionButton("Ouvrir les paramètres Windows", (_, _) => OpenUrl("ms-settings:")));
        p.Children.Add(ActionButton("Ouvrir les paramètres de démarrage", (_, _) => OpenUrl("ms-settings:startupapps")));
        ShowDynamic(p);
    }
    private void OptimizeNow_Click(object s, RoutedEventArgs e) => Optimize_Click(s, e);

    private void EnableHighPerformance_Click(object? s, RoutedEventArgs e)
    {
        if (MessageBox.Show("Le profil Haute performance peut augmenter la consommation et la température. Continuer ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        RunPowerCfg("/setactive SCHEME_MIN", "Profil Haute performance activé.");
    }
    private void RestoreBalanced_Click(object? s, RoutedEventArgs e)
    {
        if (MessageBox.Show("Revenir au profil Équilibré ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        RunPowerCfg("/setactive SCHEME_BALANCED", "Profil Équilibré demandé.");
    }
    private void RunPowerCfg(string args, string success)
    {
        try {
            var psi = new ProcessStartInfo("powercfg.exe", args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using var p = Process.Start(psi); p?.WaitForExit(5000);
            if (p?.ExitCode == 0) { AddLog(success); MainMessage.Text = success; }
            else MainMessage.Text = "Commande non appliquée. Le profil peut être indisponible ou bloqué par Windows.";
        } catch (Exception ex) { MainMessage.Text = "Impossible de modifier le profil : " + ex.Message; }
    }
    private void GameMode_Click(object? s, RoutedEventArgs e) => OpenUrl("ms-settings:gaming-gamemode");
    private void OpenUrl(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); AddLog("Ouverture : " + uri); }
        catch (Exception ex) { MainMessage.Text = "Impossible d'ouvrir le paramètre : " + ex.Message; }
    }
    private void AnalyzeTemp(StackPanel p)
    {
        try {
            string dir = Path.GetTempPath();
            long bytes = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
            MainMessage.Text = $"Analyse terminée : environ {bytes / 1024 / 1024} Mo dans le dossier temporaire. Certains fichiers peuvent être verrouillés.";
            AddLog("Analyse du dossier temporaire terminée.");
        } catch (Exception ex) { MainMessage.Text = "Analyse partielle : " + ex.Message; }
    }
    private void CleanTemp()
    {
        if (MessageBox.Show("Supprimer les fichiers du dossier temporaire utilisateur ? Les fichiers verrouillés seront ignorés. Continuer ?", "Confirmer le nettoyage", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        int deleted = 0; long bytes = 0;
        try {
            foreach (var file in Directory.EnumerateFiles(Path.GetTempPath(), "*", SearchOption.TopDirectoryOnly)) {
                try { var len = new FileInfo(file).Length; File.Delete(file); deleted++; bytes += len; } catch { }
            }
            string result = $"Nettoyage terminé : {deleted} fichiers supprimés, {bytes / 1024 / 1024} Mo récupérés.";
            MainMessage.Text = result; AddLog(result);
        } catch (Exception ex) { MainMessage.Text = "Nettoyage partiel : " + ex.Message; }
    }
    private void StopProcess(Process proc, string name)
    {
        if (new[] { "System", "Idle", "Registry", "csrss", "wininit", "services", "lsass", "smss", "winlogon" }.Contains(name, StringComparer.OrdinalIgnoreCase))
        { MessageBox.Show("Ce processus est protégé par NEXUS.", "Action bloquée", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (MessageBox.Show($"Forcer la fermeture de « {name} » ? Les données non enregistrées peuvent être perdues.", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { proc.Kill(); AddLog("Processus fermé : " + name); MainMessage.Text = "Processus terminé : " + name; }
        catch (Exception ex) { MainMessage.Text = "Impossible de fermer le processus : " + ex.Message; }
    }
    private static string SafeName(Process p) { try { return p.ProcessName; } catch { return "processus inaccessible"; } }
    private static TextBlock Header(string text) => new() { Text = text, FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 18, 0, 8), Foreground = System.Windows.Media.Brushes.MediumPurple };
    private static TextBlock Body(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.LightGray, Margin = new Thickness(0, 4, 0, 8), FontSize = 13 };
    private static Button ActionButton(string text, RoutedEventHandler handler) { var b = new Button { Content = text, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 6, 0, 6) }; b.Click += handler; return b; }
    private void ShowDynamic(StackPanel panel)
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(panel);
    }
    private void AddLog(string message) => _log.Add($"{DateTime.Now:HH:mm:ss}  {message}");
}
