using System.Text.RegularExpressions;
namespace GSMCheckTool.Services;

public sealed class DeviceInfo
{
    public string Platform { get; set; } = "";
    public string Id { get; set; } = "";
    public Dictionary<string, string> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class DeviceReader
{
    static string FindTool(string name)
    {
        var local = Path.Combine(AppContext.BaseDirectory, "tools", name + ".exe");
        if (File.Exists(local)) return local;
        var nested = Path.Combine(AppContext.BaseDirectory, "tools", "libimobiledevice", name + ".exe");
        if (File.Exists(nested)) return nested;
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        foreach (var d in dirs) { var f = Path.Combine(d, name + ".exe"); if (File.Exists(f)) return f; }
        return "";
    }

    public static async Task<List<DeviceInfo>> DetectAsync()
    {
        var list = new List<DeviceInfo>();
        var adb = FindTool("adb");
        if (adb != "")
        {
            await CommandRunner.RunAsync(adb, "start-server");
            var r = await CommandRunner.RunAsync(adb, "devices -l");
            foreach (var line in r.Output.Split('\n').Skip(1))
            {
                var s = line.Trim();
                if (s.Length == 0 || s.StartsWith("*")) continue;
                var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[1] == "device")
                    list.Add(new DeviceInfo { Platform = "Android", Id = parts[0] });
            }
        }
        var idevice = FindTool("idevice_id");
        if (idevice != "")
        {
            var r = await CommandRunner.RunAsync(idevice, "-l");
            foreach (var id in r.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0))
                if (!list.Any(x => x.Id == id)) list.Add(new DeviceInfo { Platform = "iPhone", Id = id });
        }
        return list;
    }

    public static async Task<DeviceInfo> ReadAndroidAsync(string id)
    {
        var adb = FindTool("adb");
        if (adb == "") throw new Exception("ADB não encontrado. Extraia o pacote completo com a pasta tools.");
        var d = new DeviceInfo { Platform = "Android", Id = id };
        async Task<string> Get(string prop)
        {
            var r = await CommandRunner.RunAsync(adb, $"-s {Q(id)} shell getprop {Q(prop)}");
            return Clean(r.Output);
        }
        d.Fields["Fabricante"] = await Get("ro.product.manufacturer");
        d.Fields["Marca"] = await Get("ro.product.brand");
        d.Fields["Modelo"] = await Get("ro.product.model");
        d.Fields["Nome do dispositivo"] = await Get("ro.product.device");
        d.Fields["Produto"] = await Get("ro.product.name");
        d.Fields["Hardware"] = await Get("ro.hardware");
        d.Fields["Placa"] = await Get("ro.product.board");
        d.Fields["Versão Android"] = await Get("ro.build.version.release");
        d.Fields["SDK Android"] = await Get("ro.build.version.sdk");
        d.Fields["Build / compilação"] = await Get("ro.build.display.id");
        d.Fields["Patch de segurança"] = await Get("ro.build.version.security_patch");
        d.Fields["Bootloader"] = await Get("ro.bootloader");
        d.Fields["Fingerprint do sistema"] = await Get("ro.build.fingerprint");
        d.Fields["Número de série"] = await Get("ro.serialno");
        d.Fields["IMEI"] = "Bloqueado pela segurança Android em muitos aparelhos; não disponível via ADB padrão";
        var bat = await CommandRunner.RunAsync(adb, $"-s {Q(id)} shell dumpsys battery");
        var level = Regex.Match(bat.Output, @"level:\s*(\d+)");
        d.Fields["Bateria (%)"] = level.Success ? level.Groups[1].Value + "%" : "Não disponível";
        var status = Regex.Match(bat.Output, @"status:\s*(\d+)");
        d.Fields["Estado de carga"] = status.Success ? ChargeName(status.Groups[1].Value) : "Não disponível";
        var temp = Regex.Match(bat.Output, @"temperature:\s*(-?\d+)");
        d.Fields["Temperatura da bateria"] = temp.Success ? (int.Parse(temp.Groups[1].Value) / 10.0).ToString("0.0") + " °C" : "Não disponível";
        var cap = await CommandRunner.RunAsync(adb, $"-s {Q(id)} shell df -k /data");
        var row = cap.Output.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.StartsWith("/dev/") && x.Contains("/data"));
        d.Fields["Armazenamento /data"] = row ?? "Não disponível";
        var name = await CommandRunner.RunAsync(adb, $"-s {Q(id)} shell settings get global device_name");
        d.Fields["Nome personalizado"] = Clean(name.Output);
        var sim = await CommandRunner.RunAsync(adb, $"-s {Q(id)} shell getprop gsm.version.baseband");
        d.Fields["Versão de banda base"] = Clean(sim.Output);
        return d;
    }

    public static async Task<DeviceInfo> ReadIPhoneAsync(string id)
    {
        var tool = FindTool("ideviceinfo");
        if (tool == "") throw new Exception("Ferramenta ideviceinfo não encontrada. Extraia a pasta tools/libimobiledevice junto do executável.");
        var d = new DeviceInfo { Platform = "iPhone", Id = id };
        var keys = new (string Label, string Key)[] {
            ("Nome do dispositivo", "DeviceName"), ("Modelo comercial", "MarketingName"),
            ("Identificador do modelo", "ProductType"), ("Modelo de hardware", "HardwareModel"),
            ("Versão iOS", "ProductVersion"), ("Build / compilação iOS", "BuildVersion"),
            ("Número de série", "SerialNumber"), ("IMEI", "InternationalMobileEquipmentIdentity"),
            ("IMEI 2", "InternationalMobileEquipmentIdentity2"), ("MEID", "MobileEquipmentIdentifier"),
            ("Versão do modem", "BasebandVersion"), ("Wi-Fi MAC", "WiFiAddress"),
            ("Bluetooth MAC", "BluetoothAddress"), ("Capacidade total (bytes)", "TotalDiskCapacity"),
            ("Espaço livre (bytes)", "AmountDataAvailable"), ("Operadora", "CarrierBundleInfo"),
            ("Região", "RegionInfo"), ("Estado de ativação", "ActivationState"), ("UDID", "UniqueDeviceID"),
            ("Versão do firmware", "FirmwareVersion"), ("País/região", "RegionCode")
        };
        foreach (var (label, key) in keys)
        {
            var r = await CommandRunner.RunAsync(tool, $"-u {Q(id)} -k {key}");
            var v = Clean(r.Output);
            if (v == "" || v.Contains("Unknown key", StringComparison.OrdinalIgnoreCase) || r.ExitCode != 0)
                v = "Não disponibilizado pelo iOS/ferramenta";
            d.Fields[label] = v;
        }
        var diag = FindTool("idevicediagnostics");
        if (diag != "")
        {
            var r = await CommandRunner.RunAsync(diag, $"-u {Q(id)} diagnostics");
            var m = Regex.Match(r.Output, @"(?i)(?:BatteryCurrentCapacity|BatteryPercent|CurrentCapacity)[^0-9]{0,20}(\d{1,3})");
            d.Fields["Bateria (%)"] = m.Success ? m.Groups[1].Value + "%" : "Não exposta por esta versão do iOS/ferramenta";
        }
        else d.Fields["Bateria (%)"] = "Ferramenta de diagnóstico não disponível";
        return d;
    }

    static string Clean(string s) => s.Trim().Trim('\0');
    static string Q(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";
    static string ChargeName(string s) => s switch { "2" => "Carregando", "3" => "Descarregando", "4" => "Sem bateria", "5" => "Cheia", _ => "Desconhecido" };
}