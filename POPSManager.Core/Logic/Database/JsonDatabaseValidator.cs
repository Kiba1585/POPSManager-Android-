using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using POPSManager.Core.Models;

namespace POPSManager.Core.Logic.Database
{
    public static class JsonDatabaseValidator
    {
        private static readonly Regex UrlRegex = new(@"^https?:\/\/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Ps1Regex = new(@"^(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)_[0-9]{5}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Ps2Regex = new(@"^(SLUS|SCUS|SLES|SCES|SLPM|SLPS)[0-9]{5}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<string> ValidateJsonFile(string path)
        {
            var errors = new List<string>();
            if (!File.Exists(path)) { errors.Add($"Archivo no encontrado: {path}"); return errors; }

            Dictionary<string, GameInfo>? db;
            try { db = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(File.ReadAllText(path)); }
            catch (Exception ex) { errors.Add($"❌ Error de sintaxis JSON: {ex.Message}"); return errors; }

            if (db == null) { errors.Add("❌ El archivo JSON está vacío o no se pudo deserializar."); return errors; }

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in db)
            {
                string id = entry.Key;
                GameInfo info = entry.Value;
                if (!seenKeys.Add(id)) errors.Add($"❌ ID duplicado: {id}");
                if (!Ps1Regex.IsMatch(id) && !Ps2Regex.IsMatch(id)) errors.Add($"❌ ID inválido: {id}");
                if (string.IsNullOrWhiteSpace(info.Name)) errors.Add($"❌ Nombre vacío en ID: {id}");
                if (info.DiscNumber < 1 || info.DiscNumber > 4) errors.Add($"❌ discNumber inválido ({info.DiscNumber}) en ID: {id}");
                if (!string.IsNullOrWhiteSpace(info.CoverUrl) && !UrlRegex.IsMatch(info.CoverUrl)) errors.Add($"❌ coverUrl inválida en ID: {id} → {info.CoverUrl}");
            }

            if (errors.Count == 0) errors.Add("✔ JSON válido. No se encontraron errores.");
            return errors;
        }
    }
}